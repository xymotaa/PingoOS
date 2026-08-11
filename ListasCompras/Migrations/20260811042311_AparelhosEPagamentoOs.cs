using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ListasCompras.Migrations
{
    /// <inheritdoc />
    public partial class AparelhosEPagamentoOs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A tabela nova vem primeiro para que os aparelhos já cadastrados sejam
            // copiados antes de as colunas antigas serem removidas
            migrationBuilder.CreateTable(
                name: "AparelhosOs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrdemServicoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", nullable: true),
                    Marca = table.Column<string>(type: "TEXT", nullable: true),
                    Modelo = table.Column<string>(type: "TEXT", nullable: true),
                    NumeroSerie = table.Column<string>(type: "TEXT", nullable: true),
                    SemNumeroSerie = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AparelhosOs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AparelhosOs_OrdensServico_OrdemServicoId",
                        column: x => x.OrdemServicoId,
                        principalTable: "OrdensServico",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AparelhosOs_OrdemServicoId",
                table: "AparelhosOs",
                column: "OrdemServicoId");

            // Cada OS existente vira um aparelho, preservando o que já estava cadastrado.
            // Ordens sem nenhum dado de aparelho são ignoradas.
            migrationBuilder.Sql(@"
                INSERT INTO AparelhosOs (OrdemServicoId, Tipo, Marca, Modelo, NumeroSerie, SemNumeroSerie)
                SELECT Id, DispositivoTipo, DispositivoMarca, DispositivoModelo, DispositivoSerie, SemNumeroSerie
                FROM OrdensServico
                WHERE COALESCE(DispositivoTipo, '') <> ''
                   OR COALESCE(DispositivoMarca, '') <> ''
                   OR COALESCE(DispositivoModelo, '') <> ''
                   OR COALESCE(DispositivoSerie, '') <> '';
            ");

            migrationBuilder.DropColumn(name: "DispositivoMarca", table: "OrdensServico");
            migrationBuilder.DropColumn(name: "DispositivoModelo", table: "OrdensServico");
            migrationBuilder.DropColumn(name: "DispositivoSerie", table: "OrdensServico");
            migrationBuilder.DropColumn(name: "DispositivoTipo", table: "OrdensServico");
            migrationBuilder.DropColumn(name: "SemNumeroSerie", table: "OrdensServico");

            migrationBuilder.AddColumn<decimal>(
                name: "Desconto", table: "OrdensServico", type: "TEXT", nullable: false, defaultValue: 0m);
            migrationBuilder.AddColumn<string>(
                name: "DescontoTipo", table: "OrdensServico", type: "TEXT", nullable: false, defaultValue: "percentual");
            migrationBuilder.AddColumn<string>(
                name: "FormaPagamento", table: "OrdensServico", type: "TEXT", nullable: true);
            migrationBuilder.AddColumn<bool>(
                name: "Parcelado", table: "OrdensServico", type: "INTEGER", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<int>(
                name: "Parcelas", table: "OrdensServico", type: "INTEGER", nullable: false, defaultValue: 1);
            migrationBuilder.AddColumn<decimal>(
                name: "Sinal", table: "OrdensServico", type: "TEXT", nullable: false, defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AparelhosOs");

            migrationBuilder.DropColumn(
                name: "Desconto",
                table: "OrdensServico");

            migrationBuilder.DropColumn(
                name: "DescontoTipo",
                table: "OrdensServico");

            migrationBuilder.DropColumn(
                name: "Parcelado",
                table: "OrdensServico");

            migrationBuilder.DropColumn(
                name: "Sinal",
                table: "OrdensServico");

            migrationBuilder.RenameColumn(
                name: "Parcelas",
                table: "OrdensServico",
                newName: "SemNumeroSerie");

            migrationBuilder.RenameColumn(
                name: "FormaPagamento",
                table: "OrdensServico",
                newName: "DispositivoTipo");

            migrationBuilder.AddColumn<string>(
                name: "DispositivoMarca",
                table: "OrdensServico",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DispositivoModelo",
                table: "OrdensServico",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DispositivoSerie",
                table: "OrdensServico",
                type: "TEXT",
                nullable: true);
        }
    }
}
