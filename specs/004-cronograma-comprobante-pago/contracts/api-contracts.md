# API Contracts: Cronograma Visual y Comprobante de Pago en Bandeja del Cajero

**Feature**: `004-cronograma-comprobante-pago` | **Fecha**: 2026-09-26

Extiende `specs/003-pagos-cajero-mejoras-ui/contracts/api-contracts.md`. Se documentan solo los
endpoints nuevos o modificados dentro de `/api/creditos`.

## GET /api/creditos/{prospectoId}/cronograma *(nuevo — Historia 1)*

Devuelve el cronograma completo de cuotas de un crédito con el estado visual de cada una.

**Request**: sin cuerpo.

**Response 200 OK**:

```json
{
  "estadoCredito": "Pendiente",
  "cuotasPagadas": 3,
  "totalCuotas": 12,
  "cuotas": [
    {
      "numero": 1,
      "fechaPago": "2026-02-15",
      "pagoTotal": 450.00,
      "estadoCuota": "Pagada",
      "numeroOperacion": "OP-000123",
      "tieneEvidencia": true
    },
    {
      "numero": 4,
      "fechaPago": "2026-05-15",
      "pagoTotal": 450.00,
      "estadoCuota": "Actual",
      "numeroOperacion": null,
      "tieneEvidencia": false
    },
    {
      "numero": 5,
      "fechaPago": "2026-06-15",
      "pagoTotal": 450.00,
      "estadoCuota": "Futura",
      "numeroOperacion": null,
      "tieneEvidencia": false
    }
  ]
}
```

**Errores**:
- `404 Not Found` si `prospectoId` no existe.
- `409 Conflict` con `{ "error": "CREDITO_SIN_CRONOGRAMA" }` si el crédito aún está "En proceso"
  (sin simulación aceptada / no desembolsado, FR-004).

## POST /api/creditos/{prospectoId}/pagar-cuota *(modificado — Historia 2)*

**Antes** (`003`): `POST` sin cuerpo.

**Ahora**: `POST` con `multipart/form-data`:

| Campo | Tipo | Obligatorio | Regla |
|-------|------|--------------|-------|
| numeroOperacion | texto | Sí | No vacío ni solo espacios (FR-005). |
| archivo | archivo | No | JPG, PNG o PDF, máximo 5 MB (FR-006, FR-007). |

**Response 200 OK**: igual forma que `003` (`estadoCredito`, `cuotaActual`), sin cambios de
forma.

**Errores nuevos**:
- `400 Bad Request` con `{ "error": "NUMERO_OPERACION_REQUERIDO" }` si `numeroOperacion` está
  vacío o solo espacios.
- `400 Bad Request` con `{ "error": "ARCHIVO_EVIDENCIA_INVALIDO" }` si `archivo` se adjunta pero
  no cumple formato o tamaño permitido.
- Se mantienen los errores ya existentes de `003` (`404 Not Found`, `409 Conflict` con
  `CREDITO_NO_TIENE_CUOTA_PENDIENTE`).

## GET /api/creditos/{prospectoId}/cuotas/{numero}/evidencia *(nuevo — Historia 2)*

Descarga el archivo de evidencia adjuntado al pagar la cuota indicada.

**Request**: sin cuerpo.

**Response 200 OK**: el archivo binario (`Results.File`), con el `NombreArchivo` y el
`ContentType` correspondiente al formato guardado (mismo patrón que la descarga de archivos de
Requisitos).

**Errores**:
- `404 Not Found` si la cuota no existe, no está pagada, o no tiene evidencia adjunta
  (`TieneEvidencia = false`).
