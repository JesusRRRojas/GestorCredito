namespace GestorCredito.Api.Models;

public class TipoPersona
{
    public int Id { get; set; }
    public required string Nombre { get; set; }
    public bool Activo { get; set; } = true;

    public List<TipoDocumento> TiposDocumento { get; set; } = [];
}
