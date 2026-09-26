# Feature Specification: Precarga de Datos del Cliente en Onboarding

**Feature Branch**: `005-precarga-datos-onboarding`

**Created**: 2026-09-26

**Status**: Draft

**Input**: User description: "cuando se llega a la pantalla de onboarding se debe mostrar los datos del cliente en caso de ya existir para facilitar el proceso"

**Depends on**: `001-gestion-creditos` (pantalla de Onboarding, campos Nombres/Apellidos/Dirección) y
`002-seguimiento-avanzado-prospectos` (Cuenta Bancaria de Desembolso Interna/Externa, reutilización
de cuentas internas por cliente)

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Precarga de datos personales de un cliente recurrente (Priority: P1)

Cuando el Asesor llega por primera vez a la pantalla de Onboarding de un prospecto nuevo, y ese
mismo cliente (mismo Tipo y Número de Documento) ya completó el Onboarding en un crédito anterior
ya cerrado, el sistema precarga automáticamente sus Nombres, Apellidos y Dirección, dejando que el
Asesor solo los confirme o corrija en vez de volver a escribirlos desde cero.

**Why this priority**: Es el valor central pedido: evitar que el Asesor vuelva a pedir y digitar
datos que el cliente ya proporcionó antes. Sin esta historia no hay precarga de ningún dato.

**Independent Test**: Con un cliente que tiene un crédito anterior ya "Finalizado" o "Rechazado"
(con Onboarding completo) y ahora inicia un nuevo crédito, se puede probar de forma independiente
llegando a la pantalla de Onboarding del nuevo prospecto y verificando que Nombres, Apellidos y
Dirección aparecen ya completados con los datos del crédito anterior.

**Acceptance Scenarios**:

1. **Given** un cliente tiene un prospecto anterior ya cerrado (Rechazado o Finalizado) con su
   Onboarding completo, **When** ese mismo cliente inicia un nuevo prospecto y el Asesor llega a
   la pantalla de Onboarding por primera vez, **Then** los campos Nombres, Apellidos y Dirección
   aparecen precargados con los datos de su prospecto cerrado más reciente.
2. **Given** un cliente tiene más de un prospecto anterior cerrado con Onboarding completo,
   **When** el Asesor llega al Onboarding del nuevo prospecto, **Then** los datos precargados
   corresponden al prospecto cerrado más reciente de ese cliente.
3. **Given** un cliente no tiene ningún prospecto anterior con Onboarding completo (cliente nuevo,
   o sus prospectos previos fueron rechazados antes de llegar a Onboarding), **When** el Asesor
   llega a la pantalla de Onboarding, **Then** el formulario aparece vacío, igual que hoy.
4. **Given** los datos personales llegaron precargados, **When** el Asesor corrige cualquiera de
   ellos (por ejemplo, una dirección desactualizada) y guarda, **Then** el sistema guarda los
   valores corregidos, no los originales precargados.
5. **Given** el Asesor reingresa a la pantalla de Onboarding de un prospecto que ya tiene sus
   propios datos guardados (por ejemplo, tras ser marcado "Observado", o al editarlo desde la
   bandeja), **When** la pantalla se muestra, **Then** se ven los datos propios ya guardados de
   ese prospecto, sin reemplazarlos por los de otro prospecto cerrado.

---

### User Story 2 - Precarga de la preferencia de cuenta de desembolso (Priority: P2)

Además de los datos personales, cuando el prospecto cerrado más reciente del cliente usó una
Cuenta de Desembolso (Interna o Externa), el sistema también precarga esa misma preferencia:
si fue Interna, preselecciona esa cuenta en la lista ya registrada del cliente; si fue Externa,
precarga el Nombre del Banco y el CCI usados.

**Why this priority**: Complementa la Historia 1 completando la precarga de "los datos del
cliente" con su preferencia bancaria ya conocida; depende de que exista la precarga de datos
personales (Historia 1) y de la reutilización de cuentas internas ya definida en
`002-seguimiento-avanzado-prospectos`, por lo que se prioriza después.

**Independent Test**: Con un cliente cuyo crédito anterior cerrado usó una Cuenta Externa (Banco +
CCI), se puede probar de forma independiente llegando al Onboarding del nuevo prospecto y
verificando que el Tipo de Cuenta ya aparece en "Externa" con el Banco y CCI precargados; y, en
otro caso con Cuenta Interna, que esa cuenta específica aparece preseleccionada en la lista.

**Acceptance Scenarios**:

1. **Given** el prospecto cerrado más reciente del cliente usó una Cuenta Interna que sigue
   registrada, **When** el Asesor llega al Onboarding del nuevo prospecto, **Then** el Tipo de
   Cuenta aparece en "Interna" y esa cuenta específica ya está preseleccionada en la lista.
2. **Given** el prospecto cerrado más reciente del cliente usó una Cuenta Externa, **When** el
   Asesor llega al Onboarding del nuevo prospecto, **Then** el Tipo de Cuenta aparece en
   "Externa" con el Nombre del Banco y el CCI ya precargados.
3. **Given** la cuenta de desembolso llegó precargada, **When** el Asesor la cambia por otra antes
   de guardar, **Then** el sistema guarda la opción finalmente elegida por el Asesor.

---

### Edge Cases

- Un prospecto anterior "Rechazado" antes de llegar a la pantalla de Onboarding (rechazo por
  validación de riesgo) no tiene Nombres guardados; ese prospecto no cuenta como fuente de datos
  precargables.
- Si la Cuenta Interna usada en el prospecto anterior fue eliminada o ya no existe en la lista
  actual del cliente, el sistema no la preselecciona (se comporta como si no hubiera preferencia
  previa de cuenta, sin bloquear ni mostrar error).
- La precarga ocurre una sola vez, al cargar por primera vez la pantalla de Onboarding de un
  prospecto sin datos propios; no se reemplazan datos que el Asesor ya haya empezado a escribir
  en esa misma sesión de edición.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Al cargar la pantalla de Onboarding de un prospecto cuyos campos propios de
  Onboarding (Nombres) aún están vacíos, el sistema DEBE buscar entre los prospectos ya cerrados
  (Rechazado o Finalizado) del mismo Tipo y Número de Documento que sí tengan su Onboarding
  completo (Nombres no vacío), y precargar los campos Nombres, Apellidos y Dirección del más
  reciente de ellos.
- **FR-002**: Si no existe ningún prospecto cerrado previo con Onboarding completo para ese Tipo y
  Número de Documento, el sistema DEBE mostrar el formulario de Onboarding vacío, sin precargar
  ningún dato.
- **FR-003**: El sistema DEBE permitir al Asesor modificar libremente cualquier dato precargado
  antes de guardar, y DEBE guardar los valores finales que el Asesor deja en el formulario,
  precargados o corregidos.
- **FR-004**: El sistema NO DEBE precargar datos de otro prospecto cuando el prospecto actual ya
  tiene sus propios datos de Onboarding guardados (reingreso tras "Observado" o edición desde la
  bandeja del Asesor).
- **FR-005**: Cuando el prospecto cerrado más reciente usado como fuente (FR-001) tenga registrada
  una Cuenta de Desembolso Interna que siga existiendo en la lista de cuentas del cliente, el
  sistema DEBE precargar el Tipo de Cuenta como "Interna" y preseleccionar esa cuenta específica.
- **FR-006**: Cuando el prospecto cerrado más reciente usado como fuente (FR-001) tenga registrada
  una Cuenta de Desembolso Externa, el sistema DEBE precargar el Tipo de Cuenta como "Externa"
  junto con el Nombre del Banco y el CCI de ese prospecto.
- **FR-007**: Si la Cuenta Interna identificada en FR-005 ya no existe en la lista actual del
  cliente, el sistema NO DEBE preseleccionar ninguna cuenta ni mostrar error, dejando el
  comportamiento igual que si no hubiera preferencia previa.

### Key Entities *(include if feature involves data)*

- **Prospecto** *(concepto ya existente, sin cambios de esquema)*: Se usa un prospecto cerrado
  anterior del mismo cliente (mismo Tipo/Número de Documento) como fuente de datos para precargar
  el formulario de Onboarding de un prospecto nuevo; es una consulta en lectura, no una relación
  ni tabla nueva.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Un Asesor que atiende a un cliente recurrente completa el Onboarding sin tener que
  escribir de nuevo su Nombre, Apellidos o Dirección.
- **SC-002**: El 100% de los prospectos nuevos de un cliente con al menos un crédito cerrado
  anterior con Onboarding completo muestran sus datos personales precargados al llegar por
  primera vez a la pantalla de Onboarding.
- **SC-003**: El 100% de los prospectos de un cliente sin ningún crédito previo con Onboarding
  completo muestran el formulario vacío, sin errores ni datos incorrectos.
- **SC-004**: El Asesor puede corregir cualquier dato precargado en menos de 30 segundos, sin
  pasos adicionales respecto al formulario actual.

## Assumptions

- Cuando existen varios prospectos cerrados previos con Onboarding completo para el mismo
  cliente, se usa el más reciente de ellos como fuente de la precarga (por fecha de creación o
  cierre), sin combinar datos de varios.
- La precarga es solo una sugerencia inicial: todos los campos precargados siguen siendo
  editables por el Asesor antes de guardar, sin ninguna restricción adicional.
- Un prospecto rechazado antes de llegar a la pantalla de Onboarding (rechazo por validación de
  riesgo, sin Nombres guardados) no se considera una fuente válida de datos precargables.
- Esta funcionalidad no introduce una entidad "Cliente" separada ni cambia el esquema de datos
  existente; reutiliza los mismos campos de `Prospecto` ya definidos en `001-gestion-creditos` y
  `002-seguimiento-avanzado-prospectos`, consultados en modo lectura.
- La precarga de la preferencia de cuenta de desembolso (Historia 2) sigue el mismo criterio de
  "prospecto cerrado más reciente" usado para los datos personales (Historia 1), sin una fuente
  separada.
