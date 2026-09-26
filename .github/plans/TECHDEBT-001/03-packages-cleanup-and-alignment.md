# Paso 03 - Paquetes sobrantes y desalineados

Prerrequisito: Q1 (a: se mantiene `MySql.Data` y se quita `MySqlConnector` de `Gaffer.Api`) y Q2 (b: `global.json` en 10.0.x) resueltas. Depende del paso 02 (menos código que compilar y revisar).

## Objetivo

Que `Gaffer.Api` y `Gaffer.Api.Tests` referencien solo los paquetes que usan, y que las versiones de Microsoft que se queden estén alineadas con net10 (10.0.x). Sin cambios de comportamiento.

## Alcance de ficheros

| Fichero | Cambio |
| --- | --- |
| `Gaffer.Api/Gaffer.Api.csproj` | Quitar `ClosedXML`, `AspNetCore.HealthChecks.SqlServer`, `AspNetCore.HealthChecks.UI.Client`, `AspNetCore.HealthChecks.Uris`, `Microsoft.ApplicationInsights.AspNetCore`, `Microsoft.VisualStudio.Web.CodeGeneration.Design`. Quitar `Microsoft.Extensions.Configuration.Binder`, `.Configuration.UserSecrets`, `.DependencyInjection.Abstractions` y `.Options` 9.0.9, que ya vienen en el framework compartido de ASP.NET Core 10. Subir `Microsoft.AspNetCore.Mvc.NewtonsoftJson` a 10.0.x (misma versión de parche que `Microsoft.AspNetCore.TestHost`, hoy 10.0.10). Quitar `MySqlConnector` (Q1 = a). Quitar el bloque de `PackageReference` comentado, `<Content Include="bin\Debug\net10.0\Asp.Versioning.Abstractions.dll" />` y `<None Include="bin\Debug\net10.0\appsettings.json" />`. **No tocar** `Microsoft.AspNetCore.Authentication.JwtBearer` (lo decide el paso 07 según Q8) ni el target `NSwag` (paso 04). |
| `Gaffer.Api.Tests/Gaffer.Api.Tests.csproj` | Quitar `<None Include="bin\Debug\net10.0\testSettings.json" />`, comprobando antes que `testSettings.json` se sigue copiando a la salida (ver AC3.5). Quitar la referencia directa a `MySql.Data` si ningún test usa sus tipos (hoy ninguno; se sigue recibiendo de forma transitiva desde `Gaffer.Api`). |
| `Gaffer.Api/Shared/Repositories/IPlayerRepository.cs`, `Gaffer.Api/Infrastructure/SqlPlayerRepository.cs`, `Gaffer.Api.Tests/Unit/Commands/UpdatePlayerCommandHandlerShould.cs` | Quitar los `using DocumentFormat.OpenXml.*` sueltos (dejarían de compilar sin `ClosedXML`). |
| `Gaffer.Api/Program.cs` | Quitar `using Microsoft.Identity.Client` suelto. |
| `global.json` | `version` = el SDK 10.0.x instalado (medido con `dotnet --list-sdks`) y `rollForward: latestFeature` (Q2 = b). |

Nota de implementación (no es un requisito nuevo): `Gaffer.Api` usa `Microsoft.NET.Sdk`. Si al quitar paquetes deja de llegar la referencia a `Microsoft.AspNetCore.App`, BUILDER añade `<FrameworkReference Include="Microsoft.AspNetCore.App" />` en vez de cambiar el SDK del proyecto, y lo justifica en `STATUS.md`.

## Criterios de aceptación

| ID | Criterio |
| --- | --- |
| AC3.1 | Antes de quitar cada paquete, BUILDER registra en `STATUS.md` la búsqueda que demuestra que no se usa (namespaces del paquete en `*.cs`). |
| AC3.2 | `dotnet list gaffer-api.sln package` no muestra ninguna referencia directa `Microsoft.*` 9.x en `Gaffer.Api` ni en `Gaffer.Api.Tests`, salvo `Microsoft.AspNetCore.Authentication.JwtBearer` (pendiente del paso 07). |
| AC3.3 | Ningún `.csproj` contiene rutas `bin\Debug\...` ni bloques `PackageReference` comentados. |
| AC3.4 | `dotnet build gaffer-api.sln --no-incremental -v q`: 0 errores y no más advertencias que en la línea base; las advertencias NU1901/NU1903 se cuentan antes y después y se anota cuáles desaparecen. |
| AC3.5 | `testSettings.json` y `appsettings*.json` siguen en la carpeta de salida (`Gaffer.Api.Tests/bin/Debug/net10.0/`) tras un build limpio. |
| AC3.6 | `dotnet test gaffer-api.sln`: mismo recuento en verde que la línea base, 0 omitidos. |
| AC3.7 | `dotnet --version` en la raíz del repositorio devuelve 10.0.x (el mismo SDK fijado en `global.json`) y el build sigue en verde. |

## Doble bucle

Limpieza sin comportamiento.
1. **Aceptación**: suite de aceptación en verde antes y después.
2. **Unitario**: suites unitarias e integración en verde antes y después.
3. **Refactor**: build limpio (`--no-incremental`) + suite completa. Quitar los paquetes de uno en uno o por grupos pequeños y compilar después de cada grupo, para identificar la causa si algo falla.

## Cómo demostrarlo

- Búsquedas de AC3.1, diff de `dotnet list package` antes/después, recuento de advertencias y `dotnet test gaffer-api.sln --verbosity quiet` en `STATUS.md`.
