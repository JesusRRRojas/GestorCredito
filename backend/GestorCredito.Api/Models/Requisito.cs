namespace GestorCredito.Api.Models;

public class Requisito
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }
    public required string Nombre { get; set; }
    public bool Activo { get; set; } = true;
}
