using System.Globalization;
using GestorCredito.Api.Data;
using GestorCredito.Api.Models;
using GestorCredito.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace GestorCredito.Api.Endpoints;

public record ValidacionRequestDto(int TipoDocumentoId, string NumeroDocumento);
public record ValidacionResponseDto(int? ProspectoId, int ResultadoMock, bool Aprobado);

public record CuotaDto(
    int Numero, DateOnly FechaPago, decimal SaldoInicial, decimal Amortizacion,
    decimal Interes, decimal Otros, decimal PagoTotal, decimal SaldoFinal);

public record SimulacionRequestDto(int ProductoId, decimal Monto, decimal Tasa, int Plazo);
public record SimulacionResponseDto(int ProductoId, decimal Monto, decimal Tasa, int Plazo, List<CuotaDto> Cuotas);

public record CuentaExternaDto(string Banco, string Cci);

public record OnboardingDto(
    string Nombres, string Apellidos, string Direccion,
    TipoCuentaDesembolso TipoCuentaDesembolso, int? CuentaBancariaInternaId,
    CuentaExternaDto? CuentaExterna);

public record RequisitoConEstadoDto(int RequisitoId, string Nombre, bool Adjuntado);

public record DecisionDto(string Decision, string? Comentario);

public record DeclaracionInversionDto(decimal MontoInvertir, OrigenFondos OrigenFondos, string? Detalle);

public record ProspectoResumenDto(
    int Id, string NumeroDocumento, string? ProductoNombre, EstadoProspecto Estado,
    string? NombreCompleto, DateTime? FechaCierre);

public record ProspectoDetalleDto(
    int Id, int TipoDocumentoId, string NumeroDocumento, int? ProductoId,
    string? ProductoNombre, bool ProductoRequiereRequisitos,
    bool ProductoRequiereDeclaracionInversion, string? Nombres,
    string? Apellidos, string? Direccion, TipoCuentaDesembolso? TipoCuentaDesembolso,
    int? CuentaBancariaInternaId, string? CuentaExternaBanco, string? CuentaExternaCCI,
    EstadoProspecto Estado, DateTime FechaCreacion, DateTime? FechaCierre,
    decimal? MontoSolicitado);

/// <summary>Fila de la bandeja del Asesor (FR-001). Ver research.md §1 para el algoritmo de
/// "paso actual".</summary>
public record BandejaItemDto(
    int ProspectoId, string? NombreCliente, string TipoDocumento, string NumeroDocumento,
    string EstadoResumen, string? PasoActual, string? ComentarioAprobador);

/// <summary>
/// Endpoints del macro-flujo del Prospecto: validación de riesgo (FR-006 a FR-010),
/// simulación (FR-011 a FR-016), onboarding y requisitos (FR-017 a FR-022), aprobación
/// (FR-023 a FR-026) y desembolso (FR-027/FR-028). El historial de créditos por cliente
/// (antes FR-029) se resolvió con `GET /api/creditos` (spec 003, research.md §2).
/// </summary>
public static class ProspectosEndpoints
{
    public static void MapProspectosEndpoints(this WebApplication app)
    {
        var grupo = app.MapGroup("/api/prospectos").WithTags("Prospectos");

        // ---- Validación de riesgo (Pantalla 1) ----
        grupo.MapPost("/validaciones", async (
            ValidacionRequestDto dto, AppDbContext db, IMockRiesgoService mockRiesgo) =>
        {
            var existeActivo = await db.Prospectos.AnyAsync(p =>
                p.NumeroDocumento == dto.NumeroDocumento &&
                !Prospecto.EstadosCerrados.Contains(p.Estado));
            if (existeActivo)
            {
                var activo = await db.Prospectos.FirstAsync(p =>
                    p.NumeroDocumento == dto.NumeroDocumento &&
                    !Prospecto.EstadosCerrados.Contains(p.Estado));
                return Results.Conflict(new { error = "PROSPECTO_ACTIVO_EXISTENTE", prospectoId = activo.Id });
            }

            var resultadoMock = mockRiesgo.Evaluar();
            if (resultadoMock >= 4)
            {
                // FR-008: rechazo Mock; no se persiste nada, permite reintento inmediato.
                return Results.Ok(new ValidacionResponseDto(null, resultadoMock, false));
            }

            var prospecto = new Prospecto
            {
                TipoDocumentoId = dto.TipoDocumentoId,
                NumeroDocumento = dto.NumeroDocumento,
                ResultadoMock = resultadoMock,
                Estado = EstadoProspecto.Simulacion
            };
            db.Prospectos.Add(prospecto);
            await db.SaveChangesAsync();
            return Results.Ok(new ValidacionResponseDto(prospecto.Id, resultadoMock, true));
        });

        // ---- Detalle de Prospecto (usado por las pantallas siguientes) ----
        grupo.MapGet("/{id:int}", async (int id, AppDbContext db) =>
        {
            var p = await db.Prospectos
                .Include(x => x.Producto)
                .Include(x => x.Simulaciones)
                .FirstOrDefaultAsync(x => x.Id == id);
            return p is null ? Results.NotFound() : Results.Ok(AMapaDetalle(p));
        });

        grupo.MapGet("/", async (EstadoProspecto? estado, AppDbContext db) =>
        {
            var query = db.Prospectos.Include(p => p.Producto).AsQueryable();
            if (estado is not null)
            {
                query = query.Where(p => p.Estado == estado);
            }
            return (await query.ToListAsync()).Select(AMapaResumen);
        });

        // ---- Bandeja del Asesor (FR-001 a FR-003, spec 002) ----
        grupo.MapGet("/bandeja", async (AppDbContext db) =>
        {
            var prospectos = await db.Prospectos
                .Include(p => p.Producto)
                .Include(p => p.TipoDocumento)
                .OrderByDescending(p => p.FechaCreacion)
                .ToListAsync();

            var resultado = new List<BandejaItemDto>();
            foreach (var p in prospectos)
            {
                resultado.Add(await AMapaBandejaAsync(p, db));
            }
            return Results.Ok(resultado);
        });

        // ---- Simulación (Pantalla 2) ----
        grupo.MapPost("/{id:int}/simulaciones", async (
            int id, SimulacionRequestDto dto, AppDbContext db, ICronogramaCalculator calculadora) =>
        {
            var producto = await db.Productos.FindAsync(dto.ProductoId);
            if (producto is null) return Results.NotFound(new { error = "Producto no encontrado." });

            var error = ValidarRangoSimulacion(dto, producto);
            if (error is not null) return Results.BadRequest(new { error });

            var cuotas = calculadora.Calcular(
                dto.Monto, dto.Tasa, dto.Plazo, producto.DiaPago, producto.CargoOtrosPorCuota,
                DateOnly.FromDateTime(DateTime.Now));

            var respuesta = new SimulacionResponseDto(
                dto.ProductoId, dto.Monto, dto.Tasa, dto.Plazo,
                cuotas.Select(c => new CuotaDto(
                    c.Numero, c.FechaPago, c.SaldoInicial, c.Amortizacion, c.Interes, c.Otros,
                    c.PagoTotal, c.SaldoFinal)).ToList());

            return Results.Ok(respuesta);
        });

        grupo.MapPost("/{id:int}/simulaciones/aceptar", async (
            int id, SimulacionRequestDto dto, AppDbContext db, ICronogramaCalculator calculadora) =>
        {
            var prospecto = await db.Prospectos.FindAsync(id);
            if (prospecto is null) return Results.NotFound();
            if (prospecto.Estado != EstadoProspecto.Simulacion)
            {
                return Results.BadRequest(new { error = "El prospecto ya no está en fase de simulación." });
            }

            var producto = await db.Productos.FindAsync(dto.ProductoId);
            if (producto is null) return Results.NotFound(new { error = "Producto no encontrado." });

            var error = ValidarRangoSimulacion(dto, producto);
            if (error is not null) return Results.BadRequest(new { error });

            var cuotasCalculadas = calculadora.Calcular(
                dto.Monto, dto.Tasa, dto.Plazo, producto.DiaPago, producto.CargoOtrosPorCuota,
                DateOnly.FromDateTime(DateTime.Now));

            var simulacion = new Simulacion
            {
                ProspectoId = id,
                ProductoId = dto.ProductoId,
                Monto = dto.Monto,
                Tasa = dto.Tasa,
                Plazo = dto.Plazo,
                Aceptada = true,
                FechaAceptacion = DateTime.Now,
                Cuotas = cuotasCalculadas.Select(c => new Cuota
                {
                    Numero = c.Numero,
                    FechaPago = c.FechaPago,
                    SaldoInicial = c.SaldoInicial,
                    Amortizacion = c.Amortizacion,
                    Interes = c.Interes,
                    Otros = c.Otros,
                    PagoTotal = c.PagoTotal,
                    SaldoFinal = c.SaldoFinal
                }).ToList()
            };

            db.Simulaciones.Add(simulacion);
            prospecto.ProductoId = dto.ProductoId;
            prospecto.Estado = EstadoProspecto.Evaluacion;
            await db.SaveChangesAsync();

            return Results.Ok(new { simulacionId = simulacion.Id });
        });

        grupo.MapGet("/{id:int}/simulaciones/actual/pdf", async (int id, AppDbContext db, IPdfGenerator pdf) =>
        {
            var (prospecto, producto, simulacion, error) = await CargarSimulacionAceptada(id, db);
            if (error is not null) return error;

            var bytes = pdf.GenerarCronograma(prospecto!, producto!, simulacion!);
            return Results.File(bytes, "application/pdf", "cronograma.pdf");
        });

        // ---- Onboarding (Pantalla 3) ----
        // Reutilizado también para: corregir tras "Observado" y para la edición desde la
        // bandeja (Historia 5, spec 002 — research.md §3).
        grupo.MapPut("/{id:int}/onboarding", async (int id, OnboardingDto dto, AppDbContext db) =>
        {
            var prospecto = await db.Prospectos
                .Include(p => p.Producto)
                .Include(p => p.Simulaciones)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (prospecto is null) return Results.NotFound();

            // FR-016/FR-017 (spec 002): no se puede editar mientras está en Aprobación ni en
            // un estado cerrado (Rechazado/Finalizado).
            if (prospecto.Estado == EstadoProspecto.Aprobacion || Prospecto.EstadosCerrados.Contains(prospecto.Estado))
            {
                return Results.Conflict(new { error = "EDICION_NO_PERMITIDA_EN_ESTADO_ACTUAL" });
            }

            if (dto.TipoCuentaDesembolso == TipoCuentaDesembolso.Interna)
            {
                if (dto.CuentaBancariaInternaId is null)
                {
                    return Results.BadRequest(new { error = "Debe seleccionar una Cuenta Interna." });
                }
                var cuenta = await db.CuentasBancariasInternas.FindAsync(dto.CuentaBancariaInternaId.Value);
                if (cuenta is null || cuenta.TipoDocumentoId != prospecto.TipoDocumentoId ||
                    cuenta.NumeroDocumento != prospecto.NumeroDocumento)
                {
                    return Results.BadRequest(new { error = "La Cuenta Interna no pertenece a este cliente." });
                }
                prospecto.CuentaBancariaInternaId = cuenta.Id;
                prospecto.CuentaExternaBanco = null;
                prospecto.CuentaExternaCCI = null;
            }
            else
            {
                if (dto.CuentaExterna is null || string.IsNullOrWhiteSpace(dto.CuentaExterna.Banco) ||
                    !CuentaBancariaValidator.EsCciValido(dto.CuentaExterna.Cci))
                {
                    return Results.BadRequest(new
                    {
                        error = "Debe indicar el Banco y un CCI numérico de exactamente 20 dígitos."
                    });
                }
                prospecto.CuentaExternaBanco = dto.CuentaExterna.Banco;
                prospecto.CuentaExternaCCI = dto.CuentaExterna.Cci;
                prospecto.CuentaBancariaInternaId = null;
            }

            prospecto.Nombres = dto.Nombres;
            prospecto.Apellidos = dto.Apellidos;
            prospecto.Direccion = dto.Direccion;
            prospecto.TipoCuentaDesembolso = dto.TipoCuentaDesembolso;

            // FR-019 (001): al corregir tras "Observado", el prospecto vuelve a Evaluación para
            // pasar de nuevo por Declaración de Inversión/Requisitos/Aprobación. En cualquier
            // otro estado permitido (Evaluacion, Desembolso) no se cambia el Estado.
            if (prospecto.Estado == EstadoProspecto.Observado)
            {
                prospecto.Estado = EstadoProspecto.Evaluacion;
                // El comentario del Aprobador solo es relevante mientras el prospecto está
                // "Observado" (FR-014); al corregirse dejar de mostrarlo en el tooltip.
                prospecto.ComentarioAprobador = null;
            }

            await db.SaveChangesAsync();
            return Results.Ok(AMapaDetalle(prospecto));
        });

        // ---- Declaración de Inversión (Historia 3, spec 002) ----
        grupo.MapGet("/{id:int}/declaracion-inversion", async (int id, AppDbContext db) =>
        {
            var declaracion = await db.DeclaracionesInversion.FirstOrDefaultAsync(d => d.ProspectoId == id);
            return declaracion is null
                ? Results.NotFound()
                : Results.Ok(new DeclaracionInversionDto(declaracion.MontoInvertir, declaracion.OrigenFondos, declaracion.Detalle));
        });

        grupo.MapPut("/{id:int}/declaracion-inversion", async (int id, DeclaracionInversionDto dto, AppDbContext db) =>
        {
            var prospecto = await db.Prospectos
                .Include(p => p.Producto)
                .Include(p => p.Simulaciones)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (prospecto is null) return Results.NotFound();
            if (prospecto.Producto is null || !prospecto.Producto.RequiereDeclaracionInversion)
            {
                return Results.Conflict(new { error = "Este Producto no requiere Declaración de Inversión." });
            }
            if (dto.MontoInvertir <= 0)
            {
                return Results.BadRequest(new { error = "El Monto a Invertir debe ser mayor a 0." });
            }
            if (dto.Detalle is not null && dto.Detalle.Length > 500)
            {
                return Results.BadRequest(new { error = "El Detalle no puede superar los 500 caracteres." });
            }

            // FR-018 (spec 003): el Monto a Invertir no puede superar el Monto Solicitado
            // (límite inclusive, research.md §7).
            var montoSolicitado = prospecto.Simulaciones.FirstOrDefault(s => s.Aceptada)?.Monto;
            if (montoSolicitado is not null && dto.MontoInvertir > montoSolicitado)
            {
                return Results.BadRequest(new { error = "MONTO_INVERTIR_SUPERA_MONTO_SOLICITADO" });
            }

            var declaracion = await db.DeclaracionesInversion.FirstOrDefaultAsync(d => d.ProspectoId == id);
            if (declaracion is null)
            {
                declaracion = new DeclaracionInversion { ProspectoId = id };
                db.DeclaracionesInversion.Add(declaracion);
            }
            declaracion.MontoInvertir = dto.MontoInvertir;
            declaracion.OrigenFondos = dto.OrigenFondos;
            declaracion.Detalle = dto.Detalle;

            await db.SaveChangesAsync();
            return Results.Ok(new DeclaracionInversionDto(declaracion.MontoInvertir, declaracion.OrigenFondos, declaracion.Detalle));
        });

        // ---- Requisitos (Pantalla 4) ----
        grupo.MapGet("/{id:int}/requisitos", async (int id, AppDbContext db) =>
        {
            var prospecto = await db.Prospectos.Include(p => p.Producto).FirstOrDefaultAsync(p => p.Id == id);
            if (prospecto is null) return Results.NotFound();
            if (prospecto.Producto is null || !prospecto.Producto.RequiereRequisitos)
            {
                return Results.Ok(new List<RequisitoConEstadoDto>());
            }

            var requisitos = await db.Requisitos
                .Where(r => r.ProductoId == prospecto.ProductoId && r.Activo)
                .ToListAsync();
            var adjuntadosIds = await db.DocumentosAdjuntos
                .Where(d => d.ProspectoId == id)
                .Select(d => d.RequisitoId)
                .ToListAsync();

            return Results.Ok(requisitos.Select(r =>
                new RequisitoConEstadoDto(r.Id, r.Nombre, adjuntadosIds.Contains(r.Id))));
        });

        grupo.MapPost("/{id:int}/requisitos/{requisitoId:int}/archivo", async (
            int id, int requisitoId, IFormFile archivo, AppDbContext db) =>
        {
            var esPdf = archivo.ContentType == "application/pdf" ||
                archivo.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
            if (!esPdf)
            {
                return Results.BadRequest(new { error = "Solo se aceptan archivos PDF." });
            }

            using var ms = new MemoryStream();
            await archivo.CopyToAsync(ms);

            var existente = await db.DocumentosAdjuntos.FirstOrDefaultAsync(d =>
                d.ProspectoId == id && d.RequisitoId == requisitoId);

            if (existente is not null)
            {
                existente.NombreArchivo = archivo.FileName;
                existente.Contenido = ms.ToArray();
                existente.FechaCarga = DateTime.Now;
            }
            else
            {
                db.DocumentosAdjuntos.Add(new DocumentoAdjunto
                {
                    ProspectoId = id,
                    RequisitoId = requisitoId,
                    NombreArchivo = archivo.FileName,
                    Contenido = ms.ToArray()
                });
            }

            await db.SaveChangesAsync();
            return Results.NoContent();
        }).DisableAntiforgery();

        grupo.MapPost("/{id:int}/requisitos/completar", async (int id, AppDbContext db, IPdfGenerator pdf) =>
        {
            var prospecto = await db.Prospectos.Include(p => p.Producto).FirstOrDefaultAsync(p => p.Id == id);
            if (prospecto is null) return Results.NotFound();
            if (prospecto.Producto is null) return Results.BadRequest(new { error = "El prospecto no tiene un Producto asignado." });

            // FR-005/FR-006 (spec 002): la Declaración de Inversión, si el Producto la
            // requiere, debe completarse antes de avanzar a Requisitos/Aprobación/Desembolso.
            if (prospecto.Producto.RequiereDeclaracionInversion &&
                !await db.DeclaracionesInversion.AnyAsync(d => d.ProspectoId == id))
            {
                return Results.BadRequest(new { error = "Falta completar la Declaración de Inversión." });
            }

            if (prospecto.Producto.RequiereRequisitos)
            {
                var requisitos = await db.Requisitos
                    .Where(r => r.ProductoId == prospecto.ProductoId && r.Activo)
                    .ToListAsync();

                // Edge case (spec.md): si el Producto no tiene Requisitos configurados, no
                // hay nada que completar; se bloquea el avance.
                if (requisitos.Count == 0)
                {
                    return Results.BadRequest(new
                    {
                        error = "El Producto no tiene Requisitos configurados; el Administrador debe configurar al menos uno."
                    });
                }

                var adjuntadosIds = await db.DocumentosAdjuntos
                    .Where(d => d.ProspectoId == id)
                    .Select(d => d.RequisitoId)
                    .ToListAsync();

                if (requisitos.Any(r => !adjuntadosIds.Contains(r.Id)))
                {
                    return Results.BadRequest(new { error = "Faltan Requisitos por adjuntar." });
                }

                prospecto.Estado = EstadoProspecto.Aprobacion;
            }
            else
            {
                // FR-022: Fast-Track — pasa directo a Desembolso y autogenera el PDF final.
                prospecto.Estado = EstadoProspecto.Desembolso;
                var (_, producto, simulacion, error) = await CargarSimulacionAceptada(id, db);
                if (error is null)
                {
                    var bytes = pdf.GenerarAprobacionFinal(prospecto, producto!, simulacion!);
                    db.DocumentosGenerados.Add(new DocumentoGenerado
                    {
                        ProspectoId = id,
                        Tipo = TipoDocumentoGenerado.AprobacionFinal,
                        Contenido = bytes
                    });
                }
            }

            await db.SaveChangesAsync();
            return Results.Ok(new { estado = prospecto.Estado.ToString() });
        });

        grupo.MapGet("/{id:int}/requisitos/archivos", async (int id, AppDbContext db) =>
        {
            var archivos = await db.DocumentosAdjuntos
                .Include(d => d.Requisito)
                .Where(d => d.ProspectoId == id)
                .Select(d => new { d.Id, d.RequisitoId, RequisitoNombre = d.Requisito!.Nombre, d.NombreArchivo, d.FechaCarga })
                .ToListAsync();
            return Results.Ok(archivos);
        });

        grupo.MapGet("/{id:int}/requisitos/{requisitoId:int}/archivo", async (
            int id, int requisitoId, AppDbContext db) =>
        {
            var archivo = await db.DocumentosAdjuntos.FirstOrDefaultAsync(d =>
                d.ProspectoId == id && d.RequisitoId == requisitoId);
            return archivo is null
                ? Results.NotFound()
                : Results.File(archivo.Contenido, "application/pdf", archivo.NombreArchivo);
        });

        // ---- Aprobación (Pantalla 5) ----
        grupo.MapPost("/{id:int}/decision", async (int id, DecisionDto dto, AppDbContext db) =>
        {
            if (dto.Decision is not ("Aprobado" or "Observado" or "Rechazado"))
            {
                return Results.BadRequest(new { error = "Decisión inválida." });
            }

            // FR-012/FR-013 (spec 002): comentario obligatorio (máx. 1000 caracteres) para
            // Observado/Rechazado; no se exige para Aprobado.
            if (dto.Decision is "Observado" or "Rechazado")
            {
                if (string.IsNullOrWhiteSpace(dto.Comentario))
                {
                    return Results.BadRequest(new { error = "Debe ingresar un comentario para Observar o Rechazar." });
                }
                if (dto.Comentario.Length > 1000)
                {
                    return Results.BadRequest(new { error = "El comentario no puede superar los 1000 caracteres." });
                }
            }

            var prospecto = await db.Prospectos.FindAsync(id);
            if (prospecto is null) return Results.NotFound();
            if (prospecto.Estado != EstadoProspecto.Aprobacion)
            {
                return Results.BadRequest(new { error = "El prospecto no está en fase de Aprobación." });
            }

            prospecto.Estado = dto.Decision switch
            {
                "Aprobado" => EstadoProspecto.Desembolso,
                "Observado" => EstadoProspecto.Observado,
                "Rechazado" => EstadoProspecto.Rechazado,
                _ => prospecto.Estado
            };
            prospecto.ComentarioAprobador = dto.Decision is "Observado" or "Rechazado" ? dto.Comentario : null;

            if (prospecto.Estado == EstadoProspecto.Rechazado)
            {
                prospecto.FechaCierre = DateTime.Now;
            }

            await db.SaveChangesAsync();
            return Results.Ok(new { estado = prospecto.Estado.ToString() });
        });

        // ---- Desembolso (Pantalla 6) ----
        grupo.MapGet("/{id:int}/aprobacion-final/pdf", async (int id, AppDbContext db, IPdfGenerator pdf) =>
        {
            var (prospecto, producto, simulacion, error) = await CargarSimulacionAceptada(id, db);
            if (error is not null) return error;

            var bytes = pdf.GenerarAprobacionFinal(prospecto!, producto!, simulacion!);
            db.DocumentosGenerados.Add(new DocumentoGenerado
            {
                ProspectoId = id,
                Tipo = TipoDocumentoGenerado.AprobacionFinal,
                Contenido = bytes
            });
            await db.SaveChangesAsync();

            return Results.File(bytes, "application/pdf", "aprobacion-final.pdf");
        });

        grupo.MapPost("/{id:int}/cerrar", async (int id, AppDbContext db) =>
        {
            var prospecto = await db.Prospectos.FindAsync(id);
            if (prospecto is null) return Results.NotFound();
            if (prospecto.Estado != EstadoProspecto.Desembolso)
            {
                return Results.BadRequest(new { error = "El prospecto no está en fase de Desembolso." });
            }

            prospecto.Estado = EstadoProspecto.Finalizado;
            prospecto.FechaCierre = DateTime.Now;
            await db.SaveChangesAsync();
            return Results.Ok(new { estado = prospecto.Estado.ToString() });
        });

    }

    private static string? ValidarRangoSimulacion(SimulacionRequestDto dto, Producto producto)
    {
        var ci = CultureInfo.InvariantCulture;
        if (dto.Monto < producto.MontoMin || dto.Monto > producto.MontoMax)
            return $"El Monto debe estar entre {producto.MontoMin.ToString("N2", ci)} y {producto.MontoMax.ToString("N2", ci)}.";
        if (dto.Tasa < producto.TasaMin || dto.Tasa > producto.TasaMax)
            return $"La Tasa debe estar entre {producto.TasaMin.ToString("N2", ci)}% y {producto.TasaMax.ToString("N2", ci)}%.";
        if (dto.Plazo < producto.PlazoMin || dto.Plazo > producto.PlazoMax)
            return $"El Plazo debe estar entre {producto.PlazoMin} y {producto.PlazoMax} cuotas.";
        return null;
    }

    private static async Task<(Prospecto? Prospecto, Producto? Producto, Simulacion? Simulacion, IResult? Error)>
        CargarSimulacionAceptada(int prospectoId, AppDbContext db)
    {
        var prospecto = await db.Prospectos
            .Include(p => p.CuentaBancariaInterna)
            .FirstOrDefaultAsync(p => p.Id == prospectoId);
        if (prospecto is null) return (null, null, null, Results.NotFound());

        var simulacion = await db.Simulaciones
            .Include(s => s.Cuotas)
            .Include(s => s.Producto)
            .FirstOrDefaultAsync(s => s.ProspectoId == prospectoId && s.Aceptada);
        if (simulacion is null || simulacion.Producto is null)
        {
            return (null, null, null, Results.BadRequest(new { error = "El prospecto no tiene una simulación aceptada." }));
        }

        return (prospecto, simulacion.Producto, simulacion, null);
    }

    private static ProspectoResumenDto AMapaResumen(Prospecto p) => new(
        p.Id, p.NumeroDocumento, p.Producto?.Nombre, p.Estado,
        string.IsNullOrWhiteSpace(p.Nombres) ? null : $"{p.Nombres} {p.Apellidos}", p.FechaCierre);

    private static ProspectoDetalleDto AMapaDetalle(Prospecto p) => new(
        p.Id, p.TipoDocumentoId, p.NumeroDocumento, p.ProductoId, p.Producto?.Nombre,
        p.Producto?.RequiereRequisitos ?? false, p.Producto?.RequiereDeclaracionInversion ?? false,
        p.Nombres, p.Apellidos, p.Direccion, p.TipoCuentaDesembolso, p.CuentaBancariaInternaId,
        p.CuentaExternaBanco, p.CuentaExternaCCI, p.Estado, p.FechaCreacion, p.FechaCierre,
        p.Simulaciones.FirstOrDefault(s => s.Aceptada)?.Monto);

    /// <summary>
    /// Calcula la fila de bandeja de un Prospecto (research.md §1, spec 002): mapea el
    /// `Estado` interno a un `EstadoResumen` visible (Bloqueado, EnProceso, Aprobacion,
    /// Desembolsado, Observado) y, para los estados "en proceso", calcula el `PasoActual`
    /// (pantalla exacta donde continuar) a partir de los datos ya presentes, sin agregar
    /// nuevos valores al enum `EstadoProspecto`.
    /// </summary>
    private static async Task<BandejaItemDto> AMapaBandejaAsync(Prospecto p, AppDbContext db)
    {
        var nombreCliente = string.IsNullOrWhiteSpace(p.Nombres) ? null : $"{p.Nombres} {p.Apellidos}";
        var tipoDocumentoNombre = p.TipoDocumento?.Nombre ?? string.Empty;

        string estadoResumen;
        string? pasoActual = null;

        switch (p.Estado)
        {
            case EstadoProspecto.Rechazado:
                estadoResumen = "Bloqueado";
                break;
            case EstadoProspecto.Finalizado:
                estadoResumen = "Desembolsado";
                break;
            case EstadoProspecto.Aprobacion:
                estadoResumen = "Aprobacion";
                break;
            case EstadoProspecto.Observado:
                estadoResumen = "Observado";
                pasoActual = "Onboarding";
                break;
            case EstadoProspecto.Simulacion:
                estadoResumen = "EnProceso";
                pasoActual = "Simulacion";
                break;
            case EstadoProspecto.Desembolso:
                estadoResumen = "EnProceso";
                pasoActual = "Desembolso";
                break;
            case EstadoProspecto.Evaluacion:
                estadoResumen = "EnProceso";
                pasoActual = await CalcularPasoEvaluacionAsync(p, db);
                break;
            default:
                estadoResumen = "EnProceso";
                break;
        }

        return new BandejaItemDto(
            p.Id, nombreCliente, tipoDocumentoNombre, p.NumeroDocumento, estadoResumen,
            pasoActual, p.ComentarioAprobador);
    }

    private static async Task<string> CalcularPasoEvaluacionAsync(Prospecto p, AppDbContext db)
    {
        var onboardingCompleto = !string.IsNullOrWhiteSpace(p.Nombres) &&
            !string.IsNullOrWhiteSpace(p.Apellidos) && !string.IsNullOrWhiteSpace(p.Direccion) &&
            p.TipoCuentaDesembolso is not null;
        if (!onboardingCompleto) return "Onboarding";

        if (p.Producto?.RequiereDeclaracionInversion == true)
        {
            var tieneDeclaracion = await db.DeclaracionesInversion.AnyAsync(d => d.ProspectoId == p.Id);
            if (!tieneDeclaracion) return "DeclaracionInversion";
        }

        // Tanto si faltan Requisitos por adjuntar como si el Producto no los requiere
        // (Fast-Track), el siguiente paso visible para el Asesor es la pantalla de
        // Requisitos: ella misma completa automáticamente el Fast-Track (ver
        // RequisitosComponent.ngOnInit en el frontend).
        return "Requisitos";
    }
}
