# Paso 07 - Autenticación y autorización

Prerrequisito: Q8 resuelta ("Ok." → a: quitar la autenticación por cookie sin uso; la API queda anónima de forma explícita). Depende de los pasos 03 (paquetes) y 06 (contrato HTTP estable).

## Objetivo

Que no quede autenticación ni autorización registrada que no se usa. La API es anónima por decisión explícita; la autenticación real, si llega, tendrá su propio plan.

## Alcance de ficheros

| Fichero | Cambio |
| --- | --- |
| `Gaffer.Api/Startups/AuthenticationStartup.cs` | Borrar. |
| `Gaffer.Api/Startup.cs` | Quitar `ConfigureAuthentication`, `AddAuthorization`, `UseAuthentication`, `UseAuthorization` y el `//.RequireAuthorization()` comentado. |
| `Gaffer.Api/Startups/RepositoriesStartup.cs` | Quitar `using Microsoft.AspNetCore.Authentication.Cookies` suelto. |
| `Gaffer.Api/Gaffer.Api.csproj` | Quitar `Microsoft.AspNetCore.Authentication.JwtBearer` 9.0.9 (sin uso). Comprobar AC3.2 otra vez: ya no queda ningún `Microsoft.*` 9.x. |
| `Gaffer.Api.Tests/TestFixture.cs` | Quitar `//.RequireAuthorization()` comentado (mismo incremento). |
| `Gaffer.Api.Tests/Unit/...` (nuevo, p. ej. `StartupSecurityShould.cs`) | Test de configuración de `Startup`. |

## Criterios de aceptación

| ID | Criterio |
| --- | --- |
| AC7.1 | Tras `new Startup(config).ConfigureServices(services)` (con `DatabaseConfig:Enabled=false`), no hay ningún esquema de autenticación registrado: `IAuthenticationSchemeProvider` no se puede resolver o `GetAllSchemesAsync()` está vacío. Red hoy (esquema `Cookies`) → green. |
| AC7.2 | Una petición anónima a un endpoint (p. ej. `GET /api/v1.0/facilities`) sigue devolviendo 200, sin `Set-Cookie` y sin redirección: el comportamiento público no cambia. |
| AC7.3 | Una búsqueda de `AddCookie`, `CookieAuthenticationDefaults`, `UseAuthentication`, `UseAuthorization`, `RequireAuthorization` y `JwtBearer` en `Gaffer.Api` y `Gaffer.Api.Tests` (`*.cs`, `*.csproj`) no devuelve resultados. |
| AC7.4 | `dotnet list gaffer-api.sln package`: ninguna referencia directa `Microsoft.*` 9.x. |
| AC7.5 | Build sin errores ni advertencias nuevas; `dotnet test gaffer-api.sln` en verde, 0 omitidos. |

## Doble bucle

1. **Aceptación**: AC7.2 (test HTTP en `TestServer`). Se escribe antes y queda en verde (caracterización: la API ya es anónima).
2. **Unitario**: AC7.1 red → green.
3. **Refactor**: build + suite completa.

## Cómo demostrarlo

- `dotnet test gaffer-api.sln --filter "FullyQualifiedName~StartupSecurity"`: red y green en `STATUS.md`.
- Resultados de AC7.3 y AC7.4 y suite completa.
- Nota de cierre en `STATUS.md`: la API queda anónima por decisión explícita; si se quiere autenticación real, necesita su propio plan.
