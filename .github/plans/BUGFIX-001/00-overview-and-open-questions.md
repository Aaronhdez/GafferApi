# BUGFIX-001 - Resumen, alcance y preguntas abiertas

## Petición del operador

> "vale, genera la planificación para resolver los 4 bugs."

Origen: revisión de solo lectura del repositorio. PLANNER comprobó cada hallazgo en el código (ver "Verificación").

## Objetivo

Corregir 4 defectos sin cambiar el comportamiento que ya cubren los tests existentes. Línea base: `dotnet test gaffer-api.sln` → 95/95 en verde contra el contenedor Docker local `Gaffer.Database` (MariaDB, 3306, `GafferDB_Test`).

## Pasos

| Paso | Fichero | Bug | Depende de |
| --- | --- | --- | --- |
| 01 | `01-player-search-sql-injection.md` | Inyección SQL en la búsqueda de jugadores | Q1, Q2, Q7 (resueltas) |
| 02 | `02-scoped-database-connection.md` | Conexión/transacción de BD compartida entre peticiones | Q4 (resuelta) |
| 03 | `03-player-not-found-404.md` | `GET /players/{id}` desconocido devuelve 500 en vez de 404 | Q3 (resuelta) |
| 04 | `04-startup-connection-check.md` | La comprobación de conexión al arrancar puede tumbar la API | Paso 02, Q5 (resuelta) |
| 05 | `05-final-refactor-and-regression.md` | Refactor transversal y regresión completa | Pasos 01-04 |

Cada paso 01-04 sigue el doble bucle de `.github/doc/work-guidelines.md`: aceptación (red → green), unitario (red → green) y refactor con revalidación. BUILDER registra en `STATUS.md` la evidencia red y green de cada test nuevo (comando + resultado).

## Verificación de los hallazgos (PLANNER, solo lectura)

| Bug | Evidencia en el código | Confirmado |
| --- | --- | --- |
| 1 | `Gaffer.Api/Infrastructure/SqlPlayerRepository.cs` L32-36: `AppendFilter` interpola `fieldName` y `searchCriteria.Value` en el SQL. L13: `QueryAsync<Player>(baseQuery, cancellationToken)` pasa el `CancellationToken` como `parameters` de Dapper. Patrón de referencia: `SqlFacilityRepository.BuildFilters` (L40-61, lista blanca `AllowedFilterColumns` + `DynamicParameters`). | Sí |
| 2 | `Gaffer.Api/Startups/DatabaseStartup.cs` L27 y L33: una única instancia devuelta por la factoría `AddScoped`. `SqlConnectionProvider` guarda un único `MySqlConnection` y un campo mutable `_currentTransaction`. `Gaffer.Api.Tests/TestFixture.cs` L111-112 replica el patrón. | Sí |
| 3 | `PlayerController.GetBy` (L27) lanza `PlayerNotFoundException`, ausente en `ExceptionToHttpResponseConverters.PlayerConverters`; el middleware devuelve 500 para tipos no mapeados. `FacilityNotFoundException` está mapeada a 404 con `Error.Facilities.NotFound`. | Sí |
| 4 | `SqlConnectionProvider.TestConnection` (L18-26): `Open()` síncrono, `SELECT (0) FROM Parameters`, lector sin liberar, sin captura de excepciones. Las migraciones solo crean `Players`, `Facilities`, `Adresses`, `Stands`, `Facilities_Addresses`, `Facilities_Stands`. `DatabaseStartup.ConnectionIsUp` bloquea con `.Result`. `appsettings.Development.json` tiene `DatabaseConfig.Enabled: true`, así que la ruta se ejecuta en Development. | Sí (código); ver "No verificado" |

## No verificado

- No se ha arrancado la API: que el arranque falle hoy en Development se deduce del código, no de una ejecución. Si la BD del operador tuviera una tabla `Parameters` creada a mano, el fallo solo aparecería con la BD caída.
- No se ha ejecutado la línea base 95/95; la da por buena el ORCHESTRATOR. BUILDER debe volver a ejecutarla antes de cambiar nada.
- `TestFixture` no usa `DatabaseStartup.ConfigureDatabase`: registra su propio DI. Por eso los bugs 2 y 4 necesitan tests que ejerciten `DatabaseStartup` directamente (ver pasos 02 y 04).

## Preguntas resueltas (respuestas del operador)

| ID | Pregunta | Respuesta literal del operador | Significado aplicado | Estado |
| --- | --- | --- | --- | --- |
| Q1 | ¿Qué debe devolver `POST /api/v1/players/search` cuando un filtro usa un campo fuera de la lista blanca? | "400" | Respuesta HTTP 400 Bad Request. Contenido del cuerpo: ver Q7. | Resuelta |
| Q2 | ¿Qué columnas de `Players` se pueden filtrar? | "Manten las columnas que ya están" → elección posterior: "Solo las ya usadas" | Lista blanca = exactamente `Id`, `FirstName`, `LastName`. | Resuelta (ver nota N1) |
| Q3 | Contrato del 404 para un jugador desconocido | "En principio nada, es un 404 y se auto describe por el estándar de HTTP" + "Si genera un método específico para crear una respuesta 404 sin esos campos." | `PlayerNotFoundException` → ProblemDetails con `Status = 404`, sin `Title`, `Detail` ni `ErrorTranslationKey` propios, construido con un método auxiliar nuevo y dedicado en `ExceptionToHttpResponseConverters`. El middleware sigue asignando `Instance`. | Resuelta |
| Q4 | ¿El proveedor por scope libera su conexión al terminar el scope? | "Si" | El proveedor por petición cierra y libera su conexión al final de la petición (DI lo libera al liberar el scope). | Resuelta |
| Q5 | ¿Fallback a `DummyConnectionProvider` o fallo rápido? ¿Log? | "Al Dummy y se registra en el log" | Si la BD no es alcanzable al arrancar: fallback a `DummyConnectionProvider` y entrada en el log. Sin fallo rápido. | Resuelta (ver nota N2) |
| Q6 | ¿Corregir `CriteriaType.NONE`? | "Dejalo fuera." | Fuera de alcance. | Resuelta |

### Notas de interpretación (PLANNER; si no son correctas, que el operador las corrija)

- **N1 (Q2, mayúsculas):** hoy los nombres de campo no distinguen mayúsculas (MySQL resuelve `firstname` igual que `FirstName`). La regla del plan es conservar el comportamiento existente, así que la lista blanca se compara sin distinguir mayúsculas (`OrdinalIgnoreCase`, como Facilities). El operador no lo ha dicho de forma explícita.
- **N2 (Q5, log):** `LoggingStartup.ConfigureLogging` está vacío; el host usa `Host.CreateDefaultBuilder`, que ya trae los proveedores de logging por defecto de `Microsoft.Extensions.Logging`. El plan exige que el fallback emita una entrada de log con `Microsoft.Extensions.Logging` que un test pueda comprobar. Nivel y mecanismo de inyección del logger: los decide BUILDER y los justifica en `STATUS.md`. Recomendación no vinculante: no escribir la cadena de conexión completa en el log.

| Q7 | ¿Qué cuerpo lleva el 400 por campo de filtro no permitido? | "Title: Error.Search.FilterNotPermitted / Detail: FilterNotPermitted / Error: Error.Search.FilterNotPermitted" | ProblemDetails `Status = 400`, `Title = "Error.Search.FilterNotPermitted"`, `Detail = "FilterNotPermitted"`, extensión `ErrorTranslationKey = "Error.Search.FilterNotPermitted"` ("Error" es la etiqueta del operador para esa extensión). | Resuelta |

## Preguntas abiertas

Ninguna bloqueante. Las notas N1 y N2 están pendientes de que el operador las confirme; si las corrige, se actualizan los criterios afectados.

BUILDER no empieza un paso con preguntas bloqueantes abiertas; si lo encuentra así, marca `BLOCKED`.

## Fuera de alcance (anotado, no se planifica)

- Propiedad `Reason` que nunca se asigna en las excepciones `CannotDelete*/CannotUpdate*`.
- Consultas N+1 en `SqlFacilityRepository.Get`.
- `ArgumentException` de la lista blanca de Facilities que acaba en 500 (Q1 solo afecta a Players).
- Corrección de `CriteriaType.NONE` (Q6: "Dejalo fuera.").
- Limpieza de paquetes (`MySql.Data` y `MySqlConnector`; `using DocumentFormat.OpenXml.*` sin uso).
- Registro DI duplicado entre `TestFixture` y `Startup`/`*Startup` (solo se sincroniza lo que exigen los pasos 02 y 04).
- Campo `logger` sin asignar en `ExceptionHandlingMiddleware`.
- Artefactos generados de `Gaffer.Api.Client` (NSwag): los regenera el operador.

## Restricciones

- No se tocan los artefactos generados de `Gaffer.Api.Client`.
- Los tests con MySQL se ejecutan contra el Docker local; si no está disponible, `BLOCKED` con el error exacto.
- Los cambios de DI en `Gaffer.Api` se reflejan en el mismo incremento en `Gaffer.Api.Tests/TestFixture.cs`.
- Máximo 6 ciclos.
