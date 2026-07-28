using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ListasCompras.Migrations
{
    /// <inheritdoc />
    public partial class AddDadosLojaConfiguracao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Bairro",
                table: "ConfiguracoesLoja",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cep",
                table: "ConfiguracoesLoja",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cidade",
                table: "ConfiguracoesLoja",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cnpj",
                table: "ConfiguracoesLoja",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "ConfiguracoesLoja",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Endereco",
                table: "ConfiguracoesLoja",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Numero",
                table: "ConfiguracoesLoja",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Telefone",
                table: "ConfiguracoesLoja",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Uf",
                table: "ConfiguracoesLoja",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Bairro",
                table: "ConfiguracoesLoja");

            migrationBuilder.DropColumn(
                name: "Cep",
                table: "ConfiguracoesLoja");

            migrationBuilder.DropColumn(
                name: "Cidade",
                table: "ConfiguracoesLoja");

            migrationBuilder.DropColumn(
                name: "Cnpj",
                table: "ConfiguracoesLoja");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "ConfiguracoesLoja");

            migrationBuilder.DropColumn(
                name: "Endereco",
                table: "ConfiguracoesLoja");

            migrationBuilder.DropColumn(
                name: "Numero",
                table: "ConfiguracoesLoja");

            migrationBuilder.DropColumn(
                name: "Telefone",
                table: "ConfiguracoesLoja");

            migrationBuilder.DropColumn(
                name: "Uf",
                table: "ConfiguracoesLoja");
        }
    }
}
