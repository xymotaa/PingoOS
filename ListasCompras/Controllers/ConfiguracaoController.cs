using ListasCompras.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using ListasCompras.Models;
using Microsoft.AspNetCore.Authorization;
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

    // ===== Backup =====
    // O banco é um arquivo com o cadastro de clientes, as ordens e o histórico. Copiar
    // pelo terminal é o que ninguém faz; por isso o botão.

    [Authorize(Roles = Papeis.Admin)]
    public IActionResult Backup()
    {
        var copia = BackupServico.GerarCopia(Context);
        var bytes = System.IO.File.ReadAllBytes(copia);
        System.IO.File.Delete(copia);

        var loja = Context.ConfiguracoesLoja.FirstOrDefault()?.NomeLoja ?? "loja";
        var nome = $"pingo-os-{Higienizar(loja)}-{DateTime.Now:yyyy-MM-dd-HHmm}.db";

        return File(bytes, "application/octet-stream", nome);
    }

    [Authorize(Roles = Papeis.Admin)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(200_000_000)]
    public async Task<IActionResult> Restaurar(IFormFile? backup, string confirmacao)
    {
        if (confirmacao != "RESTAURAR")
        {
            TempData["Erro"] = "Digite RESTAURAR para confirmar. A restauração substitui todos os dados atuais.";
            return RedirectToAction(nameof(Index));
        }

        if (backup == null || backup.Length == 0)
        {
            TempData["Erro"] = "Escolha o arquivo de backup.";
            return RedirectToAction(nameof(Index));
        }

        // Grava em disco antes de validar: o SQLite precisa de arquivo, não de stream
        var temporario = Path.Combine(Path.GetTempPath(), $"restauracao-{Guid.NewGuid():N}.db");
        await using (var destino = System.IO.File.Create(temporario))
        {
            await backup.CopyToAsync(destino);
        }

        try
        {
            if (!BackupServico.EhBancoValido(temporario, out var problema))
            {
                TempData["Erro"] = problema;
                return RedirectToAction(nameof(Index));
            }

            BackupServico.Restaurar(Context, temporario);
        }
        finally
        {
            if (System.IO.File.Exists(temporario)) System.IO.File.Delete(temporario);
        }

        // A sessão atual pode apontar para um usuário que não existe no banco restaurado
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["Sucesso"] = "Backup restaurado. Entre novamente com os dados do backup.";
        return RedirectToAction("Login", "Conta");
    }

    private static string Higienizar(string nome)
    {
        var limpo = new string(nome.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());
        return string.IsNullOrWhiteSpace(limpo) ? "loja" : limpo.ToLowerInvariant();
    }
}
