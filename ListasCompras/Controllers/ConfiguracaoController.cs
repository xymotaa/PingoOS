using ListasCompras.Data;
using ListasCompras.Models;
using Microsoft.AspNetCore.Mvc;

namespace ListasCompras.Controllers;

public class ConfiguracaoController : LojaControllerBase
{
    public ConfiguracaoController(AppDbContext context) : base(context) { }

    public IActionResult Index()
    {
        var config = Context.ConfiguracoesLoja.FirstOrDefault() ?? new ConfiguracaoLoja();
        return View(config);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Salvar(
        string nomeLoja,
        string? cnpj, string? telefone, string? email,
        string? cep, string? endereco, string? numero, string? bairro, string? cidade, string? uf,
        IFormFile? logo)
    {
        var config = Context.ConfiguracoesLoja.FirstOrDefault();
        if (config == null)
        {
            config = new ConfiguracaoLoja();
            Context.ConfiguracoesLoja.Add(config);
        }

        config.NomeLoja = string.IsNullOrWhiteSpace(nomeLoja) ? config.NomeLoja : nomeLoja;
        config.Cnpj = cnpj;
        config.Telefone = telefone;
        config.Email = email;
        config.Cep = cep;
        config.Endereco = endereco;
        config.Numero = numero;
        config.Bairro = bairro;
        config.Cidade = cidade;
        config.Uf = string.IsNullOrWhiteSpace(uf) ? null : uf.Trim().ToUpper();

        if (logo != null && logo.Length > 0)
        {
            using var stream = new MemoryStream();
            await logo.CopyToAsync(stream);
            var base64 = Convert.ToBase64String(stream.ToArray());
            config.LogoBase64 = $"data:{logo.ContentType};base64,{base64}";
        }

        Context.SaveChanges();

        TempData["Sucesso"] = "Configurações salvas com sucesso!";
        return RedirectToAction(nameof(Index));
    }
}
