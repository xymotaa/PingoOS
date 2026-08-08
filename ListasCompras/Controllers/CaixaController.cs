using System.Globalization;
using System.Security.Claims;
using ListasCompras.Data;
using ListasCompras.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ListasCompras.Controllers;

public class CaixaController : LojaControllerBase
{
    public CaixaController(AppDbContext context) : base(context) { }

    public IActionResult Index()
    {
        return View(Context.ProdutosEstoque.OrderBy(p => p.Nome).ToList());
    }

    public IActionResult Vendas()
    {
        var vendas = Context.Vendas
            .Include(v => v.Itens)
            .Include(v => v.Usuario)
            .OrderByDescending(v => v.Id)
            .ToList();

        return View(vendas);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Finalizar(
        string formaPagamento, string? valorRecebido,
        int[]? itemProdutoId, int[]? itemQuantidade, string[]? itemPreco, string[]? itemDesconto)
    {
        if (itemProdutoId == null || itemProdutoId.Length == 0)
        {
            TempData["Erro"] = "Adicione ao menos um produto antes de finalizar.";
            return RedirectToAction(nameof(Index));
        }

        var venda = new Venda
        {
            Numero = EstoqueServico.ProximoNumeroVenda(Context),
            FormaPagamento = FormasPagamento.Todas.Contains(formaPagamento) ? formaPagamento : FormasPagamento.Dinheiro,
            ValorRecebido = ParaDecimal(valorRecebido),
            UsuarioId = IdDoUsuarioLogado(),
        };

        var semSaldo = new List<string>();

        for (var i = 0; i < itemProdutoId.Length; i++)
        {
            var produto = Context.ProdutosEstoque.Find(itemProdutoId[i]);
            if (produto == null) continue;

            var quantidade = itemQuantidade != null && i < itemQuantidade.Length && itemQuantidade[i] > 0
                ? itemQuantidade[i] : 1;

            venda.Itens.Add(new ItemVenda
            {
                ProdutoEstoque = produto,
                Codigo = produto.Codigo,
                Descricao = produto.Nome,
                Quantidade = quantidade,
                PrecoUnitario = itemPreco != null && i < itemPreco.Length ? ParaDecimal(itemPreco[i]) : produto.PrecoVenda,
                DescontoPercentual = itemDesconto != null && i < itemDesconto.Length ? ParaDecimal(itemDesconto[i]) : 0m,
            });

            if (produto.SaldoAtual < quantidade) semSaldo.Add(produto.Nome);

            // A venda baixa o estoque: é o que liga os dois módulos
            EstoqueServico.Movimentar(produto, TiposMovimentacao.Saida, quantidade,
                $"Venda {venda.Numero}", venda.UsuarioId, venda);
        }

        if (venda.Itens.Count == 0)
        {
            TempData["Erro"] = "Nenhum dos produtos da venda foi encontrado no estoque.";
            return RedirectToAction(nameof(Index));
        }

        Context.Vendas.Add(venda);
        Context.SaveChanges();

        TempData["Sucesso"] = $"Venda {venda.Numero} registrada — {venda.QuantidadeItens} " +
            (venda.QuantidadeItens == 1 ? "item" : "itens") + $", {Moeda(venda.Total)}.";

        // Vendemos o que não tinha na prateleira: avisa, mas a venda está feita
        if (semSaldo.Count > 0)
            TempData["Aviso"] = "Saldo ficou negativo em: " + string.Join(", ", semSaldo.Distinct()) +
                ". Confira a contagem do estoque.";

        return RedirectToAction(nameof(Index));
    }

    private int? IdDoUsuarioLogado()
        => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    private static decimal ParaDecimal(string? valor)
        => decimal.TryParse(valor, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ? d : 0m;

    private static string Moeda(decimal v) => v.ToString("C2", new CultureInfo("pt-BR"));
}
