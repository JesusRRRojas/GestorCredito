namespace GestorCredito.Api.Models;

public class TipoDocumento
{
    public int Id { get; set; }
    public required string Nombre { get; set; }
    public bool Activo { get; set; } = true;

    public List<TipoPersona> TiposPersona { get; set; } = [];
}
