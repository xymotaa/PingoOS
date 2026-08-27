using System.Globalization;
using System.Security.Claims;
using ListasCompras.Data;
using ListasCompras.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ListasCompras.Controllers;

public abstract class LojaControllerBase : Controller
{
    protected readonly AppDbContext Context;

    protected LojaControllerBase(AppDbContext context)
    {
        Context = context;
    }

    // null quando não há sessão válida — nunca 0, que poderia colidir com um Id real.
    protected int? IdDoUsuarioLogado()
        => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    // Cultura invariante de propósito: os formulários mandam ponto decimal, e a
    // cultura do sistema (pt-BR) interpretaria "620.00" como 62000.
    protected static decimal ParaDecimal(string? valor)
        => decimal.TryParse(valor, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ? d : 0m;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var config = Context.ConfiguracoesLoja.FirstOrDefault();
        ViewData["NomeLoja"] = config?.NomeLoja ?? "Minha Loja";
        ViewData["LogoLoja"] = config?.LogoBase64;
        ViewData["LojaCnpj"] = config?.Cnpj;
        ViewData["LojaTelefone"] = config?.Telefone;
        ViewData["LojaEmail"] = config?.Email;
        ViewData["LojaEndereco"] = ComporEndereco(config);
        base.OnActionExecuting(context);
    }

    private static string? ComporEndereco(ConfiguracaoLoja? config)
    {
        if (config == null) return null;

        var partes = new List<string>();
        var logradouro = string.Join(", ", new[] { config.Endereco, config.Numero }
            .Where(s => !string.IsNullOrWhiteSpace(s)));
        if (!string.IsNullOrWhiteSpace(logradouro)) partes.Add(logradouro);
        if (!string.IsNullOrWhiteSpace(config.Bairro)) partes.Add(config.Bairro!);

        var cidadeUf = config.Cidade;
        if (!string.IsNullOrWhiteSpace(config.Uf))
            cidadeUf = string.IsNullOrWhiteSpace(cidadeUf) ? config.Uf : $"{cidadeUf}/{config.Uf}";
        if (!string.IsNullOrWhiteSpace(cidadeUf)) partes.Add(cidadeUf!);

        if (!string.IsNullOrWhiteSpace(config.Cep)) partes.Add($"CEP {config.Cep}");

        return partes.Count > 0 ? string.Join(" - ", partes) : null;
    }
}
