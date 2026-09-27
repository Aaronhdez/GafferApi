# REVIEW - TECHDEBT-004

## Ciclo 1 - Paso 01

Veredicto: **APPROVED** (paso 01). Estado del plan: `APPROVED` para el paso 01; los pasos 02-05 siguen pendientes (ver `STATUS.md`).

### Alcance revisado

| Comprobación | Resultado |
| --- | --- |
| `git status --short` | `M` `STATUS.md`, `SqlConnectionProviderShould.cs`, `Gaffer.Api.csproj`, `SqlConnectionProvider.cs`; `??` `usage/BUILDER.md`. Nada fuera del alcance del paso 01; cliente generado intacto (no verificado por norma). |
| Diff completo | `Gaffer.Api.csproj`: 1 línea (`TreatWarningsAsErrors` `false`→`true`). `SqlConnectionProvider.cs`: `RunOnConnectionAsync` con `try/finally` (cierra solo si `ownsConnection`) y `<summary>` sin nota de deuda Q1. Tests: 5 nuevos + helpers `AssertPropagatesTheDriverFailure`, `UnderlyingDbConnectionOf`. |
| `TestFixture.cs` | No aplica: no hay handlers ni registros DI nuevos. |
| Preflight | `docker ps`: `Gaffer.Database Up 6 hours`. |

### Evidencia propia (REVIEWER)

| Comando | Resultado |
| --- | --- |
| `dotnet build gaffer-api.sln --no-incremental -v q` | 0 Advertencia(s), 0 Errores |
| `dotnet msbuild <proj> -getProperty:TreatWarningsAsErrors` | `Gaffer.Api` true; `Gaffer.Api.Tests`, `Gaffer.Api.Migrations`, `Gaffer.Api.Client` false |
| `dotnet test ... --filter "FullyQualifiedName~SqlConnectionProvider\|FullyQualifiedName~DatabaseStartup"` | exit 0; 27/27 |
| `dotnet test gaffer-api.sln --no-build --verbosity quiet` | exit 0; 274/274 (base 269 + 5) |
| Red independiente: copia `git archive HEAD` en scratchpad + solo el fichero de tests nuevo; mismo filtro | exit 1; 4 fallos / 23 / 27: `CloseTheConnectionWhenAQueryFails`, `...AScalarQueryFails`, `...AnExecuteViolatesAConstraint`, `...AnOperationIsCancelledAfterOpeningIt`, todos `Expected provider.ConnectionState to be Closed, but found Open` (las aserciones de excepción previas pasan) |
| Mutación AC1.3 en copia scratchpad (repo no tocado) | Mutación CS0219 en `Gaffer.Api/Program.cs` + en test: `error CS0219` en `Gaffer.Api.csproj`, 1 error. Solo la del test: 1 advertencia, 0 errores (`Gaffer.Api.Tests.csproj`). Copias borradas. |
| `git diff --stat -- '*.csproj' Directory.Build.props .gitattributes` | Solo `Gaffer.Api/Gaffer.Api.csproj \| 2 +-` |
| `grep -n "Q1" SqlConnectionProvider.cs` | Sin coincidencias (exit 1) |
| `git ls-files --eol \| grep -c i/crlf` | 0 |

### Criterios

| AC | Estado | Evidencia |
| --- | --- | --- |
| AC1.1 | Demostrado | diff `--stat` y `-getProperty` arriba. |
| AC1.2 | Demostrado | Build `--no-incremental` 0/0. |
| AC1.3 | Demostrado | BUILDER (red warning → green error, `STATUS.md`) + mutación propia en copia aislada. |
| AC1.4 | Demostrado | Q1/Q2 sin respuesta → `.gitattributes` sin cambios y solo listado en `STATUS.md`; `i/crlf` = 0. |
| AC1.5 | Demostrado | Red→green reproducido (`CloseTheConnectionWhenAQueryFails`). |
| AC1.6 | Demostrado | Red→green reproducido (scalar con SQL inválido; execute con PK duplicada). |
| AC1.7 | Demostrado | Determinista vía `DbConnection.StateChange` (cancela al pasar a `Open`); red→green reproducido; propaga `OperationCanceledException`. |
| AC1.8 | Demostrado | `LeaveTheConnectionToTheTransactionWhenAnOperationInsideItFails` (estado `Open` tras el fallo, rollback, `Closed` al final); tests de transacción existentes sin cambios en verde. |
| AC1.9 | Demostrado | `DbException` de tipo `MySqlException` con mensaje del driver; pasa en HEAD (red) y tras el cambio. |
| AC1.10 | Demostrado | 27/27, 274/274, `grep Q1` vacío. |

### Hallazgos (ninguno bloqueante)

| ID | Severidad | Hallazgo |
| --- | --- | --- |
| R1-1 | Menor / informativo | `Gaffer.Api.csproj` quedó `w/lf` en el árbol tras `sed -i` (índice `i/lf`, sin atributo `text`, `core.autocrlf=true`, BOM conservado). El diff es de 1 línea y el blob versionado sigue en LF: sin efecto en el commit. Solo cosmético en el árbol de trabajo; el operador puede hacer `git checkout -- Gaffer.Api/Gaffer.Api.csproj` tras commitear si quiere CRLF local. |
| R1-2 | Observación | Si `CloseAsync` lanzara en el `finally`, enmascararía la excepción original (AC1.9). Riesgo teórico con MySql.Data; revisar en el paso 03 (`MySqlConnector`). |
| R1-3 | Observación (preexistente, fuera de alcance) | `OpenIfNeededAsync` devuelve `true` también si la conexión ya estaba abierta fuera de transacción; no es regresión de este paso. |
