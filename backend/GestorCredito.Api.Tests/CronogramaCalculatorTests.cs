using GestorCredito.Api.Models;
using GestorCredito.Api.Services;

namespace GestorCredito.Api.Tests;

public class CronogramaCalculatorTests
{
    private readonly CronogramaCalculator _calculadora = new();

    [Fact]
    public void Calcular_InteresDecreceSobreSaldoInsoluto()
    {
        // 1000 al 2% mensual, 5 cuotas, sin cargo "Otros" — coincide con la sesión de
        // /speckit-clarify: el interés decrece cuota a cuota (no es flat/constante).
        var cuotas = _calculadora.Calcular(1000m, 2m, 5, DiaPago.Dia5, 0m, new DateOnly(2026, 1, 1));

        Assert.Equal(5, cuotas.Count);
        Assert.Equal(20.00m, cuotas[0].Interes);
        Assert.Equal(16.00m, cuotas[1].Interes);
        Assert.Equal(12.00m, cuotas[2].Interes);
        Assert.Equal(8.00m, cuotas[3].Interes);
        Assert.Equal(4.00m, cuotas[4].Interes);

        // El interés debe ser estrictamente decreciente (saldo insoluto, no flat).
        for (var i = 1; i < cuotas.Count; i++)
        {
            Assert.True(cuotas[i].Interes < cuotas[i - 1].Interes);
        }
    }

    [Fact]
    public void Calcular_AmortizacionConstanteYSaldoFinalCero()
    {
        var cuotas = _calculadora.Calcular(1000m, 2m, 5, DiaPago.Dia5, 1m, new DateOnly(2026, 1, 1));

        Assert.All(cuotas.Take(4), c => Assert.Equal(200.00m, c.Amortizacion));
        Assert.Equal(0m, cuotas[^1].SaldoFinal);
    }

    [Fact]
    public void Calcular_UltimaCuotaAjustaSaldoFinalACeroCuandoNoDivideExacto()
    {
        // 1000 entre 3 cuotas no es exacto (333.33...): la última cuota debe absorber
        // la diferencia para que el saldo final cierre en 0.00 (edge case del spec).
        var cuotas = _calculadora.Calcular(1000m, 2m, 3, DiaPago.Dia5, 0m, new DateOnly(2026, 1, 1));

        Assert.Equal(333.33m, cuotas[0].Amortizacion);
        Assert.Equal(333.33m, cuotas[1].Amortizacion);
        Assert.Equal(333.34m, cuotas[2].Amortizacion);
        Assert.Equal(0m, cuotas[2].SaldoFinal);
    }

    [Theory]
    [InlineData(2026, 1, 1, DiaPago.Dia5, 2026, 1, 5)]   // antes del día 5 -> mismo mes
    [InlineData(2026, 1, 10, DiaPago.Dia5, 2026, 2, 5)]  // después del día 5 -> mes siguiente
    [InlineData(2026, 1, 1, DiaPago.Dia25, 2026, 1, 25)]
    public void Calcular_PrimeraFechaPago_SegunDiaPagoYFechaBase(
        int anioBase, int mesBase, int diaBase, DiaPago diaPago,
        int anioEsperado, int mesEsperado, int diaEsperado)
    {
        var cuotas = _calculadora.Calcular(
            1000m, 2m, 1, diaPago, 0m, new DateOnly(anioBase, mesBase, diaBase));

        Assert.Equal(new DateOnly(anioEsperado, mesEsperado, diaEsperado), cuotas[0].FechaPago);
    }

    [Fact]
    public void Calcular_CuotasSiguientesSonMensualesEnElMismoDia()
    {
        var cuotas = _calculadora.Calcular(1000m, 2m, 3, DiaPago.Dia25, 0m, new DateOnly(2026, 1, 1));

        Assert.Equal(new DateOnly(2026, 1, 25), cuotas[0].FechaPago);
        Assert.Equal(new DateOnly(2026, 2, 25), cuotas[1].FechaPago);
        Assert.Equal(new DateOnly(2026, 3, 25), cuotas[2].FechaPago);
    }
}
