namespace ListasCompras.Models;

public class Venda
{
    public int Id { get; set; }
    public string Numero { get; set; } = string.Empty; // V-000001
    public DateTime Data { get; set; } = DateTime.Now;

    // Resumo da forma de pagamento predominante — derivado das parcelas ao salvar
    // (ver CaixaController), não digitado direto. Existe pra listagem/filtro de Vendas
    // não precisar abrir as parcelas uma a uma.
    public string FormaPagamento { get; set; } = FormasPagamento.Dinheiro;
    public decimal ValorRecebido { get; set; }  // só faz sentido quando a única parcela é dinheiro

    // Cliente da venda de balcão: texto livre digitado na hora, sem depender do
    // cadastro em Cliente existir — a vendedora só anota o que a pessoa falar.
    public string? ClienteNome { get; set; }
    public string? ClienteTelefone { get; set; }
    public string? ClienteDocumento { get; set; }

    public int? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    // Exclusão nunca apaga a linha — só marca, para o rastro (HistoricoVenda) continuar
    // íntegro e o registro poder ser consultado depois se precisar.
    public bool Excluida { get; set; }
    public int? ExcluidaPorId { get; set; }
    public Usuario? ExcluidaPor { get; set; }
    public DateTime? DataExclusao { get; set; }

    public ICollection<ItemVenda> Itens { get; set; } = new List<ItemVenda>();
    public ICollection<HistoricoVenda> Historico { get; set; } = new List<HistoricoVenda>();
    public ICollection<ParcelaVenda> Parcelas { get; set; } = new List<ParcelaVenda>();

    public decimal Subtotal => Itens.Sum(i => i.Quantidade * i.PrecoUnitario);
    public decimal Desconto => Itens.Sum(i => i.DescontoTotal);
    public decimal Total => Itens.Sum(i => i.Total);
    public decimal Troco => FormaPagamento == FormasPagamento.Dinheiro && ValorRecebido > Total
        ? ValorRecebido - Total
        : 0m;

    public int QuantidadeItens => Itens.Sum(i => i.Quantidade);
}

public class ItemVenda
{
    public int Id { get; set; }

    public int VendaId { get; set; }
    public Venda Venda { get; set; } = null!;

    // O produto pode ser excluído depois; por isso o nome e o preço ficam gravados aqui
    public int? ProdutoEstoqueId { get; set; }
    public ProdutoEstoque? ProdutoEstoque { get; set; }

    public string Codigo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public int Quantidade { get; set; } = 1;
    public decimal PrecoUnitario { get; set; }
    public decimal DescontoPercentual { get; set; }
    public string? Comentario { get; set; }

    public decimal DescontoTotal => Quantidade * PrecoUnitario * (DescontoPercentual / 100m);
    public decimal Total => Quantidade * PrecoUnitario - DescontoTotal;
}

// Uma linha do pagamento — a venda pode ter uma só (à vista) ou várias (parcelado),
// cada uma com sua própria data/valor/forma, editável independente das outras
// (mesmo espírito do parcelamento livre do Bling, mais granular que o da Ordem de
// Serviço — lá é só "quantidade de parcelas" com valor dividido igualmente).
public class ParcelaVenda
{
    public int Id { get; set; }

    public int VendaId { get; set; }
    public Venda Venda { get; set; } = null!;

    public int Numero { get; set; } = 1; // ordem de exibição, 1-based
    public int DiasParaVencer { get; set; }
    public DateTime Data { get; set; } = DateTime.Today;
    public decimal Valor { get; set; }
    public string FormaPagamento { get; set; } = FormasPagamento.Dinheiro;
    public string? Observacao { get; set; }
}

// Trilha de auditoria da venda: nunca edita/apaga um evento antigo, só acrescenta um
// novo — mesmo espírito do histórico de movimentações do Estoque, aplicado aqui porque
// vendas passaram a poder ser editadas/excluídas depois de finalizadas.
public class HistoricoVenda
{
    public int Id { get; set; }

    public int VendaId { get; set; }
    public Venda Venda { get; set; } = null!;

    public string Tipo { get; set; } = TiposEventoVenda.Criada;
    public string? Descricao { get; set; }
    public DateTime Data { get; set; } = DateTime.Now;

    public int? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
}

public static class TiposEventoVenda
{
    public const string Criada = "criada";
    public const string Editada = "editada";
    public const string Excluida = "excluida";
}

public static class FormasPagamento
{
    public const string Dinheiro = "dinheiro";
    public const string Cartao = "cartao";
    public const string Pix = "pix";

    public static readonly string[] Todas = { Dinheiro, Cartao, Pix };

    public static string Rotulo(string forma) => forma switch
    {
        Cartao => "Cartão",
        Pix => "PIX",
        _ => "Dinheiro",
    };
}
