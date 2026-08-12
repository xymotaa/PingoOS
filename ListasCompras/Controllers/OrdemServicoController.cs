using ListasCompras.Data;
using ListasCompras.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ListasCompras.Controllers;

// A ordem de serviço é o aparelho que ficou na loja: tem termos, assinatura das duas partes,
// garantia contada da retirada e as duas vias impressas.
public class OrdemServicoController : DocumentoControllerBase
{
    public OrdemServicoController(AppDbContext context) : base(context) { }

    protected override string Tipo => TiposDocumento.OrdemServico;

    // Abre uma OS nova já preenchida com o cliente e os aparelhos da original,
    // marcada como retorno. O reparo em garantia não se cobra de novo.
    public IActionResult RetornoGarantia(int id)
    {
        var original = Context.OrdensServico
            .Include(o => o.Cliente)
            .Include(o => o.Aparelhos)
            .FirstOrDefault(o => o.Id == id && o.Tipo == TiposDocumento.OrdemServico);

        if (original == null) return NotFound();

        var retorno = new OrdemServico
        {
            Tipo = TiposDocumento.OrdemServico,
            ClienteId = original.ClienteId,
            Cliente = original.Cliente,
            OrdemOrigemId = original.Id,
            OrdemOrigem = original,
            Diagnostico = $"Retorno em garantia da {original.Numero}. Defeito relatado: ",
        };

        foreach (var ap in original.Aparelhos)
        {
            retorno.Aparelhos.Add(new AparelhoOs
            {
                Tipo = ap.Tipo, Marca = ap.Marca, Modelo = ap.Modelo,
                NumeroSerie = ap.NumeroSerie, SemNumeroSerie = ap.SemNumeroSerie,
            });
        }

        ViewData["EhOrcamento"] = false;
        return View("~/Views/Documento/Add.cshtml", retorno);
    }
}
