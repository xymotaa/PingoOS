using ListasCompras.Data;
using ListasCompras.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ListasCompras.Controllers;

public class ClienteController : LojaControllerBase
{
    public ClienteController(AppDbContext context) : base(context) { }

    public IActionResult Index()
    {
        return View(Context.Clientes.OrderBy(c => c.Nome).ToList());
    }

    public IActionResult Add(int? id)
    {
        // Mesma tela para cadastrar e editar; sem id é cadastro novo
        var cliente = id.HasValue ? Context.Clientes.Find(id.Value) : null;
        if (id.HasValue && cliente == null) return NotFound();
        return View(cliente ?? new Cliente());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Salvar(Cliente form)
    {
        if (string.IsNullOrWhiteSpace(form.Nome))
        {
            ViewData["Erro"] = "Informe o nome do cliente.";
            return View("Add", form);
        }

        var cliente = form.Id > 0 ? Context.Clientes.Find(form.Id) : null;
        if (form.Id > 0 && cliente == null) return NotFound();

        var novo = cliente == null;
        cliente ??= new Cliente();

        cliente.Nome = form.Nome.Trim();
        cliente.Telefone = Limpar(form.Telefone);
        cliente.Documento = Limpar(form.Documento);
        cliente.Cep = Limpar(form.Cep);
        cliente.Endereco = Limpar(form.Endereco);
        cliente.Numero = Limpar(form.Numero);
        cliente.Bairro = Limpar(form.Bairro);
        cliente.Cidade = Limpar(form.Cidade);
        cliente.Uf = string.IsNullOrWhiteSpace(form.Uf) ? null : form.Uf.Trim().ToUpper();
        cliente.Observacao = Limpar(form.Observacao);

        if (novo) Context.Clientes.Add(cliente);
        Context.SaveChanges();

        TempData["Sucesso"] = novo
            ? $"Cliente {cliente.Nome} cadastrado."
            : $"Cliente {cliente.Nome} atualizado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Excluir(int id)
    {
        var cliente = Context.Clientes.Find(id);
        if (cliente != null)
        {
            Context.Clientes.Remove(cliente);
            Context.SaveChanges();
            TempData["Sucesso"] = $"Cliente {cliente.Nome} excluído.";
        }
        return RedirectToAction(nameof(Index));
    }

    // Busca usada pelo campo de cliente do Orçamento
    [HttpGet]
    public IActionResult Buscar(string? termo)
    {
        var consulta = Context.Clientes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(termo))
        {
            // LIKE em vez de Contains: o EF traduz Contains para instr(), que no SQLite
            // diferencia maiúsculas — "maria" não encontraria "Maria".
            var t = $"%{termo.Trim()}%";
            consulta = consulta.Where(c =>
                EF.Functions.Like(c.Nome, t) ||
                (c.Telefone != null && EF.Functions.Like(c.Telefone, t)) ||
                (c.Documento != null && EF.Functions.Like(c.Documento, t)));
        }

        var resultado = consulta
            .OrderBy(c => c.Nome)
            .Take(20)
            .ToList()
            .Select(c => new
            {
                id = c.Id,
                nome = c.Nome,
                telefone = c.Telefone ?? "",
                documento = c.Documento ?? "",
                cep = c.Cep ?? "",
                endereco = c.Endereco ?? "",
                numero = c.Numero ?? "",
                bairro = c.Bairro ?? "",
                cidade = c.Cidade ?? "",
                uf = c.Uf ?? "",
                enderecoCompleto = c.EnderecoCompleto,
            });

        return Json(resultado);
    }

    private static string? Limpar(string? valor)
    {
        return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    }
}
