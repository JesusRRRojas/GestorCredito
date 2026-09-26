using GestorCredito.Api.Services;

namespace GestorCredito.Api.Tests;

public class CuentaBancariaValidationTests
{
    [Theory]
    [InlineData("00212345678901234567", true)] // exactamente 20 dígitos
    [InlineData("0021234567890123456", false)] // 19 dígitos
    [InlineData("002123456789012345678", false)] // 21 dígitos
    [InlineData("0021234567890123456A", false)] // no numérico
    [InlineData("", false)]
    [InlineData(null, false)]
    public void EsCciValido_SoloAceptaExactamente20DigitosNumericos(string? cci, bool esperado)
    {
        Assert.Equal(esperado, CuentaBancariaValidator.EsCciValido(cci));
    }
}
