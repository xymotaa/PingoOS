namespace ListasCompras.Models;

/// <summary>Dado real por trás de cada card do Painel — antes disso, a tela era só maquete estática.</summary>
public class PainelViewModel
{
    public decimal VendasHoje { get; set; }
    public int ItensEmFalta { get; set; }
    public int NovosClientesHoje { get; set; }
    public decimal ContasAPagarPendentes { get; set; }

    /// <summary>Um ponto por dia dos últimos 7 dias, mais antigo primeiro.</summary>
    public List<PontoDesempenho> DesempenhoSemanal { get; set; } = new();

    public List<AtividadeRecente> AtividadesRecentes { get; set; } = new();
    public List<OrdemServico> UltimosOrcamentos { get; set; } = new();
}

public class PontoDesempenho
{
    public DateTime Dia { get; set; }
    public decimal Total { get; set; }
}

public class AtividadeRecente
{
    public DateTime Data { get; set; }
    public string Icone { get; set; } = "info";
    public string Descricao { get; set; } = string.Empty;
    public string? Url { get; set; }
}
