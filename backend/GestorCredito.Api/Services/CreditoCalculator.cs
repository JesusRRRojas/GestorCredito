using GestorCredito.Api.Models;

namespace GestorCredito.Api.Services;

public record CuotaActualResultado(int Numero, DateOnly FechaPago, decimal PagoTotal);

/// <summary>
/// Calcula el estado visible de un Crédito (Prospecto + su simulación aceptada) y su Cuota
/// Actual, sin persistir ningún campo nuevo (FR-011 a FR-013, spec 003; research.md §3, mismo
/// patrón de cálculo en lectura ya usado para el "paso actual" de la bandeja en
/// 002-seguimiento-avanzado-prospectos).
/// </summary>
public static class CreditoCalculator
{
    public static (string EstadoCredito, CuotaActualResultado? CuotaActual) Calcular(
        EstadoProspecto estado, IReadOnlyList<Cuota> cuotas)
    {
        var desembolsado = estado is EstadoProspecto.Desembolso or EstadoProspecto.Finalizado;
        if (!desembolsado)
        {
            return ("EnProceso", null);
        }

        var cuotaActual = cuotas
            .Where(c => !c.Pagada)
            .OrderBy(c => c.Numero)
            .FirstOrDefault();

        if (cuotaActual is null)
        {
            return ("Cancelado", null);
        }

        return ("Pendiente", new CuotaActualResultado(cuotaActual.Numero, cuotaActual.FechaPago, cuotaActual.PagoTotal));
    }

    /// <summary>
    /// Clasifica cada Cuota de una simulación aceptada como "Pagada", "Actual" (la de menor
    /// Numero entre las no pagadas) o "Futura" (el resto de las no pagadas), para el
    /// cronograma visual del Cajero (spec 004, FR-002; research.md §1). Reutiliza la misma
    /// regla de "primera no pagada por Numero" ya usada por <see cref="Calcular"/>.
    /// </summary>
    public static IReadOnlyDictionary<int, string> ClasificarCuotas(IReadOnlyList<Cuota> cuotas)
    {
        var numeroActual = cuotas
            .Where(c => !c.Pagada)
            .OrderBy(c => c.Numero)
            .Select(c => (int?)c.Numero)
            .FirstOrDefault();

        return cuotas.ToDictionary(
            c => c.Numero,
            c => c.Pagada ? "Pagada" : c.Numero == numeroActual ? "Actual" : "Futura");
    }
}
