# Quickstart: Precarga de Datos del Cliente en Onboarding

**Feature**: `005-precarga-datos-onboarding` | **Fecha**: 2026-09-26

Recorrido manual (Principio V de la constitución: verificable sin leer código ni consultar la
base de datos) para validar ambas historias de este feature.

## Prerequisitos

1. Backend corriendo (`dotnet run` en `backend/GestorCredito.Api`, perfil `http`,
   `http://localhost:5264`).
2. Frontend corriendo (`npm start` en `frontend/`, `http://localhost:4200`).
3. No requiere migración (sin cambios de esquema).
4. Un cliente (Tipo y Número de Documento) que ya tiene al menos un crédito anterior cerrado
   (Rechazado tras Aprobación, o Finalizado) con su Onboarding completo — puede reutilizarse
   cualquier cliente de prueba ya "Finalizado" de features anteriores. Para la Historia 2,
   idealmente un cliente cuyo crédito cerrado haya usado Cuenta Externa (para ver el caso más
   visible) y otro que haya usado Cuenta Interna.

## Historia 1 — Precarga de datos personales

1. Como Asesor, iniciar un nuevo prospecto para ese mismo cliente: Pantalla 1 (Validación) con
   su mismo Tipo y Número de Documento, avanzar hasta aceptar una Simulación.
2. Llegar a la pantalla de Onboarding del nuevo prospecto.
   **Verificar**: los campos Nombres, Apellidos y Dirección aparecen ya completados con los
   mismos datos de su crédito anterior, sin haberlos escrito.
3. Modificar la Dirección precargada por una distinta y guardar.
   **Verificar**: el Onboarding se guarda con la Dirección corregida (no la original
   precargada); al recargar la pantalla, se ve la Dirección corregida.
4. Repetir el paso 1-2 con un Tipo/Número de Documento que nunca tuvo un crédito antes.
   **Verificar**: el formulario aparece completamente vacío, igual que antes de este feature.
5. Con un prospecto que fue marcado "Observado" (ya tiene sus propios datos guardados),
   reingresar a su Onboarding desde la bandeja.
   **Verificar**: se ven sus propios datos ya guardados (no los de otro prospecto cerrado).

## Historia 2 — Precarga de la preferencia de cuenta de desembolso

1. Con un cliente cuyo crédito anterior cerrado usó Cuenta Externa, repetir los pasos 1-2 de la
   Historia 1 con un nuevo prospecto del mismo cliente.
   **Verificar**: el Tipo de Cuenta ya aparece en "Externa", con el Nombre del Banco y el CCI
   precargados.
2. Con un cliente cuyo crédito anterior cerrado usó Cuenta Interna (y esa cuenta sigue activa),
   repetir los pasos 1-2 de la Historia 1.
   **Verificar**: el Tipo de Cuenta ya aparece en "Interna", con esa cuenta específica ya
   seleccionada en la lista.
3. Cambiar la cuenta precargada por otra antes de guardar.
   **Verificar**: el Onboarding se guarda con la cuenta finalmente elegida, no la precargada.

## Resultado esperado

Todas las verificaciones anteriores pasan sin necesidad de inspeccionar la base de datos ni el
código: la precarga (o su ausencia) es visible directamente en la pantalla de Onboarding al
llegar a ella.
