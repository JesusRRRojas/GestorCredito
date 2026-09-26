using GestorCredito.Api.Models;
using GestorCredito.Api.Services;

namespace GestorCredito.Api.Tests;

public class EstadoCreditoTests
{
    private static Cuota Cuota(int numero, bool pagada) => new()
    {
        Numero = numero,
        FechaPago = new DateOnly(2026, 1, numero),
        PagoTotal = 100m + numero,
        Pagada = pagada
    };

    [Theory]
    [InlineData(EstadoProspecto.Simulacion)]
    [InlineData(EstadoProspecto.Evaluacion)]
    [InlineData(EstadoProspecto.Aprobacion)]
    [InlineData(EstadoProspecto.Observado)]
    public void Calcular_EnProceso_CuandoAunNoSeDesembolsa(EstadoProspecto estado)
    {
        var (estadoCredito, cuotaActual) = CreditoCalculator.Calcular(estado, [Cuota(1, false)]);

        Assert.Equal("EnProceso", estadoCredito);
        Assert.Null(cuotaActual);
    }

    [Theory]
    [InlineData(EstadoProspecto.Desembolso)]
    [InlineData(EstadoProspecto.Finalizado)]
    public void Calcular_Pendiente_ConCuotaActualDeMenorNumeroSinPagar(EstadoProspecto estado)
    {
        var cuotas = new List<Cuota> { Cuota(1, true), Cuota(2, false), Cuota(3, false) };

        var (estadoCredito, cuotaActual) = CreditoCalculator.Calcular(estado, cuotas);

        Assert.Equal("Pendiente", estadoCredito);
        Assert.NotNull(cuotaActual);
        Assert.Equal(2, cuotaActual!.Numero);
    }

    [Theory]
    [InlineData(EstadoProspecto.Desembolso)]
    [InlineData(EstadoProspecto.Finalizado)]
    public void Calcular_Cancelado_CuandoTodasLasCuotasEstanPagadas(EstadoProspecto estado)
    {
        var cuotas = new List<Cuota> { Cuota(1, true), Cuota(2, true) };

        var (estadoCredito, cuotaActual) = CreditoCalculator.Calcular(estado, cuotas);

        Assert.Equal("Cancelado", estadoCredito);
        Assert.Null(cuotaActual);
    }

    [Fact]
    public void ClasificarCuotas_MarcaPagadaActualYFutura()
    {
        var cuotas = new List<Cuota> { Cuota(1, true), Cuota(2, false), Cuota(3, false) };

        var estados = CreditoCalculator.ClasificarCuotas(cuotas);

        Assert.Equal("Pagada", estados[1]);
        Assert.Equal("Actual", estados[2]);
        Assert.Equal("Futura", estados[3]);
    }

    [Fact]
    public void ClasificarCuotas_SinCuotaActual_CuandoTodasEstanPagadas()
    {
        var cuotas = new List<Cuota> { Cuota(1, true), Cuota(2, true) };

        var estados = CreditoCalculator.ClasificarCuotas(cuotas);

        Assert.Equal("Pagada", estados[1]);
        Assert.Equal("Pagada", estados[2]);
        Assert.DoesNotContain("Actual", estados.Values);
        Assert.DoesNotContain("Futura", estados.Values);
    }
}
