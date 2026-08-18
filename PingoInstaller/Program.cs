using System.Security.Principal;
using PingoInstaller;

Tela.Preparar();

if (!EhAdministrador())
{
    Console.WriteLine();
    Console.WriteLine("  Este instalador precisa ser executado como Administrador.");
    Console.WriteLine("  O manifesto do aplicativo já pede elevação automática (UAC) — se você");
    Console.WriteLine("  ainda assim está vendo esta mensagem, abra-o novamente e aceite o UAC.");
    Console.WriteLine();
    Console.WriteLine("  Pressione qualquer tecla para sair...");
    Tela.AguardarTecla();
    return 1;
}

while (true)
{
    var desenhouLayout = DesenharTelaBase();
    if (!desenhouLayout) continue; // janela estreita: já mostrou o aviso, tenta de novo

    if (Config.JaInstalado())
    {
        var opcoes = new[]
        {
            new OpcaoMenu("Atualizar / reinstalar"),
            new OpcaoMenu("Reiniciar servidor"),
            new OpcaoMenu("Resetar senha admin"),
            new OpcaoMenu("Desligar servidor"),
            new OpcaoMenu("Sair", ConsoleColor.DarkGray),
        };
        var escolha = Menu.Escolher(Tela.ColunaDireita, 10, opcoes);

        switch (escolha)
        {
            case 0: RodarInstalacao(); break;
            case 1: AcoesServico.Reiniciar(); break;
            case 2: AcoesServico.ResetarSenhaAdmin(); break;
            case 3: AcoesServico.Desligar(); break;
            case 4: return 0;
        }
    }
    else
    {
        var instalar = Menu.PerguntarSimNao(Tela.ColunaDireita, 10, "Instalar PingoOS?", padraoSim: true);
        if (!instalar) return 0;
        RodarInstalacao();
    }
}

void RodarInstalacao()
{
    Console.Clear();
    Console.ResetColor();
    Console.WriteLine();
    Console.WriteLine("  ======================================");
    Console.WriteLine("   Instalando PingoOS");
    Console.WriteLine("  ======================================");
    Console.WriteLine();

    var linhaStatus = Console.CursorTop;
    void Status(string mensagem)
    {
        Console.SetCursorPosition(0, linhaStatus);
        Console.Write(new string(' ', Console.WindowWidth - 1));
        Console.SetCursorPosition(0, linhaStatus);
        Console.Write("  " + mensagem);
    }

    var passos = new Func<Action<string>, PassoResultado>[]
    {
        Instalador.GarantirDotnet,
        Instalador.GarantirGit,
        Instalador.PararServicoSeExistir,
        Instalador.BaixarUltimaVersao,
        Instalador.PublicarERegistrarServico,
    };

    foreach (var passo in passos)
    {
        var resultado = passo(Status);
        if (!resultado.Sucesso)
        {
            Console.WriteLine();
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  [ERRO] " + resultado.MensagemErro);
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("  Pressione qualquer tecla para voltar ao menu...");
            Tela.AguardarTecla();
            return;
        }
    }

    Console.WriteLine();
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("  ======================================");
    Console.WriteLine("   Pronto! O PingoOS está rodando.");
    Console.WriteLine("  ======================================");
    Console.ResetColor();
    Console.WriteLine();
    Console.WriteLine("  Endereço: " + Config.Url);
    Console.WriteLine("  Ele inicia sozinho toda vez que o computador ligar.");
    Console.WriteLine();

    Processos.AbrirNoNavegador(Config.Url);

    Console.WriteLine("  Pressione qualquer tecla para voltar ao menu...");
    Tela.AguardarTecla();
}

bool DesenharTelaBase()
{
    Tela.Limpar();

    if (Tela.JanelaEstreitaDemais())
    {
        Console.WriteLine();
        Console.WriteLine("  Maximize esta janela (ou aumente a largura) para ver o instalador");
        Console.WriteLine($"  corretamente — precisa de pelo menos {Tela.LarguraMinima} colunas.");
        Console.WriteLine();
        Console.WriteLine("  Pressione qualquer tecla depois de redimensionar...");
        Tela.AguardarTecla();
        return false;
    }

    Tela.DesenharArte(linhaTopo: 1);

    var col = Tela.ColunaDireita;
    Tela.Escrever(col, 1, "PingoOS", ConsoleColor.Cyan);
    Tela.Escrever(col, 2, "════════", ConsoleColor.DarkCyan);

    Tela.Escrever(col, 4, $"Máquina  : {InfoMaquina.ObterMaquina()}", ConsoleColor.Gray);
    Tela.Escrever(col, 5, $"IP local : {InfoMaquina.ObterIpLocal()}", ConsoleColor.Gray);
    Tela.Escrever(col, 6, $"Usuário  : {InfoMaquina.ObterUsuario()}", ConsoleColor.Gray);
    Tela.Escrever(col, 7, $"Data/hora: {InfoMaquina.ObterDataHora()}", ConsoleColor.Gray);

    Tela.Escrever(col, 9, Config.JaInstalado()
        ? "PingoOS já está instalado nesta máquina."
        : "PingoOS ainda não está instalado nesta máquina.", ConsoleColor.DarkYellow);

    return true;
}

static bool EhAdministrador()
{
    if (!OperatingSystem.IsWindows()) return true; // manifesto/UAC só existe no Windows
    using var identidade = WindowsIdentity.GetCurrent();
    var principal = new WindowsPrincipal(identidade);
    return principal.IsInRole(WindowsBuiltInRole.Administrator);
}
