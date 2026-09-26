# Research: Gestión de Créditos (Prospecto a Desembolso)

**Feature**: `001-gestion-creditos` | **Fecha**: 2026-09-24

Este documento resuelve las decisiones técnicas necesarias para pasar de la especificación
al diseño (Fase 1), dado el stack indicado por el negocio: .NET 10, Angular + Bootstrap,
SQL Server local, EF Core Code-First.

## 1. Estilo de API: Minimal APIs vs. Controladores

- **Decision**: Minimal APIs de ASP.NET Core, agrupadas por área (`MapGroup`) para
  Productos, Tipos de Documento, Requisitos, Usuarios, Prospectos, Simulación, Onboarding,
  Requisitos-adjuntos, Aprobación, Desembolso e Historial.
- **Rationale**: Menos código repetitivo que los controladores MVC para una API interna sin
  necesidad de vistas ni convenciones REST complejas; encaja directamente con el Principio I
  (Simplicidad Ante Todo) de la constitución. El usuario autorizó ambas opciones; se elige la
  más simple.
- **Alternatives considered**: Controladores con atributos `[ApiController]` — descartados
  por añadir una capa de convención (routing por atributos, filtros) que no aporta valor
  adicional en este alcance.

## 2. Generación de PDF (cronograma y aprobación final)

- **Decision**: `PdfSharpCore` (licencia MIT) para generar los dos documentos PDF
  requeridos (RF06/FR-016 y RF11/FR-027).
- **Rationale**: Licencia MIT sin restricciones por tamaño de la empresa ni por ingresos,
  adecuada para una entidad financiera. Permite construir tablas y texto con código simple,
  sin depender de binarios nativos externos (a diferencia de wrappers de wkhtmltopdf).
- **Alternatives considered**:
  - `QuestPDF` — API muy cómoda, pero su "Community License" restringe el uso gratuito por
    ingresos anuales de la organización; riesgo legal innecesario para una entidad
    financiera. Descartado.
  - `iText7` — Licencia AGPL (obliga a liberar el código fuente) o licencia comercial de
    pago; descartado por complejidad legal/costo no solicitado.

## 3. Almacenamiento de archivos (documentos adjuntos y PDFs generados)

- **Decision**: Los archivos (Documento Adjunto y Documento Generado) se guardan como
  contenido binario (`varbinary(max)`) directamente en SQL Server, no en el sistema de
  archivos.
- **Rationale**: Con el volumen esperado en una v0 (uso interno, pocos usuarios), evita
  construir y mantener una capa adicional de gestión de rutas de archivos, permisos de
  carpeta y respaldo separado del de la base de datos. Es la opción más simple que cumple
  el requisito (Principio I).
- **Alternatives considered**: Sistema de archivos local con ruta referenciada en BD —
  descartado por añadir una dependencia operativa adicional (sincronizar respaldo de
  archivos y de BD) sin necesidad clara en esta escala.

## 4. Selector de rol y autorización (FR-005)

- **Decision**: El selector de rol (Administrador, Asesor, Aprobador) es una decisión que
  vive en el frontend (Angular): controla qué pantallas y menús se muestran. El backend
  expone sus endpoints sin verificar identidad ni rol contra un usuario autenticado.
- **Rationale**: El spec declara explícitamente fuera de alcance un "sistema de
  autenticación complejo", y la clarificación confirmó un selector de rol simple sin usuario
  ni contraseña. Construir una capa de autorización real en el backend exigiría un mecanismo
  de identidad que no fue solicitado.
- **Riesgo de negocio a comunicar**: en esta versión, cualquier persona con acceso a la URL
  de la API podría, en teoría, invocar directamente una acción reservada a otro rol sin
  pasar por la pantalla correspondiente (por ejemplo, aprobar una solicitud sin ser
  Aprobador). Es aceptable para un POC de uso interno, pero **no debe exponerse fuera de una
  red interna/confiable** ni usarse en producción sin agregar autenticación real.
- **Alternatives considered**: Autenticación basada en JWT con roles — descartada por ser la
  "complejidad anticipada" que el Principio I y el alcance del spec piden evitar en v0.

## 5. Registro de usuarios (FR-004) vs. selector de rol (FR-005)

- **Decision**: La gestión de Usuarios que hace el Administrador (FR-004) es un registro
  administrativo simple (nombre + rol asignado), independiente de la sesión activa. Ninguna
  acción del sistema (crear prospecto, simular, aprobar, desembolsar) queda vinculada a un
  Usuario específico; solo se valida contra el rol elegido en el selector de la sesión.
- **Rationale**: Vincular cada acción a un Usuario nombrado implicaría construir un mecanismo
  de sesión/identidad no solicitado (violaría el Principio III, Cero Alcance Fantasma). Se
  mantiene el CRUD de Usuarios tal como lo pide FR-004, sin extender su alcance.

## 6. Fórmula del cronograma (confirmando FR-013/FR-014 tras clarificación)

- **Decision**: Para cada cuota *i* (1..Plazo):
  - `Amortización(i) = redondear(Monto ÷ Plazo, 2)` para todas las cuotas salvo la última.
  - `Amortización(Plazo) = SaldoInicial(Plazo)` (ajuste para que el saldo final sea 0.00).
  - `Interés(i) = redondear(SaldoInicial(i) × TasaPeriódica, 2)`.
  - `Otros(i) = cargo fijo configurado en el Producto` (constante).
  - `PagoTotal(i) = Amortización(i) + Interés(i) + Otros(i)`.
  - `SaldoFinal(i) = SaldoInicial(i) − Amortización(i)`; `SaldoInicial(i+1) = SaldoFinal(i)`.
  - La Tasa que el Asesor elige (dentro del rango Tasa mínima/máxima del Producto) es ya una
    tasa periódica (mensual), sin conversión de Tasa Efectiva Anual.
- **Rationale**: Es la lectura literal de las clarificaciones (interés sobre saldo insoluto,
  redondeo con ajuste en la última cuota) resueltas en la sesión de `/speckit-clarify`.
- **Fecha de pago de cada cuota**: se calcula usando el Día de Pago (5 o 25) configurado en
  el Producto; la primera cuota cae en la próxima ocurrencia de ese día después de la fecha
  de aceptación de la simulación (mismo mes si ese día aún no pasó, o el mes siguiente si ya
  pasó), y las siguientes cuotas ocurren mensualmente en el mismo día.

## 7. Moneda del Producto

- **Decision**: El campo Moneda del Producto admite un único valor, Soles (PEN). Todos los
  montos se formatean según convenciones peruanas (S/, separador de miles/decimales).
- **Rationale**: El Principio II de la constitución establece como MUST que "todos los
  montos se expresan en Soles, S/". Una revisión de `/speckit-analyze` (hallazgo C1) detectó
  que una versión anterior de esta decisión permitía Dólares (USD) por Producto, lo cual
  violaba ese principio directamente. Se corrigió restringiendo la Moneda a Soles, en vez de
  diluir o reinterpretar el principio.
- **Alternatives considered**: Soportar PEN y USD — descartado por violar el Principio II;
  de necesitarse en el futuro, requeriría una enmienda explícita de la constitución vía
  `/speckit-constitution` antes de implementarse.

## 8. Cadena de conexión y credenciales (Principio VI de la constitución)

- **Decision**: La cadena de conexión proporcionada por el usuario (con estructura
  `Server=localhost;Database=dbGestorCredito;User Id=sa;Password=<valor local>;TrustServerCertificate=True;`)
  se usará **solo como valor de configuración local**, nunca escrita en un archivo versionado
  del repositorio. Durante la implementación se configurará mediante
  `dotnet user-secrets` (o una variable de entorno `ConnectionStrings__Default`) y el archivo
  `appsettings.json` versionado quedará sin la contraseña real.
  > Nota (`/speckit-analyze`, hallazgo C2): una versión anterior de este documento citaba la
  > contraseña real en texto plano, lo cual violaba el Principio VI al residir en un archivo
  > de los artefactos del feature. Se redactó aquí; la contraseña real solo debe vivir en la
  > configuración local de cada desarrollador (user-secrets o variable de entorno).
- **Rationale**: Cumple directamente el Principio VI ("prohibido introducir claves o
  secretos dentro del código"). Es el mecanismo estándar y más simple de ASP.NET Core para
  desarrollo local, sin herramientas adicionales.
- **A comunicar al usuario**: cada persona que clone el proyecto deberá configurar esta
  cadena de conexión en su propia máquina (con `dotnet user-secrets` o una variable de
  entorno) antes de ejecutar el backend; no vendrá lista "de fábrica" en el repositorio.

## 9. Versión de Angular y Bootstrap

- **Decision**: Se usará la versión estable más reciente de Angular (generada vía Angular
  CLI al iniciar el proyecto) junto con Bootstrap 5.x instalado como paquete npm estándar,
  sin tema personalizado (clases utilitarias de Bootstrap directamente en los componentes).
- **Rationale**: El usuario pidió explícitamente "diseño limpio y rápido sin CSS
  personalizado complejo"; usar Bootstrap tal cual, sin build de Sass propio, es la opción
  más simple.

## 10. Estrategia de pruebas

- **Decision**: Pruebas unitarias (xUnit) acotadas al cálculo del cronograma (la lógica más
  propensa a errores silenciosos de redondeo). No se construye una suite de pruebas de
  extremo a extremo ni de integración para v0.
- **Rationale**: El Principio V hace de la aplicación misma (verificación manual por criterio
  de aceptación) el mecanismo principal de validación; se reserva el esfuerzo de pruebas
  automatizadas para el único cálculo numérico crítico, evitando construir infraestructura
  de pruebas no solicitada.

## Resumen de "NEEDS CLARIFICATION" resueltos

Ninguno queda pendiente. Todos los puntos técnicos abiertos del Technical Context se
resolvieron arriba (framework de API, PDF, almacenamiento de archivos, autorización,
fórmula de cronograma, moneda, credenciales, frontend, pruebas).
