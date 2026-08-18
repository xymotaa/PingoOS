using System.Diagnostics;
using ListasCompras.Data;
using ListasCompras.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ListasCompras.Controllers;

public class HomeController : LojaControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public HomeController(AppDbContext context, IHttpClientFactory httpClientFactory) : base(context)
    {
        _httpClientFactory = httpClientFactory;
    }

    public IActionResult Index()
    {
        var hoje = DateTime.Today;
        var amanha = hoje.AddDays(1);
        var seteDiasAtras = hoje.AddDays(-6); // hoje + 6 dias anteriores = 7 pontos

        var vendasHoje = Context.Vendas
            .Where(v => v.Data >= hoje && v.Data < amanha)
            .Include(v => v.Itens)
            .AsEnumerable()
            .Sum(v => v.Total);

        var itensEmFalta = Context.ProdutosEstoque
            .AsEnumerable()
            .Count(p => p.Situacao != "ok");

        var novosClientesHoje = Context.Clientes
            .Count(c => c.DataCadastro >= hoje && c.DataCadastro < amanha);

        var contasAPagarPendentes = Context.ContasAPagar
            .Where(c => !c.Paga)
            .Sum(c => c.Valor);

        var modelo = new PainelViewModel
        {
            VendasHoje = vendasHoje,
            ItensEmFalta = itensEmFalta,
            NovosClientesHoje = novosClientesHoje,
            ContasAPagarPendentes = contasAPagarPendentes,
            DesempenhoSemanal = DesempenhoUltimos7Dias(seteDiasAtras, amanha),
            AtividadesRecentes = AtividadesRecentes(),
            UltimosOrcamentos = Context.OrdensServico
                .Include(o => o.Cliente)
                .Where(o => o.Tipo == TiposDocumento.Orcamento)
                .OrderByDescending(o => o.Id)
                .Take(5)
                .Include(o => o.Itens)
                .ToList(),
        };

        return View(modelo);
    }

    // Soma vendas + pagamentos de OS por dia — a mesma fonte usada no Fechamento de Caixa,
    // só que os últimos 7 dias em vez de um dia só, para o gráfico do Painel.
    private List<PontoDesempenho> DesempenhoUltimos7Dias(DateTime inicio, DateTime fim)
    {
        var vendas = Context.Vendas
            .Where(v => v.Data >= inicio && v.Data < fim)
            .Include(v => v.Itens)
            .AsEnumerable()
            .GroupBy(v => v.Data.Date)
            .ToDictionary(g => g.Key, g => g.Sum(v => v.Total));

        var pagamentosOs = Context.PagamentosOrdemServico
            .Where(p => p.Data >= inicio && p.Data < fim)
            .AsEnumerable()
            .GroupBy(p => p.Data.Date)
            .ToDictionary(g => g.Key, g => g.Sum(p => p.Valor));

        var pontos = new List<PontoDesempenho>();
        for (var dia = inicio; dia < fim; dia = dia.AddDays(1))
        {
            var total = vendas.GetValueOrDefault(dia, 0m) + pagamentosOs.GetValueOrDefault(dia, 0m);
            pontos.Add(new PontoDesempenho { Dia = dia, Total = total });
        }
        return pontos;
    }

    // Feed simples: as últimas vendas, OS/orçamentos e contas pagas, misturados por data —
    // não existe uma tabela de "log de atividades", então isso é composto na hora
    private List<AtividadeRecente> AtividadesRecentes()
    {
        var atividades = new List<AtividadeRecente>();

        atividades.AddRange(Context.Vendas
            .OrderByDescending(v => v.Id).Take(5)
            .Select(v => new { v.Id, v.Numero, v.Data })
            .AsEnumerable()
            .Select(v => new AtividadeRecente
            {
                Data = v.Data,
                Icone = "point_of_sale",
                Descricao = $"Venda {v.Numero} registrada",
                Url = Url.Action("Vendas", "Caixa"),
            }));

        atividades.AddRange(Context.OrdensServico
            .Include(o => o.Cliente)
            .OrderByDescending(o => o.Id).Take(5)
            .AsEnumerable()
            .Select(o => new AtividadeRecente
            {
                Data = o.DataAbertura,
                Icone = o.EhOrcamento ? "request_quote" : "receipt_long",
                Descricao = $"{o.Rotulo} {o.Numero} — {o.Cliente.Nome}",
                Url = o.EhOrcamento
                    ? Url.Action("Ver", "Orcamento", new { id = o.Id })
                    : Url.Action("Ver", "OrdemServico", new { id = o.Id }),
            }));

        atividades.AddRange(Context.ContasAPagar
            .Where(c => c.Paga && c.DataPagamento != null)
            .OrderByDescending(c => c.Id).Take(5)
            .AsEnumerable()
            .Select(c => new AtividadeRecente
            {
                Data = c.DataPagamento!.Value,
                Icone = "payments",
                Descricao = $"Conta \"{c.Descricao}\" paga",
                Url = Url.Action("Index", "Financeiro"),
            }));

        return atividades.OrderByDescending(a => a.Data).Take(8).ToList();
    }

    // Checado via fetch depois que o Painel já carregou — nunca atrasa a tela principal
    // esperando resposta do GitHub. Só administrador vê o aviso: é quem decide atualizar.
    [Authorize(Roles = Papeis.Admin)]
    public async Task<IActionResult> VerificarVersao()
    {
        var local = VersaoServico.VersaoLocal();
        var remota = await VersaoServico.VersaoRemotaAsync(_httpClientFactory.CreateClient());

        return Json(new
        {
            local,
            remota,
            atualizacaoDisponivel = remota != null && VersaoServico.RemotaMaisNova(local, remota),
        });
    }

    // A página de erro precisa aparecer mesmo para quem não está logado
    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
