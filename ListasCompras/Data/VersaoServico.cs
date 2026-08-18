namespace ListasCompras.Data;

/// <summary>
/// Compara a versão instalada (VERSION.txt publicado junto com o binário) contra a versão
/// mais recente do repositório no GitHub, para avisar no Painel quando há uma atualização.
/// Não atualiza sozinho — decisão registrada no ROADMAP.md: o .NET mantém as DLLs carregadas
/// em memória, então trocar os arquivos por baixo do processo em execução é arriscado demais
/// sem um supervisor separado. Quem atualiza continua sendo a pessoa, rodando o instalador
/// de novo (que já preserva o loja.db).
/// </summary>
public static class VersaoServico
{
    private const string UrlVersaoRemota = "https://raw.githubusercontent.com/xymotaa/xypedidos/main/VERSION.txt";

    public static string VersaoLocal()
    {
        var caminho = Path.Combine(AppContext.BaseDirectory, "VERSION.txt");
        return File.Exists(caminho) ? File.ReadAllText(caminho).Trim() : "0.0.0.0";
    }

    /// <summary>
    /// Null quando não dá para checar (sem internet, GitHub fora do ar) — o aviso simplesmente
    /// não aparece, nunca trava a tela por causa disso.
    /// </summary>
    public static async Task<string?> VersaoRemotaAsync(HttpClient http)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            var texto = await http.GetStringAsync(UrlVersaoRemota, cts.Token);
            return texto.Trim();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Compara "1.0.0.12" contra "1.0.0.9" número a número, não como texto
    /// (texto compararia "12" menor que "9").</summary>
    public static bool RemotaMaisNova(string local, string remota)
    {
        var partesLocal = PartesNumericas(local);
        var partesRemota = PartesNumericas(remota);
        if (partesLocal == null || partesRemota == null) return false;

        for (var i = 0; i < Math.Max(partesLocal.Length, partesRemota.Length); i++)
        {
            var l = i < partesLocal.Length ? partesLocal[i] : 0;
            var r = i < partesRemota.Length ? partesRemota[i] : 0;
            if (r != l) return r > l;
        }
        return false;
    }

    private static int[]? PartesNumericas(string versao)
    {
        var partes = versao.Split('.');
        var numeros = new int[partes.Length];
        for (var i = 0; i < partes.Length; i++)
        {
            if (!int.TryParse(partes[i], out numeros[i])) return null;
        }
        return numeros;
    }
}
