# Research: Cronograma Visual y Comprobante de Pago en Bandeja del Cajero

**Feature**: `004-cronograma-comprobante-pago` | **Fecha**: 2026-09-26

## §1. Cómo calcular el estado visual de cada cuota (pagada / actual / futura)

**Decision**: Extender `CreditoCalculator` (ya existente, `backend/GestorCredito.Api/Services/CreditoCalculator.cs`)
con una función que, dada la lista de `Cuota` de la simulación aceptada, devuelva cada cuota
junto con su estado: `"Pagada"` si `Cuota.Pagada = true`; `"Actual"` para la primera cuota no
pagada (menor `Numero`); `"Futura"` para el resto de cuotas no pagadas. Se calcula en lectura,
sin persistir ningún campo nuevo de estado (coherente con que "Crédito" ya es un concepto
derivado, spec `003` Key Entities).

**Rationale**: Reutiliza exactamente la misma regla que ya determina la "Cuota Actual" en
`003` (primera no pagada por número), evitando una segunda fuente de verdad. Es la opción más
simple: una función pura sobre una lista ya cargada, sin nuevas consultas a la base de datos.

**Alternatives considered**:
- Persistir un campo `Estado` en `Cuota`: rechazado, redundante con `Pagada` + el orden de
  `Numero`, y requeriría mantenerlo sincronizado en cada pago (más superficie de error).
- Calcular el estado en el frontend a partir de `Pagada` y el índice: rechazado porque duplica
  la regla de negocio (qué es la "cuota actual") en dos capas; el backend ya la tiene en
  `CreditoCalculator` y debe seguir siendo la única fuente.

## §2. Cómo almacenar el Número de Operación y la evidencia opcional del pago

**Decision**: Agregar tres columnas nuevas y simples a `Cuota`: `NumeroOperacion` (`string?`),
`EvidenciaNombreArchivo` (`string?`) y `EvidenciaContenido` (`byte[]?`). El archivo, cuando se
adjunta, se guarda igual que ya se hace para los documentos de Requisitos
(`DocumentoAdjunto.Contenido`, `ProspectosEndpoints.cs` líneas 358-389): recibido como
`IFormFile` en un endpoint Minimal API, copiado a un `MemoryStream` y guardado como `byte[]` en
la misma base de datos ya usada por el proyecto (sin almacenamiento externo).

**Rationale**: Mismo patrón ya validado en el proyecto (`DocumentoAdjunto`), por lo que no se
introduce ninguna dependencia ni mecanismo de almacenamiento nuevo (Principio I). Al ser
información que pertenece 1:1 a una Cuota específica (no a un catálogo de Requisitos como
`DocumentoAdjunto`), columnas directas en `Cuota` son más simples que crear una tabla nueva con
relación uno-a-uno.

**Alternatives considered**:
- Crear una entidad `EvidenciaPagoCuota` separada (`CuotaId`, `NumeroOperacion`,
  `NombreArchivo`, `Contenido`): rechazada por el Principio I (complejidad anticipada); no hay
  ningún requisito que necesite esa evidencia como entidad independiente (no se lista, no se
  filtra, no se relaciona con nada más que su propia Cuota).
- Guardar el archivo en el sistema de archivos del servidor en vez de la base de datos:
  rechazada por inconsistencia con el patrón ya establecido (`DocumentoAdjunto` usa la base de
  datos) y porque introduciría una ruta de configuración/despliegue nueva sin necesidad.

## §3. Cómo enviar el Número de Operación y el archivo en la confirmación del pago

**Decision**: Cambiar el endpoint `POST /api/creditos/{prospectoId}/pagar-cuota` (hoy sin
cuerpo) para que reciba `multipart/form-data` con un campo de texto `numeroOperacion`
(obligatorio) y un campo de archivo `archivo` (opcional), leídos como parámetros de Minimal API
(`string numeroOperacion, IFormFile? archivo`), igual convención que ya usa el endpoint de
subida de Requisitos.

**Rationale**: Es el mismo mecanismo de transporte (`multipart/form-data`) que el proyecto ya
usa para archivos (Requisitos), por lo que el frontend reutiliza el mismo patrón `FormData` ya
implementado en `ProspectosService.subirArchivoRequisito` (`frontend/src/app/core/services/prospectos.service.ts`),
solo que ahora agregando también el campo de texto al mismo `FormData`.

**Alternatives considered**:
- Dos llamadas separadas (una para registrar el pago con el Número de Operación en JSON, otra
  para subir el archivo): rechazada porque complica la atomicidad (¿qué pasa si la segunda
  llamada falla tras confirmar el pago?) y no aporta valor frente a una sola llamada
  multipart que guarda ambos datos en la misma transacción de base de datos.

## §4. Formatos y tamaño máximo del archivo de evidencia

**Decision**: Validar en el propio endpoint que el archivo, cuando se adjunte, sea JPG, PNG o
PDF (por `ContentType` o extensión, igual que la validación de PDF ya existente en Requisitos)
y no supere 5 MB, devolviendo `400 Bad Request` con un mensaje claro si no cumple, sin bloquear
el registro del pago si el Cajero decide continuar sin adjuntar evidencia (campo opcional).

**Rationale**: Valores documentados como supuestos razonables en el spec (`Assumptions`); se
valida con el mismo estilo ya usado para el PDF de Requisitos (comparación de `ContentType`/
extensión), sin agregar una librería de validación de archivos nueva.

**Alternatives considered**: Ninguna — son los límites ya acordados en el spec; esta sección
solo documenta el mecanismo de validación (código propio, sin dependencia nueva).
