# REVIEW - BUGFIX-001

## Ciclo 1 - REVIEWER

**Veredicto: APPROVED**

### Comprobaciones ejecutadas por REVIEWER

| Comprobación | Resultado |
| --- | --- |
| `docker ps` | `Gaffer.Database Up ... 0.0.0.0:3306->3306/tcp` |
| `git status --short` / `git diff --stat` | 12 modificados (211+/37-) + 4 nuevos + `.github/plans/`; ningún fichero de `Gaffer.Api.Client` |
| `dotnet build gaffer-api.sln --no-incremental -v q` | Compilación correcta, 0 errores, 85 advertencias (mismo número que la línea base de BUILDER). Las advertencias en ficheros tocados son todas de líneas preexistentes (CS0414/CS8625/CS8618/CS8604/CS8603). |
| `dotnet test gaffer-api.sln --no-build --verbosity quiet` (x2) | 127/127 superados, 0 con error, 0 omitidos (las dos veces) |
| Filtro paso 01 `SqlPlayerRepositoryShould\|SearchPlayers\|PlayerFeaturesShould` | 39/39 |
| Filtro paso 02 `DatabaseStartup` | 8/8 |
| Filtro paso 03 `PlayerFeaturesShould\|ExceptionToHttpResponseConverters` | 9/9 |
| Filtro paso 04 `DatabaseStartup\|SqlConnectionProvider` | 13/13 |

Red → green: REVIEWER no puede revertir código de producto, así que la evidencia red es la que BUILDER dejó en `STATUS.md` (comando, filtro y mensaje de fallo por paso y por fase). Es coherente con el código de `HEAD`: interpolación en `AppendFilter`, instancia única capturada en `AddScoped`, falta la clave `PlayerNotFoundException` y `SELECT (0) FROM Parameters` con `.Result`. Green verificado de forma independiente arriba.

### Criterios de aceptación

| ID | Evidencia | Estado |
| --- | --- | --- |
| AC1.1 | `SearchPlayersEndpointShould.ReturnNoPlayersWhenTheFilterValueTriesToInjectSql` (HTTP 200, 0 jugadores) + unitario equivalente | Demostrado |
| AC1.2 | `ReturnBadRequestWhenTheFilterFieldIsNotPermitted` x4 (Status, Title, Detail, Instance, `Extensions.ErrorTranslationKey`); unitario `ThrowWithoutQueryingWhenTheFilterFieldIsNotPermitted` x4 con `DidNotReceiveWithAnyArgs().QueryAsync` (no se ejecuta SQL); conversor en `ExceptionToHttpResponseConvertersShould` | Demostrado |
| AC1.3 | `FindAPlayerWhoseLastNameContainsASingleQuote` (HTTP) + `FindAPlayerWhenTheFilterValueContainsASingleQuote` (repo) | Demostrado |
| AC1.4 | Tests existentes `SqlPlayerRepositoryShould` y `SearchPlayersFeatureShould` en verde dentro de 127/127 | Demostrado |
| AC1.5 | Diff: `QueryAsync<Player>(query, parameters)` con `DynamicParameters`; ya no se pasa el `CancellationToken` | Demostrado (diff) |
| AC1.6 | Diff: diccionario `{Id, FirstName, LastName}` con `StringComparer.OrdinalIgnoreCase`; el SQL usa el nombre canónico entre backticks. Tests `MatchFilterFieldNamesIgnoringCase` (HTTP y repo) | Demostrado |
| AC1.7 | Solo se mapea el tipo exacto `PlayerSearchFilterNotPermittedException` (el middleware busca por tipo exacto en un único diccionario); `NotMapGenericArgumentExceptionsSoFacilitySearchKeepsItsBehaviour`; los tests de 400 existentes siguen en verde | Demostrado |
| AC2.1 | `DatabaseStartupShould.ResolveADifferentConnectionProviderPerScopeAndTheSameOneWithinAScope` | Demostrado |
| AC2.2 | `IsolateAnUncommittedTransactionOfOneScopeFromAnotherScope` (B ve 0 durante la transacción de A; 0 tras el rollback). Determinista en las 2 ejecuciones completas de REVIEWER y las 8 de BUILDER | Demostrado |
| AC2.3 | `ResolveADifferentConnectionProviderPerScopeInTheTestServer`; `ClearDatabase` sigue funcionando (toda la suite de aceptación en verde) | Demostrado |
| AC2.4 | `SqlConnectionProviderShould`: liberación por scope con conexión cerrada, scope liberado con conexión abierta dentro de una transacción, doble liberación (async + sync) sin excepción | Demostrado |
| AC2.5 | 127/127 | Demostrado |
| AC3.1 / AC3.2 | `PlayerFeaturesShould.ReturnNotFoundWhenGettingAnUnknownPlayer` (404, `Status`, `Instance`, `Title`/`Detail` nulos, sin `ErrorTranslationKey`) | Demostrado |
| AC3.2b | Diff: método nuevo `BuildNotFoundProblemDetails()`; los auxiliares existentes no cambian. `KeepTheTranslationKeysOfTheExistingPlayerNotFoundConversions` | Demostrado |
| AC3.3 / AC3.4 | Tests existentes de delete/update/GET en verde | Demostrado |
| AC4.1 | `ReportAReachableDatabaseWithoutDependingOnItsSchema` (`SELECT 1;`, conexión cerrada después) | Demostrado |
| AC4.2 | `ReportAnUnreachableDatabaseWithoutThrowing` (Port=1) | Demostrado |
| AC4.3 | AC2.1 a través de `ConfigureDatabase` + `NotLogTheFallbackWhenTheDatabaseIsReachable` | Demostrado |
| AC4.4 | `FallBackToTheDummyProviderWithoutThrowingWhenTheDatabaseIsUnreachable` | Demostrado |
| AC4.4b | `LogOnceThatTheDummyProviderIsUsedWhenTheDatabaseIsUnreachable` (una sola entrada `Warning`, sin `Pwd=`) + `NotLogTheFallbackWhenTheDatabaseIsReachable` | Demostrado |
| AC4.5 | `KeepRegisteringTheDummyProviderWhenTheDatabaseIsDisabledOrNotConfigured` x2 | Demostrado |
| AC4.6 | 127/127 | Demostrado |
| AC5.1 | Build con 0 errores y 85 advertencias, el mismo número que la línea base; ninguna advertencia en líneas nuevas | Demostrado |
| AC5.2 | 127 = 95 de la línea base + 32 nuevos, 0 omitidos, contra el Docker local | Demostrado |
| AC5.3 | Ficheros dentro del alcance salvo la desviación mínima anotada abajo; nada de `Gaffer.Api.Client` | Demostrado (con desviación aceptada) |
| AC5.4 | `STATUS.md` tiene red/green por paso y fase | Demostrado |

### Hallazgos

| # | Severidad | Hallazgo | Decisión |
| --- | --- | --- | --- |
| F1 | Informativo | Desviación de alcance: `Acceptance/Features_Refactor/PlayerFeatureShould.cs` y `Unit/Repositories/SqlFacilityRepositoryShould.cs` (+1 línea cada uno, `DisposeAsync` en `[TearDown]`). `NUnit1032` es de severidad error en NUnit.Analyzers cuando un campo `IDisposable` creado en `[SetUp]` no se libera, así que es una consecuencia directa del paso 02 (Q4). El cambio es mínimo y no altera el comportamiento de los tests. | Aceptada |
| F2 | Informativo | Superficie pública nueva `SqlConnectionProvider.IsDisposed` y `ConnectionState` (solo lectura). No está registrada en ningún contrato HTTP. Sirve para verificar AC2.4/AC4.1. | Aceptada |
| F3 | Informativo | `Startup` crea un `LoggerFactory` de arranque (Console + sección `Logging`) y lo libera tras `ConfigureDatabase`. Encaja con N2 (el plan deja el mecanismo a BUILDER); los paquetes vienen del shared framework de ASP.NET Core y compila. | Aceptada |
| F4 | Menor, no bloqueante | `TestConnection` solo captura `DbException`, `TimeoutException` y `SocketException`. Una cadena de conexión mal formada sigue fallando en el constructor de `MySqlConnection` al arrancar. Es un error de configuración y ya ocurría antes; queda fuera de Q5 (conectividad). | Sin acción |
| F5 | Menor, no bloqueante | Los helpers `ClearPlayers` de `DatabaseStartupShould` y `SearchPlayersEndpointShould` crean `SqlConnectionProvider` que nunca se liberan (solo en tests). La cadena de conexión alcanzable está repetida como literal en `DatabaseStartupShould`. | Posible limpieza futura |
| F6 | Menor, no bloqueante | Semántica con `Value` nulo: antes `IN ('')`, ahora `= NULL`, que nunca coincide. `SearchCriteria.Value` no admite nulos, así que es un caso límite sin test. | Sin acción |
| F7 | Informativo | Se sigue importando `using DocumentFormat.OpenXml.Bibliography;` sin usarlo en `SqlPlayerRepository.cs`. Es preexistente y el plan lo deja fuera de alcance explícitamente. | Sin acción |

- DI: todos los repositorios y handlers son `Scoped`, así que no hay dependencia cautiva del proveedor por scope. `TestFixture` refleja el registro por scope en el mismo incremento; no hay handlers nuevos que registrar.
- Seguridad: lista blanca más parámetros de Dapper; el nombre de columna del SQL sale de la lista, nunca de la entrada. El log de fallback no incluye la cadena de conexión.
- No verificado (opcional según el plan): arrancar `Gaffer.Api` en Development con la BD parada. No bloquea la aprobación.
