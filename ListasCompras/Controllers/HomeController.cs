using System.Diagnostics;
using ListasCompras.Data;
using ListasCompras.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ListasCompras.Controllers;

public class HomeController : LojaControllerBase
{
    public HomeController(AppDbContext context) : base(context) { }

    public IActionResult Index()
    {
        return View();
    }

    // A página de erro precisa aparecer mesmo para quem não está logado
    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
