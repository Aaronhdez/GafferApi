# REVIEW - TECHDEBT-003

## Ciclo 1 - Paso 01 (SqlConnectionProvider)

**Veredicto: APPROVED** (paso 01). Q1 sin respuesta: se aplica b); AC1.6 no aplica.

### Evidencia ejecutada por REVIEWER

| Comprobación | Comando | Resultado |
| --- | --- | --- |
| Git status | `git status --short` | M `SqlConnectionProvider.cs`, M `SqlConnectionProviderShould.cs`, M `STATUS.md`, ?? `usage/BUILDER.md`. Nada fuera de alcance; `Gaffer.Api.Client` no tocado; sin cambios de DI (`TestFixture.cs` no requiere registro) |
| Preflight | `docker ps` | `Gaffer.Database` Up |
| Red independiente (AC1.2) | `git archive HEAD` a scratchpad + test nuevo copiado; filtro `SqlConnectionProvider\|DatabaseStartup` | 17/18; falla `ReleaseTheConnectionOnlyWhenItsScopeIsDisposed` L121: `Expected releases to be greater than 0, but found 0.` (coincide con lo registrado por BUILDER) |
| Green (filtro) | `dotnet test gaffer-api.sln --filter "FullyQualifiedName~SqlConnectionProvider\|FullyQualifiedName~DatabaseStartup" --verbosity quiet` | 18/18 |
| Suite completa (AC1.7) | `dotnet test gaffer-api.sln --no-build --verbosity quiet` | 220/220, 0 omitidos (215 + 5) |
| Build | `dotnet build gaffer-api.sln --no-incremental` | 56 advertencias (57 en línea base), ninguna en `SqlConnectionProvider.cs` |
| AC1.5 | `grep -n "\.Open()"` / `grep -n "_sqlConnection\.Dispose"` | `.Open()`: 0. `_sqlConnection.Dispose()` solo L119 (dentro de `DisposeAsync`, L109) y L128 (dentro de `Dispose`, L122); ningún `_sqlConnection.DisposeAsync()` |

### Criterios

| ID | Estado | Evidencia |
| --- | --- | --- |
| AC1.1 | Demostrado | `CloseButNotReleaseTheConnectionAfterEachOperation`: 5 operaciones (incl. transacción), `Closed` e `IsDisposed == false` tras cada una. Caracterización (verde antes y después) |
| AC1.2 | Demostrado (con matiz) | Red→green reproducido. Medio: reflexión sobre `_sqlConnection` + `Component.Disposed`; no cambia API pública. Asserta 0 liberaciones tras operaciones y >0 tras `scope.DisposeAsync()`. Matiz: el red real es la mitad "se libera al fin del scope", no "se libera tras la primera operación", porque `MySqlConnection.DisposeAsync` (MySql.Data 26.7) no lanza `Disposed`; lo confirma el propio red (el código de HEAD llamaba `DisposeAsync` en cada operación y en el dispose del proveedor, y `releases` quedó en 0). La reintroducción de `DisposeAsync()` por operación solo la detecta AC1.5 (`grep`), no el test; riesgo residual aceptado y registrado |
| AC1.3 | Demostrado | El diff del test solo añade `using`, tests, `SetUp/TearDown` y helpers; aserciones previas intactas; en verde |
| AC1.4 | Demostrado | `MakeTheWorkOfACommittedTransactionVisible` (observador en otra conexión), `RollBackAndPropagateTheFailureOfATransaction`, `RunANestedTransactionInsideTheOuterOne` (rollback externo deshace la anidada) |
| AC1.5 | Demostrado | Ver tabla anterior |
| AC1.6 | No aplica | Q1 sin respuesta → b); deuda documentada en `<summary>` de `RunOnConnectionAsync` y en `STATUS.md` |
| AC1.7 | Demostrado | 220/220 |
| Refactor | Hecho | `RunOnConnectionAsync<T>` único; revalidado |

### Hallazgos

| ID | Severidad | Hallazgo | Acción |
| --- | --- | --- | --- |
| F1 | Importante, no bloqueante (higiene, sin efecto funcional) | `SqlConnectionProvider.cs` quedó en CRLF y termina en un `\r` suelto sin `\n` (159 CR / 158 LF). Git lo clasifica `w/-text` (`git ls-files --eol`), así que `core.autocrlf=true` no lo normaliza: `git diff` muestra reescritura completa (159+/182-) en vez de ~31+/54- (`--ignore-all-space`), y al commitear el fichero entraría en el repo con CRLF (el índice está en LF). | Operador, antes del commit: normalizar a LF (p. ej. `sed -i 's/\r$//' Gaffer.Api/Shared/ConnectionProviders/SqlConnectionProvider.cs`) y comprobar `git diff --stat`. Si se prefiere, BUILDER lo corrige en el ciclo 2 |
| F2 | Menor | `ClearAsync` ejecuta sin `_currentTransaction` (sin cambio respecto a HEAD); ahora, dentro de una transacción ambiente, ya no cierra la conexión (mejora). Sin acción | - |
| F3 | Menor | `QueryScalarAsync` usa `!` sobre `QueryFirstOrDefaultAsync<T>`: mismo comportamiento que antes (puede devolver `default`), solo silencia nulabilidad. Aceptable dentro de Q8/05 | - |

### Bloqueos

Ninguno.
