namespace PingoInstaller;

record OpcaoMenu(string Rotulo, ConsoleColor Cor = ConsoleColor.Gray);

// Menu vertical navegável por seta (↑/↓ + Enter) ou pelo número/tecla numérica da opção.
// Desenha a partir de uma posição fixa (coluna, linha) e redesenha só as linhas do menu
// a cada movimento — a arte e o painel de informações ao redor não piscam.
static class Menu
{
    public static int Escolher(int coluna, int linhaTopo, IReadOnlyList<OpcaoMenu> opcoes, int selecionadoInicial = 0)
    {
        var selecionado = selecionadoInicial;
        DesenharTudo(coluna, linhaTopo, opcoes, selecionado);

        while (true)
        {
            var tecla = Console.ReadKey(intercept: true);

            switch (tecla.Key)
            {
                case ConsoleKey.UpArrow:
                    selecionado = (selecionado - 1 + opcoes.Count) % opcoes.Count;
                    DesenharTudo(coluna, linhaTopo, opcoes, selecionado);
                    break;
                case ConsoleKey.DownArrow:
                    selecionado = (selecionado + 1) % opcoes.Count;
                    DesenharTudo(coluna, linhaTopo, opcoes, selecionado);
                    break;
                case ConsoleKey.Enter:
                    return selecionado;
                default:
                    // Tecla numérica (linha ou numpad) escolhe direto pelo número da opção
                    var numero = NumeroDaTecla(tecla);
                    if (numero.HasValue && numero.Value >= 1 && numero.Value <= opcoes.Count)
                        return numero.Value - 1;
                    break;
            }
        }
    }

    // Pergunta binária (Instalar? y/n) — aceita Y/N, S/N (sim/não em pt-BR), Enter confirma
    // a opção realçada, setas alternam entre as duas.
    public static bool PerguntarSimNao(int coluna, int linha, string pergunta, bool padraoSim = true)
    {
        var opcoes = new[] { new OpcaoMenu("Sim", ConsoleColor.Green), new OpcaoMenu("Não", ConsoleColor.Red) };
        var selecionado = padraoSim ? 0 : 1;

        Tela.Escrever(coluna, linha, pergunta, ConsoleColor.White);
        DesenharSimNao(coluna, linha + 1, selecionado);

        while (true)
        {
            var tecla = Console.ReadKey(intercept: true);
            switch (tecla.Key)
            {
                case ConsoleKey.LeftArrow:
                case ConsoleKey.RightArrow:
                case ConsoleKey.Tab:
                    selecionado = 1 - selecionado;
                    DesenharSimNao(coluna, linha + 1, selecionado);
                    break;
                case ConsoleKey.Y:
                    return true;
                case ConsoleKey.N:
                    return false;
                case ConsoleKey.Enter:
                    return selecionado == 0;
            }
        }

        void DesenharSimNao(int c, int l, int sel)
        {
            Tela.Escrever(c, l, "  ", ConsoleColor.Gray);
            for (var i = 0; i < opcoes.Length; i++)
            {
                var marcado = i == sel;
                var texto = marcado ? $"[ {opcoes[i].Rotulo} ]" : $"  {opcoes[i].Rotulo}  ";
                Tela.Escrever(c + i * 12, l, texto, marcado ? opcoes[i].Cor : ConsoleColor.DarkGray);
            }
        }
    }

    private static void DesenharTudo(int coluna, int linhaTopo, IReadOnlyList<OpcaoMenu> opcoes, int selecionado)
    {
        for (var i = 0; i < opcoes.Count; i++)
        {
            var marcado = i == selecionado;
            var seta = marcado ? "▶ " : "  ";
            var texto = $"{seta}{i + 1}. {opcoes[i].Rotulo}".PadRight(40);

            Console.SetCursorPosition(coluna, linhaTopo + i);
            if (marcado)
            {
                Console.BackgroundColor = ConsoleColor.Cyan;
                Console.ForegroundColor = ConsoleColor.Black;
            }
            else
            {
                Console.ForegroundColor = opcoes[i].Cor;
            }
            Console.Write(texto);
            Console.ResetColor();
        }
    }

    private static int? NumeroDaTecla(ConsoleKeyInfo tecla)
    {
        if (tecla.Key >= ConsoleKey.D1 && tecla.Key <= ConsoleKey.D9)
            return tecla.Key - ConsoleKey.D0;
        if (tecla.Key >= ConsoleKey.NumPad1 && tecla.Key <= ConsoleKey.NumPad9)
            return tecla.Key - ConsoleKey.NumPad0;
        return null;
    }
}
