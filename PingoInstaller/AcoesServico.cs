namespace PingoInstaller;

// Ações disponíveis quando o PingoOS já está instalado — cada uma chama o próprio
// executável instalado (ListasCompras.exe) ou o sc.exe/net, nunca reimplementa a lógica
// de negócio aqui (o reset de senha, por exemplo, já existe em Program.cs do
// ListasCompras via "redefinir-senha", esta tela só chama).
static class AcoesServico
{
    public static void Reiniciar()
    {
        Console.WriteLine();
        Console.WriteLine("  Reiniciando o servico PingoOS...");
        Processos.Executar("net", $"stop \"{Config.NomeServico}\"", timeoutSegundos: 30);
        var resultado = Processos.Executar("net", $"start \"{Config.NomeServico}\"", timeoutSegundos: 30);
        Console.WriteLine(resultado.Sucesso ? "  Servico reiniciado." : "  Nao foi possivel reiniciar: " + resultado.Erro);
        Console.WriteLine();
        Console.WriteLine("  Pressione qualquer tecla para voltar...");
        Tela.AguardarTecla();
    }

    public static void Desligar()
    {
        Console.WriteLine();
        Console.WriteLine("  Desligando o servico PingoOS...");
        var resultado = Processos.Executar("net", $"stop \"{Config.NomeServico}\"", timeoutSegundos: 30);
        Console.WriteLine(resultado.Sucesso ? "  Servico parado." : "  Nao foi possivel parar: " + resultado.Erro);
        Console.WriteLine();
        Console.WriteLine("  Pressione qualquer tecla para voltar...");
        Tela.AguardarTecla();
    }

    public static void ResetarSenhaAdmin()
    {
        Console.WriteLine();
        Console.WriteLine("  === Resetar senha de administrador ===");
        Console.WriteLine();

        if (!File.Exists(Config.ExecutavelApp))
        {
            Console.WriteLine("  PingoOS ainda nao esta instalado nesta maquina.");
            Console.WriteLine();
            Console.WriteLine("  Pressione qualquer tecla para voltar...");
            Tela.AguardarTecla();
            return;
        }

        try { Console.CursorVisible = true; } catch { /* ignorado de propósito */ }
        Console.Write("  E-mail do usuario: ");
        var email = Console.ReadLine()?.Trim() ?? "";

        Console.Write("  Nova senha (min. 6 caracteres): ");
        var senha = LerSenhaMascarada();
        try { Console.CursorVisible = false; } catch { /* ignorado de propósito */ }

        if (string.IsNullOrWhiteSpace(email) || senha.Length < 6)
        {
            Console.WriteLine();
            Console.WriteLine("  E-mail ou senha invalidos.");
        }
        else
        {
            // Reaproveita a rotina que já existe no próprio sistema — o serviço não
            // precisa estar parado, ela abre o loja.db direto (ver Program.cs)
            var resultado = Processos.Executar(Config.ExecutavelApp, $"redefinir-senha \"{email}\" \"{senha}\"", timeoutSegundos: 30);
            Console.WriteLine();
            Console.WriteLine(resultado.Saida.Trim());
            if (!resultado.Sucesso && !string.IsNullOrWhiteSpace(resultado.Erro))
                Console.WriteLine(resultado.Erro.Trim());
        }

        Console.WriteLine();
        Console.WriteLine("  Pressione qualquer tecla para voltar...");
        Tela.AguardarTecla();
    }

    private static string LerSenhaMascarada()
    {
        var senha = "";
        ConsoleKeyInfo tecla;
        do
        {
            tecla = Console.ReadKey(intercept: true);
            if (tecla.Key == ConsoleKey.Backspace && senha.Length > 0)
            {
                senha = senha[..^1];
                Console.Write("\b \b");
            }
            else if (!char.IsControl(tecla.KeyChar))
            {
                senha += tecla.KeyChar;
                Console.Write("*");
            }
        } while (tecla.Key != ConsoleKey.Enter);
        Console.WriteLine();
        return senha;
    }
}
