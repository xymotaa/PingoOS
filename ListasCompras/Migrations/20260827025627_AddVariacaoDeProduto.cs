using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ListasCompras.Migrations
{
    /// <inheritdoc />
    public partial class AddVariacaoDeProduto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DescricaoVariacao",
                table: "ProdutosEstoque",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProdutoPaiId",
                table: "ProdutosEstoque",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosEstoque_ProdutoPaiId",
                table: "ProdutosEstoque",
                column: "ProdutoPaiId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProdutosEstoque_ProdutosEstoque_ProdutoPaiId",
                table: "ProdutosEstoque",
                column: "ProdutoPaiId",
                principalTable: "ProdutosEstoque",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProdutosEstoque_ProdutosEstoque_ProdutoPaiId",
                table: "ProdutosEstoque");

            migrationBuilder.DropIndex(
                name: "IX_ProdutosEstoque_ProdutoPaiId",
                table: "ProdutosEstoque");

            migrationBuilder.DropColumn(
                name: "DescricaoVariacao",
                table: "ProdutosEstoque");

            migrationBuilder.DropColumn(
                name: "ProdutoPaiId",
                table: "ProdutosEstoque");
        }
    }
}
