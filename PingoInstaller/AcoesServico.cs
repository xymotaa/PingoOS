namespace PingoInstaller;

// Ações disponíveis quando o PingoOS já está instalado — cada uma chama o próprio
// executável instalado (ListasCompras.exe) ou o sc.exe/net, nunca reimplementa a lógica
// de negócio aqui (o reset de senha, por exemplo, já existe em Program.cs do
// ListasCompras via "redefinir-senha", esta tela só chama).
static class AcoesServico
{
    public static void Ligar()
    {
        Console.WriteLine();
        Console.WriteLine("  Ligando o servico PingoOS...");
        var resultado = Processos.Executar("net", $"start \"{Config.NomeServico}\"", timeoutSegundos: 30);
        Console.WriteLine(resultado.Sucesso ? "  Servico ligado." : "  Nao foi possivel ligar: " + resultado.Erro);
        Console.WriteLine();
        Console.WriteLine("  Pressione qualquer tecla para voltar...");
        Tela.AguardarTecla();
    }

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

    public static void DesinstalarInterativo()
    {
        Console.WriteLine();
        Console.WriteLine("  === Desinstalar PingoOS ===");
        Console.WriteLine();
        Console.WriteLine("  Isso para o servico, remove o registro dele no Windows e apaga o codigo");
        Console.WriteLine("  e o programa publicado desta maquina.");
        Console.WriteLine();

        try { Console.CursorVisible = true; } catch { /* ignorado de propósito */ }

        var temBanco = File.Exists(Path.Combine(Config.PastaApp, "loja.db"));
        var manterBanco = true;
        if (temBanco)
        {
            Console.WriteLine("  O banco de dados (clientes, vendas, ordens de servico) sera mantido");
            Console.WriteLine("  a menos que voce peca para apagar tambem.");
            Console.Write("  Apagar o banco de dados tambem? Digite APAGAR para confirmar (ou deixe em branco para manter): ");
            var respostaBanco = Console.ReadLine()?.Trim() ?? "";
            manterBanco = !string.Equals(respostaBanco, "APAGAR", StringComparison.OrdinalIgnoreCase);
            Console.WriteLine();
        }

        Console.Write("  Digite DESINSTALAR para confirmar: ");
        var confirmacao = Console.ReadLine()?.Trim() ?? "";
        try { Console.CursorVisible = false; } catch { /* ignorado de propósito */ }

        if (!string.Equals(confirmacao, "DESINSTALAR", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine();
            Console.WriteLine("  Cancelado — nada foi alterado.");
            Console.WriteLine();
            Console.WriteLine("  Pressione qualquer tecla para voltar...");
            Tela.AguardarTecla();
            return;
        }

        Console.WriteLine();
        var linhaStatus = Console.CursorTop;
        void Status(string mensagem)
        {
            Console.SetCursorPosition(0, linhaStatus);
            Console.Write(new string(' ', Console.WindowWidth - 1));
            Console.SetCursorPosition(0, linhaStatus);
            Console.Write("  " + mensagem);
        }

        PassoResultado resultado;
        try
        {
            resultado = Instalador.Desinstalar(Status, manterBanco);
        }
        catch (Exception ex)
        {
            resultado = new PassoResultado(false, "Erro inesperado: " + ex.Message);
        }
        Console.WriteLine();
        Console.WriteLine();
        if (resultado.Sucesso)
        {
            Console.WriteLine("  PingoOS desinstalado.");
            if (manterBanco && temBanco)
                Console.WriteLine("  O loja.db foi preservado em " + Config.PastaBase + ".");
        }
        else
        {
            Console.WriteLine("  Falha ao desinstalar: " + resultado.MensagemErro);
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
