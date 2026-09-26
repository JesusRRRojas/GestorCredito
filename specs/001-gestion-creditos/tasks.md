---

description: "Task list template for feature implementation"
---

# Tasks: Gestión de Créditos (Prospecto a Desembolso)

**Input**: Design documents from `/specs/001-gestion-creditos/`

**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Solo se incluyen pruebas automatizadas donde `research.md` §10 lo decidió
explícitamente (cálculo del cronograma). El resto de la validación es manual, guiada por
`quickstart.md` y los criterios de aceptación del spec (Principio V de la constitución).

**Organization**: Las tareas se agrupan por historia de usuario (spec.md) para poder
implementar y probar cada una de forma independiente.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Puede ejecutarse en paralelo (archivos distintos, sin dependencias pendientes)
- **[Story]**: Historia de usuario a la que pertenece (US1–US4, más US5 para el historial
  de prospectos añadido en `/speckit-clarify`, FR-029)
- Se incluye la ruta de archivo exacta en cada descripción

## Path Conventions

Proyecto web (backend + frontend), según `plan.md`:
- Backend: `backend/GestorCredito.Api/` (+ `backend/GestorCredito.Api.Tests/`)
- Frontend: `frontend/src/app/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Inicialización de los proyectos backend y frontend

- [X] T001 Crear la estructura de carpetas del repositorio (`backend/`, `frontend/`, cada uno
      con sus subcarpetas) según `plan.md` → Project Structure
- [X] T002 Inicializar el proyecto backend `backend/GestorCredito.Api` (.NET 10, plantilla
      Web API vacía) y agregar los paquetes `Microsoft.EntityFrameworkCore.SqlServer`,
      `Microsoft.EntityFrameworkCore.Design` y `PdfSharpCore`
- [X] T003 [P] Inicializar el proyecto frontend en `frontend/` con Angular CLI y agregar el
      paquete npm `bootstrap` (sin build de Sass personalizado, per research.md §9)
- [X] T004 [P] Ejecutar `dotnet user-secrets init` en `backend/GestorCredito.Api` y registrar
      `ConnectionStrings:Default` con la cadena de conexión local (research.md §8); confirmar
      que `backend/GestorCredito.Api/appsettings.json` no contiene ninguna contraseña
- [X] T005 [P] Añadir un `.gitignore` en la raíz del repositorio excluyendo `bin/`, `obj/`,
      `node_modules/` y cualquier archivo de configuración local con secretos
- [X] T006 [P] Crear las carpetas `Data/`, `Models/`, `Endpoints/`, `Services/` dentro de
      `backend/GestorCredito.Api`
- [X] T007 [P] Crear las carpetas `core/`, `features/`, `shared/` dentro de
      `frontend/src/app`
- [X] T008 [P] Crear el proyecto de pruebas `backend/GestorCredito.Api.Tests` (xUnit) con
      referencia a `backend/GestorCredito.Api`

**Checkpoint**: Ambos proyectos compilan/arrancan vacíos; listo para la fase Foundational.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Infraestructura común que TODAS las historias de usuario necesitan

**⚠️ CRÍTICO**: Ninguna historia de usuario puede implementarse hasta cerrar esta fase

- [X] T009 Crear `AppDbContext` en `backend/GestorCredito.Api/Data/AppDbContext.cs` (nota: se
      implementó con todos los `DbSet`s y relaciones desde el inicio —incluyendo los de las
      historias US2-US4— para poder generar y aplicar una única migración inicial consistente
      contra SQL Server, en vez de 4 migraciones incrementales separadas; el resultado final
      es el mismo esquema que describe data-model.md)
- [X] T010 Configurar EF Core + SQL Server y la inyección de dependencias en
      `backend/GestorCredito.Api/Program.cs`, leyendo `ConnectionStrings:Default` desde la
      configuración/user-secrets (nunca un valor embebido en el código)
- [X] T011 Crear los enums compartidos en `backend/GestorCredito.Api/Models/Enums.cs`:
      `RolUsuario { Administrador, Asesor, Aprobador }`,
      `Moneda { PEN }` (único valor soportado — Principio II de la constitución),
      `FrecuenciaPago { Mensual }` (único valor soportado en v0, FR-002),
      `DiaPago { Dia5, Dia25 }`,
      `EstadoProspecto { Simulacion, Evaluacion, Aprobacion, Observado, Rechazado, Desembolso, Finalizado }`,
      `TipoDocumentoGenerado { Cronograma, AprobacionFinal }`
- [X] T012 [P] Implementar `RolActivoService` en `frontend/src/app/core/rol-activo.service.ts`
      que guarda el rol elegido (Administrador/Asesor/Aprobador) para la sesión del
      navegador, sin usuario ni contraseña (FR-005, research.md §4)
- [X] T013 [P] Implementar un guard de rutas por rol en `frontend/src/app/core/rol.guard.ts`
      que use `RolActivoService` para mostrar/ocultar cada `feature` según el rol activo
- [X] T014 [P] Crear el componente selector de rol en
      `frontend/src/app/core/selector-rol/selector-rol.component.ts`: pantalla mostrada al
      ingresar a la aplicación con las opciones Administrador/Asesor/Aprobador, que guarda la
      elección en `RolActivoService` y redirige a la pantalla principal de ese rol (FR-005)
      (depende de T012)
- [X] T015 [P] Implementar los pipes compartidos de moneda y fecha en
      `frontend/src/app/shared/pipes/moneda.pipe.ts` y
      `frontend/src/app/shared/pipes/fecha-pe.pipe.ts` (formato peruano: símbolo S/, fechas
      `DD/MM/AAAA` — Principio II de la constitución)
- [X] T016 Implementar `MockRiesgoService` en
      `backend/GestorCredito.Api/Services/MockRiesgoService.cs`: genera un entero aleatorio
      entre 1 y 5 en cada llamada (FR-007)
- [X] T017 Implementar `CronogramaCalculator` en
      `backend/GestorCredito.Api/Services/CronogramaCalculator.cs` aplicando la fórmula de
      `research.md` §6: Amortización = `redondear(Monto ÷ Plazo, 2)` constante salvo la
      última cuota (que ajusta el saldo final a 0.00), Interés = `redondear(SaldoInicial ×
      TasaPeriódica, 2)` sobre saldo insoluto (decreciente), Otros = cargo fijo del Producto,
      Fecha de pago según el Día de Pago (5 o 25) del Producto
- [X] T018 [P] Implementar el esqueleto de `PdfGenerator` en
      `backend/GestorCredito.Api/Services/PdfGenerator.cs` (PdfSharpCore) con dos métodos:
      generar PDF de Cronograma y generar PDF de Aprobación Final (nota: se implementó el
      contenido completo de ambos métodos en esta fase, no solo el esqueleto, ya que las
      historias US2/US4 solo necesitan invocarlos)

**Checkpoint**: Fundamentos listos; las historias de usuario pueden implementarse.

---

## Phase 3: User Story 1 - Configuración de parámetros core (Priority: P1) 🎯 MVP

**Goal**: El Administrador puede configurar Tipos de Documento/Persona, Productos (con sus
rangos y el indicador `requiere_requisitos`), Requisitos por Producto y Usuarios.

**Independent Test**: Como Administrador, crear un Tipo de Documento, un Producto con sus
rangos (marcado con o sin requisitos) y verificar que ambos quedan listados y editables.

### Implementation for User Story 1

- [X] T019 [P] [US1] Crear el modelo `TipoDocumento` en
      `backend/GestorCredito.Api/Models/TipoDocumento.cs`: `Nombre` (string(100), requerido,
      único), `Activo` (bool, default true)
- [X] T020 [P] [US1] Crear el modelo `TipoPersona` en
      `backend/GestorCredito.Api/Models/TipoPersona.cs`: `Nombre` (string(100), requerido,
      único), `Activo` (bool, default true)
- [X] T021 [US1] Configurar en `AppDbContext` (`backend/GestorCredito.Api/Data/AppDbContext.cs`)
      la relación N:M `TipoDocumento`↔`TipoPersona` mediante la tabla de asociación
      `TipoDocumentoTipoPersona` (depende de T019, T020)
- [X] T022 [P] [US1] Crear el modelo `Producto` en
      `backend/GestorCredito.Api/Models/Producto.cs`: `Nombre` (string(150), requerido),
      `Moneda` (enum de un solo valor `PEN`, fijo en Soles S/ — Principio II de la
      constitución), `MontoMin`/`MontoMax` (decimal(18,2); `MontoMin > 0` y
      `MontoMax >= MontoMin`), `TasaMin`/`TasaMax` (decimal(5,2); `TasaMax >= TasaMin >= 0`),
      `PlazoMin`/`PlazoMax` (int; `PlazoMax >= PlazoMin >= 1`), `FrecuenciaPago` (enum, único
      valor `Mensual`), `DiaPago` (enum `Dia5`/`Dia25`), `CargoOtrosPorCuota` (decimal(18,2)),
      `RequiereRequisitos` (bool), `Activo` (bool, default true)
- [X] T023 [P] [US1] Crear el modelo `Requisito` en
      `backend/GestorCredito.Api/Models/Requisito.cs`: `ProductoId` (FK, requerido), `Nombre`
      (string(150), requerido), `Activo` (bool, default true)
- [X] T024 [P] [US1] Crear el modelo `Usuario` en
      `backend/GestorCredito.Api/Models/Usuario.cs`: `Nombre` (string(150), requerido), `Rol`
      (enum Administrador/Asesor/Aprobador, requerido), `Activo` (bool, default true)
- [X] T025 [US1] Añadir `DbSet<Producto>`, `DbSet<Requisito>`, `DbSet<Usuario>` y las
      validaciones de rango (Fluent API o `CHECK` constraints) a `AppDbContext` (depende de
      T022, T023, T024)
- [X] T026 [US1] Generar la migración EF Core `AddConfiguracionCore` (TipoDocumento,
      TipoPersona, Producto, Requisito, Usuario) en
      `backend/GestorCredito.Api/Data/Migrations/` (depende de T021, T025) — consolidada
      en la migración única `InitialCreate` (ver nota de T009)
- [X] T027 [US1] Implementar el grupo de endpoints de Tipos de Documento/Persona
      (`GET/POST /api/tipos-documento`, `PUT/DELETE /api/tipos-documento/{id}` y sus
      equivalentes para `/api/tipos-persona`) en
      `backend/GestorCredito.Api/Endpoints/TiposEndpoints.cs`
- [X] T028 [US1] Implementar el grupo de endpoints de Productos (`GET/POST /api/productos`,
      `GET/PUT/DELETE /api/productos/{id}`) en
      `backend/GestorCredito.Api/Endpoints/ProductosEndpoints.cs`, validando los rangos
      min/max al crear o editar; el `GET /api/productos` DEBE aceptar el parámetro de
      consulta `activos=true` para devolver solo Productos con `Activo = true` (usado por el
      selector de la Pantalla 2, FR-011)
- [X] T029 [US1] Implementar el grupo de endpoints de Requisitos
      (`GET/POST /api/productos/{id}/requisitos`, `PUT/DELETE /api/requisitos/{id}`) en
      `backend/GestorCredito.Api/Endpoints/RequisitosEndpoints.cs`
- [X] T030 [US1] Implementar el grupo de endpoints de Usuarios (`GET/POST /api/usuarios`,
      `PUT/DELETE /api/usuarios/{id}`) en
      `backend/GestorCredito.Api/Endpoints/UsuariosEndpoints.cs`
- [X] T031 [US1] Registrar los cuatro grupos de endpoints de Configuración en
      `backend/GestorCredito.Api/Program.cs` (depende de T027-T030)
- [X] T032 [P] [US1] Crear la pantalla Angular de Tipos de Documento/Persona en
      `frontend/src/app/features/configuracion/tipos-documento/` (listado + formulario)
      consumiendo `/api/tipos-documento` y `/api/tipos-persona`
- [X] T033 [P] [US1] Crear la pantalla Angular de Productos en
      `frontend/src/app/features/configuracion/productos/` (listado + formulario con rangos
      de monto/tasa/plazo, moneda fija Soles S/, día de pago, cargo "Otros" y el checkbox
      `requiere_requisitos`) consumiendo `/api/productos`
- [X] T034 [US1] Crear la subvista de Requisitos de un Producto en
      `frontend/src/app/features/configuracion/productos/requisitos/`, consumiendo
      `/api/productos/{id}/requisitos` (depende de T033)
- [X] T035 [P] [US1] Crear la pantalla Angular de Usuarios en
      `frontend/src/app/features/configuracion/usuarios/` (listado + formulario) consumiendo
      `/api/usuarios`
- [X] T036 [US1] Añadir la sección de navegación "Configuración" visible solo para el rol
      Administrador en el menú principal / rutas de `frontend/src/app/` (depende de T013,
      T014)

**Checkpoint**: User Story 1 completamente funcional y probable de forma independiente.

---

## Phase 4: User Story 2 - Validación de riesgo y simulación de crédito (Priority: P2)

**Goal**: El Asesor valida el riesgo de un prospecto (Mock) y, si es favorable, simula un
cronograma de crédito dentro de los rangos del Producto elegido y lo acepta.

**Independent Test**: Con al menos un Producto configurado (US1), validar un documento,
elegir Producto, simular y aceptar el cronograma.

### Tests for User Story 2

> Prueba unitaria del cálculo, decidida en `research.md` §10 por su criticidad numérica.

- [X] T037 [P] [US2] Prueba unitaria `CronogramaCalculatorTests` en
      `backend/GestorCredito.Api.Tests/CronogramaCalculatorTests.cs`: interés decreciente
      sobre saldo insoluto cuota a cuota, ajuste de la última cuota para que el saldo final
      sea exactamente 0.00 cuando `Monto ÷ Plazo` no es exacto, y fecha de pago correcta
      según Día de Pago (5 o 25)

### Implementation for User Story 2

- [X] T038 [P] [US2] Crear el modelo `Prospecto` en
      `backend/GestorCredito.Api/Models/Prospecto.cs`: `TipoDocumentoId` (FK, requerido),
      `NumeroDocumento` (string(20), requerido), `ResultadoMock` (int, 1–5), `ProductoId`
      (FK, nullable), `Nombres`/`Apellidos` (string(150)), `Direccion` (string(250)),
      `CuentaBancariaCCI` (string(20), numérico, exactamente 20 dígitos), `Estado` (enum
      `EstadoProspecto`), `FechaCreacion` (datetime), `FechaCierre` (datetime, nullable)
- [X] T039 [P] [US2] Crear el modelo `Simulacion` en
      `backend/GestorCredito.Api/Models/Simulacion.cs`: `ProspectoId`/`ProductoId` (FK,
      requeridos), `Monto`/`Tasa`/`Plazo` (deben caer dentro de los rangos del Producto
      elegido), `FechaSimulacion` (datetime), `Aceptada` (bool, `true` solo al presionar
      "Aceptar"), `FechaAceptacion` (datetime, nullable)
- [X] T040 [P] [US2] Crear el modelo `Cuota` en `backend/GestorCredito.Api/Models/Cuota.cs`:
      `SimulacionId` (FK, requerido), `Numero` (int, 1..Plazo), `FechaPago` (date),
      `SaldoInicial`/`Amortizacion`/`Interes`/`Otros`/`PagoTotal`/`SaldoFinal` (decimal(18,2))
- [X] T041 [US2] Añadir `DbSet<Prospecto>`, `DbSet<Simulacion>`, `DbSet<Cuota>` a
      `AppDbContext` junto con la validación de unicidad "un Prospecto activo (Estado no en
      Rechazado/Finalizado) por Número de Documento" (FR-010) (depende de T038-T040)
- [X] T042 [US2] Generar la migración EF Core `AddProspectosSimulaciones` en
      `backend/GestorCredito.Api/Data/Migrations/` (depende de T041) — consolidada en la
      migración única `InitialCreate` (ver nota de T009)
- [X] T043 [US2] Implementar el endpoint `POST /api/prospectos/validaciones` en
      `backend/GestorCredito.Api/Endpoints/ProspectosEndpoints.cs`: valida unicidad
      (FR-010) antes de generar el Mock, invoca `MockRiesgoService` (FR-007), y solo crea el
      `Prospecto` si el resultado es 1–3 (FR-008/FR-009); si es 4 o 5 responde el resultado
      sin persistir nada, permitiendo reintento inmediato (FR-008)
- [X] T044 [US2] Implementar el endpoint `POST /api/prospectos/{id}/simulaciones` (calcula el
      cronograma sin persistir, validando Monto/Tasa/Plazo contra los rangos del Producto —
      FR-012) usando `CronogramaCalculator` (depende de T017, T043)
- [X] T045 [US2] Implementar el endpoint `POST /api/prospectos/{id}/simulaciones/aceptar`
      (persiste `Simulacion` + `Cuota`s y cambia `Prospecto.Estado` a `Evaluacion` — FR-015)
      (depende de T044)
- [X] T046 [US2] Implementar el endpoint
      `GET /api/prospectos/{id}/simulaciones/actual/pdf` generando el PDF del cronograma con
      `PdfGenerator` (FR-016) (depende de T018, T045)
- [X] T047 [US2] Registrar los endpoints de Prospectos/Simulación en
      `backend/GestorCredito.Api/Program.cs` (depende de T043-T046)
- [X] T048 [P] [US2] Crear la Pantalla 1 (Validación Inicial) en
      `frontend/src/app/features/validacion/`, consumiendo `POST /api/prospectos/validaciones`:
      bloquea y muestra error si el Mock es 4/5, habilita "Siguiente" si es 1-3
- [X] T049 [P] [US2] Crear la Pantalla 2 (Simulación) en
      `frontend/src/app/features/simulacion/`: selector de Producto (solo activos,
      `?activos=true`), campos Monto/Tasa/Plazo que limitan sus valores mínimo/máximo según
      el Producto elegido (CA3), botón "Simular" (recalcula sin guardar, CA1) y botón
      "Aceptar" (persiste y avanza)
- [X] T050 [US2] Añadir la acción "Descargar PDF del cronograma" en
      `frontend/src/app/features/simulacion/` tras aceptar, consumiendo
      `GET /api/prospectos/{id}/simulaciones/actual/pdf` (depende de T046, T049)

**Checkpoint**: User Stories 1 y 2 funcionan de forma independiente.

---

## Phase 5: User Story 3 - Onboarding, requisitos y aprobación condicional (Priority: P3)

**Goal**: El Asesor registra los datos de Onboarding; según si el Producto requiere
requisitos, se adjuntan documentos y decide un Aprobador, o se salta directo a Desembolso.

**Independent Test**: Con un prospecto con simulación aceptada (US2), completar el Onboarding
y verificar que deriva al Caso A (Requisitos → Aprobación) o al Caso B (Desembolso
automático) según la configuración del Producto.

### Implementation for User Story 3

- [X] T051 [P] [US3] Crear el modelo `DocumentoAdjunto` en
      `backend/GestorCredito.Api/Models/DocumentoAdjunto.cs`: `ProspectoId`/`RequisitoId`
      (FK, requeridos), `NombreArchivo` (string(255)), `Contenido` (varbinary(max)),
      `FechaCarga` (datetime)
- [X] T052 [US3] Añadir `DbSet<DocumentoAdjunto>` a `AppDbContext` y generar la migración
      `AddDocumentosAdjuntos` (depende de T051) — consolidada en la migración única
      `InitialCreate` (ver nota de T009)
- [X] T053 [US3] Implementar el endpoint `PUT /api/prospectos/{id}/onboarding` (guarda
      Nombres, Apellidos, Dirección y Cuenta Bancaria validando que sea numérica de
      exactamente 20 dígitos — FR-017/FR-018, y marca el prospecto en fase `Evaluacion` —
      FR-019) en `backend/GestorCredito.Api/Endpoints/ProspectosEndpoints.cs`
- [X] T054 [US3] Implementar el endpoint `GET /api/prospectos/{id}/requisitos` (lista los
      Requisitos del Producto del prospecto; vacío si `RequiereRequisitos = false` — FR-020)
- [X] T055 [US3] Implementar el endpoint
      `POST /api/prospectos/{id}/requisitos/{requisitoId}/archivo` (sube o reemplaza el PDF
      de ese Requisito, rechazando cualquier archivo que no sea PDF — FR-020) (depende de
      T052)
- [X] T056 [US3] Implementar el endpoint `POST /api/prospectos/{id}/requisitos/completar`:
      si `Producto.RequiereRequisitos = true` cambia el estado a `Aprobacion` (FR-021), pero
      DEBE rechazar la operación (sin cambiar de estado) si el Producto no tiene ningún
      Requisito configurado, ya que no habría nada que el Asesor pudiera completar (edge
      case del spec); si `RequiereRequisitos = false`, omite Requisitos/Aprobación, cambia el
      estado a `Desembolso` y autogenera el `DocumentoGenerado` de Aprobación Final (FR-022)
      (depende de T018, T054, T055)
- [X] T057 [US3] Implementar el endpoint `GET /api/prospectos?estado=Aprobacion` (bandeja del
      Aprobador — FR-023)
- [X] T058 [US3] Implementar el endpoint `GET /api/prospectos/{id}/requisitos/archivos` (ver
      los PDFs adjuntos de un prospecto — FR-023)
- [X] T059 [US3] Implementar el endpoint `POST /api/prospectos/{id}/decision` con body
      `{ "decision": "Aprobado" | "Observado" | "Rechazado" }` (FR-024): `Observado` regresa
      el prospecto a `Evaluacion` para que el Asesor edite datos/reemplace PDFs (FR-025);
      `Rechazado` cierra el proceso sin pasar a Desembolso (FR-026)
- [X] T060 [US3] Registrar los endpoints de Onboarding/Requisitos/Aprobación en
      `backend/GestorCredito.Api/Program.cs` (depende de T053-T059)
- [X] T061 [P] [US3] Crear la Pantalla 3 (Onboarding) en
      `frontend/src/app/features/onboarding/` con validación de Cuenta Bancaria (numérica,
      20 dígitos exactos)
- [X] T062 [P] [US3] Crear la Pantalla 4 (Requisitos) en
      `frontend/src/app/features/requisitos/`: Caso A muestra la lista de Requisitos y
      permite subir PDFs, deshabilitando "Completar" si la lista está vacía; Caso B se omite
      automáticamente sin mostrar la interfaz de subida (CA2)
- [X] T063 [P] [US3] Crear la Pantalla 5 (Aprobación) en
      `frontend/src/app/features/aprobacion/` (visible solo para el rol Aprobador): bandeja
      de prospectos en `Aprobacion`, visor de los PDFs adjuntos, botones Aprobado/Observado/
      Rechazado (implementada como dos componentes: `aprobacion.component` para la bandeja
      y `aprobacion-detalle.component` para el detalle/decisión, según `app.routes.ts`)
- [X] T064 [US3] Habilitar en `frontend/src/app/features/onboarding/` y
      `frontend/src/app/features/requisitos/` la edición de datos y el reemplazo de PDFs
      cuando el prospecto vuelve en estado `Observado` (CA4) (depende de T059, T061, T062)

**Checkpoint**: User Stories 1, 2 y 3 funcionan de forma independiente.

---

## Phase 6: User Story 4 - Desembolso y cierre del proceso (Priority: P4)

**Goal**: El Asesor genera el PDF final de Desembolso (con la Cuenta Bancaria del Onboarding)
y cierra el proceso del prospecto.

**Independent Test**: Con un prospecto en estado `Desembolso` (por aprobación o Fast-Track),
generar el PDF final, verificar sus datos y cerrar el proceso.

### Implementation for User Story 4

- [X] T065 [P] [US4] Crear el modelo `DocumentoGenerado` en
      `backend/GestorCredito.Api/Models/DocumentoGenerado.cs`: `ProspectoId` (FK), `Tipo`
      (enum `Cronograma`/`AprobacionFinal`), `Contenido` (varbinary(max)),
      `FechaGeneracion` (datetime)
- [X] T066 [US4] Añadir `DbSet<DocumentoGenerado>` a `AppDbContext` y generar la migración
      `AddDocumentosGenerados` (depende de T065) — consolidada en la migración única
      `InitialCreate` (ver nota de T009)
- [X] T067 [US4] Implementar el endpoint `GET /api/prospectos?estado=Desembolso` (bandeja de
      prospectos listos para desembolsar)
- [X] T068 [US4] Implementar el endpoint
      `GET /api/prospectos/{id}/aprobacion-final/pdf` (genera/descarga el PDF de Aprobación
      Final incluyendo la Cuenta Bancaria del Onboarding y los datos del cronograma aceptado
      — FR-027, CA5) usando `PdfGenerator` (depende de T018, T066)
- [X] T069 [US4] Implementar el endpoint `POST /api/prospectos/{id}/cerrar` (cambia el estado
      a `Finalizado`, registra `FechaCierre`, liberando el Número de Documento para un nuevo
      prospecto — FR-028)
- [X] T070 [US4] Registrar los endpoints de Desembolso en
      `backend/GestorCredito.Api/Program.cs` (depende de T067-T069)
- [X] T071 [P] [US4] Crear la Pantalla 6 (Desembolso) en
      `frontend/src/app/features/desembolso/`: bandeja de prospectos listos, botón "Generar
      PDF final" y botón "Cerrar proceso" (implementada como `desembolso.component` para
      la bandeja y `desembolso-detalle.component` para la acción, según `app.routes.ts`)

**Checkpoint**: Las 4 historias de usuario del spec original funcionan de forma
independiente.

---

## Phase 7: Historial de prospectos cerrados (FR-029 / SC-007)

> Requisito añadido durante `/speckit-clarify`; no tenía una historia de usuario dedicada en
> el spec original, por lo que se agrupa aquí bajo la etiqueta `[US5]` solo para trazabilidad.

**Goal**: El Asesor o el Administrador pueden consultar el historial de prospectos cerrados
(Rechazados o Finalizados) de un cliente.

**Independent Test**: Con al menos un prospecto en estado `Rechazado` o `Finalizado`,
consultarlo por su Número de Documento y verificar que aparece con su estado final y fecha
de cierre.

- [X] T072 [US5] Implementar el endpoint
      `GET /api/prospectos/historial?numeroDocumento=...` (prospectos con `Estado` en
      `{Rechazado, Finalizado}` — FR-029) en
      `backend/GestorCredito.Api/Endpoints/ProspectosEndpoints.cs`
- [X] T073 [US5] Registrar el endpoint de Historial en
      `backend/GestorCredito.Api/Program.cs` (depende de T072)
- [X] T074 [P] [US5] Crear la pantalla de Historial en
      `frontend/src/app/features/historial/` (visible para Asesor y Administrador),
      mostrando Número de Documento, Producto, estado final y fecha de cierre

**Checkpoint**: Todas las historias de usuario, incluido el historial, funcionan de forma
independiente.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Verificaciones finales que afectan a todas las historias

- [X] T075 [P] Ejecutar manualmente el recorrido completo de `quickstart.md` (las 6
      pantallas + historial) y confirmar cada criterio de aceptación del spec, incluyendo
      cronometrar que la validación + simulación (SC-001) tome menos de 3 minutos —
      ejecutado con automatización de navegador (Playwright) recorriendo las 6 pantallas +
      historial end-to-end (Caso A y Caso B), sin errores de consola ni de red; cada paso
      individual tomó ~1s, muy por debajo de los 3 minutos de SC-001
- [X] T076 [P] Verificar que `backend/GestorCredito.Api/appsettings.json` no contiene ninguna
      cadena de conexión ni secreto (Principio VI de la constitución)
- [X] T077 [P] Revisar que todos los textos y fechas visibles en `frontend/src/app/` estén en
      español, con montos en Soles (S/) y fechas `DD/MM/AAAA` (Principio II de la
      constitución) — se encontró y corrigió un bug real de zona horaria en
      `fecha-pe.pipe.ts` (una fecha "solo día" del backend, ej. "2026-10-05", se mostraba
      un día antes al pasar por `new Date(...)` con interpretación UTC/local); se corrigió
      leyendo los componentes de fecha directamente del string, sin conversión de zona
      horaria
- [X] T078 Limpiar código de plantilla (`dotnet new` / `ng new`) que haya quedado sin usar en
      `backend/GestorCredito.Api` y `frontend/` — se eliminaron `UnitTest1.cs` (scaffold de
      xUnit) y `app.component.spec.ts`/`app.component.css` (scaffold de Angular no usado
      tras reescribir `AppComponent`)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sin dependencias — puede iniciar de inmediato
- **Foundational (Phase 2)**: depende de Setup — bloquea todas las historias de usuario
- **User Stories (Phase 3-7)**: todas dependen de Foundational
  - US1 (Phase 3) no depende de otras historias
  - US2 (Phase 4) necesita al menos un Producto configurado (dato de US1) para probarse,
    aunque su código no depende de los endpoints de US1
  - US3 (Phase 5) necesita un Prospecto con simulación aceptada (dato de US2)
  - US4 (Phase 6) necesita un Prospecto en estado `Desembolso` (dato de US3, o Fast-Track)
  - US5 / Historial (Phase 7) necesita Prospectos en `Rechazado`/`Finalizado` (dato de
    US3/US4)
  - En términos de **código**, cada fase solo depende de los modelos/servicios de
    Foundational; el orden P1→P2→P3→P4→P5 refleja la dependencia de **datos** para probar
    manualmente el flujo completo, no una dependencia estructural de las clases.
- **Polish (Phase 8)**: depende de que todas las historias deseadas estén completas

### Within Each User Story

- Modelos antes que el `DbSet`/migración correspondiente
- Migración antes que los endpoints que usan esas tablas
- Endpoints backend antes que la pantalla Angular que los consume
- Historia completa y verificada antes de pasar a la siguiente prioridad

### Parallel Opportunities

- Todas las tareas [P] de Setup (T003-T008) en paralelo
- Todas las tareas [P] de Foundational (T012-T015, T018) en paralelo
- Dentro de cada historia, los modelos marcados [P] (por ejemplo T019/T020, T022-T024,
  T038-T040) en paralelo entre sí
- Las pantallas Angular de una misma historia marcadas [P] en paralelo entre sí
- Con más de una persona en el equipo, US1 a US5 pueden repartirse una vez cerrada
  Foundational, aunque probar el flujo end-to-end requiere seguir el orden de datos P1→P5

---

## Parallel Example: User Story 1

```bash
# Lanzar en paralelo los modelos de User Story 1:
Task: "Crear el modelo TipoDocumento en backend/GestorCredito.Api/Models/TipoDocumento.cs"
Task: "Crear el modelo TipoPersona en backend/GestorCredito.Api/Models/TipoPersona.cs"
Task: "Crear el modelo Producto en backend/GestorCredito.Api/Models/Producto.cs"
Task: "Crear el modelo Requisito en backend/GestorCredito.Api/Models/Requisito.cs"
Task: "Crear el modelo Usuario en backend/GestorCredito.Api/Models/Usuario.cs"

# Lanzar en paralelo las pantallas Angular de User Story 1:
Task: "Crear la pantalla de Tipos de Documento/Persona en frontend/src/app/features/configuracion/tipos-documento/"
Task: "Crear la pantalla de Productos en frontend/src/app/features/configuracion/productos/"
Task: "Crear la pantalla de Usuarios en frontend/src/app/features/configuracion/usuarios/"
```

---

## Implementation Strategy

### MVP First (User Story 1 solamente)

1. Completar Phase 1: Setup
2. Completar Phase 2: Foundational (crítico — bloquea todas las historias)
3. Completar Phase 3: User Story 1
4. **DETENER y VALIDAR**: probar User Story 1 de forma independiente (crear Tipos de
   Documento y Productos, verificar que quedan listados)
5. Continuar con User Story 2 en cuanto se decida avanzar

### Incremental Delivery

1. Setup + Foundational → Fundamentos listos
2. + User Story 1 → Probar → Administrador puede configurar el sistema (MVP)
3. + User Story 2 → Probar → Asesor puede validar y simular
4. + User Story 3 → Probar → Onboarding + Requisitos + Aprobación funcionan
5. + User Story 4 → Probar → Desembolso completo
6. + Historial (US5) → Probar → consulta de prospectos cerrados
7. Cada incremento agrega valor sin romper el anterior

### Parallel Team Strategy

Con varias personas en el equipo:

1. El equipo completa Setup + Foundational en conjunto
2. Una vez cerrado Foundational:
   - Persona A: User Story 1 (Configuración)
   - Persona B: User Story 2 (Validación y Simulación) — usando datos de prueba de Producto
     mientras US1 se termina
   - Persona C: User Story 3 y 4 (Onboarding/Aprobación/Desembolso) — puede avanzar los
     endpoints en paralelo, integrando al final con datos reales de US1/US2
3. Las historias se integran siguiendo el orden de datos P1→P4→P5 antes de la validación
   end-to-end de `quickstart.md`

---

## Notes

- [P] = archivos distintos, sin dependencias pendientes
- La etiqueta [Story] mapea cada tarea a su historia de usuario para trazabilidad
- Cada historia de usuario debe poder completarse y probarse de forma independiente
- Confirmar contra `quickstart.md` al cerrar cada historia
- Evitar: tareas vagas, conflictos de archivo entre tareas paralelas, dependencias cruzadas
  entre historias que rompan su independencia
