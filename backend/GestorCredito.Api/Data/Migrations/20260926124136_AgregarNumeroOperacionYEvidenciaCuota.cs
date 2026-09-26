using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorCredito.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarNumeroOperacionYEvidenciaCuota : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "EvidenciaContenido",
                table: "Cuotas",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EvidenciaNombreArchivo",
                table: "Cuotas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroOperacion",
                table: "Cuotas",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EvidenciaContenido",
                table: "Cuotas");

            migrationBuilder.DropColumn(
                name: "EvidenciaNombreArchivo",
                table: "Cuotas");

            migrationBuilder.DropColumn(
                name: "NumeroOperacion",
                table: "Cuotas");
        }
    }
}
