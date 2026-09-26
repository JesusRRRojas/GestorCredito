# Quickstart: Pagos de Cuota, Rol Cajero y Mejoras de Simulación/Administración

**Feature**: `003-pagos-cajero-mejoras-ui` | **Fecha**: 2026-09-26

Guía de validación manual (Principio V de la constitución). Requiere que `001` y `002` ya estén
implementados y corriendo, más los cambios de este feature aplicados (incluida la migración de
base de datos).

## Prerrequisitos

1. Backend y frontend corriendo localmente.
2. Al menos un cliente con: un crédito Rechazado, uno "En proceso" (aún sin desembolsar), uno
   "Pendiente" (Desembolsado, con cuotas sin pagar) y, si es posible, uno ya "Cancelado" (todas
   las cuotas pagadas) o uno "Finalizado" de antes de este feature (para ver la migración
   automática a "Cancelado").

## Escenario 1 — Pago de cuota por el Cajero (Historia 1)

1. Entrar con el rol "Cajero" (nuevo botón en el selector de rol).
2. **Verificar**: se ve una bandeja con todos los clientes con Onboarding completo y sus
   créditos (sin ninguna opción de edición).
3. Escribir parte de un nombre o un Número de Documento en el buscador.
   **Verificar**: la bandeja se filtra a las coincidencias.
4. Seleccionar un crédito "Pendiente" de un cliente.
   **Verificar**: se muestra la Cuota Actual (número, fecha, monto) y la opción de registrar el
   pago; para créditos "En proceso" o "Cancelado" no aparece esa opción.
5. Confirmar el pago de la Cuota Actual.
   **Verificar**: la cuota queda pagada; si quedan cuotas, la siguiente pasa a ser la nueva
   Cuota Actual; si era la última, el crédito pasa a "Cancelado".
6. Intentar acceder directamente (cambiando la URL) a una pantalla de edición (Onboarding,
   Productos, Requisitos, Aprobación).
   **Verificar**: el sistema lo impide por el rol.

## Escenario 2 — Consulta de créditos por cliente (Historia 2)

1. Como Asesor o Administrador, ir a la pantalla de Créditos/Historial e ingresar el Número de
   Documento de un cliente con varios créditos (incluido uno Rechazado).
2. **Verificar**: se listan todos los créditos salvo el Rechazado, cada uno con Monto
   Solicitado, Cuota Actual (cuando aplica) y estado ("En proceso"/"Pendiente"/"Cancelado").

## Escenario 3 — Totales del cronograma (Historia 3)

1. Como Asesor, generar cualquier simulación en la Pantalla 2.
2. **Verificar**: al final de la tabla aparece una fila con la suma de Interés, la suma de
   Otros y la suma de Pago Total; al cambiar Monto/Tasa/Plazo y volver a simular, los totales se
   actualizan.

## Escenario 4 — Encabezado Nombre / Monto Solicitado (Historia 4)

1. Con un prospecto que ya completó el Onboarding, abrir Declaración de Inversión, Requisitos,
   Aprobación o Desembolso.
   **Verificar**: aparece un encabezado con el Nombre completo y el Monto Solicitado (S/).
2. Con un prospecto que aún no completó el Onboarding, abrir su pantalla correspondiente.
   **Verificar**: el encabezado no aparece.

## Escenario 5 — Validación Monto a Invertir ≤ Monto Solicitado (Historia 5)

1. En un prospecto con Producto que requiere Declaración de Inversión y Monto Solicitado de,
   por ejemplo, S/5,000, intentar guardar un Monto a Invertir de S/6,000.
   **Verificar**: el sistema rechaza el guardado.
2. Guardar un Monto a Invertir de S/5,000 (igual) o menor.
   **Verificar**: el sistema lo acepta.

## Escenario 6 — Administrador: scroll garantizado y rediseño de Productos (Historia 6)

1. Con varios Productos, Tipos de Documento y Usuarios configurados, abrir cada pantalla de
   Administrador en un ancho de pantalla estándar.
   **Verificar**: todos los registros y columnas son visibles o alcanzables con scroll, sin
   ningún elemento oculto o recortado.
2. Abrir la pantalla de Productos.
   **Verificar**: el diseño se ve ordenado y profesional (indicadores de estado con color,
   espaciado consistente), sin cambios en los datos ni el comportamiento.
3. Como Asesor, revisar el menú principal.
   **Verificar**: hay un enlace directo a la Bandeja del Asesor (`002`).

## Resultado esperado

Todos los criterios de aceptación de `spec.md` (Historias 1 a 6) quedan verificados sin
necesidad de leer código fuente, logs ni consultar la base de datos directamente.
