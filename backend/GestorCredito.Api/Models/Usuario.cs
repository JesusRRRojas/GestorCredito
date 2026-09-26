namespace GestorCredito.Api.Models;

public class Usuario
{
    public int Id { get; set; }
    public required string Nombre { get; set; }
    public RolUsuario Rol { get; set; }
    public bool Activo { get; set; } = true;
}
