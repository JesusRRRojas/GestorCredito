# Feature Specification: Gestión de Créditos (Prospecto a Desembolso)

**Feature Branch**: `001-gestion-creditos`

**Created**: 2026-09-24

**Status**: Draft

**Input**: User description: "Especificación: Gestor de credito v1 — Sistema que ayudará a una entidad financiera desde la captación del cliente hasta el desembolso de dinero, gestionando el proceso de evaluación crediticia, onboarding, simulación de crédito, aprobaciones y desembolso."

## Clarifications

### Session 2026-09-24

- Q: ¿Sobre qué base se calcula el interés de cada cuota del cronograma? → A: Interés sobre saldo insoluto (Saldo inicial de la cuota × Tasa periódica), decreciente cada cuota conforme el saldo baja. El ejemplo numérico originalmente aportado (interés constante de S/20.00) era ilustrativo y no reflejaba este método; el cálculo real produce un interés decreciente (ver Regla de Negocio actualizada).
- Q: ¿Qué representa el monto "Otros" de cada cuota y de dónde sale ese valor? → A: Es un cargo fijo por cuota, configurable por Producto (el Administrador lo define al crear/editar el Producto).
- Q: Cuando el Monto no se divide de forma exacta entre el Plazo, ¿cómo se ajusta la amortización para que el saldo final sea exactamente S/0.00? → A: Se redondea a 2 decimales cada cuota, y la última cuota ajusta su amortización (y pago total) para cuadrar el saldo final en cero.
- Q: Si un prospecto es rechazado por el Mock (4 o 5), ¿puede el Asesor reintentar de inmediato con el mismo Número de Documento? → A: Sí, reintento inmediato permitido sin límite ni tiempo de espera; un rechazo por Mock no bloquea el documento.
- Q: ¿El sistema debe conservar un historial consultable de prospectos ya cerrados (Rechazados o Desembolsados) por cliente? → A: Sí; se necesita una pantalla/listado donde el Asesor o el Administrador puedan consultar el historial de prospectos cerrados de un cliente.

### Remediación de `/speckit-analyze` (2026-09-24)

- Se restringió la Moneda del Producto a solo Soles (S/), eliminando la opción de Dólares
  que contradecía el Principio II de la constitución (hallazgo C1).
- Se nombró explícitamente el estado terminal "Finalizado" (tras cerrar el proceso en
  Desembolso) en FR-010, FR-028, FR-029, Key Entities y Assumptions, para no confundirlo con
  el estado "Desembolso" (hallazgo I1).
- Se agregó la regla explícita de bloquear la carga de Requisitos si el Producto no tiene
  ninguno configurado (hallazgo U1).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Configuración de parámetros core (Priority: P1)

El Administrador configura los Tipos de Documento y Tipos de Persona, crea los Productos
de crédito (con sus rangos de monto/tasa/plazo, moneda, frecuencia y día de pago, y si
requieren requisitos), y define los Requisitos documentales asociados a cada Producto.

**Why this priority**: Sin productos ni tipos de documento configurados no existe ningún
dato con el cual el Asesor pueda validar un prospecto o generar una simulación. Es la base
sobre la que se apoyan todas las demás historias.

**Independent Test**: Se puede probar de forma independiente ingresando como Administrador,
creando un Tipo de Documento, un Producto con sus rangos y marcándolo con o sin requisitos, y
verificando que ambos aparecen correctamente listados y editables.

**Acceptance Scenarios**:

1. **Given** el Administrador está en la pantalla de configuración, **When** crea un Tipo de
   Documento y lo asocia a un Tipo de Persona, **Then** ambos quedan disponibles para
   seleccionarse en la Pantalla 1 de validación.
2. **Given** el Administrador está creando un Producto, **When** define monto, tasa y plazo
   mínimo/máximo, moneda, frecuencia de pago, día de pago (5 o 25) y marca
   `requiere_requisitos`, **Then** el Producto queda guardado y disponible en el selector de
   Productos de la Pantalla 2.
3. **Given** un Producto con `requiere_requisitos = true`, **When** el Administrador agrega
   uno o más Requisitos documentales a ese Producto, **Then** esos Requisitos aparecerán
   listados en la Pantalla 4 para cualquier prospecto que elija ese Producto.

---

### User Story 2 - Validación de riesgo y simulación de crédito (Priority: P2)

El Asesor ingresa el Tipo y Número de Documento de un prospecto, el sistema evalúa el riesgo
(Mock), y si el resultado es favorable, el Asesor elige un Producto y genera una simulación
(cronograma de pagos) que puede aceptar para continuar el proceso.

**Why this priority**: Es la funcionalidad que genera la cotización, el valor central del
producto ("agilizar el proceso de créditos" y "generar cotizaciones"). Depende de que existan
Productos configurados (User Story 1).

**Independent Test**: Con al menos un Producto ya configurado, se puede probar de forma
independiente ingresando un documento, validando el riesgo, seleccionando un Producto,
generando el cronograma y aceptándolo.

**Acceptance Scenarios**:

1. **Given** el Asesor ingresa un Tipo y Número de Documento válidos, **When** presiona
   "Buscar/Validar" y el resultado Mock es 1, 2 o 3, **Then** se habilita el botón
   "Siguiente" para continuar a la simulación.
2. **Given** el resultado Mock es 4 o 5, **When** el sistema termina la validación, **Then**
   se muestra un mensaje de rechazo y se bloquea el avance del prospecto.
3. **Given** el Asesor ya tiene un prospecto activo para un Número de Documento, **When**
   intenta iniciar una nueva validación para el mismo documento, **Then** el sistema lo
   impide e indica que ya existe un prospecto en curso.
4. **Given** el Asesor selecciona un Producto en la Pantalla 2, **When** intenta ingresar un
   Monto, Tasa o Plazo fuera de los rangos configurados para ese Producto, **Then** el
   sistema no permite el ingreso ni el procesamiento de ese valor.
5. **Given** el Asesor presiona "Simular", **When** el sistema calcula el cronograma,
   **Then** este se muestra en pantalla sin guardarse todavía en base de datos.
6. **Given** el cronograma se muestra en pantalla, **When** el Asesor presiona "Aceptar",
   **Then** la simulación se guarda y el prospecto avanza a la fase de Onboarding.
7. **Given** una simulación aceptada, **When** el Asesor solicita el PDF del cronograma,
   **Then** el sistema genera y permite descargar dicho documento.

---

### User Story 3 - Onboarding, requisitos y aprobación condicional (Priority: P3)

El Asesor registra los datos básicos del prospecto (incluyendo la cuenta bancaria para el
desembolso). Según si el Producto elegido requiere requisitos, el prospecto pasa a que el
Asesor adjunte documentos y un Aprobador decida (Caso A), o directamente a Desembolso sin
intervención de un Aprobador (Caso B).

**Why this priority**: Formaliza la evaluación crediticia y aplica la regla de negocio más
importante del flujo (el salto condicional de Aprobación). Depende de que exista una
simulación aceptada (User Story 2).

**Independent Test**: Con un prospecto que ya tiene una simulación aceptada, se puede probar
de forma independiente completando el Onboarding y verificando que el sistema deriva
correctamente al Caso A (Requisitos → Aprobación) o al Caso B (Desembolso automático) según
la configuración del Producto.

**Acceptance Scenarios**:

1. **Given** un prospecto con simulación aceptada, **When** el Asesor completa Nombres,
   Apellidos, Dirección y Cuenta Bancaria y guarda, **Then** el prospecto entra formalmente
   en fase de Evaluación.
2. **Given** el Producto del prospecto tiene `requiere_requisitos = true`, **When** el
   Asesor llega a la Pantalla 4, **Then** ve la lista de Requisitos configurados para ese
   Producto y puede adjuntar un archivo PDF por cada uno.
3. **Given** el Asesor adjuntó todos los Requisitos solicitados, **When** finaliza la carga,
   **Then** el estado del prospecto cambia a "Aprobación".
4. **Given** el Producto del prospecto tiene `requiere_requisitos = false`, **When** el
   Asesor completa el Onboarding, **Then** el sistema omite la Pantalla 4 y la fase de
   Aprobación, cambia el estado del prospecto directamente a "Desembolso" y autogenera el
   documento de aprobación.
5. **Given** un prospecto en estado "Aprobación", **When** el Aprobador revisa los requisitos
   adjuntos y decide, **Then** puede cambiar el estado a "Aprobado", "Observado" o
   "Rechazado".
6. **Given** un prospecto marcado como "Observado", **When** el Aprobador guarda la
   decisión, **Then** el prospecto retorna a la bandeja del Asesor, quien puede editar los
   datos de Onboarding y reemplazar los archivos de Requisitos ya adjuntados.
7. **Given** un prospecto marcado como "Rechazado", **When** el Aprobador guarda la
   decisión, **Then** el proceso de ese prospecto se cierra sin avanzar a Desembolso.

---

### User Story 4 - Desembolso y cierre del proceso (Priority: P4)

El Asesor visualiza los prospectos listos para desembolso (aprobados o derivados por
Fast-Track), genera el PDF final de Desembolso y cierra el proceso.

**Why this priority**: Es el paso final que materializa el valor del proceso completo
(entrega del documento al cliente por canales externos). Depende de que el prospecto haya
llegado al estado "Desembolso" (User Story 3).

**Independent Test**: Con un prospecto en estado "Desembolso", se puede probar de forma
independiente generando el PDF final y verificando que refleja los datos correctos, luego
cerrando el proceso.

**Acceptance Scenarios**:

1. **Given** un prospecto en estado "Desembolso", **When** el Asesor genera el PDF final,
   **Then** el documento incluye la Cuenta Bancaria registrada en el Onboarding y los datos
   del cronograma aceptado.
2. **Given** el PDF final fue generado y descargado, **When** el Asesor cierra el proceso,
   **Then** el prospecto queda marcado como finalizado y deja de considerarse un prospecto
   activo para ese Número de Documento.

---

### Edge Cases

- Si el Asesor intenta iniciar una nueva validación (Pantalla 1) para un Número de Documento
  que ya tiene un prospecto activo, el sistema lo bloquea e informa del prospecto en curso.
- Si el Asesor intenta adjuntar un archivo que no sea PDF en la Pantalla 4, el sistema lo
  rechaza y solicita un archivo válido.
- Si un Producto tiene `requiere_requisitos = true` pero el Administrador no configuró
  ningún Requisito para él, la Pantalla 4 no tiene documentos que listar; el sistema DEBE
  impedir marcar la carga como completada (bloqueando el botón o rechazando la acción) hasta
  que el Administrador configure al menos un Requisito para ese Producto.
- Los valores de Monto, Tasa y Plazo iguales exactamente al mínimo o al máximo configurado
  para el Producto se consideran válidos (límites inclusive).
- Dado que las fechas de pago del cronograma usan únicamente los días 5 o 25 de cada mes
  (ver regla de negocio de Cálculo de Cronograma), no se generan fechas inválidas por meses
  cortos ni años bisiestos.
- Un prospecto "Observado" que vuelve a la bandeja del Asesor y es corregido, reingresa al
  circuito de Aprobación (no se autoaprueba ni se autorrechaza).
- Si el Monto no se divide de forma exacta entre el Plazo (ej. S/1,000 entre 3 cuotas), el
  sistema ajusta la amortización de la última cuota para que el saldo final cierre
  exactamente en S/0.00, en vez de dejar un residuo.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: El sistema DEBE permitir a un usuario con rol Administrador crear, editar,
  listar y deshabilitar Tipos de Documento y Tipos de Persona, y asociar cada Tipo de
  Documento a uno o más Tipos de Persona.
- **FR-002**: El sistema DEBE permitir al Administrador crear, editar, listar y deshabilitar
  Productos de crédito, definiendo: monto mínimo y máximo, moneda (Soles, S/ — única moneda
  soportada en esta versión, conforme al Principio II de la constitución), tasa mínima y
  máxima, plazo mínimo y máximo, frecuencia de pago, día de pago (5 o 25), el cargo fijo
  "Otros" por cuota y el indicador `requiere_requisitos`.
- **FR-003**: El sistema DEBE permitir al Administrador definir la lista de Requisitos
  documentales asociados a cada Producto que tenga `requiere_requisitos = true`.
- **FR-004**: El sistema DEBE permitir al Administrador crear y gestionar usuarios, cada uno
  asociado a un rol (Administrador, Asesor o Aprobador).
- **FR-005**: Al ingresar al sistema, este DEBE presentar un selector de rol (Administrador,
  Asesor, Aprobador) que determina las funciones disponibles durante esa sesión, sin requerir
  usuario ni contraseña.
- **FR-006**: El sistema DEBE permitir al Asesor iniciar una validación de un prospecto
  ingresando Tipo de Documento y Número de Documento.
- **FR-007**: Al ejecutar la validación, el sistema DEBE generar de forma aleatoria un valor
  Mock entre 1 y 5 que representa el resultado de riesgo del prospecto.
- **FR-008**: Si el valor Mock es 4 o 5, el sistema DEBE mostrar un mensaje de rechazo y
  bloquear el avance de ese prospecto, sin dejar un registro que impida al Asesor reintentar
  de inmediato una nueva validación para el mismo Número de Documento.
- **FR-009**: Si el valor Mock es 1, 2 o 3, el sistema DEBE habilitar el avance hacia la
  simulación de crédito.
- **FR-010**: El sistema DEBE impedir iniciar una nueva validación para un Número de
  Documento que ya tenga un prospecto activo, es decir, uno cuyo estado no sea todavía
  "Finalizado" (cierre del proceso tras Desembolso, ver FR-028) ni "Rechazado".
- **FR-011**: El sistema DEBE mostrar al Asesor un selector de Productos disponibles en la
  pantalla de Simulación.
- **FR-012**: Al seleccionar un Producto, el sistema DEBE limitar los campos Monto, Tasa y
  Plazo a los valores mínimo y máximo configurados para ese Producto, sin permitir el
  ingreso ni el procesamiento de valores fuera de rango.
- **FR-013**: El sistema DEBE calcular y mostrar en pantalla el cronograma de pagos al
  presionar "Simular", sin persistir los datos hasta la confirmación. La amortización de
  capital es constante (Monto ÷ Plazo) en cada cuota, y el interés de cada cuota se calcula
  sobre el saldo insoluto (Saldo inicial de la cuota × Tasa periódica configurada en el
  Producto), por lo que decrece cuota a cuota conforme baja el saldo. No se aplican fórmulas
  de Tasa Efectiva Anual (TEA). Todos los montos se redondean a 2 decimales; si el Monto no
  se divide de forma exacta entre el Plazo, la última cuota ajusta su amortización (y pago
  total) para que el saldo final quede exactamente en S/0.00.
- **FR-014**: El cronograma DEBE incluir, para cada cuota: número, fecha de pago (según el
  día de pago configurado en el Producto, 5 o 25), saldo inicial, amortización, interés,
  otros (cargo fijo por cuota configurado en el Producto), pago total y saldo final.
- **FR-015**: El sistema DEBE guardar la simulación únicamente cuando el Asesor presiona
  "Aceptar", y en ese momento avanzar el prospecto a la fase de Onboarding.
- **FR-016**: El sistema DEBE permitir generar y descargar un PDF del cronograma de la
  simulación aceptada.
- **FR-017**: El sistema DEBE permitir al Asesor registrar los datos de Onboarding del
  prospecto: Nombres, Apellidos, Dirección y Cuenta Bancaria.
- **FR-018**: La Cuenta Bancaria ingresada en el Onboarding DEBE validarse como un Código de
  Cuenta Interbancario (CCI) numérico de exactamente 20 dígitos.
- **FR-019**: Al guardar el Onboarding, el sistema DEBE marcar formalmente al prospecto en
  la fase de Evaluación.
- **FR-020**: Si el Producto del prospecto tiene `requiere_requisitos = true`, el sistema
  DEBE listar los Requisitos configurados para ese Producto y permitir al Asesor adjuntar un
  archivo PDF por cada uno.
- **FR-021**: Al completar la carga de todos los Requisitos solicitados, el sistema DEBE
  cambiar el estado del prospecto a "Aprobación".
- **FR-022**: Si el Producto del prospecto tiene `requiere_requisitos = false`, el sistema
  DEBE omitir la pantalla de Requisitos y la fase de Aprobación, cambiar el estado del
  prospecto directamente a "Desembolso" y generar automáticamente el documento de
  aprobación.
- **FR-023**: El sistema DEBE permitir a un usuario con rol Aprobador visualizar los
  prospectos en estado "Aprobación" junto con sus Requisitos adjuntos.
- **FR-024**: El sistema DEBE permitir al Aprobador cambiar el estado del prospecto a
  "Aprobado", "Observado" o "Rechazado".
- **FR-025**: Si el prospecto es marcado como "Observado", el sistema DEBE devolverlo a la
  bandeja del Asesor, permitiendo editar los datos de Onboarding y reemplazar los archivos
  de Requisitos ya adjuntados.
- **FR-026**: Si el prospecto es marcado como "Rechazado", el sistema DEBE cerrar el proceso
  de ese prospecto sin avanzar a Desembolso.
- **FR-027**: Cuando el prospecto llega al estado "Desembolso" (por aprobación manual o por
  Fast-Track), el sistema DEBE permitir al Asesor generar y descargar un PDF de Aprobación
  Final que incluya los datos del cronograma aceptado y la Cuenta Bancaria del Onboarding.
- **FR-028**: El sistema DEBE permitir al Asesor cerrar el proceso del prospecto luego de
  generado el PDF final de Desembolso, cambiando su estado a "Finalizado" y liberando el
  Número de Documento para un futuro prospecto.
- **FR-029**: El sistema DEBE permitir a los usuarios con rol Asesor o Administrador
  consultar un historial de los prospectos cerrados (en estado "Rechazado" o "Finalizado")
  de un cliente, mostrando al menos el Número de Documento, el Producto, el estado final y
  la fecha de cierre.

### Key Entities *(include if feature involves data)*

- **Tipo de Documento**: Documento de identidad aceptado (ej. DNI, Carné de Extranjería);
  se asocia a uno o más Tipos de Persona.
- **Tipo de Persona**: Clasificación del prospecto (ej. Natural, Jurídica).
- **Producto**: Producto de crédito ofrecido; define la moneda (Soles, S/ — única moneda
  soportada), rangos de monto/tasa/plazo, frecuencia y día de pago (5 o 25), cargo fijo
  "Otros" por cuota, e indicador `requiere_requisitos`.
- **Requisito**: Documento exigido por un Producto específico cuando este requiere
  requisitos (ej. copia de DNI).
- **Usuario**: Persona que opera el sistema, con un rol asignado (Administrador, Asesor o
  Aprobador).
- **Prospecto**: Cliente potencial en proceso; contiene Tipo/Número de Documento, resultado
  Mock, Producto elegido, datos de Onboarding, estado del macro-flujo (Evaluación,
  Aprobación, Observado, Rechazado, Desembolso, Finalizado) y las simulaciones/documentos
  asociados. "Desembolso" es el estado en que el Asesor puede generar el PDF final;
  "Finalizado" es el estado terminal tras cerrar el proceso (FR-028). Los registros de
  prospectos cerrados (Rechazado o Finalizado) se conservan para su consulta en el
  historial (FR-029), aunque ya no cuenten como prospecto activo.
- **Simulación (Cronograma)**: Resultado del cálculo de amortización para un Producto,
  Monto, Tasa y Plazo elegidos; contiene la lista de cuotas (fecha, saldo inicial,
  amortización, interés, otros, pago total, saldo final).
- **Documento Adjunto**: Archivo PDF cargado por el Asesor en respuesta a un Requisito de un
  Prospecto.
- **Documento Generado (PDF)**: Archivo PDF de salida del sistema, ya sea de tipo
  Cronograma o de tipo Aprobación Final, asociado a un Prospecto.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Un Asesor puede completar la validación de riesgo y obtener una simulación de
  crédito aceptada en menos de 3 minutos.
- **SC-002**: El 100% de los prospectos cuyo Producto no requiere requisitos llegan al
  estado "Desembolso" automáticamente al guardar el Onboarding, sin pasos manuales
  adicionales.
- **SC-003**: El 100% de los cronogramas generados respetan los rangos de monto, tasa y
  plazo configurados para el Producto seleccionado.
- **SC-004**: El 100% de los PDF de Desembolso generados muestran la misma Cuenta Bancaria
  registrada en el Onboarding del prospecto correspondiente.
- **SC-005**: Un prospecto "Observado" puede ser corregido por el Asesor y reenviado a
  Aprobación sin reiniciar el proceso desde la Pantalla 1 de validación.
- **SC-006**: El sistema impide en el 100% de los casos iniciar un segundo prospecto activo
  para el mismo Número de Documento.
- **SC-007**: El Asesor o Administrador puede encontrar el historial de prospectos cerrados
  de un cliente (en estado "Rechazado" o "Finalizado") consultando por su Número de
  Documento.

## Assumptions

- El cálculo de interés del cronograma se hace sobre el saldo insoluto de cada cuota
  (Saldo inicial × Tasa periódica), no sobre fórmulas de Tasa Efectiva Anual (TEA)
  compuesta; la amortización de capital es constante (Monto ÷ Plazo) en todas las cuotas.
- Todos los Productos están denominados en Soles (S/), conforme al Principio II de la
  constitución; esta versión no soporta productos en otra moneda (ej. Dólares).
- La Cuenta Bancaria se valida como un Código de Cuenta Interbancario (CCI) numérico de
  exactamente 20 dígitos.
- El acceso al sistema en esta versión usa un selector de rol simple (Administrador, Asesor,
  Aprobador) sin usuario ni contraseña, conforme a lo declarado fuera de alcance para
  autenticación compleja.
- El valor Mock de riesgo (1 a 5) se genera de forma aleatoria en cada validación,
  independientemente del Número de Documento ingresado.
- Un prospecto se considera "activo" mientras no haya llegado al estado terminal
  "Finalizado" (cierre del proceso tras Desembolso) ni a "Rechazado"; en esos casos el mismo
  Número de Documento puede iniciar un nuevo prospecto.
- Cada Requisito documental admite un único archivo PDF adjunto, reemplazable mientras el
  prospecto no haya sido aprobado definitivamente.
- La frecuencia de pago en esta versión es mensual, con el día de pago fijo (5 o 25)
  definido por Producto; no se soportan otras frecuencias en esta versión.
- No existe asignación de un Aprobador específico por Producto; cualquier usuario con rol
  Aprobador puede revisar y decidir sobre cualquier prospecto en estado "Aprobación".
- Los PDF de cronograma y de Aprobación Final se generan y descargan desde la aplicación;
  no se contempla envío automático por correo (declarado fuera de alcance).
