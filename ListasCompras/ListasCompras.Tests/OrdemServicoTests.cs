using ListasCompras.Models;

namespace ListasCompras.Tests;

public class OrdemServicoTests
{
    // ===== EhOrcamento / Rotulo / SituacoesPossiveis =====

    [Fact]
    public void EhOrcamento_TipoOrcamento_RetornaTrue()
    {
        var os = new OrdemServico { Tipo = TiposDocumento.Orcamento };
        Assert.True(os.EhOrcamento);
    }

    [Fact]
    public void EhOrcamento_TipoOrdemServico_RetornaFalse()
    {
        var os = new OrdemServico { Tipo = TiposDocumento.OrdemServico };
        Assert.False(os.EhOrcamento);
    }

    [Fact]
    public void Rotulo_Orcamento_RetornaOrcamento()
    {
        var os = new OrdemServico { Tipo = TiposDocumento.Orcamento };
        Assert.Equal("Orçamento", os.Rotulo);
    }

    [Fact]
    public void Rotulo_OrdemServico_RetornaOrdemDeServico()
    {
        var os = new OrdemServico { Tipo = TiposDocumento.OrdemServico };
        Assert.Equal("Ordem de serviço", os.Rotulo);
    }

    [Fact]
    public void SituacoesPossiveis_Orcamento_RetornaSituacoesDeOrcamento()
    {
        var os = new OrdemServico { Tipo = TiposDocumento.Orcamento };
        Assert.Equal(SituacoesOrcamento.Todas, os.SituacoesPossiveis);
    }

    [Fact]
    public void SituacoesPossiveis_OrdemServico_RetornaSituacoesDeOs()
    {
        var os = new OrdemServico { Tipo = TiposDocumento.OrdemServico };
        Assert.Equal(Situacoes.Todas, os.SituacoesPossiveis);
    }

    // ===== Validade / ValidadeVencida =====

    [Fact]
    public void Validade_TruncaHoraEDataAbertura_SomaValidadeDias()
    {
        var os = new OrdemServico
        {
            DataAbertura = new DateTime(2026, 1, 10, 14, 37, 0),
            ValidadeDias = 10,
        };

        Assert.Equal(new DateTime(2026, 1, 20), os.Validade);
    }

    [Fact]
    public void ValidadeVencida_OrcamentoAbertoComValidadeNoPassado_RetornaTrue()
    {
        var os = new OrdemServico
        {
            Tipo = TiposDocumento.Orcamento,
            Situacao = SituacoesOrcamento.Aberto,
            DataAbertura = DateTime.Today.AddDays(-20),
            ValidadeDias = 10,
        };

        Assert.True(os.ValidadeVencida);
    }

    [Fact]
    public void ValidadeVencida_OrcamentoAbertoComValidadeNoFuturo_RetornaFalse()
    {
        var os = new OrdemServico
        {
            Tipo = TiposDocumento.Orcamento,
            Situacao = SituacoesOrcamento.Aberto,
            DataAbertura = DateTime.Today,
            ValidadeDias = 10,
        };

        Assert.False(os.ValidadeVencida);
    }

    [Fact]
    public void ValidadeVencida_VenceExatamenteHoje_RetornaFalse()
    {
        // Validade < DateTime.Today é estrito: o próprio dia do vencimento ainda não
        // conta como vencido.
        var os = new OrdemServico
        {
            Tipo = TiposDocumento.Orcamento,
            Situacao = SituacoesOrcamento.Aberto,
            DataAbertura = DateTime.Today.AddDays(-10),
            ValidadeDias = 10,
        };

        Assert.Equal(DateTime.Today, os.Validade);
        Assert.False(os.ValidadeVencida);
    }

    [Fact]
    public void ValidadeVencida_OrcamentoJaAprovadoComValidadeNoPassado_RetornaFalse()
    {
        var os = new OrdemServico
        {
            Tipo = TiposDocumento.Orcamento,
            Situacao = SituacoesOrcamento.Aprovado,
            DataAbertura = DateTime.Today.AddDays(-20),
            ValidadeDias = 10,
        };

        Assert.False(os.ValidadeVencida);
    }

    [Fact]
    public void ValidadeVencida_NaoEhOrcamento_RetornaFalseMesmoComValidadeNoPassado()
    {
        var os = new OrdemServico
        {
            Tipo = TiposDocumento.OrdemServico,
            DataAbertura = DateTime.Today.AddDays(-20),
            ValidadeDias = 10,
        };

        Assert.False(os.ValidadeVencida);
    }

    // ===== Subtotal / DescontoEmReais / Total =====

    [Fact]
    public void Subtotal_SemItens_RetornaZero()
    {
        var os = new OrdemServico();
        Assert.Equal(0m, os.Subtotal);
    }

    [Fact]
    public void Subtotal_ComItens_SomaTotalDeCadaItem()
    {
        var os = new OrdemServico
        {
            Itens = new List<ItemOrdemServico>
            {
                new() { Quantidade = 2, ValorUnitario = 50m },
                new() { Quantidade = 1, ValorUnitario = 30m },
            },
        };

        Assert.Equal(130m, os.Subtotal);
    }

    [Fact]
    public void DescontoEmReais_TipoValorMenorQueSubtotal_AplicaLiteral()
    {
        var os = new OrdemServico
        {
            DescontoTipo = TiposDesconto.Valor,
            Desconto = 30m,
            Itens = new List<ItemOrdemServico> { new() { Quantidade = 1, ValorUnitario = 100m } },
        };

        Assert.Equal(30m, os.DescontoEmReais);
    }

    [Fact]
    public void DescontoEmReais_TipoValorMaiorQueSubtotal_LimitaAoSubtotal()
    {
        var os = new OrdemServico
        {
            DescontoTipo = TiposDesconto.Valor,
            Desconto = 500m,
            Itens = new List<ItemOrdemServico> { new() { Quantidade = 1, ValorUnitario = 100m } },
        };

        Assert.Equal(100m, os.DescontoEmReais);
    }

    [Fact]
    public void DescontoEmReais_TipoPercentual_CalculaSobreOSubtotal()
    {
        var os = new OrdemServico
        {
            DescontoTipo = TiposDesconto.Percentual,
            Desconto = 50m,
            Itens = new List<ItemOrdemServico> { new() { Quantidade = 1, ValorUnitario = 200m } },
        };

        Assert.Equal(100m, os.DescontoEmReais);
    }

    [Fact]
    public void DescontoEmReais_PercentualAcimaDeCem_LimitaEmCemPorCento()
    {
        var os = new OrdemServico
        {
            DescontoTipo = TiposDesconto.Percentual,
            Desconto = 150m,
            Itens = new List<ItemOrdemServico> { new() { Quantidade = 1, ValorUnitario = 200m } },
        };

        Assert.Equal(200m, os.DescontoEmReais);
    }

    [Fact]
    public void Total_ComDescontoPercentual_AbateSubtotal()
    {
        var os = new OrdemServico
        {
            DescontoTipo = TiposDesconto.Percentual,
            Desconto = 10m,
            Itens = new List<ItemOrdemServico> { new() { Quantidade = 1, ValorUnitario = 200m } },
        };

        Assert.Equal(180m, os.Total);
    }

    [Fact]
    public void Total_ComDescontoEmValorMaiorQueSubtotal_NaoFicaNegativo()
    {
        var os = new OrdemServico
        {
            DescontoTipo = TiposDesconto.Valor,
            Desconto = 500m,
            Itens = new List<ItemOrdemServico> { new() { Quantidade = 1, ValorUnitario = 100m } },
        };

        Assert.Equal(0m, os.Total);
    }

    // ===== SaldoAPagar =====

    [Fact]
    public void SaldoAPagar_SinalMenorQueTotal_RetornaDiferenca()
    {
        var os = new OrdemServico
        {
            Sinal = 50m,
            Itens = new List<ItemOrdemServico> { new() { Quantidade = 1, ValorUnitario = 200m } },
        };

        Assert.Equal(150m, os.SaldoAPagar);
    }

    [Fact]
    public void SaldoAPagar_SinalIgualAoTotal_RetornaZero()
    {
        var os = new OrdemServico
        {
            Sinal = 200m,
            Itens = new List<ItemOrdemServico> { new() { Quantidade = 1, ValorUnitario = 200m } },
        };

        Assert.Equal(0m, os.SaldoAPagar);
    }

    [Fact]
    public void SaldoAPagar_SinalMaiorQueTotal_NuncaFicaNegativo()
    {
        var os = new OrdemServico
        {
            Sinal = 300m,
            Itens = new List<ItemOrdemServico> { new() { Quantidade = 1, ValorUnitario = 200m } },
        };

        Assert.Equal(0m, os.SaldoAPagar);
    }

    // ===== QuantidadeParcelas / ValorParcela =====

    [Fact]
    public void QuantidadeParcelas_NaoParcelado_RetornaUmIndependenteDeParcelas()
    {
        var os = new OrdemServico { Parcelado = false, Parcelas = 5 };
        Assert.Equal(1, os.QuantidadeParcelas);
    }

    [Fact]
    public void QuantidadeParcelas_ParceladoComUmaParcela_RetornaUm()
    {
        var os = new OrdemServico { Parcelado = true, Parcelas = 1 };
        Assert.Equal(1, os.QuantidadeParcelas);
    }

    [Fact]
    public void QuantidadeParcelas_ParceladoComVariasParcelas_RetornaOValor()
    {
        var os = new OrdemServico { Parcelado = true, Parcelas = 3 };
        Assert.Equal(3, os.QuantidadeParcelas);
    }

    [Fact]
    public void QuantidadeParcelas_ParceladoComParcelasZero_RetornaUm()
    {
        var os = new OrdemServico { Parcelado = true, Parcelas = 0 };
        Assert.Equal(1, os.QuantidadeParcelas);
    }

    [Fact]
    public void ValorParcela_DivisaoExata_RetornaValorExato()
    {
        var os = new OrdemServico
        {
            Parcelado = true,
            Parcelas = 3,
            Itens = new List<ItemOrdemServico> { new() { Quantidade = 1, ValorUnitario = 300m } },
        };

        Assert.Equal(100m, os.ValorParcela);
    }

    [Fact]
    public void ValorParcela_DivisaoComDizima_ArredondaParaDuasCasas()
    {
        var os = new OrdemServico
        {
            Parcelado = true,
            Parcelas = 3,
            Itens = new List<ItemOrdemServico> { new() { Quantidade = 1, ValorUnitario = 100m } },
        };

        Assert.Equal(33.33m, os.ValorParcela);
    }

    [Fact]
    public void ValorParcela_MeioACaminhoNaTerceiraCasa_ArredondaParaCimaAwayFromZero()
    {
        // 20,05 / 2 = 10,025 — MidpointRounding.AwayFromZero arredonda pra 10,03,
        // diferente do padrão ToEven (bancário), que daria 10,02.
        var os = new OrdemServico
        {
            Parcelado = true,
            Parcelas = 2,
            Itens = new List<ItemOrdemServico> { new() { Quantidade = 1, ValorUnitario = 20.05m } },
        };

        Assert.Equal(10.03m, os.ValorParcela);
    }

    // ===== Garantia =====

    [Fact]
    public void GarantiaInicio_SemDataEntrega_RetornaNull()
    {
        var os = new OrdemServico { DataEntrega = null };
        Assert.Null(os.GarantiaInicio);
    }

    [Fact]
    public void GarantiaFim_SemDataEntrega_RetornaNull()
    {
        var os = new OrdemServico { DataEntrega = null };
        Assert.Null(os.GarantiaFim);
    }

    [Fact]
    public void GarantiaFim_ComDataEntrega_TruncaHoraESomaPrazo()
    {
        var os = new OrdemServico
        {
            DataEntrega = new DateTime(2026, 1, 1, 18, 45, 0),
            PrazoGarantiaDias = 90,
        };

        Assert.Equal(new DateTime(2026, 1, 1).AddDays(90), os.GarantiaFim);
    }

    [Fact]
    public void GarantiaIniciada_SemDataEntrega_RetornaFalse()
    {
        var os = new OrdemServico { DataEntrega = null };
        Assert.False(os.GarantiaIniciada);
    }

    [Fact]
    public void GarantiaIniciada_ComDataEntrega_RetornaTrue()
    {
        var os = new OrdemServico { DataEntrega = DateTime.Today };
        Assert.True(os.GarantiaIniciada);
    }

    [Fact]
    public void GarantiaVigente_DentroDoPrazo_RetornaTrue()
    {
        var os = new OrdemServico { DataEntrega = DateTime.Today.AddDays(-10), PrazoGarantiaDias = 90 };
        Assert.True(os.GarantiaVigente);
    }

    [Fact]
    public void GarantiaVigente_ForaDoPrazo_RetornaFalse()
    {
        var os = new OrdemServico { DataEntrega = DateTime.Today.AddDays(-100), PrazoGarantiaDias = 90 };
        Assert.False(os.GarantiaVigente);
    }

    [Fact]
    public void GarantiaVigente_NoUltimoDiaDoPrazo_AindaEhVigente()
    {
        // GarantiaFim = DataEntrega.Date + PrazoGarantiaDias; a comparação usa >=, então
        // o próprio dia do vencimento ainda conta como vigente — se alguém trocar esse
        // operador para > por engano, este teste falha.
        var prazoDias = 90;
        var os = new OrdemServico { DataEntrega = DateTime.Today.AddDays(-prazoDias), PrazoGarantiaDias = prazoDias };

        Assert.Equal(DateTime.Today, os.GarantiaFim);
        Assert.True(os.GarantiaVigente);
    }

    [Fact]
    public void GarantiaVigente_UmDiaDepoisDoPrazo_JaEstaVencida()
    {
        var prazoDias = 90;
        var os = new OrdemServico { DataEntrega = DateTime.Today.AddDays(-prazoDias - 1), PrazoGarantiaDias = prazoDias };

        Assert.False(os.GarantiaVigente);
    }

    [Fact]
    public void GarantiaVigente_SemDataEntrega_RetornaFalse()
    {
        var os = new OrdemServico { DataEntrega = null };
        Assert.False(os.GarantiaVigente);
    }

    [Fact]
    public void DiasDeGarantiaRestantes_SemDataEntrega_RetornaNull()
    {
        var os = new OrdemServico { DataEntrega = null };
        Assert.Null(os.DiasDeGarantiaRestantes);
    }

    [Fact]
    public void DiasDeGarantiaRestantes_FaltandoDias_RetornaPositivo()
    {
        var os = new OrdemServico { DataEntrega = DateTime.Today.AddDays(-85), PrazoGarantiaDias = 90 };
        Assert.Equal(5, os.DiasDeGarantiaRestantes);
    }

    [Fact]
    public void DiasDeGarantiaRestantes_JaVencida_RetornaNegativo()
    {
        var os = new OrdemServico { DataEntrega = DateTime.Today.AddDays(-95), PrazoGarantiaDias = 90 };
        Assert.Equal(-5, os.DiasDeGarantiaRestantes);
    }

    [Fact]
    public void DiasDeGarantiaRestantes_VenceHoje_RetornaZero()
    {
        var os = new OrdemServico { DataEntrega = DateTime.Today.AddDays(-90), PrazoGarantiaDias = 90 };
        Assert.Equal(0, os.DiasDeGarantiaRestantes);
    }

    [Fact]
    public void SituacaoGarantia_SemDataEntrega_RetornaNaoIniciada()
    {
        var os = new OrdemServico { DataEntrega = null };
        Assert.Equal("Não iniciada", os.SituacaoGarantia);
    }

    [Fact]
    public void SituacaoGarantia_DentroDoPrazoInclusiveNoUltimoDia_RetornaVigente()
    {
        var os = new OrdemServico { DataEntrega = DateTime.Today.AddDays(-90), PrazoGarantiaDias = 90 };
        Assert.Equal("Vigente", os.SituacaoGarantia);
    }

    [Fact]
    public void SituacaoGarantia_ForaDoPrazo_RetornaVencida()
    {
        var os = new OrdemServico { DataEntrega = DateTime.Today.AddDays(-100), PrazoGarantiaDias = 90 };
        Assert.Equal("Vencida", os.SituacaoGarantia);
    }

    // ===== AparelhosResumo =====

    [Fact]
    public void AparelhosResumo_SemAparelhos_RetornaTraco()
    {
        var os = new OrdemServico();
        Assert.Equal("—", os.AparelhosResumo);
    }

    [Fact]
    public void AparelhosResumo_UmAparelho_RetornaSeuResumoSemSufixo()
    {
        var os = new OrdemServico
        {
            Aparelhos = new List<AparelhoOs> { new() { Marca = "Samsung", Modelo = "A15" } },
        };

        Assert.Equal("Samsung A15", os.AparelhosResumo);
    }

    [Fact]
    public void AparelhosResumo_VariosAparelhos_AdicionaSufixoComQuantidadeRestante()
    {
        var os = new OrdemServico
        {
            Aparelhos = new List<AparelhoOs>
            {
                new() { Marca = "Samsung", Modelo = "A15" },
                new() { Marca = "Apple", Modelo = "iPhone 12" },
                new() { Marca = "Motorola", Modelo = "Edge 40" },
            },
        };

        Assert.Equal("Samsung A15 +2", os.AparelhosResumo);
    }

    // ===== ItemOrdemServico.Total =====

    [Fact]
    public void ItemOrdemServico_Total_MultiplicaQuantidadePorValorUnitario()
    {
        var item = new ItemOrdemServico { Quantidade = 3, ValorUnitario = 50m };
        Assert.Equal(150m, item.Total);
    }

    [Fact]
    public void ItemOrdemServico_Total_QuantidadeZero_RetornaZero()
    {
        var item = new ItemOrdemServico { Quantidade = 0, ValorUnitario = 50m };
        Assert.Equal(0m, item.Total);
    }
}
