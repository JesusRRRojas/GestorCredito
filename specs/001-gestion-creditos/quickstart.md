# Quickstart: Gestión de Créditos (Prospecto a Desembolso)

**Feature**: `001-gestion-creditos` | **Fecha**: 2026-09-24

Guía para levantar el entorno local y validar el flujo completo (Pantallas 1 a 6) contra los
criterios de aceptación del spec. No sustituye a `tasks.md` (que detalla la implementación).

## Prerrequisitos

- .NET 10 SDK instalado.
- Node.js + Angular CLI instalados.
- SQL Server local en ejecución (instancia accesible como `localhost`).
- Base de datos `dbGestorCredito` (creada por las migraciones de EF Core en el primer
  arranque, o manualmente si se prefiere).

## Configuración local (una sola vez)

La cadena de conexión **no** está en el repositorio (ver `research.md` §8, Principio VI de
la constitución). Configúrala localmente antes de ejecutar el backend, reemplazando
`<TU_PASSWORD_LOCAL>` por la contraseña real de tu instancia local de SQL Server (nunca la
escribas en un archivo versionado):

```bash
cd backend/GestorCredito.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Default" \
  "Server=localhost;Database=dbGestorCredito;User Id=sa;Password=<TU_PASSWORD_LOCAL>;TrustServerCertificate=True;"
```

(Alternativa: variable de entorno `ConnectionStrings__Default` con el mismo valor.)

## Levantar el backend

```bash
cd backend/GestorCredito.Api
dotnet ef database update   # aplica las migraciones Code-First
dotnet run
```

## Levantar el frontend

```bash
cd frontend
npm install
npm start
```

## Escenario de validación end-to-end (recorre las 4 historias de usuario del spec)

1. **Configuración (Historia 1)**
   - Como Administrador: crear un Tipo de Documento ("DNI") asociado a un Tipo de Persona
     ("Natural").
   - Crear un Producto con `RequiereRequisitos = false` (para probar el Fast-Track) y otro
     con `RequiereRequisitos = true` con al menos un Requisito ("Copia de DNI").
   - ✅ Verificación manual: ambos productos aparecen listados y editables (CA de Historia 1).

2. **Validación y simulación (Historia 2)**
   - Como Asesor: ingresar un Número de Documento nuevo y presionar "Buscar/Validar".
   - Si el resultado Mock es 4 o 5, repetir con el mismo documento hasta obtener 1–3 (según
     FR-008, no hay bloqueo).
   - Elegir el Producto sin requisitos, simular con Monto/Tasa/Plazo dentro de rango y
     presionar "Aceptar".
   - ✅ Verificación manual: el cronograma mostrado antes de "Aceptar" no persiste hasta
     presionar el botón (CA1); intentar un valor fuera de rango debe ser rechazado por la
     interfaz (CA3).

3. **Onboarding y Fast-Track (Historia 3, Caso B)**
   - Completar Nombres, Apellidos, Dirección y una Cuenta Bancaria de 20 dígitos.
   - ✅ Verificación manual: el prospecto pasa directo a "Desembolso" sin mostrar la pantalla
     de Requisitos (CA2).

4. **Onboarding, requisitos y aprobación (Historia 3, Caso A)**
   - Repetir la validación con otro documento hasta obtener Mock 1–3, elegir el Producto
     **con** requisitos, aceptar la simulación y completar el Onboarding.
   - Adjuntar el PDF del Requisito solicitado.
   - Como Aprobador: revisar el prospecto en la bandeja de "Aprobación", ver el PDF adjunto y
     marcarlo como "Observado".
   - ✅ Verificación manual: el prospecto vuelve a la bandeja del Asesor, quien puede editar
     los datos y reemplazar el PDF (CA4).
   - Reenviarlo y, como Aprobador, marcarlo esta vez como "Aprobado".

5. **Desembolso (Historia 4)**
   - Como Asesor: generar el PDF final de Desembolso para ambos prospectos (el de Fast-Track
     y el aprobado manualmente).
   - ✅ Verificación manual: la Cuenta Bancaria del PDF coincide con la ingresada en el
     Onboarding (CA5).
   - Cerrar el proceso de ambos prospectos.

6. **Historial**
   - Consultar `/api/prospectos/historial` (o la pantalla correspondiente) con el Número de
     Documento de un prospecto ya "Finalizado".
   - ✅ Verificación manual: aparece en el historial con su estado final y fecha de cierre.

## Notas de validación de reglas de negocio

- **Unicidad (FR-010)**: iniciar una validación para un documento con un prospecto activo
  debe bloquearse con un mensaje claro.
- **Redondeo (Sección 6 del spec / research.md §6)**: probar un Monto que no se divide
  exacto entre el Plazo (ej. S/1,000 entre 3 cuotas) y confirmar que la última cuota ajusta
  el saldo final a exactamente 0.00.
