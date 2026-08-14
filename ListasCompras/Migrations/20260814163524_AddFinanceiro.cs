using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ListasCompras.Migrations
{
    /// <inheritdoc />
    public partial class AddFinanceiro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LancamentosFinanceiros",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Data = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", nullable: false),
                    Categoria = table.Column<string>(type: "TEXT", nullable: true),
                    Valor = table.Column<decimal>(type: "TEXT", nullable: false),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: true),
                    DataCadastro = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LancamentosFinanceiros", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LancamentosFinanceiros_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PagamentosOrdemServico",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrdemServicoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Data = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Valor = table.Column<decimal>(type: "TEXT", nullable: false),
                    FormaPagamento = table.Column<string>(type: "TEXT", nullable: false),
                    Origem = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagamentosOrdemServico", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PagamentosOrdemServico_OrdensServico_OrdemServicoId",
                        column: x => x.OrdemServicoId,
                        principalTable: "OrdensServico",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContasAPagar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Descricao = table.Column<string>(type: "TEXT", nullable: false),
                    Valor = table.Column<decimal>(type: "TEXT", nullable: false),
                    Vencimento = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Paga = table.Column<bool>(type: "INTEGER", nullable: false),
                    DataPagamento = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LancamentoId = table.Column<int>(type: "INTEGER", nullable: true),
                    DataCadastro = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContasAPagar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContasAPagar_LancamentosFinanceiros_LancamentoId",
                        column: x => x.LancamentoId,
                        principalTable: "LancamentosFinanceiros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContasAPagar_LancamentoId",
                table: "ContasAPagar",
                column: "LancamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_LancamentosFinanceiros_UsuarioId",
                table: "LancamentosFinanceiros",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_PagamentosOrdemServico_OrdemServicoId",
                table: "PagamentosOrdemServico",
                column: "OrdemServicoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContasAPagar");

            migrationBuilder.DropTable(
                name: "PagamentosOrdemServico");

            migrationBuilder.DropTable(
                name: "LancamentosFinanceiros");
        }
    }
}
