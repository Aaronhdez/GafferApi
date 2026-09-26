# Paso 10 - Versionado SemVer del cliente (1.0.0)

Petición del operador (literal): "Regenera el cliente con nueva version 1.0.0, ajusta el proceso para que use Semantic Versioning"

Decisiones del operador (literal): "Q14. Opción B" / "Q15. a)+c)" (ver `00-overview-and-open-questions.md`).

Prerrequisitos: Q14 y Q15 resueltas. Q13 (publicación) no bloquea este paso, solo la acción de cierre. Q16 no bloquea (ver 00). Depende de los pasos 04 (configuración NSwag), 05 y 06 (cambios de contrato cerrados).

Reparto (AGENTS.md): este paso cambia solo **configuración y proceso** (`Directory.Build.props` nuevo, `nswag.json`). La regeneración del cliente, su versionado y su publicación como 1.0.0 las hace el operador al cerrar. BUILDER y REVIEWER no editan, ejecutan para verificar ni auditan la salida generada (`GafferApiClient.cs`, `OpenApiSpecs.json`). La documentación la escribe DOCUMENTER al cerrar (Q15 = c), no BUILDER.

## Objetivo

Que la versión del paquete `Gaffer.Api.Client` se defina en un único sitio de la configuración (un `Directory.Build.props` nuevo en la raíz, Q14 = b), valga 1.0.0 por defecto y tenga que cumplir SemVer 2.0.0; que la versión del documento OpenAPI (`infoVersion`) sea la misma (Q15 = a); y que la regla para subirla quede escrita (Q15 = c):

| Cambio del contrato de la API | Componente que sube | Ejemplo |
| --- | --- | --- |
| Incompatible (cambia códigos de estado, tipos, formato o elimina algo) | MAJOR | 201 en Create y enums como texto (este plan, que parte de 1.0.0) |
| Compatible que añade algo (endpoint, propiedad opcional) | MINOR | - |
| Corrección sin cambio de contrato | PATCH | - |

## Estado actual (verificado, solo lectura)

- `Gaffer.Api.Client.csproj`: `<Version>$(GlobalPackageVersion)</Version>`; `GeneratePackageOnBuild` solo en Release.
- `GlobalPackageVersion` solo aparece en `Gaffer.Api.Client.csproj`; no se define en ningún fichero del repositorio. Si nadie lo pasa, el SDK usa su valor por defecto (inferido).
- No existe ningún `Directory.Build.props` ni `Directory.Build.targets` en el repositorio ni en `E:\Proyectos\Colaborativos\Gaffer` o `E:\Proyectos\Colaborativos`. No se pudo comprobar `E:\Proyectos` ni `E:\` (búsqueda agotó el tiempo): BUILDER lo comprueba (un `Directory.Build.props` en la raíz corta la búsqueda hacia arriba de MSBuild y dejaría de importar uno superior).
- La solución tiene 4 proyectos: `Gaffer.Api`, `Gaffer.Api.Client`, `Gaffer.Api.Migrations`, `Gaffer.Api.Tests`. MSBuild importa un `Directory.Build.props` de la raíz en **todos** ellos.
- `nswag.json`: `infoVersion: "1.0.0"`.

## Ámbito del `Directory.Build.props` raíz

Q14 trata de la versión del **paquete `Gaffer.Api.Client`** ("lo mismo" que la opción a, en otro fichero). Por tanto el fichero se importa en los 4 proyectos, pero su contenido solo tiene efecto en `Gaffer.Api.Client`:

- La propiedad por defecto y el target de validación están condicionados a `'$(MSBuildProjectName)' == 'Gaffer.Api.Client'` (en el `PropertyGroup` y en el `Target`, o en un `PropertyGroup`/`Target` equivalente con esa condición).
- En `Gaffer.Api`, `Gaffer.Api.Migrations` y `Gaffer.Api.Tests` las propiedades `Version`, `PackageVersion`, `AssemblyVersion`, `FileVersion` y `GlobalPackageVersion` evaluadas quedan igual que antes del cambio, y el target de validación no se ejecuta en ellos.
- Si el operador quiere que la versión o la validación se apliquen a todos los proyectos, es Q16 (abierta, no bloquea); hasta entonces se aplica este ámbito.

## Alcance de ficheros (Q14 = b, Q15 = a + c)

| Fichero | Cambio |
| --- | --- |
| `Directory.Build.props` (nuevo, raíz del repositorio) | Con la condición de proyecto de arriba: `<GlobalPackageVersion Condition="'$(GlobalPackageVersion)' == ''">1.0.0</GlobalPackageVersion>` (se mantiene el override `/p:GlobalPackageVersion=`). Target MSBuild que falla con un error claro si `$(Version)` no cumple SemVer 2.0.0 (`MAJOR.MINOR.PATCH` con prerelease/build opcionales), enganchado antes de empaquetar y compilar `Gaffer.Api.Client`. Sin otras propiedades ni imports. |
| `Gaffer.Api/nswag.json` | `infoVersion` alineada con la versión del paquete (1.0.0) (Q15 = a). Hoy ya vale `1.0.0`: si no cambia, se registra como verificado sin diff. |
| `Gaffer.Api.Client/Gaffer.Api.Client.csproj` | **Sin cambios** (sigue con `<Version>$(GlobalPackageVersion)</Version>`). Si BUILDER encuentra que hace falta tocarlo para cumplir los criterios, lo registra como bloqueo en `STATUS.md` en vez de ampliarlo. **No** tocar `GafferApiClient.cs` ni `OpenApiSpecs.json`. |

Documentación (Q15 = c), la hace DOCUMENTER al cerrar, no BUILDER: `doc/features/technical/` (p. ej. `api-client-versioning.md`) y una línea en `.github/doc/common/dev-stack.md`, sección "Build and Packaging Notes".

## Criterios de aceptación

| ID | Criterio |
| --- | --- |
| AC10.1 | `dotnet msbuild Gaffer.Api.Client/Gaffer.Api.Client.csproj -getProperty:Version -getProperty:PackageVersion` (solo evalúa, no compila) devuelve `1.0.0` para las dos, antes con el valor actual registrado en `STATUS.md`. |
| AC10.2 | Con `-p:GlobalPackageVersion=2.3.4` la evaluación devuelve `2.3.4` (el override sigue funcionando). |
| AC10.3 | Llamar solo al target de validación (`dotnet msbuild Gaffer.Api.Client/Gaffer.Api.Client.csproj -t:<TargetDeValidación> -p:GlobalPackageVersion=<v>`) termina con error para `1.0`, `v1.0.0` y `1.0.0.0`, y con éxito para `1.0.0`, `1.1.0-beta.1` y `2.0.0+build.5`. Este target no compila código. Red: antes del cambio el target no existe → green. |
| AC10.4 | `nswag.json` `infoVersion` = `1.0.0`, igual a la versión evaluada en AC10.1 (Q15 = a). |
| AC10.5 | `git status` no muestra cambios en `Gaffer.Api.Client/` (ni en `GafferApiClient.cs`, `OpenApiSpecs.json` ni `Gaffer.Api.Client.csproj`). BUILDER no ejecuta builds Release de `Gaffer.Api` ni de la solución. |
| AC10.6 | `dotnet build gaffer-api.sln -v q` (Debug) sin errores ni advertencias nuevas y `dotnet test gaffer-api.sln` en verde, 0 omitidos. |
| AC10.7 | `STATUS.md` deja para DOCUMENTER la regla SemVer de la tabla de arriba y el procedimiento de versionado (la versión está en `Directory.Build.props` de la raíz, cómo cambiarla, override por `/p:GlobalPackageVersion=`, `infoVersion` de `nswag.json` debe coincidir), con los destinos de Q15 = c: `doc/features/technical/` (p. ej. `api-client-versioning.md`) y una línea en `.github/doc/common/dev-stack.md`, "Build and Packaging Notes". |
| AC10.8 | `STATUS.md` registra la acción de cierre del operador: regenerar `Gaffer.Api.Client` después de aprobarse 05 y 06, versionarlo como 1.0.0 y publicarlo según Q13. |
| AC10.9 | Sin efecto en los demás proyectos: para `Gaffer.Api/Gaffer.Api.csproj`, `Gaffer.Api.Migrations/Gaffer.Api.Migrations.csproj` y `Gaffer.Api.Tests/Gaffer.Api.Tests.csproj`, `dotnet msbuild <csproj> -getProperty:Version -getProperty:PackageVersion -getProperty:AssemblyVersion -getProperty:FileVersion -getProperty:GlobalPackageVersion` da la misma salida antes y después del cambio (ambas registradas en `STATUS.md`). |
| AC10.10 | El target de validación no actúa fuera del cliente: `dotnet msbuild Gaffer.Api/Gaffer.Api.csproj -t:<TargetDeValidación> -p:GlobalPackageVersion=v1.0.0` no falla por la validación SemVer (o bien el target no está definido en ese proyecto, o está definido pero se omite por su condición); se registra la salida y el código de salida. |
| AC10.11 | `Directory.Build.props` solo contiene lo descrito en "Alcance de ficheros": ninguna otra propiedad global, `PackageReference` ni `Import`. BUILDER registra en `STATUS.md` si existe algún `Directory.Build.props`/`.targets` en directorios superiores al repositorio (y, si existe, que el nuevo fichero no deja de importarlo o que su pérdida no cambia AC10.9). |

## Doble bucle

1. **Aceptación**: AC10.1, AC10.3 y AC10.9 antes del cambio (valor actual, target inexistente: red; valores de los otros proyectos como línea base) y después (green; otros proyectos iguales).
2. **Unitario**: casos válidos e inválidos de AC10.3 (hacen de tests de la validación), AC10.2 y AC10.10.
3. **Refactor**: build Debug de la solución + suite completa (AC10.6).

## Cómo demostrarlo

- Comandos `dotnet msbuild -getProperty` (cliente y los otros 3 proyectos) y del target de validación con su salida y código de salida (antes/después) en `STATUS.md`.
- `git status -- Gaffer.Api.Client Directory.Build.props Gaffer.Api/nswag.json`: solo `Directory.Build.props` nuevo y, como mucho, `nswag.json`.
- Suite completa en verde.
