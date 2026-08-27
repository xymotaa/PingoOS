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
        return View(Context.ProdutosEstoque.Include(p => p.Categoria).OrderBy(p => p.Nome).ToList());
    }

    public IActionResult Add(int? id)
    {
        var produto = id.HasValue
            ? Context.ProdutosEstoque.Include(p => p.ModelosCompativeis).FirstOrDefault(p => p.Id == id.Value)
            : null;
        if (id.HasValue && produto == null) return NotFound();

        ViewBag.Categorias = Context.Categorias.OrderBy(c => c.Nome).ToList();
        ViewBag.MarcasCelular = Context.MarcasCelular.Include(m => m.Modelos).OrderBy(m => m.Nome).ToList();
        ViewBag.ModelosCompativeisIds = produto?.ModelosCompativeis.Select(v => v.ModeloCelularId).ToList()
            ?? new List<int>();

        return View(produto ?? new ProdutoEstoque());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Salvar(
        int id, string nome, string? codigo, int? categoriaId, string? unidade,
        string? preco, string? custo, int estoqueInicial, int estoqueMinimo, int estoqueMaximo,
        int[]? modelosCelularIds, IFormFile? foto, bool removerFoto = false,
        string? formato = TiposFormatoProduto.Simples, string? tipo = TiposProduto.Produto,
        string? condicao = CondicoesProduto.NaoEspecificado, string? descricao = null,
        string? marca = null, string? modeloRef = null, string? gtin = null,
        string? peso = null, string? largura = null, string? altura = null, string? profundidade = null,
        string? localizacao = null, int origemFiscal = 0, string? ncm = null, string? cest = null, string? cfop = null)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            TempData["Erro"] = "Informe o nome do produto.";
            return RedirectToAction(nameof(Add), new { id = id > 0 ? id : (int?)null });
        }

        estoqueMinimo = Math.Max(0, estoqueMinimo);
        estoqueMaximo = Math.Max(0, estoqueMaximo);
        if (estoqueMaximo > 0 && estoqueMaximo < estoqueMinimo)
        {
            TempData["Erro"] = "O estoque máximo não pode ser menor que o estoque mínimo.";
            return RedirectToAction(nameof(Add), new { id = id > 0 ? id : (int?)null });
        }

        if (foto != null && foto.Length > 0 &&
            (!ProdutoImagemServico.TipoValido(foto.ContentType) || !ProdutoImagemServico.TamanhoValido(foto.Length)))
        {
            TempData["Erro"] = "Envie uma imagem em JPEG, PNG ou WEBP de até 8 MB.";
            return RedirectToAction(nameof(Add), new { id = id > 0 ? id : (int?)null });
        }

        // "Com variação" ainda não tem a etapa de cadastro implementada — bloqueia aqui
        // pra não deixar o produto num estado que a tela não sabe editar depois.
        if (formato == TiposFormatoProduto.ComVariacao)
        {
            TempData["Erro"] = "Produtos com variação ainda não são suportados. Cadastre como \"Simples\" por enquanto.";
            return RedirectToAction(nameof(Add), new { id = id > 0 ? id : (int?)null });
        }

        var produto = id > 0
            ? Context.ProdutosEstoque.Include(p => p.ModelosCompativeis).FirstOrDefault(p => p.Id == id)
            : null;
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
        produto.CategoriaId = categoriaId > 0 ? categoriaId : null;
        produto.Unidade = Limpar(unidade);
        produto.CustoUnitario = ParaDecimal(custo);
        produto.PrecoVenda = ParaDecimal(preco);
        produto.EstoqueMinimo = estoqueMinimo;
        produto.EstoqueMaximo = estoqueMaximo;

        produto.Formato = formato == TiposFormatoProduto.ComVariacao ? TiposFormatoProduto.ComVariacao : TiposFormatoProduto.Simples;
        produto.Tipo = tipo == TiposProduto.Servico ? TiposProduto.Servico : TiposProduto.Produto;
        produto.Condicao = condicao is CondicoesProduto.Novo or CondicoesProduto.Usado ? condicao : CondicoesProduto.NaoEspecificado;
        produto.Descricao = Limpar(descricao);
        produto.Marca = Limpar(marca);
        produto.ModeloRef = Limpar(modeloRef);
        produto.Gtin = Limpar(gtin);
        produto.Peso = ParaDecimalNullable(peso);
        produto.Largura = ParaDecimalNullable(largura);
        produto.Altura = ParaDecimalNullable(altura);
        produto.Profundidade = ParaDecimalNullable(profundidade);
        produto.Localizacao = Limpar(localizacao);
        produto.OrigemFiscal = origemFiscal is 1 or 2 ? origemFiscal : 0;
        produto.Ncm = Limpar(ncm);
        produto.Cest = Limpar(cest);
        produto.Cfop = Limpar(cfop);

        if (novo)
        {
            Context.ProdutosEstoque.Add(produto);
            // O saldo inicial entra como movimentação, senão nasceria sem histórico
            if (estoqueInicial > 0)
                EstoqueServico.Movimentar(produto, TiposMovimentacao.Entrada, estoqueInicial,
                    "Saldo inicial do cadastro", IdDoUsuarioLogado());
        }
        else
        {
            // Editar o estoque aqui não pula a auditoria: a diferença vira uma
            // movimentação normal, igual a ajustar pela tela de Movimentação — só
            // evita o usuário ter que abrir outra tela pra corrigir um número.
            var diferenca = Math.Max(0, estoqueInicial) - produto.SaldoAtual;
            if (diferenca > 0)
                EstoqueServico.Movimentar(produto, TiposMovimentacao.Entrada, diferenca,
                    "Ajuste via cadastro", IdDoUsuarioLogado());
            else if (diferenca < 0)
                EstoqueServico.Movimentar(produto, TiposMovimentacao.Saida, -diferenca,
                    "Ajuste via cadastro", IdDoUsuarioLogado());
        }

        SincronizarModelosCompativeis(produto, modelosCelularIds);

        if (foto != null && foto.Length > 0)
        {
            // Salva o arquivo novo antes de apagar o antigo — se SalvarAsync falhar
            // (disco cheio etc.), o produto não fica sem imagem nenhuma.
            var imagemAntiga = produto.Imagem;
            await using var stream = foto.OpenReadStream();
            produto.Imagem = await ProdutoImagemServico.SalvarAsync(stream, foto.ContentType!);
            if (imagemAntiga != null) ProdutoImagemServico.Remover(imagemAntiga);
        }
        else if (removerFoto && produto.Imagem != null)
        {
            ProdutoImagemServico.Remover(produto.Imagem);
            produto.Imagem = null;
        }

        Context.SaveChanges();
        TempData["Sucesso"] = novo ? $"Produto {produto.Nome} cadastrado." : $"Produto {produto.Nome} atualizado.";
        return RedirectToAction(nameof(Index));
    }

    // Substitui a lista inteira pela que veio do formulário — mais simples que diff
    // incremental, e o volume (poucos modelos por produto) não justifica otimizar isso.
    private void SincronizarModelosCompativeis(ProdutoEstoque produto, int[]? modelosCelularIds)
    {
        if (produto.Id > 0)
        {
            Context.ProdutoEstoqueModeloCompativeis.RemoveRange(produto.ModelosCompativeis);
            produto.ModelosCompativeis.Clear();
        }

        foreach (var modeloId in (modelosCelularIds ?? Array.Empty<int>()).Distinct())
            produto.ModelosCompativeis.Add(new ProdutoEstoqueModeloCompativel { ModeloCelularId = modeloId });
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

        if (produto.Imagem != null) ProdutoImagemServico.Remover(produto.Imagem);

        Context.ProdutosEstoque.Remove(produto);
        Context.SaveChanges();
        TempData["Sucesso"] = $"Produto {produto.Nome} excluído. As vendas antigas continuam no histórico.";
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Historico(int id)
    {
        var produto = Context.ProdutosEstoque
            .Include(p => p.Categoria)
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

    // Peso/dimensões são opcionais e, diferente de Preço/Custo, não têm máscara de
    // digitação — o usuário digita "1,5" à mão (natural em pt-BR). NumberStyles.Number
    // na cultura invariante trataria a vírgula como separador de milhar ("1,5" -> 15),
    // então a vírgula é normalizada para ponto antes do parse. "Não preenchido" vira
    // null (sem valor), não 0 (que pareceria um peso real de zero).
    private static decimal? ParaDecimalNullable(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return null;
        var normalizado = valor.Trim().Replace(",", ".");
        return decimal.TryParse(normalizado, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ? d : null;
    }

    private static string? Limpar(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
}
