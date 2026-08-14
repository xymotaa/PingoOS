using ListasCompras.Data;
using ListasCompras.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ListasCompras.Controllers;

/// <summary>
/// Confere o dinheiro físico da gaveta contra o sistema no fim do expediente. Soma Vendas e
/// pagamentos de Ordem de Serviço do dia, cada um por forma de pagamento — separados de
/// propósito, para não incentivar lançar a mesma OS como venda "para aparecer no fechamento",
/// o que duplicaria o faturamento que a tela de Faturamento MEI já soma.
/// </summary>
public class FechamentoCaixaController : LojaControllerBase
{
    public FechamentoCaixaController(AppDbContext context) : base(context) { }

    public IActionResult Index(DateTime? data)
    {
        var dia = (data ?? DateTime.Today).Date;
        var proximoDia = dia.AddDays(1);

        var vendas = Context.Vendas
            .Where(v => v.Data >= dia && v.Data < proximoDia)
            .Include(v => v.Itens)
            .AsEnumerable()
            .ToList();

        // O pagamento é a fonte da verdade do dia — não o Total da OS, que pode ter sido
        // cobrado em dias diferentes (haver na abertura, saldo na entrega)
        var pagamentosOs = Context.PagamentosOrdemServico
            .Include(p => p.OrdemServico)
            .Where(p => p.Data >= dia && p.Data < proximoDia)
            .ToList();

        var vendasPorForma = vendas
            .GroupBy(v => v.FormaPagamento)
            .ToDictionary(g => FormasPagamento.Rotulo(g.Key), g => g.Sum(v => v.Total));

        var osPorForma = pagamentosOs
            .GroupBy(p => RotuloFormaOs(p.FormaPagamento))
            .ToDictionary(g => g.Key, g => g.Sum(p => p.Valor));

        ViewData["Data"] = dia;
        ViewData["Vendas"] = vendas;
        ViewData["PagamentosOs"] = pagamentosOs;
        ViewData["VendasPorForma"] = vendasPorForma;
        ViewData["OsPorForma"] = osPorForma;
        ViewData["TotalVendas"] = vendas.Sum(v => v.Total);
        ViewData["TotalOs"] = pagamentosOs.Sum(p => p.Valor);

        return View();
    }

    private static string RotuloFormaOs(string forma) => forma switch
    {
        "dinheiro" => "Dinheiro",
        "pix" => "PIX",
        "debito" => "Cartão de débito",
        "credito" => "Cartão de crédito",
        _ => "A combinar",
    };
}
