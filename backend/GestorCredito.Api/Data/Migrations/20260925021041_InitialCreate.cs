using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorCredito.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Productos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Moneda = table.Column<int>(type: "int", nullable: false),
                    MontoMin = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MontoMax = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TasaMin = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    TasaMax = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    PlazoMin = table.Column<int>(type: "int", nullable: false),
                    PlazoMax = table.Column<int>(type: "int", nullable: false),
                    FrecuenciaPago = table.Column<int>(type: "int", nullable: false),
                    DiaPago = table.Column<int>(type: "int", nullable: false),
                    CargoOtrosPorCuota = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RequiereRequisitos = table.Column<bool>(type: "bit", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Productos", x => x.Id);
                    table.CheckConstraint("CK_Producto_MontoMax", "[MontoMax] >= [MontoMin]");
                    table.CheckConstraint("CK_Producto_MontoMin", "[MontoMin] > 0");
                    table.CheckConstraint("CK_Producto_PlazoRango", "[PlazoMax] >= [PlazoMin] AND [PlazoMin] >= 1");
                    table.CheckConstraint("CK_Producto_TasaRango", "[TasaMax] >= [TasaMin] AND [TasaMin] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "TiposDocumento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposDocumento", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TiposPersona",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposPersona", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Rol = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Requisitos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Requisitos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Requisitos_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Prospectos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TipoDocumentoId = table.Column<int>(type: "int", nullable: false),
                    NumeroDocumento = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ResultadoMock = table.Column<int>(type: "int", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: true),
                    Nombres = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Apellidos = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Direccion = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CuentaBancariaCCI = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaCierre = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prospectos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Prospectos_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Prospectos_TiposDocumento_TipoDocumentoId",
                        column: x => x.TipoDocumentoId,
                        principalTable: "TiposDocumento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TipoDocumentoTipoPersona",
                columns: table => new
                {
                    TiposDocumentoId = table.Column<int>(type: "int", nullable: false),
                    TiposPersonaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TipoDocumentoTipoPersona", x => new { x.TiposDocumentoId, x.TiposPersonaId });
                    table.ForeignKey(
                        name: "FK_TipoDocumentoTipoPersona_TiposDocumento_TiposDocumentoId",
                        column: x => x.TiposDocumentoId,
                        principalTable: "TiposDocumento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TipoDocumentoTipoPersona_TiposPersona_TiposPersonaId",
                        column: x => x.TiposPersonaId,
                        principalTable: "TiposPersona",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentosAdjuntos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProspectoId = table.Column<int>(type: "int", nullable: false),
                    RequisitoId = table.Column<int>(type: "int", nullable: false),
                    NombreArchivo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Contenido = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    FechaCarga = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentosAdjuntos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentosAdjuntos_Prospectos_ProspectoId",
                        column: x => x.ProspectoId,
                        principalTable: "Prospectos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DocumentosAdjuntos_Requisitos_RequisitoId",
                        column: x => x.RequisitoId,
                        principalTable: "Requisitos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DocumentosGenerados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProspectoId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Contenido = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    FechaGeneracion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentosGenerados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentosGenerados_Prospectos_ProspectoId",
                        column: x => x.ProspectoId,
                        principalTable: "Prospectos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Simulaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProspectoId = table.Column<int>(type: "int", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Tasa = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Plazo = table.Column<int>(type: "int", nullable: false),
                    FechaSimulacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Aceptada = table.Column<bool>(type: "bit", nullable: false),
                    FechaAceptacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Simulaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Simulaciones_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Simulaciones_Prospectos_ProspectoId",
                        column: x => x.ProspectoId,
                        principalTable: "Prospectos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Cuotas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SimulacionId = table.Column<int>(type: "int", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    FechaPago = table.Column<DateOnly>(type: "date", nullable: false),
                    SaldoInicial = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Amortizacion = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Interes = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Otros = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PagoTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SaldoFinal = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cuotas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cuotas_Simulaciones_SimulacionId",
                        column: x => x.SimulacionId,
                        principalTable: "Simulaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cuotas_SimulacionId",
                table: "Cuotas",
                column: "SimulacionId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosAdjuntos_ProspectoId_RequisitoId",
                table: "DocumentosAdjuntos",
                columns: new[] { "ProspectoId", "RequisitoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosAdjuntos_RequisitoId",
                table: "DocumentosAdjuntos",
                column: "RequisitoId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosGenerados_ProspectoId",
                table: "DocumentosGenerados",
                column: "ProspectoId");

            migrationBuilder.CreateIndex(
                name: "IX_Prospecto_NumeroDocumento_Activo",
                table: "Prospectos",
                column: "NumeroDocumento",
                unique: true,
                filter: "[Estado] IN (0, 1, 2, 3, 5)");

            migrationBuilder.CreateIndex(
                name: "IX_Prospectos_ProductoId",
                table: "Prospectos",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_Prospectos_TipoDocumentoId",
                table: "Prospectos",
                column: "TipoDocumentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Requisitos_ProductoId",
                table: "Requisitos",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_Simulacion_ProspectoId_Aceptada",
                table: "Simulaciones",
                column: "ProspectoId",
                unique: true,
                filter: "[Aceptada] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Simulaciones_ProductoId",
                table: "Simulaciones",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_TipoDocumentoTipoPersona_TiposPersonaId",
                table: "TipoDocumentoTipoPersona",
                column: "TiposPersonaId");

            migrationBuilder.CreateIndex(
                name: "IX_TiposDocumento_Nombre",
                table: "TiposDocumento",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TiposPersona_Nombre",
                table: "TiposPersona",
                column: "Nombre",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cuotas");

            migrationBuilder.DropTable(
                name: "DocumentosAdjuntos");

            migrationBuilder.DropTable(
                name: "DocumentosGenerados");

            migrationBuilder.DropTable(
                name: "TipoDocumentoTipoPersona");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Simulaciones");

            migrationBuilder.DropTable(
                name: "Requisitos");

            migrationBuilder.DropTable(
                name: "TiposPersona");

            migrationBuilder.DropTable(
                name: "Prospectos");

            migrationBuilder.DropTable(
                name: "Productos");

            migrationBuilder.DropTable(
                name: "TiposDocumento");
        }
    }
}
