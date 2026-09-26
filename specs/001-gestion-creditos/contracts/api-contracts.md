# API Contracts: Gestión de Créditos (Prospecto a Desembolso)

**Feature**: `001-gestion-creditos` | **Fecha**: 2026-09-24

API HTTP/JSON expuesta por el backend (.NET 10, Minimal APIs) y consumida por el frontend
Angular. Todas las rutas van bajo el prefijo `/api`. No hay autenticación de servidor (ver
`research.md` §4); el rol activo es una decisión de la interfaz Angular.

## Configuración (Administrador)

| Método | Ruta | Descripción | FR |
|--------|------|-------------|----|
| GET/POST | `/api/tipos-documento` | Listar / crear Tipos de Documento | FR-001 |
| PUT/DELETE | `/api/tipos-documento/{id}` | Editar / deshabilitar | FR-001 |
| GET/POST | `/api/tipos-persona` | Listar / crear Tipos de Persona | FR-001 |
| PUT/DELETE | `/api/tipos-persona/{id}` | Editar / deshabilitar | FR-001 |
| GET/POST | `/api/productos` | Listar / crear Productos | FR-002 |
| GET/PUT/DELETE | `/api/productos/{id}` | Detalle / editar / deshabilitar | FR-002 |
| GET/POST | `/api/productos/{id}/requisitos` | Listar / agregar Requisitos del Producto | FR-003 |
| PUT/DELETE | `/api/requisitos/{id}` | Editar / deshabilitar un Requisito | FR-003 |
| GET/POST | `/api/usuarios` | Listar / crear Usuarios | FR-004 |
| PUT/DELETE | `/api/usuarios/{id}` | Editar / deshabilitar | FR-004 |

## Validación de riesgo (Pantalla 1 — Asesor)

**POST** `/api/prospectos/validaciones`

Request:
```json
{ "tipoDocumentoId": 1, "numeroDocumento": "12345678" }
```

Response `200 OK` (favorable, crea Prospecto — FR-007/FR-009):
```json
{ "prospectoId": 501, "resultadoMock": 2, "aprobado": true }
```

Response `200 OK` (rechazado, no se crea Prospecto — FR-008):
```json
{ "resultadoMock": 5, "aprobado": false }
```

Response `409 Conflict` (ya existe un prospecto activo para ese documento — FR-010):
```json
{ "error": "PROSPECTO_ACTIVO_EXISTENTE", "prospectoId": 498 }
```

## Simulación (Pantalla 2 — Asesor)

| Método | Ruta | Descripción | FR |
|--------|------|-------------|----|
| GET | `/api/productos?activos=true` | Productos disponibles para el selector | FR-011 |
| POST | `/api/prospectos/{id}/simulaciones` | Calcula el cronograma (no persiste) | FR-012/FR-013/FR-014 |
| POST | `/api/prospectos/{id}/simulaciones/aceptar` | Persiste la simulación aceptada y avanza a `Evaluacion` | FR-015 |
| GET | `/api/prospectos/{id}/simulaciones/actual/pdf` | Descarga el PDF del cronograma aceptado | FR-016 |

Request de `POST .../simulaciones`:
```json
{ "productoId": 10, "monto": 1000.00, "tasa": 2.0, "plazo": 5 }
```
El backend valida `monto/tasa/plazo` contra los rangos del Producto (`400 Bad Request` si
están fuera de rango) y devuelve la lista de cuotas calculada (ver `data-model.md`).

## Onboarding y Requisitos (Pantallas 3 y 4 — Asesor)

| Método | Ruta | Descripción | FR |
|--------|------|-------------|----|
| PUT | `/api/prospectos/{id}/onboarding` | Guarda Nombres, Apellidos, Dirección, Cuenta Bancaria | FR-017/FR-018/FR-019 |
| GET | `/api/prospectos/{id}/requisitos` | Lista los Requisitos a adjuntar (vacío si `RequiereRequisitos=false`) | FR-020 |
| POST | `/api/prospectos/{id}/requisitos/{requisitoId}/archivo` | Sube/reemplaza el PDF de ese Requisito | FR-020 |
| POST | `/api/prospectos/{id}/requisitos/completar` | Marca la carga como finalizada → pasa a `Aprobacion` (o a `Desembolso` si el Producto no requiere requisitos) | FR-021/FR-022 |

## Aprobación (Pantalla 5 — Aprobador)

| Método | Ruta | Descripción | FR |
|--------|------|-------------|----|
| GET | `/api/prospectos?estado=Aprobacion` | Bandeja de prospectos pendientes de decisión | FR-023 |
| GET | `/api/prospectos/{id}/requisitos/archivos` | Ver los PDFs adjuntos del prospecto | FR-023 |
| POST | `/api/prospectos/{id}/decision` | Registrar decisión: `Aprobado` \| `Observado` \| `Rechazado` | FR-024/FR-025/FR-026 |

Request de `POST .../decision`:
```json
{ "decision": "Observado" }
```

## Desembolso (Pantalla 6 — Asesor)

| Método | Ruta | Descripción | FR |
|--------|------|-------------|----|
| GET | `/api/prospectos?estado=Desembolso` | Bandeja de prospectos listos para desembolsar | — |
| GET | `/api/prospectos/{id}/aprobacion-final/pdf` | Genera/descarga el PDF de Aprobación Final | FR-027 |
| POST | `/api/prospectos/{id}/cerrar` | Cierra el proceso → `Finalizado` | FR-028 |

## Historial (Asesor / Administrador)

| Método | Ruta | Descripción | FR |
|--------|------|-------------|----|
| GET | `/api/prospectos/historial?numeroDocumento=12345678` | Prospectos cerrados (`Rechazado`/`Finalizado`) de un cliente | FR-029 |

## Convenciones generales

- Fechas en formato `DD/MM/AAAA` en las respuestas destinadas a mostrarse en pantalla
  (Principio II de la constitución); internamente se usa ISO 8601.
- Montos en Soles (`PEN`), con 2 decimales — única moneda soportada (Principio II).
- Errores de validación devuelven `400 Bad Request` con un código y mensaje en español.
