using ListasCompras.Data;
using Microsoft.AspNetCore.Mvc;

namespace ListasCompras.Controllers;

public class OrcamentoController : LojaControllerBase
{
    public OrcamentoController(AppDbContext context) : base(context) { }

    public IActionResult Index()
    {
        return View();
    }
}
