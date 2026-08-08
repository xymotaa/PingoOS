namespace ListasCompras.Models;

public class Venda
{
    public int Id { get; set; }
    public string Numero { get; set; } = string.Empty; // V-000001
    public DateTime Data { get; set; } = DateTime.Now;

    public string FormaPagamento { get; set; } = FormasPagamento.Dinheiro;
    public decimal ValorRecebido { get; set; }  // só faz sentido em dinheiro

    public int? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public ICollection<ItemVenda> Itens { get; set; } = new List<ItemVenda>();

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

    public decimal DescontoTotal => Quantidade * PrecoUnitario * (DescontoPercentual / 100m);
    public decimal Total => Quantidade * PrecoUnitario - DescontoTotal;
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
