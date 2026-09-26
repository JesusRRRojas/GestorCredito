# Feature Specification: Pagos de Cuota, Rol Cajero y Mejoras de Simulación/Administración

**Feature Branch**: `003-pagos-cajero-mejoras-ui`

**Created**: 2026-09-26

**Status**: Draft

**Input**: User description: "- en la pantalla 2 de simulacion de credito agregar la suma por columna de interes, otros y pago total. - cuando ya se tiene datos del cliente en la parte superior mostrar los datos de Nombre - Monto solicitado. - en la pantalla de declaracion de inversion agregar la validacion de que el monto a invertir debe ser menor o igual al solicitado. - cuando se ingresa por cada cliente listar los creditos asociado a su usuario, ademas debe mostrar el monto solicitado, cuota actual, estado si esta en proceso (en prospecto), pendiente (Desembolsado), cancelado (pago todas sus cuotas), los rechazados no se le muestra. - agregar un nuevo rol \"cajero\", el podra ingresar y ver una bandeja de todos los clientes registrados, no puede editar. Puede buscar por nombre del cliente o numero de documento, puede seleccionar al cliente y el credito vigente o con deuda, y registrar el pago de cuota. - mejorar la pantalla de administrador porque hay registros que no se ve, asi como la pantalla de productos darle un diseno mas profesional"

**Depends on**: `001-gestion-creditos` (cronograma de cuotas, Producto, Prospecto) y
`002-seguimiento-avanzado-prospectos` (bandeja del Asesor, patrón de "paso actual" derivado)

## Clarifications

### Session 2026-09-26

- Q: Los prospectos que ya están en estado "Finalizado" (cerrados por el Asesor antes de que
  existiera el registro de pago de cuotas) no tienen ninguna cuota marcada como pagada. ¿Cómo
  deben tratarse al activar esta funcionalidad? → A: Se tratan como "Cancelado": al activar la
  funcionalidad, todas las cuotas de los créditos ya Finalizados se marcan automáticamente como
  pagadas (se asume que el Asesor los cerró porque el proceso concluyó exitosamente).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Registro de pago de cuotas por el Cajero (Priority: P1)

Un usuario que elige el rol Cajero ingresa a una bandeja con todos los clientes registrados
(sin poder editar sus datos), busca a un cliente por nombre o por Número de Documento,
selecciona uno de sus créditos vigentes o con deuda pendiente, y registra el pago de la cuota
que corresponde a continuación.

**Why this priority**: Es la capacidad central y más grande de esta entrega: introduce el
concepto de pago de cuota, del cual dependen directamente la Historia 2 (estado
Pendiente/Cancelado de un crédito) y buena parte del valor de negocio de esta versión (permite
llevar el proceso más allá del desembolso). Sin esta historia no hay datos de pago que mostrar
en ningún otro lugar.

**Independent Test**: Con al menos un cliente que tiene un crédito en estado "Desembolsado"
(con cuotas aún no pagadas), se puede probar de forma independiente iniciando sesión como
Cajero, buscando a ese cliente, seleccionando su crédito y registrando el pago de la cuota
vigente, verificando que la cuota queda marcada como pagada y que la siguiente pasa a ser la
"cuota actual".

**Acceptance Scenarios**:

1. **Given** un usuario elige el rol "Cajero", **When** ingresa al sistema, **Then** ve una
   bandeja con todos los clientes registrados (con datos de Onboarding ya completados) y sus
   créditos, sin ninguna opción de edición de sus datos.
2. **Given** el Cajero está en su bandeja, **When** escribe parte de un nombre de cliente o un
   Número de Documento en el buscador, **Then** la bandeja se filtra mostrando solo las
   coincidencias.
3. **Given** el Cajero encuentra al cliente, **When** selecciona uno de sus créditos en estado
   "vigente" (Desembolsado, con cuotas aún por pagar), **Then** el sistema muestra la cuota
   actual pendiente (número, fecha de pago, monto) y la opción de registrar su pago.
4. **Given** el Cajero confirma el registro de pago de la cuota vigente, **When** el sistema
   guarda el pago, **Then** esa cuota queda marcada como pagada y, si quedan cuotas
   pendientes, la siguiente pasa a ser la nueva "cuota actual"; si esa era la última cuota,
   el crédito pasa a mostrarse como "Cancelado".
5. **Given** un crédito ya está "Cancelado" (todas sus cuotas pagadas) o aún "En proceso" (no
   desembolsado), **When** el Cajero lo selecciona, **Then** el sistema no ofrece la opción de
   registrar un pago (no hay cuota pendiente que cobrar).
6. **Given** el Cajero intenta acceder a una pantalla de edición de datos de cliente, Producto,
   Requisitos o decisión de Aprobación, **When** el sistema evalúa su rol, **Then** se lo
   impide, dado que el rol Cajero es de solo lectura salvo por el registro de pago de cuota.

---

### User Story 2 - Consulta de créditos por cliente con estado de pago (Priority: P2)

Al ingresar el Número de Documento de un cliente (Asesor o Administrador), el sistema lista
todos los créditos asociados a ese cliente —salvo los Rechazados— indicando el Monto
Solicitado, la Cuota Actual (si aplica) y un estado resumido: "En proceso" (el prospecto aún
no ha sido desembolsado), "Pendiente" (Desembolsado, con cuotas por pagar) o "Cancelado"
(todas las cuotas pagadas).

**Why this priority**: Da visibilidad de negocio sobre la cartera de un cliente y es la base
de datos que también usa la bandeja del Cajero (Historia 1) para decidir qué créditos son
seleccionables. Depende de que exista el registro de pago de cuotas (Historia 1) para poder
calcular "Cuota Actual" y "Cancelado" correctamente.

**Independent Test**: Con un cliente que tiene un crédito Rechazado, uno "En proceso", uno
"Pendiente" y uno "Cancelado", se puede probar de forma independiente ingresando su Número de
Documento y verificando que la lista muestra los tres créditos no rechazados con su Monto
Solicitado, Cuota Actual y estado correctos, y que el Rechazado no aparece.

**Acceptance Scenarios**:

1. **Given** un cliente tiene varios créditos (prospectos) en distintos estados, **When** el
   Asesor o Administrador consulta por su Número de Documento, **Then** ve listados todos
   excepto los Rechazados, cada uno con Monto Solicitado, Cuota Actual y estado ("En proceso",
   "Pendiente" o "Cancelado").
2. **Given** un crédito aún no ha sido desembolsado (Simulación, Evaluación, Aprobación u
   Observado), **When** aparece en la lista, **Then** se muestra como "En proceso" sin Cuota
   Actual (todavía no aplica, pues no se ha desembolsado dinero).
3. **Given** un crédito fue desembolsado y aún tiene cuotas sin pagar, **When** aparece en la
   lista, **Then** se muestra como "Pendiente" junto con el número de su Cuota Actual (la
   próxima cuota sin pagar).
4. **Given** un crédito fue desembolsado y ya se pagaron todas sus cuotas, **When** aparece en
   la lista, **Then** se muestra como "Cancelado", sin Cuota Actual.
5. **Given** un crédito está en estado Rechazado, **When** se genera la lista, **Then** ese
   crédito no aparece en ningún caso.

---

### User Story 3 - Suma de columnas en el cronograma de Simulación (Priority: P3)

En la Pantalla 2 (Simulación de Crédito), tras calcular el cronograma, el Asesor ve una fila
adicional con la suma total de las columnas Interés, Otros y Pago Total.

**Why this priority**: Mejora puntual de una pantalla ya existente; no depende de ninguna otra
historia y no bloquea el resto del alcance.

**Independent Test**: Generando cualquier simulación, se puede verificar de forma
independiente que la fila de totales suma correctamente los valores de Interés, Otros y Pago
Total de todas las cuotas mostradas.

**Acceptance Scenarios**:

1. **Given** el Asesor genera un cronograma de cuotas, **When** el sistema lo muestra en
   pantalla, **Then** al final de la tabla aparece una fila de totales con la suma de la
   columna Interés, la suma de la columna Otros y la suma de la columna Pago Total.
2. **Given** el cronograma cambia (se vuelve a simular con otro Monto, Tasa o Plazo), **When**
   se recalcula, **Then** la fila de totales se actualiza para reflejar los nuevos valores.

---

### User Story 4 - Encabezado con datos del cliente y Monto Solicitado (Priority: P3)

En toda pantalla del flujo de un prospecto donde ya se conocen sus datos de Onboarding (nombre
completo y monto aceptado), el sistema muestra en la parte superior un encabezado con
"Nombre — Monto Solicitado".

**Why this priority**: Mejora de usabilidad transversal a varias pantallas ya construidas;
depende de que el Monto de la simulación aceptada esté disponible para el frontend (ajuste de
datos expuestos), pero no bloquea ni es bloqueada por las demás historias.

**Independent Test**: Con un prospecto que ya completó el Onboarding, se puede probar de forma
independiente abriendo la pantalla de Declaración de Inversión, Requisitos, Aprobación o
Desembolso y verificando que el encabezado "Nombre — Monto Solicitado" aparece correctamente.

**Acceptance Scenarios**:

1. **Given** un prospecto ya completó el Onboarding y tiene una simulación aceptada, **When**
   el Asesor, Aprobador o Cajero abre cualquier pantalla posterior (Declaración de Inversión,
   Requisitos, Aprobación, Desembolso, o el propio Onboarding en modo edición), **Then** ve un
   encabezado con el Nombre completo del cliente y el Monto Solicitado (S/).
2. **Given** un prospecto todavía no completó el Onboarding (aún no tiene Nombre), **When** se
   muestra cualquier pantalla de ese prospecto, **Then** el encabezado no se muestra (no hay
   datos de cliente que mostrar todavía).

---

### User Story 5 - Validación del Monto a Invertir contra el Monto Solicitado (Priority: P3)

En la pantalla de Declaración de Inversión, el sistema impide guardar un Monto a Invertir
mayor al Monto Solicitado (el monto de la simulación aceptada) del mismo prospecto.

**Why this priority**: Es una regla de validación puntual sobre una pantalla ya construida en
`002-seguimiento-avanzado-prospectos`; depende de que el Monto Solicitado esté disponible para
el frontend (mismo ajuste de datos que la Historia 4), pero es independiente de las demás
historias de esta entrega.

**Independent Test**: Con un prospecto que tiene un Monto Solicitado de S/5,000, se puede
probar de forma independiente intentando guardar una Declaración de Inversión con Monto a
Invertir de S/6,000 (debe rechazarse) y luego con S/5,000 o menos (debe aceptarse).

**Acceptance Scenarios**:

1. **Given** el Asesor completa la Declaración de Inversión, **When** ingresa un Monto a
   Invertir mayor al Monto Solicitado del prospecto, **Then** el sistema rechaza el guardado e
   indica que no puede superar el Monto Solicitado.
2. **Given** el Asesor ingresa un Monto a Invertir igual o menor al Monto Solicitado, **When**
   guarda, **Then** el sistema lo acepta con normalidad.

---

### User Story 6 - Rediseño profesional de las pantallas de Administrador (Priority: P4)

El Administrador ve todas sus pantallas de configuración (Productos, Tipos de Documento y
Persona, Usuarios) con un diseño visual más cuidado y con la garantía de que ningún registro
queda oculto o recortado, sin importar cuántas columnas o filas existan.

**Why this priority**: Es una mejora visual/de usabilidad que no cambia ningún dato ni regla
de negocio; se prioriza al final porque no bloquea ni es bloqueada por ninguna otra historia.

**Independent Test**: Con varios Productos configurados (suficientes para exceder el ancho o
alto visible de una pantalla estándar), se puede verificar de forma independiente que todos
los registros y todas sus columnas permanecen accesibles (mediante scroll u otro mecanismo
visible), y que el diseño de la pantalla de Productos luce ordenado y profesional
(agrupación clara, espaciado consistente, indicadores visuales de estado).

**Acceptance Scenarios**:

1. **Given** la pantalla de Productos tiene varios registros con muchas columnas, **When** el
   Administrador la abre en un ancho de pantalla estándar, **Then** puede ver o desplazarse
   para ver todos los registros y todas las columnas sin que ninguno quede inaccesible.
2. **Given** el Administrador navega a Productos, Tipos de Documento/Persona o Usuarios,
   **When** revisa la pantalla, **Then** el diseño usa un estilo visual consistente y cuidado
   (tarjetas/tablas bien delimitadas, indicadores de estado con color, espaciado uniforme) en
   vez de una tabla básica sin estilo.
3. **Given** el Asesor ya tiene la bandeja de seguimiento (`002-seguimiento-avanzado-prospectos`),
   **When** navega por el menú principal, **Then** encuentra un enlace directo a su bandeja
   (hoy ausente en el menú, ver Edge Cases).

---

### Edge Cases

- Un crédito "Pendiente" cuya última cuota se paga en el mismo momento en que el Cajero
  también intenta registrar otro pago duplicado sobre la misma cuota: el sistema no debe
  permitir marcar como pagada una cuota que ya estaba pagada.
- Los créditos ya "Finalizado" antes de esta versión (sin ninguna cuota marcada como pagada)
  se migran automáticamente a "Cancelado" (todas sus cuotas se marcan como pagadas) al activar
  esta funcionalidad, conforme a la clarificación de esta sesión.
- Si el Cajero busca un texto que no coincide con ningún cliente, la bandeja muestra un
  mensaje de "sin resultados" en vez de una lista vacía sin explicación.
- Si un prospecto no tiene ninguna simulación aceptada todavía (por ejemplo, fue rechazado por
  el Mock antes de llegar a esa pantalla), no se muestra el encabezado "Nombre — Monto
  Solicitado" ni se calcula Cuota Actual, ya que no hay Monto Solicitado que mostrar.
- Si el Monto a Invertir declarado es igual al Monto Solicitado (límite exacto), la validación
  de la Historia 5 lo permite (límite inclusive).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: El sistema DEBE permitir marcar una Cuota del cronograma de un crédito
  desembolsado como pagada, junto con la fecha en que se registró el pago.
- **FR-002**: El sistema DEBE calcular la "Cuota Actual" de un crédito como la cuota de menor
  número que aún no está marcada como pagada; si todas las cuotas están pagadas, el crédito no
  tiene Cuota Actual.
- **FR-003**: El sistema DEBE permitir a un usuario con el nuevo rol "Cajero" ingresar mediante
  el mismo selector de rol ya existente (sin usuario ni contraseña, igual que los demás
  roles).
- **FR-004**: El sistema DEBE mostrar al Cajero una bandeja con todos los clientes que ya
  completaron su Onboarding, cada uno con sus créditos no Rechazados y el estado de cada uno
  ("En proceso", "Pendiente" o "Cancelado").
- **FR-005**: El sistema DEBE permitir al Cajero filtrar esa bandeja escribiendo parte del
  nombre del cliente o su Número de Documento.
- **FR-006**: El sistema DEBE permitir al Cajero seleccionar únicamente créditos en estado
  "Pendiente" (vigente o con deuda) para ver su Cuota Actual y registrar su pago; los créditos
  "En proceso" o "Cancelado" no ofrecen esta acción.
- **FR-007**: Al confirmar el Cajero el registro de pago de la Cuota Actual de un crédito, el
  sistema DEBE marcarla como pagada y recalcular cuál es la nueva Cuota Actual (o marcar el
  crédito como "Cancelado" si esa era la última cuota pendiente).
- **FR-008**: El sistema DEBE impedir marcar como pagada una Cuota que ya estaba pagada.
- **FR-009**: El rol Cajero NO DEBE tener acceso a ninguna pantalla de edición de datos de
  cliente, Producto, Requisitos, Declaración de Inversión o decisión de Aprobación; su única
  acción de escritura permitida es el registro de pago de cuota.
- **FR-010**: El sistema DEBE permitir a un usuario con rol Asesor o Administrador consultar,
  ingresando el Número de Documento de un cliente, la lista de todos sus créditos no
  Rechazados, mostrando para cada uno el Monto Solicitado, la Cuota Actual (cuando aplica) y
  su estado ("En proceso", "Pendiente" o "Cancelado").
- **FR-011**: El sistema DEBE clasificar como "En proceso" todo crédito cuyo prospecto aún no
  ha llegado al estado Desembolso ni Finalizado.
- **FR-012**: El sistema DEBE clasificar como "Pendiente" todo crédito desembolsado
  (Desembolso o Finalizado) que todavía tenga al menos una cuota sin pagar.
- **FR-013**: El sistema DEBE clasificar como "Cancelado" todo crédito desembolsado cuyas
  cuotas estén todas pagadas.
- **FR-014**: Al activar esta funcionalidad, el sistema DEBE marcar automáticamente como
  pagadas todas las cuotas de los créditos que ya estaban en estado "Finalizado" previamente
  (para que se clasifiquen como "Cancelado", conforme a la clarificación de esta sesión).
- **FR-015**: El sistema DEBE mostrar, en la Pantalla 2 (Simulación), una fila de totales con
  la suma de las columnas Interés, Otros y Pago Total del cronograma calculado.
- **FR-016**: El sistema DEBE exponer, para cada prospecto con una simulación aceptada, el
  Monto Solicitado (el Monto de esa simulación) junto con el resto de sus datos.
- **FR-017**: El sistema DEBE mostrar un encabezado con el Nombre completo del cliente y su
  Monto Solicitado en toda pantalla del flujo posterior al Onboarding (Onboarding en modo
  edición, Declaración de Inversión, Requisitos, Aprobación, Desembolso) cuando ambos datos ya
  existen para ese prospecto.
- **FR-018**: El sistema NO DEBE permitir guardar una Declaración de Inversión cuyo Monto a
  Invertir sea mayor al Monto Solicitado del mismo prospecto (límite inclusive).
- **FR-019**: Las pantallas de Productos, Tipos de Documento/Persona y Usuarios DEBEN mostrar
  todos sus registros y columnas de forma completamente accesible (visibles o alcanzables con
  scroll), sin recortes ni elementos ocultos, sin importar la cantidad de registros o el ancho
  de pantalla.
- **FR-020**: La pantalla de Productos DEBE presentar un diseño visual más cuidado que el
  actual (agrupación clara de la información, indicadores de estado con color, espaciado
  consistente), sin cambiar los datos ni el comportamiento funcional ya definidos en
  `001-gestion-creditos` y `002-seguimiento-avanzado-prospectos`.
- **FR-021**: El menú principal del Asesor DEBE incluir un enlace directo a su bandeja de
  seguimiento (`002-seguimiento-avanzado-prospectos`).

### Key Entities *(include if feature involves data)*

- **Cuota** *(extiende la entidad de `001-gestion-creditos`)*: Agrega si está pagada y, de
  estarlo, la fecha en que se registró ese pago.
- **Rol Cajero** *(extiende el catálogo de roles ya existente: Administrador, Asesor,
  Aprobador)*: Rol de solo lectura salvo por el registro de pago de cuota; se gestiona con el
  mismo mecanismo de selector de rol y de registro administrativo de Usuarios ya existente.
- **Crédito** *(concepto derivado, no una tabla nueva)*: Es el Prospecto junto con su
  simulación aceptada; su estado de cara al cliente ("En proceso", "Pendiente", "Cancelado")
  y su "Cuota Actual" se calculan a partir del `Estado` del Prospecto y de qué Cuotas de su
  cronograma están pagadas, sin introducir una entidad nueva de "Crédito".

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: El Cajero puede encontrar a un cliente por nombre o documento y registrar el
  pago de su cuota vigente en menos de 1 minuto.
- **SC-002**: El 100% de los créditos Rechazados quedan excluidos de la bandeja del Cajero y
  de la consulta de créditos por cliente.
- **SC-003**: El 100% de los créditos desembolsados con todas sus cuotas pagadas se muestran
  como "Cancelado" tanto en la bandeja del Cajero como en la consulta por cliente.
- **SC-004**: El Asesor puede leer el total de Interés, Otros y Pago Total de un cronograma
  sin tener que sumarlo manualmente cuota por cuota.
- **SC-005**: El 100% de las Declaraciones de Inversión guardadas cumplen que su Monto a
  Invertir no supera el Monto Solicitado del prospecto correspondiente.
- **SC-006**: El Administrador puede ver o alcanzar cualquier registro de las pantallas de
  configuración (Productos, Tipos de Documento/Persona, Usuarios) sin que ninguno quede
  oculto, independientemente de cuántos existan.

## Assumptions

- El registro de pago de una cuota es de "todo o nada" (se marca la cuota completa como
  pagada por su monto de Pago Total ya calculado); esta versión no contempla pagos parciales
  de una misma cuota ni pagos fuera de orden (siempre se paga primero la Cuota Actual).
- Esta versión no contempla reversar o anular un pago de cuota ya registrado por error; se
  declara fuera de alcance.
- La búsqueda del Cajero por nombre es una coincidencia parcial (contiene el texto buscado),
  sin distinguir mayúsculas/minúsculas, sobre el Nombre completo (Nombres + Apellidos) del
  Onboarding.
- La bandeja del Cajero solo incluye prospectos que ya completaron su Onboarding (tienen
  Nombre); un prospecto todavía en Simulación no tiene un "cliente" identificable por nombre.
- La consulta de créditos por cliente (Historia 2) extiende la pantalla de Historial ya
  existente de `001-gestion-creditos` (antes limitada a créditos cerrados) en vez de crear una
  pantalla nueva y paralela.
- El encabezado "Nombre — Monto Solicitado" de la Historia 4 usa el Monto de la simulación
  aceptada del prospecto (el mismo que ya se usa en el cronograma y en los PDF generados).
- El rediseño de las pantallas de Administrador (Historia 6) es únicamente visual/de
  legibilidad; no agrega, quita ni cambia ningún campo o regla de negocio ya definida.
