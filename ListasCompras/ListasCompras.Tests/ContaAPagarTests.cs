using ListasCompras.Models;

namespace ListasCompras.Tests;

public class ContaAPagarTests
{
    [Fact]
    public void Vencida_NaoPagaEVencimentoNoPassado_RetornaTrue()
    {
        var conta = new ContaAPagar { Paga = false, Vencimento = DateTime.Today.AddDays(-1) };
        Assert.True(conta.Vencida);
    }

    [Fact]
    public void Vencida_NaoPagaEVencimentoNoFuturo_RetornaFalse()
    {
        var conta = new ContaAPagar { Paga = false, Vencimento = DateTime.Today.AddDays(1) };
        Assert.False(conta.Vencida);
    }

    [Fact]
    public void Vencida_JaPagaMesmoComVencimentoNoPassado_RetornaFalse()
    {
        var conta = new ContaAPagar { Paga = true, Vencimento = DateTime.Today.AddDays(-10) };
        Assert.False(conta.Vencida);
    }

    [Fact]
    public void Vencida_VencimentoExatamenteHoje_NaoPaga_RetornaFalse()
    {
        // Vencimento.Date < DateTime.Today é estrito: o próprio dia do vencimento ainda
        // não é considerado "vencida" — assimetria proposital de registrar, já que
        // OrdemServico.GarantiaVigente usa >= para o mesmo tipo de borda (lá o dia do
        // vencimento AINDA conta como vigente; aqui o dia do vencimento AINDA NÃO conta
        // como vencida — são conceitos diferentes, não um bug de inconsistência).
        var conta = new ContaAPagar { Paga = false, Vencimento = DateTime.Today };
        Assert.False(conta.Vencida);
    }
}
