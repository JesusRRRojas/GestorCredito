using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorCredito.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCuentaBancariaYDeclaracionInversion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CuentaBancariaCCI",
                table: "Prospectos",
                newName: "CuentaExternaCCI");

            migrationBuilder.AddColumn<string>(
                name: "ComentarioAprobador",
                table: "Prospectos",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CuentaBancariaInternaId",
                table: "Prospectos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CuentaExternaBanco",
                table: "Prospectos",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TipoCuentaDesembolso",
                table: "Prospectos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiereDeclaracionInversion",
                table: "Productos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CuentasBancariasInternas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TipoDocumentoId = table.Column<int>(type: "int", nullable: false),
                    NumeroDocumento = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NumeroCuenta = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Moneda = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CuentasBancariasInternas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeclaracionesInversion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProspectoId = table.Column<int>(type: "int", nullable: false),
                    MontoInvertir = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OrigenFondos = table.Column<int>(type: "int", nullable: false),
                    Detalle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeclaracionesInversion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeclaracionesInversion_Prospectos_ProspectoId",
                        column: x => x.ProspectoId,
                        principalTable: "Prospectos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Prospectos_CuentaBancariaInternaId",
                table: "Prospectos",
                column: "CuentaBancariaInternaId");

            migrationBuilder.CreateIndex(
                name: "IX_CuentaBancariaInterna_Cliente_NumeroCuenta",
                table: "CuentasBancariasInternas",
                columns: new[] { "TipoDocumentoId", "NumeroDocumento", "NumeroCuenta" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeclaracionesInversion_ProspectoId",
                table: "DeclaracionesInversion",
                column: "ProspectoId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Prospectos_CuentasBancariasInternas_CuentaBancariaInternaId",
                table: "Prospectos",
                column: "CuentaBancariaInternaId",
                principalTable: "CuentasBancariasInternas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // research.md §8: los prospectos ya existentes conservan su CCI (renombrado por EF
            // Core a CuentaExternaCCI); se marcan explícitamente como cuenta Externa
            // (TipoCuentaDesembolso = 1) para que el Onboarding y el PDF final los interpreten
            // correctamente. CuentaExternaBanco queda null (a completar manualmente si aplica).
            migrationBuilder.Sql(
                "UPDATE Prospectos SET TipoCuentaDesembolso = 1 WHERE CuentaExternaCCI IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Prospectos_CuentasBancariasInternas_CuentaBancariaInternaId",
                table: "Prospectos");

            migrationBuilder.DropTable(
                name: "CuentasBancariasInternas");

            migrationBuilder.DropTable(
                name: "DeclaracionesInversion");

            migrationBuilder.DropIndex(
                name: "IX_Prospectos_CuentaBancariaInternaId",
                table: "Prospectos");

            migrationBuilder.DropColumn(
                name: "ComentarioAprobador",
                table: "Prospectos");

            migrationBuilder.DropColumn(
                name: "CuentaBancariaInternaId",
                table: "Prospectos");

            migrationBuilder.DropColumn(
                name: "CuentaExternaBanco",
                table: "Prospectos");

            migrationBuilder.DropColumn(
                name: "TipoCuentaDesembolso",
                table: "Prospectos");

            migrationBuilder.DropColumn(
                name: "RequiereDeclaracionInversion",
                table: "Productos");

            migrationBuilder.RenameColumn(
                name: "CuentaExternaCCI",
                table: "Prospectos",
                newName: "CuentaBancariaCCI");
        }
    }
}
