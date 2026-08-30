using System.Globalization;
using ListasCompras.Data;
using ListasCompras.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ListasCompras.Controllers;

public class CaixaController : LojaControllerBase
{
    public CaixaController(AppDbContext context) : base(context) { }

    public IActionResult Index()
    {
        ViewBag.VendaEmEdicaoId = 0;
        return View(ProdutosVendaveis());
    }

    public IActionResult Vendas()
    {
        var vendas = Context.Vendas
            .Where(v => !v.Excluida)
            .Include(v => v.Itens)
            .Include(v => v.Usuario)
            .Include(v => v.Parcelas)
            .OrderByDescending(v => v.Id)
            .ToList();

        return View(vendas);
    }

    // Reaproveita a mesma tela/JS do PDV (Index.cshtml + caixa.js): o carrinho nasce
    // preenchido com os itens da venda, e o formulário final aponta para SalvarEdicao
    // em vez de Finalizar — ver ViewBag.VendaEmEdicaoId / ViewBag.ItensEmEdicao.
    public IActionResult EditarVenda(int id)
    {
        var venda = Context.Vendas
            .Include(v => v.Itens)
            .Include(v => v.Parcelas)
            .FirstOrDefault(v => v.Id == id && !v.Excluida);
        if (venda == null) return NotFound();

        ViewBag.VendaEmEdicaoId = venda.Id;
        ViewBag.VendaEmEdicaoNumero = venda.Numero;
        ViewBag.ItensEmEdicao = venda.Itens.Select(i => new
        {
            id = i.ProdutoEstoqueId ?? 0,
            codigo = i.Codigo,
            nome = i.Descricao,
            precoUnitario = i.PrecoUnitario,
            qtd = i.Quantidade,
            desconto = i.DescontoPercentual,
            descontoTipo = "percentual",
            comentario = i.Comentario,
        }).ToList();
        ViewBag.ClienteEmEdicao = new
        {
            nome = venda.ClienteNome,
            telefone = venda.ClienteTelefone,
            documento = venda.ClienteDocumento,
        };
        ViewBag.ParcelasEmEdicao = venda.Parcelas.OrderBy(p => p.Numero).Select(p => new
        {
            dias = p.DiasParaVencer,
            data = p.Data.ToString("yyyy-MM-dd"),
            valor = p.Valor,
            formaPagamento = p.FormaPagamento,
            observacao = p.Observacao,
        }).ToList();
        ViewBag.ValorRecebidoEmEdicao = venda.ValorRecebido;

        return View("Index", ProdutosVendaveis());
    }

    // Só produtos raiz (simples ou pai de variação) — variação nunca aparece sozinha
    // na busca do Caixa. Inclui ModelosCompativeis para montar, na view, a lista de
    // "produtos que servem no mesmo aparelho" exibida como sugestão na busca.
    private List<ProdutoEstoque> ProdutosVendaveis()
        => Context.ProdutosEstoque
            .Include(p => p.Variacoes)
            .Include(p => p.ModelosCompativeis)
            .Where(p => p.ProdutoPaiId == null)
            .OrderBy(p => p.Nome).ToList();

    // Monta as linhas de ItemVenda a partir dos arrays paralelos que o carrinho do PDV
    // manda no POST — usado tanto por Finalizar quanto por SalvarEdicao.
    private List<ItemVenda> MontarItens(
        int[] itemProdutoId, int[]? itemQuantidade, string[]? itemPreco, string[]? itemDesconto, string[]? itemComentario,
        string numeroVenda, int? usuarioId, Venda venda, List<string> semSaldo)
    {
        var produtosPorId = Context.ProdutosEstoque
            .Where(p => itemProdutoId.Contains(p.Id))
            .ToDictionary(p => p.Id);

        var itens = new List<ItemVenda>();
        for (var i = 0; i < itemProdutoId.Length; i++)
        {
            if (!produtosPorId.TryGetValue(itemProdutoId[i], out var produto)) continue;

            var quantidade = itemQuantidade != null && i < itemQuantidade.Length && itemQuantidade[i] > 0
                ? itemQuantidade[i] : 1;

            itens.Add(new ItemVenda
            {
                ProdutoEstoque = produto,
                Codigo = produto.Codigo,
                Descricao = produto.Nome,
                Quantidade = quantidade,
                PrecoUnitario = itemPreco != null && i < itemPreco.Length ? ParaDecimal(itemPreco[i]) : produto.PrecoVenda,
                DescontoPercentual = itemDesconto != null && i < itemDesconto.Length ? ParaDecimal(itemDesconto[i]) : 0m,
                Comentario = itemComentario != null && i < itemComentario.Length && !string.IsNullOrWhiteSpace(itemComentario[i])
                    ? itemComentario[i].Trim() : null,
            });

            if (produto.SaldoAtual < quantidade) semSaldo.Add(produto.Nome);

            // A venda baixa o estoque: é o que liga os dois módulos
            EstoqueServico.Movimentar(produto, TiposMovimentacao.Saida, quantidade, $"Venda {numeroVenda}", usuarioId, venda);
        }
        return itens;
    }

    // Uma parcela por trio (dias/data/valor/forma/observação) nos arrays paralelos vindos
    // do PDV. Sem parcela nenhuma informada, cai numa única parcela à vista na forma
    // padrão — cobre o caminho comum (dinheiro/cartão/pix à vista) sem forçar a
    // vendedora a preencher a tabela de parcelas toda vez.
    private List<ParcelaVenda> MontarParcelas(
        int[]? parcelaDias, string[]? parcelaData, string[]? parcelaValor, string[]? parcelaForma, string[]? parcelaObservacao,
        decimal total)
    {
        if (parcelaValor == null || parcelaValor.Length == 0)
        {
            return new List<ParcelaVenda>
            {
                new() { Numero = 1, DiasParaVencer = 0, Data = DateTime.Today, Valor = total, FormaPagamento = FormasPagamento.Dinheiro },
            };
        }

        var parcelas = new List<ParcelaVenda>();
        for (var i = 0; i < parcelaValor.Length; i++)
        {
            var forma = parcelaForma != null && i < parcelaForma.Length && FormasPagamento.Todas.Contains(parcelaForma[i])
                ? parcelaForma[i] : FormasPagamento.Dinheiro;
            var data = parcelaData != null && i < parcelaData.Length && DateTime.TryParse(parcelaData[i], out var d) ? d : DateTime.Today;

            parcelas.Add(new ParcelaVenda
            {
                Numero = i + 1,
                DiasParaVencer = parcelaDias != null && i < parcelaDias.Length ? parcelaDias[i] : 0,
                Data = data,
                Valor = ParaDecimal(parcelaValor[i]),
                FormaPagamento = forma,
                Observacao = parcelaObservacao != null && i < parcelaObservacao.Length && !string.IsNullOrWhiteSpace(parcelaObservacao[i])
                    ? parcelaObservacao[i].Trim() : null,
            });
        }
        return parcelas;
    }

    // Forma de pagamento "resumo" da venda (ver comentário em Models/Venda.cs): a
    // primeira parcela representa a venda nas listagens; ValorRecebido só faz sentido
    // quando há uma única parcela em dinheiro (onde troco é calculado).
    private static void AplicarResumoPagamento(Venda venda, List<ParcelaVenda> parcelas, string? valorRecebido)
    {
        var primeira = parcelas.OrderBy(p => p.Numero).First();
        venda.FormaPagamento = primeira.FormaPagamento;
        venda.ValorRecebido = parcelas.Count == 1 && primeira.FormaPagamento == FormasPagamento.Dinheiro
            ? ParaDecimal(valorRecebido) : 0m;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SalvarEdicao(
        int vendaId, string? valorRecebido, string? clienteNome, string? clienteTelefone, string? clienteDocumento,
        int[]? itemProdutoId, int[]? itemQuantidade, string[]? itemPreco, string[]? itemDesconto, string[]? itemComentario,
        int[]? parcelaDias, string[]? parcelaData, string[]? parcelaValor, string[]? parcelaForma, string[]? parcelaObservacao)
    {
        var venda = Context.Vendas.Include(v => v.Itens).Include(v => v.Parcelas).FirstOrDefault(v => v.Id == vendaId && !v.Excluida);
        if (venda == null) return NotFound();

        if (itemProdutoId == null || itemProdutoId.Length == 0)
        {
            TempData["Erro"] = "A venda precisa ter ao menos um produto.";
            return RedirectToAction(nameof(EditarVenda), new { id = vendaId });
        }

        var usuarioId = IdDoUsuarioLogado();
        var totalAntes = venda.Total;

        venda.ClienteNome = string.IsNullOrWhiteSpace(clienteNome) ? null : clienteNome.Trim();
        venda.ClienteTelefone = string.IsNullOrWhiteSpace(clienteTelefone) ? null : clienteTelefone.Trim();
        venda.ClienteDocumento = string.IsNullOrWhiteSpace(clienteDocumento) ? null : clienteDocumento.Trim();

        // Devolve tudo que a venda original tinha baixado, depois relança do zero com
        // os itens da edição — mais simples e seguro que comparar item a item o que
        // mudou, e o rastro (estorno + saída nova) fica claro no histórico do produto.
        EstoqueServico.EstornarItensVenda(Context, venda, $"Edição da venda {venda.Numero} — estorno dos itens anteriores", usuarioId);

        Context.ItensVenda.RemoveRange(venda.Itens);
        venda.Itens.Clear();
        Context.ParcelasVenda.RemoveRange(venda.Parcelas);
        venda.Parcelas.Clear();

        var semSaldo = new List<string>();
        var itens = MontarItens(itemProdutoId, itemQuantidade, itemPreco, itemDesconto, itemComentario, venda.Numero, usuarioId, venda, semSaldo);
        foreach (var item in itens) venda.Itens.Add(item);

        if (venda.Itens.Count == 0)
        {
            TempData["Erro"] = "Nenhum dos produtos informados foi encontrado no estoque.";
            return RedirectToAction(nameof(EditarVenda), new { id = vendaId });
        }

        var parcelas = MontarParcelas(parcelaDias, parcelaData, parcelaValor, parcelaForma, parcelaObservacao, venda.Total);
        foreach (var parcela in parcelas) venda.Parcelas.Add(parcela);
        AplicarResumoPagamento(venda, parcelas, valorRecebido);

        Context.HistoricoVendas.Add(new HistoricoVenda
        {
            VendaId = venda.Id,
            Tipo = TiposEventoVenda.Editada,
            Descricao = $"Total mudou de {Moeda(totalAntes)} para {Moeda(venda.Total)}.",
            UsuarioId = usuarioId,
        });

        Context.SaveChanges();

        TempData["Sucesso"] = $"Venda {venda.Numero} atualizada.";
        return RedirectToAction(nameof(Vendas));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ExcluirVenda(int id, string? motivo)
    {
        var venda = Context.Vendas.Include(v => v.Itens).FirstOrDefault(v => v.Id == id && !v.Excluida);
        if (venda == null) return NotFound();

        var usuarioId = IdDoUsuarioLogado();

        EstoqueServico.EstornarItensVenda(Context, venda, $"Exclusão da venda {venda.Numero}", usuarioId);

        venda.Excluida = true;
        venda.ExcluidaPorId = usuarioId;
        venda.DataExclusao = DateTime.Now;

        Context.HistoricoVendas.Add(new HistoricoVenda
        {
            VendaId = venda.Id,
            Tipo = TiposEventoVenda.Excluida,
            Descricao = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim(),
            UsuarioId = usuarioId,
        });

        Context.SaveChanges();

        TempData["Sucesso"] = $"Venda {venda.Numero} excluída. O estoque foi devolvido.";
        return RedirectToAction(nameof(Vendas));
    }

    public IActionResult HistoricoVenda(int id)
    {
        var venda = Context.Vendas
            .Include(v => v.Historico).ThenInclude(h => h.Usuario)
            .FirstOrDefault(v => v.Id == id);
        if (venda == null) return NotFound();

        venda.Historico = venda.Historico.OrderByDescending(h => h.Id).ToList();
        return View(venda);
    }

    // Comprovante não fiscal (cupom térmico 80mm) — sem valor fiscal, só o registro
    // da compra para o cliente. Aberto do painel de fechamento da venda.
    public IActionResult Recibo(int id)
    {
        var venda = Context.Vendas
            .Include(v => v.Itens)
            .Include(v => v.Parcelas)
            .Include(v => v.Usuario)
            .FirstOrDefault(v => v.Id == id && !v.Excluida);
        if (venda == null) return NotFound();

        venda.Parcelas = venda.Parcelas.OrderBy(p => p.Numero).ToList();
        return View(venda);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Finalizar(
        string? valorRecebido, string? clienteNome, string? clienteTelefone, string? clienteDocumento,
        int[]? itemProdutoId, int[]? itemQuantidade, string[]? itemPreco, string[]? itemDesconto, string[]? itemComentario,
        int[]? parcelaDias, string[]? parcelaData, string[]? parcelaValor, string[]? parcelaForma, string[]? parcelaObservacao)
    {
        // O PDV envia por fetch (para abrir o painel de fechamento sem navegar);
        // outras chamadas ao mesmo endpoint continuam recebendo o redirect tradicional.
        var viaAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        if (itemProdutoId == null || itemProdutoId.Length == 0)
        {
            if (viaAjax) return BadRequest(new { erro = "Adicione ao menos um produto antes de finalizar." });
            TempData["Erro"] = "Adicione ao menos um produto antes de finalizar.";
            return RedirectToAction(nameof(Index));
        }

        var venda = new Venda
        {
            Numero = EstoqueServico.ProximoNumeroVenda(Context),
            ClienteNome = string.IsNullOrWhiteSpace(clienteNome) ? null : clienteNome.Trim(),
            ClienteTelefone = string.IsNullOrWhiteSpace(clienteTelefone) ? null : clienteTelefone.Trim(),
            ClienteDocumento = string.IsNullOrWhiteSpace(clienteDocumento) ? null : clienteDocumento.Trim(),
            UsuarioId = IdDoUsuarioLogado(),
        };

        var semSaldo = new List<string>();
        var itens = MontarItens(itemProdutoId, itemQuantidade, itemPreco, itemDesconto, itemComentario, venda.Numero, venda.UsuarioId, venda, semSaldo);
        foreach (var item in itens) venda.Itens.Add(item);

        if (venda.Itens.Count == 0)
        {
            if (viaAjax) return BadRequest(new { erro = "Nenhum dos produtos da venda foi encontrado no estoque." });
            TempData["Erro"] = "Nenhum dos produtos da venda foi encontrado no estoque.";
            return RedirectToAction(nameof(Index));
        }

        var parcelas = MontarParcelas(parcelaDias, parcelaData, parcelaValor, parcelaForma, parcelaObservacao, venda.Total);
        foreach (var parcela in parcelas) venda.Parcelas.Add(parcela);
        AplicarResumoPagamento(venda, parcelas, valorRecebido);

        Context.Vendas.Add(venda);
        Context.HistoricoVendas.Add(new HistoricoVenda
        {
            Venda = venda,
            Tipo = TiposEventoVenda.Criada,
            UsuarioId = venda.UsuarioId,
        });
        Context.SaveChanges();

        var aviso = semSaldo.Count > 0
            ? "Saldo ficou negativo em: " + string.Join(", ", semSaldo.Distinct()) + ". Confira a contagem do estoque."
            : null;

        if (viaAjax)
        {
            return Json(new
            {
                id = venda.Id,
                numero = venda.Numero,
                total = venda.Total,
                quantidadeItens = venda.QuantidadeItens,
                formaPagamento = venda.FormaPagamento,
                itens = venda.Itens.Select(i => new { i.Descricao, i.Quantidade, total = i.Total }),
                aviso,
            });
        }

        TempData["Sucesso"] = $"Venda {venda.Numero} registrada — {venda.QuantidadeItens} " +
            (venda.QuantidadeItens == 1 ? "item" : "itens") + $", {Moeda(venda.Total)}.";

        // Vendemos o que não tinha na prateleira: avisa, mas a venda está feita
        if (aviso != null) TempData["Aviso"] = aviso;

        return RedirectToAction(nameof(Index));
    }

    private static string Moeda(decimal v) => v.ToString("C2", new CultureInfo("pt-BR"));
}
