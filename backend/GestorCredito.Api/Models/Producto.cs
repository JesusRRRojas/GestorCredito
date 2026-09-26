namespace GestorCredito.Api.Models;

public class Producto
{
    public int Id { get; set; }
    public required string Nombre { get; set; }
    public Moneda Moneda { get; set; } = Moneda.PEN;
    public decimal MontoMin { get; set; }
    public decimal MontoMax { get; set; }
    public decimal TasaMin { get; set; }
    public decimal TasaMax { get; set; }
    public int PlazoMin { get; set; }
    public int PlazoMax { get; set; }
    public FrecuenciaPago FrecuenciaPago { get; set; } = FrecuenciaPago.Mensual;
    public DiaPago DiaPago { get; set; }
    public decimal CargoOtrosPorCuota { get; set; }
    public bool RequiereRequisitos { get; set; }
    public bool RequiereDeclaracionInversion { get; set; }
    public bool Activo { get; set; } = true;

    public List<Requisito> Requisitos { get; set; } = [];
}
