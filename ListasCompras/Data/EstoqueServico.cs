using Microsoft.EntityFrameworkCore;
using ListasCompras.Models;

namespace ListasCompras.Data;

// Único lugar que altera saldo. Ajuste manual e venda passam por aqui, então o saldo
// nunca muda sem deixar registro no histórico.
public static class EstoqueServico
{
    public static MovimentacaoEstoque Movimentar(
        ProdutoEstoque produto, string tipo, int quantidade,
        string? motivo, int? usuarioId, Venda? venda = null)
    {
        if (quantidade <= 0) quantidade = 1;

        // Saldo pode ficar negativo: numa loja pequena a prateleira e o sistema divergem,
        // e travar a venda no balcão é pior que registrar o negativo como sinal disso.
        produto.SaldoAtual += tipo == TiposMovimentacao.Saida ? -quantidade : quantidade;

        var movimento = new MovimentacaoEstoque
        {
            ProdutoEstoque = produto,
            Tipo = tipo,
            Quantidade = quantidade,
            Motivo = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim(),
            SaldoResultante = produto.SaldoAtual,
            UsuarioId = usuarioId,
            Venda = venda,
        };

        produto.Movimentacoes.Add(movimento);
        return movimento;
    }

    // Devolve ao estoque tudo que uma venda tinha baixado — usado ao editar (antes de
    // relançar os itens novos) ou excluir uma venda. Nunca edita a MovimentacaoEstoque
    // original: lança uma entrada nova de estorno, preservando o rastro de que a saída
    // aconteceu (mesmo que a venda tenha sido desfeita depois).
    public static void EstornarItensVenda(AppDbContext context, Venda venda, string motivo, int? usuarioId)
    {
        foreach (var item in venda.Itens)
        {
            if (item.ProdutoEstoqueId == null) continue; // produto já foi excluído do estoque

            var produto = context.ProdutosEstoque
                .Include(p => p.Movimentacoes)
                .FirstOrDefault(p => p.Id == item.ProdutoEstoqueId);
            if (produto == null) continue;

            Movimentar(produto, TiposMovimentacao.Entrada, item.Quantidade, motivo, usuarioId, venda);
        }
    }

    // P-000001, P-000002... o anterior vinha do relógio no navegador
    public static string ProximoCodigo(AppDbContext context)
    {
        var maiorSalvo = context.ProdutosEstoque
            .Where(p => p.Codigo.StartsWith("P-"))
            .OrderByDescending(p => p.Id)
            .Select(p => p.Codigo)
            .FirstOrDefault();

        var sequencia = 0;
        if (maiorSalvo != null && int.TryParse(maiorSalvo[2..], out var n)) sequencia = n;

        // Cadastrar várias variações de um produto novo na mesma requisição gera vários
        // ProdutoEstoque no ChangeTracker antes de qualquer SaveChanges — sem considerar
        // esses códigos já reservados (mas ainda não persistidos), duas variações sem
        // código informado receberiam o mesmo "próximo código" e o SaveChanges falharia
        // com violação de unicidade.
        foreach (var entrada in context.ChangeTracker.Entries<ProdutoEstoque>())
        {
            if (entrada.State != EntityState.Added) continue;
            var codigo = entrada.Entity.Codigo;
            if (codigo != null && codigo.StartsWith("P-") && int.TryParse(codigo.AsSpan(2), out var pendente) && pendente > sequencia)
                sequencia = pendente;
        }

        return $"P-{sequencia + 1:D6}";
    }

    public static string ProximoNumeroVenda(AppDbContext context)
    {
        var ultimo = context.Vendas
            .OrderByDescending(v => v.Id)
            .Select(v => v.Numero)
            .FirstOrDefault();

        var sequencia = 0;
        if (ultimo != null && ultimo.StartsWith("V-") && int.TryParse(ultimo[2..], out var n))
            sequencia = n;

        return $"V-{sequencia + 1:D6}";
    }
}
