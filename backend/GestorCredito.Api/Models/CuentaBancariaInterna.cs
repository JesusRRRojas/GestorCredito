namespace GestorCredito.Api.Models;

public class CuentaBancariaInterna
{
    public int Id { get; set; }
    public int TipoDocumentoId { get; set; }
    public required string NumeroDocumento { get; set; }
    public required string NumeroCuenta { get; set; }
    public Moneda Moneda { get; set; } = Moneda.PEN;
    public bool Activo { get; set; } = true;
    public DateTime FechaRegistro { get; set; } = DateTime.Now;
}
