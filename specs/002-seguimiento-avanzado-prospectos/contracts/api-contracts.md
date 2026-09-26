# API Contracts: Seguimiento Avanzado de Prospectos (Bandeja, Onboarding Bancario y Comentarios de Aprobación)

**Feature**: `002-seguimiento-avanzado-prospectos` | **Fecha**: 2026-09-25

Extiende `001-gestion-creditos/contracts/api-contracts.md`. Se documentan solo las rutas
nuevas y las modificadas; el resto de la API (configuración de Tipos, Requisitos, Usuarios,
Simulación, Historial) no cambia.

## Bandeja del Asesor (Historia 1)

**GET** `/api/prospectos/bandeja`

Response `200 OK` (FR-001):
```json
[
  {
    "prospectoId": 501,
    "nombreCliente": "Juan Pérez",
    "tipoDocumento": "DNI",
    "numeroDocumento": "12345678",
    "estadoResumen": "EnProceso",
    "pasoActual": "Onboarding",
    "comentarioAprobador": null
  },
  {
    "prospectoId": 498,
    "nombreCliente": "María Gómez",
    "tipoDocumento": "DNI",
    "numeroDocumento": "87654321",
    "estadoResumen": "Observado",
    "pasoActual": "Onboarding",
    "comentarioAprobador": "Falta el sustento de ingresos en el Requisito 2."
  }
]
```

- `estadoResumen` ∈ `{ Bloqueado, EnProceso, Aprobacion, Desembolsado, Observado }` (FR-001;
  ver mapeo en `spec.md` Assumptions).
- `pasoActual` ∈ `{ Simulacion, Onboarding, DeclaracionInversion, Requisitos, Desembolso }`;
  el frontend lo usa para navegar directamente a la pantalla correcta (FR-002). Ausente/`null`
  cuando `estadoResumen` es `Bloqueado`, `Aprobacion` o `Desembolsado` (no aplica navegación de
  "continuar").
- `nombreCliente` puede venir vacío mientras el prospecto está en `Simulacion` (Onboarding aún
  no completado).
- `comentarioAprobador`: presente solo cuando `estadoResumen = Observado` (o el prospecto fue
  recientemente `Bloqueado` por rechazo con comentario); se muestra en el tooltip (FR-014).

## Onboarding con cuenta bancaria Interna/Externa (Historia 2)

**GET** `/api/clientes/{tipoDocumentoId}/{numeroDocumento}/cuentas-internas`

Response `200 OK` (FR-008):
```json
[
  { "id": 12, "numeroCuenta": "191-2345678-0-11", "moneda": "PEN" }
]
```

**POST** `/api/clientes/{tipoDocumentoId}/{numeroDocumento}/cuentas-internas`

Request (FR-008):
```json
{ "numeroCuenta": "191-2345678-0-22", "moneda": "PEN" }
```

Response `201 Created`: la cuenta creada (mismo formato que el listado). `409 Conflict` si ya
existe una cuenta con el mismo `numeroCuenta` para ese cliente (edge case del spec).

**PUT** `/api/prospectos/{id}/onboarding` *(modificado respecto a `001`)*

Request — cuenta interna:
```json
{
  "nombres": "Juan",
  "apellidos": "Pérez",
  "direccion": "Av. Siempre Viva 123, Lima",
  "tipoCuentaDesembolso": "Interna",
  "cuentaBancariaInternaId": 12
}
```

Request — cuenta externa:
```json
{
  "nombres": "Juan",
  "apellidos": "Pérez",
  "direccion": "Av. Siempre Viva 123, Lima",
  "tipoCuentaDesembolso": "Externa",
  "cuentaExterna": { "banco": "Banco XYZ", "cci": "00212345678901234567" }
}
```

- Este mismo endpoint se usa para: (a) el guardado inicial del Onboarding (FR-007/FR-008/
  FR-009/FR-010), (b) la corrección tras "Observado" (ya vigente en `001`), y (c) la edición
  desde la bandeja (Historia 5, FR-015). El backend aplica el guard de estado:
  - `200 OK` si el `Estado` del prospecto es distinto de `Aprobacion`, `Rechazado` o
    `Finalizado`.
  - `409 Conflict` (`{ "error": "EDICION_NO_PERMITIDA_EN_ESTADO_ACTUAL" }`) si el `Estado` es
    `Aprobacion` (FR-016) o un estado cerrado (FR-017).
- `400 Bad Request` si el CCI de la cuenta externa no es numérico de exactamente 20 dígitos.

## Declaración de Inversión (Historia 3)

**GET** `/api/prospectos/{id}/declaracion-inversion`

Response `200 OK` si ya existe, `404 Not Found` si el prospecto aún no la completó.

**PUT** `/api/prospectos/{id}/declaracion-inversion`

Request (FR-005):
```json
{
  "montoInvertir": 5000.00,
  "origenFondos": "Ahorros",
  "detalle": null
}
```

- `origenFondos` ∈ `{ Ahorros, Herencia, VentaActivo, ActividadEmpresarial, Otro }`.
- `409 Conflict` si `Producto.RequiereDeclaracionInversion = false` para ese prospecto
  (pantalla no aplicable, FR-006).
- Al guardar, el "paso actual" (ver bandeja) avanza a `Requisitos` o `Desembolso` según
  corresponda.

## Productos — indicador de Declaración de Inversión (extiende `001`)

**PUT/POST** `/api/productos` / `/api/productos/{id}` *(modificado)*

El body agrega el campo:
```json
{ "requiereDeclaracionInversion": true }
```
(FR-004), independiente de `requiereRequisitos` ya existente.

## Aprobación con comentario obligatorio (modifica `001`)

**POST** `/api/prospectos/{id}/decision` *(modificado)*

Request:
```json
{ "decision": "Observado", "comentario": "Falta el sustento de ingresos en el Requisito 2." }
```

- `comentario`: requerido (no vacío, máx. 1000 caracteres) cuando `decision` ∈
  `{ Observado, Rechazado }` (FR-012); `400 Bad Request` si falta o excede el límite. Opcional
  (ignorado si se envía) cuando `decision = Aprobado` (FR-013).
- Al guardar, `Prospecto.ComentarioAprobador` se actualiza con el valor recibido.

## Convenciones

Se mantienen las mismas convenciones generales de `001-gestion-creditos/contracts/
api-contracts.md` (fechas `DD/MM/AAAA` en pantalla, montos en Soles con 2 decimales, errores
`400 Bad Request` con código y mensaje en español).
