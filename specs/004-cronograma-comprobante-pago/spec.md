# Feature Specification: Cronograma Visual y Comprobante de Pago en Bandeja del Cajero

**Feature Branch**: `004-cronograma-comprobante-pago`

**Created**: 2026-09-26

**Status**: Draft

**Input**: User description: "(1) La vista del cajero cuadno selecciona un credito se debe mostrar el cronograma del credito con el estado por cuota al costado para que visualmente se entendible en que cuota esta y cuanto le falta. (2) cuando decide realizar el pago debe ingresar el numero de oepracion como evidencia del pago y opcionalmente para poder subir una evidencia"

**Depends on**: `003-pagos-cajero-mejoras-ui` (bandeja del Cajero, selección de crédito y registro de pago de la Cuota Actual)

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Cronograma visual con estado por cuota (Priority: P1)

Cuando el Cajero selecciona uno de los créditos de un cliente en su bandeja, el sistema
muestra el cronograma completo de cuotas de ese crédito, y junto a cada cuota un indicador
visual de su estado (pagada, la cuota actual pendiente de cobro, o una cuota futura), de modo
que el Cajero entiende de un vistazo en qué cuota está el cliente y cuántas le faltan por
pagar.

**Why this priority**: Es la base visual sobre la que el Cajero decide si corresponde
registrar un pago (Historia 2); sin ver el cronograma y su progreso, el Cajero no tiene forma
de confirmar en qué cuota está el crédito antes de cobrar.

**Independent Test**: Con un cliente que tiene un crédito "Pendiente" con varias cuotas ya
pagadas, una cuota actual y varias cuotas futuras, se puede probar de forma independiente
ingresando como Cajero, seleccionando ese crédito y verificando que el cronograma se muestra
completo con el estado correcto junto a cada cuota y un resumen de cuántas faltan.

**Acceptance Scenarios**:

1. **Given** el Cajero selecciona un crédito "Pendiente" de un cliente, **When** el sistema
   muestra el detalle del crédito, **Then** se presenta el cronograma completo de cuotas
   (número, fecha de pago, monto) con un indicador visual junto a cada cuota que distingue
   claramente si está pagada, si es la cuota actual pendiente de cobro, o si es una cuota
   futura.
2. **Given** el cronograma se muestra, **When** el Cajero lo revisa, **Then** puede identificar
   sin ambigüedad cuál es la cuota actual (la próxima a pagar) y ve un resumen de cuántas
   cuotas ya están pagadas y cuántas faltan por pagar del total del crédito.
3. **Given** el Cajero selecciona un crédito "Cancelado" (todas las cuotas pagadas), **When**
   se muestra el cronograma, **Then** todas las cuotas aparecen marcadas como pagadas y no se
   señala ninguna cuota actual.
4. **Given** el Cajero selecciona un crédito "En proceso" (aún no desembolsado, sin
   cronograma), **When** intenta ver su detalle, **Then** el sistema indica que ese crédito
   todavía no tiene cronograma en vez de mostrar una tabla vacía sin explicación.

---

### User Story 2 - Número de operación y evidencia opcional al registrar el pago (Priority: P2)

Cuando el Cajero confirma el registro de pago de la Cuota Actual, el sistema le exige ingresar
el Número de Operación como evidencia de que el pago se realizó, y le permite, de forma
opcional, adjuntar un archivo con la evidencia del pago (por ejemplo, una foto o captura del
comprobante).

**Why this priority**: Refuerza la trazabilidad del pago ya registrado por la Historia 1 de
`003-pagos-cajero-mejoras-ui`; depende de que exista la acción de registrar pago, por lo que se
prioriza después de tener visibilidad del cronograma.

**Independent Test**: Con un crédito "Pendiente" con una Cuota Actual disponible, se puede
probar de forma independiente intentando confirmar el pago sin ingresar Número de Operación
(debe bloquearse), luego ingresando un Número de Operación sin adjuntar evidencia (debe
aceptarse) y finalmente ingresando un Número de Operación con un archivo de evidencia adjunto
(debe aceptarse y guardar ambos datos).

**Acceptance Scenarios**:

1. **Given** el Cajero abre el formulario de registro de pago de la Cuota Actual, **When** el
   sistema lo muestra, **Then** incluye un campo obligatorio de Número de Operación y un campo
   opcional para adjuntar un archivo de evidencia del pago.
2. **Given** el Cajero intenta confirmar el pago sin haber ingresado un Número de Operación,
   **When** el sistema valida el formulario, **Then** rechaza el registro e indica que el
   Número de Operación es obligatorio.
3. **Given** el Cajero ingresa un Número de Operación y no adjunta ningún archivo, **When**
   confirma el pago, **Then** el sistema registra el pago con normalidad, guardando el Número
   de Operación como evidencia.
4. **Given** el Cajero ingresa un Número de Operación y adjunta un archivo de evidencia válido,
   **When** confirma el pago, **Then** el sistema registra el pago guardando tanto el Número de
   Operación como el archivo adjunto asociados a esa cuota.
5. **Given** una cuota ya fue pagada y tiene Número de Operación (y evidencia, si se adjuntó),
   **When** se consulta esa cuota posteriormente (por ejemplo, en el cronograma o el
   historial), **Then** el Número de Operación queda visible, junto con acceso a la evidencia
   adjunta si existe.

---

### Edge Cases

- Si el Cajero intenta adjuntar un archivo de evidencia con un formato no soportado o que
  excede el tamaño máximo permitido, el sistema rechaza el archivo e indica el motivo, sin
  bloquear el registro del pago si el Cajero decide continuar sin adjuntar evidencia.
- Si el Cajero ingresa un Número de Operación compuesto solo de espacios en blanco, el sistema
  lo trata como si estuviera vacío y bloquea el registro.
- Si un crédito no tiene ninguna cuota pendiente de cobro (ya "Cancelado") o aún no fue
  desembolsado ("En proceso"), el sistema no ofrece el formulario de registro de pago (regla ya
  definida en `003-pagos-cajero-mejoras-ui`), por lo que tampoco pide Número de Operación.
- Si dos cuotas pagadas consecutivas comparten accidentalmente el mismo Número de Operación
  ingresado por el Cajero, el sistema lo permite (el Número de Operación es solo un dato de
  evidencia declarado por el Cajero, no se valida su unicidad contra un sistema bancario real).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: El sistema DEBE mostrar, al seleccionar el Cajero un crédito "Pendiente" o
  "Cancelado", el cronograma completo de cuotas de ese crédito (número, fecha de pago y monto
  de cada cuota).
- **FR-002**: El sistema DEBE mostrar junto a cada cuota del cronograma un indicador visual de
  su estado: pagada, cuota actual (pendiente de cobro) o futura (pendiente, posterior a la
  actual).
- **FR-003**: El sistema DEBE mostrar un resumen visible de cuántas cuotas están pagadas y
  cuántas faltan por pagar respecto del total de cuotas del crédito.
- **FR-004**: El sistema DEBE indicar, cuando el Cajero selecciona un crédito "En proceso" (aún
  no desembolsado), que ese crédito todavía no tiene un cronograma disponible.
- **FR-005**: El sistema DEBE requerir un Número de Operación (texto no vacío) al confirmar el
  registro de pago de la Cuota Actual, y DEBE impedir el registro si el campo está vacío o
  contiene solo espacios en blanco.
- **FR-006**: El sistema DEBE permitir, de forma opcional, adjuntar un archivo de evidencia del
  pago (imagen o documento) al registrar el pago de la Cuota Actual, sin impedir el registro si
  el Cajero no adjunta ningún archivo.
- **FR-007**: El sistema DEBE validar que el archivo de evidencia adjunto, cuando se proporcione,
  no exceda el tamaño máximo permitido ni use un formato no soportado, mostrando un mensaje claro
  del motivo cuando se rechace.
- **FR-008**: El sistema DEBE almacenar el Número de Operación (y el archivo de evidencia, si
  fue adjuntado) asociados a la cuota que se marca como pagada.
- **FR-009**: El sistema DEBE mostrar el Número de Operación de una cuota ya pagada al
  consultarla posteriormente (en el cronograma del crédito o en el historial), junto con acceso
  a la evidencia adjunta cuando exista.

### Key Entities *(include if feature involves data)*

- **Cuota** *(extiende la entidad ya definida en `001-gestion-creditos` y `003-pagos-cajero-mejoras-ui`)*:
  Agrega el Número de Operación ingresado como evidencia del pago y, opcionalmente, una
  referencia al archivo de evidencia adjunto (nombre e identificador del archivo almacenado).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: El Cajero identifica en menos de 5 segundos, con solo mirar el cronograma, en qué
  cuota está el crédito y cuántas cuotas faltan por pagar, sin necesidad de contarlas
  manualmente.
- **SC-002**: El 100% de los pagos registrados por el Cajero quedan con un Número de Operación
  no vacío asociado.
- **SC-003**: El Cajero puede completar el registro de un pago sin adjuntar evidencia en menos
  de 30 segundos, confirmando que el campo de evidencia es opcional y no bloquea el flujo.
- **SC-004**: El 100% de las cuotas pagadas muestran su Número de Operación al ser consultadas
  posteriormente.

## Assumptions

- El archivo de evidencia adjunto acepta formatos comunes de imagen (JPG, PNG) y PDF, con un
  tamaño máximo de 5 MB; estos límites son configurables pero no se solicitó un valor distinto.
- El Número de Operación es un campo de texto libre declarado por el Cajero (sin conexión a un
  sistema bancario real, dado que este proyecto es un Mock), por lo que no se valida su formato
  ni su unicidad, solo que no esté vacío.
- El archivo de evidencia, una vez adjuntado, no se puede reemplazar ni eliminar después de
  confirmado el pago en esta versión; corregirlo se considera fuera de alcance, igual que
  reversar un pago (ya definido como fuera de alcance en `003-pagos-cajero-mejoras-ui`).
- El indicador visual de estado de cuota (pagada / actual / futura) se implementa como parte de
  la misma pantalla de detalle de crédito del Cajero introducida en
  `003-pagos-cajero-mejoras-ui`, no como una pantalla nueva y separada.
