# Paso 10 - Versionado SemVer del cliente (1.0.0)

Petición del operador (literal): "Regenera el cliente con nueva version 1.0.0, ajusta el proceso para que use Semantic Versioning"

Prerrequisitos: Q14 resuelta; Q15 resuelta para `infoVersion`. Q13 (publicación) no bloquea este paso, solo la acción de cierre. Depende de los pasos 04 (configuración NSwag), 05 y 06 (cambios de contrato cerrados).

Reparto (AGENTS.md): este paso cambia solo **configuración y proceso** (csproj, `nswag.json`). La regeneración del cliente, su versionado y su publicación como 1.0.0 las hace el operador al cerrar. BUILDER y REVIEWER no editan, ejecutan para verificar ni auditan la salida generada (`GafferApiClient.cs`, `OpenApiSpecs.json`).

## Objetivo

Que la versión del paquete `Gaffer.Api.Client` se defina en un único sitio de la configuración, valga 1.0.0 y tenga que cumplir SemVer 2.0.0. Que la regla para subirla quede escrita:

| Cambio del contrato de la API | Componente que sube | Ejemplo |
| --- | --- | --- |
| Incompatible (cambia códigos de estado, tipos, formato o elimina algo) | MAJOR | 201 en Create y enums como texto (este plan, que parte de 1.0.0) |
| Compatible que añade algo (endpoint, propiedad opcional) | MINOR | - |
| Corrección sin cambio de contrato | PATCH | - |

## Estado actual (verificado, solo lectura)

- `Gaffer.Api.Client.csproj`: `<Version>$(GlobalPackageVersion)</Version>`; `GeneratePackageOnBuild` solo en Release.
- `GlobalPackageVersion` no se define en ningún fichero del repositorio. Si nadie lo pasa, el SDK usa su valor por defecto (inferido).
- `nswag.json`: `infoVersion: "1.0.0"`.

## Alcance de ficheros (Q14 = a y Q15 = a, recomendados)

| Fichero | Cambio |
| --- | --- |
| `Gaffer.Api.Client/Gaffer.Api.Client.csproj` | `<GlobalPackageVersion Condition="'$(GlobalPackageVersion)' == ''">1.0.0</GlobalPackageVersion>` (se mantiene el override `/p:GlobalPackageVersion=`). Target MSBuild que falla con un error claro si `$(Version)` no cumple SemVer 2.0.0 (`MAJOR.MINOR.PATCH` con prerelease/build opcionales), enganchado antes de empaquetar y compilar. **No** tocar `GafferApiClient.cs` ni `OpenApiSpecs.json`. |
| `Gaffer.Api/nswag.json` | `infoVersion` alineada con la versión del paquete (1.0.0) (Q15 = a). Si Q15 = b, sin cambios. |

Si el operador elige otra opción en Q14 (p. ej. `Directory.Build.props`), PLANNER actualiza este alcance antes de que BUILDER empiece.

## Criterios de aceptación

| ID | Criterio |
| --- | --- |
| AC10.1 | `dotnet msbuild Gaffer.Api.Client/Gaffer.Api.Client.csproj -getProperty:Version -getProperty:PackageVersion` (solo evalúa, no compila) devuelve `1.0.0` para las dos, antes con el valor actual registrado en `STATUS.md`. |
| AC10.2 | Con `-p:GlobalPackageVersion=2.3.4` la evaluación devuelve `2.3.4` (el override sigue funcionando). |
| AC10.3 | Llamar solo al target de validación (`dotnet msbuild Gaffer.Api.Client/Gaffer.Api.Client.csproj -t:<TargetDeValidación> -p:GlobalPackageVersion=<v>`) termina con error para `1.0`, `v1.0.0` y `1.0.0.0`, y con éxito para `1.0.0`, `1.1.0-beta.1` y `2.0.0+build.5`. Este target no compila código. Red: antes del cambio el target no existe → green. |
| AC10.4 | `nswag.json` `infoVersion` = `1.0.0` (Q15 = a). |
| AC10.5 | `git status` no muestra cambios en `Gaffer.Api.Client/GafferApiClient.cs` ni en `Gaffer.Api.Client/OpenApiSpecs.json`. BUILDER no ejecuta builds Release de `Gaffer.Api` ni de la solución. |
| AC10.6 | `dotnet build gaffer-api.sln -v q` (Debug) sin errores ni advertencias nuevas y `dotnet test gaffer-api.sln` en verde, 0 omitidos. |
| AC10.7 | `STATUS.md` deja para DOCUMENTER la regla SemVer de la tabla de arriba y el procedimiento de versionado (dónde está la versión, cómo cambiarla, override por `/p:`). DOCUMENTER la escribe donde diga Q15 (recomendado: `doc/features/technical/api-client-versioning.md` y una línea en `.github/doc/common/dev-stack.md`, "Build and Packaging Notes"). |
| AC10.8 | `STATUS.md` registra la acción de cierre del operador: regenerar `Gaffer.Api.Client` después de aprobarse 05 y 06, versionarlo como 1.0.0 y publicarlo según Q13. |

## Doble bucle

1. **Aceptación**: AC10.1 y AC10.3 antes del cambio (valor actual y target inexistente: red) y después (green).
2. **Unitario**: casos válidos e inválidos de AC10.3 (hacen de tests de la validación) y AC10.2.
3. **Refactor**: build Debug de la solución + suite completa (AC10.6).

## Cómo demostrarlo

- Comandos `dotnet msbuild -getProperty` y del target de validación con su salida y código de salida (antes/después) en `STATUS.md`.
- `git status -- Gaffer.Api.Client` sin cambios en la salida generada.
- Suite completa en verde.
