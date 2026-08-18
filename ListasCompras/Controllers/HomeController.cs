using System.Diagnostics;
using ListasCompras.Data;
using ListasCompras.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ListasCompras.Controllers;

public class HomeController : LojaControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public HomeController(AppDbContext context, IHttpClientFactory httpClientFactory) : base(context)
    {
        _httpClientFactory = httpClientFactory;
    }

    public IActionResult Index()
    {
        return View();
    }

    // Checado via fetch depois que o Painel já carregou — nunca atrasa a tela principal
    // esperando resposta do GitHub. Só administrador vê o aviso: é quem decide atualizar.
    [Authorize(Roles = Papeis.Admin)]
    public async Task<IActionResult> VerificarVersao()
    {
        var local = VersaoServico.VersaoLocal();
        var remota = await VersaoServico.VersaoRemotaAsync(_httpClientFactory.CreateClient());

        return Json(new
        {
            local,
            remota,
            atualizacaoDisponivel = remota != null && VersaoServico.RemotaMaisNova(local, remota),
        });
    }

    // A página de erro precisa aparecer mesmo para quem não está logado
    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
