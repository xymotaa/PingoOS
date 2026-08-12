using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ListasCompras.Migrations
{
    /// <inheritdoc />
    public partial class SepararOrcamentoDeOrdemServico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrcamentoOrigemId",
                table: "OrdensServico",
                type: "INTEGER",
                nullable: true);

            // Tudo que existe hoje foi aberto como ordem de serviço — o orçamento separado
            // nasce agora. O defaultValue é o que classifica as linhas antigas.
            migrationBuilder.AddColumn<string>(
                name: "Tipo",
                table: "OrdensServico",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "OrdemServico");

            migrationBuilder.AddColumn<int>(
                name: "ValidadeDias",
                table: "OrdensServico",
                type: "INTEGER",
                nullable: false,
                defaultValue: 10);

            // O padrão do C# só vale para objeto novo; linha que já está no banco precisa
            // deste UPDATE (foi o que faltou quando a garantia entrou zerada)
            migrationBuilder.Sql(
                "UPDATE OrdensServico SET Tipo = 'OrdemServico' WHERE Tipo IS NULL OR Tipo = '';");
            migrationBuilder.Sql(
                "UPDATE OrdensServico SET ValidadeDias = 10 WHERE ValidadeDias <= 0;");

            migrationBuilder.CreateIndex(
                name: "IX_OrdensServico_OrcamentoOrigemId",
                table: "OrdensServico",
                column: "OrcamentoOrigemId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdensServico_Tipo",
                table: "OrdensServico",
                column: "Tipo");

            migrationBuilder.AddForeignKey(
                name: "FK_OrdensServico_OrdensServico_OrcamentoOrigemId",
                table: "OrdensServico",
                column: "OrcamentoOrigemId",
                principalTable: "OrdensServico",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrdensServico_OrdensServico_OrcamentoOrigemId",
                table: "OrdensServico");

            migrationBuilder.DropIndex(
                name: "IX_OrdensServico_OrcamentoOrigemId",
                table: "OrdensServico");

            migrationBuilder.DropIndex(
                name: "IX_OrdensServico_Tipo",
                table: "OrdensServico");

            migrationBuilder.DropColumn(
                name: "OrcamentoOrigemId",
                table: "OrdensServico");

            migrationBuilder.DropColumn(
                name: "Tipo",
                table: "OrdensServico");

            migrationBuilder.DropColumn(
                name: "ValidadeDias",
                table: "OrdensServico");
        }
    }
}
