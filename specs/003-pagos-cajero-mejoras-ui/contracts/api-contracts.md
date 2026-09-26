# API Contracts: Pagos de Cuota, Rol Cajero y Mejoras de Simulación/Administración

**Feature**: `003-pagos-cajero-mejoras-ui` | **Fecha**: 2026-09-26

Extiende `001-gestion-creditos/contracts/api-contracts.md` y
`002-seguimiento-avanzado-prospectos/contracts/api-contracts.md`. Se documentan solo las rutas
nuevas y las modificadas.

## Créditos (Historias 1 y 2 — Cajero, Asesor, Administrador)

**GET** `/api/creditos?buscar={texto opcional}`

Sin `buscar`: devuelve todos los clientes con Onboarding completo y al menos un crédito no
Rechazado (uso de la bandeja del Cajero, FR-004). Con `buscar`: filtra por coincidencia parcial
(sin distinguir mayúsculas) contra el Nombre completo o el Número de Documento (FR-005); un
`buscar` igual al Número de Documento exacto de un cliente sirve también a la consulta puntual
de la Historia 2 (reemplaza `GET /api/prospectos/historial` de `001`, retirado — ver
`research.md` §2).

Response `200 OK`:
```json
[
  {
    "tipoDocumentoId": 1,
    "tipoDocumento": "DNI",
    "numeroDocumento": "12345678",
    "nombreCompleto": "Juan Pérez",
    "creditos": [
      {
        "prospectoId": 501,
        "productoNombre": "Crédito Personal",
        "montoSolicitado": 5000.00,
        "estadoCredito": "Pendiente",
        "cuotaActual": { "numero": 3, "fechaPago": "2026-11-05", "pagoTotal": 225.00 }
      },
      {
        "prospectoId": 480,
        "productoNombre": "Crédito Personal",
        "montoSolicitado": 3000.00,
        "estadoCredito": "Cancelado",
        "cuotaActual": null
      }
    ]
  }
]
```

- `estadoCredito` ∈ `{ EnProceso, Pendiente, Cancelado }` (FR-011/FR-012/FR-013).
- `cuotaActual`: `null` cuando `estadoCredito` es `EnProceso` o `Cancelado`.
- Los créditos con `Prospecto.Estado = Rechazado` nunca aparecen (FR-010).

**POST** `/api/creditos/{prospectoId}/pagar-cuota`

Registra el pago de la Cuota Actual del crédito indicado (FR-001/FR-007).

Response `200 OK`:
```json
{ "estadoCredito": "Pendiente", "cuotaActual": { "numero": 4, "fechaPago": "2026-12-05", "pagoTotal": 221.00 } }
```
o, si esa era la última cuota:
```json
{ "estadoCredito": "Cancelado", "cuotaActual": null }
```

- `409 Conflict` (`{ "error": "CREDITO_NO_TIENE_CUOTA_PENDIENTE" }`) si el crédito no está en
  estado `Pendiente` (ya `Cancelado`, o `EnProceso` sin desembolsar aún) — FR-006/FR-008,
  validado en el servidor (`research.md` §5).
- `404 Not Found` si el `prospectoId` no existe.

## Onboarding / Requisitos / Aprobación / Desembolso — Monto Solicitado (extiende `001`/`002`)

**GET** `/api/prospectos/{id}` *(modificado)* — `ProspectoDetalleDto` agrega:
```json
{ "montoSolicitado": 5000.00 }
```
`null` si el prospecto todavía no tiene una simulación aceptada (FR-016).

## Declaración de Inversión — validación contra Monto Solicitado (modifica `002`)

**PUT** `/api/prospectos/{id}/declaracion-inversion` *(modificado)*

- `400 Bad Request` (`{ "error": "MONTO_INVERTIR_SUPERA_MONTO_SOLICITADO" }`) si
  `montoInvertir > montoSolicitado` del prospecto (FR-018, límite inclusive: igual sí se
  permite).

## Selector de rol (extiende `001`)

El selector de rol del frontend admite ahora un cuarto valor `"Cajero"` (FR-003); no hay cambio
de contrato HTTP (el backend nunca validó rol del lado del servidor, ver `research.md` `001`
§4).

## Convenciones

Se mantienen las mismas convenciones generales de `001`/`002` (fechas `DD/MM/AAAA` en pantalla,
montos en Soles con 2 decimales, errores `400`/`409` con código y mensaje en español).
