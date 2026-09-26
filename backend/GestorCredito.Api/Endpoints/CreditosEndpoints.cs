using GestorCredito.Api.Data;
using GestorCredito.Api.Models;
using GestorCredito.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestorCredito.Api.Endpoints;

public record CuotaActualDto(int Numero, DateOnly FechaPago, decimal PagoTotal);

public record CreditoResumenDto(
    int ProspectoId, string? ProductoNombre, decimal MontoSolicitado, string EstadoCredito,
    CuotaActualDto? CuotaActual);

public record ClienteConCreditosDto(
    int TipoDocumentoId, string TipoDocumento, string NumeroDocumento, string NombreCompleto,
    List<CreditoResumenDto> Creditos);

public record CronogramaCuotaDto(
    int Numero, DateOnly FechaPago, decimal PagoTotal, string EstadoCuota,
    string? NumeroOperacion, bool TieneEvidencia);

public record CronogramaCreditoDto(
    string EstadoCredito, int CuotasPagadas, int TotalCuotas, List<CronogramaCuotaDto> Cuotas);

/// <summary>
/// "Crédito" es un concepto derivado (Prospecto + su simulación aceptada + Cuotas), sin tabla
/// propia (spec 003, Key Entities). Sirve tanto a la bandeja del Cajero (sin filtro) como a la
/// consulta de créditos por cliente (filtrando por Número de Documento) — research.md §2.
/// </summary>
public static class CreditosEndpoints
{
    public static void MapCreditosEndpoints(this WebApplication app)
    {
        var grupo = app.MapGroup("/api/creditos").WithTags("Creditos");

        grupo.MapGet("/", async (string? buscar, AppDbContext db) =>
        {
            var prospectos = await db.Prospectos
                .Include(p => p.TipoDocumento)
                .Include(p => p.Simulaciones).ThenInclude(s => s.Producto)
                .Include(p => p.Simulaciones).ThenInclude(s => s.Cuotas)
                .Where(p => p.Nombres != null && p.Estado != EstadoProspecto.Rechazado)
                .ToListAsync();

            var texto = buscar?.Trim();

            var clientes = prospectos
                .GroupBy(p => new { p.TipoDocumentoId, p.NumeroDocumento })
                .Select(g =>
                {
                    var primero = g.First();
                    var nombreCompleto = $"{primero.Nombres} {primero.Apellidos}".Trim();
                    return new ClienteConCreditosDto(
                        g.Key.TipoDocumentoId,
                        primero.TipoDocumento?.Nombre ?? string.Empty,
                        g.Key.NumeroDocumento,
                        nombreCompleto,
                        g.Select(AMapaCredito).ToList());
                })
                .Where(c => string.IsNullOrEmpty(texto) ||
                    c.NombreCompleto.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                    c.NumeroDocumento.Contains(texto, StringComparison.OrdinalIgnoreCase))
                .OrderBy(c => c.NombreCompleto)
                .ToList();

            return Results.Ok(clientes);
        });

        grupo.MapGet("/{prospectoId:int}/cronograma", async (int prospectoId, AppDbContext db) =>
        {
            var prospecto = await db.Prospectos
                .Include(p => p.Simulaciones).ThenInclude(s => s.Cuotas)
                .FirstOrDefaultAsync(p => p.Id == prospectoId);
            if (prospecto is null) return Results.NotFound();

            var simulacionAceptada = prospecto.Simulaciones.FirstOrDefault(s => s.Aceptada);
            var (estadoCredito, _) = CreditoCalculator.Calcular(
                prospecto.Estado, simulacionAceptada?.Cuotas ?? []);

            if (simulacionAceptada is null || estadoCredito == "EnProceso")
            {
                return Results.Conflict(new { error = "CREDITO_SIN_CRONOGRAMA" });
            }

            var estadoPorCuota = CreditoCalculator.ClasificarCuotas(simulacionAceptada.Cuotas);
            var cuotas = simulacionAceptada.Cuotas
                .OrderBy(c => c.Numero)
                .Select(c => new CronogramaCuotaDto(
                    c.Numero, c.FechaPago, c.PagoTotal, estadoPorCuota[c.Numero],
                    c.NumeroOperacion, c.EvidenciaContenido != null))
                .ToList();

            return Results.Ok(new CronogramaCreditoDto(
                estadoCredito, cuotas.Count(c => c.EstadoCuota == "Pagada"), cuotas.Count, cuotas));
        });

        grupo.MapPost("/{prospectoId:int}/pagar-cuota", async (
            int prospectoId, [FromForm] string numeroOperacion, IFormFile? archivo, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(numeroOperacion))
            {
                return Results.BadRequest(new { error = "NUMERO_OPERACION_REQUERIDO" });
            }

            byte[]? evidenciaContenido = null;
            if (archivo is not null)
            {
                var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
                var esFormatoValido = extension is ".jpg" or ".jpeg" or ".png" or ".pdf";
                var esTamanoValido = archivo.Length > 0 && archivo.Length <= 5 * 1024 * 1024;
                if (!esFormatoValido || !esTamanoValido)
                {
                    return Results.BadRequest(new { error = "ARCHIVO_EVIDENCIA_INVALIDO" });
                }

                using var ms = new MemoryStream();
                await archivo.CopyToAsync(ms);
                evidenciaContenido = ms.ToArray();
            }

            var prospecto = await db.Prospectos
                .Include(p => p.Simulaciones).ThenInclude(s => s.Cuotas)
                .FirstOrDefaultAsync(p => p.Id == prospectoId);
            if (prospecto is null) return Results.NotFound();

            var simulacionAceptada = prospecto.Simulaciones.FirstOrDefault(s => s.Aceptada);
            if (simulacionAceptada is null)
            {
                return Results.Conflict(new { error = "CREDITO_NO_TIENE_CUOTA_PENDIENTE" });
            }

            var (estadoCredito, cuotaActual) = CreditoCalculator.Calcular(prospecto.Estado, simulacionAceptada.Cuotas);
            if (estadoCredito != "Pendiente" || cuotaActual is null)
            {
                return Results.Conflict(new { error = "CREDITO_NO_TIENE_CUOTA_PENDIENTE" });
            }

            var cuota = simulacionAceptada.Cuotas.First(c => c.Numero == cuotaActual.Numero);
            cuota.Pagada = true;
            cuota.FechaPagoRealizado = DateTime.Now;
            cuota.NumeroOperacion = numeroOperacion.Trim();
            if (archivo is not null)
            {
                cuota.EvidenciaNombreArchivo = archivo.FileName;
                cuota.EvidenciaContenido = evidenciaContenido;
            }
            await db.SaveChangesAsync();

            var (nuevoEstado, nuevaCuotaActual) = CreditoCalculator.Calcular(prospecto.Estado, simulacionAceptada.Cuotas);
            return Results.Ok(new
            {
                estadoCredito = nuevoEstado,
                cuotaActual = nuevaCuotaActual is null
                    ? null
                    : new CuotaActualDto(nuevaCuotaActual.Numero, nuevaCuotaActual.FechaPago, nuevaCuotaActual.PagoTotal)
            });
        }).DisableAntiforgery();

        grupo.MapGet("/{prospectoId:int}/cuotas/{numero:int}/evidencia", async (
            int prospectoId, int numero, AppDbContext db) =>
        {
            var cuota = await db.Cuotas
                .FirstOrDefaultAsync(c =>
                    c.Simulacion!.ProspectoId == prospectoId && c.Numero == numero);

            if (cuota?.EvidenciaContenido is null) return Results.NotFound();

            var extension = Path.GetExtension(cuota.EvidenciaNombreArchivo ?? string.Empty).ToLowerInvariant();
            var contentType = extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/pdf"
            };

            return Results.File(cuota.EvidenciaContenido, contentType, cuota.EvidenciaNombreArchivo);
        });
    }

    private static CreditoResumenDto AMapaCredito(Prospecto p)
    {
        var simulacionAceptada = p.Simulaciones.FirstOrDefault(s => s.Aceptada);
        var (estadoCredito, cuotaActual) = CreditoCalculator.Calcular(
            p.Estado, simulacionAceptada?.Cuotas ?? []);

        return new CreditoResumenDto(
            p.Id,
            simulacionAceptada?.Producto?.Nombre,
            simulacionAceptada?.Monto ?? 0,
            estadoCredito,
            cuotaActual is null ? null : new CuotaActualDto(cuotaActual.Numero, cuotaActual.FechaPago, cuotaActual.PagoTotal));
    }
}
