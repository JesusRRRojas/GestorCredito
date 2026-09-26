using System.Text.Json.Serialization;
using GestorCredito.Api.Data;
using GestorCredito.Api.Endpoints;
using GestorCredito.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Los enums se serializan como texto (ej. "PEN", "Aprobado") para que el frontend no
// dependa de valores numéricos arbitrarios.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException(
        "No se encontró la cadena de conexión 'ConnectionStrings:Default'. " +
        "Configúrala con `dotnet user-secrets` o una variable de entorno (ver quickstart.md).");

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddSingleton<IMockRiesgoService, MockRiesgoService>();
builder.Services.AddSingleton<ICronogramaCalculator, CronogramaCalculator>();
builder.Services.AddSingleton<IPdfGenerator, PdfGenerator>();

builder.Services.AddCors(options =>
{
    // Sin autenticación de servidor (research.md §4): se habilita CORS abierto para que
    // el frontend Angular en desarrollo local pueda consumir la API.
    options.AddDefaultPolicy(policy => policy
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader());
});

var app = builder.Build();

app.UseCors();

app.MapGet("/", () => "GestorCredito.Api");

app.MapTiposEndpoints();
app.MapProductosEndpoints();
app.MapRequisitosEndpoints();
app.MapUsuariosEndpoints();
app.MapProspectosEndpoints();
app.MapCuentasInternasEndpoints();
app.MapCreditosEndpoints();

app.Run();
