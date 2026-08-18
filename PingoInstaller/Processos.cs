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
}
