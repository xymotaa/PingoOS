namespace PingoInstaller;

// Mesmos caminhos e convenções do install.bat — mantidos idênticos de propósito, para
// quem já instalou pelo .bat continuar sendo reconhecido por este instalador (e
// vice-versa): mesma pasta, mesmo nome de serviço, mesma porta.
static class Config
{
    public const string RepoUrl = "https://github.com/xymotaa/PingoOS.git";
    public const int Porta = 5096;
    public const string NomeServico = "PingoOS";

    public static string PastaBase => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PingoOS");
    public static string PastaCodigo => Path.Combine(PastaBase, "codigo");
    public static string PastaApp => Path.Combine(PastaBase, "app");
    public static string PastaProjeto => Path.Combine(PastaCodigo, "ListasCompras");
    public static string ExecutavelApp => Path.Combine(PastaApp, "ListasCompras.exe");
    public static string Url => $"http://localhost:{Porta}";

    // Instalação já existe na máquina quando o clone válido está presente — usado pra
    // decidir se pergunta "Instalar? y/n" ou pula direto pro menu de já instalado.
    public static bool JaInstalado() =>
        Directory.Exists(Path.Combine(PastaCodigo, ".git")) && File.Exists(ExecutavelApp);
}
