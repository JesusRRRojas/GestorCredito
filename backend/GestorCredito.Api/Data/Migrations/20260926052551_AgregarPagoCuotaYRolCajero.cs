using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorCredito.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarPagoCuotaYRolCajero : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaPagoRealizado",
                table: "Cuotas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Pagada",
                table: "Cuotas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // FR-014 (spec 003, research.md §1): los créditos ya "Finalizado" antes de esta
            // versión no tienen ninguna cuota marcada como pagada; se asume que el Asesor los
            // cerró porque el proceso concluyó exitosamente, así que se marcan todas sus
            // cuotas como pagadas (para que se clasifiquen como "Cancelado").
            migrationBuilder.Sql(@"
                UPDATE c
                SET c.Pagada = 1, c.FechaPagoRealizado = p.FechaCierre
                FROM Cuotas c
                INNER JOIN Simulaciones s ON s.Id = c.SimulacionId
                INNER JOIN Prospectos p ON p.Id = s.ProspectoId
                WHERE p.Estado = 6;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FechaPagoRealizado",
                table: "Cuotas");

            migrationBuilder.DropColumn(
                name: "Pagada",
                table: "Cuotas");
        }
    }
}
