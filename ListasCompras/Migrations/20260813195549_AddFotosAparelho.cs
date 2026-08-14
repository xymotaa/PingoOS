using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ListasCompras.Migrations
{
    /// <inheritdoc />
    public partial class AddFotosAparelho : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FotosAparelho",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AparelhoOsId = table.Column<int>(type: "INTEGER", nullable: false),
                    Arquivo = table.Column<string>(type: "TEXT", nullable: false),
                    DataEnvio = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FotosAparelho", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FotosAparelho_AparelhosOs_AparelhoOsId",
                        column: x => x.AparelhoOsId,
                        principalTable: "AparelhosOs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FotosAparelho_AparelhoOsId",
                table: "FotosAparelho",
                column: "AparelhoOsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FotosAparelho");
        }
    }
}
