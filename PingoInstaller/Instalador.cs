namespace PingoInstaller;

record PassoResultado(bool Sucesso, string? MensagemErro = null);

// Mesma sequência do install.bat, linha por linha — só a forma de mostrar progresso
// muda (aqui atualiza uma linha de status na TUI em vez de imprimir echo). Qualquer
// mudança de comportamento de instalação deve ficar igual nos dois enquanto ambos
// existirem; ver ROADMAP.md sobre a descontinuação do .bat quando este for validado.
static class Instalador
{
    public static PassoResultado GarantirDotnet(Action<string> status)
    {
        status("Verificando o .NET...");
        if (Processos.ComandoExiste("dotnet"))
        {
            status("Verificando o .NET... ja instalado.");
            return new PassoResultado(true);
        }

        status("Verificando o .NET... nao encontrado, instalando (pode levar alguns minutos)...");
        var tempScript = Path.Combine(Path.GetTempPath(), "dotnet-install.ps1");
        var download = Processos.Executar("powershell",
            $"-NoProfile -Command \"Invoke-WebRequest -Uri https://dot.net/v1/dotnet-install.ps1 -OutFile '{tempScript}'\"",
            timeoutSegundos: 60);
        if (!download.Sucesso)
            return new PassoResultado(false, "Nao foi possivel baixar o instalador do .NET. Confira sua conexao com a internet.");

        var instalarDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet");
        var instalacao = Processos.Executar("powershell",
            $"-NoProfile -ExecutionPolicy Bypass -File \"{tempScript}\" -Channel 10.0 -InstallDir \"{instalarDir}\"",
            timeoutSegundos: 600);
        File.Delete(tempScript);
        if (!instalacao.Sucesso)
            return new PassoResultado(false, "Falha ao instalar o .NET: " + instalacao.Erro);

        // Path do processo atual não vê a variável de ambiente de máquina recém-gravada.
        // setx não expande %PATH% quando chamado direto via Process.Start (isso só
        // acontece dentro de um cmd.exe interpretando um .bat) — por isso lemos o valor
        // atual da variável de máquina e escrevemos o novo PATH por extenso.
        Environment.SetEnvironmentVariable("PATH",
            Environment.GetEnvironmentVariable("PATH") + ";" + instalarDir);
        var pathMaquina = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine) ?? "";
        if (!pathMaquina.Contains(instalarDir, StringComparison.OrdinalIgnoreCase))
        {
            Processos.Executar("setx", $"PATH \"{pathMaquina};{instalarDir}\" /M", timeoutSegundos: 15);
        }

        status("Verificando o .NET... instalado.");
        return new PassoResultado(true);
    }

    public static PassoResultado GarantirGit(Action<string> status)
    {
        status("Verificando o Git...");
        if (Processos.ComandoExiste("git"))
        {
            status("Verificando o Git... ja instalado.");
            return new PassoResultado(true);
        }

        status("Verificando o Git... nao encontrado, instalando via winget...");
        if (Processos.ComandoExiste("winget"))
        {
            Processos.Executar("winget",
                "install --id Git.Git -e --source winget --accept-package-agreements --accept-source-agreements --silent",
                timeoutSegundos: 300);
        }

        if (Processos.ComandoExiste("git")) { status("Verificando o Git... instalado."); return new PassoResultado(true); }

        status("Verificando o Git... winget indisponivel, baixando o instalador oficial...");
        var tempInstalador = Path.Combine(Path.GetTempPath(), "git-installer.exe");
        var download = Processos.Executar("powershell",
            $"-NoProfile -Command \"Invoke-WebRequest -Uri https://github.com/git-for-windows/git/releases/latest/download/Git-64-bit.exe -OutFile '{tempInstalador}'\"",
            timeoutSegundos: 120);
        if (!download.Sucesso)
            return new PassoResultado(false, "Nao foi possivel baixar o instalador do Git. Instale manualmente em https://git-scm.com/download/win e rode de novo.");

        Processos.Executar(tempInstalador,
            "/VERYSILENT /NORESTART /NOCANCEL /SP- /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS /COMPONENTS=\"icons,ext\\reg\\shellhere,assoc,assoc_sh\"",
            timeoutSegundos: 300);
        File.Delete(tempInstalador);

        var pastaGit = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Git", "cmd");
        Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + ";" + pastaGit);

        if (!Processos.ComandoExiste("git"))
            return new PassoResultado(false, "Nao foi possivel instalar o Git automaticamente. Instale manualmente em https://git-scm.com/download/win e rode de novo.");

        status("Verificando o Git... instalado.");
        return new PassoResultado(true);
    }

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

    // Só busca até a última tag publicada, nunca o commit mais recente direto — mesma
    // trava de propósito do install.bat: sem ela, um commit ainda em teste viraria
    // produção em todas as lojas assim que alguém rodasse o instalador de novo.
    public static PassoResultado BaixarUltimaVersao(Action<string> status)
    {
        var cloneValido = Directory.Exists(Path.Combine(Config.PastaCodigo, ".git"))
            && Processos.Executar("git", $"-C \"{Config.PastaCodigo}\" rev-parse --is-inside-work-tree", timeoutSegundos: 10).Sucesso;

        if (!cloneValido)
        {
            if (Directory.Exists(Config.PastaCodigo))
            {
                status("Encontrei uma copia incompleta de instalacao anterior, refazendo...");
                Directory.Delete(Config.PastaCodigo, recursive: true);
            }

            status("Primeira instalacao: clonando o repositorio...");
            var clone = Processos.Executar("git", $"clone --quiet \"{Config.RepoUrl}\" \"{Config.PastaCodigo}\"", timeoutSegundos: 180);
            if (!clone.Sucesso)
                return new PassoResultado(false, "Nao foi possivel baixar o sistema. Confira sua conexao com a internet.");
        }
        else
        {
            status("Buscando novidades no repositorio...");
            var fetch = Processos.Executar("git", $"-C \"{Config.PastaCodigo}\" fetch --quiet --tags origin", timeoutSegundos: 60);
            if (!fetch.Sucesso)
                return new PassoResultado(false, "Nao foi possivel buscar atualizacoes. Confira sua conexao com a internet.");
        }

        var tags = Processos.Executar("git", $"-C \"{Config.PastaCodigo}\" tag --sort=-creatordate", timeoutSegundos: 15);
        var ultimaTag = tags.Sucesso
            ? tags.Saida.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault()
            : null;

        if (!string.IsNullOrEmpty(ultimaTag))
        {
            status($"Instalando a versao {ultimaTag}...");
            Processos.Executar("git", $"-C \"{Config.PastaCodigo}\" checkout --quiet \"{ultimaTag}\"", timeoutSegundos: 30);
        }
        else
        {
            status("Nenhuma versao marcada ainda; usando a mais recente do repositorio.");
            Processos.Executar("git", $"-C \"{Config.PastaCodigo}\" checkout --quiet main", timeoutSegundos: 30);
            Processos.Executar("git", $"-C \"{Config.PastaCodigo}\" pull --quiet origin main", timeoutSegundos: 60);
        }

        if (!File.Exists(Path.Combine(Config.PastaProjeto, "ListasCompras.csproj")))
            return new PassoResultado(false, "O repositorio baixado nao tem ListasCompras\\ListasCompras.csproj.");

        // Limpa obj/bin antes de publicar: evita "Access to the path is denied" quando
        // esses arquivos foram criados por outro usuário/contexto de permissão antes
        var pastaObj = Path.Combine(Config.PastaProjeto, "obj");
        var pastaBin = Path.Combine(Config.PastaProjeto, "bin");
        if (Directory.Exists(pastaObj)) Directory.Delete(pastaObj, recursive: true);
        if (Directory.Exists(pastaBin)) Directory.Delete(pastaBin, recursive: true);

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
        var publish = Processos.Executar("dotnet",
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
}
