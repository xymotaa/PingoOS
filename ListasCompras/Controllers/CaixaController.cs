using System.Globalization;
using System.Security.Claims;
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
        return View(Context.ProdutosEstoque.OrderBy(p => p.Nome).ToList());
    }

    public IActionResult Vendas()
    {
        var vendas = Context.Vendas
            .Where(v => !v.Excluida)
            .Include(v => v.Itens)
            .Include(v => v.Usuario)
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
        }).ToList();
        ViewBag.FormaPagamentoEmEdicao = venda.FormaPagamento;
        ViewBag.ValorRecebidoEmEdicao = venda.ValorRecebido;

        return View("Index", Context.ProdutosEstoque.OrderBy(p => p.Nome).ToList());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SalvarEdicao(
        int vendaId, string formaPagamento, string? valorRecebido,
        int[]? itemProdutoId, int[]? itemQuantidade, string[]? itemPreco, string[]? itemDesconto)
    {
        var venda = Context.Vendas.Include(v => v.Itens).FirstOrDefault(v => v.Id == vendaId && !v.Excluida);
        if (venda == null) return NotFound();

        if (itemProdutoId == null || itemProdutoId.Length == 0)
        {
            TempData["Erro"] = "A venda precisa ter ao menos um produto.";
            return RedirectToAction(nameof(EditarVenda), new { id = vendaId });
        }

        var usuarioId = IdDoUsuarioLogado();
        var totalAntes = venda.Total;

        // Devolve tudo que a venda original tinha baixado, depois relança do zero com
        // os itens da edição — mais simples e seguro que comparar item a item o que
        // mudou, e o rastro (estorno + saída nova) fica claro no histórico do produto.
        EstoqueServico.EstornarItensVenda(Context, venda, $"Edição da venda {venda.Numero} — estorno dos itens anteriores", usuarioId);

        Context.ItensVenda.RemoveRange(venda.Itens);
        venda.Itens.Clear();

        venda.FormaPagamento = FormasPagamento.Todas.Contains(formaPagamento) ? formaPagamento : FormasPagamento.Dinheiro;
        venda.ValorRecebido = ParaDecimal(valorRecebido);

        for (var i = 0; i < itemProdutoId.Length; i++)
        {
            var produto = Context.ProdutosEstoque.Find(itemProdutoId[i]);
            if (produto == null) continue;

            var quantidade = itemQuantidade != null && i < itemQuantidade.Length && itemQuantidade[i] > 0
                ? itemQuantidade[i] : 1;

            venda.Itens.Add(new ItemVenda
            {
                ProdutoEstoque = produto,
                Codigo = produto.Codigo,
                Descricao = produto.Nome,
                Quantidade = quantidade,
                PrecoUnitario = itemPreco != null && i < itemPreco.Length ? ParaDecimal(itemPreco[i]) : produto.PrecoVenda,
                DescontoPercentual = itemDesconto != null && i < itemDesconto.Length ? ParaDecimal(itemDesconto[i]) : 0m,
            });

            EstoqueServico.Movimentar(produto, TiposMovimentacao.Saida, quantidade,
                $"Venda {venda.Numero} (editada)", usuarioId, venda);
        }

        if (venda.Itens.Count == 0)
        {
            TempData["Erro"] = "Nenhum dos produtos informados foi encontrado no estoque.";
            return RedirectToAction(nameof(EditarVenda), new { id = vendaId });
        }

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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Finalizar(
        string formaPagamento, string? valorRecebido,
        int[]? itemProdutoId, int[]? itemQuantidade, string[]? itemPreco, string[]? itemDesconto)
    {
        if (itemProdutoId == null || itemProdutoId.Length == 0)
        {
            TempData["Erro"] = "Adicione ao menos um produto antes de finalizar.";
            return RedirectToAction(nameof(Index));
        }

        var venda = new Venda
        {
            Numero = EstoqueServico.ProximoNumeroVenda(Context),
            FormaPagamento = FormasPagamento.Todas.Contains(formaPagamento) ? formaPagamento : FormasPagamento.Dinheiro,
            ValorRecebido = ParaDecimal(valorRecebido),
            UsuarioId = IdDoUsuarioLogado(),
        };

        var semSaldo = new List<string>();

        for (var i = 0; i < itemProdutoId.Length; i++)
        {
            var produto = Context.ProdutosEstoque.Find(itemProdutoId[i]);
            if (produto == null) continue;

            var quantidade = itemQuantidade != null && i < itemQuantidade.Length && itemQuantidade[i] > 0
                ? itemQuantidade[i] : 1;

            venda.Itens.Add(new ItemVenda
            {
                ProdutoEstoque = produto,
                Codigo = produto.Codigo,
                Descricao = produto.Nome,
                Quantidade = quantidade,
                PrecoUnitario = itemPreco != null && i < itemPreco.Length ? ParaDecimal(itemPreco[i]) : produto.PrecoVenda,
                DescontoPercentual = itemDesconto != null && i < itemDesconto.Length ? ParaDecimal(itemDesconto[i]) : 0m,
            });

            if (produto.SaldoAtual < quantidade) semSaldo.Add(produto.Nome);

            // A venda baixa o estoque: é o que liga os dois módulos
            EstoqueServico.Movimentar(produto, TiposMovimentacao.Saida, quantidade,
                $"Venda {venda.Numero}", venda.UsuarioId, venda);
        }

        if (venda.Itens.Count == 0)
        {
            TempData["Erro"] = "Nenhum dos produtos da venda foi encontrado no estoque.";
            return RedirectToAction(nameof(Index));
        }

        Context.Vendas.Add(venda);
        Context.HistoricoVendas.Add(new HistoricoVenda
        {
            Venda = venda,
            Tipo = TiposEventoVenda.Criada,
            UsuarioId = venda.UsuarioId,
        });
        Context.SaveChanges();

        TempData["Sucesso"] = $"Venda {venda.Numero} registrada — {venda.QuantidadeItens} " +
            (venda.QuantidadeItens == 1 ? "item" : "itens") + $", {Moeda(venda.Total)}.";

        // Vendemos o que não tinha na prateleira: avisa, mas a venda está feita
        if (semSaldo.Count > 0)
            TempData["Aviso"] = "Saldo ficou negativo em: " + string.Join(", ", semSaldo.Distinct()) +
                ". Confira a contagem do estoque.";

        return RedirectToAction(nameof(Index));
    }

    private int? IdDoUsuarioLogado()
        => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    private static decimal ParaDecimal(string? valor)
        => decimal.TryParse(valor, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ? d : 0m;

    private static string Moeda(decimal v) => v.ToString("C2", new CultureInfo("pt-BR"));
}
