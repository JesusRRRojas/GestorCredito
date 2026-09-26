<!--
SYNC IMPACT REPORT
==================
Version change: (plantilla sin ratificar) → 1.0.0
Tipo de cambio: MAYOR (ratificación inicial de la constitución del proyecto)

Principios definidos (nuevos):
  - I. Simplicidad Ante Todo
  - II. Idioma y Mercado (Perú)
  - III. Cero Alcance Fantasma
  - IV. Propuesta Antes de Construir
  - V. Verificable por una Persona No Técnica
  - VI. Datos del Usuario y Seguridad de Credenciales

Secciones añadidas:
  - Estándares de Producto
  - Flujo de Trabajo y Control de Alcance
  - Governance (reglas de enmienda y versionado)

Secciones eliminadas: ninguna (plantilla base sin contenido previo)

Placeholders pendientes (TODO): ninguno

Plantillas dependientes revisadas: no se modifican en este comando (fuera de alcance,
según el scope guard de /speckit-constitution). Se recomienda validar en la próxima
ejecución de /speckit-plan, /speckit-tasks y /speckit-specify que sean coherentes
con estos principios.
-->

# Gestor de Crédito Constitution

## Core Principles

### I. Simplicidad Ante Todo
Ante dos soluciones posibles para resolver un mismo problema, se DEBE elegir siempre
la más simple. Al tratarse de una versión inicial (MVP) del producto, NO se debe
introducir complejidad anticipada: sin capas de abstracción, configuraciones,
generalizaciones ni funcionalidades pensadas para necesidades futuras no confirmadas
en el spec vigente.
**Razón**: reduce el tiempo de desarrollo, la superficie de errores y el costo de
mantenimiento en una etapa temprana donde los requisitos aún pueden cambiar.

### II. Idioma y Mercado (Perú)
Todo el producto — interfaz, textos, mensajes de error, ayudas, formatos de fecha
y moneda — DEBE desarrollarse en español, adaptado al mercado peruano (moneda en
Soles, S/; formato de fecha DD/MM/AAAA; terminología crediticia y comercial usada
en Perú).
**Razón**: el producto está dirigido a usuarios y al mercado financiero peruano;
la coherencia de idioma y formato evita fricción y errores de interpretación.

### III. Cero Alcance Fantasma
NO se implementará ninguna funcionalidad, pantalla, campo, validación o regla de
negocio que no esté expresamente escrita en la especificación (spec) vigente de la
funcionalidad correspondiente. Si el spec no lo describe, no se construye.
**Razón**: mantiene el producto alineado con lo acordado y evita trabajo no
solicitado ni verificable contra un documento de referencia.

### IV. Propuesta Antes de Construir
Toda idea nueva, mejora o cambio de alcance que surja durante el desarrollo DEBE
proponerse primero (actualizando o creando el spec correspondiente) y ser aceptada
antes de implementarse. Está prohibido construir directamente sobre una idea que
todavía no forma parte del spec.
**Razón**: complementa el Principio III (Cero Alcance Fantasma) asegurando que el
control de alcance ocurra antes de escribir código, no después.

### V. Verificable por una Persona No Técnica
Cada criterio de aceptación de cada funcionalidad DEBE poder comprobarse usando
directamente la aplicación (navegando, llenando formularios, leyendo resultados en
pantalla), sin necesidad de leer código fuente, logs técnicos ni consultar la base
de datos.
**Razón**: permite que el negocio valide las entregas de forma autónoma, sin
depender de conocimiento técnico.

### VI. Datos del Usuario y Seguridad de Credenciales
Se solicitará al usuario únicamente la información estrictamente necesaria para
generar cotizaciones y procesar créditos, conforme a lo definido en el spec de cada
funcionalidad. Queda PROHIBIDO incluir claves, contraseñas, tokens o cualquier
secreto directamente en el código fuente o en el repositorio; estos DEBEN
gestionarse mediante variables de entorno u otro mecanismo de configuración seguro
fuera del control de versiones.
**Razón**: minimiza la recolección innecesaria de datos y el riesgo de fuga de
credenciales o información sensible.

## Estándares de Producto

- Moneda: todos los montos se expresan en Soles (S/). Fechas en formato DD/MM/AAAA.
- Terminología: se usa vocabulario financiero y crediticio propio del mercado
  peruano (por ejemplo: TCEA, cuota, cronograma de pagos) según lo defina el spec.
- Formularios y captura de datos: los campos solicitados al usuario se limitan a
  los mínimos necesarios definidos explícitamente en el spec de cada funcionalidad.
- Seguridad: ninguna clave, API key, contraseña o secreto puede residir en el
  código o en archivos versionados; se usan variables de entorno o un gestor de
  secretos externo al repositorio.

## Flujo de Trabajo y Control de Alcance

- Toda funcionalidad nueva inicia con la creación o actualización de su spec
  (`/speckit-specify`) antes de escribir código.
- Cualquier idea nueva detectada durante el desarrollo se registra como propuesta
  en el spec (o como una nueva feature) antes de implementarse (ver Principio IV).
- Las revisiones de Pull Request DEBEN verificar como mínimo:
  (a) que cada criterio de aceptación es comprobable manualmente por una persona
  no técnica (Principio V);
  (b) que no se introdujo funcionalidad fuera de lo escrito en el spec
  (Principio III);
  (c) que no existen claves ni secretos en el código (Principio VI).
- Todo cambio que agregue complejidad respecto de una alternativa más simple debe
  justificarse explícitamente, indicando por qué la opción simple no es viable.

## Governance

Esta constitución prevalece sobre cualquier otra práctica, plantilla o preferencia
individual de desarrollo dentro del proyecto.

Toda enmienda a esta constitución requiere:
1. Una propuesta escrita del cambio y su motivo.
2. La actualización de este documento con el nuevo número de versión y fecha.
3. El registro del cambio en el Sync Impact Report al inicio del archivo.

Versionado semántico de la constitución:
- MAYOR: eliminación o redefinición incompatible de un principio existente.
- MENOR: incorporación de un nuevo principio o expansión material de una guía.
- PARCHE: aclaraciones de redacción o correcciones no semánticas.

Todo spec, plan, tarea o Pull Request debe verificar cumplimiento de los
principios anteriores antes de aprobarse. La complejidad debe justificarse frente
al Principio I (Simplicidad Ante Todo).

**Version**: 1.0.0 | **Ratified**: 2026-09-24 | **Last Amended**: 2026-09-24
