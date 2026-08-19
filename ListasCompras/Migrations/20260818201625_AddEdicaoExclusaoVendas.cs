using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ListasCompras.Migrations
{
    /// <inheritdoc />
    public partial class AddEdicaoExclusaoVendas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DataExclusao",
                table: "Vendas",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Excluida",
                table: "Vendas",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ExcluidaPorId",
                table: "Vendas",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "HistoricoVendas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VendaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", nullable: true),
                    Data = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricoVendas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistoricoVendas_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_HistoricoVendas_Vendas_VendaId",
                        column: x => x.VendaId,
                        principalTable: "Vendas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vendas_ExcluidaPorId",
                table: "Vendas",
                column: "ExcluidaPorId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoVendas_UsuarioId",
                table: "HistoricoVendas",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoVendas_VendaId",
                table: "HistoricoVendas",
                column: "VendaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Vendas_Usuarios_ExcluidaPorId",
                table: "Vendas",
                column: "ExcluidaPorId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vendas_Usuarios_ExcluidaPorId",
                table: "Vendas");

            migrationBuilder.DropTable(
                name: "HistoricoVendas");

            migrationBuilder.DropIndex(
                name: "IX_Vendas_ExcluidaPorId",
                table: "Vendas");

            migrationBuilder.DropColumn(
                name: "DataExclusao",
                table: "Vendas");

            migrationBuilder.DropColumn(
                name: "Excluida",
                table: "Vendas");

            migrationBuilder.DropColumn(
                name: "ExcluidaPorId",
                table: "Vendas");
        }
    }
}
