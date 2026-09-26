# Implementation Plan: Precarga de Datos del Cliente en Onboarding

**Branch**: `005-precarga-datos-onboarding` | **Date**: 2026-09-26 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/005-precarga-datos-onboarding/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Extender la pantalla de Onboarding ya construida en `001-gestion-creditos` para que, cuando un
prospecto nuevo aún no tiene sus propios datos de Onboarding guardados (Nombres vacío), el sistema
precargue automáticamente Nombres, Apellidos, Dirección y la preferencia de Cuenta de Desembolso
(Interna preseleccionada o Externa con Banco/CCI) tomados del prospecto cerrado (Rechazado o
Finalizado) más reciente del mismo cliente (mismo Tipo/Número de Documento) que sí completó su
Onboarding. Se reutiliza el mismo endpoint `GET /api/prospectos/{id}` ya consumido por la pantalla
de Onboarding, agregando un campo de precarga opcional a su respuesta, sin crear un endpoint ni
una entidad nueva.

## Technical Context

**Language/Version**: C# 13 / .NET 10 (backend, ya en uso); TypeScript + Angular 19 (frontend,
ya en uso)

**Primary Dependencies**: ASP.NET Core Minimal APIs, Entity Framework Core (Code-First, SQL
Server provider), Angular, Bootstrap 5 — mismas dependencias que features anteriores, sin agregar
ninguna nueva.

**Storage**: Mismo SQL Server local (`dbGestorCredito`) vía EF Core; sin cambios de esquema. La
precarga es una consulta adicional de solo lectura sobre la tabla `Prospectos` ya existente
(research.md §1).

**Testing**: xUnit, acotado a la selección del "prospecto cerrado más reciente con Onboarding
completo" (research.md §1) y a la validación de que una Cuenta Interna inactiva/eliminada no se
preselecciona (FR-007); validación funcional principal mediante `quickstart.md` (recorrido
manual, Principio V de la constitución).

**Target Platform**: Misma aplicación web local (navegador de escritorio moderno contra backend
Kestrel local); sin cambios de plataforma.

**Project Type**: Web (frontend Angular + backend .NET Web API) — se extienden los mismos dos
proyectos (`backend/`, `frontend/`), sin crear proyectos nuevos.

**Performance Goals**: Igual que features anteriores — uso interno de pocos usuarios
concurrentes; la consulta adicional de precarga solo se ejecuta cuando el prospecto todavía no
tiene Onboarding completo, por lo que no afecta las pantallas posteriores del flujo.

**Constraints**: Sin autenticación real; sin cambios de esquema de base de datos (no requiere
migración); cadena de conexión y secretos fuera del código versionado (Principio VI), sin
cambios respecto a features anteriores.

**Scale/Scope**: 0 endpoints nuevos, 1 endpoint existente extendido (`GET /api/prospectos/{id}`)
con un campo de respuesta adicional opcional; 1 pantalla existente extendida (Onboarding) para
aplicar la precarga y mostrar un aviso informativo cuando ocurre.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principio | Evaluación | Cómo se cumple |
|-----------|------------|-----------------|
| I. Simplicidad Ante Todo | ✅ PASS | Sin entidad "Cliente" ni tabla nueva: la precarga es una consulta de lectura sobre `Prospecto` ya existente (mismo patrón ya usado para reutilizar Cuentas Internas en `002`, research.md §1). Se reutiliza el mismo endpoint `GET /api/prospectos/{id}` ya consumido por la pantalla, en vez de crear uno paralelo. |
| II. Idioma y Mercado (Perú) | ✅ PASS | Aviso de precarga y textos en español; sin cambios de moneda/formato de fecha respecto a lo ya existente. |
| III. Cero Alcance Fantasma | ✅ PASS | Cada campo precargado deriva uno a uno de los FR-001 a FR-007 del spec `005`; no se agrega precarga de Requisitos, Declaración de Inversión ni ningún otro dato no listado en el spec. |
| IV. Propuesta Antes de Construir | ✅ PASS | El spec `005` ya se validó sin `[NEEDS CLARIFICATION]` pendientes antes de este plan. |
| V. Verificable por una Persona No Técnica | ✅ PASS | `quickstart.md` documenta un recorrido manual por ambas historias (datos personales y preferencia de cuenta), sin necesidad de leer código. |
| VI. Datos del Usuario y Seguridad de Credenciales | ✅ PASS | No se introduce ningún dato ni secreto nuevo; se reutilizan campos ya capturados y almacenados por el propio cliente en un crédito anterior. |

**Resultado**: Ninguna violación que requiera justificación en "Complexity Tracking".

## Project Structure

### Documentation (this feature)

```text
specs/005-precarga-datos-onboarding/
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
│   └── Endpoints/
│       └── ProspectosEndpoints.cs   # Extiende: registro PrecargaOnboardingDto; extiende
│                                    #   ProspectoDetalleDto con el campo Precarga; extiende
│                                    #   el handler GET /{id:int} para calcular la precarga
│                                    #   (solo cuando Nombres está vacío) y extiende
│                                    #   AMapaDetalle con un parámetro opcional
└── GestorCredito.Api.Tests/
    └── PrecargaOnboardingTests.cs   # Nuevo: selección del prospecto cerrado más reciente y
                                     #   descarte de Cuenta Interna inactiva/eliminada

frontend/
├── src/app/
│   ├── core/services/prospectos.service.ts   # Extiende: interfaz ProspectoDetalle con
│   │                                          #   precarga: PrecargaOnboarding | null
│   └── features/onboarding/
│       ├── onboarding.component.ts           # Extiende: aplica los valores de precarga al
│       │                                      #   formulario cuando p.nombres está vacío
│       └── onboarding.component.html          # Extiende: aviso informativo cuando los datos
│                                              #   fueron precargados
└── (estructura estándar de Angular CLI, ya presente)
```

**Structure Decision**: Se mantiene la misma estructura de features anteriores (un solo
proyecto backend, un solo proyecto frontend). Todo lo nuevo son extensiones de archivos ya
existentes (`Endpoints/ProspectosEndpoints.cs`, `features/onboarding/`), conforme al Principio
I. No se crea ningún endpoint, entidad, migración ni pantalla nueva.

## Complexity Tracking

> No hay violaciones del Constitution Check que requieran justificación. Tabla no aplica.
