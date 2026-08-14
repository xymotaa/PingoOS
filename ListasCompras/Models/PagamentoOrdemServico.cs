namespace ListasCompras.Models;

/// <summary>
/// Cada entrada de dinheiro na ordem de serviço, na data em que ela realmente aconteceu — o
/// haver deixado na abertura (ou numa edição) é um registro; o saldo cobrado na entrega é outro.
/// Sem isso, "quanto entrou no caixa hoje" não tem como ser respondido: a OS só guarda o Sinal e
/// o Total como valores únicos, sem dizer em que dia cada parte foi de fato paga.
/// </summary>
public class PagamentoOrdemServico
{
    public int Id { get; set; }

    public int OrdemServicoId { get; set; }
    public OrdemServico OrdemServico { get; set; } = null!;

    public DateTime Data { get; set; } = DateTime.Now;
    public decimal Valor { get; set; }
    public string FormaPagamento { get; set; } = string.Empty;

    /// <summary>Distingue o haver da abertura do saldo cobrado na entrega, só para leitura/relatório.</summary>
    public string Origem { get; set; } = OrigensPagamento.Haver;
}

public static class OrigensPagamento
{
    public const string Haver = "Haver";
    public const string Saldo = "Saldo";
}
