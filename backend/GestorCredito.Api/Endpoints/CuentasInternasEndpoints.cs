using GestorCredito.Api.Data;
using GestorCredito.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GestorCredito.Api.Endpoints;

public record CuentaInternaDto(int Id, string NumeroCuenta, Moneda Moneda);
public record CuentaInternaGuardarDto(string NumeroCuenta);

/// <summary>
/// Cuentas Bancarias Internas del cliente (Historia 2, spec 002): reutilizables entre
/// prospectos del mismo cliente (mismo Tipo/Número de Documento), sin una entidad "Cliente"
/// separada (research.md §2).
/// </summary>
public static class CuentasInternasEndpoints
{
    public static void MapCuentasInternasEndpoints(this WebApplication app)
    {
        var grupo = app.MapGroup("/api/clientes/{tipoDocumentoId:int}/{numeroDocumento}/cuentas-internas")
            .WithTags("CuentasInternas");

        grupo.MapGet("/", async (int tipoDocumentoId, string numeroDocumento, AppDbContext db) =>
        {
            var cuentas = await db.CuentasBancariasInternas
                .Where(c => c.TipoDocumentoId == tipoDocumentoId && c.NumeroDocumento == numeroDocumento && c.Activo)
                .ToListAsync();
            return Results.Ok(cuentas.Select(AMapaDto));
        });

        grupo.MapPost("/", async (
            int tipoDocumentoId, string numeroDocumento, CuentaInternaGuardarDto dto, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(dto.NumeroCuenta))
            {
                return Results.BadRequest(new { error = "El Número de Cuenta es requerido." });
            }

            var existe = await db.CuentasBancariasInternas.AnyAsync(c =>
                c.TipoDocumentoId == tipoDocumentoId && c.NumeroDocumento == numeroDocumento &&
                c.NumeroCuenta == dto.NumeroCuenta);
            if (existe)
            {
                return Results.Conflict(new { error = "Esa cuenta ya está registrada para este cliente." });
            }

            var entidad = new CuentaBancariaInterna
            {
                TipoDocumentoId = tipoDocumentoId,
                NumeroDocumento = numeroDocumento,
                NumeroCuenta = dto.NumeroCuenta,
                Moneda = Moneda.PEN
            };
            db.CuentasBancariasInternas.Add(entidad);
            await db.SaveChangesAsync();
            return Results.Created($"/api/clientes/{tipoDocumentoId}/{numeroDocumento}/cuentas-internas/{entidad.Id}", AMapaDto(entidad));
        });
    }

    private static CuentaInternaDto AMapaDto(CuentaBancariaInterna c) => new(c.Id, c.NumeroCuenta, c.Moneda);
}
