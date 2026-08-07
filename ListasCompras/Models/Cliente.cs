namespace ListasCompras.Models;

public class Cliente
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public string? Documento { get; set; } // CPF ou RG

    public string? Cep { get; set; }
    public string? Endereco { get; set; }
    public string? Numero { get; set; }
    public string? Bairro { get; set; }
    public string? Cidade { get; set; }
    public string? Uf { get; set; }

    public string? Observacao { get; set; }
    public DateTime DataCadastro { get; set; } = DateTime.Now;

    // Endereço em uma linha, como sai no cabeçalho da Ordem de Serviço
    public string EnderecoCompleto
    {
        get
        {
            var partes = new List<string>();

            var logradouro = string.Join(", ", new[] { Endereco, Numero }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
            if (!string.IsNullOrWhiteSpace(logradouro)) partes.Add(logradouro);
            if (!string.IsNullOrWhiteSpace(Bairro)) partes.Add(Bairro!);

            var cidadeUf = Cidade;
            if (!string.IsNullOrWhiteSpace(Uf))
                cidadeUf = string.IsNullOrWhiteSpace(cidadeUf) ? Uf : $"{cidadeUf}/{Uf}";
            if (!string.IsNullOrWhiteSpace(cidadeUf)) partes.Add(cidadeUf!);

            if (!string.IsNullOrWhiteSpace(Cep)) partes.Add($"CEP {Cep}");

            return partes.Count > 0 ? string.Join(" - ", partes) : "";
        }
    }
}
