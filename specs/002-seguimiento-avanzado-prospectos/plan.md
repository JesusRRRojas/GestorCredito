# Implementation Plan: Seguimiento Avanzado de Prospectos (Bandeja, Onboarding Bancario y Comentarios de Aprobación)

**Branch**: `002-seguimiento-avanzado-prospectos` | **Date**: 2026-09-25 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/002-seguimiento-avanzado-prospectos/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Extender la aplicación ya construida en `001-gestion-creditos` (backend .NET 10 + frontend
Angular/Bootstrap + SQL Server) con: (1) una bandeja del Asesor que lista sus prospectos
activos con un estado resumido y navega al paso exacto donde quedó cada uno; (2) selección de
cuenta bancaria de desembolso Interna (reutilizable entre créditos del mismo cliente) o
Externa (banco + CCI), reemplazando el campo único de CCI del Onboarding; (3) una etapa
condicional de Declaración de Inversión, activable por Producto; (4) un comentario obligatorio
del Aprobador al Observar/Rechazar, visible en tooltip en la bandeja; y (5) edición de los
datos del cliente desde la bandeja en cualquier estado activo salvo "Aprobación". El enfoque
técnico reutiliza el stack y los patrones ya elegidos en `001-gestion-creditos` (Minimal APIs,
EF Core Code-First, un único proyecto backend, Angular+Bootstrap sin theming propio) sin
introducir nuevas dependencias ni capas de arquitectura adicionales.

## Technical Context

**Language/Version**: C# 13 / .NET 10 (backend, ya en uso); TypeScript + Angular 19 (frontend,
ya en uso)

**Primary Dependencies**: ASP.NET Core Minimal APIs, Entity Framework Core (Code-First, SQL
Server provider), PdfSharpCore (se reutiliza para actualizar el PDF de Aprobación Final),
Angular, Bootstrap 5 — mismas dependencias que `001-gestion-creditos`, sin agregar ninguna
nueva.

**Storage**: Mismo SQL Server local (`dbGestorCredito`) vía EF Core; se agregan tablas
`CuentaBancariaInterna` y `DeclaracionInversion`, y columnas nuevas en `Prospecto` y
`Producto` mediante una nueva migración (ver `research.md` §8 y `data-model.md`).

**Testing**: xUnit para la validación del CCI (20 dígitos, ya cubierta en `001`) extendida a
la validación de Cuenta Interna/Externa si aplica; validación funcional principal mediante
`quickstart.md` (recorrido manual guiado por criterios de aceptación, Principio V de la
constitución), igual que en `001-gestion-creditos`.

**Target Platform**: Misma aplicación web local (navegador de escritorio moderno contra
backend Kestrel local); sin cambios de plataforma.

**Project Type**: Web (frontend Angular + backend .NET Web API) — se extienden los mismos dos
proyectos (`backend/`, `frontend/`) de `001-gestion-creditos`, sin crear proyectos nuevos.

**Performance Goals**: Igual que `001-gestion-creditos` — uso interno de pocos usuarios
concurrentes, sin metas de throughput específicas.

**Constraints**: Sin autenticación real (se mantiene el selector de rol de `001`, ver
`research.md` §2); sin integraciones externas nuevas; cadena de conexión y secretos fuera del
código versionado (Principio VI), sin cambios respecto a `001`.

**Scale/Scope**: 1 pantalla nueva (Bandeja del Asesor) + 1 pantalla condicional nueva
(Declaración de Inversión) + extensión de las pantallas de Onboarding (cuenta interna/
externa, modo edición) y Aprobación (comentario obligatorio) ya existentes; 2 entidades
nuevas (`CuentaBancariaInterna`, `DeclaracionInversion`) + campos nuevos en `Prospecto` y
`Producto`.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principio | Evaluación | Cómo se cumple |
|-----------|------------|-----------------|
| I. Simplicidad Ante Todo | ✅ PASS | No se agregan nuevos proyectos, capas ni dependencias; el "paso actual" de un prospecto se calcula a partir del `Estado` y los datos ya presentes (sin nuevos valores de enum), y el comentario del Aprobador se guarda como un único campo en `Prospecto` en vez de una tabla de historial (ver `research.md` §1 y §4). La edición de datos del cliente reutiliza el mismo endpoint de Onboarding en vez de crear uno paralelo (`research.md` §3). |
| II. Idioma y Mercado (Perú) | ✅ PASS | UI en español; la Moneda de `CuentaBancariaInterna` se restringe a Soles (PEN), igual que `Producto.Moneda`, siguiendo el precedente ya establecido en `001-gestion-creditos` para no introducir soporte multi-moneda no solicitado (ver `research.md` §5). |
| III. Cero Alcance Fantasma | ✅ PASS | Cada entidad y endpoint nuevo deriva uno a uno de los FR-001 a FR-018 del spec `002`; no se agrega ningún campo, validación o pantalla adicional no descrita (ej. no se agrega historial de comentarios, no se agrega bloqueo optimista, no se agrega validación KYC/AML adicional — ver Assumptions del spec). |
| IV. Propuesta Antes de Construir | ✅ PASS | Las 3 decisiones que requerían aclaración (campos de Declaración de Inversión, alcance de cuentas internas, alcance de edición desde bandeja) se resolvieron con el usuario en la sesión de `/speckit-specify` antes de este plan; ninguna se decide unilateralmente aquí. |
| V. Verificable por una Persona No Técnica | ✅ PASS | `quickstart.md` documenta un recorrido manual para cada Historia de Usuario (bandeja, cuenta interna/externa, declaración de inversión, comentario del Aprobador, edición desde bandeja), sin necesidad de leer código. |
| VI. Datos del Usuario y Seguridad de Credenciales | ✅ PASS | No se introduce ningún secreto nuevo; los campos nuevos (cuenta bancaria, declaración de inversión, comentario) son datos de negocio ya autorizados por el spec, no credenciales. Se mantiene la configuración de cadena de conexión vía `dotnet user-secrets` ya establecida en `001`. |

**Resultado**: Ninguna violación que requiera justificación en "Complexity Tracking".

## Project Structure

### Documentation (this feature)

```text
specs/002-seguimiento-avanzado-prospectos/
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
│   │   ├── AppDbContext.cs             # Se agregan DbSet<CuentaBancariaInterna>, DbSet<DeclaracionInversion>
│   │   └── Migrations/                 # Nueva migración: columnas Prospecto/Producto + tablas nuevas
│   ├── Models/
│   │   ├── Prospecto.cs                # Extiende: TipoCuentaDesembolso, CuentaBancariaInternaId,
│   │   │                               #   CuentaExternaBanco, CuentaExternaCCI, ComentarioAprobador
│   │   ├── Producto.cs                 # Extiende: RequiereDeclaracionInversion
│   │   ├── CuentaBancariaInterna.cs    # Nuevo (Historia 2)
│   │   └── DeclaracionInversion.cs     # Nuevo (Historia 3)
│   ├── Endpoints/
│   │   ├── ProspectosEndpoints.cs      # Extiende: bandeja, onboarding (cuenta interna/externa,
│   │   │                               #   reutilizado para edición), declaración de inversión, decisión con comentario
│   │   ├── ProductosEndpoints.cs       # Extiende: RequiereDeclaracionInversion en el body
│   │   └── CuentasInternasEndpoints.cs # Nuevo: listar/agregar cuentas internas por cliente (Historia 2)
│   └── Services/
│       └── PdfGenerator.cs             # Extiende: lee Cuenta Interna/Externa en vez del CCI único
└── GestorCredito.Api.Tests/
    └── CuentaBancariaValidationTests.cs # Nuevo: valida CCI de 20 dígitos también para cuenta externa

frontend/
├── src/app/
│   ├── features/
│   │   ├── bandeja/                    # Nuevo (Historia 1): lista + navegación a paso actual
│   │   ├── onboarding/                 # Extiende: selector Interna/Externa, lista de cuentas
│   │   │                               #   internas, modo edición reutilizado desde la bandeja
│   │   ├── declaracion-inversion/      # Nuevo (Historia 3), condicional por Producto
│   │   ├── aprobacion/                 # Extiende: campo de comentario obligatorio (Observado/Rechazado)
│   │   └── configuracion/productos/    # Extiende: checkbox "Requiere Declaración de Inversión"
│   └── shared/                         # Reutiliza pipes/componentes existentes (sin cambios)
└── (estructura estándar de Angular CLI, ya presente)
```

**Structure Decision**: Se mantiene la misma estructura de `001-gestion-creditos` (un solo
proyecto backend `GestorCredito.Api`, un solo proyecto frontend Angular con feature folders).
No se agrega ningún proyecto ni carpeta de arquitectura nueva (Domain/Application/
Infrastructure); todo lo nuevo son archivos adicionales dentro de las carpetas `Models/`,
`Endpoints/`, `Services/` (backend) y `features/` (frontend) ya existentes, conforme al
Principio I.

## Complexity Tracking

> No hay violaciones del Constitution Check que requieran justificación. Tabla no aplica.
