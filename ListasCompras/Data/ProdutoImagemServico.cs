namespace ListasCompras.Data;

// Mesma lógica de FotoAparelhoServico, mas para a imagem única de ProdutoEstoque — ver o
// comentário lá para o raciocínio de guardar em disco em vez de Base64 no banco.
public static class ProdutoImagemServico
{
    private const long TamanhoMaximoBytes = 8 * 1024 * 1024;

    private static readonly Dictionary<string, string> TiposAceitos = new()
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
    };

    public static string Pasta => Path.Combine(AppContext.BaseDirectory, "wwwroot", "uploads", "produtos");

    public static bool TipoValido(string? contentType) => contentType != null && TiposAceitos.ContainsKey(contentType);

    public static bool TamanhoValido(long tamanho) => tamanho > 0 && tamanho <= TamanhoMaximoBytes;

    /// <summary>Salva o arquivo com nome novo (GUID) — o nome original do upload não é confiável nem guardado.</summary>
    public static async Task<string> SalvarAsync(Stream conteudo, string contentType)
    {
        Directory.CreateDirectory(Pasta);
        var nome = $"{Guid.NewGuid():N}{TiposAceitos[contentType]}";
        var caminho = Path.Combine(Pasta, nome);

        using var destino = File.Create(caminho);
        await conteudo.CopyToAsync(destino);

        return nome;
    }

    /// <summary>Silencioso de propósito: se o arquivo já sumiu do disco, o registro ainda precisa sair do banco.</summary>
    public static void Remover(string arquivo)
    {
        var caminho = Path.Combine(Pasta, arquivo);
        if (File.Exists(caminho)) File.Delete(caminho);
    }
}
