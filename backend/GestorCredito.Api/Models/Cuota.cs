namespace GestorCredito.Api.Models;

public class Cuota
{
    public int Id { get; set; }
    public int SimulacionId { get; set; }
    public Simulacion? Simulacion { get; set; }
    public int Numero { get; set; }
    public DateOnly FechaPago { get; set; }
    public decimal SaldoInicial { get; set; }
    public decimal Amortizacion { get; set; }
    public decimal Interes { get; set; }
    public decimal Otros { get; set; }
    public decimal PagoTotal { get; set; }
    public decimal SaldoFinal { get; set; }
    public bool Pagada { get; set; }
    public DateTime? FechaPagoRealizado { get; set; }
    public string? NumeroOperacion { get; set; }
    public string? EvidenciaNombreArchivo { get; set; }
    public byte[]? EvidenciaContenido { get; set; }
}
