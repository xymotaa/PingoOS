namespace ListasCompras.Models;

public class OrdemServico
{
    public int Id { get; set; }

    // Número sequencial da loja (OS-000001). Antes vinha do relógio no JavaScript
    // e não sobrevivia à impressão.
    public string Numero { get; set; } = string.Empty;

    public DateTime DataAbertura { get; set; } = DateTime.Now;
    public DateTime? DataEntrega { get; set; }
    public string Situacao { get; set; } = Situacoes.Aberta;

    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;

    // Quem emitiu. Preenche a linha "Responsável Técnico" da OS impressa.
    public int? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public string? DispositivoTipo { get; set; }
    public string? DispositivoMarca { get; set; }
    public string? DispositivoModelo { get; set; }
    public string? DispositivoSerie { get; set; }
    public bool SemNumeroSerie { get; set; }

    public string? Diagnostico { get; set; }

    public ICollection<ItemOrdemServico> Itens { get; set; } = new List<ItemOrdemServico>();

    public decimal Total => Itens.Sum(i => i.Total);

    public string DispositivoResumo
    {
        get
        {
            var partes = new[] { DispositivoMarca, DispositivoModelo }
                .Where(s => !string.IsNullOrWhiteSpace(s));
            var resumo = string.Join(" ", partes);
            return string.IsNullOrWhiteSpace(resumo) ? (DispositivoTipo ?? "—") : resumo;
        }
    }
}

public class ItemOrdemServico
{
    public int Id { get; set; }
    public int OrdemServicoId { get; set; }
    public OrdemServico OrdemServico { get; set; } = null!;

    public string Descricao { get; set; } = string.Empty;
    public int Quantidade { get; set; } = 1;
    public decimal ValorUnitario { get; set; }

    public decimal Total => Quantidade * ValorUnitario;
}

public static class Situacoes
{
    public const string Aberta = "Aberta";
    public const string Pronta = "Pronta";
    public const string Entregue = "Entregue";

    public static readonly string[] Todas = { Aberta, Pronta, Entregue };
}
