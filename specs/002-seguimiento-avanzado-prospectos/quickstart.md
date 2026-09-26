# Quickstart: Seguimiento Avanzado de Prospectos

**Feature**: `002-seguimiento-avanzado-prospectos` | **Fecha**: 2026-09-25

Guía de validación manual (Principio V de la constitución: verificable por una persona no
técnica, sin leer código ni consultar la base de datos). Requiere que `001-gestion-creditos`
ya esté implementado y corriendo (backend + frontend + SQL Server), más los cambios de este
feature aplicados.

## Prerrequisitos

1. Backend corriendo en `http://localhost:5264` y frontend en `http://localhost:4200` (ver
   sesión de arranque habitual del proyecto).
2. Como Administrador: tener al menos dos Productos configurados — uno con
   `Requiere Declaración de Inversión` activado y otro sin activar; al menos uno con
   `Requiere Requisitos` activado.
3. Migración de base de datos aplicada (agrega `CuentaBancariaInterna`,
   `DeclaracionInversion` y las columnas nuevas de `Prospecto`/`Producto`).

## Escenario 1 — Bandeja del Asesor (Historia 1)

1. Entrar con el rol Asesor y abrir la Bandeja.
2. Crear (o usar) prospectos en distintos estados: uno recién simulado, uno en Onboarding, uno
   Observado, uno en Aprobación, uno Desembolsado y uno Rechazado.
3. **Verificar**: la bandeja muestra, para cada prospecto, Nombre del cliente (vacío si aún no
   completó Onboarding), Tipo y Número de Documento, y una etiqueta de estado: Bloqueado, En
   proceso, Aprobación, Desembolsado u Observado.
4. Hacer clic en el prospecto "En proceso" que quedó a medio Onboarding.
   **Verificar**: se abre directamente la pantalla de Onboarding con los datos ya ingresados,
   no la pantalla de Simulación.
5. Hacer clic en el prospecto "Observado".
   **Verificar**: se abre la pantalla de Onboarding (no la de Requisitos ni Aprobación).

## Escenario 2 — Cuenta bancaria interna y externa (Historia 2)

1. Como Asesor, llevar un prospecto nuevo hasta la pantalla de Onboarding.
2. Elegir "Cuenta Interna". **Verificar**: se muestra la lista de cuentas internas ya
   registradas para ese Número de Documento (vacía si es la primera vez) y la opción de
   agregar una nueva indicando solo Número de Cuenta y Moneda.
3. Agregar una cuenta interna nueva y guardar el Onboarding.
4. Iniciar un segundo prospecto para el mismo Número de Documento (tras cerrar o rechazar el
   primero) y volver a la pantalla de Onboarding, cuenta Interna.
   **Verificar**: la cuenta agregada en el paso 3 aparece disponible para reutilizar.
5. En otro prospecto, elegir "Cuenta Externa", ingresar Nombre del Banco y un CCI de 20
   dígitos, y guardar.
   **Verificar**: el sistema acepta el Onboarding; probar con un CCI de menos de 20 dígitos y
   confirmar que el sistema lo rechaza.

## Escenario 3 — Declaración de Inversión condicional (Historia 3)

1. Llevar un prospecto con Producto que **requiere** Declaración de Inversión hasta completar
   el Onboarding.
   **Verificar**: el sistema muestra la pantalla de Declaración de Inversión (Monto a
   Invertir, Origen de Fondos, Detalle opcional) antes de continuar.
2. Completar y guardar la Declaración de Inversión.
   **Verificar**: el flujo continúa a Requisitos (si el Producto los requiere) o a Aprobación/
   Desembolso.
3. Llevar otro prospecto con Producto que **no** requiere Declaración de Inversión hasta
   completar el Onboarding.
   **Verificar**: el sistema omite por completo esa pantalla.

## Escenario 4 — Comentario obligatorio del Aprobador (Historia 4)

1. Como Aprobador, abrir un prospecto en estado "Aprobación".
2. Intentar guardar la decisión "Observado" sin ingresar comentario.
   **Verificar**: el sistema lo impide y pide un comentario.
3. Ingresar un comentario y guardar la decisión "Observado".
4. Como Asesor, abrir la Bandeja y ubicar ese prospecto (ahora "Observado").
   **Verificar**: al pasar el cursor sobre el indicador de estado, aparece un tooltip con el
   comentario ingresado por el Aprobador.
5. Repetir los pasos 1–2 con la decisión "Rechazado" (también debe exigir comentario) y
   confirmar que la decisión "Aprobado" **no** exige comentario.

## Escenario 5 — Edición de datos del cliente desde la bandeja (Historia 5)

1. Como Asesor, en la Bandeja, elegir la opción de editar un cliente cuyo prospecto está "En
   proceso" (no en Aprobación).
2. Modificar la Dirección y guardar.
   **Verificar**: el cambio se refleja al continuar el flujo (por ejemplo, en el PDF de
   Aprobación Final generado más adelante).
3. Intentar editar los datos de un prospecto en estado "Aprobación".
   **Verificar**: el sistema lo impide mientras el Aprobador no haya decidido.

## Resultado esperado

Todos los criterios de aceptación de `spec.md` (Historias 1 a 5) quedan verificados sin
necesidad de leer código fuente, logs ni consultar la base de datos directamente.
