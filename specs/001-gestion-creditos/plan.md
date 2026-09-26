# Implementation Plan: Gestión de Créditos (Prospecto a Desembolso)

**Branch**: `001-gestion-creditos` | **Date**: 2026-09-24 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-gestion-creditos/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Construir una aplicación web (backend .NET 10 + frontend Angular/Bootstrap + SQL Server) que
lleve a un prospecto de crédito desde la validación inicial de riesgo hasta el desembolso:
configuración de productos/requisitos (Administrador), validación y simulación de crédito
(Asesor), onboarding con bifurcación condicional de requisitos y aprobación (Asesor +
Aprobador), y desembolso con generación de PDF final (Asesor). El enfoque técnico prioriza la
simplicidad: un único proyecto backend con Minimal APIs y EF Core Code-First contra SQL
Server, un único proyecto Angular con Bootstrap sin theming propio, sin autenticación real
(selector de rol en el frontend) y sin capas de arquitectura adicionales a las
estrictamente necesarias.

## Technical Context

**Language/Version**: C# 13 / .NET 10 (backend); TypeScript + Angular (versión estable más
reciente vía Angular CLI) (frontend)

**Primary Dependencies**: ASP.NET Core Minimal APIs, Entity Framework Core (Code-First,
SQL Server provider), PdfSharpCore (generación de PDF), Angular, Bootstrap 5 (npm)

**Storage**: SQL Server local (`dbGestorCredito`), acceso vía EF Core; archivos adjuntos y
PDFs generados almacenados como `varbinary(max)` en la misma base de datos (ver
`research.md` §3)

**Testing**: xUnit, acotado al cálculo del cronograma (`research.md` §10); validación
funcional principal mediante `quickstart.md` (recorrido manual guiado por criterios de
aceptación, Principio V de la constitución)

**Target Platform**: Aplicación web ejecutada localmente (navegador de escritorio moderno
contra backend Kestrel local); despliegue en la nube fuera de alcance (declarado en el spec)

**Project Type**: Web (frontend Angular + backend .NET Web API) — estructura de dos
proyectos (`backend/`, `frontend/`)

**Performance Goals**: Uso interno con pocos usuarios concurrentes (un equipo de asesores,
aprobadores y un administrador); sin metas de throughput específicas más allá de una
respuesta percibida como inmediata en formularios y cálculo de cronograma

**Constraints**: Sin autenticación real (selector de rol solamente, ver `research.md` §4);
sin integraciones externas reales (Mock de riesgo, sin Reniec/Equifax); cadena de conexión y
cualquier secreto fuera del código versionado (Principio VI de la constitución)

**Scale/Scope**: 6 pantallas de flujo (Validación, Simulación, Onboarding, Requisitos,
Aprobación, Desembolso) + módulo de configuración (Tipos de Documento/Persona, Productos,
Requisitos, Usuarios) + historial de prospectos cerrados (FR-029); volumen de datos bajo
(uso de una oficina/equipo, no miles de usuarios)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principio | Evaluación | Cómo se cumple |
|-----------|------------|-----------------|
| I. Simplicidad Ante Todo | ✅ PASS | Un solo proyecto backend (sin capas Domain/Application/Infrastructure separadas), Minimal APIs en vez de MVC, EF Core directo sin patrón Repository adicional, Bootstrap sin theming propio, sin autenticación real. |
| II. Idioma y Mercado (Perú) | ✅ PASS | UI en español, fechas `DD/MM/AAAA`, todos los montos en Soles (S/) — ver `research.md` §7 (corregido tras `/speckit-analyze`: se eliminó la opción de Dólares que contradecía este principio). |
| III. Cero Alcance Fantasma | ✅ PASS | Contratos de API y modelo de datos derivan uno a uno de los FR-001 a FR-029 del spec; ninguna entidad o endpoint excede lo escrito. |
| IV. Propuesta Antes de Construir | ✅ PASS | No se introduce en este plan ninguna funcionalidad nueva no presente en el spec ya clarificado. |
| V. Verificable por una Persona No Técnica | ✅ PASS | `quickstart.md` documenta un recorrido manual completo, sin necesidad de leer código, para validar cada criterio de aceptación. |
| VI. Datos del Usuario y Seguridad de Credenciales | ⚠️ REQUIERE ACCIÓN EN IMPLEMENTACIÓN | La cadena de conexión provista por el usuario contiene una contraseña; el plan exige (research.md §8) que se configure vía `dotnet user-secrets`/variable de entorno y **nunca** se escriba en `appsettings.json` versionado. Esto debe verificarse explícitamente al implementar. |

**Resultado**: Ninguna violación que requiera justificación en "Complexity Tracking". El
único punto de atención (Principio VI) es una instrucción de implementación, no una excepción
al principio.

## Project Structure

### Documentation (this feature)

```text
specs/001-gestion-creditos/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md         # Phase 1 output (/speckit-plan command)
├── quickstart.md         # Phase 1 output (/speckit-plan command)
├── contracts/
│   └── api-contracts.md  # Phase 1 output (/speckit-plan command)
└── tasks.md              # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
backend/
├── GestorCredito.Api/
│   ├── Program.cs                 # Composición de la app, mapeo de endpoints Minimal API
│   ├── appsettings.json            # Sin secretos (ver research.md §8)
│   ├── Data/
│   │   ├── AppDbContext.cs
│   │   └── Migrations/
│   ├── Models/                     # Entidades de data-model.md (Producto, Prospecto, ...)
│   ├── Endpoints/                  # Grupos de Minimal API por área (Productos, Prospectos, ...)
│   └── Services/                   # CronogramaCalculator, MockRiesgoService, PdfGenerator
└── GestorCredito.Api.Tests/
    └── CronogramaCalculatorTests.cs

frontend/
├── src/app/
│   ├── core/                       # Interceptors HTTP, servicio de "rol activo"
│   ├── features/
│   │   ├── configuracion/          # Tipos de Documento/Persona, Productos, Requisitos, Usuarios
│   │   ├── validacion/             # Pantalla 1
│   │   ├── simulacion/             # Pantalla 2
│   │   ├── onboarding/             # Pantalla 3
│   │   ├── requisitos/             # Pantalla 4
│   │   ├── aprobacion/             # Pantalla 5
│   │   ├── desembolso/             # Pantalla 6
│   │   └── historial/              # FR-029
│   └── shared/                     # Pipes de moneda/fecha (formato peruano), componentes comunes
└── (estructura estándar de Angular CLI)
```

**Structure Decision**: Opción "Web application" (backend + frontend separados), acorde al
stack pedido (.NET + Angular). Dentro del backend se usa **un solo proyecto** de clase
(`GestorCredito.Api`) en vez de una separación en múltiples proyectos (Domain/Application/
Infrastructure), porque el Principio I (Simplicidad Ante Todo) exige evitar capas de
abstracción no justificadas por el tamaño actual del sistema.

## Complexity Tracking

> No hay violaciones del Constitution Check que requieran justificación. Tabla no aplica.
