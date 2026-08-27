using ListasCompras.Models;

namespace ListasCompras.Tests;

public class VendaTests
{
    // ===== Subtotal / Desconto / Total / QuantidadeItens =====

    [Fact]
    public void Subtotal_SemItens_RetornaZero()
    {
        var venda = new Venda();
        Assert.Equal(0m, venda.Subtotal);
    }

    [Fact]
    public void Subtotal_Desconto_Total_ComItensVariados_SomamCorretamente()
    {
        var venda = new Venda
        {
            Itens = new List<ItemVenda>
            {
                new() { Quantidade = 2, PrecoUnitario = 50m, DescontoPercentual = 0m },
                new() { Quantidade = 1, PrecoUnitario = 100m, DescontoPercentual = 10m },
            },
        };

        // Subtotal: (2*50) + (1*100) = 200
        Assert.Equal(200m, venda.Subtotal);
        // Desconto: item1 sem desconto (0) + item2 (100 * 0.10 = 10) = 10
        Assert.Equal(10m, venda.Desconto);
        // Total: (100 - 0) + (100 - 10) = 190
        Assert.Equal(190m, venda.Total);
    }

    [Fact]
    public void QuantidadeItens_SomaQuantidadeDeCadaItem()
    {
        var venda = new Venda
        {
            Itens = new List<ItemVenda>
            {
                new() { Quantidade = 2 },
                new() { Quantidade = 3 },
            },
        };

        Assert.Equal(5, venda.QuantidadeItens);
    }

    [Fact]
    public void QuantidadeItens_SemItens_RetornaZero()
    {
        var venda = new Venda();
        Assert.Equal(0, venda.QuantidadeItens);
    }

    // ===== Troco =====

    [Fact]
    public void Troco_DinheiroComValorMaiorQueTotal_RetornaDiferenca()
    {
        var venda = new Venda
        {
            FormaPagamento = FormasPagamento.Dinheiro,
            ValorRecebido = 150m,
            Itens = new List<ItemVenda> { new() { Quantidade = 1, PrecoUnitario = 100m } },
        };

        Assert.Equal(50m, venda.Troco);
    }

    [Fact]
    public void Troco_DinheiroComValorIgualAoTotal_RetornaZero()
    {
        var venda = new Venda
        {
            FormaPagamento = FormasPagamento.Dinheiro,
            ValorRecebido = 100m,
            Itens = new List<ItemVenda> { new() { Quantidade = 1, PrecoUnitario = 100m } },
        };

        Assert.Equal(0m, venda.Troco);
    }

    [Fact]
    public void Troco_DinheiroComValorMenorQueTotal_RetornaZero()
    {
        var venda = new Venda
        {
            FormaPagamento = FormasPagamento.Dinheiro,
            ValorRecebido = 50m,
            Itens = new List<ItemVenda> { new() { Quantidade = 1, PrecoUnitario = 100m } },
        };

        Assert.Equal(0m, venda.Troco);
    }

    [Theory]
    [InlineData(FormasPagamento.Cartao)]
    [InlineData(FormasPagamento.Pix)]
    public void Troco_FormaDiferenteDeDinheiro_RetornaZeroMesmoComValorRecebidoMaior(string forma)
    {
        var venda = new Venda
        {
            FormaPagamento = forma,
            ValorRecebido = 500m, // preenchido por engano/reuso de estado
            Itens = new List<ItemVenda> { new() { Quantidade = 1, PrecoUnitario = 100m } },
        };

        Assert.Equal(0m, venda.Troco);
    }

    // ===== ItemVenda.DescontoTotal / Total =====

    [Fact]
    public void ItemVenda_DescontoTotal_SemDesconto_RetornaZero()
    {
        var item = new ItemVenda { Quantidade = 2, PrecoUnitario = 50m, DescontoPercentual = 0m };
        Assert.Equal(0m, item.DescontoTotal);
        Assert.Equal(100m, item.Total);
    }

    [Fact]
    public void ItemVenda_DescontoTotal_CemPorCento_ZeraOTotal()
    {
        var item = new ItemVenda { Quantidade = 2, PrecoUnitario = 50m, DescontoPercentual = 100m };
        Assert.Equal(100m, item.DescontoTotal);
        Assert.Equal(0m, item.Total);
    }

    [Fact]
    public void ItemVenda_DescontoTotal_PercentualFracionario_CalculaProporcional()
    {
        var item = new ItemVenda { Quantidade = 1, PrecoUnitario = 200m, DescontoPercentual = 12.5m };
        Assert.Equal(25m, item.DescontoTotal);
        Assert.Equal(175m, item.Total);
    }

    // ===== FormasPagamento.Rotulo =====

    [Theory]
    [InlineData(FormasPagamento.Cartao, "Cartão")]
    [InlineData(FormasPagamento.Pix, "PIX")]
    [InlineData(FormasPagamento.Dinheiro, "Dinheiro")]
    [InlineData("valor-desconhecido", "Dinheiro")]
    public void Rotulo_VariasFormasDePagamento_RetornaRotuloCorreto(string forma, string esperado)
    {
        Assert.Equal(esperado, FormasPagamento.Rotulo(forma));
    }
}
