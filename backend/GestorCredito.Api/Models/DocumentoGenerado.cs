namespace GestorCredito.Api.Models;

public class DocumentoGenerado
{
    public int Id { get; set; }
    public int ProspectoId { get; set; }
    public Prospecto? Prospecto { get; set; }
    public TipoDocumentoGenerado Tipo { get; set; }
    public required byte[] Contenido { get; set; }
    public DateTime FechaGeneracion { get; set; } = DateTime.Now;
}
