---

description: "Task list template for feature implementation"
---

# Tasks: Seguimiento Avanzado de Prospectos (Bandeja, Onboarding Bancario y Comentarios de Aprobación)

**Input**: Design documents from `specs/002-seguimiento-avanzado-prospectos/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/api-contracts.md, quickstart.md (todos presentes)

**Tests**: No se solicitaron explícitamente en el spec; se incluye únicamente una tarea de
prueba unitaria puntual en Polish, consistente con la estrategia de pruebas ya usada en
`001-gestion-creditos` (xUnit acotado a la validación crítica, no TDD completo).

**Organization**: Tareas agrupadas por Historia de Usuario (US1–US5, prioridades P1/P2/P2/P3/
P2 según `spec.md`) para permitir implementación y prueba independientes.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Puede ejecutarse en paralelo (archivo distinto, sin dependencias pendientes)
- **[Story]**: Historia de Usuario a la que pertenece la tarea (US1–US5)
- Rutas de archivo exactas en cada descripción, relativas a la raíz del repo.

## Path Conventions

Proyecto web existente (`001-gestion-creditos`): `backend/GestorCredito.Api/`,
`backend/GestorCredito.Api.Tests/`, `frontend/src/app/`. Este feature extiende archivos ya
existentes y agrega los nuevos indicados en `plan.md`.

---

## Phase 1: Setup

**Purpose**: Prerrequisito mínimo compartido por el resto del feature (nuevos enums usados por
las entidades de la Fase 2).

- [X] T001 [P] Agregar los enums `TipoCuentaDesembolso { Interna, Externa }` y
  `OrigenFondos { Ahorros, Herencia, VentaActivo, ActividadEmpresarial, Otro }` en
  `backend/GestorCredito.Api/Models/Enums.cs` (data-model.md: campos `Prospecto.TipoCuentaDesembolso`
  y `DeclaracionInversion.OrigenFondos`)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Entidades, `DbContext` y migración de base de datos que TODAS las historias de
usuario necesitan (la bandeja de US1 debe poder consultar estas tablas para calcular el "paso
actual", aunque las pantallas que las alimentan se construyan en US2/US3).

**⚠️ CRITICAL**: Ninguna historia de usuario puede completarse sin esta fase.

- [X] T002 [P] Crear el modelo `CuentaBancariaInterna` en
  `backend/GestorCredito.Api/Models/CuentaBancariaInterna.cs` con los campos exactos de
  `data-model.md`: `Id` (int, PK, autogenerado), `TipoDocumentoId` (int, FK a `TipoDocumento`,
  requerido), `NumeroDocumento` (string(20), requerido), `NumeroCuenta` (string(30), requerido),
  `Moneda` (enum `{ PEN }`, fijo en Soles), `Activo` (bool, default `true`), `FechaRegistro`
  (datetime, requerido)
- [X] T003 [P] Crear el modelo `DeclaracionInversion` en
  `backend/GestorCredito.Api/Models/DeclaracionInversion.cs` con los campos exactos de
  `data-model.md`: `Id` (int, PK), `ProspectoId` (int, FK a `Prospecto`, único — a lo sumo una
  Declaración por Prospecto), `MontoInvertir` (decimal(18,2), requerido, `> 0`), `OrigenFondos`
  (enum `{ Ahorros, Herencia, VentaActivo, ActividadEmpresarial, Otro }`, requerido), `Detalle`
  (string(500), nullable, opcional), `FechaRegistro` (datetime, requerido)
- [X] T004 Extender `backend/GestorCredito.Api/Models/Prospecto.cs` (depende de T002): eliminar
  la propiedad `CuentaBancariaCCI`; agregar `TipoCuentaDesembolso` (enum `{ Interna, Externa }`,
  requerido al completar el Onboarding), `CuentaBancariaInternaId` (int?, FK nullable a
  `CuentaBancariaInterna`, requerido solo cuando `TipoCuentaDesembolso = Interna` y debe
  pertenecer al mismo cliente — mismo `TipoDocumentoId` + `NumeroDocumento` del Prospecto),
  `CuentaExternaBanco` (string(150), nullable, requerido solo cuando `TipoCuentaDesembolso =
  Externa`), `CuentaExternaCCI` (string(20), nullable, requerido solo cuando `Externa`;
  numérico, exactamente 20 dígitos), `ComentarioAprobador` (string(1000), nullable)
- [X] T005 [P] Extender `backend/GestorCredito.Api/Models/Producto.cs`: agregar
  `RequiereDeclaracionInversion` (bool, default `false`), independiente de
  `RequiereRequisitos`
- [X] T006 Registrar `DbSet<CuentaBancariaInterna>` y `DbSet<DeclaracionInversion>` y configurar
  en `OnModelCreating` de `backend/GestorCredito.Api/Data/AppDbContext.cs` (depende de T002,
  T003, T004, T005): índice único de `CuentaBancariaInterna` sobre `(TipoDocumentoId,
  NumeroDocumento, NumeroCuenta)` ("no puede existir más de una `CuentaBancariaInterna` con el
  mismo `(TipoDocumentoId, NumeroDocumento, NumeroCuenta)`" — data-model.md); relación 1:1 de
  `DeclaracionInversion.ProspectoId` con índice único; `MontoInvertir` como
  `decimal(18,2)`; en `Prospecto`, `MaxLength` de `CuentaExternaBanco` (150),
  `CuentaExternaCCI` (20) y `ComentarioAprobador` (1000); eliminar el mapeo de
  `CuentaBancariaCCI`
- [X] T007 Crear la migración de EF Core (`dotnet ef migrations add
  AgregarCuentaBancariaYDeclaracionInversion`) en
  `backend/GestorCredito.Api/Data/Migrations/` (depende de T006), incluyendo el paso de datos
  de `research.md` §8: migrar el valor existente de `CuentaBancariaCCI` (si lo hay) a
  `TipoCuentaDesembolso = Externa`, `CuentaExternaCCI = CuentaBancariaCCI`,
  `CuentaExternaBanco = null`, antes de eliminar la columna `CuentaBancariaCCI`

**Checkpoint**: Esquema de datos listo — las historias de usuario pueden implementarse.

---

## Phase 3: User Story 1 - Bandeja de seguimiento del Asesor (Priority: P1) 🎯 MVP

**Goal**: El Asesor ve todos sus prospectos activos con nombre, documento y estado resumido
(Bloqueado, En proceso, Aprobación, Desembolsado, Observado), y navega directamente al paso
donde quedó cada uno.

**Independent Test**: Con prospectos en distintos estados, abrir la bandeja y verificar que
cada fila muestra los datos correctos y que el clic navega a la pantalla exacta (quickstart.md
Escenario 1).

### Implementation for User Story 1

- [X] T008 [US1] En `backend/GestorCredito.Api/Endpoints/ProspectosEndpoints.cs`, agregar el
  registro `BandejaItemDto(int ProspectoId, string? NombreCliente, string TipoDocumento, string
  NumeroDocumento, string EstadoResumen, string? PasoActual, string? ComentarioAprobador)` y un
  método privado que calcule `EstadoResumen` y `PasoActual` según `research.md` §1: `Estado =
  Simulacion` → `PasoActual = "Simulacion"`; `Estado = Evaluacion` con Onboarding incompleto
  (`Nombres`/`Apellidos`/`Direccion`/cuenta de desembolso vacíos) → `"Onboarding"`; Onboarding
  completo, `Producto.RequiereDeclaracionInversion = true` y no existe `DeclaracionInversion`
  para el prospecto → `"DeclaracionInversion"`; ese paso resuelto (o no aplica) y
  `Producto.RequiereRequisitos = true` con Requisitos incompletos → `"Requisitos"`; `Estado =
  Desembolso` → `"Desembolso"`; mapeo de `EstadoResumen`: `Rechazado` → `"Bloqueado"`;
  `Simulacion`/`Evaluacion`/`Desembolso` → `"EnProceso"`; `Aprobacion` → `"Aprobacion"`;
  `Finalizado` → `"Desembolsado"`; `Observado` → `"Observado"` (siempre con `PasoActual =
  "Onboarding"` para este último, FR-003)
- [X] T009 [US1] En `backend/GestorCredito.Api/Endpoints/ProspectosEndpoints.cs` (depende de
  T008), agregar `grupo.MapGet("/bandeja", ...)` que devuelva la lista de `BandejaItemDto` para
  todos los prospectos no cerrados y los recientemente cerrados con comentario (FR-001)
- [X] T010 [P] [US1] En `frontend/src/app/core/services/prospectos.service.ts`, agregar la
  interfaz `BandejaItem` (mismos campos que `BandejaItemDto`) y el método `bandeja(): Observable
  <BandejaItem[]>` que llame a `GET {API_BASE_URL}/prospectos/bandeja`
- [X] T011 [US1] Crear `frontend/src/app/features/bandeja/bandeja.component.ts` y
  `bandeja.component.html` (depende de T010): tabla con Nombre del Cliente, Tipo de Documento,
  Número de Documento y una etiqueta de estado (Bloqueado, En proceso, Aprobación, Desembolsado,
  Observado); al hacer clic en una fila con `estadoResumen` distinto de `Bloqueado`,
  `Aprobacion` o `Desembolsado`, navegar según `pasoActual`: `Simulacion` → `/simulacion/:id`,
  `Onboarding` → `/onboarding/:id`, `DeclaracionInversion` → `/declaracion-inversion/:id`,
  `Requisitos` → `/requisitos/:id`, `Desembolso` → `/desembolso/:id` (FR-002/FR-003)
- [X] T012 [US1] Registrar la ruta `bandeja` (guardada para el rol `Asesor`) en
  `frontend/src/app/app.routes.ts` (depende de T011), cargando
  `BandejaComponent`

**Checkpoint**: La bandeja lista y navega correctamente (los pasos `DeclaracionInversion` y la
selección de cuenta interna/externa del Onboarding se completan visualmente al implementar US2
y US3).

---

## Phase 4: User Story 2 - Selección de cuenta bancaria en el Onboarding (Priority: P2)

**Goal**: El Onboarding pregunta si la cuenta de desembolso es Interna (reutilizando cuentas ya
registradas del cliente o agregando una nueva) o Externa (Banco + CCI).

**Independent Test**: Completar el Onboarding con cuenta Interna (eligiendo o agregando una) y,
en otro caso, con cuenta Externa, verificando que ambos casos se guardan correctamente
(quickstart.md Escenario 2).

### Implementation for User Story 2

- [X] T013 [P] [US2] Crear `backend/GestorCredito.Api/Endpoints/CuentasInternasEndpoints.cs` con
  los registros `CuentaInternaDto(int Id, string NumeroCuenta, string Moneda)` y
  `CuentaInternaGuardarDto(string NumeroCuenta)`, y los endpoints: `GET
  /api/clientes/{tipoDocumentoId}/{numeroDocumento}/cuentas-internas` (lista cuentas activas de
  ese cliente) y `POST` en la misma ruta (crea una cuenta nueva con `Moneda = PEN` fijo;
  `409 Conflict` si ya existe una `CuentaBancariaInterna` con el mismo `(TipoDocumentoId,
  NumeroDocumento, NumeroCuenta)`, edge case del spec)
- [X] T014 [US2] Registrar `app.MapCuentasInternasEndpoints();` en
  `backend/GestorCredito.Api/Program.cs` (depende de T013)
- [X] T015 [P] [US2] En `backend/GestorCredito.Api/Endpoints/ProspectosEndpoints.cs`, modificar
  `OnboardingDto` para reemplazar `CuentaBancariaCCI` por `TipoCuentaDesembolso` ("Interna" |
  "Externa"), `CuentaBancariaInternaId` (int?, requerido si Interna) y `CuentaExterna` (record
  con `Banco` string y `Cci` string, requerido si Externa); en el handler `PUT
  /{id:int}/onboarding`: validar que exactamente uno de los dos conjuntos de datos esté
  presente según `TipoCuentaDesembolso`; si Interna, validar que
  `CuentaBancariaInternaId` referencia una `CuentaBancariaInterna` cuyo `(TipoDocumentoId,
  NumeroDocumento)` coincide con el del Prospecto (`400 Bad Request` si no); si Externa, validar
  que el CCI "debe ser numérica de exactamente 20 dígitos" (mismo mensaje/regla ya usada para
  `CuentaBancariaCCI`); agregar el guard de estado (FR-016/FR-017): devolver `409 Conflict`
  (`{ "error": "EDICION_NO_PERMITIDA_EN_ESTADO_ACTUAL" }`) si el `Estado` actual es
  `Aprobacion`, `Rechazado` o `Finalizado`
- [X] T016 [P] [US2] Crear `frontend/src/app/core/services/cuentas-internas.service.ts` con la
  interfaz `CuentaInterna { id: number; numeroCuenta: string; moneda: 'PEN'; }` y los métodos
  `listar(tipoDocumentoId, numeroDocumento)` y `agregar(tipoDocumentoId, numeroDocumento,
  numeroCuenta)` contra las rutas de T013
- [X] T017 [P] [US2] En `frontend/src/app/core/services/prospectos.service.ts`, reemplazar
  `cuentaBancariaCCI` en `ProspectoDetalle` por `tipoCuentaDesembolso: 'Interna' | 'Externa'`,
  `cuentaBancariaInternaId: number | null`, `cuentaExternaBanco: string | null`,
  `cuentaExternaCCI: string | null`; actualizar la firma de `guardarOnboarding(...)` para
  enviar el nuevo cuerpo (`tipoCuentaDesembolso` + `cuentaBancariaInternaId` o
  `cuentaExterna: { banco, cci }`) en vez de `cuentaBancariaCCI`
- [X] T018 [US2] Rediseñar la sección de cuenta bancaria en
  `frontend/src/app/features/onboarding/onboarding.component.ts` y `.html` (depende de T016,
  T017): selector "Interna"/"Externa"; si Interna, listar cuentas vía
  `CuentasInternasService.listar(...)` con opción "Agregar cuenta nueva" (solo Número de Cuenta;
  Moneda fija en Soles, sin selector ya que `Moneda { PEN }` es el único valor); si Externa,
  campos Nombre del Banco y CCI (validación de 20 dígitos en el formulario, además de la del
  backend)

**Checkpoint**: US1 + US2 funcionan juntas — la bandeja puede navegar a un Onboarding que ya
soporta cuenta interna/externa.

---

## Phase 5: User Story 3 - Etapa de Declaración de Inversión para productos específicos (Priority: P3)

**Goal**: Para Productos marcados por el Administrador, el flujo agrega una pantalla de
Declaración de Inversión entre el Onboarding y Requisitos/Aprobación.

**Independent Test**: Con un Producto que requiere la etapa, verificar que aparece tras el
Onboarding; con uno que no la requiere, verificar que se omite (quickstart.md Escenario 3).

### Implementation for User Story 3

- [X] T019 [P] [US3] En `backend/GestorCredito.Api/Endpoints/ProductosEndpoints.cs`, agregar
  `RequiereDeclaracionInversion` (bool) a `ProductoDto` y `ProductoGuardarDto`, y asignarlo en
  los handlers `POST`/`PUT` (independiente de `RequiereRequisitos`)
- [X] T020 [P] [US3] En `backend/GestorCredito.Api/Endpoints/ProspectosEndpoints.cs`, agregar el
  registro `DeclaracionInversionDto(decimal MontoInvertir, string OrigenFondos, string? Detalle)`
  y los endpoints `GET /{id:int}/declaracion-inversion` (`404` si no existe) y `PUT
  /{id:int}/declaracion-inversion` (valida `MontoInvertir > 0`, `OrigenFondos` dentro de
  `{ Ahorros, Herencia, VentaActivo, ActividadEmpresarial, Otro }`, `Detalle` opcional hasta 500
  caracteres; `409 Conflict` si `Producto.RequiereDeclaracionInversion = false` para ese
  prospecto)
- [X] T021 [P] [US3] En `frontend/src/app/core/services/prospectos.service.ts`, agregar la
  interfaz `DeclaracionInversion { montoInvertir: number; origenFondos: 'Ahorros' | 'Herencia' |
  'VentaActivo' | 'ActividadEmpresarial' | 'Otro'; detalle: string | null; }` y los métodos
  `obtenerDeclaracionInversion(prospectoId)` / `guardarDeclaracionInversion(prospectoId, dto)`
- [X] T022 [US3] Crear `frontend/src/app/features/declaracion-inversion/declaracion-inversion.component.ts`
  y `.html` (depende de T021): formulario con Monto a Invertir, Origen de Fondos (lista
  desplegable con las 5 opciones cerradas) y Detalle opcional
- [X] T023 [US3] Registrar la ruta `declaracion-inversion/:prospectoId` (rol `Asesor`) en
  `frontend/src/app/app.routes.ts`, y actualizar el método `continuar()` de
  `frontend/src/app/features/onboarding/onboarding.component.ts` (depende de T018 de US2 y
  T022) para navegar a `/declaracion-inversion/:id` cuando el Producto del prospecto tenga
  `requiereDeclaracionInversion = true`, o a `/requisitos/:id` en caso contrario (manteniendo el
  comportamiento Fast-Track ya existente cuando el Producto tampoco requiere Requisitos)
- [X] T024 [P] [US3] Agregar el checkbox "Requiere Declaración de Inversión" a
  `frontend/src/app/features/configuracion/productos/productos.component.ts` y `.html` (depende
  de T019), guardado junto con el resto del formulario de Producto

**Checkpoint**: Las tres primeras historias funcionan juntas: bandeja, cuenta bancaria y
Declaración de Inversión condicional.

---

## Phase 6: User Story 4 - Comentario obligatorio del Aprobador (Priority: P2)

**Goal**: El Aprobador debe ingresar un comentario (máx. 1000 caracteres) al Observar o
Rechazar, visible luego en un tooltip en la bandeja del Asesor.

**Independent Test**: Intentar guardar "Observado"/"Rechazado" sin comentario y verificar que
el sistema lo impide; verificar que el comentario aparece en el tooltip de la bandeja
(quickstart.md Escenario 4).

### Implementation for User Story 4

- [X] T025 [US4] En `backend/GestorCredito.Api/Endpoints/ProspectosEndpoints.cs`, modificar
  `DecisionDto` para agregar `Comentario` (string?) y, en el handler `POST
  /{id:int}/decision`: si `Decision` es `"Observado"` o `"Rechazado"`, exigir que `Comentario`
  no esté vacío y tenga como máximo 1000 caracteres (`400 Bad Request` si falta o excede el
  límite); si `Decision` es `"Aprobado"`, no exigirlo; en cualquier caso, guardar el valor
  recibido en `Prospecto.ComentarioAprobador`
- [X] T026 [P] [US4] En `frontend/src/app/core/services/prospectos.service.ts`, agregar el
  parámetro `comentario?: string` al método `decidir(...)`, enviándolo en el body de la petición
- [X] T027 [US4] En `frontend/src/app/features/aprobacion/aprobacion-detalle.component.ts` y
  `.html` (depende de T026), agregar un `<textarea>` de comentario (con `maxlength="1000"`) y
  deshabilitar los botones "Observado"/"Rechazado" mientras el comentario esté vacío; el botón
  "Aprobado" permanece habilitado sin comentario
- [X] T028 [US4] En `frontend/src/app/features/bandeja/bandeja.component.html` (depende de T011
  de US1), agregar un tooltip de Bootstrap sobre la etiqueta de estado que muestre
  `comentarioAprobador` cuando el prospecto esté `Observado` (o recién `Bloqueado` por rechazo
  con comentario)

**Checkpoint**: El comentario del Aprobador es obligatorio donde corresponde y visible en la
bandeja.

---

## Phase 7: User Story 5 - Edición de datos del cliente desde la bandeja (Priority: P2)

**Goal**: El Asesor edita los datos del cliente (Nombres, Apellidos, Dirección, Cuenta
Bancaria) desde la bandeja, en cualquier estado activo salvo "Aprobación".

**Independent Test**: Editar un dato simple de un prospecto "En proceso" desde la bandeja y
verificar que se refleja después; confirmar que la edición se bloquea en estado "Aprobación"
(quickstart.md Escenario 5).

### Implementation for User Story 5

- [X] T029 [US5] En `frontend/src/app/features/bandeja/bandeja.component.html` (depende de
  T011), agregar una acción "Editar cliente" por fila que navegue a `/onboarding/:id`,
  visible únicamente cuando `estadoResumen` no sea `Aprobacion`, `Bloqueado` ni `Desembolsado`
  (FR-015/FR-016/FR-017)
- [X] T030 [US5] En `frontend/src/app/features/onboarding/onboarding.component.ts` (depende de
  T015 de US2 y T018 de US2), agregar el manejo del error `409 Conflict`
  (`EDICION_NO_PERMITIDA_EN_ESTADO_ACTUAL`) devuelto por el backend, mostrando un mensaje claro
  al Asesor en vez de un error genérico

**Checkpoint**: Las 5 historias de usuario funcionan de forma independiente y en conjunto.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Ajustes que afectan a varias historias.

- [X] T031 [P] Actualizar `GenerarAprobacionFinal` en
  `backend/GestorCredito.Api/Services/PdfGenerator.cs` para imprimir la Cuenta Bancaria de
  Desembolso vigente (Interna: Número de Cuenta + Moneda; Externa: Banco + CCI) en vez de la
  línea `Cuenta Bancaria (CCI): {prospecto.CuentaBancariaCCI}` ya eliminada (FR-011)
- [X] T032 [P] Crear `backend/GestorCredito.Api.Tests/CuentaBancariaValidationTests.cs` con
  pruebas xUnit para la validación del CCI de 20 dígitos (cuenta externa) y de la unicidad
  `(TipoDocumentoId, NumeroDocumento, NumeroCuenta)` de `CuentaBancariaInterna`
- [X] T033 Ejecutar manualmente los 5 escenarios de `specs/002-seguimiento-avanzado-prospectos/quickstart.md`
  de principio a fin y confirmar que cada criterio de aceptación del spec se cumple
- [X] T034 [P] Revisar que todos los textos nuevos (Bandeja, Declaración de Inversión, cuenta
  Interna/Externa, comentario del Aprobador) estén en español y usen los formatos peruanos
  (Soles S/, fechas DD/MM/AAAA), conforme al Principio II de la constitución

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sin dependencias — inicia de inmediato.
- **Foundational (Phase 2)**: depende de Setup (T001) — BLOQUEA las 5 historias de usuario.
- **User Stories (Phase 3–7)**: todas dependen de Foundational (Phase 2).
  - US1 es independiente de US2–US5 para su propia prueba (lista y navega), aunque el destino
    de navegación de los pasos `DeclaracionInversion` y la sección bancaria del Onboarding se
    completan visualmente al construir US2/US3.
  - US4 (T028) y US5 (T029) modifican el mismo archivo que crea US1 (`bandeja.component.html`,
    T011) — deben ejecutarse después de US1.
  - US3 (T023) modifica el mismo archivo que US2 (`onboarding.component.ts`, T018) — debe
    ejecutarse después de US2.
  - US5 (T030) depende del guard de estado agregado en US2 (T015).
- **Polish (Phase 8)**: depende de que todas las historias deseadas estén completas.

### User Story Dependencies

- **US1 (P1)**: solo depende de Foundational.
- **US2 (P2)**: solo depende de Foundational; independiente de US1 en su propia prueba
  (Onboarding funciona sin la bandeja).
- **US3 (P3)**: depende de Foundational; su tarea de integración (T023) toca el mismo archivo
  que US2 (T018), por lo que se implementa después de US2.
- **US4 (P2)**: depende de Foundational; su tooltip (T028) se apoya en el componente de US1
  (T011).
- **US5 (P2)**: depende de Foundational, del guard de US2 (T015/T018) y del componente de US1
  (T011).

### Parallel Opportunities

- Fase 1: T001 (única tarea, no requiere paralelismo).
- Fase 2: T002, T003, T005 en paralelo; T004 tras T002; T006 tras T002–T005; T007 tras T006.
- Fase 3 (US1): T010 en paralelo con el desarrollo backend de T008/T009 (archivos distintos).
- Fase 4 (US2): T013, T015, T016, T017 en paralelo (archivos distintos); T014 tras T013; T018
  tras T016 y T017.
- Fase 5 (US3): T019, T020, T021, T024 en paralelo (archivos distintos); T022 tras T021; T023
  tras T018 (US2) y T022.
- Fase 6 (US4): T026 en paralelo con el trabajo backend de T025; T027 tras T026; T028 tras T011
  (US1).
- Fase 8: T031, T032, T034 en paralelo; T033 al final, tras todo lo demás.

---

## Parallel Example: Foundational (Phase 2)

```bash
# Lanzar en paralelo (archivos distintos, sin dependencias entre sí):
Task: "Crear el modelo CuentaBancariaInterna en backend/GestorCredito.Api/Models/CuentaBancariaInterna.cs"
Task: "Crear el modelo DeclaracionInversion en backend/GestorCredito.Api/Models/DeclaracionInversion.cs"
Task: "Extender backend/GestorCredito.Api/Models/Producto.cs con RequiereDeclaracionInversion"
```

## Parallel Example: User Story 2 (Phase 4)

```bash
# Lanzar en paralelo (archivos distintos):
Task: "Crear backend/GestorCredito.Api/Endpoints/CuentasInternasEndpoints.cs"
Task: "Modificar OnboardingDto y el handler PUT /onboarding en ProspectosEndpoints.cs"
Task: "Crear frontend/src/app/core/services/cuentas-internas.service.ts"
Task: "Actualizar ProspectoDetalle y guardarOnboarding en prospectos.service.ts"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Completar Fase 1: Setup.
2. Completar Fase 2: Foundational (bloqueante).
3. Completar Fase 3: US1 (Bandeja).
4. **Detener y validar**: probar la bandeja de forma independiente (quickstart.md Escenario 1,
   con navegación limitada a los pasos ya existentes de `001` — Simulación, Onboarding,
   Requisitos, Desembolso).

### Incremental Delivery

1. Setup + Foundational → base lista.
2. US1 (Bandeja) → probar de forma independiente → MVP.
3. US2 (Cuenta Interna/Externa) → probar de forma independiente.
4. US3 (Declaración de Inversión) → probar de forma independiente.
5. US4 (Comentario del Aprobador) → probar de forma independiente.
6. US5 (Edición desde la bandeja) → probar de forma independiente.
7. Polish: PDF, prueba unitaria, recorrido completo de `quickstart.md`, revisión de idioma.

---

## Notes

- `[P]` = archivos distintos, sin dependencias pendientes entre sí.
- La etiqueta `[Story]` mapea cada tarea a su Historia de Usuario para trazabilidad con
  `spec.md`.
- Cada Historia de Usuario debe poder completarse y probarse de forma independiente según su
  "Independent Test", aunque algunas tareas de integración (US3→US2, US4/US5→US1) toquen
  archivos ya creados por una historia anterior — esto es intencional y se documenta en
  "Dependencies".
- Confirmar en `quickstart.md` cada escenario antes de dar por cerrada la historia
  correspondiente.
