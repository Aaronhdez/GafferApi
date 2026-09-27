# Paso 02 (ciclo 2) - Entorno de hosting desde la variable de entorno

Decisión del operador (literal): "Leer de APNETCORE_ENVIRONMENT y si no está, usar Development en su defecto."

**Bloqueo**: Q3 (nombre exacto de la variable; recomendación `ASPNETCORE_ENVIRONMENT`, vacío = ausente). Q4 (código muerto `SetConfigurationProviders`) no bloquea. En este fichero, `<VAR>` = la variable que fije Q3.

## Objetivo

- El entorno del host (`IHostEnvironment.EnvironmentName`) y `Program.ProductContext.Environment` toman el valor de `<VAR>`; si falta (o, según Q3, está vacía), `Development`.
- `UseEnvironment(...)` deja de forzar un valor fijo.
- Q4 = a): eliminar `SetConfigurationProviders`, `AddDevelopmentSettings` y el `ConfigurationBuilder` sin uso de `GetProductContext`.

## Consecuencias derivadas (no son requisitos nuevos)

| Tema | Consecuencia |
| --- | --- |
| Entorno distinto de `Development` | `CreateDefaultBuilder` solo carga User Secrets en `Development`; con otro entorno la cadena de conexión tiene que llegar por variable de entorno (`DatabaseConfig__ConnectionString`) o `appsettings`. Tampoco se carga `appsettings.Development.json` ni `UseDeveloperExceptionPage` (`Startup.cs` L56). Es el comportamiento estándar de ASP.NET Core; se comunica, no se cambia. |
| Tests | `TestFixture`, `StartupTestServer` y `ApiDatabaseConfigurationShould` fijan su propio entorno con `UseEnvironment` y no pasan por `Program.Main`: no deben cambiar. |
| NSwag | Si BUILDER extrae un método de construcción del host (p. ej. `CreateHostBuilder(string[] args)`) para poder probarlo, NSwag (`aspNetCoreToOpenApi`) puede empezar a usarlo al generar el cliente. No verificable por BUILDER (sin Release); lo registra como riesgo para el paso 05 y la regeneración del operador. |

## Alcance de ficheros

| Fichero | Cambio |
| --- | --- |
| `Gaffer.Api/Program.cs` | Resolución del entorno (función pura o equivalente, probable por tests); `UseEnvironment` con el valor resuelto; Q4. |
| `Gaffer.Api/ProductContext.cs` | Solo si hace falta para que `Environment` refleje el valor resuelto (el valor por defecto `"Development"` puede quedarse). |
| `Gaffer.Api.Tests/Unit/...` (p. ej. `Unit/ProgramShould.cs` o `Unit/Hosting/...`) y/o `Gaffer.Api.Tests/Integration/...` | Tests nuevos. Reutilizar `EnvironmentVariableScope` (ya existe en tests) para fijar/quitar la variable. |

## Criterios de aceptación

| ID | Criterio |
| --- | --- |
| AC2.1 | Aceptación (host real construido con el mismo código que usa `Main`, sin `Run`): con `<VAR>` = `Staging` → `IHostEnvironment.EnvironmentName == "Staging"`. Red hoy: `Development`. |
| AC2.2 | Con `<VAR>` ausente → `Development`. Con `<VAR>` vacía o solo espacios → según Q3 (recomendación: `Development`). |
| AC2.3 | `Program.ProductContext.Environment` (o el valor que exponga el contexto) coincide con el entorno del host en AC2.1-AC2.2. Si el campo estático se inicializa una sola vez por proceso, el test prueba la función de resolución y lo registra. |
| AC2.4 | Unitario de la función de resolución: valor presente → ese valor; ausente → `Development`; vacío/espacios → según Q3. |
| AC2.5 | Q4 = a): `grep -n "SetConfigurationProviders\|AddDevelopmentSettings" -r Gaffer.Api` → 0; `appsettings.json` y `appsettings.{Environment}.json` se siguen cargando (lo demuestra un test existente o uno nuevo que lee un valor de `appsettings.json` desde el host). Q4 sin respuesta: sin cambios y anotado. |
| AC2.6 | Regresión: `TestFixture`/`StartupTestServer` siguen en entorno `Test`; `ApiDatabaseConfigurationShould` en verde; suite completa en verde con recuento; build 0 warnings (con `TreatWarningsAsErrors` del paso 01). |
| AC2.7 | Los tests que tocan la variable de entorno la restauran (sin fugas entre tests); si pueden interferir con tests en paralelo, se marcan como no paralelizables y se registra. |

## Doble bucle

1. **Aceptación**: AC2.1-AC2.3 (red: siempre `Development`) → green.
2. **Unitario**: AC2.4 → green.
3. **Refactor**: Q4 (AC2.5). Revalidar la suite completa y el build.

## Cómo demostrarlo

- Filtro de los tests nuevos + `ApiDatabaseConfigurationShould`: red y green en `STATUS.md`.
- `grep` de AC2.5; `git diff --stat Gaffer.Api/Program.cs Gaffer.Api/ProductContext.cs`.
- Suite completa y `dotnet build --no-incremental -v q` (solo resumen).
- Opcional, manual: `dotnet run --project Gaffer.Api` con y sin `<VAR>` y la línea de log `Hosting environment:` (no sustituye a los tests).
