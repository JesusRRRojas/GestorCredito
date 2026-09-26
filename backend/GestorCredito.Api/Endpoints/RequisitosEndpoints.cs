using GestorCredito.Api.Data;
using GestorCredito.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GestorCredito.Api.Endpoints;

public record RequisitoDto(int Id, int ProductoId, string Nombre, bool Activo);
public record RequisitoCrearDto(string Nombre);
public record RequisitoEditarDto(string Nombre, bool Activo);

/// <summary>FR-003: Requisitos documentales asociados a un Producto.</summary>
public static class RequisitosEndpoints
{
    public static void MapRequisitosEndpoints(this WebApplication app)
    {
        app.MapGet("/api/productos/{productoId:int}/requisitos", async (int productoId, AppDbContext db) =>
            (await db.Requisitos.Where(r => r.ProductoId == productoId).ToListAsync())
                .Select(AMapaDto))
            .WithTags("Requisitos");

        app.MapPost("/api/productos/{productoId:int}/requisitos", async (
            int productoId, RequisitoCrearDto dto, AppDbContext db) =>
        {
            var producto = await db.Productos.FindAsync(productoId);
            if (producto is null) return Results.NotFound();

            var entidad = new Requisito { ProductoId = productoId, Nombre = dto.Nombre };
            db.Requisitos.Add(entidad);
            await db.SaveChangesAsync();
            return Results.Created($"/api/requisitos/{entidad.Id}", AMapaDto(entidad));
        }).WithTags("Requisitos");

        app.MapPut("/api/requisitos/{id:int}", async (int id, RequisitoEditarDto dto, AppDbContext db) =>
        {
            var entidad = await db.Requisitos.FindAsync(id);
            if (entidad is null) return Results.NotFound();
            entidad.Nombre = dto.Nombre;
            entidad.Activo = dto.Activo;
            await db.SaveChangesAsync();
            return Results.Ok(AMapaDto(entidad));
        }).WithTags("Requisitos");

        app.MapDelete("/api/requisitos/{id:int}", async (int id, AppDbContext db) =>
        {
            var entidad = await db.Requisitos.FindAsync(id);
            if (entidad is null) return Results.NotFound();
            entidad.Activo = false;
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).WithTags("Requisitos");
    }

    private static RequisitoDto AMapaDto(Requisito r) => new(r.Id, r.ProductoId, r.Nombre, r.Activo);
}
