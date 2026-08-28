using ListasCompras.Data;
using ListasCompras.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ListasCompras.Controllers;

// MEI tem teto anual de faturamento; passar dele sem perceber desenquadra o regime e
// aumenta a carga tributária. Esta tela soma o que já está gravado contra esse teto — visão
// gerencial, só o administrador acessa (Vendedor/Técnico de Celular usam Financeiro).
[Authorize(Roles = Papeis.Admin)]
public class FaturamentoController : LojaControllerBase
{
    // Padrão vigente do MEI. Fixo por enquanto — virar configurável é ideia anotada no
    // ROADMAP.md para quando a lei mudar o valor ou a loja precisar de outra faixa.
    public const decimal TetoAnualMei = 81_000m;

    public FaturamentoController(AppDbContext context) : base(context) { }

    public IActionResult Index(int? ano)
    {
        var anoConsultado = ano ?? DateTime.Today.Year;

        // Regime de caixa: conta quando o dinheiro entrou, não quando o serviço foi aberto.
        // Na venda é a data da venda; na OS é a entrega, que é quando o saldo é cobrado.
        var totalVendas = Context.Vendas
            .Where(v => v.Data.Year == anoConsultado && !v.Excluida)
            .Include(v => v.Itens)
            .AsEnumerable()
            .Sum(v => v.Total);

        var totalOrdens = Context.OrdensServico
            .Where(o => o.Tipo == TiposDocumento.OrdemServico
                        && o.DataEntrega != null && o.DataEntrega.Value.Year == anoConsultado)
            .Include(o => o.Itens)
            .AsEnumerable()
            .Sum(o => o.Total);

        var totalAno = totalVendas + totalOrdens;

        ViewData["Ano"] = anoConsultado;
        ViewData["AnosDisponiveis"] = AnosComMovimento();
        ViewData["TotalVendas"] = totalVendas;
        ViewData["TotalOrdens"] = totalOrdens;
        ViewData["Teto"] = TetoAnualMei;
        ViewData["Percentual"] = TetoAnualMei > 0 ? Math.Min(totalAno / TetoAnualMei, 1.5m) : 0m;

        return View(totalAno);
    }

    private List<int> AnosComMovimento()
    {
        var anos = Context.Vendas.Select(v => v.Data.Year)
            .Union(Context.OrdensServico
                .Where(o => o.Tipo == TiposDocumento.OrdemServico && o.DataEntrega != null)
                .Select(o => o.DataEntrega!.Value.Year))
            .Distinct()
            .OrderByDescending(a => a)
            .ToList();

        // Sem nenhum dado ainda, oferece ao menos o ano corrente
        return anos.Count > 0 ? anos : new List<int> { DateTime.Today.Year };
    }
}
