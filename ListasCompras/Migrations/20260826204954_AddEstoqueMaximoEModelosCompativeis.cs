using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ListasCompras.Migrations
{
    /// <inheritdoc />
    public partial class AddEstoqueMaximoEModelosCompativeis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EstoqueMaximo",
                table: "ProdutosEstoque",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ProdutoEstoqueModeloCompativeis",
                columns: table => new
                {
                    ProdutoEstoqueId = table.Column<int>(type: "INTEGER", nullable: false),
                    ModeloCelularId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProdutoEstoqueModeloCompativeis", x => new { x.ProdutoEstoqueId, x.ModeloCelularId });
                    table.ForeignKey(
                        name: "FK_ProdutoEstoqueModeloCompativeis_ModelosCelular_ModeloCelularId",
                        column: x => x.ModeloCelularId,
                        principalTable: "ModelosCelular",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProdutoEstoqueModeloCompativeis_ProdutosEstoque_ProdutoEstoqueId",
                        column: x => x.ProdutoEstoqueId,
                        principalTable: "ProdutosEstoque",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProdutoEstoqueModeloCompativeis_ModeloCelularId",
                table: "ProdutoEstoqueModeloCompativeis",
                column: "ModeloCelularId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProdutoEstoqueModeloCompativeis");

            migrationBuilder.DropColumn(
                name: "EstoqueMaximo",
                table: "ProdutosEstoque");
        }
    }
}
