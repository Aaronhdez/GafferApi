# Paso 01 (ciclo 1) - TreatWarningsAsErrors, `.gitattributes` y cierre de la conexión con `try/finally`

Decisiones del operador (literales): TreatWarningsAsErrors "Solo Gaffer.Api"; conexión tras un fallo "Sí, try/finally"; `.gitattributes` entra en este plan (OPT-015). Preguntas relacionadas: Q1, Q2 (no bloquean; ver `00`).

## Objetivo

1. `Gaffer.Api` compila con `TreatWarningsAsErrors = true` (solo ese proyecto).
2. `.gitattributes` evita que un fichero editado con CRLF se versione sin normalizar (OPT-015, REVIEW F1 de TECHDEBT-003), según Q1/Q2.
3. `SqlConnectionProvider` cierra la conexión que abrió también cuando la operación lanza (fuera de transacción), en `QueryScalarAsync`, `QueryAsync` y `ExecuteAsync` (y, por compartir `RunOnConnectionAsync`, en `ClearAsync`/`CreateAsync`). Cierra la deuda Q1 de TECHDEBT-003, incluida la variante "operación cancelada tras abrir".

## Alcance de ficheros

| Fichero | Cambio |
| --- | --- |
| `Gaffer.Api/Gaffer.Api.csproj` | `TreatWarningsAsErrors` → `true`. Nada más. |
| `.gitattributes` | Según Q1 (sin respuesta: sin reglas nuevas). |
| Ficheros con `i/crlf` o sin atributo `text` | Solo según Q2 (sin respuesta: solo se listan en `STATUS.md`). Nunca `Gaffer.Api.Client/GafferApiClient.cs` ni `OpenApiSpecs.json`. |
| `Gaffer.Api/Shared/ConnectionProviders/SqlConnectionProvider.cs` | `RunOnConnectionAsync` con `try/finally`; actualizar su `<summary>` (retirar la nota de deuda Q1). |
| `Gaffer.Api.Tests/Unit/ConnectionProviders/SqlConnectionProviderShould.cs` | Tests nuevos. |

Fuera: otros `.csproj` (`Gaffer.Api.Tests`, `Gaffer.Api.Migrations`, `Gaffer.Api.Client`) y `Directory.Build.props`.

## Criterios de aceptación

| ID | Criterio |
| --- | --- |
| AC1.1 | `Gaffer.Api.csproj` declara `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`; ningún otro `.csproj` ni `Directory.Build.props` cambia (`git diff --stat`). |
| AC1.2 | Build limpio: `dotnet build gaffer-api.sln --no-incremental` → 0 warnings, 0 errores. |
| AC1.3 | El ajuste tiene efecto (mutación, se revierte): un warning introducido temporalmente en un fichero de `Gaffer.Api` (p. ej. variable local sin usar, CS0168/CS0219) hace fallar el build con `error CS....`; el mismo warning en `Gaffer.Api.Tests` sigue siendo solo warning. Tras revertir, `git diff` de esos ficheros vacío. |
| AC1.4 | `.gitattributes` contiene las reglas decididas en Q1 (o sin cambios si no hay respuesta). `git ls-files --eol` antes/después registrado en `STATUS.md`: lista de ficheros con `i/crlf` o sin atributo `text`, y, si Q2 = a), que los ficheros de producto/tests/config quedan con `w/lf` en el árbol. |
| AC1.5 | Aceptación (proveedor contra la BD de Docker): tras una `QueryAsync` con SQL inválido que lanza, `ConnectionState` es `Closed`. Red hoy: `Open`. |
| AC1.6 | Igual que AC1.5 para `QueryScalarAsync` y `ExecuteAsync` (p. ej. SQL inválido o violación de restricción). |
| AC1.7 | Operación cancelada **después** de abrir (token cancelado durante la operación, o doble que cancela tras `OpenAsync`) → la excepción se propaga y `ConnectionState` es `Closed`. Si no se puede provocar de forma determinista con el driver actual, BUILDER lo registra y deja la cobertura a AC1.5/AC1.6 (el paso 03 vuelve a comprobarlo con `MySqlConnector`). |
| AC1.8 | Dentro de una transacción ambiente, una operación que falla **no** cierra la conexión (la transacción es su dueña): la transacción hace rollback y el cierre sigue en su `finally`. Tests existentes `RollBackAndPropagateTheFailureOfATransaction`, `RunANestedTransactionInsideTheOuterOne` y `RollBackATransactionWhoseOperationIsCancelled` en verde sin cambiar aserciones. |
| AC1.9 | La excepción original se propaga sin envolver (mismo tipo y mensaje que hoy). |
| AC1.10 | Regresión: `SqlConnectionProviderShould`, `DatabaseStartupShould` y la suite completa en verde; recuento antes/después. `grep -n "Q1" Gaffer.Api/Shared/ConnectionProviders/SqlConnectionProvider.cs` sin la nota de deuda. |

## Doble bucle

1. **Aceptación**: AC1.5-AC1.7 en `SqlConnectionProviderShould` (red: `Open`) → `try/finally` → green.
2. **Unitario**: AC1.8-AC1.9 (caracterización: verde antes y después). AC1.1-AC1.3 como comprobación de build (red = mutación AC1.3 antes del cambio del `.csproj` compila con warning; green = falla con error).
3. **Refactor**: `<summary>`; `.gitattributes` y EOL (AC1.4). Revalidar build `--no-incremental` y suite completa.

## Cómo demostrarlo

- `dotnet test gaffer-api.sln --filter "FullyQualifiedName~SqlConnectionProvider|FullyQualifiedName~DatabaseStartup"`: red y green en `STATUS.md` (criterio: código de salida del proceso; `grep` solo para el detalle de fallos).
- `dotnet build gaffer-api.sln --no-incremental -v q`: solo la línea de resumen (0/0) y la del error de la mutación AC1.3.
- `git diff --stat -- '*.csproj' Directory.Build.props`; `git ls-files --eol` filtrado a `i/crlf` y a ficheros sin `attr/text`.
- Suite completa con recuento (base: 269/269 según el operador; BUILDER la revalida).
