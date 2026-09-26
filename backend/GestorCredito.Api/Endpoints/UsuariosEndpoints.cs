using GestorCredito.Api.Data;
using GestorCredito.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GestorCredito.Api.Endpoints;

public record UsuarioDto(int Id, string Nombre, RolUsuario Rol, bool Activo);
public record UsuarioGuardarDto(string Nombre, RolUsuario Rol);

/// <summary>FR-004: gestión administrativa de Usuarios (registro de referencia; no
/// gatilla ninguna sesión, ver research.md §5).</summary>
public static class UsuariosEndpoints
{
    public static void MapUsuariosEndpoints(this WebApplication app)
    {
        var grupo = app.MapGroup("/api/usuarios").WithTags("Usuarios");

        grupo.MapGet("/", async (AppDbContext db) =>
            (await db.Usuarios.ToListAsync()).Select(AMapaDto));

        grupo.MapPost("/", async (UsuarioGuardarDto dto, AppDbContext db) =>
        {
            var entidad = new Usuario { Nombre = dto.Nombre, Rol = dto.Rol };
            db.Usuarios.Add(entidad);
            await db.SaveChangesAsync();
            return Results.Created($"/api/usuarios/{entidad.Id}", AMapaDto(entidad));
        });

        grupo.MapPut("/{id:int}", async (int id, UsuarioGuardarDto dto, AppDbContext db) =>
        {
            var entidad = await db.Usuarios.FindAsync(id);
            if (entidad is null) return Results.NotFound();
            entidad.Nombre = dto.Nombre;
            entidad.Rol = dto.Rol;
            await db.SaveChangesAsync();
            return Results.Ok(AMapaDto(entidad));
        });

        grupo.MapDelete("/{id:int}", async (int id, AppDbContext db) =>
        {
            var entidad = await db.Usuarios.FindAsync(id);
            if (entidad is null) return Results.NotFound();
            entidad.Activo = false;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    private static UsuarioDto AMapaDto(Usuario u) => new(u.Id, u.Nombre, u.Rol, u.Activo);
}
