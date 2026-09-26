# Data Model: Pagos de Cuota, Rol Cajero y Mejoras de Simulación/Administración

**Feature**: `003-pagos-cajero-mejoras-ui` | **Fecha**: 2026-09-26

Extiende `001-gestion-creditos/data-model.md` y `002-seguimiento-avanzado-prospectos/data-model.md`.
Se listan solo los cambios; el resto del modelo no cambia.

## Cuota *(extiende `001`)*

| Campo nuevo | Tipo | Regla |
|-------------|------|-------|
| Pagada | bool | default `false`; `true` cuando el Cajero registra su pago (FR-001) |
| FechaPagoRealizado | datetime, nullable | fecha en que se registró el pago; `null` mientras `Pagada = false`; distinto del campo `FechaPago` ya existente de `001` (fecha programada de vencimiento de la cuota) |

**Regla (FR-008)**: no se puede volver a marcar como pagada una Cuota que ya tiene
`Pagada = true` (idempotencia del endpoint de pago).

**Migración de datos (FR-014)**: al aplicar la migración de este feature, toda `Cuota` cuya
`Simulacion.Prospecto.Estado = Finalizado` se marca `Pagada = true` con `FechaPagoRealizado` igual a la
`FechaCierre` del Prospecto (ver `research.md` §1).

## RolUsuario *(extiende `001`)*

Se agrega el valor `Cajero` al enum ya existente `{ Administrador, Asesor, Aprobador }`. Sin
cambios de esquema (se almacena como el mismo tipo entero ya usado por EF Core).

## Crédito *(concepto derivado — sin tabla nueva, ver spec Key Entities)*

Vista calculada a partir de `Prospecto` + su `Simulacion` con `Aceptada = true` + las `Cuota` de
esa simulación. No se persiste; se calcula en cada respuesta de `GET /api/creditos` (ver
`contracts/api-contracts.md`).

| Campo calculado | Origen | Regla |
|------------------|--------|-------|
| MontoSolicitado | `Simulacion.Monto` (aceptada) | Monto de la simulación aceptada del Prospecto |
| EstadoCredito | `Prospecto.Estado` + `Cuota.Pagada` | `"EnProceso"` si `Estado ∉ {Desembolso, Finalizado}`; `"Pendiente"` si `Estado ∈ {Desembolso, Finalizado}` y alguna Cuota no pagada; `"Cancelado"` si `Estado ∈ {Desembolso, Finalizado}` y todas las Cuotas pagadas (`research.md` §3) |
| CuotaActual | `Cuota` de menor `Numero` con `Pagada = false` | Solo aplica cuando `EstadoCredito = "Pendiente"`; incluye `Numero`, `FechaPago` (de la cuota, no confundir con el pago), `PagoTotal` |

**Regla (FR-010)**: los créditos con `Prospecto.Estado = Rechazado` se excluyen por completo de
toda respuesta de este endpoint.

**Agrupación por cliente**: la clave de "cliente" sigue siendo `(TipoDocumentoId,
NumeroDocumento)`, igual que en `002-seguimiento-avanzado-prospectos/data-model.md` (sin
entidad `Cliente` separada). Solo se incluyen Prospectos con Onboarding completo (`Nombres` no
nulo).

## Producto *(sin cambios de esquema)*

Sin campos nuevos en esta versión; Historia 6 (rediseño de Productos) es puramente visual.

## Validación cruzada con Success Criteria del spec `003`

- SC-002/SC-003 (exclusión de Rechazados; clasificación Cancelado): se resuelven con las reglas
  de `EstadoCredito` y la exclusión explícita de `Rechazado` descritas arriba.
- SC-005 (Monto a Invertir ≤ Monto Solicitado): se resuelve comparando
  `DeclaracionInversion.MontoInvertir` contra el `MontoSolicitado` calculado arriba (mismo dato
  que expone `ProspectoDetalleDto.MontoSolicitado`, ver `contracts/api-contracts.md`).
