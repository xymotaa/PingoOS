using ListasCompras.Models;
using Microsoft.EntityFrameworkCore;

namespace ListasCompras.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Categoria> Categorias { get; set; }
    public DbSet<MarcaCelular> MarcasCelular { get; set; }
    public DbSet<ModeloCelular> ModelosCelular { get; set; }
    public DbSet<Produto> Produtos { get; set; }
    public DbSet<ListaCompra> ListasCompra { get; set; }
    public DbSet<ItemListaCompra> ItensListaCompra { get; set; }
    public DbSet<ConfiguracaoLoja> ConfiguracoesLoja { get; set; }
    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Cliente> Clientes { get; set; }
    public DbSet<OrdemServico> OrdensServico { get; set; }
    public DbSet<ItemOrdemServico> ItensOrdemServico { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // E-mail é o login: não pode repetir
        modelBuilder.Entity<Usuario>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // Endereço completo é montado em memória, não é coluna
        modelBuilder.Entity<Cliente>()
            .Ignore(c => c.EnderecoCompleto);

        modelBuilder.Entity<Cliente>()
            .HasIndex(c => c.Nome);

        // Totais são somados em memória, não são colunas
        modelBuilder.Entity<OrdemServico>().Ignore(o => o.Total).Ignore(o => o.DispositivoResumo);
        modelBuilder.Entity<ItemOrdemServico>().Ignore(i => i.Total);

        modelBuilder.Entity<OrdemServico>()
            .HasIndex(o => o.Numero)
            .IsUnique();

        modelBuilder.Entity<OrdemServico>()
            .Property(o => o.Situacao)
            .HasMaxLength(20);

        // Apagar um cliente não pode apagar o histórico de ordens dele
        modelBuilder.Entity<OrdemServico>()
            .HasOne(o => o.Cliente)
            .WithMany()
            .HasForeignKey(o => o.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        // Usuário é desativado, nunca excluído — mas se sumir, a OS continua existindo
        modelBuilder.Entity<OrdemServico>()
            .HasOne(o => o.Usuario)
            .WithMany()
            .HasForeignKey(o => o.UsuarioId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ItemOrdemServico>()
            .HasOne(i => i.OrdemServico)
            .WithMany(o => o.Itens)
            .HasForeignKey(i => i.OrdemServicoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}