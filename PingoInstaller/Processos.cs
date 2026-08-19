using System.Diagnostics;

namespace PingoInstaller;

record ResultadoProcesso(int CodigoSaida, string Saida, string Erro)
{
    public bool Sucesso => CodigoSaida == 0;
}

// Wrapper fino sobre Process.Start — todo comando externo (dotnet, git, sc, winget,
// powershell) passa por aqui, sempre sem janela própria e com saída capturada, para o
// instalador decidir o que mostrar em vez de deixar o console do subprocesso vazar.
static class Processos
{
    public static ResultadoProcesso Executar(string arquivo, string argumentos, int timeoutSegundos = 120)
    {
        var info = new ProcessStartInfo
        {
            FileName = arquivo,
            Arguments = argumentos,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        try
        {
            using var processo = Process.Start(info);
            if (processo == null) return new ResultadoProcesso(-1, "", "Não foi possível iniciar o processo.");

            var saidaTask = processo.StandardOutput.ReadToEndAsync();
            var erroTask = processo.StandardError.ReadToEndAsync();
            var terminou = processo.WaitForExit(timeoutSegundos * 1000);

            if (!terminou)
            {
                try { processo.Kill(entireProcessTree: true); } catch { /* já pode ter terminado entre a checagem e o Kill */ }
                return new ResultadoProcesso(-1, "", "Tempo limite excedido.");
            }

            return new ResultadoProcesso(processo.ExitCode, saidaTask.Result, erroTask.Result);
        }
        catch (Exception ex)
        {
            return new ResultadoProcesso(-1, "", ex.Message);
        }
    }

    public static bool ComandoExiste(string comando)
    {
        var resultado = Executar("where", comando, timeoutSegundos: 10);
        return resultado.Sucesso;
    }

    public static void AbrirNoNavegador(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch { /* sem navegador padrão configurado — não é motivo pra falhar a instalação */ }
    }

    // Download em C# puro (não via PowerShell/Invoke-WebRequest) para poder reportar
    // progresso em tempo real — o Invoke-WebRequest só devolve controle quando termina,
    // então a TUI ficava travada em "Baixando..." sem nenhum feedback do quanto faltava.
    public static bool BaixarArquivo(string url, string destino, Action<double> progresso, int timeoutSegundos = 300)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(timeoutSegundos) };
            using var resposta = http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult();
            resposta.EnsureSuccessStatusCode();

            var total = resposta.Content.Headers.ContentLength;
            using var stream = resposta.Content.ReadAsStream();
            using var arquivo = File.Create(destino);

            var buffer = new byte[81920];
            long lidos = 0;
            int n;
            while ((n = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                arquivo.Write(buffer, 0, n);
                lidos += n;
                if (total is > 0)
                    progresso(Math.Min(100.0, lidos * 100.0 / total.Value));
            }

            if (total is null or 0) progresso(100);
            return true;
        }
        catch
        {
            try { if (File.Exists(destino)) File.Delete(destino); } catch { /* melhor esforço */ }
            return false;
        }
    }
}
