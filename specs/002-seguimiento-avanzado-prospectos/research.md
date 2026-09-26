# Research: Seguimiento Avanzado de Prospectos (Bandeja, Onboarding Bancario y Comentarios de Aprobación)

**Feature**: `002-seguimiento-avanzado-prospectos` | **Fecha**: 2026-09-25

Este documento resuelve las decisiones técnicas necesarias para pasar del spec al diseño
(Fase 1). Al ser una extensión de `001-gestion-creditos`, se reutiliza el stack y los patrones
ya decididos allí (`001-gestion-creditos/research.md`); aquí solo se documentan las decisiones
nuevas que introduce este feature.

## 1. "Paso actual" del prospecto sin nuevos valores de `Estado`

- **Decision**: No se agregan nuevos valores al enum `EstadoProspecto` de `001`. En su lugar,
  cuando un prospecto está en estado `Simulacion` o `Evaluacion` ("En proceso" en la bandeja,
  FR-001/FR-002), el backend calcula un "paso actual" a partir de los datos ya presentes:
  1. `Estado = Simulacion` → paso = Simulación (Pantalla 2).
  2. `Estado = Evaluacion` y faltan datos de Onboarding (Nombres/Apellidos/Dirección/Cuenta) →
     paso = Onboarding (Pantalla 3).
  3. `Estado = Evaluacion`, Onboarding completo, `Producto.RequiereDeclaracionInversion = true`
     y no existe `DeclaracionInversion` para el prospecto → paso = Declaración de Inversión.
  4. `Estado = Evaluacion`, Onboarding (y Declaración de Inversión si aplica) completos,
     `Producto.RequiereRequisitos = true` y faltan Requisitos por adjuntar → paso = Requisitos
     (Pantalla 4).
  5. `Estado = Desembolso` → paso = Desembolso (Pantalla 6, pendiente de generar el PDF final
     y cerrar).
  El estado `Observado` siempre navega a Onboarding (FR-003), independientemente del paso
  calculado, tal como pide el spec.
- **Rationale**: Cumple el Principio I (Simplicidad Ante Todo): añadir nuevos valores de
  `Estado` por cada sub-paso duplicaría información ya derivable de los datos existentes y
  complicaría la máquina de estados ya validada en `001`. Calcular el paso en el momento de
  listar la bandeja es más simple y no requiere migraciones adicionales de datos históricos.
- **Alternatives considered**: Agregar un campo `PasoActual` persistido en `Prospecto` que se
  actualiza en cada transición — descartado porque duplica información derivable (riesgo de
  quedar desincronizado) sin aportar valor adicional frente al cálculo en lectura.

## 2. Identidad del "asesor" en la bandeja

- **Decision**: Al igual que en `001-gestion-creditos` (research.md §4/§5), no existe una
  sesión de usuario autenticada. La "bandeja del Asesor" es una vista global de todos los
  prospectos activos (y, para Historia 5, editables), visible igual para cualquier usuario que
  haya elegido el rol Asesor en el selector de rol. No se filtra por un Asesor específico.
- **Rationale**: El spec de `002` no pide un mecanismo de asignación de prospectos por Asesor,
  y el spec de `001` ya declaró fuera de alcance la autenticación real. Introducir una noción
  de "mis prospectos" por Asesor requeriría un mecanismo de identidad no solicitado, violando
  el Principio III (Cero Alcance Fantasma).
- **Alternatives considered**: Asociar cada Prospecto a un `Usuario` (Asesor) al crearse —
  descartado por la misma razón que en `001-gestion-creditos` research.md §5 (Usuario es un
  registro administrativo, no una sesión).

## 3. Edición de datos del cliente desde la bandeja (Historia 5)

- **Decision**: Se reutiliza el mismo endpoint `PUT /api/prospectos/{id}/onboarding` tanto
  para el guardado inicial del Onboarding como para la edición posterior desde la bandeja
  (Historia 5) y para la corrección tras "Observado" (FR-003, ya existente en `001`). El
  backend valida el `Estado` actual del prospecto y rechaza la operación (`409 Conflict`) si
  está en `Aprobacion` o en un estado cerrado (`Rechazado`/`Finalizado`), conforme a FR-016/
  FR-017.
- **Rationale**: Los tres casos de uso (guardar por primera vez, corregir tras Observado,
  editar desde la bandeja) actualizan exactamente los mismos campos (Nombres, Apellidos,
  Dirección, Cuenta Bancaria de Desembolso). Crear un endpoint paralelo solo para la bandeja
  duplicaría lógica de validación sin necesidad, en contra del Principio I.
- **Alternatives considered**: Endpoint separado `PUT /api/prospectos/{id}/datos-cliente` —
  descartado por duplicar la validación de campos y las reglas de cuenta bancaria ya definidas
  para el Onboarding.

## 4. Comentario del Aprobador (Historia 4)

- **Decision**: El comentario se guarda como un único campo `ComentarioAprobador`
  (`string(1000)`, nullable) en `Prospecto`, sobrescrito cada vez que el Aprobador registra una
  decisión de "Observado" o "Rechazado". No se crea una tabla de historial de comentarios.
- **Rationale**: El spec (FR-014, tooltip en la bandeja) solo exige mostrar "el comentario"
  asociado a la decisión vigente, no un historial completo de comentarios pasados. Un único
  campo es la solución más simple que cumple el requisito (Principio I).
- **Alternatives considered**: Tabla `ComentarioDecision` con historial completo — descartada
  por no estar solicitada en el spec (violaría el Principio III, Cero Alcance Fantasma, si se
  construyera sin que el negocio la haya pedido).

## 5. Moneda de la Cuenta Bancaria Interna

- **Decision**: El campo `Moneda` de `CuentaBancariaInterna` usa el mismo enum `Moneda { PEN }`
  ya definido para `Producto` en `001-gestion-creditos`, es decir, un único valor posible
  (Soles).
- **Rationale**: El Principio II de la constitución exige que "todos los montos" se expresen
  en Soles. Aunque el spec no fue explícito sobre si la Cuenta Interna admite otra moneda,
  introducir Dólares (u otra moneda) para la cuenta de desembolso sin que el negocio lo pida
  expresamente arriesgaría contradecir el mismo principio que ya obligó a restringir la Moneda
  del Producto en `001` (ver `001-gestion-creditos/research.md` §7). Se sigue el mismo
  precedente por consistencia y para evitar alcance no solicitado.
- **Alternatives considered**: Permitir Soles y Dólares en la Cuenta Interna — descartado por
  las mismas razones que en `001`; de necesitarse, requeriría una enmienda explícita de la
  constitución antes de implementarse.

## 6. Origen de Fondos de la Declaración de Inversión (Historia 3)

- **Decision**: El campo `Origen de Fondos` se implementa como una lista cerrada (enum):
  `Ahorros`, `Herencia`, `VentaActivo`, `ActividadEmpresarial`, `Otro` — tal como se acordó con
  el usuario en la sesión de clarificación del spec.
- **Rationale**: Una lista cerrada es más simple de validar y mostrar en pantalla que un campo
  de texto libre, y corresponde exactamente a lo que el usuario aprobó en `/speckit-specify`.
  El campo `Detalle` (opcional, texto libre) cubre los casos no contemplados en la lista (ej.
  "Otro").
- **Alternatives considered**: Texto libre único — descartado porque el usuario ya aprobó
  explícitamente la lista cerrada con un campo de detalle opcional.

## 7. Generación del PDF de Aprobación Final (actualización de FR-027 de `001`)

- **Decision**: `PdfGenerator` (ya existente, `PdfSharpCore`) se modifica para leer la Cuenta
  Bancaria de Desembolso vigente del prospecto (Interna: Número de Cuenta + Moneda; Externa:
  Banco + CCI) en vez del campo único `CuentaBancariaCCI` que usaba en `001`.
- **Rationale**: Es un cambio incremental sobre el mismo servicio ya implementado; no se
  introduce una nueva herramienta de generación de PDF ni una nueva dependencia.
- **Alternatives considered**: Ninguna — no hay una alternativa técnica relevante, es una
  actualización directa del origen de datos que consume el generador existente.

## 8. Migración de datos existentes (prospectos creados bajo `001`)

- **Decision**: La migración de EF Core que agrega las columnas nuevas a `Prospecto`
  (`TipoCuentaDesembolso`, `CuentaBancariaInternaId`, `CuentaExternaBanco`, `CuentaExternaCCI`,
  `ComentarioAprobador`) migra el valor ya existente de `CuentaBancariaCCI` (si lo hay) como
  cuenta **Externa** con `CuentaExternaCCI = CuentaBancariaCCI` y `CuentaExternaBanco = null`
  (a completar manualmente si se requiere), y luego elimina la columna `CuentaBancariaCCI`.
- **Rationale**: Evita perder datos de prospectos ya en curso al pasar de la v1 a la v2 del
  Onboarding, sin necesidad de construir una pantalla de migración de datos (fuera de alcance
  del spec). Como el proyecto está en una etapa temprana (sin datos de producción reales), este
  camino simple es suficiente.
- **Alternatives considered**: Mantener ambas columnas (`CuentaBancariaCCI` y las nuevas) —
  descartado por dejar un campo muerto en el modelo, en contra del Principio I.

## Resumen de "NEEDS CLARIFICATION" resueltos

Ninguno queda pendiente. Los tres puntos que requerían aclaración de negocio (campos de la
Declaración de Inversión, alcance de las cuentas internas, alcance de la edición desde la
bandeja) ya se resolvieron con el usuario durante `/speckit-specify` y están reflejados en el
spec. Las decisiones técnicas de este documento (§1 a §8) no dejan ningún punto abierto del
Technical Context del plan.
