namespace ListasCompras.Models;

public class ConfiguracaoLoja
{
    public int Id { get; set; }
    public string NomeLoja { get; set; } = "Minha Loja";
    public string? LogoBase64 { get; set; } // data URL (ex: data:image/png;base64,...)

    // Dados que vão para o cabeçalho do PDF/OS do orçamento
    public string? Cnpj { get; set; }
    public string? Telefone { get; set; }
    public string? Email { get; set; }

    // Endereço da loja
    public string? Cep { get; set; }
    public string? Endereco { get; set; }
    public string? Numero { get; set; }
    public string? Bairro { get; set; }
    public string? Cidade { get; set; }
    public string? Uf { get; set; }
}
