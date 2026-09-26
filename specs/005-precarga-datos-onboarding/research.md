# Research: Precarga de Datos del Cliente en Onboarding

**Feature**: `005-precarga-datos-onboarding` | **Fecha**: 2026-09-26

## §1. Cómo identificar el "prospecto cerrado más reciente" del mismo cliente

**Decision**: Al cargar `GET /api/prospectos/{id}` (`ProspectosEndpoints.cs`), cuando el
prospecto solicitado tiene `Nombres` vacío (aún no completó su propio Onboarding), consultar en
la misma base de datos el `Prospecto` con `TipoDocumentoId` y `NumeroDocumento` iguales, `Id`
distinto, `Estado` en `Prospecto.EstadosCerrados` (`Rechazado` o `Finalizado`, ya definido en
`001-gestion-creditos`) y `Nombres` no vacío, ordenado por `FechaCierre` descendente, tomando el
primero. `FechaCierre` se asigna en el mismo momento en que un prospecto pasa a `Rechazado` (tras
una decisión de Aprobación, `ProspectosEndpoints.cs` línea ~518) o a `Finalizado` (al cerrar en
Desembolso, línea ~553), por lo que ordenar por ese campo identifica correctamente "el más
reciente" sin ambigüedad.

**Rationale**: Reutiliza el mismo par `(TipoDocumentoId, NumeroDocumento)` que ya identifica a
"el cliente" en `002-seguimiento-avanzado-prospectos` (Cuentas Internas) y en
`003-pagos-cajero-mejoras-ui` (agrupación de créditos), sin introducir una entidad `Cliente`
nueva (Principio I). La condición `Nombres no vacío` excluye automáticamente los prospectos
rechazados por validación de riesgo (Mock 4/5) que nunca llegaron a Onboarding, cumpliendo el
edge case del spec sin lógica adicional.

**Alternatives considered**:
- Ordenar por `FechaCreacion` en vez de `FechaCierre`: rechazado porque un prospecto creado
  después pero cerrado antes (por ejemplo, rechazado rápido en Aprobación) no sería
  necesariamente "el más reciente" en términos de qué datos del cliente son más actuales; el
  cierre es el momento en que esos datos quedaron congelados como definitivos.
- Buscar entre TODOS los prospectos del cliente (activos y cerrados): rechazado porque, por
  regla ya existente (FR-010, `001-gestion-creditos`), no puede haber dos prospectos activos
  simultáneos para el mismo `NumeroDocumento`; el único prospecto activo posible es el propio
  prospecto que se está cargando (cuyo `Nombres` ya se sabe vacío), por lo que buscar solo entre
  cerrados es suficiente y más simple.

## §2. Cómo exponer la precarga al frontend sin duplicar la fuente de verdad

**Decision**: Agregar un campo opcional `Precarga` (registro `PrecargaOnboardingDto`) a
`ProspectoDetalleDto`, poblado únicamente cuando se encontró un prospecto cerrado calificado
(§1); `null` en cualquier otro caso (incluido cuando el prospecto ya tiene sus propios datos).
El frontend (`onboarding.component.ts`) solo usa `Precarga` para inicializar los campos del
formulario cuando `p.nombres` está vacío; nunca sobrescribe datos ya guardados del prospecto
actual.

**Rationale**: Mantiene `GET /api/prospectos/{id}` como el único punto de verdad ya consumido
por la pantalla (Principio I), evitando una llamada o endpoint adicional. La doble guarda
(backend solo la calcula si `Nombres` está vacío; frontend solo la aplica si `p.nombres` está
vacío) cumple FR-004 sin necesitar una bandera adicional de "es un dato precargado" en el
propio `Prospecto`.

**Alternatives considered**:
- Endpoint nuevo `GET /api/prospectos/{id}/precarga`: rechazado por Principio I; una segunda
  llamada de red para un dato que siempre se necesita junto con el resto del detalle no aporta
  valor frente a incluirlo en la misma respuesta ya existente.
- Devolver la precarga siempre (aunque el prospecto ya tenga sus propios datos) y dejar que el
  frontend decida si usarla: rechazado porque ejecutaría la consulta adicional en pantallas
  posteriores del flujo (Requisitos, Aprobación, Desembolso) donde nunca se necesita, sin
  ningún beneficio.

## §3. Cómo validar que la Cuenta Interna precargada sigue siendo válida

**Decision**: Antes de incluir un `CuentaBancariaInternaId` en la precarga, verificar con una
consulta a `CuentasBancariasInternas` que esa cuenta siga existiendo y con `Activo = true`
(mismo filtro ya usado por `GET /api/clientes/{tipoDocumentoId}/{numeroDocumento}/cuentas-internas`
en `002-seguimiento-avanzado-prospectos`); si no, se omite el `CuentaBancariaInternaId` de la
precarga (queda `null`), sin bloquear ni mostrar error (FR-007).

**Rationale**: Evita que el formulario preseleccione una cuenta que ya no aparece en la lista
visible del cliente (inconsistencia visual), reutilizando el mismo criterio `Activo` ya
existente en vez de introducir un estado nuevo para "cuenta eliminada".

**Alternatives considered**: Ninguna — es el mismo filtro ya usado por el endpoint de listado
de cuentas internas; esta sección solo documenta que se reutiliza también aquí.
