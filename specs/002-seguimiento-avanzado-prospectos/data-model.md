# Data Model: Seguimiento Avanzado de Prospectos (Bandeja, Onboarding Bancario y Comentarios de Aprobación)

**Feature**: `002-seguimiento-avanzado-prospectos` | **Fecha**: 2026-09-25

Este documento extiende `001-gestion-creditos/data-model.md`. Las entidades ya existentes se
listan solo con los cambios; las entidades nuevas se documentan completas.

## Producto *(extiende `001`)*

| Campo nuevo | Tipo | Regla |
|-------------|------|-------|
| RequiereDeclaracionInversion | bool | default `false`; activa/desactiva la etapa de Declaración de Inversión (FR-004/FR-005/FR-006), independiente de `RequiereRequisitos` |

## Prospecto *(extiende `001`)*

| Campo | Cambio | Regla |
|-------|--------|-------|
| CuentaBancariaCCI | **Eliminado** | Reemplazado por los campos siguientes (ver `research.md` §8 para la migración) |
| TipoCuentaDesembolso | Nuevo — enum { Interna, Externa } | requerido al completar el Onboarding (FR-007) |
| CuentaBancariaInternaId | Nuevo — int (FK → CuentaBancariaInterna, nullable) | requerido cuando `TipoCuentaDesembolso = Interna`; debe pertenecer al mismo cliente (mismo `TipoDocumentoId` + `NumeroDocumento` del Prospecto) |
| CuentaExternaBanco | Nuevo — string(150), nullable | requerido cuando `TipoCuentaDesembolso = Externa` (FR-009) |
| CuentaExternaCCI | Nuevo — string(20), nullable | requerido cuando `TipoCuentaDesembolso = Externa`; numérico, exactamente 20 dígitos (mismo formato que `001` FR-018) |
| ComentarioAprobador | Nuevo — string(1000), nullable | se completa al marcar "Observado" o "Rechazado" (FR-012); vacío si la última decisión fue "Aprobado" o si aún no hay decisión |

**Regla (FR-007/FR-008/FR-009)**: exactamente uno de los dos conjuntos de campos de cuenta
debe estar completo según `TipoCuentaDesembolso` (Interna → `CuentaBancariaInternaId`;
Externa → `CuentaExternaBanco` + `CuentaExternaCCI`); el otro conjunto queda `null`.

**Regla (FR-012/FR-013)**: `ComentarioAprobador` es obligatorio (no vacío, máx. 1000
caracteres) cuando la decisión del Aprobador es "Observado" o "Rechazado"; no se exige cuando
la decisión es "Aprobado".

### "Paso actual" derivado (no persistido) — usado por la bandeja (FR-002)

Ver `research.md` §1 para el algoritmo completo. Se calcula en el momento de listar la bandeja
a partir de: `Estado`, si los campos de Onboarding están completos, si existe
`DeclaracionInversion` (cuando el Producto la requiere), y si los Requisitos están completos
(cuando el Producto los requiere).

## CuentaBancariaInterna *(nueva)*

| Campo | Tipo | Regla |
|-------|------|-------|
| Id | int (PK) | autogenerado |
| TipoDocumentoId | int (FK → TipoDocumento) | requerido — junto con `NumeroDocumento`, identifica al cliente dueño de la cuenta (ver `research.md` §2, sin entidad `Cliente` separada) |
| NumeroDocumento | string(20) | requerido |
| NumeroCuenta | string(30) | requerido |
| Moneda | enum { PEN } | fijo en Soles (ver `research.md` §5) |
| Activo | bool | default `true` |
| FechaRegistro | datetime | requerido |

**Regla de unicidad (edge case del spec)**: no puede existir más de una `CuentaBancariaInterna`
con el mismo `(TipoDocumentoId, NumeroDocumento, NumeroCuenta)`.

**Reutilización (FR-008)**: al mostrar la lista de cuentas internas durante el Onboarding, el
sistema consulta todas las `CuentaBancariaInterna` activas cuyo `(TipoDocumentoId,
NumeroDocumento)` coincida con el del Prospecto actual, sin importar en qué Prospecto anterior
se hayan registrado.

## DeclaracionInversion *(nueva)*

| Campo | Tipo | Regla |
|-------|------|-------|
| Id | int (PK) | autogenerado |
| ProspectoId | int (FK → Prospecto, único) | un Prospecto tiene a lo sumo una Declaración de Inversión |
| MontoInvertir | decimal(18,2) | requerido, `> 0` |
| OrigenFondos | enum { Ahorros, Herencia, VentaActivo, ActividadEmpresarial, Otro } | requerido (ver `research.md` §6) |
| Detalle | string(500), nullable | opcional |
| FechaRegistro | datetime | requerido |

**Regla (FR-005/FR-006)**: solo existe (y solo se solicita) cuando
`Prospecto.Producto.RequiereDeclaracionInversion = true`.

## Máquina de estados de Prospecto — sin cambios respecto a `001`

La máquina de estados (`Simulacion → Evaluacion → [Aprobacion | Desembolso automático] →
Desembolso → Finalizado`, con las ramas de `Observado`/`Rechazado`) definida en
`001-gestion-creditos/data-model.md` no cambia. Este feature solo agrega:

1. Un sub-paso opcional dentro de `Evaluacion` (Declaración de Inversión, entre Onboarding y
   Requisitos) cuando el Producto lo requiere.
2. La regla de que "Observado" siempre navega a Onboarding, ya vigente en `001` (FR-025) y
   reafirmada aquí (FR-003).

## Validación cruzada con Success Criteria del spec `002`

- SC-002 (navegación exacta al paso donde quedó): se resuelve con el "Paso actual" derivado
  descrito arriba, evaluado en cada solicitud a la bandeja.
- SC-003 (Declaración de Inversión mostrada/omitida según el Producto): se resuelve con
  `Producto.RequiereDeclaracionInversion` + existencia de `DeclaracionInversion`.
- SC-004 (Cuenta Bancaria de Desembolso válida, Interna o Externa): se resuelve con la regla
  de exclusividad de campos de `Prospecto` descrita arriba.
- SC-005 (comentario visible en la bandeja): se resuelve con `Prospecto.ComentarioAprobador`
  y la regla de obligatoriedad en Observado/Rechazado.
- SC-006 (edición rápida desde la bandeja): se resuelve reutilizando el endpoint de Onboarding
  (ver `research.md` §3) con el guard de `Estado` de FR-016/FR-017.
