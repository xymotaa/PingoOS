using ListasCompras.Data;
using ListasCompras.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ListasCompras.Controllers;

// A OS impressa promete 90 dias de garantia. Esta tela é onde se confere essa promessa
// quando o cliente volta dizendo "está na garantia".
public class GarantiaController : LojaControllerBase
{
    public GarantiaController(AppDbContext context) : base(context) { }

    public IActionResult Index()
    {
        // Só ordens entregues têm garantia correndo — antes da retirada ela nem começou
        var ordens = Context.OrdensServico
            .Include(o => o.Cliente)
            .Include(o => o.Aparelhos)
            .Include(o => o.Itens)
            .Include(o => o.OrdemOrigem)
            .Where(o => o.Tipo == TiposDocumento.OrdemServico && o.DataEntrega != null)
            .OrderByDescending(o => o.DataEntrega)
            .ToList();

        // Quantos retornos cada ordem já teve
        var retornos = Context.OrdensServico
            .Where(o => o.OrdemOrigemId != null)
            .GroupBy(o => o.OrdemOrigemId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        ViewData["Retornos"] = retornos;
        return View(ordens);
    }
}
