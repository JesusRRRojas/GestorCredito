namespace GestorCredito.Api.Models;

public enum RolUsuario
{
    Administrador,
    Asesor,
    Aprobador,
    Cajero
}

public enum Moneda
{
    PEN
}

public enum FrecuenciaPago
{
    Mensual
}

public enum DiaPago
{
    Dia5 = 5,
    Dia25 = 25
}

public enum EstadoProspecto
{
    Simulacion,
    Evaluacion,
    Aprobacion,
    Observado,
    Rechazado,
    Desembolso,
    Finalizado
}

public enum TipoDocumentoGenerado
{
    Cronograma,
    AprobacionFinal
}

public enum TipoCuentaDesembolso
{
    Interna,
    Externa
}

public enum OrigenFondos
{
    Ahorros,
    Herencia,
    VentaActivo,
    ActividadEmpresarial,
    Otro
}
