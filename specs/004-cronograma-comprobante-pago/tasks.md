---

description: "Task list template for feature implementation"
---

# Tasks: Cronograma Visual y Comprobante de Pago en Bandeja del Cajero

**Input**: Design documents from `specs/004-cronograma-comprobante-pago/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/api-contracts.md, quickstart.md (todos presentes)

**Tests**: No se solicitaron explícitamente en el spec; se incluye únicamente una tarea de
prueba unitaria puntual en Polish (clasificación de estado por cuota), consistente con la
estrategia de pruebas ya usada en `001`/`002`/`003` (xUnit acotado a la lógica crítica, no TDD
completo).

**Organization**: Tareas agrupadas por Historia de Usuario (US1–US2, prioridades P1/P2 según
`spec.md`).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Puede ejecutarse en paralelo (archivo distinto, sin dependencias pendientes)
- **[Story]**: Historia de Usuario a la que pertenece la tarea (US1–US2)
- Rutas de archivo exactas en cada descripción, relativas a la raíz del repo.

## Path Conventions

Proyecto web existente: `backend/GestorCredito.Api/`, `backend/GestorCredito.Api.Tests/`,
`frontend/src/app/`. Este feature extiende archivos ya existentes de `003` (bandeja del
Cajero, `CreditosEndpoints.cs`, `creditos.service.ts`, `creditos-cajero.component.*`), sin
crear pantallas nuevas.

---

## Phase 1: Foundational (Blocking Prerequisites)

**Purpose**: Esquema de datos compartido por ambas historias — el cronograma (US1) expone
`NumeroOperacion`/evidencia de cuotas ya pagadas, y el registro de pago (US2) es quien los
llena. Ninguna historia puede completarse sin esta fase.

- [X] T001 Extender `backend/GestorCredito.Api/Models/Cuota.cs`: agregar `NumeroOperacion`
  (`string?`, `null` mientras `Pagada = false`; no vacío ni solo espacios al momento de marcar
  la cuota como pagada — FR-005), `EvidenciaNombreArchivo` (`string?`, nombre original del
  archivo adjunto, `null` si no se adjuntó ninguno) y `EvidenciaContenido` (`byte[]?`,
  contenido binario del archivo, máximo 5 MB, formato JPG/PNG/PDF, `null` si no se adjuntó
  ninguno) — campos exactos de `data-model.md`
- [X] T002 Crear la migración de EF Core (`dotnet ef migrations add
  AgregarNumeroOperacionYEvidenciaCuota`) en `backend/GestorCredito.Api/Data/Migrations/`
  (depende de T001), sin script de datos adicional (las cuotas ya pagadas antes de esta
  versión quedan con `NumeroOperacion = null`, lo cual es válido: FR-009 solo exige mostrarlo
  cuando exista)

**Checkpoint**: Esquema de datos listo — ambas historias de usuario pueden implementarse.

---

## Phase 2: User Story 1 - Cronograma visual con estado por cuota (Priority: P1) 🎯 MVP

**Goal**: Al seleccionar un crédito, el Cajero ve el cronograma completo de cuotas con un
indicador visual de estado (pagada / actual / futura) y un resumen de cuántas faltan.

**Independent Test**: Con un cliente con un crédito "Pendiente" con cuotas pagadas, una actual
y futuras, seleccionar el crédito como Cajero y verificar que el cronograma completo se
muestra con el estado correcto por cuota y el resumen de avance (quickstart.md Historia 1).

### Implementation for User Story 1

- [X] T003 [US1] En `backend/GestorCredito.Api/Services/CreditoCalculator.cs` (depende de
  T001), agregar un método que reciba la lista de `Cuota` de la simulación aceptada y
  devuelva, para cada una, su `EstadoCuota`: `"Pagada"` si `Pagada = true`; `"Actual"` para la
  de menor `Numero` entre las no pagadas; `"Futura"` para el resto de no pagadas
  (`research.md` §1 — misma regla ya usada para calcular la Cuota Actual del crédito, sin
  duplicar la fuente de verdad)
- [X] T004 [US1] En `backend/GestorCredito.Api/Endpoints/CreditosEndpoints.cs` (depende de
  T003), agregar los registros `CronogramaCuotaDto(int Numero, DateOnly FechaPago, decimal
  PagoTotal, string EstadoCuota, string? NumeroOperacion, bool TieneEvidencia)` y
  `CronogramaCreditoDto(string EstadoCredito, int CuotasPagadas, int TotalCuotas,
  List<CronogramaCuotaDto> Cuotas)`, y el endpoint `GET
  /api/creditos/{prospectoId}/cronograma` que: (a) devuelve `404 Not Found` si el Prospecto no
  existe; (b) devuelve `409 Conflict` con `{ "error": "CREDITO_SIN_CRONOGRAMA" }` si no tiene
  simulación aceptada o el crédito está `"EnProceso"` (FR-004); (c) en otro caso, devuelve las
  cuotas de la simulación aceptada ordenadas por `Numero`, con su `EstadoCuota` (T003),
  `TieneEvidencia = EvidenciaContenido != null`, y el resumen `CuotasPagadas`/`TotalCuotas`
  (FR-001, FR-002, FR-003)
- [X] T005 [P] [US1] En `frontend/src/app/core/services/creditos.service.ts`, agregar las
  interfaces `CronogramaCuota { numero: number; fechaPago: string; pagoTotal: number;
  estadoCuota: 'Pagada' | 'Actual' | 'Futura'; numeroOperacion: string | null; tieneEvidencia:
  boolean; }` y `CronogramaCredito { estadoCredito: EstadoCredito; cuotasPagadas: number;
  totalCuotas: number; cuotas: CronogramaCuota[]; }`, y el método
  `obtenerCronograma(prospectoId: number): Observable<CronogramaCredito>` contra `GET
  {API_BASE_URL}/creditos/{prospectoId}/cronograma`
- [X] T006 [US1] En `frontend/src/app/features/creditos-cajero/creditos-cajero.component.ts`
  (depende de T005), al seleccionar un crédito llamar a
  `CreditosService.obtenerCronograma(prospectoId)`, guardando el resultado en un signal
  `cronograma`; si la respuesta es `409` con `CREDITO_SIN_CRONOGRAMA`, guardar en `mensaje` el
  texto "Este crédito todavía no tiene cronograma (aún no ha sido desembolsado)." en vez de
  fallar silenciosamente (FR-004)
- [X] T007 [US1] En
  `frontend/src/app/features/creditos-cajero/creditos-cajero.component.html` (depende de
  T006), reemplazar el bloque actual de "Cuota Actual" por una tabla del cronograma completo
  (columnas Número, Fecha de Pago, Monto, Estado y Número de Operación) cuando
  `cronograma()` tiene datos: cada fila usa un badge distinto por `estadoCuota` (`bg-success`
  "Pagada", `bg-warning` "Actual", `bg-secondary` "Futura" — FR-002), la columna Número de
  Operación muestra el valor o "—" si es `null`, y arriba de la tabla se muestra el resumen
  "`{cuotasPagadas}` de `{totalCuotas}` cuotas pagadas" (FR-003); cuando en cambio hay un
  mensaje de "sin cronograma" (T006), se muestra ese mensaje en vez de la tabla (FR-004)

**Checkpoint**: El cronograma visual funciona de forma independiente (quickstart.md Historia
1).

---

## Phase 3: User Story 2 - Número de operación y evidencia opcional al registrar el pago (Priority: P2)

**Goal**: Al confirmar el pago de la Cuota Actual, el Cajero debe ingresar un Número de
Operación obligatorio y puede adjuntar opcionalmente un archivo de evidencia.

**Independent Test**: Con un crédito "Pendiente" con Cuota Actual disponible, intentar
confirmar el pago sin Número de Operación (debe bloquearse), luego con Número de Operación sin
archivo (debe aceptarse) y finalmente con Número de Operación y un archivo adjunto (debe
aceptarse y guardar ambos) — quickstart.md Historia 2.

### Implementation for User Story 2

- [X] T008 [US2] En `backend/GestorCredito.Api/Endpoints/CreditosEndpoints.cs` (depende de
  T001), modificar `POST /api/creditos/{prospectoId}/pagar-cuota` para recibir
  `multipart/form-data` con `string numeroOperacion` (obligatorio) e `IFormFile? archivo`
  (opcional): devuelve `400 Bad Request` con `{ "error": "NUMERO_OPERACION_REQUERIDO" }` si
  `numeroOperacion` está vacío o solo contiene espacios en blanco (FR-005); si `archivo` viene
  y no es JPG/PNG/PDF o supera 5 MB, devuelve `400 Bad Request` con `{ "error":
  "ARCHIVO_EVIDENCIA_INVALIDO" }` (FR-007, mismo estilo de validación de formato ya usado en
  `ProspectosEndpoints.cs` para los PDF de Requisitos); si todo es válido, guarda
  `cuota.NumeroOperacion = numeroOperacion.Trim()` y, si hay archivo,
  `cuota.EvidenciaNombreArchivo`/`EvidenciaContenido` (copiados a `MemoryStream`, mismo patrón
  que `DocumentoAdjunto`), antes de marcar `Pagada = true` (FR-006, FR-008); agregar
  `.DisableAntiforgery()` al endpoint (mismo requisito ya presente en el endpoint de subida de
  archivo de Requisitos, `ProspectosEndpoints.cs`, para parámetros `IFormFile` en Minimal APIs)
  y anotar `numeroOperacion` con `[FromForm]` (un `string` simple junto a un `IFormFile` no se
  enlaza como campo de formulario por inferencia automática en Minimal APIs)
- [X] T009 [US2] En `backend/GestorCredito.Api/Endpoints/CreditosEndpoints.cs` (depende de
  T001, después de T008), agregar `GET
  /api/creditos/{prospectoId}/cuotas/{numero}/evidencia`: busca la `Cuota` por
  `prospectoId`+`numero`; si no existe o `EvidenciaContenido` es `null`, devuelve `404 Not
  Found`; si existe, devuelve el archivo con `Results.File(...)` usando
  `EvidenciaNombreArchivo` y el `ContentType` correspondiente a su extensión (`image/jpeg`,
  `image/png` o `application/pdf`)
- [X] T010 [US2] En `frontend/src/app/core/services/creditos.service.ts` (depende de T005),
  cambiar `pagarCuota(prospectoId: number)` para recibir `(prospectoId: number,
  numeroOperacion: string, archivo: File | null)` y enviarlos como `FormData` (`numeroOperacion`
  + `archivo` si no es `null`) al mismo endpoint `POST
  {API_BASE_URL}/creditos/{prospectoId}/pagar-cuota`; agregar el método
  `urlEvidencia(prospectoId: number, numero: number): string` que arma la URL `GET
  {API_BASE_URL}/creditos/{prospectoId}/cuotas/{numero}/evidencia`
- [X] T011 [US2] En
  `frontend/src/app/features/creditos-cajero/creditos-cajero.component.ts` (depende de T006,
  T010), agregar el estado del formulario de pago (`numeroOperacion: string`,
  `archivoEvidencia: File | null`), un manejador `onArchivoSeleccionado(event: Event)` que
  toma el archivo del input, y actualizar `registrarPago(prospectoId)` para llamar a
  `CreditosService.pagarCuota(prospectoId, this.numeroOperacion, this.archivoEvidencia)`,
  limpiando el formulario y recargando el cronograma (T006) tras un pago exitoso
- [X] T012 [US2] En
  `frontend/src/app/features/creditos-cajero/creditos-cajero.component.html` (depende de T007,
  T011), reemplazar el botón directo "Registrar Pago" por un formulario con un campo de texto
  obligatorio "Número de Operación" (`[(ngModel)]="numeroOperacion"`, con mensaje de error
  visible si se intenta enviar vacío) y un campo de archivo opcional "Evidencia (opcional)"
  (`(change)="onArchivoSeleccionado($event)"`), con el botón "Registrar Pago" deshabilitado
  mientras `numeroOperacion` esté vacío o solo tenga espacios (FR-005); en la columna Número de
  Operación de la tabla del cronograma (T007), cuando `tieneEvidencia` sea `true`, agregar un
  enlace "Ver evidencia" que abra `CreditosService.urlEvidencia(prospectoId, numero)` en una
  nueva pestaña (FR-009)

**Checkpoint**: El registro de pago con Número de Operación y evidencia opcional funciona de
forma independiente (quickstart.md Historia 2).

---

## Phase 4: Polish & Cross-Cutting Concerns

- [X] T013 [P] Extender `backend/GestorCredito.Api.Tests/EstadoCreditoTests.cs` (depende de
  T003) con pruebas xUnit para la clasificación por cuota: `"Pagada"` para cuotas con `Pagada
  = true`; `"Actual"` para la de menor `Numero` entre las no pagadas; `"Futura"` para el resto
  de no pagadas; caso con todas pagadas (ninguna `"Actual"` ni `"Futura"`)
- [X] T014 Ejecutar manualmente las 2 historias de
  `specs/004-cronograma-comprobante-pago/quickstart.md` de principio a fin y confirmar que
  cada criterio de aceptación del spec se cumple, incluyendo los casos de error (Número de
  Operación vacío, archivo de formato/tamaño inválido)
- [X] T015 [P] Revisar que todos los textos nuevos (cronograma, formulario de pago, mensajes de
  error) estén en español y usen los formatos peruanos (Soles S/, fechas DD/MM/AAAA),
  conforme al Principio II de la constitución

---

## Dependencies & Execution Order

### Phase Dependencies

- **Foundational (Phase 1)**: sin dependencias — inicia de inmediato; BLOQUEA ambas historias
  de usuario.
- **User Story 1 (Phase 2)**: depende de Foundational (Phase 1).
- **User Story 2 (Phase 3)**: depende de Foundational (Phase 1); reutiliza `creditos.service.ts`
  (T005) y el estado de selección de crédito (T006) ya creados por US1, por lo que en la
  práctica se implementa después de US1, aunque su prueba de aceptación (quickstart.md Historia
  2) es independiente de si US1 ya está o no visualmente terminada.
- **Polish (Phase 4)**: depende de que ambas historias estén completas.

### User Story Dependencies

- **US1 (P1)**: depende solo de Foundational (Phase 1).
- **US2 (P2)**: depende de Foundational (Phase 1) y, por compartir archivos (`creditos.service.ts`,
  `creditos-cajero.component.ts/html`), se construye sobre el trabajo de US1 en esos mismos
  archivos; sigue siendo verificable de forma independiente con su propio recorrido de
  `quickstart.md`.

### Parallel Opportunities

- Fase 1: T001 y T002 son secuenciales (T002 depende de T001); no hay paralelismo real en esta
  fase (una sola entidad modificada).
- Fase 2 (US1): T005 (frontend) en paralelo con T003→T004 (backend), ya que el contrato ya
  está definido en `contracts/api-contracts.md`.
- Fase 4: T013 y T015 en paralelo; T014 al final, tras todo lo demás.

---

## Parallel Example: User Story 1 (Phase 2)

```bash
Task: "Agregar obtenerCronograma() e interfaces en frontend/src/app/core/services/creditos.service.ts"
# (mientras, en paralelo, se construye el backend: T003 → T004)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Completar Fase 1: Foundational (bloqueante).
2. Completar Fase 2: US1 (cronograma visual).
3. **Detener y validar**: probar el cronograma de forma independiente (quickstart.md Historia
   1).

### Incremental Delivery

1. Foundational → base lista.
2. US1 (Cronograma visual) → probar de forma independiente → MVP.
3. US2 (Número de Operación + evidencia) → probar de forma independiente.
4. Polish: prueba unitaria, recorrido completo de `quickstart.md`, revisión de idioma.

---

## Notes

- `[P]` = archivos distintos, sin dependencias pendientes entre sí.
- La etiqueta `[Story]` mapea cada tarea a su Historia de Usuario para trazabilidad con
  `spec.md`.
- Confirmar en `quickstart.md` cada escenario antes de dar por cerrada la historia
  correspondiente.
