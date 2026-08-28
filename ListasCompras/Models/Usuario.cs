namespace ListasCompras.Models;

public class Usuario
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // Hash PBKDF2 gerado pelo PasswordHasher do ASP.NET Core — nunca a senha em texto
    public string SenhaHash { get; set; } = string.Empty;

    // Código de recuperação, guardado com o mesmo hash da senha: é mostrado uma única vez,
    // no momento em que é gerado, e nem o banco revela qual é.
    public string? CodigoRecuperacaoHash { get; set; }

    public string Papel { get; set; } = Papeis.Tecnico; // Admin | Vendedor | Tecnico
    public bool Ativo { get; set; } = true;
    public DateTime DataCriacao { get; set; } = DateTime.Now;
}

public static class Papeis
{
    public const string Admin = "Admin";
    public const string Vendedor = "Vendedor";

    // Valor salvo no banco continua "Tecnico" por compatibilidade com quem já está cadastrado;
    // só o rótulo exibido virou "Técnico de Celular" (ver Rotulo() abaixo).
    public const string Tecnico = "Tecnico";

    public static readonly string[] Todos = { Admin, Vendedor, Tecnico };

    public static string Rotulo(string papel) => papel switch
    {
        Admin => "Administrador",
        Vendedor => "Vendedor",
        Tecnico => "Técnico de Celular",
        _ => papel,
    };
}
