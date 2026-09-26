# Data Model: Gestión de Créditos (Prospecto a Desembolso)

**Feature**: `001-gestion-creditos` | **Fecha**: 2026-09-24

Todas las entidades se modelan como clases de EF Core (Code-First) con sus migraciones
correspondientes. Los nombres de tabla/columna finales los define la implementación; aquí se
fija el contrato de datos (campos, tipos, relaciones, reglas).

## TipoDocumento

| Campo | Tipo | Regla |
|-------|------|-------|
| Id | int (PK) | autogenerado |
| Nombre | string(100) | requerido, único (ej. "DNI", "Carné de Extranjería") |
| Activo | bool | default true (deshabilitar en vez de borrar) |

**Relación**: N:M con `TipoPersona` mediante tabla de asociación `TipoDocumentoTipoPersona`.

## TipoPersona

| Campo | Tipo | Regla |
|-------|------|-------|
| Id | int (PK) | autogenerado |
| Nombre | string(100) | requerido, único (ej. "Natural", "Jurídica") |
| Activo | bool | default true |

## Producto

| Campo | Tipo | Regla |
|-------|------|-------|
| Id | int (PK) | autogenerado |
| Nombre | string(150) | requerido |
| Moneda | enum { PEN } | fijo en Soles (S/); única moneda soportada (Principio II de la constitución, corregido tras `/speckit-analyze`) |
| MontoMin / MontoMax | decimal(18,2) | `MontoMin > 0` y `MontoMax >= MontoMin` |
| TasaMin / TasaMax | decimal(5,2) | tasa periódica (%), `TasaMax >= TasaMin >= 0` |
| PlazoMin / PlazoMax | int | número de cuotas, `PlazoMax >= PlazoMin >= 1` |
| FrecuenciaPago | enum { Mensual } | único valor soportado en v0 (FR-002) |
| DiaPago | enum { Dia5, Dia25 } | requerido |
| CargoOtrosPorCuota | decimal(18,2) | cargo fijo por cuota (Sección "Otros" del cronograma) |
| RequiereRequisitos | bool | activa/desactiva la fase de Requisitos y Aprobación (FR-020/FR-022) |
| Activo | bool | default true |

**Relación**: 1:N con `Requisito` (solo relevante cuando `RequiereRequisitos = true`).

## Requisito

| Campo | Tipo | Regla |
|-------|------|-------|
| Id | int (PK) | autogenerado |
| ProductoId | int (FK → Producto) | requerido |
| Nombre | string(150) | requerido (ej. "Copia de DNI") |
| Activo | bool | default true |

## Usuario

| Campo | Tipo | Regla |
|-------|------|-------|
| Id | int (PK) | autogenerado |
| Nombre | string(150) | requerido |
| Rol | enum { Administrador, Asesor, Aprobador } | requerido |
| Activo | bool | default true |

**Nota de diseño**: registro administrativo de referencia (FR-004). No se vincula a la sesión
activa: el selector de rol (FR-005) no requiere iniciar sesión como un Usuario específico
(ver `research.md` §5).

## Prospecto

| Campo | Tipo | Regla |
|-------|------|-------|
| Id | int (PK) | autogenerado |
| TipoDocumentoId | int (FK) | requerido |
| NumeroDocumento | string(20) | requerido |
| ResultadoMock | int | 1–5, generado en la validación (FR-007) |
| ProductoId | int (FK, nullable) | se asigna al elegir Producto en la Simulación |
| Nombres / Apellidos | string(150) | requeridos al completar Onboarding (FR-017) |
| Direccion | string(250) | requerido al completar Onboarding |
| CuentaBancariaCCI | string(20) | numérico, exactamente 20 dígitos (FR-018) |
| Estado | enum (ver máquina de estados abajo) | controla el macro-flujo (RF10) |
| FechaCreacion | datetime | fecha de la validación que originó el prospecto |
| FechaCierre | datetime (nullable) | fecha en que se llega a Rechazado o Finalizado |

**Regla de unicidad (FR-010)**: no puede existir más de un `Prospecto` con el mismo
`NumeroDocumento` cuyo `Estado` no sea `Rechazado` ni `Finalizado` ("activo").

**Regla de creación (FR-008)**: si el `ResultadoMock` de un intento de validación es 4 o 5,
**no se crea una fila de Prospecto**; el rechazo se muestra solo en pantalla, permitiendo un
reintento inmediato con el mismo documento.

### Máquina de estados de Prospecto

```
Simulacion → Evaluacion → [RequiereRequisitos?]
  Sí → Aprobacion → (Aprobado → Desembolso) | (Observado → Evaluacion) | (Rechazado → [fin])
  No → Desembolso (automático, Fast-Track)
Desembolso → Finalizado (el Asesor cierra el proceso, FR-028)
```

- `Simulacion`: creado tras una validación Mock favorable (1–3); aún sin Onboarding.
- `Evaluacion`: tras aceptar la simulación (FR-015) y mientras se completa Onboarding /
  Requisitos.
- `Aprobacion`: solo si `Producto.RequiereRequisitos = true`, tras cargar todos los
  Requisitos (FR-021).
- `Observado`: decisión del Aprobador (FR-025); regresa a `Evaluacion` una vez el Asesor
  corrige datos/documentos, para volver a pasar por `Aprobacion`.
- `Rechazado`: decisión del Aprobador (FR-026); estado terminal, cuenta para el historial
  (FR-029).
- `Desembolso`: alcanzado por Fast-Track (FR-022) o por aprobación (FR-024); el Asesor genera
  el PDF final (FR-027).
- `Finalizado`: el Asesor cierra el proceso (FR-028); estado terminal, cuenta para el
  historial (FR-029) y libera el `NumeroDocumento`.

"Historial de prospectos cerrados" (FR-029) = todo `Prospecto` con `Estado` en
`{Rechazado, Finalizado}`.

## Simulacion

| Campo | Tipo | Regla |
|-------|------|-------|
| Id | int (PK) | autogenerado |
| ProspectoId | int (FK) | requerido |
| ProductoId | int (FK) | producto elegido al simular |
| Monto | decimal(18,2) | dentro de `[Producto.MontoMin, Producto.MontoMax]` |
| Tasa | decimal(5,2) | dentro de `[Producto.TasaMin, Producto.TasaMax]`, tasa periódica |
| Plazo | int | dentro de `[Producto.PlazoMin, Producto.PlazoMax]` |
| FechaSimulacion | datetime | momento del cálculo (botón "Simular") |
| Aceptada | bool | `true` solo cuando el Asesor presiona "Aceptar" (FR-015) |
| FechaAceptacion | datetime (nullable) | momento de aceptación |

**Regla (CA1/FR-015)**: una simulación no aceptada no debe persistirse; solo se guarda al
presionar "Aceptar". Solo puede existir una `Simulacion` con `Aceptada = true` por Prospecto.

### Cuota

| Campo | Tipo | Regla |
|-------|------|-------|
| Id | int (PK) | autogenerado |
| SimulacionId | int (FK) | requerido |
| Numero | int | 1..Plazo |
| FechaPago | date | según `Producto.DiaPago` (ver `research.md` §6) |
| SaldoInicial | decimal(18,2) | ver fórmula en `research.md` §6 |
| Amortizacion | decimal(18,2) | `Monto ÷ Plazo`, ajustada en la última cuota |
| Interes | decimal(18,2) | `SaldoInicial × Tasa` (saldo insoluto) |
| Otros | decimal(18,2) | `Producto.CargoOtrosPorCuota` |
| PagoTotal | decimal(18,2) | `Amortizacion + Interes + Otros` |
| SaldoFinal | decimal(18,2) | `SaldoInicial − Amortizacion`; en la última cuota, `0.00` |

## DocumentoAdjunto

| Campo | Tipo | Regla |
|-------|------|-------|
| Id | int (PK) | autogenerado |
| ProspectoId | int (FK) | requerido |
| RequisitoId | int (FK) | requerido |
| NombreArchivo | string(255) | nombre original del PDF cargado |
| Contenido | varbinary(max) | contenido binario del PDF (ver `research.md` §3) |
| FechaCarga | datetime | requerido |

**Regla (FR-020/edge case)**: solo se aceptan archivos PDF; un nuevo cargue del mismo
Requisito reemplaza al anterior (permite corregir tras "Observado", FR-025).

## DocumentoGenerado

| Campo | Tipo | Regla |
|-------|------|-------|
| Id | int (PK) | autogenerado |
| ProspectoId | int (FK) | requerido |
| Tipo | enum { Cronograma, AprobacionFinal } | requerido |
| Contenido | varbinary(max) | PDF generado (FR-016 / FR-027) |
| FechaGeneracion | datetime | requerido |

## Validación cruzada con Success Criteria

- SC-003 (rangos respetados): se aplica al validar `Simulacion.Monto/Tasa/Plazo` contra los
  límites del `Producto` antes de calcular o guardar.
- SC-004 (cuenta bancaria refleja en el PDF): `DocumentoGenerado(Tipo=AprobacionFinal)` se
  construye leyendo `Prospecto.CuentaBancariaCCI` en el momento de generación.
- SC-006 (unicidad): se aplica en la regla de unicidad de `Prospecto` descrita arriba.
- SC-007 (historial): se resuelve con la consulta de `Prospecto` por `Estado` descrita arriba.
