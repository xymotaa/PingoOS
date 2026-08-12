using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ListasCompras.Migrations
{
    /// <inheritdoc />
    public partial class AddGarantiaOs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrdemOrigemId",
                table: "OrdensServico",
                type: "INTEGER",
                nullable: true);

            // 90 e não 0: o padrão do tipo deixaria as ordens já existentes sem garantia
            // nenhuma, contrariando o que a OS delas prometeu impresso
            migrationBuilder.AddColumn<int>(
                name: "PrazoGarantiaDias",
                table: "OrdensServico",
                type: "INTEGER",
                nullable: false,
                defaultValue: 90);

            migrationBuilder.Sql("UPDATE OrdensServico SET PrazoGarantiaDias = 90 WHERE PrazoGarantiaDias <= 0;");

            migrationBuilder.CreateIndex(
                name: "IX_OrdensServico_OrdemOrigemId",
                table: "OrdensServico",
                column: "OrdemOrigemId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrdensServico_OrdensServico_OrdemOrigemId",
                table: "OrdensServico",
                column: "OrdemOrigemId",
                principalTable: "OrdensServico",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrdensServico_OrdensServico_OrdemOrigemId",
                table: "OrdensServico");

            migrationBuilder.DropIndex(
                name: "IX_OrdensServico_OrdemOrigemId",
                table: "OrdensServico");

            migrationBuilder.DropColumn(
                name: "OrdemOrigemId",
                table: "OrdensServico");

            migrationBuilder.DropColumn(
                name: "PrazoGarantiaDias",
                table: "OrdensServico");
        }
    }
}
