using ListasCompras.Models;

namespace ListasCompras.Tests;

public class ProdutoEstoqueTests
{
    // ===== ValorEmEstoque =====

    [Fact]
    public void ValorEmEstoque_MultiplicaSaldoPorCustoUnitario()
    {
        var produto = new ProdutoEstoque { SaldoAtual = 10, CustoUnitario = 5m };
        Assert.Equal(50m, produto.ValorEmEstoque);
    }

    [Fact]
    public void ValorEmEstoque_SaldoZero_RetornaZero()
    {
        var produto = new ProdutoEstoque { SaldoAtual = 0, CustoUnitario = 5m };
        Assert.Equal(0m, produto.ValorEmEstoque);
    }

    [Fact]
    public void ValorEmEstoque_CustoZero_RetornaZero()
    {
        var produto = new ProdutoEstoque { SaldoAtual = 10, CustoUnitario = 0m };
        Assert.Equal(0m, produto.ValorEmEstoque);
    }

    // ===== Situacao =====

    [Theory]
    [InlineData(0, 10, "esgotado")]
    [InlineData(-5, 10, "esgotado")]
    [InlineData(5, 10, "baixo")]
    [InlineData(5, 5, "baixo")] // saldo == mínimo, ainda "baixo" (borda <=)
    [InlineData(20, 10, "ok")]
    [InlineData(5, 0, "ok")] // EstoqueMinimo == 0 desliga o alerta
    public void Situacao_VariosSaldosEMinimos_RetornaRotuloCorreto(int saldo, int minimo, string esperado)
    {
        var produto = new ProdutoEstoque { SaldoAtual = saldo, EstoqueMinimo = minimo };
        Assert.Equal(esperado, produto.Situacao);
    }

    // ===== SaldoAtualExibido / ValorEmEstoqueExibido =====

    [Fact]
    public void SaldoAtualExibido_ProdutoSimples_IgnoraVariacoesEUsaSaldoProprio()
    {
        var produto = new ProdutoEstoque
        {
            Formato = TiposFormatoProduto.Simples,
            SaldoAtual = 10,
            Variacoes = new List<ProdutoEstoque> { new() { SaldoAtual = 999 } },
        };

        Assert.Equal(10, produto.SaldoAtualExibido);
    }

    [Fact]
    public void SaldoAtualExibido_ProdutoComVariacao_SomaSaldoDasVariacoes()
    {
        var produto = new ProdutoEstoque
        {
            Formato = TiposFormatoProduto.ComVariacao,
            Variacoes = new List<ProdutoEstoque>
            {
                new() { SaldoAtual = 10 },
                new() { SaldoAtual = 5 },
            },
        };

        Assert.Equal(15, produto.SaldoAtualExibido);
    }

    [Fact]
    public void SaldoAtualExibido_ComVariacaoSemNenhumaVariacao_RetornaZero()
    {
        var produto = new ProdutoEstoque { Formato = TiposFormatoProduto.ComVariacao };
        Assert.Equal(0, produto.SaldoAtualExibido);
    }

    [Fact]
    public void ValorEmEstoqueExibido_ProdutoSimples_IgnoraVariacoes()
    {
        var produto = new ProdutoEstoque
        {
            Formato = TiposFormatoProduto.Simples,
            SaldoAtual = 10,
            CustoUnitario = 2m,
            Variacoes = new List<ProdutoEstoque> { new() { SaldoAtual = 999, CustoUnitario = 999m } },
        };

        Assert.Equal(20m, produto.ValorEmEstoqueExibido);
    }

    [Fact]
    public void ValorEmEstoqueExibido_ComVariacao_SomaValorDasVariacoes()
    {
        var produto = new ProdutoEstoque
        {
            Formato = TiposFormatoProduto.ComVariacao,
            Variacoes = new List<ProdutoEstoque>
            {
                new() { SaldoAtual = 10, CustoUnitario = 2m },
                new() { SaldoAtual = 5, CustoUnitario = 3m },
            },
        };

        Assert.Equal(35m, produto.ValorEmEstoqueExibido);
    }

    // ===== SituacaoExibida =====

    [Fact]
    public void SituacaoExibida_ProdutoSimples_DelegaParaSituacao()
    {
        var produto = new ProdutoEstoque { Formato = TiposFormatoProduto.Simples, SaldoAtual = 0 };
        Assert.Equal(produto.Situacao, produto.SituacaoExibida);
        Assert.Equal("esgotado", produto.SituacaoExibida);
    }

    [Fact]
    public void SituacaoExibida_ComVariacaoSemNenhumaVariacao_RetornaEsgotado()
    {
        var produto = new ProdutoEstoque { Formato = TiposFormatoProduto.ComVariacao };
        Assert.Equal("esgotado", produto.SituacaoExibida);
    }

    [Fact]
    public void SituacaoExibida_ComVariacaoSaldoAgregadoZero_RetornaEsgotado()
    {
        var produto = new ProdutoEstoque
        {
            Formato = TiposFormatoProduto.ComVariacao,
            Variacoes = new List<ProdutoEstoque> { new() { SaldoAtual = 0 } },
        };

        Assert.Equal("esgotado", produto.SituacaoExibida);
    }

    [Fact]
    public void SituacaoExibida_ComVariacaoSaldoAbaixoDoMinimoAgregado_RetornaBaixo()
    {
        var produto = new ProdutoEstoque
        {
            Formato = TiposFormatoProduto.ComVariacao,
            Variacoes = new List<ProdutoEstoque>
            {
                new() { SaldoAtual = 2, EstoqueMinimo = 3 },
                new() { SaldoAtual = 1, EstoqueMinimo = 2 },
            },
        };

        // saldo agregado = 3, mínimo agregado = 5 -> baixo
        Assert.Equal("baixo", produto.SituacaoExibida);
    }

    [Fact]
    public void SituacaoExibida_ComVariacaoSemNenhumMinimoDefinido_RetornaOkMesmoComSaldoBaixo()
    {
        var produto = new ProdutoEstoque
        {
            Formato = TiposFormatoProduto.ComVariacao,
            Variacoes = new List<ProdutoEstoque>
            {
                new() { SaldoAtual = 1, EstoqueMinimo = 0 },
            },
        };

        Assert.Equal("ok", produto.SituacaoExibida);
    }
}
