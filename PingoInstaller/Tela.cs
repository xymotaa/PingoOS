using System.Text;

namespace PingoInstaller;

// Desenho de baixo nível: cores, posicionamento por coordenada, e composição de duas
// colunas lado a lado (arte à esquerda, texto à direita) — nada aqui sabe o que é
// instalação ou menu, só sabe pintar caracteres na tela.
static class Tela
{
    // A arte (AsciiArt.txt) tem até 40 colunas de largura em algumas linhas — o painel
    // de texto começa depois disso com uma folga, pra nunca sobrepor.
    public const int ColunaDireita = 46;
    public const int LarguraMinima = 90;

    public static void Preparar()
    {
        Console.OutputEncoding = Encoding.UTF8;
        // Cada chamada aqui pode falhar dependendo de como o console foi alocado (ex:
        // sem console real por trás, ou terminal que recusa redimensionar) — nenhuma
        // delas é essencial ao funcionamento, só à aparência, então nunca derruba o app.
        try { Console.CursorVisible = false; } catch { /* ignorado de propósito */ }
        try { Console.SetWindowSize(Math.Min(120, Console.LargestWindowWidth), Math.Min(40, Console.LargestWindowHeight)); } catch { /* ignorado de propósito */ }
        try { Console.Title = "PingoOS — Instalador"; } catch { /* ignorado de propósito */ }
    }

    public static void Limpar()
    {
        Console.ResetColor();
        Console.Clear();
    }

    // Verdadeiro quando a janela é estreita demais pro painel de texto (a partir da
    // coluna 46) não colidir com a arte — SetWindowSize já tenta abrir largo o
    // suficiente em Preparar(), isso aqui cobre quando o usuário redimensiona depois
    // ou o terminal recusou o pedido.
    public static bool JanelaEstreitaDemais()
    {
        try { return Console.WindowWidth < LarguraMinima; }
        catch { return false; } // sem like informação de largura, assume que está tudo bem
    }

    public static void Escrever(int coluna, int linha, string texto, ConsoleColor cor = ConsoleColor.Gray)
    {
        Console.SetCursorPosition(coluna, linha);
        Console.ForegroundColor = cor;
        Console.Write(texto);
        Console.ResetColor();
    }

    // Arte embutida no assembly (não um arquivo solto ao lado do .exe) — a distribuição
    // é um único binário baixado da Release do GitHub, então nada pode depender de outro
    // arquivo estar na mesma pasta.
    public static void DesenharArte(int linhaTopo)
    {
        var assembly = typeof(Tela).Assembly;
        var nomeRecurso = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("AsciiArt.txt", StringComparison.OrdinalIgnoreCase));
        if (nomeRecurso == null) return;

        using var stream = assembly.GetManifestResourceStream(nomeRecurso);
        if (stream == null) return;
        using var leitor = new StreamReader(stream, Encoding.UTF8);

        var linha = 0;
        while (leitor.ReadLine() is { } texto)
        {
            Escrever(0, linhaTopo + linha, texto, ConsoleColor.Cyan);
            linha++;
        }
    }

    public static void AguardarTecla()
    {
        Console.ReadKey(intercept: true);
    }
}
