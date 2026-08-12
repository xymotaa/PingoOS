namespace ListasCompras.Models;

// Catálogo do que a loja faz, com o preço que costuma cobrar. Serve para o item da
// Ordem de Serviço deixar de ser texto livre — cada técnico escrevendo de um jeito e
// cobrando o que lembra.
public class Servico
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Categoria { get; set; }
    public string? Descricao { get; set; }

    public decimal ValorPadrao { get; set; }

    /// <summary>Serviço que a loja parou de oferecer some do seletor mas não do histórico.</summary>
    public bool Ativo { get; set; } = true;

    public DateTime DataCadastro { get; set; } = DateTime.Now;
}
