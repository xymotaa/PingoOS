using ListasCompras.Data;
using ListasCompras.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ListasCompras.Controllers;

// Números consolidados da loja: só o dono/administrador vê. Vendedor e Técnico de Celular
// usam Vendas/Financeiro no dia a dia, mas não têm acesso à visão gerencial.
[Authorize(Roles = Papeis.Admin)]
public class DashboardsController : LojaControllerBase
{
    public DashboardsController(AppDbContext context) : base(context) { }

    public IActionResult Index()
    {
        return View("EmBreve", new ModuloEmBreveViewModel
        {
            Icone = "📊",
            Nome = "Dashboards",
            Descricao = "Veja os números da loja em gráficos e indicadores.",
            Recursos = new()
            {
                "Vendas por período e por categoria",
                "Produtos mais vendidos",
                "Evolução de estoque e reposição",
                "Indicadores de faturamento",
            },
        });
    }
}
