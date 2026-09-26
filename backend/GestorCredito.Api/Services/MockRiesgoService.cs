namespace GestorCredito.Api.Services;

public interface IMockRiesgoService
{
    int Evaluar();
}

/// <summary>
/// FR-007: genera un resultado de riesgo simulado (1 a 5) sin depender de ningún dato
/// del prospecto ni de una integración externa real (Reniec/Equifax quedan fuera de
/// alcance de esta versión).
/// </summary>
public class MockRiesgoService : IMockRiesgoService
{
    public int Evaluar() => Random.Shared.Next(1, 6);
}
