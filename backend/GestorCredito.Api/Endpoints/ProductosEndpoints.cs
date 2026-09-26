using GestorCredito.Api.Data;
using GestorCredito.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GestorCredito.Api.Endpoints;

public record ProductoDto(
    int Id, string Nombre, Moneda Moneda, decimal MontoMin, decimal MontoMax,
    decimal TasaMin, decimal TasaMax, int PlazoMin, int PlazoMax,
    FrecuenciaPago FrecuenciaPago, DiaPago DiaPago, decimal CargoOtrosPorCuota,
    bool RequiereRequisitos, bool RequiereDeclaracionInversion, bool Activo);

public record ProductoGuardarDto(
    string Nombre, decimal MontoMin, decimal MontoMax, decimal TasaMin, decimal TasaMax,
    int PlazoMin, int PlazoMax, DiaPago DiaPago, decimal CargoOtrosPorCuota,
    bool RequiereRequisitos, bool RequiereDeclaracionInversion);

/// <summary>FR-002: CRUD de Productos de crédito, con sus rangos y configuración.</summary>
public static class ProductosEndpoints
{
    public static void MapProductosEndpoints(this WebApplication app)
    {
        var grupo = app.MapGroup("/api/productos").WithTags("Productos");

        // FR-011: el selector de la Pantalla 2 solo debe listar Productos activos
        // (?activos=true); sin el parámetro, se listan todos para la pantalla de
        // Configuración del Administrador.
        grupo.MapGet("/", async (bool? activos, AppDbContext db) =>
        {
            var query = db.Productos.AsQueryable();
            if (activos == true)
            {
                query = query.Where(p => p.Activo);
            }
            return (await query.ToListAsync()).Select(AMapaDto);
        });

        grupo.MapGet("/{id:int}", async (int id, AppDbContext db) =>
        {
            var producto = await db.Productos.FindAsync(id);
            return producto is null ? Results.NotFound() : Results.Ok(AMapaDto(producto));
        });

        grupo.MapPost("/", async (ProductoGuardarDto dto, AppDbContext db) =>
        {
            var error = ValidarRangos(dto);
            if (error is not null) return Results.BadRequest(new { error });

            var entidad = new Producto
            {
                Nombre = dto.Nombre,
                Moneda = Moneda.PEN,
                MontoMin = dto.MontoMin,
                MontoMax = dto.MontoMax,
                TasaMin = dto.TasaMin,
                TasaMax = dto.TasaMax,
                PlazoMin = dto.PlazoMin,
                PlazoMax = dto.PlazoMax,
                FrecuenciaPago = FrecuenciaPago.Mensual,
                DiaPago = dto.DiaPago,
                CargoOtrosPorCuota = dto.CargoOtrosPorCuota,
                RequiereRequisitos = dto.RequiereRequisitos,
                RequiereDeclaracionInversion = dto.RequiereDeclaracionInversion
            };
            db.Productos.Add(entidad);
            await db.SaveChangesAsync();
            return Results.Created($"/api/productos/{entidad.Id}", AMapaDto(entidad));
        });

        grupo.MapPut("/{id:int}", async (int id, ProductoGuardarDto dto, AppDbContext db) =>
        {
            var error = ValidarRangos(dto);
            if (error is not null) return Results.BadRequest(new { error });

            var entidad = await db.Productos.FindAsync(id);
            if (entidad is null) return Results.NotFound();

            entidad.Nombre = dto.Nombre;
            entidad.MontoMin = dto.MontoMin;
            entidad.MontoMax = dto.MontoMax;
            entidad.TasaMin = dto.TasaMin;
            entidad.TasaMax = dto.TasaMax;
            entidad.PlazoMin = dto.PlazoMin;
            entidad.PlazoMax = dto.PlazoMax;
            entidad.DiaPago = dto.DiaPago;
            entidad.CargoOtrosPorCuota = dto.CargoOtrosPorCuota;
            entidad.RequiereRequisitos = dto.RequiereRequisitos;
            entidad.RequiereDeclaracionInversion = dto.RequiereDeclaracionInversion;

            await db.SaveChangesAsync();
            return Results.Ok(AMapaDto(entidad));
        });

        grupo.MapDelete("/{id:int}", async (int id, AppDbContext db) =>
        {
            var entidad = await db.Productos.FindAsync(id);
            if (entidad is null) return Results.NotFound();
            entidad.Activo = false;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    private static string? ValidarRangos(ProductoGuardarDto dto)
    {
        if (dto.MontoMin <= 0) return "El monto mínimo debe ser mayor a 0.";
        if (dto.MontoMax < dto.MontoMin) return "El monto máximo debe ser mayor o igual al mínimo.";
        if (dto.TasaMin < 0) return "La tasa mínima no puede ser negativa.";
        if (dto.TasaMax < dto.TasaMin) return "La tasa máxima debe ser mayor o igual a la mínima.";
        if (dto.PlazoMin < 1) return "El plazo mínimo debe ser al menos 1.";
        if (dto.PlazoMax < dto.PlazoMin) return "El plazo máximo debe ser mayor o igual al mínimo.";
        return null;
    }

    private static ProductoDto AMapaDto(Producto p) => new(
        p.Id, p.Nombre, p.Moneda, p.MontoMin, p.MontoMax, p.TasaMin, p.TasaMax,
        p.PlazoMin, p.PlazoMax, p.FrecuenciaPago, p.DiaPago, p.CargoOtrosPorCuota,
        p.RequiereRequisitos, p.RequiereDeclaracionInversion, p.Activo);
}
