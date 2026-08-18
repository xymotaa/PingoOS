using System.Net;
using System.Net.Sockets;

namespace PingoInstaller;

static class InfoMaquina
{
    public static string ObterIpLocal()
    {
        try
        {
            // Não abre conexão de verdade (UDP connect é só resolução de rota) — pega o IP
            // da interface que o sistema usaria para sair à rede, sem depender de internet.
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Connect("8.8.8.8", 65530);
            return (socket.LocalEndPoint as IPEndPoint)?.Address.ToString() ?? "indisponível";
        }
        catch
        {
            return "indisponível";
        }
    }

    public static string ObterUsuario() => Environment.UserName;

    public static string ObterDataHora() => DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

    public static string ObterMaquina() => Environment.MachineName;
}
