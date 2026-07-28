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

    public IActionResult Add()
    {
        return View();
    }

    public IActionResult Edit(string id)
    {
        ViewData["EditarCodigo"] = id;
        return View("Add");
    }
}
