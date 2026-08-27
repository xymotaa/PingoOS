using ListasCompras.Models;

namespace ListasCompras.Tests;

public class ClienteTests
{
    [Fact]
    public void EnderecoCompleto_TodosOsCamposPreenchidos_MontaFormatoCompleto()
    {
        var cliente = new Cliente
        {
            Endereco = "Rua das Flores",
            Numero = "123",
            Bairro = "Centro",
            Cidade = "Marabá",
            Uf = "PA",
            Cep = "68500-000",
        };

        Assert.Equal("Rua das Flores, 123 - Centro - Marabá/PA - CEP 68500-000", cliente.EnderecoCompleto);
    }

    [Fact]
    public void EnderecoCompleto_SoCidadePreenchida_RetornaSoACidade()
    {
        var cliente = new Cliente { Cidade = "Marabá" };
        Assert.Equal("Marabá", cliente.EnderecoCompleto);
    }

    [Fact]
    public void EnderecoCompleto_SoUfPreenchidaSemCidade_RetornaAUfSozinha()
    {
        var cliente = new Cliente { Uf = "PA" };
        Assert.Equal("PA", cliente.EnderecoCompleto);
    }

    [Fact]
    public void EnderecoCompleto_EnderecoSemNumero_NaoDeixaVirgulaSobrando()
    {
        var cliente = new Cliente { Endereco = "Rua das Flores" };
        Assert.Equal("Rua das Flores", cliente.EnderecoCompleto);
    }

    [Fact]
    public void EnderecoCompleto_NumeroSemEndereco_UsaSoONumero()
    {
        var cliente = new Cliente { Numero = "123" };
        Assert.Equal("123", cliente.EnderecoCompleto);
    }

    [Fact]
    public void EnderecoCompleto_TodosOsCamposNulos_RetornaStringVazia()
    {
        var cliente = new Cliente();
        Assert.Equal("", cliente.EnderecoCompleto);
    }

    [Fact]
    public void EnderecoCompleto_CamposComEspacoEmBranco_TratadosComoAusentes()
    {
        var cliente = new Cliente { Endereco = "   ", Numero = "  ", Bairro = "", Cidade = " ", Uf = null, Cep = "  " };
        Assert.Equal("", cliente.EnderecoCompleto);
    }

    [Fact]
    public void EnderecoCompleto_CidadeEUfPreenchidas_CombinaComBarra()
    {
        var cliente = new Cliente { Cidade = "Marabá", Uf = "PA" };
        Assert.Equal("Marabá/PA", cliente.EnderecoCompleto);
    }
}
