# Quickstart: Cronograma Visual y Comprobante de Pago en Bandeja del Cajero

**Feature**: `004-cronograma-comprobante-pago` | **Fecha**: 2026-09-26

Recorrido manual (Principio V de la constitución: verificable sin leer código ni consultar la
base de datos) para validar ambas historias de este feature. Requiere tener al menos un cliente
con un crédito en estado "Pendiente" (desembolsado, con cuotas por pagar) — puede reutilizarse
el mismo cliente de prueba usado en el quickstart de `003-pagos-cajero-mejoras-ui`.

## Prerequisitos

1. Backend corriendo (`dotnet run` en `backend/GestorCredito.Api`, perfil `http`,
   `http://localhost:5264`).
2. Frontend corriendo (`npm start` en `frontend/`, `http://localhost:4200`).
3. Migración de este feature aplicada (`dotnet ef database update` desde
   `backend/GestorCredito.Api`) para que `Cuota` tenga las columnas
   `NumeroOperacion`/`EvidenciaNombreArchivo`/`EvidenciaContenido`.
4. Un cliente con un crédito "Pendiente" (al menos una cuota ya pagada, una Cuota Actual y al
   menos una cuota futura, para poder distinguir los tres estados visuales).

## Historia 1 — Cronograma visual con estado por cuota

1. Entrar a la aplicación y elegir el rol **Cajero**.
2. En la bandeja, buscar al cliente de prueba (por nombre o Número de Documento).
3. Seleccionar su crédito "Pendiente".
4. **Verificar**: se muestra el cronograma completo de cuotas (número, fecha, monto), con un
   indicador visual distinto junto a cada cuota según su estado (pagada / actual / futura), y un
   resumen del tipo "X de Y cuotas pagadas".
5. **Verificar**: la cuota marcada como "actual" corresponde a la primera no pagada (coincide
   con lo que antes mostraba el bloque "Cuota Actual" de `003`).
6. Repetir con un crédito "Cancelado" del mismo u otro cliente: **verificar** que todas las
   cuotas aparecen como pagadas y ninguna se marca como actual.
7. Repetir con un crédito "En proceso" (aún no desembolsado): **verificar** que el sistema
   indica que ese crédito todavía no tiene cronograma, en vez de una tabla vacía.

## Historia 2 — Número de Operación y evidencia opcional

1. Con el crédito "Pendiente" seleccionado (paso 3 de la Historia 1), abrir el formulario de
   registro de pago de la Cuota Actual.
2. Intentar confirmar el pago sin ingresar ningún Número de Operación.
   **Verificar**: el sistema rechaza el registro e indica que el Número de Operación es
   obligatorio; la cuota sigue sin marcarse como pagada.
3. Ingresar un Número de Operación (por ejemplo, `OP-000123`) sin adjuntar ningún archivo y
   confirmar el pago.
   **Verificar**: el pago se registra con normalidad (igual que en `003`), la cuota pasa a
   "Pagada" en el cronograma y, al consultarla, muestra el Número de Operación ingresado.
4. Con otra cuota (u otro crédito "Pendiente"), ingresar un Número de Operación y adjuntar un
   archivo de evidencia válido (una imagen JPG/PNG o un PDF de menos de 5 MB) y confirmar el
   pago.
   **Verificar**: el pago se registra, la cuota queda "Pagada" con su Número de Operación, y es
   posible abrir/descargar la evidencia adjunta desde el cronograma.
5. Intentar adjuntar un archivo con un formato no soportado (por ejemplo, un `.zip`) o mayor a
   5 MB.
   **Verificar**: el sistema rechaza el archivo con un mensaje claro del motivo, sin impedir
   que el Cajero registre el pago si decide continuar sin adjuntar evidencia.

## Resultado esperado

Todas las verificaciones anteriores pasan sin necesidad de inspeccionar la base de datos ni el
código: el cronograma, los indicadores de estado y el Número de Operación son visibles
directamente en la pantalla de la bandeja del Cajero.
