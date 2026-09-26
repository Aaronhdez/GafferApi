# Paso 04 - Comprobación de conexión robusta al arrancar

Prerequisitos: paso 02 terminado (mismo fichero `DatabaseStartup.cs`); Q5 = "Al Dummy y se registra en el log" (ver nota N2 en `00-overview-and-open-questions.md`).

## Objetivo

`TestConnection` comprueba la conectividad sin depender de una tabla inexistente, de forma asíncrona, liberando los recursos que abre y sin propagar excepciones de conectividad. `DatabaseStartup` aplica la política de Q5 sin bloquear con `.Result`.

## Alcance de ficheros

| Fichero | Cambio |
| --- | --- |
| `Gaffer.Api/Shared/ConnectionProviders/SqlConnectionProvider.cs` | `TestConnection`: consulta que no depende del esquema, `OpenAsync`, comando y lector liberados, excepciones de conectividad → `false`. |
| `Gaffer.Api/Startups/DatabaseStartup.cs` | Quitar `.Result` (o aislarlo de forma segura si `ConfigureServices` sigue siendo síncrono; BUILDER justifica la opción elegida); en el fallback, registrar `DummyConnectionProvider` y escribir una entrada de log con `Microsoft.Extensions.Logging`. |
| `Gaffer.Api/Startup.cs` | Solo si hace falta para pasar un logger a `ConfigureDatabase`. |
| `Gaffer.Api.Tests/.../DatabaseStartupShould.cs` (creado en el paso 02) y test nuevo de `SqlConnectionProvider` | Tests. |
| `Gaffer.Api.Tests/TestFixture.cs` | Solo si el cambio de firma o DI lo exige. |

## Criterios de aceptación

| ID | Criterio |
| --- | --- |
| AC4.1 | Con la BD de Docker disponible (`GafferDB_Test`, sin tabla `Parameters`), `TestConnection()` devuelve `true` (hoy lanza `MySqlException`). |
| AC4.2 | Con una cadena de conexión inalcanzable (p. ej. `Port=1`, `Connection Timeout` corto), `TestConnection()` devuelve `false` sin lanzar excepción. |
| AC4.3 | `ConfigureDatabase` con BD alcanzable registra el proveedor SQL por scope (se mantiene AC2.1). |
| AC4.4 | `ConfigureDatabase` con BD inalcanzable no lanza excepción e `IDatabaseConnectionProvider` resuelve a `DummyConnectionProvider`. |
| AC4.4b | En ese fallback se emite exactamente una entrada de log (`Microsoft.Extensions.Logging`) que indica que la BD no es alcanzable y que se usa `DummyConnectionProvider`. Se comprueba con un logger o proveedor de captura en el test. Con BD alcanzable no se emite esa entrada. |
| AC4.5 | `Enabled: false` o una cadena de conexión vacía siguen registrando `DummyConnectionProvider` (comportamiento actual). |
| AC4.6 | Toda la suite existente sigue en verde. |

## Doble bucle

1. **Aceptación**: tests de `ConfigureDatabase` para AC4.3-AC4.5 con `IConfiguration` en memoria. Red (AC4.4 hoy lanza `AggregateException`) → green.
2. **Unitario**: tests de `SqlConnectionProvider.TestConnection` para AC4.1 y AC4.2. Red → green.
3. **Refactor**: limpiar `DatabaseStartup` y `TestConnection`; volver a ejecutar la suite.

## Cómo demostrarlo

- `dotnet test gaffer-api.sln --filter "FullyQualifiedName~DatabaseStartup|FullyQualifiedName~SqlConnectionProvider"`: red y luego green en `STATUS.md`.
- `dotnet test gaffer-api.sln` completo en verde.
- Opcional, y solo si el operador lo autoriza: arrancar `Gaffer.Api` en Development con la BD parada y registrar el resultado. No es obligatorio para aprobar.
