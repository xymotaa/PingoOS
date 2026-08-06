namespace ListasCompras.Models;

public class ModeloCelular
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty; // Galaxy A12, iPhone 13...

    public int MarcaCelularId { get; set; }
    public MarcaCelular MarcaCelular { get; set; } = null!;
}