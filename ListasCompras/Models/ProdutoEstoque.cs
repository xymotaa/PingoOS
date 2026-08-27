namespace ListasCompras.Models;

// Item concreto de prateleira, com código e preço. Diferente do `Produto` da Lista de
// compras, que é genérico ("Capinha Silicone") e se combina com um modelo de celular.
public class ProdutoEstoque
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public int? CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }
    public string? Unidade { get; set; }
    public string? Imagem { get; set; } // nome do arquivo em wwwroot/uploads/produtos, sem caminho

    public int? ProdutoPaiId { get; set; }
    public ProdutoEstoque? ProdutoPai { get; set; }

    // Só relevante quando ProdutoPaiId != null. Texto livre digitado pelo usuário nesta
    // variação (ex: "Preta, 64GB"). Não é modelado como atributos estruturados (Cor/
    // Capacidade em colunas separadas) de propósito: o cadastro é manual, sem geração de
    // combinação — não há necessidade de filtrar/agrupar por atributo isoladamente.
    public string? DescricaoVariacao { get; set; }

    public ICollection<ProdutoEstoque> Variacoes { get; set; } = new List<ProdutoEstoque>();

    // "simples" | "variacao"
    public string Formato { get; set; } = TiposFormatoProduto.Simples;
    public string Tipo { get; set; } = TiposProduto.Produto; // "produto" | "servico"
    public string Condicao { get; set; } = CondicoesProduto.NaoEspecificado; // "nao_especificado" | "novo" | "usado"

    public string? Descricao { get; set; }
    public string? Marca { get; set; }
    public string? ModeloRef { get; set; }
    public string? Gtin { get; set; }
    public decimal? Peso { get; set; }
    public decimal? Largura { get; set; }
    public decimal? Altura { get; set; }
    public decimal? Profundidade { get; set; }
    public string? Localizacao { get; set; }

    // Tributação — só dado de referência por enquanto, o sistema não emite NF-e ainda
    public int OrigemFiscal { get; set; } // 0 Nacional | 1 Estrangeira (importação direta) | 2 Estrangeira (mercado interno)
    public string? Ncm { get; set; }
    public string? Cest { get; set; }
    public string? Cfop { get; set; }

    public int SaldoAtual { get; set; }
    public int EstoqueMinimo { get; set; }
    public int EstoqueMaximo { get; set; } // 0 = sem máximo definido
    public decimal CustoUnitario { get; set; }
    public decimal PrecoVenda { get; set; }

    public DateTime DataCadastro { get; set; } = DateTime.Now;

    public ICollection<MovimentacaoEstoque> Movimentacoes { get; set; } = new List<MovimentacaoEstoque>();
    public ICollection<ProdutoEstoqueModeloCompativel> ModelosCompativeis { get; set; } = new List<ProdutoEstoqueModeloCompativel>();

    public decimal ValorEmEstoque => SaldoAtual * CustoUnitario;

    public string Situacao =>
        SaldoAtual <= 0 ? "esgotado"
        : (EstoqueMinimo > 0 && SaldoAtual <= EstoqueMinimo ? "baixo" : "ok");

    // Um produto pai (Formato=variacao) não guarda saldo próprio — o número exibido é
    // sempre a soma das variações, calculado a partir da coleção já carregada, nunca
    // sincronizado em segundo plano (evita a classe de bug "saldo do pai desatualizado").
    public int SaldoAtualExibido =>
        Formato == TiposFormatoProduto.ComVariacao ? Variacoes.Sum(v => v.SaldoAtual) : SaldoAtual;

    public decimal ValorEmEstoqueExibido =>
        Formato == TiposFormatoProduto.ComVariacao ? Variacoes.Sum(v => v.ValorEmEstoque) : ValorEmEstoque;

    public string SituacaoExibida
    {
        get
        {
            if (Formato != TiposFormatoProduto.ComVariacao) return Situacao;
            if (Variacoes.Count == 0) return "esgotado";
            var saldo = SaldoAtualExibido;
            var minimoAgregado = Variacoes.Sum(v => v.EstoqueMinimo);
            return saldo <= 0 ? "esgotado" : (minimoAgregado > 0 && saldo <= minimoAgregado ? "baixo" : "ok");
        }
    }
}

// Vínculo explícito "este produto atende este modelo de celular" — ex: uma película
// específica da Galaxy A12. Opcional: produto genérico (cabo USB-C) não tem vínculo
// nenhum. Usado hoje só no cadastro; sugerir substituto no Caixa quando o produto
// principal está esgotado é integração futura sobre esta mesma tabela.
public class ProdutoEstoqueModeloCompativel
{
    public int ProdutoEstoqueId { get; set; }
    public ProdutoEstoque ProdutoEstoque { get; set; } = null!;
    public int ModeloCelularId { get; set; }
    public ModeloCelular ModeloCelular { get; set; } = null!;
}

// Todo ajuste de saldo passa por aqui: o saldo é consequência do histórico, não um
// número que alguém digita.
public class MovimentacaoEstoque
{
    public int Id { get; set; }

    public int ProdutoEstoqueId { get; set; }
    public ProdutoEstoque ProdutoEstoque { get; set; } = null!;

    public string Tipo { get; set; } = TiposMovimentacao.Entrada; // entrada | saida
    public int Quantidade { get; set; }
    public string? Motivo { get; set; }

    // Saldo depois do movimento, para o histórico não depender de recalcular tudo
    public int SaldoResultante { get; set; }

    public DateTime Data { get; set; } = DateTime.Now;

    public int? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    // Preenchido quando o movimento veio de uma venda no Caixa
    public int? VendaId { get; set; }
    public Venda? Venda { get; set; }
}

public static class TiposMovimentacao
{
    public const string Entrada = "entrada";
    public const string Saida = "saida";
}

public static class TiposFormatoProduto
{
    public const string Simples = "simples";
    public const string ComVariacao = "variacao";
}

public static class TiposProduto
{
    public const string Produto = "produto";
    public const string Servico = "servico";
}

public static class CondicoesProduto
{
    public const string NaoEspecificado = "nao_especificado";
    public const string Novo = "novo";
    public const string Usado = "usado";
}
