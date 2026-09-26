namespace GestorCredito.Api.Services;

/// <summary>Valida el Código de Cuenta Interbancario (CCI) de una cuenta externa (FR-009,
/// spec 002; mismo formato que FR-018 de 001-gestion-creditos).</summary>
public static class CuentaBancariaValidator
{
    public static bool EsCciValido(string? cci) =>
        !string.IsNullOrEmpty(cci) && cci.Length == 20 && cci.All(char.IsDigit);
}
