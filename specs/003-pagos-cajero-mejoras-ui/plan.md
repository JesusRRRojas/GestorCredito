# Implementation Plan: Pagos de Cuota, Rol Cajero y Mejoras de Simulación/Administración

**Branch**: `003-pagos-cajero-mejoras-ui` | **Date**: 2026-09-26 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/003-pagos-cajero-mejoras-ui/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Extender la aplicación ya construida en `001-gestion-creditos` y `002-seguimiento-avanzado-prospectos`
con: (1) registro de pago de cuotas y un nuevo rol de solo lectura "Cajero" con su propia
bandeja buscable de clientes y créditos; (2) una consulta de créditos por cliente (evolución de
la pantalla de Historial) con Monto Solicitado, Cuota Actual y estado (En proceso/Pendiente/
Cancelado); (3) una fila de totales en el cronograma de Simulación; (4) un encabezado
"Nombre — Monto Solicitado" reutilizable en las pantallas posteriores al Onboarding; (5)
validación de que la Declaración de Inversión no supere el Monto Solicitado; y (6) mejoras
visuales en las pantallas de Administrador (scroll horizontal garantizado + rediseño de
Productos) más el enlace faltante a la Bandeja del Asesor. Se reutiliza el mismo stack y los
mismos patrones de `001`/`002` (Minimal APIs, EF Core Code-First, Angular + Bootstrap) sin
introducir nuevas dependencias.

## Technical Context

**Language/Version**: C# 13 / .NET 10 (backend, ya en uso); TypeScript + Angular 19 (frontend,
ya en uso)

**Primary Dependencies**: ASP.NET Core Minimal APIs, Entity Framework Core (Code-First, SQL
Server provider), Angular, Bootstrap 5 — mismas dependencias que `001`/`002`, sin agregar
ninguna nueva.

**Storage**: Mismo SQL Server local (`dbGestorCredito`) vía EF Core; se agregan columnas
`Pagada`/`FechaPago` a `Cuota` mediante una nueva migración, con script de datos para marcar
como pagadas las cuotas de créditos ya `Finalizado` (ver `research.md` §1 y la clarificación
del spec).

**Testing**: xUnit, acotado al cálculo de "Cuota Actual"/estado de crédito (misma lógica que
`002` usó para el "paso actual" de la bandeja) y a la validación Monto a Invertir ≤ Monto
Solicitado; validación funcional principal mediante `quickstart.md` (recorrido manual, Principio
V de la constitución).

**Target Platform**: Misma aplicación web local (navegador de escritorio moderno contra backend
Kestrel local); sin cambios de plataforma.

**Project Type**: Web (frontend Angular + backend .NET Web API) — se extienden los mismos dos
proyectos (`backend/`, `frontend/`) de `001`/`002`, sin crear proyectos nuevos.

**Performance Goals**: Igual que `001`/`002` — uso interno de pocos usuarios concurrentes, sin
metas de throughput específicas.

**Constraints**: Sin autenticación real (se mantiene el selector de rol, ahora con 4 roles); sin
integraciones externas nuevas; cadena de conexión y secretos fuera del código versionado
(Principio VI), sin cambios respecto a `001`/`002`.

**Scale/Scope**: 1 rol nuevo (Cajero) + 1 pantalla nueva (bandeja de Cajero con pago de cuota) +
extensión de Historial (renombrado conceptualmente a "Créditos") + 1 componente compartido
(encabezado Nombre/Monto Solicitado, reusado en 5 pantallas) + ajustes visuales en 3 pantallas
de Administrador; 1 endpoint nuevo de dominio "Créditos" + extensiones puntuales a 3 endpoints
ya existentes.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principio | Evaluación | Cómo se cumple |
|-----------|------------|-----------------|
| I. Simplicidad Ante Todo | ✅ PASS | "Crédito" sigue siendo un concepto derivado (Prospecto + Simulación aceptada + Cuotas), sin tabla nueva (ver spec, Key Entities); un único endpoint `GET /api/creditos` sirve tanto a la bandeja del Cajero como a la consulta por cliente (Historia 1 y 2), evitando duplicar lógica de búsqueda/estado (ver `research.md` §2). Se retira el endpoint `historial` angosto de `001` en vez de mantener dos rutas que hacen casi lo mismo. |
| II. Idioma y Mercado (Perú) | ✅ PASS | UI en español; montos en Soles (S/) con el mismo pipe `monedaPe` ya existente; fechas `DD/MM/AAAA` con el pipe `fechaPe` ya existente. Sin cambios de moneda. |
| III. Cero Alcance Fantasma | ✅ PASS | Cada endpoint/campo nuevo deriva uno a uno de los FR-001 a FR-021 del spec `003`; no se agrega reversión de pagos, pagos parciales, ni edición para el Cajero (declarados fuera de alcance en las Assumptions del spec). |
| IV. Propuesta Antes de Construir | ✅ PASS | La única aclaración necesaria (créditos "Finalizado" previos sin historial de pago) ya se resolvió con el usuario durante `/speckit-specify`, antes de este plan. |
| V. Verificable por una Persona No Técnica | ✅ PASS | `quickstart.md` documenta un recorrido manual por cada una de las 6 historias, sin necesidad de leer código. |
| VI. Datos del Usuario y Seguridad de Credenciales | ✅ PASS | No se introduce ningún secreto nuevo; el rol Cajero reutiliza el mismo selector de rol sin usuario/contraseña ya usado por los demás roles (research.md `001` §4). |

**Resultado**: Ninguna violación que requiera justificación en "Complexity Tracking".

## Project Structure

### Documentation (this feature)

```text
specs/003-pagos-cajero-mejoras-ui/
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
│   │   ├── AppDbContext.cs             # Se agregan columnas Cuota.Pagada/FechaPago
│   │   └── Migrations/                 # Nueva migración: columnas + script de datos (FR-014)
│   ├── Models/
│   │   ├── Cuota.cs                    # Extiende: Pagada (bool), FechaPago (DateTime?)
│   │   └── Enums.cs                    # Extiende: RolUsuario.Cajero
│   ├── Endpoints/
│   │   ├── CreditosEndpoints.cs        # Nuevo: GET /api/creditos (bandeja Cajero + consulta
│   │   │                               #   por cliente), POST /api/creditos/{id}/pagar-cuota
│   │   └── ProspectosEndpoints.cs      # Extiende: ProspectoDetalleDto.MontoSolicitado,
│   │                                   #   validación Monto a Invertir ≤ Monto Solicitado
│   └── (Program.cs registra CreditosEndpoints)
└── GestorCredito.Api.Tests/
    └── EstadoCreditoTests.cs           # Nuevo: clasificación En proceso/Pendiente/Cancelado
                                        #   y cálculo de Cuota Actual

frontend/
├── src/app/
│   ├── core/
│   │   ├── rol-activo.service.ts       # Extiende: Rol incluye 'Cajero'
│   │   └── services/
│   │       └── creditos.service.ts     # Nuevo: consumo de /api/creditos
│   ├── features/
│   │   ├── creditos-cajero/            # Nuevo (Historia 1): bandeja del Cajero + pago de cuota
│   │   ├── historial/                  # Renombrado conceptualmente "Créditos" (Historia 2);
│   │   │                               #   mismo componente, nueva fuente de datos
│   │   ├── simulacion/                 # Extiende: fila de totales (Historia 3)
│   │   ├── declaracion-inversion/      # Extiende: validación ≤ Monto Solicitado (Historia 5)
│   │   └── configuracion/
│   │       ├── tipos-documento/        # Extiende: table-responsive (Historia 6)
│   │       ├── usuarios/               # Extiende: table-responsive (Historia 6)
│   │       └── productos/              # Rediseño visual (Historia 6)
│   └── shared/
│       └── encabezado-cliente/         # Nuevo: componente compartido "Nombre — Monto
│                                       #   Solicitado" (Historia 4), usado por Onboarding,
│                                       #   Declaración de Inversión, Requisitos, Aprobación,
│                                       #   Desembolso
└── (estructura estándar de Angular CLI, ya presente)
```

**Structure Decision**: Se mantiene la misma estructura de `001`/`002` (un solo proyecto
backend, un solo proyecto frontend con feature folders). Todo lo nuevo son archivos
adicionales dentro de las carpetas ya existentes (`Models/`, `Endpoints/`, `Services/` /
`features/`, `shared/`), conforme al Principio I. No se crea un proyecto ni una carpeta de
arquitectura nueva.

## Complexity Tracking

> No hay violaciones del Constitution Check que requieran justificación. Tabla no aplica.
