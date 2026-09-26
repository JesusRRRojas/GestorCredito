# API Contracts: Precarga de Datos del Cliente en Onboarding

**Feature**: `005-precarga-datos-onboarding` | **Fecha**: 2026-09-26

No se agrega ningún endpoint nuevo. Se documenta únicamente el cambio de forma en la respuesta
de un endpoint ya existente de `001-gestion-creditos`.

## GET /api/prospectos/{id} *(modificado — Historias 1 y 2)*

**Antes**: devolvía `ProspectoDetalleDto` sin el campo `precarga`.

**Ahora**: agrega el campo opcional `precarga`, presente solo cuando el prospecto todavía no
tiene su propio Onboarding completo y existe un prospecto cerrado calificado del mismo cliente.

**Response 200 OK — ejemplo con precarga (cliente recurrente, cuenta interna previa)**:

```json
{
  "id": 12,
  "tipoDocumentoId": 1,
  "numeroDocumento": "87654321",
  "productoId": 3,
  "productoNombre": "Crédito Personal",
  "productoRequiereRequisitos": true,
  "productoRequiereDeclaracionInversion": false,
  "nombres": null,
  "apellidos": null,
  "direccion": null,
  "tipoCuentaDesembolso": null,
  "cuentaBancariaInternaId": null,
  "cuentaExternaBanco": null,
  "cuentaExternaCCI": null,
  "estado": "Evaluacion",
  "fechaCreacion": "2026-09-26T10:00:00",
  "fechaCierre": null,
  "montoSolicitado": 5000.00,
  "precarga": {
    "nombres": "Juan",
    "apellidos": "Perez",
    "direccion": "Av. Siempre Viva 123",
    "tipoCuentaDesembolso": "Interna",
    "cuentaBancariaInternaId": 4,
    "cuentaExternaBanco": null,
    "cuentaExternaCCI": null
  }
}
```

**Response 200 OK — ejemplo sin precarga (cliente nuevo, o prospecto ya con Onboarding propio)**:

```json
{
  "id": 12,
  "...": "...",
  "nombres": null,
  "precarga": null
}
```

**Sin cambios**: código de estado (`200 OK` / `404 Not Found`), demás campos y el resto de
endpoints de `/api/prospectos` (incluido `PUT /{id}/onboarding`, que sigue guardando
exactamente lo que el Asesor deja en el formulario, precargado o corregido — FR-003).
