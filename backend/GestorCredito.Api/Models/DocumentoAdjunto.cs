namespace GestorCredito.Api.Models;

public class DocumentoAdjunto
{
    public int Id { get; set; }
    public int ProspectoId { get; set; }
    public Prospecto? Prospecto { get; set; }
    public int RequisitoId { get; set; }
    public Requisito? Requisito { get; set; }
    public required string NombreArchivo { get; set; }
    public required byte[] Contenido { get; set; }
    public DateTime FechaCarga { get; set; } = DateTime.Now;
}
