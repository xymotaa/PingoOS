using System.Globalization;
using ListasCompras.Data;
using ListasCompras.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ListasCompras.Controllers;

public class ServicoController : LojaControllerBase
{
    public ServicoController(AppDbContext context) : base(context) { }

    public IActionResult Index()
    {
        return View(Context.Servicos.OrderBy(s => s.Nome).ToList());
    }

    public IActionResult Add(int? id)
    {
        var servico = id.HasValue ? Context.Servicos.Find(id.Value) : null;
        if (id.HasValue && servico == null) return NotFound();
        return View(servico ?? new Servico());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Salvar(int id, string nome, string? categoria, string? descricao, string? valor, bool ativo)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            TempData["Erro"] = "Informe o nome do serviço.";
            return RedirectToAction(nameof(Add), id > 0 ? new { id } : null);
        }

        var servico = id > 0 ? Context.Servicos.Find(id) : null;
        if (id > 0 && servico == null) return NotFound();

        var novo = servico == null;
        servico ??= new Servico();

        servico.Nome = nome.Trim();
        servico.Categoria = Limpar(categoria);
        servico.Descricao = Limpar(descricao);
        servico.ValorPadrao = ParaDecimal(valor);
        servico.Ativo = ativo;

        if (novo) Context.Servicos.Add(servico);
        Context.SaveChanges();

        TempData["Sucesso"] = novo ? $"Serviço {servico.Nome} cadastrado." : $"Serviço {servico.Nome} atualizado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Excluir(int id)
    {
        var servico = Context.Servicos.Find(id);
        if (servico == null) return RedirectToAction(nameof(Index));

        // As ordens antigas guardam descrição e valor como texto, não como referência —
        // excluir do catálogo não mexe em nada que já foi feito
        Context.Servicos.Remove(servico);
        Context.SaveChanges();

        TempData["Sucesso"] = $"Serviço {servico.Nome} excluído do catálogo. As ordens antigas não mudam.";
        return RedirectToAction(nameof(Index));
    }

    // Usada pelo seletor de itens do Orçamento
    [HttpGet]
    public IActionResult Buscar(string? termo)
    {
        var consulta = Context.Servicos.Where(s => s.Ativo);

        if (!string.IsNullOrWhiteSpace(termo))
        {
            var t = $"%{termo.Trim()}%";
            consulta = consulta.Where(s =>
                EF.Functions.Like(s.Nome, t) ||
                (s.Categoria != null && EF.Functions.Like(s.Categoria, t)));
        }

        var resultado = consulta.OrderBy(s => s.Nome).Take(30).ToList()
            .Select(s => new
            {
                nome = s.Nome,
                categoria = s.Categoria ?? "",
                descricao = s.Descricao ?? "",
                valor = s.ValorPadrao,
            });

        return Json(resultado);
    }

    private static decimal ParaDecimal(string? valor)
        => decimal.TryParse(valor, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ? d : 0m;

    private static string? Limpar(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
}
