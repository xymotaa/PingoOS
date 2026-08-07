using System.Security.Claims;
using ListasCompras.Data;
using ListasCompras.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ListasCompras.Controllers;

public class ContaController : LojaControllerBase
{
    private readonly IPasswordHasher<Usuario> _hasher;

    public ContaController(AppDbContext context, IPasswordHasher<Usuario> hasher) : base(context)
    {
        _hasher = hasher;
    }

    // ===== Primeiro acesso: cria a conta do dono e os dados da loja =====

    [AllowAnonymous]
    public IActionResult PrimeiroAcesso()
    {
        // Já instalado: ninguém mais entra por aqui
        if (Context.Usuarios.Any()) return RedirectToAction(nameof(Login));
        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PrimeiroAcesso(string nome, string email, string senha, string confirmacao, string nomeLoja)
    {
        if (Context.Usuarios.Any()) return RedirectToAction(nameof(Login));

        var erro = ValidarCadastro(nome, email, senha, confirmacao);
        if (erro != null)
        {
            ViewData["Erro"] = erro;
            return View();
        }

        var admin = new Usuario
        {
            Nome = nome.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            Papel = Papeis.Admin,
        };
        admin.SenhaHash = _hasher.HashPassword(admin, senha);
        Context.Usuarios.Add(admin);

        // Aproveita o primeiro acesso para nomear a loja, em vez do "Minha Loja" genérico
        if (!string.IsNullOrWhiteSpace(nomeLoja) && !Context.ConfiguracoesLoja.Any())
        {
            Context.ConfiguracoesLoja.Add(new ConfiguracaoLoja { NomeLoja = nomeLoja.Trim() });
        }

        Context.SaveChanges();
        await Autenticar(admin);
        return RedirectToAction("Index", "Home");
    }

    // ===== Login / logout =====

    [AllowAnonymous]
    public IActionResult Login(string? returnUrl)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string senha, string? returnUrl)
    {
        ViewData["ReturnUrl"] = returnUrl;

        var normalizado = (email ?? "").Trim().ToLowerInvariant();
        var usuario = Context.Usuarios.FirstOrDefault(u => u.Email == normalizado);

        // Mensagem única para e-mail inexistente e senha errada: não entrega quais e-mails existem
        if (usuario == null || !usuario.Ativo ||
            _hasher.VerifyHashedPassword(usuario, usuario.SenhaHash, senha ?? "") == PasswordVerificationResult.Failed)
        {
            ViewData["Erro"] = "E-mail ou senha incorretos.";
            return View();
        }

        await Autenticar(usuario);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
        return RedirectToAction("Index", "Home");
    }

    public async Task<IActionResult> Sair()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    public IActionResult SemPermissao() => View();

    // ===== Gestão de usuários (só admin) =====

    [Authorize(Roles = Papeis.Admin)]
    public IActionResult Usuarios()
    {
        return View(Context.Usuarios.OrderBy(u => u.Nome).ToList());
    }

    [Authorize(Roles = Papeis.Admin)]
    public IActionResult NovoUsuario() => View();

    [Authorize(Roles = Papeis.Admin)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult NovoUsuario(string nome, string email, string senha, string confirmacao, string papel)
    {
        var erro = ValidarCadastro(nome, email, senha, confirmacao);
        if (erro != null)
        {
            ViewData["Erro"] = erro;
            return View();
        }

        var usuario = new Usuario
        {
            Nome = nome.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            Papel = papel == Papeis.Admin ? Papeis.Admin : Papeis.Tecnico,
        };
        usuario.SenhaHash = _hasher.HashPassword(usuario, senha);
        Context.Usuarios.Add(usuario);
        Context.SaveChanges();

        TempData["Sucesso"] = $"Usuário {usuario.Nome} cadastrado.";
        return RedirectToAction(nameof(Usuarios));
    }

    [Authorize(Roles = Papeis.Admin)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AlternarAtivo(int id)
    {
        var usuario = Context.Usuarios.Find(id);
        if (usuario != null && usuario.Id != IdDoUsuarioLogado())
        {
            usuario.Ativo = !usuario.Ativo;
            Context.SaveChanges();
        }
        return RedirectToAction(nameof(Usuarios));
    }

    // Sem envio de e-mail no sistema, quem esquece a senha depende do admin redefinir
    [Authorize(Roles = Papeis.Admin)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RedefinirSenha(int id, string senha, string confirmacao)
    {
        var usuario = Context.Usuarios.Find(id);
        if (usuario == null) return RedirectToAction(nameof(Usuarios));

        if (senha != confirmacao)
        {
            TempData["Erro"] = "As senhas não conferem.";
        }
        else if ((senha ?? "").Length < 6)
        {
            TempData["Erro"] = "A senha precisa ter ao menos 6 caracteres.";
        }
        else
        {
            usuario.SenhaHash = _hasher.HashPassword(usuario, senha!);
            Context.SaveChanges();
            TempData["Sucesso"] = $"Senha de {usuario.Nome} redefinida.";
        }
        return RedirectToAction(nameof(Usuarios));
    }

    // ===== Auxiliares =====

    private string? ValidarCadastro(string nome, string email, string senha, string confirmacao)
    {
        if (string.IsNullOrWhiteSpace(nome)) return "Informe o nome.";
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@')) return "Informe um e-mail válido.";
        if ((senha ?? "").Length < 6) return "A senha precisa ter ao menos 6 caracteres.";
        if (senha != confirmacao) return "As senhas não conferem.";

        var normalizado = email.Trim().ToLowerInvariant();
        if (Context.Usuarios.Any(u => u.Email == normalizado)) return "Já existe um usuário com esse e-mail.";

        return null;
    }

    private int IdDoUsuarioLogado()
    {
        return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    }

    private async Task Autenticar(Usuario usuario)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.Nome),
            new(ClaimTypes.Email, usuario.Email),
            new(ClaimTypes.Role, usuario.Papel),
        };

        var identidade = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identidade));
    }
}
