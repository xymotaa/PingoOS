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
    public DbSet<AparelhoOs> AparelhosOs { get; set; }
    public DbSet<ProdutoEstoque> ProdutosEstoque { get; set; }
    public DbSet<MovimentacaoEstoque> MovimentacoesEstoque { get; set; }
    public DbSet<Venda> Vendas { get; set; }
    public DbSet<ItemVenda> ItensVenda { get; set; }

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
        modelBuilder.Entity<OrdemServico>()
            .Ignore(o => o.Subtotal).Ignore(o => o.DescontoEmReais).Ignore(o => o.Total)
            .Ignore(o => o.SaldoAPagar).Ignore(o => o.QuantidadeParcelas).Ignore(o => o.ValorParcela)
            .Ignore(o => o.AparelhosResumo)
            .Ignore(o => o.GarantiaInicio).Ignore(o => o.GarantiaFim).Ignore(o => o.GarantiaIniciada)
            .Ignore(o => o.GarantiaVigente).Ignore(o => o.DiasDeGarantiaRestantes)
            .Ignore(o => o.SituacaoGarantia);

        // Retorno em garantia aponta para a ordem original; excluir a original não
        // pode apagar o retorno, que é o registro de que o problema voltou
        modelBuilder.Entity<OrdemServico>()
            .HasOne(o => o.OrdemOrigem)
            .WithMany()
            .HasForeignKey(o => o.OrdemOrigemId)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<AparelhoOs>().Ignore(a => a.Resumo);

        modelBuilder.Entity<AparelhoOs>()
            .HasOne(a => a.OrdemServico)
            .WithMany(o => o.Aparelhos)
            .HasForeignKey(a => a.OrdemServicoId)
            .OnDelete(DeleteBehavior.Cascade);
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

        // ===== Estoque e Caixa =====

        modelBuilder.Entity<ProdutoEstoque>().Ignore(p => p.ValorEmEstoque).Ignore(p => p.Situacao);
        modelBuilder.Entity<Venda>()
            .Ignore(v => v.Subtotal).Ignore(v => v.Desconto).Ignore(v => v.Total)
            .Ignore(v => v.Troco).Ignore(v => v.QuantidadeItens);
        modelBuilder.Entity<ItemVenda>().Ignore(i => i.DescontoTotal).Ignore(i => i.Total);

        modelBuilder.Entity<ProdutoEstoque>().HasIndex(p => p.Codigo).IsUnique();
        modelBuilder.Entity<Venda>().HasIndex(v => v.Numero).IsUnique();

        // Excluir um produto não pode apagar o histórico de vendas: o item guarda
        // código, descrição e preço praticado, então a venda continua legível
        modelBuilder.Entity<ItemVenda>()
            .HasOne(i => i.ProdutoEstoque)
            .WithMany()
            .HasForeignKey(i => i.ProdutoEstoqueId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ItemVenda>()
            .HasOne(i => i.Venda)
            .WithMany(v => v.Itens)
            .HasForeignKey(i => i.VendaId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MovimentacaoEstoque>()
            .HasOne(m => m.ProdutoEstoque)
            .WithMany(p => p.Movimentacoes)
            .HasForeignKey(m => m.ProdutoEstoqueId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MovimentacaoEstoque>()
            .HasOne(m => m.Venda)
            .WithMany()
            .HasForeignKey(m => m.VendaId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<MovimentacaoEstoque>()
            .HasOne(m => m.Usuario).WithMany().HasForeignKey(m => m.UsuarioId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Venda>()
            .HasOne(v => v.Usuario).WithMany().HasForeignKey(v => v.UsuarioId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}