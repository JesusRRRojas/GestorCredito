using GestorCredito.Api.Data;
using GestorCredito.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GestorCredito.Api.Endpoints;

public record TipoDocumentoDto(int Id, string Nombre, bool Activo, List<int> TipoPersonaIds);
public record TipoDocumentoCrearDto(string Nombre, List<int> TipoPersonaIds);
public record TipoDocumentoEditarDto(string Nombre, bool Activo, List<int> TipoPersonaIds);

public record TipoPersonaDto(int Id, string Nombre, bool Activo);
public record TipoPersonaCrearDto(string Nombre);
public record TipoPersonaEditarDto(string Nombre, bool Activo);

/// <summary>FR-001: CRUD de Tipos de Documento y Tipos de Persona, y su asociación.</summary>
public static class TiposEndpoints
{
    public static void MapTiposEndpoints(this WebApplication app)
    {
        var documentos = app.MapGroup("/api/tipos-documento").WithTags("TiposDocumento");

        documentos.MapGet("/", async (AppDbContext db) =>
            (await db.TiposDocumento.Include(t => t.TiposPersona).ToListAsync())
                .Select(AMapaDto));

        documentos.MapPost("/", async (TipoDocumentoCrearDto dto, AppDbContext db) =>
        {
            var tiposPersona = await db.TiposPersona
                .Where(tp => dto.TipoPersonaIds.Contains(tp.Id))
                .ToListAsync();

            var entidad = new TipoDocumento { Nombre = dto.Nombre, TiposPersona = tiposPersona };
            db.TiposDocumento.Add(entidad);
            await db.SaveChangesAsync();
            return Results.Created($"/api/tipos-documento/{entidad.Id}", AMapaDto(entidad));
        });

        documentos.MapPut("/{id:int}", async (int id, TipoDocumentoEditarDto dto, AppDbContext db) =>
        {
            var entidad = await db.TiposDocumento.Include(t => t.TiposPersona)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (entidad is null) return Results.NotFound();

            entidad.Nombre = dto.Nombre;
            entidad.Activo = dto.Activo;
            entidad.TiposPersona = await db.TiposPersona
                .Where(tp => dto.TipoPersonaIds.Contains(tp.Id))
                .ToListAsync();

            await db.SaveChangesAsync();
            return Results.Ok(AMapaDto(entidad));
        });

        documentos.MapDelete("/{id:int}", async (int id, AppDbContext db) =>
        {
            var entidad = await db.TiposDocumento.FindAsync(id);
            if (entidad is null) return Results.NotFound();
            entidad.Activo = false;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        var personas = app.MapGroup("/api/tipos-persona").WithTags("TiposPersona");

        personas.MapGet("/", async (AppDbContext db) =>
            (await db.TiposPersona.ToListAsync()).Select(AMapaDto));

        personas.MapPost("/", async (TipoPersonaCrearDto dto, AppDbContext db) =>
        {
            var entidad = new TipoPersona { Nombre = dto.Nombre };
            db.TiposPersona.Add(entidad);
            await db.SaveChangesAsync();
            return Results.Created($"/api/tipos-persona/{entidad.Id}", AMapaDto(entidad));
        });

        personas.MapPut("/{id:int}", async (int id, TipoPersonaEditarDto dto, AppDbContext db) =>
        {
            var entidad = await db.TiposPersona.FindAsync(id);
            if (entidad is null) return Results.NotFound();
            entidad.Nombre = dto.Nombre;
            entidad.Activo = dto.Activo;
            await db.SaveChangesAsync();
            return Results.Ok(AMapaDto(entidad));
        });

        personas.MapDelete("/{id:int}", async (int id, AppDbContext db) =>
        {
            var entidad = await db.TiposPersona.FindAsync(id);
            if (entidad is null) return Results.NotFound();
            entidad.Activo = false;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    private static TipoDocumentoDto AMapaDto(TipoDocumento t) =>
        new(t.Id, t.Nombre, t.Activo, t.TiposPersona.Select(p => p.Id).ToList());

    private static TipoPersonaDto AMapaDto(TipoPersona t) => new(t.Id, t.Nombre, t.Activo);
}
