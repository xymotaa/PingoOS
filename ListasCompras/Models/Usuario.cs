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

    public string Papel { get; set; } = Papeis.Tecnico; // Admin | Tecnico
    public bool Ativo { get; set; } = true;
    public DateTime DataCriacao { get; set; } = DateTime.Now;
}

public static class Papeis
{
    public const string Admin = "Admin";
    public const string Tecnico = "Tecnico";
}
