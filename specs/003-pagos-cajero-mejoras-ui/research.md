# Research: Pagos de Cuota, Rol Cajero y Mejoras de Simulación/Administración

**Feature**: `003-pagos-cajero-mejoras-ui` | **Fecha**: 2026-09-26

Este documento resuelve las decisiones técnicas nuevas de este feature, extendiendo el stack y
los patrones ya decididos en `001-gestion-creditos/research.md` y
`002-seguimiento-avanzado-prospectos/research.md`.

## 1. Registro de pago de cuotas: campos en `Cuota`, sin nueva tabla

- **Decision**: Se agregan dos campos a la entidad `Cuota` ya existente: `Pagada` (bool, default
  `false`) y `FechaPagoRealizado` (DateTime, nullable — distinto del `FechaPago` ya existente de
  `001`, que es la fecha programada de vencimiento). No se crea una tabla `Pago` separada.
- **Rationale**: El spec (Assumptions) declara explícitamente que el pago es "todo o nada" (sin
  pagos parciales) y que no se contempla reversar un pago. Con esas dos restricciones, no hace
  falta un historial de transacciones de pago; basta con un flag + fecha por Cuota, igual de
  simple que el patrón ya usado para `Simulacion.Aceptada`/`FechaAceptacion` en `001`.
- **Alternatives considered**: Tabla `PagoCuota` con historial completo — descartada por
  Principio I (Cero Alcance Fantasma): el spec no pide reversar ni auditar múltiples intentos de
  pago sobre la misma cuota.
- **Migración de datos (FR-014)**: la nueva migración de EF Core incluye un
  `migrationBuilder.Sql(...)` que marca `Pagada = 1` y `FechaPagoRealizado = FechaCierre` en todas las
  `Cuotas` cuya `Simulacion.ProspectoId` pertenezca a un `Prospecto` con `Estado = Finalizado`
  (valor 6 del enum), replicando el patrón ya usado en `002-seguimiento-avanzado-prospectos`
  research.md §8 para migrar datos existentes sin pantalla de migración manual.

## 2. Un único endpoint de "Créditos" para Cajero, Asesor y Administrador

- **Decision**: Se crea `GET /api/creditos?buscar={texto opcional}`, que agrupa los Prospectos
  con Onboarding completo (Nombres no nulo) y `Estado != Rechazado` por cliente
  (`TipoDocumentoId` + `NumeroDocumento`), calculando para cada crédito su Monto Solicitado,
  Cuota Actual y estado ("En proceso"/"Pendiente"/"Cancelado"). El parámetro `buscar` filtra por
  coincidencia parcial (sin distinguir mayúsculas) contra el Nombre completo o el Número de
  Documento. Sin `buscar`, devuelve todos los clientes (uso del Cajero, Historia 1); con
  `buscar` igual al Número de Documento exacto, sirve también a la consulta puntual de un
  cliente (Historia 2, hoy resuelta por la pantalla de Historial).
- **Rationale**: Las Historias 1 y 2 del spec piden esencialmente la misma proyección de datos
  (créditos de un cliente con Monto Solicitado/Cuota Actual/estado, excluyendo Rechazados);
  solo cambia si se filtra por un cliente puntual o se listan todos. Un único endpoint
  parametrizado evita duplicar la lógica de agrupación y de cálculo de estado (Principio I).
  Esto reemplaza el uso que `historial.component.ts` hacía de
  `GET /api/prospectos/historial` (endpoint angosto de `001`, limitado a Rechazado/Finalizado).
- **Alternatives considered**: Mantener `/api/prospectos/historial` para la Historia 2 y crear
  un endpoint totalmente aparte para la bandeja del Cajero — descartado por duplicar la consulta
  de agrupación por cliente y el cálculo de estado de crédito en dos lugares.
- **Retiro de `/api/prospectos/historial`**: Ningún otro componente del frontend lo consume
  aparte de `historial.component.ts` (verificado por búsqueda en el código), por lo que se
  reemplaza en el mismo componente sin dejar código muerto.

## 3. Cálculo de "Cuota Actual" y estado de Crédito (sin nuevo estado en `Prospecto`)

- **Decision**: Igual que el "paso actual" derivado de `002` (research.md §1), el estado del
  Crédito se calcula en el momento de responder la petición, sin nuevos valores en el enum
  `EstadoProspecto`:
  - `Estado` distinto de `Desembolso` y `Finalizado` → `"EnProceso"` (sin Cuota Actual).
  - `Estado` en `{Desembolso, Finalizado}` y existe alguna `Cuota` de la simulación aceptada con
    `Pagada = false` → `"Pendiente"`; la Cuota Actual es la de menor `Numero` con
    `Pagada = false`.
  - `Estado` en `{Desembolso, Finalizado}` y todas las `Cuota` tienen `Pagada = true` →
    `"Cancelado"` (sin Cuota Actual).
  - `Estado = Rechazado` → el crédito se excluye por completo de la respuesta (FR-010).
- **Rationale**: Reutiliza exactamente el mismo patrón ya validado en `002` para el "paso
  actual" de la bandeja del Asesor; evita introducir un campo de estado redundante que podría
  desincronizarse del estado real de las Cuotas.
- **Alternatives considered**: Persistir un campo `EstadoCredito` en `Prospecto` actualizado en
  cada pago — descartado por la misma razón que en `002` §1 (duplica información derivable).

## 4. Rol "Cajero": mismo mecanismo de selector de rol, sin nueva infraestructura de sesión

- **Decision**: `Cajero` se agrega como un cuarto valor del tipo `Rol` del frontend
  (`rol-activo.service.ts`) y del enum `RolUsuario` del backend, reutilizando exactamente el
  mismo selector de rol sin usuario/contraseña ya definido en `001-gestion-creditos/research.md`
  §4. El backend no valida el rol del lado del servidor (mismo riesgo ya aceptado y documentado
  en `001`).
- **Rationale**: El spec no pide un mecanismo de autenticación real para el Cajero; sería
  alcance no solicitado (Principio III) introducir uno solo para este rol mientras los otros
  tres no lo tienen.
- **Alternatives considered**: Ninguna — ya se decidió el patrón de roles sin autenticación real
  en `001`; este feature solo añade un valor más a un mecanismo ya existente.

## 5. Endpoint de pago de cuota: valida estado "Pendiente" en el servidor

- **Decision**: `POST /api/creditos/{prospectoId}/pagar-cuota` recalcula el estado del crédito
  en el servidor (ver §3) y devuelve `409 Conflict` si no es `"Pendiente"` (créditos
  `"EnProceso"` o `"Cancelado"` no tienen cuota que cobrar), en vez de confiar únicamente en que
  el frontend oculte la acción.
- **Rationale**: El backend de este proyecto no valida identidad ni rol (ver §4), por lo que
  cualquier regla de negocio que dependa del estado de los datos (no del rol) sí debe validarse
  en el servidor para mantener la integridad de los datos, igual que ya se hace con los guards
  de estado de `002` (edición bloqueada en Aprobación/estados cerrados).
- **Alternatives considered**: Confiar solo en el frontend (ocultar el botón) — descartado por
  ser la única capa de protección de un dato de negocio (el estado del pago), a diferencia del
  control de acceso por rol que sí se dejó deliberadamente sin verificar en el servidor.

## 6. Encabezado "Nombre — Monto Solicitado": componente compartido, dato expuesto en `ProspectoDetalleDto`

- **Decision**: Se agrega `MontoSolicitado` (decimal, nullable) a `ProspectoDetalleDto`,
  calculado desde `Simulacion.Monto` de la simulación aceptada del prospecto (la misma que ya
  usan el cronograma y los PDF generados). En el frontend, se crea un componente standalone
  compartido `EncabezadoClienteComponent` (recibe un `ProspectoDetalle` por `@Input`) que se
  incluye en Onboarding (modo edición), Declaración de Inversión, Requisitos, Aprobación y
  Desembolso.
- **Rationale**: Evitar duplicar la misma lógica de formato ("si hay Nombres y MontoSolicitado,
  mostrar encabezado") en 5 plantillas distintas (Principio I); ya existe precedente de
  extraer un helper compartido en `002` (`cuenta-desembolso.ts`).
- **Alternatives considered**: Repetir el `<p>{{ nombre }} — {{ monto }}</p>` en cada plantilla
  — descartado por duplicación innecesaria.

## 7. Validación Monto a Invertir ≤ Monto Solicitado

- **Decision**: La validación se agrega en el backend (`PUT
  /api/prospectos/{id}/declaracion-inversion`, ya existente desde `002`), comparando
  `dto.MontoInvertir` contra el `MontoSolicitado` calculado en §6 (mismo dato, reutilizado); el
  frontend agrega la misma validación para dar retroalimentación inmediata, sin depender solo
  del backend.
- **Rationale**: Coherente con el patrón ya usado en todo el proyecto (validación de rango de
  Monto/Tasa/Plazo en la Simulación, validación de CCI en Onboarding): el servidor es la fuente
  de verdad, el frontend solo mejora la experiencia.

## 8. Diagnóstico del "hay registros que no se ve" en pantallas de Administrador

- **Decision**: Se confirmó (revisando el HTML actual) que las tablas de `tipos-documento.
  component.html` y `usuarios.component.html` **no** están envueltas en un contenedor
  `.table-responsive`, a diferencia de `productos.component.html` que sí lo tiene desde `001`.
  Además, `tipos-documento.component.html` ubica su tabla dentro de una columna `col-md-7`
  (58% del ancho), lo que agrava el problema: cualquier tabla más ancha que esa columna se
  desborda sin mecanismo de scroll, superponiéndose visualmente con la columna vecina
  (`col-md-5`) y dejando inaccesibles las últimas columnas/acciones. No se encontró ningún
  `overflow: hidden`, límite de altura, ni paginación/`Take()` en el backend que oculte filas;
  el problema es puramente de layout, no de datos faltantes.
- **Rationale para la solución**: Envolver ambas tablas en `.table-responsive` (mismo patrón que
  `productos.component.html`) resuelve el desbordamiento sin cambiar ningún dato ni
  comportamiento, cumpliendo FR-019 con el cambio más simple posible.
- **Alternatives considered**: Rediseñar el layout de dos columnas de Tipos de Documento a una
  sola columna apilada — no es necesario; con `.table-responsive` el problema ya queda resuelto
  incluso dentro de una columna angosta.

## Resumen de "NEEDS CLARIFICATION" resueltos

Ninguno queda pendiente. La única aclaración de negocio (créditos "Finalizado" previos) ya se
resolvió con el usuario en `/speckit-specify`. Las decisiones técnicas §1 a §8 no dejan ningún
punto abierto del Technical Context del plan.
