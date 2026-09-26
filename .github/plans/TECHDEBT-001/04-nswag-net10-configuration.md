# Paso 04 - Configuración de NSwag para net10

Prerrequisito: Q3 resuelta (b: ejecución de `dotnet-nswag` Net100 hacia una carpeta temporal y registro solo del código de salida). Depende del paso 03 (el proyecto debe compilar con sus paquetes finales).

## Objetivo

Que la generación del cliente use el runtime de NSwag para .NET 10, en línea con `TargetFramework` net10.0 (lección permanente de AGENTS.md). Solo cambia la configuración; la salida generada en `Gaffer.Api.Client` queda fuera de alcance.

## Alcance de ficheros

| Fichero | Cambio |
| --- | --- |
| `Gaffer.Api/Gaffer.Api.csproj` | Target `NSwag`: `$(NSwagExe_Net90)` → `$(NSwagExe_Net100)` (definida en `NSwag.MSBuild` 14.6.1, `build/NSwag.MSBuild.props`). |
| `Gaffer.Api/nswag.json` | `"runtime": "Net90"` → `"Net100"`. `documentGenerator.aspNetCoreToOpenApi.targetFramework` se queda en `net10.0`. |

Prohibido: `dotnet build -c Release` (u otra configuración que no sea Debug) sobre `Gaffer.Api` o la solución, porque el target escribiría en `Gaffer.Api.Client`. Prohibido editar, abrir para revisar o versionar ficheros de `Gaffer.Api.Client`.

## Criterios de aceptación

| ID | Criterio |
| --- | --- |
| AC4.1 | El csproj referencia `$(NSwagExe_Net100)` y `nswag.json` tiene `"runtime": "Net100"`; ya no queda ninguna referencia a `Net90` en los dos ficheros. |
| AC4.2 | La propiedad `NSwagExe_Net100` existe en la versión de `NSwag.MSBuild` referenciada (búsqueda en el `.props` del paquete restaurado, registrada en `STATUS.md`). |
| AC4.3 | `dotnet "<ruta NuGet>/nswag.msbuild/14.6.1/tools/Net100/dotnet-nswag.dll" run nswag.json /variables:Configuration=Debug,ClientOutputPath=<carpeta temporal fuera del repo>`, ejecutado desde `Gaffer.Api` tras un build Debug, termina con código 0. Solo se registra el código de salida; no se revisa lo generado y la carpeta temporal se borra. |
| AC4.4 | `git status` no muestra cambios en `Gaffer.Api.Client/`. |
| AC4.5 | `dotnet build gaffer-api.sln -v q` (Debug) y `dotnet test gaffer-api.sln` siguen igual que tras el paso 03. |

## Doble bucle

Configuración sin comportamiento de la API.
1. **Aceptación**: AC4.3 antes del cambio (red esperado o advertencia de runtime, anotar lo que ocurra) y después (green).
2. **Unitario**: no aplica.
3. **Refactor**: build Debug + suite completa.

## Cómo demostrarlo

- Diff de los dos ficheros, comando y código de salida de AC4.3 y `git status -- Gaffer.Api.Client` en `STATUS.md`.
