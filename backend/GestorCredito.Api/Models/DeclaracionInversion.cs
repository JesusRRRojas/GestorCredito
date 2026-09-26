namespace GestorCredito.Api.Models;

public class DeclaracionInversion
{
    public int Id { get; set; }
    public int ProspectoId { get; set; }
    public Prospecto? Prospecto { get; set; }
    public decimal MontoInvertir { get; set; }
    public OrigenFondos OrigenFondos { get; set; }
    public string? Detalle { get; set; }
    public DateTime FechaRegistro { get; set; } = DateTime.Now;
}
