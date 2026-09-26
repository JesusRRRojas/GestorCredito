# Implementation Plan: Cronograma Visual y Comprobante de Pago en Bandeja del Cajero

**Branch**: `004-cronograma-comprobante-pago` | **Date**: 2026-09-26 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/004-cronograma-comprobante-pago/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Extender la bandeja del Cajero ya construida en `003-pagos-cajero-mejoras-ui` con: (1) al
seleccionar un crédito, mostrar su cronograma completo de cuotas con un indicador visual de
estado por cuota (pagada / actual / futura) y un resumen de avance; (2) al confirmar el pago de
la Cuota Actual, exigir un Número de Operación como evidencia y permitir adjuntar
opcionalmente un archivo de evidencia. Se reutiliza el mismo stack y los mismos patrones ya
usados en `001`/`002`/`003` (Minimal APIs, EF Core Code-First, Angular + Bootstrap) y, en
particular, el patrón de subida de archivos ya existente para Requisitos
(`IFormFile` + almacenamiento de `byte[]` en la base de datos), sin introducir dependencias
nuevas.

## Technical Context

**Language/Version**: C# 13 / .NET 10 (backend, ya en uso); TypeScript + Angular 19 (frontend,
ya en uso)

**Primary Dependencies**: ASP.NET Core Minimal APIs, Entity Framework Core (Code-First, SQL
Server provider), Angular, Bootstrap 5 — mismas dependencias que `001`/`002`/`003`, sin agregar
ninguna nueva.

**Storage**: Mismo SQL Server local (`dbGestorCredito`) vía EF Core; se agregan columnas
`NumeroOperacion`, `EvidenciaNombreArchivo` y `EvidenciaContenido` a `Cuota` mediante una nueva
migración (ver `research.md` §2 y `data-model.md`).

**Testing**: xUnit, acotado a la clasificación de estado por cuota (pagada/actual/futura) y a
la validación del Número de Operación obligatorio; validación funcional principal mediante
`quickstart.md` (recorrido manual, Principio V de la constitución).

**Target Platform**: Misma aplicación web local (navegador de escritorio moderno contra backend
Kestrel local); sin cambios de plataforma.

**Project Type**: Web (frontend Angular + backend .NET Web API) — se extienden los mismos dos
proyectos (`backend/`, `frontend/`) de `001`/`002`/`003`, sin crear proyectos nuevos.

**Performance Goals**: Igual que features anteriores — uso interno de pocos usuarios
concurrentes, sin metas de throughput específicas.

**Constraints**: Sin autenticación real (se mantiene el selector de rol); tamaño máximo de
archivo de evidencia 5 MB, formatos permitidos JPG/PNG/PDF (spec, Assumptions); cadena de
conexión y secretos fuera del código versionado (Principio VI), sin cambios respecto a
features anteriores.

**Scale/Scope**: 1 endpoint nuevo (cronograma detallado de un crédito) + 1 endpoint modificado
(registro de pago, ahora con Número de Operación obligatorio y archivo opcional) + 1 endpoint
nuevo de descarga de evidencia + extensión visual de la pantalla ya existente de la bandeja del
Cajero (sin pantalla nueva).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principio | Evaluación | Cómo se cumple |
|-----------|------------|-----------------|
| I. Simplicidad Ante Todo | ✅ PASS | El estado por cuota (pagada/actual/futura) se calcula en lectura extendiendo `CreditoCalculator` (ya existente), sin nueva tabla; la evidencia de pago se guarda como columnas nuevas directamente en `Cuota` (mismo patrón simple que `Pagada`/`FechaPagoRealizado` de `003`) en vez de crear una entidad `EvidenciaPago` separada. Se reutiliza el patrón de subida de archivos (`IFormFile` + `byte[]`) ya usado en Requisitos, sin introducir una librería de almacenamiento nueva. |
| II. Idioma y Mercado (Perú) | ✅ PASS | UI en español; fechas y montos con los mismos pipes `fechaPe`/`monedaPe` ya existentes. Sin cambios de moneda o idioma. |
| III. Cero Alcance Fantasma | ✅ PASS | Cada endpoint/campo nuevo deriva uno a uno de los FR-001 a FR-009 del spec `004`; no se agrega edición/reemplazo de evidencia, ni reversión de pago (declarados fuera de alcance en las Assumptions del spec). |
| IV. Propuesta Antes de Construir | ✅ PASS | El spec `004` ya se validó y no dejó `[NEEDS CLARIFICATION]` pendientes; los límites de archivo se documentaron como supuestos razonables en el spec, no como decisiones nuevas tomadas aquí. |
| V. Verificable por una Persona No Técnica | ✅ PASS | `quickstart.md` documenta un recorrido manual por ambas historias (ver cronograma con estado por cuota; registrar pago con y sin evidencia), sin necesidad de leer código. |
| VI. Datos del Usuario y Seguridad de Credenciales | ✅ PASS | No se introduce ningún secreto nuevo; el archivo de evidencia se almacena en la misma base de datos ya usada para Requisitos, sin credenciales ni claves en el código. |

**Resultado**: Ninguna violación que requiera justificación en "Complexity Tracking".

## Project Structure

### Documentation (this feature)

```text
specs/004-cronograma-comprobante-pago/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/
│   └── api-contracts.md # Phase 1 output (/speckit-plan command)
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
backend/
├── GestorCredito.Api/
│   ├── Data/
│   │   ├── AppDbContext.cs             # Sin cambios de configuración adicionales (columnas
│   │   │                               #   simples, sin relaciones nuevas)
│   │   └── Migrations/                 # Nueva migración: NumeroOperacion,
│   │                                   #   EvidenciaNombreArchivo, EvidenciaContenido en Cuota
│   ├── Models/
│   │   └── Cuota.cs                    # Extiende: NumeroOperacion (string?),
│   │                                   #   EvidenciaNombreArchivo (string?),
│   │                                   #   EvidenciaContenido (byte[]?)
│   ├── Services/
│   │   └── CreditoCalculator.cs        # Extiende: clasificación por cuota
│   │                                   #   (Pagada/Actual/Futura) además del resumen ya existente
│   └── Endpoints/
│       └── CreditosEndpoints.cs        # Nuevo: GET /api/creditos/{id}/cronograma
│                                       #   Modifica: POST /api/creditos/{id}/pagar-cuota
│                                       #   (multipart/form-data: numeroOperacion + archivo?)
│                                       #   Nuevo: GET /api/creditos/{id}/cuotas/{numero}/evidencia
└── GestorCredito.Api.Tests/
    └── EstadoCreditoTests.cs           # Extiende: clasificación por cuota y validación de
                                        #   Número de Operación obligatorio

frontend/
├── src/app/
│   ├── core/
│   │   └── services/
│   │       └── creditos.service.ts     # Extiende: obtenerCronograma(), pagarCuota() con
│   │                                   #   FormData (numeroOperacion + archivo opcional),
│   │                                   #   urlEvidencia()
│   └── features/
│       └── creditos-cajero/            # Extiende (Historias 1 y 2): tabla de cronograma con
│                                       #   estado por cuota + formulario de pago con Número de
│                                       #   Operación y adjunto opcional
└── (estructura estándar de Angular CLI, ya presente)
```

**Structure Decision**: Se mantiene la misma estructura de `001`/`002`/`003` (un solo proyecto
backend, un solo proyecto frontend con feature folders). Todo lo nuevo son archivos
modificados/adicionales dentro de las carpetas ya existentes (`Models/`, `Services/`,
`Endpoints/` / `features/creditos-cajero/`), conforme al Principio I. No se crea ningún
proyecto, carpeta de arquitectura ni pantalla nueva independiente.

## Complexity Tracking

> No hay violaciones del Constitution Check que requieran justificación. Tabla no aplica.
