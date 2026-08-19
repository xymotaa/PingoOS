using System.Security.Principal;
using PingoInstaller;

Tela.Preparar();

if (!EhAdministrador())
{
    Console.WriteLine();
    Console.WriteLine("  Este instalador precisa ser executado como Administrador.");
    Console.WriteLine("  O manifesto do aplicativo ja pede elevacao automatica (UAC) - se voce");
    Console.WriteLine("  ainda assim esta vendo esta mensagem, abra-o novamente e aceite o UAC.");
    Console.WriteLine();
    Console.WriteLine("  Pressione qualquer tecla para sair...");
    Tela.AguardarTecla();
    return 1;
}

while (true)
{
    var linhaMenu = DesenharTelaBase();

    if (Config.JaInstalado())
    {
        var opcoes = new[]
        {
            new OpcaoMenu("Atualizar"),
            new OpcaoMenu("Reinstalar"),
            new OpcaoMenu("Ligar servidor"),
            new OpcaoMenu("Reiniciar servidor"),
            new OpcaoMenu("Resetar senha admin"),
            new OpcaoMenu("Desligar servidor"),
            new OpcaoMenu("Desinstalar", ConsoleColor.Red),
            new OpcaoMenu("Sair", ConsoleColor.DarkGray),
        };
        var colunaMenu = ColunaCentralizada(opcoes.Max(o => o.Rotulo.Length) + 4);
        var escolha = Menu.Escolher(colunaMenu, linhaMenu, opcoes);

        switch (escolha)
        {
            case 0: RodarInstalacao(reinstalar: false); break;
            case 1: RodarInstalacao(reinstalar: true); break;
            case 2: AcoesServico.Ligar(); break;
            case 3: AcoesServico.Reiniciar(); break;
            case 4: AcoesServico.ResetarSenhaAdmin(); break;
            case 5: AcoesServico.Desligar(); break;
            case 6: AcoesServico.DesinstalarInterativo(); break;
            case 7: return 0;
        }
    }
    else
    {
        var colunaPergunta = ColunaCentralizada(20);
        var instalar = Menu.PerguntarSimNao(colunaPergunta, linhaMenu, "Instalar PingoOS?", padraoSim: true);
        if (!instalar) return 0;
        RodarInstalacao(reinstalar: true);
    }
}

// Atualizar pressupõe que .NET/Git já estão presentes e o serviço já existe — pula direto
// para buscar a versão mais nova e republicar (mais rápido). Reinstalar roda o pipeline
// completo: checa .NET/Git, para o serviço, apaga e reclona o código do zero. A primeira
// instalação (Config.JaInstalado() == false) sempre é tratada como reinstalar.
void RodarInstalacao(bool reinstalar)
{
    Console.Clear();
    Console.ResetColor();
    Console.WriteLine();
    Console.WriteLine("  ======================================");
    Console.WriteLine("   " + (reinstalar ? "Reinstalando PingoOS" : "Atualizando PingoOS"));
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

    var passos = reinstalar
        ? new Func<Action<string>, PassoResultado>[]
        {
            Instalador.GarantirDotnet,
            Instalador.GarantirGit,
            Instalador.PararServicoSeExistir,
            Instalador.ForcarCloneLimpo,
            Instalador.BaixarUltimaVersao,
            Instalador.PublicarERegistrarServico,
        }
        : new Func<Action<string>, PassoResultado>[]
        {
            // Sem GarantirDotnet/GarantirGit (pressupõe que já estão instalados), mas
            // ainda precisa parar o serviço: publicar por cima do .exe em execução falha
            // com "Access to the path is denied".
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
    Console.WriteLine("   Pronto! O PingoOS esta rodando.");
    Console.WriteLine("  ======================================");
    Console.ResetColor();
    Console.WriteLine();
    Console.WriteLine("  Endereco: " + Config.Url);
    Console.WriteLine("  Ele inicia sozinho toda vez que o computador ligar.");
    Console.WriteLine();

    Processos.AbrirNoNavegador(Config.Url);

    Console.WriteLine("  Pressione qualquer tecla para voltar ao menu...");
    Tela.AguardarTecla();
}

// Desenha logo + painel de info e devolve a primeira linha livre abaixo deles, onde o
// menu (ou a pergunta Instalar y/n) deve começar.
int DesenharTelaBase()
{
    Tela.Limpar();

    // Centraliza o logo como um bloco único (mesma coluna inicial para todas as linhas),
    // não linha por linha — senão a largura desigual de cada linha do desenho ASCII
    // desalinha as letras entre si.
    var larguraLogo = Logo.Linhas.Max(l => l.Length);
    var colunaLogo = ColunaCentralizada(larguraLogo);
    var linha = 1;
    foreach (var linhaLogo in Logo.Linhas)
    {
        Tela.Escrever(colunaLogo, linha, linhaLogo, ConsoleColor.Cyan);
        linha++;
    }

    linha += 1;
    Tela.EscreverCentralizado(linha, "Maquina: " + InfoMaquina.ObterMaquina() + "   IP: " + InfoMaquina.ObterIpLocal(), ConsoleColor.Gray);
    linha++;
    Tela.EscreverCentralizado(linha, "Usuario: " + InfoMaquina.ObterUsuario() + "   " + InfoMaquina.ObterDataHora(), ConsoleColor.Gray);
    linha += 2;

    Tela.EscreverCentralizado(linha, Config.JaInstalado()
        ? "PingoOS ja esta instalado nesta maquina."
        : "PingoOS ainda nao esta instalado nesta maquina.", ConsoleColor.DarkYellow);
    linha += 2;

    return linha;
}

int ColunaCentralizada(int larguraConteudo)
{
    int largura;
    try { largura = Console.WindowWidth; } catch { largura = Tela.LarguraJanela; }
    return Math.Max(0, (largura - larguraConteudo) / 2);
}

static bool EhAdministrador()
{
    if (!OperatingSystem.IsWindows()) return true; // manifesto/UAC só existe no Windows
    using var identidade = WindowsIdentity.GetCurrent();
    var principal = new WindowsPrincipal(identidade);
    return principal.IsInRole(WindowsBuiltInRole.Administrator);
}
