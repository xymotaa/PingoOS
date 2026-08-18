using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ListasCompras.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoriaEmProdutoEstoque : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CategoriaId",
                table: "ProdutosEstoque",
                type: "INTEGER",
                nullable: true);

            // A coluna "Categoria" era texto livre (ex: "Capinha", "Cabo"). Antes de
            // derrubá-la, cria (ou reaproveita) uma linha em Categorias para cada valor
            // distinto já usado, e aponta CategoriaId para lá — senão perde a categorização
            // de quem já tinha produtos cadastrados.
            migrationBuilder.Sql(@"
                INSERT INTO Categorias (Nome, RequerModelo)
                SELECT DISTINCT trim(pe.Categoria), 0
                FROM ProdutosEstoque pe
                WHERE pe.Categoria IS NOT NULL AND trim(pe.Categoria) <> ''
                  AND NOT EXISTS (
                      SELECT 1 FROM Categorias c WHERE lower(c.Nome) = lower(trim(pe.Categoria))
                  );

                UPDATE ProdutosEstoque
                SET CategoriaId = (
                    SELECT c.Id FROM Categorias c WHERE lower(c.Nome) = lower(trim(ProdutosEstoque.Categoria))
                )
                WHERE Categoria IS NOT NULL AND trim(Categoria) <> '';
            ");

            migrationBuilder.DropColumn(
                name: "Categoria",
                table: "ProdutosEstoque");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosEstoque_CategoriaId",
                table: "ProdutosEstoque",
                column: "CategoriaId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProdutosEstoque_Categorias_CategoriaId",
                table: "ProdutosEstoque",
                column: "CategoriaId",
                principalTable: "Categorias",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProdutosEstoque_Categorias_CategoriaId",
                table: "ProdutosEstoque");

            migrationBuilder.DropIndex(
                name: "IX_ProdutosEstoque_CategoriaId",
                table: "ProdutosEstoque");

            migrationBuilder.DropColumn(
                name: "CategoriaId",
                table: "ProdutosEstoque");

            migrationBuilder.AddColumn<string>(
                name: "Categoria",
                table: "ProdutosEstoque",
                type: "TEXT",
                nullable: true);
        }
    }
}
