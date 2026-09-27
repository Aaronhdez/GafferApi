# STATUS - TECHDEBT-004

| Campo | Valor |
| --- | --- |
| Estado | APPROVED |
| Ciclo | 1 |
| Pasos completados | 01 (APPROVED por REVIEWER, ciclo 1) |
| Pasos bloqueados | 02 hasta confirmar Q3; 04 hasta Q9, Q10, Q12; 05 hasta Q17, Q18, Q19 (ver `00-overview-and-open-questions.md`) |
| Siguiente | OPTIMIZER ciclo 1; checkpoint humano (ciclo 2 = paso 02) |
| Bloqueos | Ninguno |

## Decisiones aplicadas en el ciclo 1

Operador: "Apruebo el plan" (sin responder preguntas). Se aplican los valores por defecto de la columna "Bloquea":

| Pregunta | Valor aplicado | Efecto |
| --- | --- | --- |
| Q1 | a) (sin respuesta) | `.gitattributes` sin reglas nuevas (sigue solo `*.cs text eol=lf`). |
| Q2 | b) (sin respuesta) | Solo se listan los ficheros; no se convierte ningún EOL. Renormalizar queda para el operador. |
| Q8 | no es c) | Sin renumeración. |

## Secuenciación (un paso = un ciclo)

En el ciclo N, BUILDER implementa solo el paso N (más las correcciones de REVIEWER del ciclo anterior, si las hay) y REVIEWER solo aprueba el paso de ese ciclo. El ciclo 6 es la única reserva para `NEEDS_CHANGES`; cómo se combina una corrección con el paso siguiente es Q0.

| Ciclo | Paso previsto | Veredicto |
| --- | --- | --- |
| 1 | 01 - TreatWarningsAsErrors (`Gaffer.Api`) + `.gitattributes` + cierre de conexión con `try/finally` | APPROVED |
| 2 | 02 - Entorno desde variable de entorno (`Development` por defecto) | - |
| 3 | 03 - Migración a `MySqlConnector` + cancelación en curso | - |
| 4 | 04 - CRUD de gradas + lock en transacción | - |
| 5 | 05 - CI con GitHub Actions | - |
| 6 | Reserva | - |

## Tiempos por agente

| Agente | Ciclo | Inicio (UTC) | Fin (UTC) |
| --- | --- | --- | --- |
| PLANNER | 0 | n/a | n/a |
| BUILDER | 1 | 2026-09-27T17:30:59Z | 2026-09-27T17:34:46Z |
| REVIEWER | 1 | 2026-09-27T17:36:05Z | 2026-09-27T17:38:43Z |

## Prerrequisitos

| Comprobación | Resultado |
| --- | --- |
| Línea base (dato del operador, 2026-09-27, `main`) | `dotnet build --no-incremental`: 0 errores, 0 warnings. `dotnet test`: 269/269 contra Docker `Gaffer.Database` (MariaDB 12.3.2). BUILDER la revalida al inicio del ciclo 1. |
| Revalidación BUILDER ciclo 1 (rama `TECHDEBT-004`, árbol limpio) | `docker ps`: `Gaffer.Database mariadb Up 6 hours`. `dotnet build gaffer-api.sln --no-incremental -v q`: 0 advertencias, 0 errores. `dotnet test gaffer-api.sln --no-build --verbosity quiet`: 269/269, exit 0. |

## Comandos y resultados

| Ciclo | Paso | Fase | Comando / filtro | Resultado |
| --- | --- | --- | --- | --- |
| 1 | 01 | AC1.3 red (antes de cambiar el `.csproj`) | Mutación temporal: `var ac13Mutation = 1;` en `Gaffer.Api/Program.cs` L17 y `var ac13TestMutation = 1;` en `SqlConnectionProviderShould.cs` L15; `dotnet build gaffer-api.sln --no-incremental -v q` | Compilación correcta, 2 advertencias: `Program.cs(17,17): warning CS0219` y `SqlConnectionProviderShould.cs(15,13): warning CS0219` |
| 1 | 01 | AC1.1/AC1.3 green | `TreatWarningsAsErrors` `false` → `true` en `Gaffer.Api/Gaffer.Api.csproj`; mismo build con ambas mutaciones | ERROR al compilar, 1 error: `Gaffer.Api\Program.cs(17,17): error CS0219 ... [Gaffer.Api.csproj]` |
| 1 | 01 | AC1.3 (tests siguen en warning) | `git checkout -- Gaffer.Api/Program.cs` (solo la mutación); mismo build con la mutación de tests | Compilación correcta, 1 advertencia: `SqlConnectionProviderShould.cs(15,13): warning CS0219 ... [Gaffer.Api.Tests.csproj]`, 0 errores |
| 1 | 01 | AC1.3 reversión | `git checkout -- <fichero de test>` (antes de añadir tests); `git diff --stat -- Gaffer.Api/Program.cs Gaffer.Api.Tests/` | Vacío |
| 1 | 01 | Aceptación red (AC1.5-AC1.9) | 5 tests nuevos en `SqlConnectionProviderShould`; `dotnet test gaffer-api.sln --filter "FullyQualifiedName~SqlConnectionProvider\|FullyQualifiedName~DatabaseStartup" --verbosity quiet` | exit 1; 4 con error / 23 superados / 27. Los 4 (`CloseTheConnectionWhenAQueryFails`, `...AScalarQueryFails`, `...AnExecuteViolatesAConstraint`, `...AnOperationIsCancelledAfterOpeningIt`): `Expected provider.ConnectionState to be ConnectionState.Closed, but found ConnectionState.Open`. `LeaveTheConnectionToTheTransactionWhenAnOperationInsideItFails` (AC1.8, caracterización) ya en verde. Las aserciones de AC1.9 (`DbException` de tipo `MySqlException` con el mensaje del driver) pasan antes del fallo de estado. |
| 1 | 01 | Aceptación green | `RunOnConnectionAsync` con `try/finally` (cierra solo si abrió); `<summary>` sin nota de deuda Q1; mismo filtro | exit 0; 27/27 |
| 1 | 01 | Refactor / AC1.2 | `dotnet build gaffer-api.sln --no-incremental -v q` | 0 advertencias, 0 errores |
| 1 | 01 | AC1.10 suite completa | `dotnet test gaffer-api.sln --no-build --verbosity quiet` | exit 0; 274/274 (antes 269; +5 tests nuevos) |
| 1 | 01 | AC1.10 estático | `grep -n "Q1" Gaffer.Api/Shared/ConnectionProviders/SqlConnectionProvider.cs` | Sin coincidencias (exit 1) |
| 1 | 01 | AC1.1 | `git diff --stat -- '*.csproj' Directory.Build.props` | Solo `Gaffer.Api/Gaffer.Api.csproj \| 2 +-` |
| 1 | 01 | AC1.4 (Q1 = a), Q2 = b)) | `git ls-files --eol` filtrado | Ver "EOL (AC1.4)" abajo. `.gitattributes` sin cambios. |
| 1 | 01 | REVIEWER | Build `--no-incremental`; `-getProperty:TreatWarningsAsErrors`; filtro 27/27; suite 274/274; red reproducido en copia `git archive HEAD` (4/27 fallan con `Open`); mutación AC1.3 en copia aislada | Ver `REVIEW.md`: APPROVED |

## Paso 01 - evidencia por criterio

| AC | Evidencia |
| --- | --- |
| AC1.1 | `Gaffer.Api.csproj` L8 `true`; `git diff --stat` solo lista ese `.csproj`. |
| AC1.2 | Build `--no-incremental` final: 0/0. |
| AC1.3 | Red: warning CS0219 en ambos proyectos. Green: `error CS0219` en `Gaffer.Api`; en `Gaffer.Api.Tests` sigue `warning CS0219`. Mutaciones revertidas (`git diff` vacío en esos ficheros; el fichero de test solo contiene después los tests nuevos). |
| AC1.4 | Q1 sin respuesta → sin reglas nuevas; Q2 sin respuesta → solo listado (abajo). |
| AC1.5 | `CloseTheConnectionWhenAQueryFails` (`QueryAsync` con `SELEC 1;`): red `Open` → green `Closed`. |
| AC1.6 | `CloseTheConnectionWhenAScalarQueryFails` (SQL inválido) y `CloseTheConnectionWhenAnExecuteViolatesAConstraint` (PK duplicada, "Duplicate entry"): red `Open` → green `Closed`. |
| AC1.7 | Determinista: `CloseTheConnectionWhenAnOperationIsCancelledAfterOpeningIt` cancela el token en `DbConnection.StateChange` al pasar a `Open` (tras `OpenAsync`, antes del comando); `OperationCanceledException` propagada; red `Open` → green `Closed`. |
| AC1.8 | `LeaveTheConnectionToTheTransactionWhenAnOperationInsideItFails`: estado `Open` justo tras el fallo dentro de la transacción, rollback (fila no escrita) y `Closed` al final; verde antes y después. `RollBackAndPropagateTheFailureOfATransaction`, `RunANestedTransactionInsideTheOuterOne`, `RollBackATransactionWhoseOperationIsCancelled` sin cambios y en verde. |
| AC1.9 | Helper `AssertPropagatesTheDriverFailure`: `ThrowAsync<DbException>`, tipo exacto `MySqlException` (por nombre, agnóstico del driver para el paso 03) y mensaje del driver ("syntax" / "Duplicate entry"); verde antes y después. |
| AC1.10 | Filtro 27/27; suite 274/274; `grep Q1` sin coincidencias. |

## EOL (AC1.4, listado; Q2 = b): no se convierte nada)

| Comprobación | Resultado |
| --- | --- |
| Ficheros con `i/crlf` en el índice | Ninguno. |
| Ficheros sin atributo `text` | 186: 157 `.md` (todos `i/lf`), 8 `.json`, 6 `.gitkeep` (`i/none`), 4 `.csproj`, 2 `.ps1`, 1 `.sln`, 1 `.sh`, 1 `.props`, 1 `.mjs`, `.gitignore`, `.gitattributes`, `.dockerignore`, 2 `.code-workspace`. Todos `i/lf` salvo `.gitkeep`. |
| De ellos en `w/crlf` (árbol de trabajo) | `.dockerignore`, `.gitattributes`, `.gitignore`, `Directory.Build.props`, `gaffer-api.sln`, `global.json`, `.github/agent-harness/tools/usage-slice.mjs`, `Gaffer.Api.Client/Gaffer.Api.Client.csproj`, `Gaffer.Api.Client/OpenApiSpecs.json` (generado, fuera de BUILDER), `Gaffer.Api.Migrations/Gaffer.Api.Migrations.csproj`, `Gaffer.Api.Tests/Gaffer.Api.Tests.csproj`, `Gaffer.Api.Tests/testSettings.json`, `Gaffer.Api/Properties/launchSettings.json`, `Gaffer.Api/appsettings.json`, `Gaffer.Api/appsettings.Development.json`, `Gaffer.Api/nswag.json`. |
| `*.cs` con `attr eol=lf` pero `w/crlf` en el árbol | 41 ficheros (índice `i/lf`; `git status` limpio): p. ej. `Gaffer.Api/ProductContext.cs`, `Gaffer.Api/Startups/*.cs`, `Gaffer.Api.Tests/TestShared/*.cs`, `Gaffer.Api.Migrations/Migrations/M000*.cs`, `Gaffer.Api.Client/GafferApiClient.cs` (generado). Se corrige con un checkout del operador si lo desea. |
| Ficheros tocados en este ciclo | `SqlConnectionProvider.cs` y `SqlConnectionProviderShould.cs`: `i/lf w/lf attr/text eol=lf`. `Gaffer.Api.csproj`: `i/lf w/lf` (sin atributo; Git avisa "LF will be replaced by CRLF" por `core.autocrlf`; diff de 1 línea). |

## Documentación modificada

Pendiente (DOCUMENTER, tras `APPROVED` y aprobación del operador). Incluye `dev-stack.md`/`dev-stack-documentation.md` por el cambio de driver (Q7).
