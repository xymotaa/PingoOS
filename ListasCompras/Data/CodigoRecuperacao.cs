using System.Security.Cryptography;
using System.Text;

namespace ListasCompras.Data;

// Código que a pessoa anota no papel para redefinir a própria senha sem depender de e-mail.
public static class CodigoRecuperacao
{
    // Sem 0/O e 1/I/L: são os pares que a pessoa erra ao copiar de um papel
    private const string Alfabeto = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
    private const int Grupos = 4;
    private const int PorGrupo = 4;

    // Ex: K7HM-3XQP-9RTW-2FVN — 16 caracteres, cerca de 79 bits
    public static string Gerar()
    {
        var texto = new StringBuilder();
        for (var g = 0; g < Grupos; g++)
        {
            if (g > 0) texto.Append('-');
            for (var c = 0; c < PorGrupo; c++)
            {
                texto.Append(Alfabeto[RandomNumberGenerator.GetInt32(Alfabeto.Length)]);
            }
        }
        return texto.ToString();
    }

    // Aceita o código digitado com ou sem hífen, em maiúscula ou minúscula
    public static string Normalizar(string? codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo)) return "";
        return new string(codigo.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
    }
}
