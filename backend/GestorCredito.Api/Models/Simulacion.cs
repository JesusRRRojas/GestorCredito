namespace GestorCredito.Api.Models;

public class Simulacion
{
    public int Id { get; set; }
    public int ProspectoId { get; set; }
    public Prospecto? Prospecto { get; set; }
    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }
    public decimal Monto { get; set; }
    public decimal Tasa { get; set; }
    public int Plazo { get; set; }
    public DateTime FechaSimulacion { get; set; } = DateTime.Now;
    public bool Aceptada { get; set; }
    public DateTime? FechaAceptacion { get; set; }

    public List<Cuota> Cuotas { get; set; } = [];
}
