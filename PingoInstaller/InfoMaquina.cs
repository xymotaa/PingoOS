using System.Net;
using System.Net.Sockets;

namespace PingoInstaller;

static class InfoMaquina
{
    public static string ObterIpLocal()
    {
        // UDP connect não abre conexão de verdade (não há handshake), então normalmente é
        // instantâneo — mas roda numa tarefa com timeout curto por segurança: numa rede
        // sem rota de saída configurada, alguma pilha de rede pode travar por alguns
        // segundos, e isso não pode atrasar a tela de boas-vindas do instalador.
        var tarefa = Task.Run(() =>
        {
            try
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.Connect("8.8.8.8", 65530);
                return (socket.LocalEndPoint as IPEndPoint)?.Address.ToString() ?? "indisponivel";
            }
            catch
            {
                return "indisponivel";
            }
        });

        return tarefa.Wait(TimeSpan.FromMilliseconds(500)) ? tarefa.Result : "indisponivel";
    }

    public static string ObterUsuario() => Environment.UserName;

    public static string ObterDataHora() => DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

    public static string ObterMaquina() => Environment.MachineName;
}
