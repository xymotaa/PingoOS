namespace ListasCompras.Models;

// A prova exigida pela cláusula de abandono dos termos da OS: "notificado por escrito".
// Registrado no clique do botão, não depois — não há como saber se o WhatsApp Web
// realmente abriu ou se a mensagem foi enviada, então a prova é a intenção registrada.
public class NotificacaoCliente
{
    public int Id { get; set; }

    public int OrdemServicoId { get; set; }
    public OrdemServico OrdemServico { get; set; } = null!;

    public string Canal { get; set; } = CanaisNotificacao.WhatsApp;
    public string Destinatario { get; set; } = string.Empty; // telefone ou e-mail usado
    public string Mensagem { get; set; } = string.Empty;
    public DateTime DataEnvio { get; set; } = DateTime.Now;

    public int? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
}

public static class CanaisNotificacao
{
    public const string WhatsApp = "WhatsApp";
    public const string Email = "E-mail";
}
