# Paso 05 - Refactor transversal y regresión completa

## Objetivo

Hacer los refactors transversales de los pasos 01-04 (nombres, duplicación introducida en los tests nuevos, `using` sin uso añadidos en este plan) sin cambiar el comportamiento, y demostrar la regresión completa.

## Alcance de ficheros

Solo los ficheros ya tocados en los pasos 01-04. No se hacen refactors de los puntos "Fuera de alcance" de `00-overview-and-open-questions.md`.

## Criterios de aceptación

| ID | Criterio |
| --- | --- |
| AC5.1 | `dotnet build gaffer-api.sln` sin errores y sin warnings nuevos atribuibles a este plan. |
| AC5.2 | `dotnet test gaffer-api.sln` en verde: los 95 tests de la línea base más todos los tests nuevos de los pasos 01-04, ninguno omitido (tests con MySQL contra el Docker local). |
| AC5.3 | `git status`/`git diff --stat` muestran solo ficheros dentro del alcance de los pasos 01-05 y del plan; ningún artefacto generado de `Gaffer.Api.Client`. |
| AC5.4 | `STATUS.md` contiene la evidencia red → green de cada test nuevo (paso, comando, resultado). |

## Cómo demostrarlo

- Registrar en `STATUS.md` los comandos `dotnet build gaffer-api.sln`, `dotnet test gaffer-api.sln --verbosity quiet` (total/aprobados/fallidos/omitidos) y `git diff --stat`.
