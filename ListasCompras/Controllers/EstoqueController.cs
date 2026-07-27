using ListasCompras.Data;
using ListasCompras.Models;
using Microsoft.AspNetCore.Mvc;

namespace ListasCompras.Controllers;

public class EstoqueController : LojaControllerBase
{
    public EstoqueController(AppDbContext context) : base(context) { }

    public IActionResult Index()
    {
        return View(new EstoqueIndexViewModel
        {
            Produtos = new(),
        });
    }
}
