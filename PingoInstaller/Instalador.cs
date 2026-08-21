using System.Text.Json;

namespace PingoInstaller;

record PassoResultado(bool Sucesso, string? MensagemErro = null);

// Mesma sequência do install.bat, linha por linha — só a forma de mostrar progresso
// muda (aqui atualiza uma linha de status na TUI em vez de imprimir echo). Qualquer
// mudança de comportamento de instalação deve ficar igual nos dois enquanto ambos
// existirem; ver ROADMAP.md sobre a descontinuação do .bat quando este for validado.
static class Instalador
{
    // Caminho fixo, checado direto no disco em vez de confiar só no PATH — depois de
    // instalar algo nesta mesma execução, o PATH do processo atual não reflete
    // atualizações feitas por um instalador externo (Inno Setup, winget) de forma
    // confiável, e foi exatamente por causa disso que a checagem "where git" continuava
    // dando negativo mesmo com o Git já instalado no disco.
    private static string CaminhoGit =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Git", "cmd", "git.exe");

    private static string CaminhoDotnet =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet", "dotnet.exe");

    public static PassoResultado GarantirDotnet(Action<string> status)
    {
        status("Verificando o .NET...");
        if (Processos.ComandoExiste("dotnet") || File.Exists(CaminhoDotnet))
        {
            status("Verificando o .NET... ja instalado.");
            AdicionarAoPathDoProcesso(Path.GetDirectoryName(CaminhoDotnet)!);
            return new PassoResultado(true);
        }

        var instalarDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet");
        var tempScript = Path.Combine(Path.GetTempPath(), "dotnet-install.ps1");

        status("Verificando o .NET... baixando o instalador...  " + Tela.BarraProgresso(0));
        var baixouScript = Processos.BaixarArquivo(
            "https://dot.net/v1/dotnet-install.ps1", tempScript,
            p => status("Verificando o .NET... baixando o instalador...  " + Tela.BarraProgresso(p)));
        if (!baixouScript)
            return new PassoResultado(false, "Nao foi possivel baixar o instalador do .NET. Confira sua conexao com a internet.");

        status("Verificando o .NET... instalando (pode levar alguns minutos)...");
        var instalacao = Processos.Executar("powershell",
            $"-NoProfile -ExecutionPolicy Bypass -File \"{tempScript}\" -Channel 10.0 -InstallDir \"{instalarDir}\"",
            timeoutSegundos: 600);
        File.Delete(tempScript);
        if (!instalacao.Sucesso)
            return new PassoResultado(false, "Falha ao instalar o .NET: " + instalacao.Erro);

        AdicionarAoPathDoProcesso(instalarDir);
        // setx não expande %PATH% quando chamado direto via Process.Start (isso só
        // acontece dentro de um cmd.exe interpretando um .bat) — por isso lemos o valor
        // atual da variável de máquina e escrevemos o novo PATH por extenso.
        var pathMaquina = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine) ?? "";
        if (!pathMaquina.Contains(instalarDir, StringComparison.OrdinalIgnoreCase))
        {
            Processos.Executar("setx", $"PATH \"{pathMaquina};{instalarDir}\" /M", timeoutSegundos: 15);
        }

        if (!File.Exists(CaminhoDotnet))
            return new PassoResultado(false, "O instalador do .NET rodou, mas o dotnet.exe nao apareceu em " + instalarDir + ".");

        status("Verificando o .NET... instalado.");
        return new PassoResultado(true);
    }

    public static PassoResultado GarantirGit(Action<string> status)
    {
        status("Verificando o Git...");
        if (Processos.ComandoExiste("git") || File.Exists(CaminhoGit))
        {
            status("Verificando o Git... ja instalado.");
            AdicionarAoPathDoProcesso(Path.GetDirectoryName(CaminhoGit)!);
            return new PassoResultado(true);
        }

        if (Processos.ComandoExiste("winget"))
        {
            status("Verificando o Git... nao encontrado, instalando via winget...");
            Processos.Executar("winget",
                "install --id Git.Git -e --source winget --accept-package-agreements --accept-source-agreements --silent",
                timeoutSegundos: 300);

            if (File.Exists(CaminhoGit))
            {
                status("Verificando o Git... instalado.");
                AdicionarAoPathDoProcesso(Path.GetDirectoryName(CaminhoGit)!);
                return new PassoResultado(true);
            }
        }

        // O nome do arquivo muda a cada versão (ex: Git-2.55.0.4-64-bit.exe) — um link
        // fixo tipo ".../latest/download/Git-64-bit.exe" fica quebrado (404) assim que a
        // versão muda de número. A API do GitHub resolve o nome real da versão atual.
        status("Verificando o Git... buscando o link de download mais recente...");
        var urlGit = ResolverUrlUltimoInstaladorGit();
        if (urlGit == null)
            return new PassoResultado(false, "Nao foi possivel encontrar o instalador do Git no GitHub. Instale manualmente em https://git-scm.com/download/win e rode de novo.");

        status("Verificando o Git... baixando o instalador oficial...  " + Tela.BarraProgresso(0));
        var tempInstalador = Path.Combine(Path.GetTempPath(), "git-installer.exe");
        var baixou = Processos.BaixarArquivo(
            urlGit, tempInstalador,
            p => status("Verificando o Git... baixando o instalador oficial...  " + Tela.BarraProgresso(p)));
        if (!baixou)
            return new PassoResultado(false, "Nao foi possivel baixar o instalador do Git. Instale manualmente em https://git-scm.com/download/win e rode de novo.");

        status("Verificando o Git... instalando...");
        Processos.Executar(tempInstalador,
            "/VERYSILENT /NORESTART /NOCANCEL /SP- /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS /COMPONENTS=\"icons,ext\\reg\\shellhere,assoc,assoc_sh\"",
            timeoutSegundos: 300);
        File.Delete(tempInstalador);

        // O instalador (Inno Setup) pode devolver o controle um instante antes de
        // terminar de gravar os arquivos em disco — dá uma folga curta antes de checar,
        // em vez de reportar falha por uma corrida de poucos milissegundos.
        for (var tentativa = 0; tentativa < 10 && !File.Exists(CaminhoGit); tentativa++)
            Thread.Sleep(500);

        if (!File.Exists(CaminhoGit))
            return new PassoResultado(false, "Nao foi possivel instalar o Git automaticamente. Instale manualmente em https://git-scm.com/download/win e rode de novo.");

        AdicionarAoPathDoProcesso(Path.GetDirectoryName(CaminhoGit)!);
        status("Verificando o Git... instalado.");
        return new PassoResultado(true);
    }

    // Pergunta pra API do GitHub qual é o asset "64-bit.exe" da última release do Git for
    // Windows — o nome muda a cada versão (ex: Git-2.55.0.4-64-bit.exe), então não dá pra
    // deixar fixo no código.
    private static string? ResolverUrlUltimoInstaladorGit()
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            // A API do GitHub exige um User-Agent identificável, senão recusa a requisição
            http.DefaultRequestHeaders.UserAgent.ParseAdd("PingoInstaller");
            var json = http.GetStringAsync("https://api.github.com/repos/git-for-windows/git/releases/latest").GetAwaiter().GetResult();

            using var doc = JsonDocument.Parse(json);
            foreach (var asset in doc.RootElement.GetProperty("assets").EnumerateArray())
            {
                var nome = asset.GetProperty("name").GetString() ?? "";
                if (nome.EndsWith("64-bit.exe", StringComparison.OrdinalIgnoreCase))
                    return asset.GetProperty("browser_download_url").GetString();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static void AdicionarAoPathDoProcesso(string pasta)
    {
        var pathAtual = Environment.GetEnvironmentVariable("PATH") ?? "";
        if (!pathAtual.Contains(pasta, StringComparison.OrdinalIgnoreCase))
            Environment.SetEnvironmentVariable("PATH", pathAtual + ";" + pasta);
    }

    // Usa o caminho absoluto conhecido quando o arquivo existe ali — não depende do PATH
    // do processo atual ter sido atualizado corretamente após uma instalação recém-feita
    // na mesma execução. Cai para o nome nu (resolvido via PATH normal) quando o Git/​.NET
    // já estava instalado antes em outro lugar (ex: instalação custom do usuário).
    private static string ComandoGit => File.Exists(CaminhoGit) ? CaminhoGit : "git";
    private static string ComandoDotnet => File.Exists(CaminhoDotnet) ? CaminhoDotnet : "dotnet";

    public static PassoResultado PararServicoSeExistir(Action<string> status)
    {
        var existe = Processos.Executar("sc", $"query \"{Config.NomeServico}\"", timeoutSegundos: 10).Sucesso;
        if (existe)
        {
            status("Parando o servico atual...");
            Processos.Executar("net", $"stop \"{Config.NomeServico}\"", timeoutSegundos: 30);
        }
        Directory.CreateDirectory(Config.PastaBase);
        return new PassoResultado(true);
    }

    // Reinstalar sempre apaga a pasta codigo/ (mesmo que o clone atual seja válido) e clona
    // do zero — para quando algo no clone local está corrompido de um jeito que o "git
    // rev-parse" de BaixarUltimaVersao não detecta. Só usado pelo fluxo de Reinstalar;
    // Atualizar nunca chega aqui.
    public static PassoResultado ForcarCloneLimpo(Action<string> status)
    {
        if (Directory.Exists(Config.PastaCodigo))
        {
            status("Apagando a copia local para reinstalar do zero...");
            Processos.ApagarPastaForcado(Config.PastaCodigo);
        }
        return new PassoResultado(true);
    }

    // Só busca até a última tag publicada, nunca o commit mais recente direto — mesma
    // trava de propósito do install.bat: sem ela, um commit ainda em teste viraria
    // produção em todas as lojas assim que alguém rodasse o instalador de novo.
    public static PassoResultado BaixarUltimaVersao(Action<string> status)
    {
        var cloneValido = Directory.Exists(Path.Combine(Config.PastaCodigo, ".git"))
            && Processos.Executar(ComandoGit, $"-C \"{Config.PastaCodigo}\" rev-parse --is-inside-work-tree", timeoutSegundos: 10).Sucesso;

        if (!cloneValido)
        {
            if (Directory.Exists(Config.PastaCodigo))
            {
                status("Encontrei uma copia incompleta de instalacao anterior, refazendo...");
                Processos.ApagarPastaForcado(Config.PastaCodigo);
            }

            status("Primeira instalacao: clonando o repositorio...");
            var clone = Processos.Executar(ComandoGit, $"clone --quiet \"{Config.RepoUrl}\" \"{Config.PastaCodigo}\"", timeoutSegundos: 180);
            if (!clone.Sucesso)
                return new PassoResultado(false, "Nao foi possivel baixar o sistema. Confira sua conexao com a internet.");
        }
        else
        {
            status("Buscando novidades no repositorio...");
            var fetch = Processos.Executar(ComandoGit, $"-C \"{Config.PastaCodigo}\" fetch --quiet --tags origin", timeoutSegundos: 60);
            if (!fetch.Sucesso)
                return new PassoResultado(false, "Nao foi possivel buscar atualizacoes. Confira sua conexao com a internet.");
        }

        var tags = Processos.Executar(ComandoGit, $"-C \"{Config.PastaCodigo}\" tag --sort=-creatordate", timeoutSegundos: 15);
        var ultimaTag = tags.Sucesso
            ? tags.Saida.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault()
            : null;

        if (!string.IsNullOrEmpty(ultimaTag))
        {
            status($"Instalando a versao {ultimaTag}...");
            Processos.Executar(ComandoGit, $"-C \"{Config.PastaCodigo}\" checkout --quiet \"{ultimaTag}\"", timeoutSegundos: 30);
        }
        else
        {
            status("Nenhuma versao marcada ainda; usando a mais recente do repositorio.");
            Processos.Executar(ComandoGit, $"-C \"{Config.PastaCodigo}\" checkout --quiet main", timeoutSegundos: 30);
            Processos.Executar(ComandoGit, $"-C \"{Config.PastaCodigo}\" pull --quiet origin main", timeoutSegundos: 60);
        }

        if (!File.Exists(Path.Combine(Config.PastaProjeto, "ListasCompras.csproj")))
            return new PassoResultado(false, "O repositorio baixado nao tem ListasCompras\\ListasCompras.csproj.");

        // Limpa obj/bin antes de publicar: evita "Access to the path is denied" quando
        // esses arquivos foram criados por outro usuário/contexto de permissão antes
        var pastaObj = Path.Combine(Config.PastaProjeto, "obj");
        var pastaBin = Path.Combine(Config.PastaProjeto, "bin");
        if (Directory.Exists(pastaObj)) Processos.ApagarPastaForcado(pastaObj);
        if (Directory.Exists(pastaBin)) Processos.ApagarPastaForcado(pastaBin);

        return new PassoResultado(true);
    }

    public static PassoResultado PublicarERegistrarServico(Action<string> status)
    {
        Directory.CreateDirectory(Config.PastaApp);

        string? backupDb = null;
        var dbAtual = Path.Combine(Config.PastaApp, "loja.db");
        if (File.Exists(dbAtual))
        {
            backupDb = Path.Combine(Path.GetTempPath(), "loja.db.bak");
            File.Copy(dbAtual, backupDb, overwrite: true);
        }

        status("Publicando o sistema...");
        var publish = Processos.Executar(ComandoDotnet,
            $"publish \"{Config.PastaProjeto}\" -c Release -o \"{Config.PastaApp}\" --nologo -v q",
            timeoutSegundos: 300);
        if (!publish.Sucesso)
            return new PassoResultado(false,
                "Falha ao publicar o sistema.\n" +
                "Se o erro falar em \"Access to the path is denied\", apague as pastas obj e bin em " +
                Config.PastaProjeto + " e rode o instalador de novo.\n" + publish.Erro);

        if (backupDb != null)
        {
            File.Copy(backupDb, dbAtual, overwrite: true);
            File.Delete(backupDb);
        }

        status("Registrando o servico do Windows...");
        var servicoExiste = Processos.Executar("sc", $"query \"{Config.NomeServico}\"", timeoutSegundos: 10).Sucesso;
        if (servicoExiste)
        {
            Processos.Executar("sc", $"config \"{Config.NomeServico}\" binPath= \"\\\"{Config.ExecutavelApp}\\\"\"", timeoutSegundos: 15);
        }
        else
        {
            Processos.Executar("sc",
                $"create \"{Config.NomeServico}\" binPath= \"\\\"{Config.ExecutavelApp}\\\"\" start= auto DisplayName= \"Pingo OS\"",
                timeoutSegundos: 15);
            Processos.Executar("sc",
                $"failure \"{Config.NomeServico}\" reset= 86400 actions= restart/5000/restart/5000/restart/5000",
                timeoutSegundos: 15);
        }

        Processos.Executar("net", $"start \"{Config.NomeServico}\"", timeoutSegundos: 30);

        status("Aguardando o sistema subir...");
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(1) };
        for (var tentativa = 0; tentativa < 20; tentativa++)
        {
            try
            {
                var resposta = http.GetAsync(Config.Url).GetAwaiter().GetResult();
                if (resposta.IsSuccessStatusCode || (int)resposta.StatusCode < 500) break;
            }
            catch { /* ainda subindo, tenta de novo */ }
            Thread.Sleep(1000);
        }

        return new PassoResultado(true);
    }

    // Para e remove o serviço do Windows, apaga o código clonado e o binário publicado.
    // O loja.db só é apagado se manterBanco == false — decisão separada da pessoa, porque
    // ali ficam clientes, vendas e ordens de serviço reais, diferente do resto (código e
    // binário, que voltam do zero num "Instalar" novo).
    public static PassoResultado Desinstalar(Action<string> status, bool manterBanco)
    {
        var servicoExiste = Processos.Executar("sc", $"query \"{Config.NomeServico}\"", timeoutSegundos: 10).Sucesso;
        if (servicoExiste)
        {
            status("Parando o servico...");
            Processos.Executar("net", $"stop \"{Config.NomeServico}\"", timeoutSegundos: 30);
            status("Removendo o servico do Windows...");
            Processos.Executar("sc", $"delete \"{Config.NomeServico}\"", timeoutSegundos: 15);

            // "net stop" retorna antes do processo do serviço liberar de fato o handle
            // do .exe — apagar a pasta app/ logo em seguida pode esbarrar em "arquivo em
            // uso". Uma folga curta evita essa corrida.
            Thread.Sleep(1000);
        }

        string? backupDb = null;
        var dbAtual = Path.Combine(Config.PastaApp, "loja.db");
        if (manterBanco && File.Exists(dbAtual))
        {
            backupDb = Path.Combine(Path.GetTempPath(), "loja.db.bak");
            File.Copy(dbAtual, backupDb, overwrite: true);
        }

        status("Apagando os arquivos...");
        if (Directory.Exists(Config.PastaCodigo)) Processos.ApagarPastaForcado(Config.PastaCodigo);
        if (Directory.Exists(Config.PastaApp)) Processos.ApagarPastaForcado(Config.PastaApp);

        if (backupDb != null)
        {
            Directory.CreateDirectory(Config.PastaBase);
            File.Copy(backupDb, Path.Combine(Config.PastaBase, "loja.db"), overwrite: true);
            File.Delete(backupDb);
        }
        else if (Directory.Exists(Config.PastaBase) && !Directory.EnumerateFileSystemEntries(Config.PastaBase).Any())
        {
            // Só remove a pasta base se ela ficou vazia — se o banco foi mantido, ele mora
            // ali agora (ver acima) e a pasta precisa continuar existindo.
            Directory.Delete(Config.PastaBase, recursive: true);
        }

        return new PassoResultado(true);
    }
}
