# Data Model: Precarga de Datos del Cliente en Onboarding

**Feature**: `005-precarga-datos-onboarding` | **Fecha**: 2026-09-26

Sin cambios de esquema de base de datos (no requiere migración). Extiende únicamente la forma
de respuesta de la API sobre entidades ya existentes de `001-gestion-creditos` y
`002-seguimiento-avanzado-prospectos`.

## PrecargaOnboardingDto *(vista calculada — sin tabla nueva)*

Fila de precarga calculada a partir del prospecto cerrado más reciente del mismo cliente
(`research.md` §1), incluida en la respuesta de `GET /api/prospectos/{id}` solo cuando el
prospecto solicitado todavía no tiene su propio Onboarding completo.

| Campo | Tipo | Origen | Regla |
|-------|------|--------|-------|
| Nombres | string | `Prospecto.Nombres` del prospecto fuente | No vacío, ya que la búsqueda exige `Nombres != null` (FR-001). |
| Apellidos | string | `Prospecto.Apellidos` del prospecto fuente | Igual que Nombres. |
| Direccion | string | `Prospecto.Direccion` del prospecto fuente | Igual que Nombres. |
| TipoCuentaDesembolso | enum (`Interna`\|`Externa`) | `Prospecto.TipoCuentaDesembolso` del prospecto fuente | Determina si se precarga `CuentaBancariaInternaId` o los campos de cuenta externa (FR-005/FR-006). |
| CuentaBancariaInternaId | int?, nullable | `Prospecto.CuentaBancariaInternaId` del prospecto fuente | `null` si `TipoCuentaDesembolso = Externa`, o si la cuenta ya no existe/no está `Activo` en `CuentaBancariaInterna` (FR-007, `research.md` §3). |
| CuentaExternaBanco | string?, nullable | `Prospecto.CuentaExternaBanco` del prospecto fuente | `null` si `TipoCuentaDesembolso = Interna`. |
| CuentaExternaCCI | string?, nullable | `Prospecto.CuentaExternaCCI` del prospecto fuente | `null` si `TipoCuentaDesembolso = Interna`. |

## ProspectoDetalleDto *(extiende el registro ya existente de `001`)*

| Campo nuevo | Tipo | Regla |
|-------------|------|-------|
| Precarga | `PrecargaOnboardingDto?`, nullable | `null` si el prospecto ya tiene `Nombres` guardado, o si no existe ningún prospecto cerrado calificado del mismo cliente (FR-002); en caso contrario, contiene los datos del prospecto cerrado más reciente (FR-001). |

## Selección del prospecto fuente *(consulta, no entidad)*

Criterio exacto (ya detallado en `research.md` §1): entre los `Prospecto` con el mismo
`TipoDocumentoId` + `NumeroDocumento` que el prospecto solicitado, `Id` distinto, `Estado` en
`{Rechazado, Finalizado}` y `Nombres` no vacío, se toma el de `FechaCierre` más reciente.

## Validación cruzada con Success Criteria del spec `005`

- SC-002 (100% de prospectos nuevos de un cliente recurrente muestran precarga): se resuelve
  con la consulta de `research.md` §1, ejecutada en cada `GET /api/prospectos/{id}` mientras
  `Nombres` esté vacío.
- SC-003 (100% de clientes sin crédito previo muestran formulario vacío): se resuelve con
  `Precarga = null` cuando la consulta no encuentra ningún prospecto calificado (FR-002).
