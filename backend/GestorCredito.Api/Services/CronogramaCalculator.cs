using GestorCredito.Api.Models;

namespace GestorCredito.Api.Services;

public record CuotaCalculada(
    int Numero,
    DateOnly FechaPago,
    decimal SaldoInicial,
    decimal Amortizacion,
    decimal Interes,
    decimal Otros,
    decimal PagoTotal,
    decimal SaldoFinal);

public interface ICronogramaCalculator
{
    List<CuotaCalculada> Calcular(
        decimal monto,
        decimal tasaPeriodicaPorcentaje,
        int plazo,
        DiaPago diaPago,
        decimal cargoOtrosPorCuota,
        DateOnly fechaBase);
}

/// <summary>
/// FR-013/FR-014: amortización de capital constante (Monto / Plazo, ajustada en la
/// última cuota para que el saldo final sea exactamente 0.00) e interés calculado sobre
/// el saldo insoluto de cada cuota (decreciente), tal como se definió en la sesión de
/// /speckit-clarify (research.md §6).
/// </summary>
public class CronogramaCalculator : ICronogramaCalculator
{
    public List<CuotaCalculada> Calcular(
        decimal monto,
        decimal tasaPeriodicaPorcentaje,
        int plazo,
        DiaPago diaPago,
        decimal cargoOtrosPorCuota,
        DateOnly fechaBase)
    {
        var tasaPeriodica = tasaPeriodicaPorcentaje / 100m;
        var amortizacionBase = Math.Round(monto / plazo, 2, MidpointRounding.AwayFromZero);

        var cuotas = new List<CuotaCalculada>(plazo);
        var saldoInicial = monto;
        var fechaPago = CalcularPrimeraFechaPago(fechaBase, diaPago);

        for (var numero = 1; numero <= plazo; numero++)
        {
            var esUltimaCuota = numero == plazo;
            var amortizacion = esUltimaCuota ? saldoInicial : amortizacionBase;
            var interes = Math.Round(saldoInicial * tasaPeriodica, 2, MidpointRounding.AwayFromZero);
            var pagoTotal = amortizacion + interes + cargoOtrosPorCuota;
            var saldoFinal = esUltimaCuota ? 0m : saldoInicial - amortizacion;

            cuotas.Add(new CuotaCalculada(
                numero, fechaPago, saldoInicial, amortizacion, interes, cargoOtrosPorCuota,
                pagoTotal, saldoFinal));

            saldoInicial = saldoFinal;
            fechaPago = fechaPago.AddMonths(1);
        }

        return cuotas;
    }

    private static DateOnly CalcularPrimeraFechaPago(DateOnly fechaBase, DiaPago diaPago)
    {
        var dia = (int)diaPago;
        // La primera cuota cae en la próxima ocurrencia del día de pago después de la
        // fecha base (mismo mes si el día aún no llegó, o el mes siguiente en caso
        // contrario). Los días 5 y 25 existen en todos los meses, por lo que no se
        // requiere ajuste por meses cortos o años bisiestos (ver CL1 del spec).
        var candidato = new DateOnly(fechaBase.Year, fechaBase.Month, dia);
        return fechaBase.Day < dia ? candidato : candidato.AddMonths(1);
    }
}
