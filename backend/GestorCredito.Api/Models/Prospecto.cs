namespace GestorCredito.Api.Models;

public class Prospecto
{
    public int Id { get; set; }
    public int TipoDocumentoId { get; set; }
    public TipoDocumento? TipoDocumento { get; set; }
    public required string NumeroDocumento { get; set; }
    public int ResultadoMock { get; set; }
    public int? ProductoId { get; set; }
    public Producto? Producto { get; set; }
    public string? Nombres { get; set; }
    public string? Apellidos { get; set; }
    public string? Direccion { get; set; }
    public TipoCuentaDesembolso? TipoCuentaDesembolso { get; set; }
    public int? CuentaBancariaInternaId { get; set; }
    public CuentaBancariaInterna? CuentaBancariaInterna { get; set; }
    public string? CuentaExternaBanco { get; set; }
    public string? CuentaExternaCCI { get; set; }
    public string? ComentarioAprobador { get; set; }
    public EstadoProspecto Estado { get; set; } = EstadoProspecto.Simulacion;
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    public DateTime? FechaCierre { get; set; }

    public List<Simulacion> Simulaciones { get; set; } = [];
    public List<DocumentoAdjunto> DocumentosAdjuntos { get; set; } = [];
    public List<DocumentoGenerado> DocumentosGenerados { get; set; } = [];
    public DeclaracionInversion? DeclaracionInversion { get; set; }

    public static readonly EstadoProspecto[] EstadosCerrados =
        [EstadoProspecto.Rechazado, EstadoProspecto.Finalizado];
}
