# Paso 11 - Refactor transversal y regresión completa

Prerrequisito: pasos 01-10 completados, o anotados en `STATUS.md` como bloqueados o aplazados por una respuesta del operador.

## Objetivo

Comprobar que el conjunto de cambios es coherente, que no quedan restos de los puntos tratados y que toda la suite pasa contra Docker local.

## Alcance de ficheros

Solo los ficheros ya tocados en los pasos 01-10. No se añade alcance nuevo.

## Criterios de aceptación

| ID | Criterio |
| --- | --- |
| AC11.1 | `dotnet build gaffer-api.sln --no-incremental -v q` (Debug): 0 errores; advertencias ≤ línea base, con el recuento antes/después en `STATUS.md`. |
| AC11.2 | `dotnet test gaffer-api.sln`: todos en verde, 0 omitidos, contra `Gaffer.Database`/`GafferDB_Test`; recuento final = línea base + tests nuevos. |
| AC11.3 | Comprobaciones de restos (búsquedas registradas): ninguna referencia directa `Microsoft.*` 9.x; ningún `Net90` en la configuración de NSwag; ningún `AddJsonOptions`; `defaultEnumHandling: "String"` en `nswag.json`; ninguna contraseña en ficheros versionados (AC8.1); ningún `Adresses` fuera de `M0002`/`M0003`; ningún símbolo de AC2.1; ninguna autenticación registrada (AC7.3); versión del cliente evaluada = 1.0.0 (AC10.1) e igual a `infoVersion` de `nswag.json` (AC10.4). Si un paso quedó aplazado, su comprobación se marca "aplazada (Qn)". |
| AC11.4 | `git status` y `git diff --stat`: solo ficheros dentro del alcance de los pasos. `Directory.Build.props` nuevo en la raíz (paso 10, Q14 = b); nada cambiado en `Gaffer.Api.Client/` (ni csproj ni salida generada). La versión evaluada de `Gaffer.Api`, `Gaffer.Api.Migrations` y `Gaffer.Api.Tests` sigue igual a la línea base de AC10.9. |
| AC11.5 | `STATUS.md` reúne las acciones pendientes del operador: `git rm --cached` (paso 01), configurar secretos (08), migrar `GafferDB` (09), regenerar `Gaffer.Api.Client` como 1.0.0 y publicarlo (10, Q13) y revisar a sus consumidores (201 y enums como texto). |
| AC11.6 | `STATUS.md` lista, para DOCUMENTER, las referencias de paquetes cambiadas (`.github/doc/common/dev-stack.md`, `test-stack.md`), la regla SemVer y el procedimiento de versionado (paso 10, AC10.7; Q15 = c: `doc/features/technical/` y una línea en `.github/doc/common/dev-stack.md`, "Build and Packaging Notes") y los documentos afectados (`doc/features/`, `doc/features/technical/`, `.github/doc/features/facility-create.md`). |

## Doble bucle

1. **Aceptación**: suite de aceptación completa.
2. **Unitario**: suites unitarias e integración.
3. **Refactor**: solo duplicación evidente introducida por este plan (p. ej. construir `Startup` en los tests de configuración de los pasos 05 y 07 con un helper compartido); volver a ejecutar AC11.1 y AC11.2.

## Cómo demostrarlo

- Comandos y resultados de AC11.1-AC11.4 en `STATUS.md`.
