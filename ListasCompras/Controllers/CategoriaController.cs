using ListasCompras.Data;
using ListasCompras.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ListasCompras.Controllers;

public class CategoriaController : LojaControllerBase
{
    public CategoriaController(AppDbContext context) : base(context) { }

    public IActionResult Index()
    {
        var categorias = Context.Categorias
            .OrderBy(c => c.Nome)
            .Select(c => new
            {
                Categoria = c,
                QtdProdutosReposicao = Context.Produtos.Count(p => p.CategoriaId == c.Id),
                QtdProdutosEstoque = Context.ProdutosEstoque.Count(p => p.CategoriaId == c.Id),
            })
            .ToList();

        return View(categorias.Select(x => new CategoriaListaItem
        {
            Id = x.Categoria.Id,
            Nome = x.Categoria.Nome,
            RequerModelo = x.Categoria.RequerModelo,
            QtdProdutos = x.QtdProdutosReposicao + x.QtdProdutosEstoque,
        }).ToList());
    }

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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Editar(int id, string nome, bool requerModelo)
    {
        nome = (nome ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(nome))
        {
            TempData["Erro"] = "Informe o nome da categoria.";
            return RedirectToAction(nameof(Index));
        }

        var categoria = Context.Categorias.Find(id);
        if (categoria == null) return NotFound();

        if (Context.Categorias.Any(c => c.Id != id && c.Nome.ToLower() == nome.ToLower()))
        {
            TempData["Erro"] = $"Já existe uma categoria chamada \"{nome}\".";
            return RedirectToAction(nameof(Index));
        }

        categoria.Nome = nome;
        categoria.RequerModelo = requerModelo;
        Context.SaveChanges();

        TempData["Sucesso"] = $"Categoria \"{categoria.Nome}\" atualizada.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Excluir(int id)
    {
        var categoria = Context.Categorias.Find(id);
        if (categoria == null) return RedirectToAction(nameof(Index));

        var emUso = Context.Produtos.Any(p => p.CategoriaId == id)
                    || Context.ProdutosEstoque.Any(p => p.CategoriaId == id);
        if (emUso)
        {
            TempData["Erro"] = $"A categoria \"{categoria.Nome}\" está em uso por algum produto — " +
                "troque a categoria desses produtos antes de excluir.";
            return RedirectToAction(nameof(Index));
        }

        Context.Categorias.Remove(categoria);
        Context.SaveChanges();
        TempData["Sucesso"] = $"Categoria \"{categoria.Nome}\" excluída.";
        return RedirectToAction(nameof(Index));
    }
}

public class CategoriaListaItem
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public bool RequerModelo { get; set; }
    public int QtdProdutos { get; set; }
}
