using System.Text;

namespace PingoInstaller;

// Desenho de baixo nível: cores e posicionamento por coordenada. Layout em coluna única,
// centralizado — nada de arte ASCII (blocos Unicode/braille não renderizam no console
// padrão do Windows 10: a fonte Consolas/Terminal não cobre esse bloco e vira "????").
static class Tela
{
    public const int LarguraJanela = 90;

    public static void Preparar()
    {
        Console.OutputEncoding = Encoding.UTF8;
        // Cada chamada aqui pode falhar dependendo de como o console foi alocado (ex:
        // sem console real por trás, ou terminal que recusa redimensionar) — nenhuma
        // delas é essencial ao funcionamento, só à aparência, então nunca derruba o app.
        try { Console.CursorVisible = false; } catch { /* ignorado de propósito */ }
        try { Console.SetWindowSize(Math.Min(LarguraJanela, Console.LargestWindowWidth), Math.Min(35, Console.LargestWindowHeight)); } catch { /* ignorado de propósito */ }
        try { Console.Title = "PingoOS — Instalador"; } catch { /* ignorado de propósito */ }
    }

    public static void Limpar()
    {
        Console.ResetColor();
        Console.Clear();
    }

    public static void Escrever(int coluna, int linha, string texto, ConsoleColor cor = ConsoleColor.Gray)
    {
        Console.SetCursorPosition(coluna, linha);
        Console.ForegroundColor = cor;
        Console.Write(texto);
        Console.ResetColor();
    }

    // Centraliza dentro da largura da janela atual (ou LarguraJanela, se o console não
    // souber informar a largura real).
    public static void EscreverCentralizado(int linha, string texto, ConsoleColor cor = ConsoleColor.Gray)
    {
        int largura;
        try { largura = Console.WindowWidth; } catch { largura = LarguraJanela; }
        var coluna = Math.Max(0, (largura - texto.Length) / 2);
        Escrever(coluna, linha, texto, cor);
    }

    public static void AguardarTecla()
    {
        Console.ReadKey(intercept: true);
    }

    // Barra "10% #####-----" em ASCII puro (sem bloco Unicode, mesmo motivo do logo).
    // largura fixa de 20 caracteres de barra, então a linha inteira sempre tem o mesmo
    // tamanho — sobrescreve sozinha a cada chamada, sem deixar lixo de uma leitura mais
    // longa que a anterior.
    public static string BarraProgresso(double percentual)
    {
        var pct = Math.Clamp(percentual, 0, 100);
        var preenchidos = (int)Math.Round(pct / 100 * 20);
        var barra = new string('#', preenchidos) + new string('-', 20 - preenchidos);
        return $"{pct,3:F0}% [{barra}]";
    }
}
