using ListasCompras.Models;

namespace ListasCompras.Tests;

public class AparelhoOsTests
{
    [Fact]
    public void Resumo_MarcaEModelo_JuntaOsDois()
    {
        var aparelho = new AparelhoOs { Marca = "Samsung", Modelo = "Galaxy A15" };
        Assert.Equal("Samsung Galaxy A15", aparelho.Resumo);
    }

    [Fact]
    public void Resumo_SoMarca_RetornaSoAMarca()
    {
        var aparelho = new AparelhoOs { Marca = "Samsung" };
        Assert.Equal("Samsung", aparelho.Resumo);
    }

    [Fact]
    public void Resumo_SoModelo_RetornaSoOModelo()
    {
        var aparelho = new AparelhoOs { Modelo = "Galaxy A15" };
        Assert.Equal("Galaxy A15", aparelho.Resumo);
    }

    [Fact]
    public void Resumo_SemMarcaNemModeloComTipoPreenchido_UsaOTipo()
    {
        var aparelho = new AparelhoOs { Tipo = "Notebook" };
        Assert.Equal("Notebook", aparelho.Resumo);
    }

    [Fact]
    public void Resumo_NadaPreenchido_RetornaTraco()
    {
        var aparelho = new AparelhoOs();
        Assert.Equal("—", aparelho.Resumo);
    }
}
