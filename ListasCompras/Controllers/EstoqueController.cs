using System.Globalization;
using System.Security.Claims;
using ListasCompras.Data;
using ListasCompras.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ListasCompras.Controllers;

public class EstoqueController : LojaControllerBase
{
    public EstoqueController(AppDbContext context) : base(context) { }

    public IActionResult Index()
    {
        return View(Context.ProdutosEstoque.OrderBy(p => p.Nome).ToList());
    }

    public IActionResult Add(int? id)
    {
        var produto = id.HasValue ? Context.ProdutosEstoque.Find(id.Value) : null;
        if (id.HasValue && produto == null) return NotFound();
        return View(produto ?? new ProdutoEstoque());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Salvar(
        int id, string nome, string? codigo, string? categoria, string? unidade,
        string? preco, string? custo, int estoqueInicial, int estoqueMinimo)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            TempData["Erro"] = "Informe o nome do produto.";
            return RedirectToAction(nameof(Add), new { id = id > 0 ? id : (int?)null });
        }

        var produto = id > 0 ? Context.ProdutosEstoque.Find(id) : null;
        if (id > 0 && produto == null) return NotFound();

        var novo = produto == null;
        produto ??= new ProdutoEstoque();

        var codigoFinal = string.IsNullOrWhiteSpace(codigo)
            ? (novo ? EstoqueServico.ProximoCodigo(Context) : produto.Codigo)
            : codigo.Trim();

        if (Context.ProdutosEstoque.Any(p => p.Codigo == codigoFinal && p.Id != produto.Id))
        {
            TempData["Erro"] = $"Já existe um produto com o código \"{codigoFinal}\".";
            return RedirectToAction(nameof(Add), new { id = id > 0 ? id : (int?)null });
        }

        produto.Codigo = codigoFinal;
        produto.Nome = nome.Trim();
        produto.Categoria = Limpar(categoria);
        produto.Unidade = Limpar(unidade);
        produto.CustoUnitario = ParaDecimal(custo);
        produto.PrecoVenda = ParaDecimal(preco);
        produto.EstoqueMinimo = Math.Max(0, estoqueMinimo);

        if (novo)
        {
            Context.ProdutosEstoque.Add(produto);
            // O saldo inicial entra como movimentação, senão nasceria sem histórico
            if (estoqueInicial > 0)
                EstoqueServico.Movimentar(produto, TiposMovimentacao.Entrada, estoqueInicial,
                    "Saldo inicial do cadastro", IdDoUsuarioLogado());
        }

        Context.SaveChanges();
        TempData["Sucesso"] = novo ? $"Produto {produto.Nome} cadastrado." : $"Produto {produto.Nome} atualizado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Movimentar(int produtoId, string tipo, int quantidade, string? motivo)
    {
        var produto = Context.ProdutosEstoque.Find(produtoId);
        if (produto == null)
        {
            TempData["Erro"] = "Produto não encontrado.";
            return RedirectToAction(nameof(Index));
        }

        tipo = tipo == TiposMovimentacao.Saida ? TiposMovimentacao.Saida : TiposMovimentacao.Entrada;
        EstoqueServico.Movimentar(produto, tipo, quantidade, motivo, IdDoUsuarioLogado());
        Context.SaveChanges();

        var rotulo = tipo == TiposMovimentacao.Saida ? "Saída" : "Entrada";
        TempData["Sucesso"] = $"{rotulo} de {quantidade} registrada. {produto.Nome} agora tem {produto.SaldoAtual}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Excluir(int id)
    {
        var produto = Context.ProdutosEstoque.Find(id);
        if (produto == null) return RedirectToAction(nameof(Index));

        Context.ProdutosEstoque.Remove(produto);
        Context.SaveChanges();
        TempData["Sucesso"] = $"Produto {produto.Nome} excluído. As vendas antigas continuam no histórico.";
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Historico(int id)
    {
        var produto = Context.ProdutosEstoque
            .Include(p => p.Movimentacoes).ThenInclude(m => m.Usuario)
            .FirstOrDefault(p => p.Id == id);

        if (produto == null) return NotFound();
        produto.Movimentacoes = produto.Movimentacoes.OrderByDescending(m => m.Id).ToList();
        return View(produto);
    }

    // Busca usada pelo Caixa
    [HttpGet]
    public IActionResult Buscar(string? termo)
    {
        var consulta = Context.ProdutosEstoque.AsQueryable();

        if (!string.IsNullOrWhiteSpace(termo))
        {
            var t = $"%{termo.Trim()}%";
            consulta = consulta.Where(p =>
                EF.Functions.Like(p.Nome, t) || EF.Functions.Like(p.Codigo, t));
        }

        var resultado = consulta.OrderBy(p => p.Nome).Take(20).ToList()
            .Select(p => new
            {
                id = p.Id,
                codigo = p.Codigo,
                nome = p.Nome,
                precoVenda = p.PrecoVenda,
                saldoAtual = p.SaldoAtual,
            });

        return Json(resultado);
    }

    private int? IdDoUsuarioLogado()
        => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    // Cultura invariante de propósito: o formulário manda ponto decimal
    private static decimal ParaDecimal(string? valor)
        => decimal.TryParse(valor, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ? d : 0m;

    private static string? Limpar(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
}
