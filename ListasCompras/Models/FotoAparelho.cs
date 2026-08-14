namespace ListasCompras.Models;

// Fotografar o aparelho na entrada é a defesa contra "esse arranhão não estava aí" —
// reforça as cláusulas de risco e mau uso já impressas nos termos da OS.
public class FotoAparelho
{
    public int Id { get; set; }

    public int AparelhoOsId { get; set; }
    public AparelhoOs AparelhoOs { get; set; } = null!;

    // Nome do arquivo em disco (GUID + extensão); o nome original do upload não é guardado
    public string Arquivo { get; set; } = string.Empty;

    public DateTime DataEnvio { get; set; } = DateTime.Now;
}
