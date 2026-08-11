namespace ListasCompras.Models;

// Um cliente pode deixar mais de um aparelho na mesma visita, e abrir uma OS por
// aparelho separaria o que na prática é um atendimento só.
public class AparelhoOs
{
    public int Id { get; set; }

    public int OrdemServicoId { get; set; }
    public OrdemServico OrdemServico { get; set; } = null!;

    public string? Tipo { get; set; }
    public string? Marca { get; set; }
    public string? Modelo { get; set; }
    public string? NumeroSerie { get; set; }
    public bool SemNumeroSerie { get; set; }

    public string Resumo
    {
        get
        {
            var partes = new[] { Marca, Modelo }.Where(s => !string.IsNullOrWhiteSpace(s));
            var resumo = string.Join(" ", partes);
            return string.IsNullOrWhiteSpace(resumo) ? (Tipo ?? "—") : resumo;
        }
    }
}
