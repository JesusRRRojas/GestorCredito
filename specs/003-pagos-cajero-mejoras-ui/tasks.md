---

description: "Task list template for feature implementation"
---

# Tasks: Pagos de Cuota, Rol Cajero y Mejoras de Simulación/Administración

**Input**: Design documents from `specs/003-pagos-cajero-mejoras-ui/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/api-contracts.md, quickstart.md (todos presentes)

**Tests**: No se solicitaron explícitamente en el spec; se incluye únicamente una tarea de
prueba unitaria puntual en Polish (clasificación de estado de crédito), consistente con la
estrategia de pruebas ya usada en `001`/`002` (xUnit acotado a la lógica crítica, no TDD
completo).

**Organization**: Tareas agrupadas por Historia de Usuario (US1–US6, prioridades P1/P2/P3/P3/
P3/P4 según `spec.md`).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Puede ejecutarse en paralelo (archivo distinto, sin dependencias pendientes)
- **[Story]**: Historia de Usuario a la que pertenece la tarea (US1–US6)
- Rutas de archivo exactas en cada descripción, relativas a la raíz del repo.

## Path Conventions

Proyecto web existente: `backend/GestorCredito.Api/`, `backend/GestorCredito.Api.Tests/`,
`frontend/src/app/`. Este feature extiende archivos ya existentes de `001`/`002` y agrega los
nuevos indicados en `plan.md`.

---

## Phase 1: Setup

- [X] T001 Agregar el valor `Cajero` al enum `RolUsuario` (hoy `{ Administrador, Asesor,
  Aprobador }`) en `backend/GestorCredito.Api/Models/Enums.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Registro de pago de cuota + el endpoint de "Créditos" que las Historias 1 y 2
comparten (research.md §2). Ninguna historia puede completarse sin esta fase.

- [X] T002 Extender `backend/GestorCredito.Api/Models/Cuota.cs` (depende de T001 solo por orden
  de fase, sin dependencia real): agregar `Pagada` (bool, default `false`) y `FechaPago`
  (`DateTime?`, nullable; `null` mientras `Pagada = false`) — campos exactos de
  `data-model.md`
- [X] T003 Crear la migración de EF Core (`dotnet ef migrations add
  AgregarPagoCuotaYRolCajero`) en `backend/GestorCredito.Api/Data/Migrations/` (depende de
  T002), incluyendo el script de datos de `research.md` §1: marcar `Pagada = 1` y
  `FechaPagoRealizado = FechaCierre` en toda `Cuota` cuya `Simulacion.Prospecto.Estado = Finalizado`
  (valor 6 del enum `EstadoProspecto`) — FR-014
- [X] T004 [P] Extender el tipo `Rol` en `frontend/src/app/core/rol-activo.service.ts` (hoy
  `'Administrador' | 'Asesor' | 'Aprobador'`) para incluir `'Cajero'`, y actualizar la
  validación de `leerDeSesion()` para aceptar ese valor al leer `sessionStorage`
- [X] T005 Crear `backend/GestorCredito.Api/Services/CreditoCalculator.cs` con un método
  estático que reciba el `Estado` del Prospecto y la lista de `Cuota` de su simulación
  aceptada, y devuelva `EstadoCredito` (`"EnProceso"` | `"Pendiente"` | `"Cancelado"`) y la
  `CuotaActual` (la de menor `Numero` con `Pagada = false`, o `null`), aplicando exactamente
  las reglas de `research.md` §3 y `data-model.md` (EnProceso si `Estado ∉ {Desembolso,
  Finalizado}`; Pendiente si `Estado ∈ {Desembolso, Finalizado}` y alguna Cuota no pagada;
  Cancelado si todas están pagadas)
- [X] T006 Crear `backend/GestorCredito.Api/Endpoints/CreditosEndpoints.cs` con el registro
  `CreditoResumenDto(int ProspectoId, string ProductoNombre, decimal MontoSolicitado, string
  EstadoCredito, CuotaActualDto? CuotaActual)`, `CuotaActualDto(int Numero, DateOnly FechaPago,
  decimal PagoTotal)` y `ClienteConCreditosDto(int TipoDocumentoId, string TipoDocumento,
  string NumeroDocumento, string NombreCompleto, List<CreditoResumenDto> Creditos)`; agregar
  `GET /api/creditos?buscar={texto opcional}` (depende de T005) que: (a) selecciona Prospectos
  con `Nombres` no nulo y `Estado != Rechazado` (FR-010), (b) los agrupa por
  `(TipoDocumentoId, NumeroDocumento)`, (c) si `buscar` viene, filtra por coincidencia parcial
  sin distinguir mayúsculas contra el Nombre completo o el Número de Documento (FR-005), (d)
  usa `CreditoCalculator` para `EstadoCredito`/`CuotaActual` de cada crédito
- [X] T007 Registrar `app.MapCreditosEndpoints();` en `backend/GestorCredito.Api/Program.cs`
  (depende de T006)
- [X] T008 [P] Crear `frontend/src/app/core/services/creditos.service.ts` con las interfaces
  `CuotaActual { numero: number; fechaPago: string; pagoTotal: number; }`,
  `CreditoResumen { prospectoId: number; productoNombre: string; montoSolicitado: number;
  estadoCredito: 'EnProceso' | 'Pendiente' | 'Cancelado'; cuotaActual: CuotaActual | null; }`,
  `ClienteConCreditos { tipoDocumentoId: number; tipoDocumento: string; numeroDocumento:
  string; nombreCompleto: string; creditos: CreditoResumen[]; }`, y el método
  `listar(buscar?: string): Observable<ClienteConCreditos[]>` contra `GET
  {API_BASE_URL}/creditos`

**Checkpoint**: Esquema de datos y endpoint de Créditos listos — las historias de usuario
pueden implementarse.

---

## Phase 3: User Story 1 - Registro de pago de cuotas por el Cajero (Priority: P1) 🎯 MVP

**Goal**: Un usuario con rol Cajero ve una bandeja de todos los clientes registrados (sin
editar), busca por nombre o documento, y registra el pago de la Cuota Actual de un crédito
"Pendiente".

**Independent Test**: Con un cliente con un crédito "Desembolsado" con cuotas sin pagar,
iniciar sesión como Cajero, buscarlo, seleccionar su crédito y registrar el pago,
verificando que la cuota queda pagada y la Cuota Actual avanza (quickstart.md Escenario 1).

### Implementation for User Story 1

- [X] T009 [US1] En `backend/GestorCredito.Api/Endpoints/CreditosEndpoints.cs` (depende de
  T006), agregar `POST /api/creditos/{prospectoId}/pagar-cuota`: recalcula `EstadoCredito` con
  `CreditoCalculator`; si no es `"Pendiente"`, devuelve `409 Conflict` (`{ "error":
  "CREDITO_NO_TIENE_CUOTA_PENDIENTE" }` — FR-006/FR-008, research.md §5); si es `"Pendiente"`,
  marca la Cuota Actual con `Pagada = true` y `FechaPago = DateTime.Now` (FR-001/FR-007) y
  devuelve el `EstadoCredito`/`CuotaActual` recalculados
- [X] T010 [P] [US1] En `frontend/src/app/core/services/creditos.service.ts` (depende de T008),
  agregar el método `pagarCuota(prospectoId: number): Observable<{ estadoCredito: string;
  cuotaActual: CuotaActual | null }>` contra `POST
  {API_BASE_URL}/creditos/{prospectoId}/pagar-cuota`
- [X] T011 [US1] Agregar el botón "Cajero" en
  `frontend/src/app/core/selector-rol/selector-rol.component.html` y su manejo en
  `selector-rol.component.ts` (destino `/creditos-cajero` en `destinoPorRol`, junto a
  `Administrador`/`Asesor`/`Aprobador` ya existentes) — FR-003
- [X] T012 [US1] Crear `frontend/src/app/features/creditos-cajero/creditos-cajero.component.ts`
  y `.html` (depende de T004, T010): input de búsqueda (nombre o Número de Documento, FR-005)
  que llama a `CreditosService.listar(texto)`; lista de clientes con sus créditos (Monto
  Solicitado, estado); al seleccionar un crédito en estado `"Pendiente"`, muestra la Cuota
  Actual (número, fecha, monto) y un botón "Registrar Pago" que llama a `pagarCuota(...)`; los
  créditos `"EnProceso"`/`"Cancelado"` no ofrecen esa acción (FR-006); ningún control de
  edición en toda la pantalla (FR-009)
- [X] T013 [US1] Registrar la ruta `creditos-cajero` (guardada para el rol `Cajero`, `canActivate:
  [rolGuard(['Cajero'])]`) en `frontend/src/app/app.routes.ts`, cargando
  `CreditosCajeroComponent`
- [X] T014 [US1] Agregar la sección de navegación del rol `Cajero` en
  `frontend/src/app/app.component.html` (depende de T013): un único enlace a
  `/creditos-cajero`, siguiendo el mismo patrón `@if (rol === 'Cajero')` que las demás
  secciones de rol

**Checkpoint**: El Cajero puede buscar clientes y registrar pagos de forma independiente
(quickstart.md Escenario 1).

---

## Phase 4: User Story 2 - Consulta de créditos por cliente con estado de pago (Priority: P2)

**Goal**: El Asesor o Administrador, ingresando el Número de Documento de un cliente, ve todos
sus créditos no Rechazados con Monto Solicitado, Cuota Actual y estado.

**Independent Test**: Con un cliente con créditos en distintos estados (incluido uno
Rechazado), buscar por su Número de Documento y verificar que la lista excluye el Rechazado y
muestra el resto con los datos correctos (quickstart.md Escenario 2).

### Implementation for User Story 2

- [X] T015 [US2] Reescribir `frontend/src/app/features/historial/historial.component.ts`
  (depende de T008) para usar `CreditosService.listar(numeroDocumento)` en vez de
  `ProspectosService.historial(...)`, exponiendo el resultado como `ClienteConCreditos[]`
- [X] T016 [US2] Reescribir `frontend/src/app/features/historial/historial.component.html`
  (depende de T015): título "Créditos del Cliente"; tabla con columnas Producto, Monto
  Solicitado, Cuota Actual (número + fecha, o "—" si no aplica) y Estado (`"En proceso"` /
  `"Pendiente"` / `"Cancelado"`, con badge de color); sin columnas de Estado Final/Fecha de
  Cierre (ya no existen en el nuevo modelo de datos)
- [X] T017 [US2] Eliminar el endpoint `GET /api/prospectos/historial` de
  `backend/GestorCredito.Api/Endpoints/ProspectosEndpoints.cs` y el método `historial(...)` de
  `frontend/src/app/core/services/prospectos.service.ts` (depende de T015/T016 — research.md
  §2: sin otro consumidor en el frontend, se retira en vez de mantener dos rutas equivalentes)

**Checkpoint**: La consulta de créditos por cliente funciona de forma independiente
(quickstart.md Escenario 2).

---

## Phase 5: User Story 3 - Suma de columnas en el cronograma de Simulación (Priority: P3)

**Goal**: Tras calcular el cronograma en la Pantalla 2, se muestra una fila con la suma de
Interés, Otros y Pago Total.

**Independent Test**: Generar cualquier simulación y verificar que la fila de totales suma
correctamente esas tres columnas (quickstart.md Escenario 3).

### Implementation for User Story 3

- [X] T018 [US3] En `frontend/src/app/features/simulacion/simulacion.component.ts`, agregar un
  `computed()` `totales` que sume `interes`, `otros` y `pagoTotal` de `this.cuotas()`
  (recalculado automáticamente cada vez que `cuotas` cambia, FR-015)
- [X] T019 [US3] En `frontend/src/app/features/simulacion/simulacion.component.html` (depende
  de T018), agregar una fila `<tfoot>` con "Totales" y los tres valores sumados (formateados
  con el pipe `monedaPe` ya usado en el resto de la tabla), después de la última fila de
  `cuotas()`

**Checkpoint**: Los totales del cronograma funcionan de forma independiente (quickstart.md
Escenario 3).

---

## Phase 6: User Story 4 - Encabezado con datos del cliente y Monto Solicitado (Priority: P3)

**Goal**: Las pantallas posteriores al Onboarding muestran "Nombre — Monto Solicitado" cuando
ambos datos ya existen.

**Independent Test**: Abrir Declaración de Inversión, Requisitos, Aprobación o Desembolso de un
prospecto con Onboarding completo y verificar el encabezado (quickstart.md Escenario 4).

### Implementation for User Story 4

- [X] T020 [US4] En `backend/GestorCredito.Api/Endpoints/ProspectosEndpoints.cs`, agregar
  `MontoSolicitado` (decimal?) a `ProspectoDetalleDto` y calcularlo en `AMapaDetalle`/el
  handler `GET /{id:int}` a partir de `Simulacion.Monto` de la simulación con `Aceptada = true`
  del prospecto (`null` si no tiene ninguna simulación aceptada) — FR-016
- [X] T021 [US4] En `frontend/src/app/core/services/prospectos.service.ts`, agregar
  `montoSolicitado: number | null` a la interfaz `ProspectoDetalle` (depende de T020)
- [X] T022 [US4] Crear `frontend/src/app/shared/encabezado-cliente/encabezado-cliente.component.ts`
  y `.html` (depende de T021): componente standalone con `@Input() prospecto:
  ProspectoDetalle | null`; no renderiza nada si `prospecto` es `null`, si `nombres` está vacío
  o si `montoSolicitado` es `null` (FR-017, edge case del spec); si ambos datos existen,
  muestra "{Nombres} {Apellidos} — {montoSolicitado en S/}" usando el pipe `monedaPe`
- [X] T023 [P] [US4] Insertar `<app-encabezado-cliente [prospecto]="prospecto()" />` al inicio
  de la plantilla y agregar `EncabezadoClienteComponent` a `imports` en
  `frontend/src/app/features/onboarding/onboarding.component.html` y `.ts` (depende de T022)
- [X] T024 [P] [US4] Insertar `<app-encabezado-cliente>` en
  `frontend/src/app/features/declaracion-inversion/declaracion-inversion.component.html` y
  `.ts` (depende de T022) — requiere cargar el `ProspectoDetalle` completo (hoy el componente
  solo usa datos propios de la declaración; agregar la carga vía
  `ProspectosService.obtener(...)`)
- [X] T025 [P] [US4] Insertar `<app-encabezado-cliente [prospecto]="prospecto()" />` en
  `frontend/src/app/features/requisitos/requisitos.component.html` y `.ts` (depende de T022)
- [X] T026 [P] [US4] Insertar `<app-encabezado-cliente [prospecto]="prospecto()" />` en
  `frontend/src/app/features/aprobacion/aprobacion-detalle.component.html` y `.ts` (depende de
  T022)
- [X] T027 [P] [US4] Insertar `<app-encabezado-cliente [prospecto]="prospecto()" />` en
  `frontend/src/app/features/desembolso/desembolso-detalle.component.html` y `.ts` (depende de
  T022)

**Checkpoint**: El encabezado aparece de forma independiente en las 5 pantallas (quickstart.md
Escenario 4).

---

## Phase 7: User Story 5 - Validación del Monto a Invertir contra el Monto Solicitado (Priority: P3)

**Goal**: La Declaración de Inversión no permite un Monto a Invertir mayor al Monto Solicitado.

**Independent Test**: Con un prospecto de Monto Solicitado S/5,000, intentar guardar S/6,000
(rechazado) y luego S/5,000 (aceptado) — quickstart.md Escenario 5.

### Implementation for User Story 5

- [X] T028 [US5] En el handler `PUT /{id:int}/declaracion-inversion` de
  `backend/GestorCredito.Api/Endpoints/ProspectosEndpoints.cs` (depende de T020, mismo cálculo
  de Monto Solicitado), agregar: `400 Bad Request` (`{ "error":
  "MONTO_INVERTIR_SUPERA_MONTO_SOLICITADO" }`) si `dto.MontoInvertir` es mayor al
  `MontoSolicitado` del prospecto (límite inclusive: igual sí se permite) — FR-018
- [X] T029 [US5] En
  `frontend/src/app/features/declaracion-inversion/declaracion-inversion.component.ts`
  (depende de T024, ya carga el `ProspectoDetalle`), validar en el cliente antes de guardar que
  `montoInvertir <= prospecto.montoSolicitado`, mostrando el mismo mensaje de error sin llamar
  al backend si ya se sabe que fallará

**Checkpoint**: La validación funciona de forma independiente en backend y frontend
(quickstart.md Escenario 5).

---

## Phase 8: User Story 6 - Rediseño profesional de las pantallas de Administrador (Priority: P4)

**Goal**: Ningún registro de Administrador queda oculto; Productos tiene un diseño más
cuidado; el Asesor tiene un enlace a su Bandeja en el menú.

**Independent Test**: Con varios registros configurados, verificar que todas las pantallas de
Administrador son completamente accesibles y que Productos luce ordenado (quickstart.md
Escenario 6).

### Implementation for User Story 6

- [X] T030 [P] [US6] Envolver ambas tablas (Tipos de Documento y Tipos de Persona) de
  `frontend/src/app/features/configuracion/tipos-documento/tipos-documento.component.html` en
  un contenedor `<div class="table-responsive">` (mismo patrón ya usado en
  `productos.component.html`, research.md §8) — FR-019
- [X] T031 [P] [US6] Envolver la tabla de
  `frontend/src/app/features/configuracion/usuarios/usuarios.component.html` en un contenedor
  `<div class="table-responsive">` — FR-019
- [X] T032 [US6] Rediseñar
  `frontend/src/app/features/configuracion/productos/productos.component.html`: agrupar la
  tabla y el formulario en tarjetas (`card`) claramente delimitadas con encabezados, usar
  badges de color (`badge bg-success`/`bg-secondary`) en vez de texto "Sí"/"No" para
  `requiereRequisitos`, `requiereDeclaracionInversion` y `activo`, y aplicar espaciado
  consistente (`table-hover`, `align-middle` ya presente); sin cambiar ningún dato ni
  comportamiento funcional (FR-020)
- [X] T033 [US6] Agregar el enlace "Bandeja" (`routerLink="/bandeja"`) a la sección `@if (rol
  === 'Asesor')` de `frontend/src/app/app.component.html`, antes de "Nueva Validación" —
  FR-021

**Checkpoint**: Las 6 historias de usuario funcionan de forma independiente y en conjunto.

---

## Phase 9: Polish & Cross-Cutting Concerns

- [X] T034 [P] Crear `backend/GestorCredito.Api.Tests/EstadoCreditoTests.cs` con pruebas xUnit
  para `CreditoCalculator` (T005): `EnProceso` cuando `Estado` no es Desembolso ni Finalizado;
  `Pendiente` con Cuota Actual correcta cuando hay cuotas sin pagar; `Cancelado` sin Cuota
  Actual cuando todas están pagadas; ambos casos con `Estado = Desembolso` y `Estado =
  Finalizado`
- [X] T035 Ejecutar manualmente los 6 escenarios de
  `specs/003-pagos-cajero-mejoras-ui/quickstart.md` de principio a fin y confirmar que cada
  criterio de aceptación del spec se cumple
- [X] T036 [P] Revisar que todos los textos nuevos (bandeja del Cajero, Créditos del Cliente,
  encabezado, rediseño de Productos) estén en español y usen los formatos peruanos (Soles S/,
  fechas DD/MM/AAAA), conforme al Principio II de la constitución

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sin dependencias — inicia de inmediato.
- **Foundational (Phase 2)**: depende de Setup (T001) — BLOQUEA las 6 historias de usuario.
- **User Stories (Phase 3–8)**: todas dependen de Foundational (Phase 2).
  - US1 y US2 comparten el endpoint `GET /api/creditos` construido en Foundational (T006); son
    independientes entre sí para su propia prueba.
  - US2 (T017) depende de que US2 (T015/T016) ya haya migrado el consumidor antes de eliminar
    el endpoint viejo.
  - US4 (T023–T027) modifica 5 archivos de pantallas ya existentes; cada uno es independiente
    de los demás (marcados `[P]`).
  - US5 (T029) depende de que US4 (T024) ya cargue el `ProspectoDetalle` completo en
    Declaración de Inversión.
  - US3 y US6 son independientes de todas las demás historias.
- **Polish (Phase 9)**: depende de que todas las historias deseadas estén completas.

### User Story Dependencies

- **US1 (P1)**: depende de Foundational (incluye el endpoint de Créditos compartido).
- **US2 (P2)**: depende de Foundational; independiente de US1 en su propia prueba.
- **US3 (P3)**: depende solo de Foundational (en realidad ni siquiera de eso; es
  autocontenida en el frontend, pero se agrupa aquí por orden de prioridad del spec).
- **US4 (P3)**: depende de Foundational; T024 también sirve de base a US5.
- **US5 (P3)**: depende de US4 (T024, carga de `ProspectoDetalle` en Declaración de Inversión).
- **US6 (P4)**: depende solo de Foundational.

### Parallel Opportunities

- Fase 2: T004 y T008 en paralelo con el trabajo backend de T002/T003/T005/T006/T007 (archivos
  distintos).
- Fase 3 (US1): T010 en paralelo con T009 (archivos distintos).
- Fase 6 (US4): T023, T024, T025, T026, T027 completamente en paralelo (5 archivos de pantalla
  distintos, todos dependen solo de T022).
- Fase 8 (US6): T030, T031 en paralelo (archivos distintos); T032 y T033 independientes de
  ambos.
- Fase 9: T034 y T036 en paralelo; T035 al final, tras todo lo demás.

---

## Parallel Example: Foundational (Phase 2)

```bash
Task: "Extender el tipo Rol en frontend/src/app/core/rol-activo.service.ts"
Task: "Crear frontend/src/app/core/services/creditos.service.ts"
# (mientras, en paralelo, se construye el backend: T002 → T003, T005 → T006 → T007)
```

## Parallel Example: User Story 4 (Phase 6)

```bash
Task: "Insertar <app-encabezado-cliente> en onboarding.component.html/.ts"
Task: "Insertar <app-encabezado-cliente> en declaracion-inversion.component.html/.ts"
Task: "Insertar <app-encabezado-cliente> en requisitos.component.html/.ts"
Task: "Insertar <app-encabezado-cliente> en aprobacion-detalle.component.html/.ts"
Task: "Insertar <app-encabezado-cliente> en desembolso-detalle.component.html/.ts"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Completar Fase 1: Setup.
2. Completar Fase 2: Foundational (bloqueante).
3. Completar Fase 3: US1 (Cajero + pago de cuota).
4. **Detener y validar**: probar el pago de cuota de forma independiente (quickstart.md
   Escenario 1).

### Incremental Delivery

1. Setup + Foundational → base lista.
2. US1 (Cajero) → probar de forma independiente → MVP.
3. US2 (Créditos por cliente) → probar de forma independiente.
4. US3 (Totales) → probar de forma independiente.
5. US4 (Encabezado) → probar de forma independiente.
6. US5 (Validación Monto a Invertir) → probar de forma independiente.
7. US6 (Rediseño Administrador) → probar de forma independiente.
8. Polish: prueba unitaria, recorrido completo de `quickstart.md`, revisión de idioma.

---

## Notes

- `[P]` = archivos distintos, sin dependencias pendientes entre sí.
- La etiqueta `[Story]` mapea cada tarea a su Historia de Usuario para trazabilidad con
  `spec.md`.
- Confirmar en `quickstart.md` cada escenario antes de dar por cerrada la historia
  correspondiente.
