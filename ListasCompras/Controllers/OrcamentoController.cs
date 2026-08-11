using System.Globalization;
using System.Security.Claims;
using ListasCompras.Data;
using ListasCompras.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ListasCompras.Controllers;

public class OrcamentoController : LojaControllerBase
{
    public OrcamentoController(AppDbContext context) : base(context) { }

    public IActionResult Index()
    {
        var ordens = Context.OrdensServico
            .Include(o => o.Cliente)
            .Include(o => o.Itens)
            .Include(o => o.Aparelhos)
            .OrderByDescending(o => o.Id)
            .ToList();

        return View(ordens);
    }

    // Mesma tela cria e edita: faltar um dado ou digitar errado é comum, e reabrir a OS
    // é melhor que excluir e refazer, que perderia o número e o histórico
    public IActionResult Add(int? id)
    {
        if (!id.HasValue) return View(new OrdemServico());

        var ordem = Context.OrdensServico
            .Include(o => o.Cliente)
            .Include(o => o.Itens)
            .Include(o => o.Aparelhos)
            .FirstOrDefault(o => o.Id == id.Value);

        if (ordem == null) return NotFound();
        return View(ordem);
    }

    public IActionResult Ver(int id)
    {
        var ordem = Context.OrdensServico
            .Include(o => o.Cliente)
            .Include(o => o.Usuario)
            .Include(o => o.Itens)
            .Include(o => o.Aparelhos)
            .FirstOrDefault(o => o.Id == id);

        if (ordem == null) return NotFound();
        return View(ordem);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Salvar(
        int id, int clienteId, string? diagnostico,
        string[]? aparelhoTipo, string[]? aparelhoMarca, string[]? aparelhoModelo,
        string[]? aparelhoSerie, string[]? aparelhoSemSerie,
        string? sinal, string? desconto, string? descontoTipo,
        string? formaPagamento, bool parcelado, int parcelas,
        // Valor chega como texto e é convertido com cultura invariante de propósito: o binding
        // do .NET usa a cultura do sistema, e num Windows/Linux em pt-BR "620.00" viraria 62000.
        string[]? itemDescricao, int[]? itemQuantidade, string[]? itemValor)
    {
        if (clienteId <= 0 || !Context.Clientes.Any(c => c.Id == clienteId))
        {
            TempData["Erro"] = "Selecione o cliente antes de salvar a ordem de serviço.";
            return RedirectToAction(nameof(Add), id > 0 ? new { id } : null);
        }

        var ordem = id > 0
            ? Context.OrdensServico.Include(o => o.Itens).Include(o => o.Aparelhos).FirstOrDefault(o => o.Id == id)
            : null;
        if (id > 0 && ordem == null) return NotFound();

        var novo = ordem == null;
        if (novo)
        {
            ordem = new OrdemServico
            {
                Numero = ProximoNumero(),
                UsuarioId = IdDoUsuarioLogado(),
            };
        }
        else
        {
            // Itens e aparelhos são substituídos pelos que vieram do formulário
            Context.ItensOrdemServico.RemoveRange(ordem!.Itens);
            ordem.Itens.Clear();
            Context.AparelhosOs.RemoveRange(ordem.Aparelhos);
            ordem.Aparelhos.Clear();
        }

        ordem!.ClienteId = clienteId;
        ordem.Diagnostico = Limpar(diagnostico);

        ordem.Sinal = ParaDecimal(sinal);
        ordem.Desconto = ParaDecimal(desconto);
        ordem.DescontoTipo = descontoTipo == TiposDesconto.Valor ? TiposDesconto.Valor : TiposDesconto.Percentual;
        ordem.FormaPagamento = Limpar(formaPagamento);
        ordem.Parcelado = parcelado;
        ordem.Parcelas = parcelado ? Math.Clamp(parcelas, 1, 24) : 1;

        // Aparelhos: um cliente pode deixar mais de um na mesma visita
        if (aparelhoModelo != null)
        {
            for (var i = 0; i < aparelhoModelo.Length && ordem.Aparelhos.Count < OrdemServico.MaximoAparelhos; i++)
            {
                var tipo = Limpar(Em(aparelhoTipo, i));
                var marca = Limpar(Em(aparelhoMarca, i));
                var modelo = Limpar(aparelhoModelo[i]);
                var semSerie = Em(aparelhoSemSerie, i) == "1";

                // Bloco vazio do formulário não vira aparelho
                if (tipo == null && marca == null && modelo == null) continue;

                ordem.Aparelhos.Add(new AparelhoOs
                {
                    Tipo = tipo,
                    Marca = marca,
                    Modelo = modelo,
                    NumeroSerie = semSerie ? null : Limpar(Em(aparelhoSerie, i)),
                    SemNumeroSerie = semSerie,
                });
            }
        }

        if (itemDescricao != null)
        {
            for (var i = 0; i < itemDescricao.Length; i++)
            {
                var descricao = Limpar(itemDescricao[i]);
                var quantidade = itemQuantidade != null && i < itemQuantidade.Length ? itemQuantidade[i] : 0;
                var valor = itemValor != null && i < itemValor.Length ? ParaDecimal(itemValor[i]) : 0m;

                // Linha em branco no formulário não vira item
                if (descricao == null && valor == 0) continue;

                ordem.Itens.Add(new ItemOrdemServico
                {
                    Descricao = descricao ?? "—",
                    Quantidade = quantidade > 0 ? quantidade : 1,
                    ValorUnitario = valor,
                });
            }
        }

        if (novo) Context.OrdensServico.Add(ordem);
        Context.SaveChanges();

        TempData["Sucesso"] = novo
            ? $"Ordem de serviço {ordem.Numero} salva."
            : $"Ordem de serviço {ordem.Numero} atualizada.";
        return RedirectToAction(nameof(Ver), new { id = ordem.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AlterarSituacao(int id, string situacao, string? retorno)
    {
        var ordem = Context.OrdensServico.Find(id);
        if (ordem == null || !Situacoes.Todas.Contains(situacao)) return RedirectToAction(nameof(Index));

        ordem.Situacao = situacao;

        // A garantia conta da retirada. Voltar atrás limpa a data — foi marcada por engano,
        // então não faz sentido a garantia seguir contando daquele dia.
        if (situacao == Situacoes.Entregue)
            ordem.DataEntrega ??= DateTime.Now;
        else
            ordem.DataEntrega = null;

        Context.SaveChanges();

        TempData["Sucesso"] = $"Ordem {ordem.Numero} agora está como {situacao.ToLower()}.";
        return retorno == "Ver"
            ? RedirectToAction(nameof(Ver), new { id })
            : RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Excluir(int id)
    {
        var ordem = Context.OrdensServico.Include(o => o.Itens).FirstOrDefault(o => o.Id == id);
        if (ordem != null)
        {
            Context.OrdensServico.Remove(ordem);
            Context.SaveChanges();
            TempData["Sucesso"] = $"Ordem {ordem.Numero} excluída.";
        }
        return RedirectToAction(nameof(Index));
    }

    // OS-000001, OS-000002... continua de onde parou mesmo se a última for excluída
    private string ProximoNumero()
    {
        var ultimo = Context.OrdensServico
            .OrderByDescending(o => o.Id)
            .Select(o => o.Numero)
            .FirstOrDefault();

        var sequencia = 0;
        if (ultimo != null && ultimo.StartsWith("OS-") && int.TryParse(ultimo[3..], out var n))
            sequencia = n;

        return $"OS-{sequencia + 1:D6}";
    }

    private int? IdDoUsuarioLogado()
    {
        return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    }

    // O campo escondido do formulário sempre manda ponto decimal (toFixed)
    private static decimal ParaDecimal(string? valor)
    {
        return decimal.TryParse(valor, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ? d : 0m;
    }

    private static string? Em(string[]? lista, int i)
        => lista != null && i < lista.Length ? lista[i] : null;

    private static string? Limpar(string? valor)
    {
        return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    }
}
