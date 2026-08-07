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
            .OrderByDescending(o => o.Id)
            .ToList();

        return View(ordens);
    }

    public IActionResult Add()
    {
        return View();
    }

    public IActionResult Ver(int id)
    {
        var ordem = Context.OrdensServico
            .Include(o => o.Cliente)
            .Include(o => o.Usuario)
            .Include(o => o.Itens)
            .FirstOrDefault(o => o.Id == id);

        if (ordem == null) return NotFound();
        return View(ordem);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Salvar(
        int clienteId,
        string? dispositivoTipo, string? dispositivoMarca, string? dispositivoModelo,
        string? dispositivoSerie, bool semNumeroSerie, string? diagnostico,
        // Valor chega como texto e é convertido com cultura invariante de propósito: o binding
        // do .NET usa a cultura do sistema, e num Windows/Linux em pt-BR "620.00" viraria 62000.
        string[]? itemDescricao, int[]? itemQuantidade, string[]? itemValor)
    {
        if (clienteId <= 0 || !Context.Clientes.Any(c => c.Id == clienteId))
        {
            TempData["Erro"] = "Selecione o cliente antes de salvar a ordem de serviço.";
            return RedirectToAction(nameof(Add));
        }

        var ordem = new OrdemServico
        {
            Numero = ProximoNumero(),
            ClienteId = clienteId,
            UsuarioId = IdDoUsuarioLogado(),
            DispositivoTipo = Limpar(dispositivoTipo),
            DispositivoMarca = Limpar(dispositivoMarca),
            DispositivoModelo = Limpar(dispositivoModelo),
            DispositivoSerie = semNumeroSerie ? null : Limpar(dispositivoSerie),
            SemNumeroSerie = semNumeroSerie,
            Diagnostico = Limpar(diagnostico),
        };

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

        Context.OrdensServico.Add(ordem);
        Context.SaveChanges();

        TempData["Sucesso"] = $"Ordem de serviço {ordem.Numero} salva.";
        return RedirectToAction(nameof(Ver), new { id = ordem.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AlterarSituacao(int id, string situacao)
    {
        var ordem = Context.OrdensServico.Find(id);
        if (ordem == null || !Situacoes.Todas.Contains(situacao)) return RedirectToAction(nameof(Index));

        ordem.Situacao = situacao;
        // A garantia conta da retirada, então a data de entrega precisa ficar registrada
        ordem.DataEntrega = situacao == Situacoes.Entregue ? DateTime.Now : null;
        Context.SaveChanges();

        TempData["Sucesso"] = $"Ordem {ordem.Numero} marcada como {situacao.ToLower()}.";
        return RedirectToAction(nameof(Index));
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

    private static string? Limpar(string? valor)
    {
        return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    }
}
