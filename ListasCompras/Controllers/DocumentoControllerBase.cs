using ListasCompras.Data;
using ListasCompras.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ListasCompras.Controllers;

/// <summary>
/// Orçamento e ordem de serviço são o mesmo documento em dois momentos do atendimento:
/// o orçamento responde "quanto custa?" no balcão; a ordem de serviço é o aparelho que ficou.
/// Os dois compartilham cliente, aparelhos, itens e valores — por isso compartilham também
/// esta base e as telas em <c>Views/Documento</c>. O que muda vem do <see cref="Tipo"/>.
/// </summary>
public abstract class DocumentoControllerBase : LojaControllerBase
{
    protected DocumentoControllerBase(AppDbContext context) : base(context) { }

    protected abstract string Tipo { get; }

    protected bool EhOrcamento => Tipo == TiposDocumento.Orcamento;

    private string SituacaoInicial => EhOrcamento ? SituacoesOrcamento.Aberto : Situacoes.Aberta;

    private string[] SituacoesValidas => EhOrcamento ? SituacoesOrcamento.Todas : Situacoes.Todas;

    // As três telas servem aos dois documentos; o caminho é explícito para não depender
    // de uma pasta com o nome do controller
    private IActionResult Tela(string nome, object modelo)
    {
        ViewData["EhOrcamento"] = EhOrcamento;
        return View($"~/Views/Documento/{nome}.cshtml", modelo);
    }

    public IActionResult Index()
    {
        var documentos = Context.OrdensServico
            .Include(o => o.Cliente)
            .Include(o => o.Itens)
            .Include(o => o.Aparelhos)
            .Where(o => o.Tipo == Tipo)
            .OrderByDescending(o => o.Id)
            .ToList();

        return Tela("Index", documentos);
    }

    // Mesma tela cria e edita: faltar um dado ou digitar errado é comum, e reabrir o documento
    // é melhor que excluir e refazer, que perderia o número e o histórico
    //
    // "retorno" segue o mesmo padrão já usado em AlterarSituacao: quem chegou aqui a partir da
    // tela Ver leva esse rótulo consigo (via link), e Salvar devolve pra lá em vez de sempre
    // ir pra Index — evita a queixa de "editei e salvei, mas caiu num lugar que eu não esperava".
    public IActionResult Add(int? id, string? retorno)
    {
        ViewBag.Retorno = retorno;

        if (!id.HasValue)
            return Tela("Add", new OrdemServico { Tipo = Tipo, Situacao = SituacaoInicial });

        var documento = Context.OrdensServico
            .Include(o => o.Cliente)
            .Include(o => o.Itens)
            .Include(o => o.Aparelhos)
            .FirstOrDefault(o => o.Id == id.Value && o.Tipo == Tipo);

        if (documento == null) return NotFound();
        return Tela("Add", documento);
    }

    public IActionResult Ver(int id)
    {
        var documento = Context.OrdensServico
            .Include(o => o.Cliente)
            .Include(o => o.Usuario)
            .Include(o => o.Itens)
            .Include(o => o.Aparelhos).ThenInclude(a => a.Fotos)
            .Include(o => o.OrdemOrigem)
            .Include(o => o.OrcamentoOrigem)
            .FirstOrDefault(o => o.Id == id && o.Tipo == Tipo);

        if (documento == null) return NotFound();

        // No orçamento, saber se ele já virou ordem de serviço muda o que a tela oferece
        if (EhOrcamento)
        {
            ViewData["OrdemGerada"] = Context.OrdensServico
                .Where(o => o.OrcamentoOrigemId == id)
                .Select(o => new[] { o.Id.ToString(), o.Numero })
                .FirstOrDefault();
        }
        else
        {
            ViewData["Notificacoes"] = Context.NotificacoesCliente
                .Include(n => n.Usuario)
                .Where(n => n.OrdemServicoId == id)
                .OrderByDescending(n => n.DataEnvio)
                .ToList();
        }

        return Tela("Ver", documento);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Salvar(
        int id, int clienteId, string? diagnostico,
        string[]? aparelhoTipo, string[]? aparelhoMarca, string[]? aparelhoModelo,
        string[]? aparelhoSerie, string[]? aparelhoSemSerie,
        string? sinal, string? desconto, string? descontoTipo,
        string? formaPagamento, bool parcelado, int parcelas, int prazoGarantiaDias, int validadeDias,
        int? ordemOrigemId, string? retorno,
        // Valor chega como texto e é convertido com cultura invariante de propósito: o binding
        // do .NET usa a cultura do sistema, e num Windows/Linux em pt-BR "620.00" viraria 62000.
        string[]? itemDescricao, int[]? itemQuantidade, string[]? itemValor)
    {
        if (clienteId <= 0 || !Context.Clientes.Any(c => c.Id == clienteId))
        {
            TempData["Erro"] = $"Selecione o cliente antes de salvar {(EhOrcamento ? "o orçamento" : "a ordem de serviço")}.";
            return RedirectToAction(nameof(Add), new { id = id > 0 ? id : (int?)null, retorno });
        }

        decimal haverRecebidoAgora = 0m;

        var documento = id > 0
            ? Context.OrdensServico.Include(o => o.Itens).Include(o => o.Aparelhos)
                .FirstOrDefault(o => o.Id == id && o.Tipo == Tipo)
            : null;
        if (id > 0 && documento == null) return NotFound();

        var novo = documento == null;
        if (novo)
        {
            documento = new OrdemServico
            {
                Tipo = Tipo,
                Situacao = SituacaoInicial,
                Numero = ProximoNumero(Tipo),
                UsuarioId = IdDoUsuarioLogado(),
            };
        }
        else
        {
            // Itens e aparelhos são substituídos pelos que vieram do formulário
            Context.ItensOrdemServico.RemoveRange(documento!.Itens);
            documento.Itens.Clear();
            Context.AparelhosOs.RemoveRange(documento.Aparelhos);
            documento.Aparelhos.Clear();
        }

        documento!.ClienteId = clienteId;
        documento.Diagnostico = Limpar(diagnostico);

        documento.Desconto = ParaDecimal(desconto);
        documento.DescontoTipo = descontoTipo == TiposDesconto.Valor ? TiposDesconto.Valor : TiposDesconto.Percentual;

        if (EhOrcamento)
        {
            // Orçamento não recebe dinheiro nem promete garantia: quem faz isso é a ordem
            // de serviço gerada quando o cliente aprova
            documento.ValidadeDias = validadeDias > 0 ? validadeDias : OrdemServico.ValidadePadraoDias;
            documento.Sinal = 0m;
            documento.FormaPagamento = null;
            documento.Parcelado = false;
            documento.Parcelas = 1;
        }
        else
        {
            // O sinal é substituído a cada salvamento, mas só a diferença para cima é dinheiro
            // que entrou na gaveta agora — editar outra coisa da OS não pode recriar o haver
            // com a data de hoje, e reduzir o sinal (correção de digitação) não é uma saída.
            // O registro em si só pode ser feito depois do SaveChanges, quando o Id existe
            // (numa OS nova); por isso a diferença fica guardada aqui só para uso mais abaixo.
            var novoSinal = ParaDecimal(sinal);
            haverRecebidoAgora = novoSinal - documento.Sinal;

            documento.Sinal = novoSinal;
            documento.FormaPagamento = Limpar(formaPagamento);
            documento.Parcelado = parcelado;
            documento.Parcelas = parcelado ? Math.Clamp(parcelas, 1, 24) : 1;

            // 90 dias é o mínimo do CDC; a loja pode prometer mais, nunca menos
            documento.PrazoGarantiaDias = Math.Max(prazoGarantiaDias, OrdemServico.PrazoGarantiaPadrao);

            if (novo && ordemOrigemId.HasValue && Context.OrdensServico.Any(o => o.Id == ordemOrigemId))
                documento.OrdemOrigemId = ordemOrigemId;
        }

        // Aparelhos: um cliente pode deixar mais de um na mesma visita
        if (aparelhoModelo != null)
        {
            for (var i = 0; i < aparelhoModelo.Length && documento.Aparelhos.Count < OrdemServico.MaximoAparelhos; i++)
            {
                var tipo = Limpar(Em(aparelhoTipo, i));
                var marca = Limpar(Em(aparelhoMarca, i));
                var modelo = Limpar(aparelhoModelo[i]);
                var semSerie = Em(aparelhoSemSerie, i) == "1";

                // Bloco vazio do formulário não vira aparelho
                if (tipo == null && marca == null && modelo == null) continue;

                documento.Aparelhos.Add(new AparelhoOs
                {
                    Tipo = tipo,
                    Marca = marca,
                    Modelo = modelo,
                    NumeroSerie = semSerie ? null : Limpar(Em(aparelhoSerie, i)),
                    SemNumeroSerie = semSerie,
                });
            }
        }

        if (itemDescricao != null)
        {
            for (var i = 0; i < itemDescricao.Length; i++)
            {
                var descricao = Limpar(itemDescricao[i]);
                var quantidade = itemQuantidade != null && i < itemQuantidade.Length ? itemQuantidade[i] : 0;
                var valor = itemValor != null && i < itemValor.Length ? ParaDecimal(itemValor[i]) : 0m;

                // Linha em branco no formulário não vira item
                if (descricao == null && valor == 0) continue;

                documento.Itens.Add(new ItemOrdemServico
                {
                    Descricao = descricao ?? "—",
                    Quantidade = quantidade > 0 ? quantidade : 1,
                    ValorUnitario = valor,
                });
            }
        }

        if (novo) Context.OrdensServico.Add(documento);

        // Dois SaveChanges (o pagamento só pode ser criado depois que o documento tem Id,
        // que numa OS nova só existe após o primeiro salvamento) — sem transação, um erro
        // entre os dois deixaria a OS salva sem o pagamento do sinal correspondente.
        using var transacao = Context.Database.BeginTransaction();
        Context.SaveChanges();

        if (haverRecebidoAgora > 0)
        {
            Context.PagamentosOrdemServico.Add(new PagamentoOrdemServico
            {
                OrdemServicoId = documento.Id,
                Valor = haverRecebidoAgora,
                FormaPagamento = documento.FormaPagamento ?? "",
                Origem = OrigensPagamento.Haver,
            });
            Context.SaveChanges();
        }

        transacao.Commit();

        TempData["Sucesso"] = $"{documento.Rotulo} {documento.Numero} {(novo ? "salvo" : "atualizado")}.";

        // Documento recém-criado: continua indo pra Ver, é natural conferir/imprimir na hora.
        // Documento editado: volta pra onde a edição começou — Index se veio da listagem, Ver
        // se veio de lá (mesmo padrão de "retorno" já usado em AlterarSituacao).
        if (novo || retorno != "Index")
            return RedirectToAction(nameof(Ver), new { id = documento.Id });
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AlterarSituacao(int id, string situacao, string? retorno)
    {
        var documento = Context.OrdensServico.Include(o => o.Itens)
            .FirstOrDefault(o => o.Id == id && o.Tipo == Tipo);
        if (documento == null || !SituacoesValidas.Contains(situacao)) return RedirectToAction(nameof(Index));

        documento.Situacao = situacao;

        // A garantia conta da retirada. Voltar atrás limpa a data — foi marcada por engano,
        // então não faz sentido a garantia seguir contando daquele dia.
        if (!EhOrcamento)
        {
            // Não basta olhar DataEntrega: ela é resetada de propósito quando a situação volta
            // atrás (Pronta), e marcar Entregue de novo depois não pode cobrar o saldo duas
            // vezes. Confere se o pagamento do saldo já existe antes de criar outro.
            var saldoJaRegistrado = situacao == Situacoes.Entregue
                && Context.PagamentosOrdemServico.Any(p => p.OrdemServicoId == documento.Id && p.Origem == OrigensPagamento.Saldo);

            if (situacao == Situacoes.Entregue && !saldoJaRegistrado && documento.SaldoAPagar > 0)
            {
                Context.PagamentosOrdemServico.Add(new PagamentoOrdemServico
                {
                    OrdemServicoId = documento.Id,
                    Valor = documento.SaldoAPagar,
                    FormaPagamento = documento.FormaPagamento ?? "",
                    Origem = OrigensPagamento.Saldo,
                });
            }

            if (situacao == Situacoes.Entregue)
                documento.DataEntrega ??= DateTime.Now;
            else
                documento.DataEntrega = null;
        }

        Context.SaveChanges();

        TempData["Sucesso"] = $"{documento.Rotulo} {documento.Numero} agora está como {situacao.ToLower()}.";
        return retorno == "Ver"
            ? RedirectToAction(nameof(Ver), new { id })
            : RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Excluir(int id)
    {
        var documento = Context.OrdensServico
            .Include(o => o.Itens)
            .FirstOrDefault(o => o.Id == id && o.Tipo == Tipo);

        if (documento != null)
        {
            Context.OrdensServico.Remove(documento);
            Context.SaveChanges();
            TempData["Sucesso"] = $"{documento.Rotulo} {documento.Numero} excluído.";
        }
        return RedirectToAction(nameof(Index));
    }

    // ORC-000001 / OS-000001: cada documento tem sua própria sequência, e ela continua de
    // onde parou mesmo se o último for excluído
    protected string ProximoNumero(string tipo)
    {
        var prefixo = tipo == TiposDocumento.Orcamento ? "ORC-" : "OS-";

        var ultimo = Context.OrdensServico
            .Where(o => o.Tipo == tipo)
            .OrderByDescending(o => o.Id)
            .Select(o => o.Numero)
            .FirstOrDefault();

        var sequencia = 0;
        if (ultimo != null && ultimo.StartsWith(prefixo) && int.TryParse(ultimo[prefixo.Length..], out var n))
            sequencia = n;

        return $"{prefixo}{sequencia + 1:D6}";
    }

    private static string? Em(string[]? lista, int i)
        => lista != null && i < lista.Length ? lista[i] : null;

    private static string? Limpar(string? valor)
    {
        return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    }
}
