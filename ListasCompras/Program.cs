using ListasCompras.Data;
using ListasCompras.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// SQLite — arquivo loja.db na mesma pasta do executável
var dbPath = Path.Combine(AppContext.BaseDirectory, "loja.db");
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