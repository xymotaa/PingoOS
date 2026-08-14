namespace ListasCompras.Models;

/// <summary>
/// Entrada ou saída manual de dinheiro que não é venda nem ordem de serviço: aluguel, conta de
/// luz, retirada do dono, compra de material de uso. Vendas e OS já são a entrada — lançar de
/// novo aqui duplicaria o faturamento, então este módulo é só para o que mais nenhuma tela grava.
/// </summary>
public class LancamentoFinanceiro
{
    public int Id { get; set; }

    public DateTime Data { get; set; } = DateTime.Now;
    public string Tipo { get; set; } = TiposLancamento.Saida;
    public string Descricao { get; set; } = string.Empty;
    public string? Categoria { get; set; }
    public decimal Valor { get; set; }

    public int? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public DateTime DataCadastro { get; set; } = DateTime.Now;
}

public static class TiposLancamento
{
    public const string Entrada = "Entrada";
    public const string Saida = "Saida";

    public static readonly string[] Todas = { Entrada, Saida };
}

/// <summary>Um compromisso com vencimento — aluguel do mês que vem, fornecedor a prazo — antes
/// de virar um lançamento de saída de fato pago.</summary>
public class ContaAPagar
{
    public int Id { get; set; }

    public string Descricao { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public DateTime Vencimento { get; set; }
    public bool Paga { get; set; }
    public DateTime? DataPagamento { get; set; }

    /// <summary>Preenchido quando a conta é marcada como paga: vira uma saída no fluxo do dia.</summary>
    public int? LancamentoId { get; set; }
    public LancamentoFinanceiro? Lancamento { get; set; }

    public DateTime DataCadastro { get; set; } = DateTime.Now;

    public bool Vencida => !Paga && Vencimento.Date < DateTime.Today;
}
