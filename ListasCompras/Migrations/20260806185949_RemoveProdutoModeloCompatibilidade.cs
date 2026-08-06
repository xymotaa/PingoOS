using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ListasCompras.Migrations
{
    /// <inheritdoc />
    public partial class RemoveProdutoModeloCompatibilidade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProdutoModeloCompatibilidades");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProdutoModeloCompatibilidades",
                columns: table => new
                {
                    ProdutoId = table.Column<int>(type: "INTEGER", nullable: false),
                    ModeloCelularId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProdutoModeloCompatibilidades", x => new { x.ProdutoId, x.ModeloCelularId });
                    table.ForeignKey(
                        name: "FK_ProdutoModeloCompatibilidades_ModelosCelular_ModeloCelularId",
                        column: x => x.ModeloCelularId,
                        principalTable: "ModelosCelular",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProdutoModeloCompatibilidades_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProdutoModeloCompatibilidades_ModeloCelularId",
                table: "ProdutoModeloCompatibilidades",
                column: "ModeloCelularId");
        }
    }
}
