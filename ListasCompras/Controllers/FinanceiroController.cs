using ListasCompras.Data;
using ListasCompras.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ListasCompras.Controllers;

// Entradas/saídas que não são venda nem OS (aluguel, retirada, material de uso) e compromissos
// com vencimento antes de virarem saída de fato. Vendas e OS já entram pelo Faturamento MEI e
// pelo Fechamento de caixa — lançar de novo aqui duplicaria.
public class FinanceiroController : LojaControllerBase
{
    public FinanceiroController(AppDbContext context) : base(context) { }

    public IActionResult Index(int? mes, int? ano)
    {
        var hoje = DateTime.Today;
        var mesConsultado = mes ?? hoje.Month;
        var anoConsultado = ano ?? hoje.Year;
        var inicio = new DateTime(anoConsultado, mesConsultado, 1);
        var fim = inicio.AddMonths(1);

        var lancamentos = Context.LancamentosFinanceiros
            .Include(l => l.Usuario)
            .Where(l => l.Data >= inicio && l.Data < fim)
            .OrderByDescending(l => l.Data)
            .ToList();

        var contas = Context.ContasAPagar
            .OrderBy(c => c.Vencimento)
            .ToList();

        ViewData["Mes"] = mesConsultado;
        ViewData["Ano"] = anoConsultado;
        ViewData["Entradas"] = lancamentos.Where(l => l.Tipo == TiposLancamento.Entrada).Sum(l => l.Valor);
        ViewData["Saidas"] = lancamentos.Where(l => l.Tipo == TiposLancamento.Saida).Sum(l => l.Valor);
        ViewData["Contas"] = contas;

        return View(lancamentos);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SalvarLancamento(string tipo, string descricao, string? categoria, string? valor, DateTime? data)
    {
        if (string.IsNullOrWhiteSpace(descricao))
        {
            TempData["Erro"] = "Informe a descrição do lançamento.";
            return RedirectToAction(nameof(Index));
        }

        Context.LancamentosFinanceiros.Add(new LancamentoFinanceiro
        {
            Tipo = tipo == TiposLancamento.Entrada ? TiposLancamento.Entrada : TiposLancamento.Saida,
            Descricao = descricao.Trim(),
            Categoria = Limpar(categoria),
            Valor = ParaDecimal(valor),
            Data = data ?? DateTime.Now,
            UsuarioId = IdDoUsuarioLogado(),
        });
        Context.SaveChanges();

        TempData["Sucesso"] = "Lançamento registrado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ExcluirLancamento(int id)
    {
        var lancamento = Context.LancamentosFinanceiros.Find(id);
        if (lancamento != null)
        {
            Context.LancamentosFinanceiros.Remove(lancamento);
            Context.SaveChanges();
            TempData["Sucesso"] = "Lançamento excluído.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SalvarConta(string descricao, string? valor, DateTime vencimento)
    {
        if (string.IsNullOrWhiteSpace(descricao))
        {
            TempData["Erro"] = "Informe a descrição da conta.";
            return RedirectToAction(nameof(Index));
        }

        Context.ContasAPagar.Add(new ContaAPagar
        {
            Descricao = descricao.Trim(),
            Valor = ParaDecimal(valor),
            Vencimento = vencimento.Date,
        });
        Context.SaveChanges();

        TempData["Sucesso"] = "Conta a pagar cadastrada.";
        return RedirectToAction(nameof(Index));
    }

    // Marcar como paga não é só um flag: gera o lançamento de saída correspondente,
    // senão a conta paga desapareceria do fluxo de caixa do mês
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult MarcarContaPaga(int id)
    {
        var conta = Context.ContasAPagar.Find(id);
        if (conta == null || conta.Paga) return RedirectToAction(nameof(Index));

        var lancamento = new LancamentoFinanceiro
        {
            Tipo = TiposLancamento.Saida,
            Descricao = conta.Descricao,
            Categoria = "Conta a pagar",
            Valor = conta.Valor,
            Data = DateTime.Now,
            UsuarioId = IdDoUsuarioLogado(),
        };
        Context.LancamentosFinanceiros.Add(lancamento);
        Context.SaveChanges();

        conta.Paga = true;
        conta.DataPagamento = DateTime.Now;
        conta.LancamentoId = lancamento.Id;
        Context.SaveChanges();

        TempData["Sucesso"] = $"Conta \"{conta.Descricao}\" marcada como paga.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ExcluirConta(int id)
    {
        var conta = Context.ContasAPagar.Find(id);
        // Conta já paga tem lançamento associado; excluir aqui só remove o compromisso
        // futuro, não o histórico do que já saiu do caixa
        if (conta != null && !conta.Paga)
        {
            Context.ContasAPagar.Remove(conta);
            Context.SaveChanges();
            TempData["Sucesso"] = "Conta excluída.";
        }
        return RedirectToAction(nameof(Index));
    }

    private static string? Limpar(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
