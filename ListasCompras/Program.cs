using ListasCompras.Data;
using ListasCompras.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;

// SQLite — arquivo loja.db na mesma pasta do executável
var dbPath = Path.Combine(AppContext.BaseDirectory, "loja.db");

// Rede de segurança: quem perdeu a senha E o código de recuperação redefine pelo terminal.
// Faz sentido porque o sistema roda local — quem alcança a máquina já alcança o loja.db.
if (args.Length > 0 && args[0] == "redefinir-senha")
{
    return RedefinirSenhaPeloTerminal(dbPath, args);
}

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// Login por cookie. Usamos só o PasswordHasher do Identity (PBKDF2), sem o pacote inteiro.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Conta/Login";
        options.LogoutPath = "/Conta/Sair";
        options.AccessDeniedPath = "/Conta/SemPermissao";

        // Uma jornada de trabalho: renova enquanto o sistema está em uso, mas expira da noite
        // para o dia. Assim o balcão sempre começa o expediente pedindo login.
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddSingleton<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();

// Tudo exige login por padrão; o que é público leva [AllowAnonymous]
var exigirLogin = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AuthorizeFilter(exigirLogin));
});

var app = builder.Build();

// Cria/atualiza o banco e popula dados iniciais
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    SeedData.Initialize(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();

// Sem nenhum usuário cadastrado, o sistema só abre a tela de primeiro acesso
app.Use(async (ctx, next) =>
{
    var caminho = ctx.Request.Path.Value ?? "";
    if (!caminho.StartsWith("/Conta/PrimeiroAcesso", StringComparison.OrdinalIgnoreCase))
    {
        var db = ctx.RequestServices.GetRequiredService<AppDbContext>();
        if (!db.Usuarios.Any())
        {
            ctx.Response.Redirect("/Conta/PrimeiroAcesso");
            return;
        }
    }
    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
return 0;

// Uso: dotnet run -- redefinir-senha <e-mail> <nova senha>
static int RedefinirSenhaPeloTerminal(string dbPath, string[] args)
{
    if (args.Length < 3)
    {
        Console.WriteLine("Uso: redefinir-senha <e-mail> <nova senha>");
        Console.WriteLine("Ex.:  dotnet run -- redefinir-senha dono@loja.com novasenha123");
        return 1;
    }

    var email = args[1].Trim().ToLowerInvariant();
    var senha = args[2];

    if (senha.Length < 6)
    {
        Console.WriteLine("A senha precisa ter ao menos 6 caracteres.");
        return 1;
    }

    var opcoes = new DbContextOptionsBuilder<AppDbContext>()
        .UseSqlite($"Data Source={dbPath}")
        .Options;

    using var db = new AppDbContext(opcoes);
    var usuario = db.Usuarios.FirstOrDefault(u => u.Email == email);
    if (usuario == null)
    {
        Console.WriteLine($"Nenhum usuário com o e-mail {email}.");
        Console.WriteLine("Usuários cadastrados: " + string.Join(", ", db.Usuarios.Select(u => u.Email)));
        return 1;
    }

    var hasher = new PasswordHasher<Usuario>();
    usuario.SenhaHash = hasher.HashPassword(usuario, senha);

    // Reativa a conta: de nada adianta a senha nova se o acesso está desligado
    usuario.Ativo = true;
    db.SaveChanges();

    Console.WriteLine($"Senha de {usuario.Nome} ({usuario.Email}) redefinida.");
    Console.WriteLine("Entre no sistema e gere um novo código de recuperação em Usuários.");
    return 0;
}
