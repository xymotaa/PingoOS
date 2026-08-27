using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ListasCompras.Migrations
{
    /// <inheritdoc />
    public partial class AddCamposDetalhadosEmProdutoEstoque : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Altura",
                table: "ProdutosEstoque",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cest",
                table: "ProdutosEstoque",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cfop",
                table: "ProdutosEstoque",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Condicao",
                table: "ProdutosEstoque",
                type: "TEXT",
                nullable: false,
                defaultValue: "nao_especificado");

            migrationBuilder.AddColumn<string>(
                name: "Descricao",
                table: "ProdutosEstoque",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Formato",
                table: "ProdutosEstoque",
                type: "TEXT",
                nullable: false,
                defaultValue: "simples");

            migrationBuilder.AddColumn<string>(
                name: "Gtin",
                table: "ProdutosEstoque",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Largura",
                table: "ProdutosEstoque",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Localizacao",
                table: "ProdutosEstoque",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Marca",
                table: "ProdutosEstoque",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModeloRef",
                table: "ProdutosEstoque",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ncm",
                table: "ProdutosEstoque",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrigemFiscal",
                table: "ProdutosEstoque",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "Peso",
                table: "ProdutosEstoque",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Profundidade",
                table: "ProdutosEstoque",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tipo",
                table: "ProdutosEstoque",
                type: "TEXT",
                nullable: false,
                defaultValue: "produto");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Altura",
                table: "ProdutosEstoque");

            migrationBuilder.DropColumn(
                name: "Cest",
                table: "ProdutosEstoque");

            migrationBuilder.DropColumn(
                name: "Cfop",
                table: "ProdutosEstoque");

            migrationBuilder.DropColumn(
                name: "Condicao",
                table: "ProdutosEstoque");

            migrationBuilder.DropColumn(
                name: "Descricao",
                table: "ProdutosEstoque");

            migrationBuilder.DropColumn(
                name: "Formato",
                table: "ProdutosEstoque");

            migrationBuilder.DropColumn(
                name: "Gtin",
                table: "ProdutosEstoque");

            migrationBuilder.DropColumn(
                name: "Largura",
                table: "ProdutosEstoque");

            migrationBuilder.DropColumn(
                name: "Localizacao",
                table: "ProdutosEstoque");

            migrationBuilder.DropColumn(
                name: "Marca",
                table: "ProdutosEstoque");

            migrationBuilder.DropColumn(
                name: "ModeloRef",
                table: "ProdutosEstoque");

            migrationBuilder.DropColumn(
                name: "Ncm",
                table: "ProdutosEstoque");

            migrationBuilder.DropColumn(
                name: "OrigemFiscal",
                table: "ProdutosEstoque");

            migrationBuilder.DropColumn(
                name: "Peso",
                table: "ProdutosEstoque");

            migrationBuilder.DropColumn(
                name: "Profundidade",
                table: "ProdutosEstoque");

            migrationBuilder.DropColumn(
                name: "Tipo",
                table: "ProdutosEstoque");
        }
    }
}
