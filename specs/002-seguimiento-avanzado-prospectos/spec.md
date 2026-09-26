# Feature Specification: Seguimiento Avanzado de Prospectos (Bandeja, Onboarding Bancario y Comentarios de Aprobación)

**Feature Branch**: `002-seguimiento-avanzado-prospectos`

**Created**: 2026-09-25

**Status**: Draft

**Input**: User description: "- Vista de asesor: Debemos mostrar la lista de prospectos del asesor, cada prospecto debe indicar el nombre del cliente + tipo documento + numero documento y el estado del proceso (Bloqueado, en proceso, aprobacion, desembolsado, observado), para los procesos que estan en proceso se debe continuar desde el paso(pantalla que se ha quedado), en caso de observado se regresa a la pantalla(paso) de onboarding. Vamos a agregar una etapa adicional donde solo algunos productos pediran informacion, y esta etapa es respecto a declaracion de inversion. Onboarding de cliente: Vamos a pedir mas datos, primero vamos a preguntar si tiene cuenta en el mismo banco o cuenta externa, en caso de ser cuenta interna se le debe mostrar la lista de bancos registrados o puede agregar uno nuevo solo se debe tener en cuenta el numero de cuenta y moneda, en caso de ser externo se le debe pedir el nombre del banco y cci. Aprobador: Al observar o rechazar se debe insertar un comentario de maximo 1000 caracteres, este comentario se debe mostrar en la bandeja del asesor en un tooltip. Asesor: Debe tener una bandeja para ver los clientes y poder editar sus datos."

**Depends on**: `001-gestion-creditos` (extiende el flujo de Prospecto, Onboarding, Aprobación y Desembolso ya definido en esa especificación)

## Clarifications

### Session 2026-09-25

- Q: ¿Qué información concreta debe capturarse en la nueva etapa de "Declaración de Inversión"? → A: Monto a Invertir y Origen de los Fondos (lista cerrada: Ahorros, Herencia, Venta de activo, Actividad empresarial, Otro), más un campo de Detalle/Observación opcional.
- Q: La "lista de cuentas internas registradas" que se muestra para cuenta interna, ¿es específica del cliente (sus propias cuentas, reutilizables en futuros créditos) o un catálogo genérico sin relación al cliente? → A: Son las cuentas propias del cliente (mismo Tipo/Número de Documento), ya registradas en procesos anteriores; se conservan y pueden reutilizarse en futuros prospectos del mismo cliente.
- Q: La capacidad del Asesor de "editar datos" del cliente desde la bandeja, ¿aplica en cualquier estado del prospecto activo, o solo cuando está en "Observado" (comportamiento ya existente)? → A: Aplica en cualquier estado activo del prospecto (Simulación, Evaluación, Observado, Desembolso), excepto mientras está en "Aprobación" (en revisión del Aprobador) o en estados cerrados ("Bloqueado"/"Desembolsado").

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Bandeja de seguimiento del Asesor (Priority: P1)

El Asesor abre su bandeja principal y ve la lista de todos sus prospectos activos, cada uno
identificado por el nombre del cliente, el Tipo y Número de Documento, y un estado de proceso
resumido (Bloqueado, En proceso, Aprobación, Desembolsado, Observado). Al seleccionar un
prospecto "En proceso", el sistema lo lleva directamente a la pantalla/paso donde se quedó; al
seleccionar uno "Observado", lo lleva a la pantalla de Onboarding para corregir los datos.

**Why this priority**: Sin esta bandeja el Asesor no tiene forma de retomar prospectos ya
iniciados ni de distinguir cuáles requieren su atención inmediata (Observados) de los que
solo están avanzando con normalidad. Es la puerta de entrada a todo el resto de la gestión
diaria del Asesor.

**Independent Test**: Con varios prospectos en distintos estados (Simulación, Evaluación,
Aprobación, Observado, Desembolso, Rechazado), se puede verificar de forma independiente que
la bandeja lista a todos con su estado correcto y que al hacer clic en cada uno se navega a
la pantalla correspondiente.

**Acceptance Scenarios**:

1. **Given** el Asesor tiene prospectos en distintos estados, **When** abre su bandeja,
   **Then** ve una fila por cada prospecto activo con nombre del cliente, Tipo de Documento,
   Número de Documento y una etiqueta de estado (Bloqueado, En proceso, Aprobación,
   Desembolsado u Observado).
2. **Given** un prospecto en estado "En proceso" (Simulación, Evaluación o Desembolso
   pendiente de cierre), **When** el Asesor lo selecciona desde la bandeja, **Then** el
   sistema lo lleva exactamente a la pantalla/paso en la que quedó ese prospecto, sin
   reiniciar el flujo.
3. **Given** un prospecto en estado "Observado", **When** el Asesor lo selecciona desde la
   bandeja, **Then** el sistema lo lleva a la pantalla de Onboarding para corregir los datos
   del cliente.
4. **Given** un prospecto en estado "Aprobación", **When** el Asesor lo selecciona, **Then**
   el sistema muestra el detalle de solo lectura del prospecto en espera de la decisión del
   Aprobador (el Asesor no puede editar mientras está en revisión).
5. **Given** un prospecto rechazado (mostrado como "Bloqueado"), **When** el Asesor lo
   selecciona, **Then** el sistema muestra el detalle de cierre del prospecto sin permitir
   reabrir el proceso para ese registro.

---

### User Story 2 - Selección de cuenta bancaria en el Onboarding (Priority: P2)

Durante el Onboarding, el Asesor indica si el cliente recibirá el desembolso en una cuenta
del mismo banco (interna) o en una cuenta externa. Si es interna, el sistema muestra las
cuentas ya registradas para elegir una, o permite agregar una nueva indicando número de
cuenta y moneda. Si es externa, el sistema pide el nombre del banco y el Código de Cuenta
Interbancario (CCI).

**Why this priority**: Reemplaza y enriquece el dato único de "Cuenta Bancaria (CCI)" que
hoy exige el Onboarding (ver `001-gestion-creditos` FR-017/FR-018), habilitando el caso más
común en la práctica (cuenta propia del banco) sin perder la opción de cuenta externa.
Depende de que el prospecto ya haya llegado a la fase de Onboarding (Historia 3 de
`001-gestion-creditos`).

**Independent Test**: Con un prospecto en fase de Onboarding, se puede probar de forma
independiente completando el flujo eligiendo "cuenta interna" (seleccionando o creando una
cuenta) y, en otro caso, eligiendo "cuenta externa" (ingresando banco y CCI), verificando que
ambos casos guardan correctamente los datos bancarios del desembolso.

**Acceptance Scenarios**:

1. **Given** el Asesor está completando el Onboarding, **When** llega a la sección de datos
   bancarios, **Then** el sistema pregunta primero si la cuenta de desembolso es "del mismo
   banco" (interna) o "externa".
2. **Given** el Asesor elige "cuenta interna", **When** el sistema muestra la sección de
   cuenta, **Then** presenta la lista de cuentas internas ya registradas para elegir una, y
   ofrece la opción de agregar una nueva indicando únicamente Número de Cuenta y Moneda.
3. **Given** el Asesor elige "cuenta externa", **When** el sistema muestra la sección de
   cuenta, **Then** pide Nombre del Banco y Código de Cuenta Interbancario (CCI), validando
   el CCI como numérico de exactamente 20 dígitos.
4. **Given** el Asesor guarda el Onboarding con una cuenta interna o externa completa,
   **When** el prospecto avanza, **Then** los datos de la cuenta elegida quedan disponibles
   para el PDF de Aprobación Final (igual que el CCI en `001-gestion-creditos` FR-027).

---

### User Story 3 - Etapa de Declaración de Inversión para productos específicos (Priority: P3)

Para los Productos que el Administrador marque como que requieren Declaración de Inversión,
el flujo del prospecto incluye una etapa adicional (después del Onboarding) donde el Asesor
registra la información de la declaración antes de continuar a Requisitos o Aprobación.

**Why this priority**: Es una etapa condicional que solo afecta a un subconjunto de
Productos; no bloquea el flujo general para el resto de casos, por lo que puede
incorporarse después de que la bandeja y el Onboarding bancario ya funcionen. Depende de
que el prospecto haya completado el Onboarding (Historia 2 de esta especificación).

**Independent Test**: Con un Producto marcado como que requiere Declaración de Inversión, se
puede probar de forma independiente verificando que el flujo del prospecto muestra esta
pantalla adicional tras el Onboarding, y que para un Producto que no la requiere, el flujo la
omite por completo.

**Acceptance Scenarios**:

1. **Given** el Administrador marca un Producto como que requiere Declaración de Inversión,
   **When** un prospecto con ese Producto completa el Onboarding, **Then** el sistema
   presenta la pantalla de Declaración de Inversión antes de continuar con Requisitos o
   Aprobación.
2. **Given** un Producto que NO requiere Declaración de Inversión, **When** un prospecto con
   ese Producto completa el Onboarding, **Then** el sistema omite esta etapa por completo y
   continúa el flujo normal.
3. **Given** el Asesor completa y guarda la Declaración de Inversión, **When** el prospecto
   continúa el flujo, **Then** la información queda asociada al prospecto y disponible para
   su consulta posterior.

---

### User Story 4 - Comentario obligatorio del Aprobador visible para el Asesor (Priority: P2)

Cuando el Aprobador marca un prospecto como "Observado" o "Rechazado", el sistema exige
ingresar un comentario (máximo 1000 caracteres) explicando el motivo. Ese comentario se
muestra al Asesor en su bandeja mediante un tooltip sobre el prospecto correspondiente.

**Why this priority**: Cierra un vacío de comunicación del flujo actual: hoy el Aprobador
puede observar o rechazar sin dejar constancia del motivo (`001-gestion-creditos`
FR-024/FR-025/FR-026), obligando al Asesor a adivinar qué corregir. Depende de que exista la
bandeja del Asesor (Historia 1) donde mostrar el tooltip.

**Independent Test**: Con un prospecto en estado "Aprobación", se puede probar de forma
independiente que el Aprobador no puede guardar una decisión de "Observado" o "Rechazado"
sin ingresar un comentario, y que dicho comentario aparece luego en un tooltip sobre el
prospecto en la bandeja del Asesor.

**Acceptance Scenarios**:

1. **Given** el Aprobador decide marcar un prospecto como "Observado", **When** intenta
   guardar la decisión sin ingresar comentario, **Then** el sistema lo impide y solicita un
   comentario.
2. **Given** el Aprobador decide marcar un prospecto como "Rechazado", **When** intenta
   guardar la decisión sin ingresar comentario, **Then** el sistema lo impide y solicita un
   comentario.
3. **Given** el Aprobador ingresa un comentario de más de 1000 caracteres, **When** intenta
   guardarlo, **Then** el sistema no permite superar ese límite.
4. **Given** el Aprobador guardó una decisión de "Observado" o "Rechazado" con comentario,
   **When** el Asesor ve ese prospecto en su bandeja, **Then** puede pasar el cursor sobre el
   indicador de estado y ver el comentario completo en un tooltip.

---

### User Story 5 - Edición de datos del cliente desde la bandeja (Priority: P2)

El Asesor, desde su bandeja de clientes, puede acceder directamente a editar los datos de un
cliente (Nombres, Apellidos, Dirección y datos de la cuenta bancaria de desembolso) sin tener
que recorrer de nuevo cada paso previo del flujo.

**Why this priority**: Evita que el Asesor deba "adivinar" en qué pantalla se guardan los
datos del cliente o esperar a que un prospecto sea marcado como "Observado" para poder
corregir un dato simple (por ejemplo, una dirección mal escrita). Depende de que la bandeja
(Historia 1) y el Onboarding bancario (Historia 2) ya existan.

**Independent Test**: Con un prospecto activo en cualquier estado editable, se puede probar
de forma independiente que el Asesor accede a la edición de datos del cliente desde la
bandeja, modifica un campo y verifica que el cambio se refleja en el resto del flujo (por
ejemplo, en el PDF final).

**Acceptance Scenarios**:

1. **Given** el Asesor está en su bandeja, **When** selecciona la opción de editar un
   cliente, **Then** el sistema muestra un formulario con los datos actuales de Onboarding
   (Nombres, Apellidos, Dirección, Cuenta Bancaria) para modificar.
2. **Given** el Asesor modifica y guarda los datos del cliente, **When** el prospecto
   continúa su flujo, **Then** los documentos generados posteriormente (cronograma, PDF de
   Aprobación Final) reflejan los datos actualizados.
3. **Given** un prospecto está en estado "Aprobación" (en revisión del Aprobador), **When**
   el Asesor intenta editar sus datos desde la bandeja, **Then** el sistema lo impide hasta
   que el Aprobador emita una decisión, para evitar que los datos cambien mientras se evalúa.

---

### Edge Cases

- Si un Producto requiere Declaración de Inversión pero el prospecto ya había avanzado antes
  de que el Administrador activara ese requisito para el Producto, el sistema exige la
  Declaración de Inversión antes de permitir el siguiente paso, sin importar en qué punto del
  flujo posterior al Onboarding se encuentre.
- Si el Asesor intenta agregar una cuenta interna con un Número de Cuenta ya registrado para
  el mismo cliente, el sistema lo bloquea e indica que esa cuenta ya existe.
- Si el Asesor cambia de "cuenta interna" a "cuenta externa" (o viceversa) antes de guardar el
  Onboarding, el sistema descarta los datos de la opción no elegida y solo persiste los de la
  opción final seleccionada.
- Si el Aprobador intenta guardar una decisión de "Aprobado" (no "Observado" ni "Rechazado"),
  el comentario permanece opcional, ya que el requisito de comentario obligatorio aplica solo
  a "Observado" y "Rechazado".
- Un prospecto en estado "Bloqueado" (Rechazado) no aparece como editable ni permite retomar
  el flujo; solo puede consultarse su detalle de cierre.
- Si dos Asesores intentan editar el mismo prospecto desde la bandeja al mismo tiempo, el
  sistema conserva la última versión guardada (no se especifica bloqueo optimista en esta
  versión).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: El sistema DEBE mostrar al Asesor una bandeja con todos sus prospectos activos,
  indicando para cada uno el nombre del cliente, el Tipo de Documento, el Número de Documento
  y una etiqueta de estado de proceso resumida: "Bloqueado" (Rechazado), "En proceso"
  (Simulación, Evaluación o Desembolso pendiente de cierre), "Aprobación", "Desembolsado"
  (Finalizado) u "Observado".
- **FR-002**: Al seleccionar desde la bandeja un prospecto en estado "En proceso", el sistema
  DEBE navegar directamente a la pantalla/paso específico en que se encuentra ese prospecto
  (Simulación, Onboarding o Desembolso pendiente de cierre), sin reiniciar pasos anteriores ya
  completados.
- **FR-003**: Al seleccionar desde la bandeja un prospecto en estado "Observado", el sistema
  DEBE navegar a la pantalla de Onboarding para permitir la corrección de los datos del
  cliente.
- **FR-004**: El sistema DEBE permitir al Administrador marcar cada Producto con un indicador
  `requiere_declaracion_inversion`, de forma independiente al indicador `requiere_requisitos`
  ya existente.
- **FR-005**: Si el Producto del prospecto tiene `requiere_declaracion_inversion = true`, el
  sistema DEBE presentar, inmediatamente después del Onboarding y antes de Requisitos o
  Aprobación, una pantalla de Declaración de Inversión donde el Asesor registra: Monto a
  Invertir, Origen de los Fondos (lista cerrada: Ahorros, Herencia, Venta de activo,
  Actividad empresarial, Otro) y un campo de Detalle/Observación opcional.
- **FR-006**: Si el Producto del prospecto tiene `requiere_declaracion_inversion = false`, el
  sistema DEBE omitir por completo la pantalla de Declaración de Inversión.
- **FR-007**: Durante el Onboarding, el sistema DEBE preguntar primero si la cuenta de
  desembolso del cliente es "Interna" (mismo banco) o "Externa".
- **FR-008**: Si el Asesor indica cuenta "Interna", el sistema DEBE mostrar la lista de
  cuentas internas ya registradas para ese mismo cliente (mismo Tipo/Número de Documento) en
  procesos anteriores, reutilizables en futuros prospectos, para elegir una; y DEBE permitir
  agregar una cuenta interna nueva solicitando únicamente Número de Cuenta y Moneda.
- **FR-009**: Si el Asesor indica cuenta "Externa", el sistema DEBE solicitar Nombre del
  Banco y Código de Cuenta Interbancario (CCI), validando el CCI como numérico de exactamente
  20 dígitos (mismo formato que `001-gestion-creditos` FR-018).
- **FR-010**: El sistema DEBE persistir los datos de la cuenta de desembolso elegida (interna
  o externa) como parte del Onboarding del prospecto, reemplazando el campo único de Cuenta
  Bancaria (CCI) de `001-gestion-creditos` FR-017.
- **FR-011**: El sistema DEBE incluir los datos de la cuenta de desembolso elegida (interna o
  externa) en el PDF de Aprobación Final, en lugar del CCI único que exige hoy
  `001-gestion-creditos` FR-027.
- **FR-012**: Al marcar un prospecto como "Observado" o "Rechazado", el sistema DEBE exigir al
  Aprobador ingresar un comentario de hasta 1000 caracteres, sin permitir guardar la decisión
  sin dicho comentario.
- **FR-013**: El sistema NO DEBE exigir comentario al Aprobador cuando la decisión es
  "Aprobado".
- **FR-014**: El sistema DEBE mostrar el comentario del Aprobador al Asesor en un tooltip
  sobre el indicador de estado del prospecto correspondiente en la bandeja.
- **FR-015**: El sistema DEBE permitir al Asesor acceder, desde la bandeja, a un formulario de
  edición de los datos del cliente (Nombres, Apellidos, Dirección y datos de la cuenta
  bancaria de desembolso) para cualquier prospecto en estado activo distinto de "Aprobación"
  (Simulación, Evaluación, Observado o Desembolso).
- **FR-016**: El sistema DEBE impedir la edición de los datos del cliente mientras el
  prospecto está en estado "Aprobación", para evitar cambios durante la revisión del
  Aprobador.
- **FR-017**: El sistema DEBE impedir la edición de los datos del cliente para prospectos en
  estado "Bloqueado" (Rechazado) o "Desembolsado" (Finalizado).
- **FR-018**: Al guardar una edición de los datos del cliente desde la bandeja, el sistema
  DEBE reflejar los datos actualizados en cualquier documento generado posteriormente
  (cronograma pendiente, PDF de Aprobación Final).

### Key Entities *(include if feature involves data)*

- **Cuenta Bancaria de Desembolso**: Datos de la cuenta a la que se transferirá el
  desembolso; puede ser de tipo Interna (Número de Cuenta + Moneda, seleccionada de una lista
  ya registrada o creada en el momento) o Externa (Nombre del Banco + CCI de 20 dígitos).
  Reemplaza el campo único de Cuenta Bancaria (CCI) de `001-gestion-creditos`.
- **Declaración de Inversión**: Información adicional capturada para prospectos cuyo Producto
  la requiere (`requiere_declaracion_inversion = true`); contiene Monto a Invertir, Origen de
  los Fondos (lista cerrada) y un Detalle/Observación opcional, registrados por el Asesor
  tras el Onboarding (ver FR-005).
- **Comentario de Decisión**: Texto de hasta 1000 caracteres que el Aprobador registra al
  marcar un prospecto como "Observado" o "Rechazado"; queda asociado a esa decisión y visible
  para el Asesor en la bandeja.
- **Producto** *(extiende la entidad de `001-gestion-creditos`)*: Agrega el indicador
  `requiere_declaracion_inversion`, independiente de `requiere_requisitos`.
- **Prospecto** *(extiende la entidad de `001-gestion-creditos`)*: La bandeja del Asesor
  resume su estado interno en una etiqueta de proceso (Bloqueado, En proceso, Aprobación,
  Desembolsado, Observado) y ahora incluye la Cuenta Bancaria de Desembolso (Interna o
  Externa) y, cuando aplica, la Declaración de Inversión.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: El Asesor puede identificar, en menos de 10 segundos desde que abre su bandeja,
  cuáles de sus prospectos requieren atención inmediata (Observados) frente a los que solo
  están avanzando con normalidad (En proceso).
- **SC-002**: El 100% de los prospectos "En proceso" seleccionados desde la bandeja llevan al
  Asesor exactamente a la pantalla donde quedó el proceso, sin pasos repetidos.
- **SC-003**: El 100% de los prospectos con Producto que requiere Declaración de Inversión
  muestran esa pantalla antes de continuar a Requisitos o Aprobación; el 100% de los que no
  la requieren la omiten.
- **SC-004**: El 100% de los Onboarding completados registran una Cuenta Bancaria de
  Desembolso válida, ya sea Interna (Número de Cuenta + Moneda) o Externa (Banco + CCI de 20
  dígitos).
- **SC-005**: El 100% de las decisiones de "Observado" o "Rechazado" quedan acompañadas de un
  comentario visible para el Asesor en su bandeja.
- **SC-006**: El Asesor puede corregir un dato simple del cliente (ej. dirección) en menos de
  1 minuto desde la bandeja, sin tener que recorrer pantallas previas del flujo.

## Assumptions

- Las etiquetas de estado de la bandeja del Asesor se mapean a los estados internos del
  prospecto definidos en `001-gestion-creditos` así: "Bloqueado" = Rechazado; "En proceso" =
  Simulación, Evaluación o Desembolso (pendiente de generar el PDF final y cerrar); "Aprobación"
  = Aprobación; "Desembolsado" = Finalizado; "Observado" = Observado. El estado "Desembolso"
  (pendiente de generar el PDF final) se agrupa bajo "En proceso" porque todavía requiere una
  acción del Asesor antes de cerrarse.
- Las cuentas internas registradas se guardan asociadas al cliente (Tipo/Número de Documento)
  y se conservan entre prospectos, de modo que un cliente recurrente no necesita volver a
  registrar la misma cuenta en un crédito futuro.
- La Declaración de Inversión captura Monto a Invertir, Origen de los Fondos (lista cerrada:
  Ahorros, Herencia, Venta de activo, Actividad empresarial, Otro) y un Detalle/Observación
  opcional; no se contemplan validaciones adicionales tipo KYC/AML (ej. Persona Expuesta
  Políticamente) en esta versión.
- La Declaración de Inversión es una etapa condicional por Producto (similar en mecánica al
  indicador `requiere_requisitos` ya existente), configurable únicamente por el Administrador.
- El indicador `requiere_declaracion_inversion` es independiente de `requiere_requisitos`: un
  Producto puede requerir ambas etapas, solo una, o ninguna.
- La Cuenta Bancaria de Desembolso reemplaza el campo único de CCI de `001-gestion-creditos`
  a partir de esta versión; los prospectos nuevos capturan el dato en su forma Interna o
  Externa.
- El comentario del Aprobador es obligatorio únicamente para "Observado" y "Rechazado"; para
  "Aprobado" sigue sin ser necesario, conforme al flujo ya definido en
  `001-gestion-creditos`.
- La edición de datos del cliente desde la bandeja no está disponible mientras el prospecto
  está en revisión del Aprobador (estado "Aprobación"), ni para prospectos ya cerrados
  ("Bloqueado" o "Desembolsado"), para mantener la integridad de lo ya evaluado o finalizado.
- No se especifica en esta versión un mecanismo de bloqueo optimista para ediciones
  concurrentes sobre el mismo prospecto; se declara fuera de alcance.
