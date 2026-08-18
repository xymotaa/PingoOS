using ListasCompras.Data;
using ListasCompras.Models;
using Microsoft.AspNetCore.Mvc;

namespace ListasCompras.Controllers;

public class CategoriaController : LojaControllerBase
{
    public CategoriaController(AppDbContext context) : base(context) { }

    // Usado pelo modal "Nova categoria" tanto em Pedidos quanto em Estoque
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Criar(string nome, bool requerModelo = false)
    {
        nome = (nome ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(nome))
            return BadRequest(new { erro = "Informe o nome da categoria." });

        var existente = Context.Categorias.FirstOrDefault(c => c.Nome.ToLower() == nome.ToLower());
        if (existente != null)
            return Ok(new { id = existente.Id, nome = existente.Nome });

        var categoria = new Categoria { Nome = nome, RequerModelo = requerModelo };
        Context.Categorias.Add(categoria);
        Context.SaveChanges();

        return Ok(new { id = categoria.Id, nome = categoria.Nome });
    }
}
