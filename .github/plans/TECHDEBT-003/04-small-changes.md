# Paso 04 (ciclo 4) - Cambios pequeños

Q5: sin respuesta se aplica b) (solo los 12 registros). Q6 necesaria para AC4.5.

## Objetivo

Seis limpiezas sin cambio de comportamiento HTTP: TestFixture reutiliza el registro de DI de producción; `IGafferApiClient` pasa a tests; `infoTitle` real en `nswag.json`; sin código comentado en `Startup.cs`/`SwaggerStartup.cs`; nombres correctos en `Player`; una sola instancia de `GafferApiVersioningDefinition` en `VersioningStartup`.

## Alcance de ficheros

| Fichero | Cambio |
| --- | --- |
| `Gaffer.Api.Tests/TestFixture.cs` | Sustituir los registros de repositorios y handlers por `services.ConfigureRepositories(...)` y `services.ConfigureActions()`; mantener el `IDatabaseConnectionProvider` por scope contra la BD de test (o `ConfigureDatabase` con configuración de test, si BUILDER lo justifica). Q5 = a: también `ConfigureVersioning()`. |
| `Gaffer.Api/Startups/*Startup.cs` | Solo si hace falta exponer algo para que TestFixture lo reutilice (sin cambiar lo que registra producción). |
| `Gaffer.Api/Shared/Clients/IGafferApiClient.cs` → `Gaffer.Api.Tests/TestShared/IGafferApiClient.cs` | Mover y ajustar `namespace`/`using` en los tests. |
| `Gaffer.Api/nswag.json` | `infoTitle` según Q6 (sin tocar la salida generada). |
| `Gaffer.Api/Startup.cs` L52-55, `Gaffer.Api/Startups/SwaggerStartup.cs` L27-44 | Borrar código comentado. |
| `Gaffer.Api/Domain/Player.cs` + llamadores (`UpdatePlayerCommandHandler`, tests) | Parámetros con nombre correcto; `GetBirthDateName`/`SetBirthDateName` → nombres sin `Name` (p. ej. `GetBirthDate`/`SetBirthDate`); revisar `GetGenderName` con el mismo criterio. Partir del estado que deje TECHDEBT-002 paso 01. |
| `Gaffer.Api/Startups/VersioningStartup.cs` | Usar la instancia de L12 en la convención de L20. |

## Criterios de aceptación

| ID | Criterio |
| --- | --- |
| AC4.1 | `TestFixture.cs` no contiene `AddScoped<` de repositorios ni de handlers; un test comprueba que el `TestServer` resuelve los 2 repositorios y los 8 handlers registrados en producción (así un handler nuevo registrado solo en `ActionsStartup` queda cubierto sin tocar TestFixture). |
| AC4.2 | `IGafferApiClient` no existe en `Gaffer.Api` (`grep -rn "IGafferApiClient" Gaffer.Api` → 0) y los tests compilan y pasan. |
| AC4.3 | `grep -n "^\s*//" Gaffer.Api/Startup.cs Gaffer.Api/Startups/SwaggerStartup.cs` solo devuelve comentarios explicativos (no código). |
| AC4.4 | `Player.cs`: ningún parámetro se llama `commandFirstName` salvo en `SetFirstName` si procede; ningún método se llama `*BirthDateName`. Tests de dominio y de `UpdatePlayerCommandHandler` en verde. |
| AC4.5 | `nswag.json` `infoTitle` = valor de Q6. Evidencia opcional como en TECHDEBT-001 paso 04: `dotnet-nswag` hacia una carpeta temporal fuera del repo, registrando solo el código de salida. |
| AC4.6 | `VersioningStartup.cs` crea `GafferApiVersioningDefinition` una sola vez; el OpenAPI servido y las versiones de la API no cambian (tests de versionado/OpenAPI existentes en verde). |
| AC4.7 | Suite completa en verde con el mismo recuento más los tests nuevos; el `swagger.json` servido es igual antes y después salvo lo esperado. |

## Doble bucle

1. **Aceptación**: AC4.1 (test de resolución de dependencias: green antes y después, es caracterización) y AC4.6/AC4.7 como regresión.
2. **Unitario**: AC4.4 (tests existentes de `Player`/handler tras el renombre).
3. **Refactor**: es el contenido del paso; revalidar la suite completa tras cada una de las 6 limpiezas o al final (BUILDER registra cuál).

## Cómo demostrarlo

- `grep` de AC4.2, AC4.3 y AC4.4 antes/después en `STATUS.md`.
- `dotnet test gaffer-api.sln --verbosity quiet` completo en verde con recuento.
