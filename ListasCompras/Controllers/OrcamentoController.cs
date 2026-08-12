using ListasCompras.Data;
using ListasCompras.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ListasCompras.Controllers;

// O orçamento é a pergunta do balcão: "quanto custa a frontal desse aparelho?". Não tem
// termos, assinatura nem garantia — nada foi autorizado ainda, e o aparelho nem ficou na loja.
public class OrcamentoController : DocumentoControllerBase
{
    public OrcamentoController(AppDbContext context) : base(context) { }

    protected override string Tipo => TiposDocumento.Orcamento;

    /// <summary>
    /// Cliente aprovou: o orçamento vira ordem de serviço, levando junto cliente, aparelhos,
    /// diagnóstico e itens. É aqui que o documento passa a ter garantia, termos e assinatura.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult GerarOrdem(int id)
    {
        var orcamento = Context.OrdensServico
            .Include(o => o.Aparelhos)
            .Include(o => o.Itens)
            .FirstOrDefault(o => o.Id == id && o.Tipo == TiposDocumento.Orcamento);

        if (orcamento == null) return NotFound();

        // Aprovar duas vezes não pode gerar duas ordens para o mesmo serviço
        var jaGerada = Context.OrdensServico.FirstOrDefault(o => o.OrcamentoOrigemId == id);
        if (jaGerada != null)
        {
            TempData["Sucesso"] = $"Este orçamento já virou a ordem {jaGerada.Numero}.";
            return RedirectToAction("Ver", "OrdemServico", new { id = jaGerada.Id });
        }

        var ordem = new OrdemServico
        {
            Tipo = TiposDocumento.OrdemServico,
            Numero = ProximoNumero(TiposDocumento.OrdemServico),
            Situacao = Situacoes.Aberta,
            UsuarioId = IdDoUsuarioLogado(),
            ClienteId = orcamento.ClienteId,
            Diagnostico = orcamento.Diagnostico,
            Desconto = orcamento.Desconto,
            DescontoTipo = orcamento.DescontoTipo,
            OrcamentoOrigemId = orcamento.Id,
        };

        foreach (var ap in orcamento.Aparelhos)
        {
            ordem.Aparelhos.Add(new AparelhoOs
            {
                Tipo = ap.Tipo, Marca = ap.Marca, Modelo = ap.Modelo,
                NumeroSerie = ap.NumeroSerie, SemNumeroSerie = ap.SemNumeroSerie,
            });
        }

        foreach (var item in orcamento.Itens)
        {
            ordem.Itens.Add(new ItemOrdemServico
            {
                Descricao = item.Descricao,
                Quantidade = item.Quantidade,
                ValorUnitario = item.ValorUnitario,
            });
        }

        // O orçamento fica no histórico como aprovado; quem manda a partir daqui é a ordem
        orcamento.Situacao = SituacoesOrcamento.Aprovado;

        Context.OrdensServico.Add(ordem);
        Context.SaveChanges();

        TempData["Sucesso"] = $"Orçamento {orcamento.Numero} aprovado — ordem {ordem.Numero} aberta. "
            + "Confira a garantia e o pagamento antes de imprimir.";
        return RedirectToAction("Ver", "OrdemServico", new { id = ordem.Id });
    }
}
