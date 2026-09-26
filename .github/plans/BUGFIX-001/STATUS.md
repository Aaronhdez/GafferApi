# STATUS - BUGFIX-001

| Campo | Valor |
| --- | --- |
| Estado | DONE |
| Ciclo | 1 |
| Pasos completados | 01, 02, 03, 04, 05 |
| Bloqueos | Ninguno. Docker `Gaffer.Database` (3306, `GafferDB_Test`) disponible; ningún test omitido. |

## Tiempos por agente

| Agente | Ciclo | Inicio (UTC) | Fin (UTC) |
| --- | --- | --- | --- |
| PLANNER | 0 | n/a | n/a |
| BUILDER | 1 | 2026-09-26T10:22:59Z | 2026-09-26T10:35:05Z |
| REVIEWER | 1 | 2026-09-26T10:36:17Z | 2026-09-26T10:38:50Z |

## Prerrequisitos (BUILDER, ciclo 1)

| Comprobación | Resultado |
| --- | --- |
| `docker ps` | `Gaffer.Database Up ... 0.0.0.0:3306->3306/tcp` |
| `dotnet --version` | `10.0.302` |
| Tablas de `GafferDB_Test` (`SHOW TABLES`) | `Adresses, Facilities, Facilities_Addresses, Facilities_Stands, Players, Stands, VersionInfo` (sin `Parameters`: confirma bug 4) |
| Línea base `dotnet build gaffer-api.sln --no-incremental -v q` | 0 errores, 85 advertencias (CS y NU1901/NU1903 preexistentes) |
| Línea base `dotnet test gaffer-api.sln --verbosity quiet` | Correctas: 95/95, 0 omitidos |

## Comandos y resultados

Filtros abreviados: `dotnet test gaffer-api.sln --filter "<filtro>"` (salida `--verbosity quiet` o `--logger "console;verbosity=normal"`).

| Ciclo | Paso | Fase | Comando / filtro | Resultado |
| --- | --- | --- | --- | --- |
| 1 | 01 | Aceptación red | `FullyQualifiedName~SearchPlayersEndpointShould` | 6/7 con error: AC1.1 `Expected players to be empty` (inyección devuelve filas); AC1.2 x4 `expected 400 but found 200`; AC1.3 `expected 200 but found 500`. `MatchFilterFieldNamesIgnoringCase` (AC1.6) en verde: guarda de regresión del comportamiento actual. |
| 1 | 01 | Aceptación green | mismo filtro | 7/7 correctas |
| 1 | 01 | Unitario red | `FullyQualifiedName~WhenFilteringPlayersWithUntrustedInput\|FullyQualifiedName~ExceptionToHttpResponseConvertersShould` con `SqlPlayerRepository.cs` y `ExceptionToHttpResponseConverters.cs` restaurados temporalmente a `HEAD` (tipo de excepción nuevo mantenido para compilar) | 7/9 con error (inyección, 4 campos no permitidos, comilla, conversor 400). En verde las 2 guardas: minúsculas (AC1.6) y `ArgumentException` no mapeada (AC1.7). Después se restauró la implementación nueva. |
| 1 | 01 | Unitario green | mismo filtro, implementación nueva | 9/9 correctas |
| 1 | 01 | Refactor | `FullyQualifiedName~SqlPlayerRepositoryShould\|FullyQualifiedName~SearchPlayers\|FullyQualifiedName~PlayerFeaturesShould\|FullyQualifiedName~ExceptionToHttpResponseConverters` + suite completa | 40/40; suite 111/111 |
| 1 | 02 | Aceptación red | `FullyQualifiedName~DatabaseStartup` | 3/3 con error: AC2.1 y AC2.2 `AggregateException (Table 'GafferDB_Test.Parameters' doesn't exist)` (bug 4 impide construir el contenedor); AC2.3 falla porque ambos scopes del `TestServer` devuelven la misma instancia. |
| 1 | 02 | Unitario red | `FullyQualifiedName~SqlConnectionProviderShould` con `DisposeAsync`/`Dispose` vacíos (stub) | 2/3 con error: `Expected provider.IsDisposed to be True, but found False` (scope liberado y scope con conexión abierta). Doble liberación en verde. |
| 1 | 02 | Green parcial | `FullyQualifiedName~SqlConnectionProviderShould\|FullyQualifiedName~DatabaseStartup` | 4/6: AC2.3 y AC2.4 en verde; AC2.1/AC2.2 siguen en rojo solo por bug 4 (`Parameters`). Suite 115/117. |
| 1 | 02 | Green final | tras el paso 04 (`TestConnection` corregido) | AC2.1 y AC2.2 en verde (ver paso 04) |
| 1 | 03 | Aceptación + unitario red | `FullyQualifiedName~PlayerFeaturesShould\|FullyQualifiedName~ExceptionToHttpResponseConverters` | 2/9 con error: `ReturnNotFoundWhenGettingAnUnknownPlayer` `expected 404 but found 500`; `ConvertAnUnknownPlayerIntoABareNotFound` `PlayerConverters` sin la clave `PlayerNotFoundException`. |
| 1 | 03 | Green | mismo filtro | 9/9 correctas |
| 1 | 04 | Aceptación + unitario red | `FullyQualifiedName~DatabaseStartup\|FullyQualifiedName~SqlConnectionProvider` | 5/11 con error: AC4.4 `found (aggregated) MySqlException: Unable to connect`; AC4.2 `found MySqlException: Unable to connect`; AC4.1 `MySqlException: Table 'GafferDB_Test.Parameters' doesn't exist`; AC2.1/AC2.2 `AggregateException (...Parameters...)`. AC4.5 (2 casos) en verde: guarda del comportamiento actual. |
| 1 | 04 | Green | mismo filtro | 11/11 correctas (incluye AC2.1/AC2.2) |
| 1 | 04 | Log red (compilación) | `dotnet build Gaffer.Api.Tests -v q` | `error CS1501: Ninguna sobrecarga para el método 'ConfigureDatabase' toma 2 argumentos` |
| 1 | 04 | Log red (comportamiento) | `FullyQualifiedName~DatabaseStartup` con parámetro `ILogger?` sin escribir log | 1/8 con error: `Expected ... logger.Entries to contain a single item, but the collection is empty` |
| 1 | 04 | Log green | `FullyQualifiedName~DatabaseStartup\|FullyQualifiedName~SqlConnectionProvider` | 13/13 correctas |
| 1 | 05 | Build | `dotnet build gaffer-api.sln --no-incremental -v q` | 0 errores, 85 advertencias; conjunto de advertencias CS por fichero idéntico a la línea base (diff vacío): ninguna advertencia nueva |
| 1 | 05 | Suite completa | `dotnet test gaffer-api.sln --no-build --verbosity quiet` | Correctas: 127/127 (95 línea base + 32 nuevos), 0 con error, 0 omitidos |
| 1 | 05 | Filtros de demostración | paso 01 / 02 / 03 / 04 (filtros de cada plan) | 39/39, 8/8, 9/9, 13/13 |
| 1 | 05 | `git status --short` / `git diff --stat` | | 12 modificados (211+/37-) + 4 nuevos (ver abajo); ningún fichero de `Gaffer.Api.Client` |

## Tests nuevos (32)

| Paso | Fichero | Tests |
| --- | --- | --- |
| 01 | `Gaffer.Api.Tests/Acceptance/SearchPlayersEndpointShould.cs` (nuevo) | AC1.1 inyección → 200 y 0; AC1.2 x4 campos no permitidos → 400 + cuerpo (`Status`, `Title`, `Detail`, `Instance`, `Extensions.ErrorTranslationKey`); AC1.3 `O'Neill`; AC1.6 `firstname` |
| 01 | `Gaffer.Api.Tests/Unit/Repositories/SqlPlayerRepositoryShould.cs` (`WhenFilteringPlayersWithUntrustedInput`) | inyección → 0; x4 campo no permitido → excepción y `DidNotReceive QueryAsync` (no se ejecuta SQL); minúsculas; comilla |
| 01/03 | `Gaffer.Api.Tests/Unit/Exceptions/ExceptionToHttpResponseConvertersShould.cs` (nuevo) | conversor 400 (AC1.2); `ArgumentException` no mapeada (AC1.7); 404 mínimo (AC3.2); claves de delete/update sin cambios (AC3.2b) |
| 02/04 | `Gaffer.Api.Tests/Integration/DatabaseStartupShould.cs` (nuevo) | AC2.1, AC2.2, AC2.3 (TestServer), AC4.4, AC4.4b (log único / sin log con BD alcanzable), AC4.5 x2 |
| 02/04 | `Gaffer.Api.Tests/Unit/ConnectionProviders/SqlConnectionProviderShould.cs` (nuevo) | AC2.4 x3 (liberación por scope, con conexión abierta, doble liberación); AC4.1; AC4.2 |
| 03 | `Gaffer.Api.Tests/Acceptance/PlayerFeaturesShould.cs` | `ReturnNotFoundWhenGettingAnUnknownPlayer` (AC3.1, AC3.2) |

## Decisiones y notas para REVIEWER

| Tema | Decisión |
| --- | --- |
| N1 (mayúsculas) | Lista blanca `Id, FirstName, LastName` con `StringComparer.OrdinalIgnoreCase`; el SQL usa el nombre canónico de la lista, nunca el texto del cliente. |
| Excepción de Players | `PlayerSearchFilterNotPermittedException` en `IPlayerRepository.cs`; solo ese tipo se mapea a 400, así que Facilities no cambia (AC1.7). |
| Formato del cuerpo | El middleware serializa `ProblemDetails` con Newtonsoft: claves en PascalCase y extensión dentro de `Extensions` (formato ya existente, no cambiado). En el 404, `Title`/`Detail` se serializan como `null` y `Extensions` va vacío. |
| AC1.5 | `Get` pasa `DynamicParameters` como `parameters`; el `CancellationToken` ya no se pasa a `QueryAsync`. |
| N2 (log) | Nivel `Warning`: la API sigue en modo degradado. Mensaje: `Database is not reachable at startup; falling back to DummyConnectionProvider.`, sin cadena de conexión (el test comprueba que no contiene `Pwd=`). Inyección: parámetro opcional `ILogger? logger = null` en `ConfigureDatabase`. `Startup` crea un `LoggerFactory` de arranque (Console + sección `Logging`) y lo libera al terminar: el pipeline de logging del host no se puede resolver mientras se registran servicios. |
| `.Result` | `ConfigureServices` sigue siendo síncrono: `Task.Run(probe.TestConnection).GetAwaiter().GetResult()`. Así se evita un interbloqueo por contexto de sincronización y no aparece `AggregateException`. `TestConnection` no lanza excepciones de conectividad (`DbException`, `TimeoutException`, `SocketException` → `false`). La sonda es una instancia aparte que se libera con `using`. |
| Superficie pública nueva | `SqlConnectionProvider.IsDisposed` y `SqlConnectionProvider.ConnectionState` (solo lectura), añadidos para demostrar AC2.4 y AC4.1. |
| Desviación de alcance | Implementar `IDisposable`/`IAsyncDisposable` provoca el error del analizador `NUnit1032` en 3 fixtures existentes que crean proveedores en `[SetUp]`. La corrección mínima fue liberarlos en su `[TearDown]` existente. Afecta a `SqlPlayerRepositoryShould.cs` (en alcance) y a dos ficheros fuera del alcance listado: `Acceptance/Features_Refactor/PlayerFeatureShould.cs` y `Unit/Repositories/SqlFacilityRepositoryShould.cs` (+1 línea cada uno). |
| Orden 02/04 | AC2.1/AC2.2 no pueden llegar a verde sin corregir bug 4: `ConfigureDatabase` lanza con la BD actual porque falta `Parameters`. Su green se demuestra tras el paso 04, como se ve en la tabla. |
| AC2.2 | El aislamiento con `REPEATABLE READ` fue determinista en todas las ejecuciones (8 en total). |
| TestFixture | La factoría por scope crea `new SqlConnectionProvider(testDatabase)`. `SqlConnectionProviderTest` sigue como instancia no registrada, solo para `ClearDatabase`, y se crea fuera del lambda de `ConfigureServices`. |
| Sin verificar | No se arrancó `Gaffer.Api` en Development con la BD parada (opcional y requiere autorización del operador). |

## Documentación modificada

| Agente | Ciclo | Inicio (UTC) | Fin (UTC) |
| --- | --- | --- | --- |
| DOCUMENTER | 1 | 2026-09-26T11:05:00Z | 2026-09-26T11:20:00Z |

| Tipo | Documento | Acción |
| --- | --- | --- |
| Funcional | `doc/features/player-search.md` | Creado |
| Funcional | `doc/features/database-connection-reliability.md` | Creado |
| Técnico | `doc/features/technical/player-search-and-persistence.md` | Creado |
| Técnico | `doc/features/technical/database-connection-lifecycle.md` | Creado |

Impacto indirecto evaluado: `.github/doc/features/facility-crud.md` y `facility-create.md` no requieren cambios (el diff no altera el comportamiento de Facilities; `PlayerConverters` solo añade tipos exactos de Player, y `SqlFacilityRepository`/`ExceptionToHttpResponseConverters` para Facilities no cambian, ver AC1.7). No hubo cambios de paquetes ni de herramientas de test en el diff (`git diff --stat` no toca ningún `.csproj`), así que `.github/doc/common/*-stack*.md` no se actualiza.

## Revisión (REVIEWER, ciclo 1)

| Comando | Resultado |
| --- | --- |
| `dotnet build gaffer-api.sln --no-incremental -v q` | 0 errores, 85 advertencias |
| `dotnet test gaffer-api.sln --no-build --verbosity quiet` (x2) | 127/127, 0 omitidos |
| Filtros de los pasos 01/02/03/04 | 39/39, 8/8, 9/9, 13/13 |
| Veredicto | APPROVED (ver `REVIEW.md`) |
