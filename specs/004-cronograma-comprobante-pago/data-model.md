# Data Model: Cronograma Visual y Comprobante de Pago en Bandeja del Cajero

**Feature**: `004-cronograma-comprobante-pago` | **Fecha**: 2026-09-26

Extiende `001-gestion-creditos/data-model.md` y `003-pagos-cajero-mejoras-ui/data-model.md`. Se
listan solo los cambios; el resto del modelo no cambia.

## Cuota *(extiende `001` y `003`)*

| Campo nuevo | Tipo | Regla |
|-------------|------|-------|
| NumeroOperacion | string, nullable | `null` mientras `Pagada = false`; se exige no vacío (ni solo espacios) al momento de marcar la cuota como pagada (FR-005); una vez guardado, no se modifica en esta versión. |
| EvidenciaNombreArchivo | string, nullable | Nombre original del archivo de evidencia adjuntado por el Cajero; `null` si no se adjuntó ninguno (campo opcional, FR-006). |
| EvidenciaContenido | byte[], nullable | Contenido binario del archivo de evidencia; `null` si no se adjuntó ninguno. Máximo 5 MB, formato JPG/PNG/PDF (FR-007, `research.md` §4). |

**Regla (FR-005/FR-008)**: `NumeroOperacion` es obligatorio y se persiste en la misma operación
que marca `Pagada = true` (mismo endpoint `pagar-cuota` ya definido en `003`); no se puede
registrar un pago sin él.

**Regla (FR-009)**: `NumeroOperacion` y, si existe, `EvidenciaNombreArchivo`/`EvidenciaContenido`
se exponen al consultar la cuota (cronograma del crédito), igual que el resto de sus campos ya
existentes.

## CronogramaCuotaDto *(vista calculada — sin tabla nueva)*

Fila del cronograma que ve el Cajero al seleccionar un crédito (Historia 1). Se calcula a
partir de una `Cuota` real (persistida desde `001`), agregando el estado visual derivado.

| Campo calculado | Origen | Regla |
|------------------|--------|-------|
| Numero, FechaPago, PagoTotal | `Cuota` (ya existentes desde `001`) | Sin cambios. |
| EstadoCuota | `Cuota.Pagada` + orden de `Numero` entre las no pagadas | `"Pagada"` si `Pagada = true`; `"Actual"` si es la de menor `Numero` entre las no pagadas; `"Futura"` para el resto de no pagadas (`research.md` §1, extensión de `CreditoCalculator`). |
| NumeroOperacion | `Cuota.NumeroOperacion` | Visible solo cuando `EstadoCuota = "Pagada"`. |
| TieneEvidencia | `Cuota.EvidenciaContenido != null` | Indica si hay un archivo de evidencia descargable para esa cuota (evita exponer el binario completo en el listado). |

**Resumen de avance (FR-003)**: `CuotasPagadas` (conteo de `Pagada = true`) y `TotalCuotas`
(conteo total), calculados sobre la misma lista, para mostrar por ejemplo "3 de 12 pagadas".

## Validación cruzada con Success Criteria del spec `004`

- SC-001 (identificar en &lt;5s en qué cuota está y cuántas faltan): se resuelve con
  `EstadoCuota` por fila más el resumen `CuotasPagadas`/`TotalCuotas` descritos arriba.
- SC-002 (100% de pagos con Número de Operación no vacío): se resuelve exigiendo
  `NumeroOperacion` no nulo/no vacío como condición del endpoint de pago (mismo momento en que
  se marca `Pagada = true`).
- SC-004 (100% de cuotas pagadas muestran su Número de Operación al consultarlas): se resuelve
  exponiendo `NumeroOperacion` en `CronogramaCuotaDto` para toda cuota con `EstadoCuota = "Pagada"`.
