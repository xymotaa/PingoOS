namespace ListasCompras.Models;

public class OrdemServico
{
    // Um cliente com muitos aparelhos é raro; o limite existe porque cada aparelho
    // ocupa uma linha nas duas vias impressas na mesma folha A4.
    public const int MaximoAparelhos = 5;

    /// <summary>CDC, art. 26, II: 90 dias para serviço durável. É o que a OS impressa promete.</summary>
    public const int PrazoGarantiaPadrao = 90;

    public int Id { get; set; }

    // Número sequencial da loja (OS-000001). Antes vinha do relógio no JavaScript
    // e não sobrevivia à impressão.
    public string Numero { get; set; } = string.Empty;

    public DateTime DataAbertura { get; set; } = DateTime.Now;
    public DateTime? DataEntrega { get; set; }
    public string Situacao { get; set; } = Situacoes.Aberta;

    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;

    // Quem emitiu. Preenche a linha "Responsável Técnico" da OS impressa.
    public int? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public string? Diagnostico { get; set; }

    // ===== Garantia =====

    /// <summary>Dias de garantia sobre o serviço. 90 é o mínimo legal do CDC art. 26, II.</summary>
    public int PrazoGarantiaDias { get; set; } = PrazoGarantiaPadrao;

    /// <summary>Preenchido quando esta OS é um retorno em garantia de outra.</summary>
    public int? OrdemOrigemId { get; set; }
    public OrdemServico? OrdemOrigem { get; set; }

    public ICollection<AparelhoOs> Aparelhos { get; set; } = new List<AparelhoOs>();
    public ICollection<ItemOrdemServico> Itens { get; set; } = new List<ItemOrdemServico>();

    // ===== Pagamento =====

    /// <summary>Adiantamento que o cliente deixou na entrada (o "haver").</summary>
    public decimal Sinal { get; set; }

    public decimal Desconto { get; set; }
    public string DescontoTipo { get; set; } = TiposDesconto.Percentual;

    public string? FormaPagamento { get; set; }
    public bool Parcelado { get; set; }
    public int Parcelas { get; set; } = 1;

    // ===== Contas =====

    public decimal Subtotal => Itens.Sum(i => i.Total);

    public decimal DescontoEmReais => DescontoTipo == TiposDesconto.Valor
        ? Math.Min(Desconto, Subtotal)
        : Subtotal * (Math.Min(Desconto, 100m) / 100m);

    public decimal Total => Subtotal - DescontoEmReais;

    /// <summary>O que falta receber depois de abater o sinal.</summary>
    public decimal SaldoAPagar => Math.Max(Total - Sinal, 0m);

    public int QuantidadeParcelas => Parcelado && Parcelas > 1 ? Parcelas : 1;

    public decimal ValorParcela => QuantidadeParcelas > 0
        ? Math.Round(SaldoAPagar / QuantidadeParcelas, 2, MidpointRounding.AwayFromZero)
        : SaldoAPagar;

    // ===== Contas da garantia =====

    /// <summary>A garantia só começa a correr na retirada.</summary>
    public DateTime? GarantiaInicio => DataEntrega;

    public DateTime? GarantiaFim => DataEntrega?.Date.AddDays(PrazoGarantiaDias);

    public bool GarantiaIniciada => DataEntrega.HasValue;

    public bool GarantiaVigente => GarantiaFim.HasValue && GarantiaFim.Value >= DateTime.Today;

    /// <summary>Negativo depois de vencida; nulo enquanto o aparelho não foi retirado.</summary>
    public int? DiasDeGarantiaRestantes => GarantiaFim.HasValue
        ? (int)(GarantiaFim.Value - DateTime.Today).TotalDays
        : null;

    public string SituacaoGarantia =>
        !GarantiaIniciada ? "Não iniciada"
        : GarantiaVigente ? "Vigente"
        : "Vencida";

    public string AparelhosResumo
    {
        get
        {
            if (Aparelhos.Count == 0) return "—";
            var primeiro = Aparelhos.First().Resumo;
            return Aparelhos.Count == 1 ? primeiro : $"{primeiro} +{Aparelhos.Count - 1}";
        }
    }
}

public class ItemOrdemServico
{
    public int Id { get; set; }
    public int OrdemServicoId { get; set; }
    public OrdemServico OrdemServico { get; set; } = null!;

    public string Descricao { get; set; } = string.Empty;
    public int Quantidade { get; set; } = 1;
    public decimal ValorUnitario { get; set; }

    public decimal Total => Quantidade * ValorUnitario;
}

public static class Situacoes
{
    public const string Aberta = "Aberta";
    public const string Pronta = "Pronta";
    public const string Entregue = "Entregue";

    public static readonly string[] Todas = { Aberta, Pronta, Entregue };
}

public static class TiposDesconto
{
    public const string Percentual = "percentual";
    public const string Valor = "valor";
}
