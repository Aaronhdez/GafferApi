# Paso 05 (ciclo 5) - 0 warnings de compilación

Q7 (activar `TreatWarningsAsErrors`) necesaria para AC5.5. Q8: sin respuesta se aplica a) (sin cambios de contrato).

## Objetivo

`dotnet build gaffer-api.sln --no-incremental` termina con 0 warnings en los proyectos que BUILDER puede editar, sin cambiar el contrato OpenAPI ni el comportamiento. Va el último porque los pasos anteriores tocan los mismos ficheros.

## Alcance de ficheros

Los ficheros que emitan warnings en `Gaffer.Api`, `Gaffer.Api.Tests` y `Gaffer.Api.Migrations` (lista medida por BUILDER al empezar), y los `.csproj` solo para Q7 o para supresiones justificadas (Q8). **No** la salida generada de `Gaffer.Api.Client`: si emite warnings, se registran y decide el operador (Q8).

## Criterios de aceptación

| ID | Criterio |
| --- | --- |
| AC5.1 | Medición inicial en `STATUS.md`: total y recuento por código y por proyecto de `dotnet build gaffer-api.sln --no-incremental -v q` (sin duplicados), tras TECHDEBT-002 y los pasos 01-04. |
| AC5.2 | Medición final: 0 warnings (o solo los de salida generada, listados y aceptados por el operador según Q8). |
| AC5.3 | El `swagger.json` servido es idéntico antes y después (comparación guardada fuera del repo, resultado registrado). |
| AC5.4 | Ninguna supresión (`#pragma warning disable`, `NoWarn`, `!` indiscriminado) sin justificación en `STATUS.md`; REVIEWER revisa cada una. |
| AC5.5 | Según Q7: `TreatWarningsAsErrors = true` en los proyectos elegidos y el build sigue en verde; o sin cambio si Q7 = c. |
| AC5.6 | Suite completa en verde con el mismo recuento. |

## Doble bucle

1. **Aceptación**: AC5.3 y la suite completa como caracterización antes de tocar nada.
2. **Unitario**: por grupos de código de warning (CS8618, CS8625, CS8602, CS0414, CS8603, CS9113, CS8604, CS8601, CS0169), build tras cada grupo con su recuento; tests afectados en verde.
3. **Refactor**: revisar que las correcciones de anulabilidad reflejan la semántica real (no solo silencian). Revalidar suite y AC5.3.

## Cómo demostrarlo

- Tabla de recuentos por grupo (antes → después) en `STATUS.md`.
- Resultado de la comparación de `swagger.json` (AC5.3).
- `dotnet test gaffer-api.sln --verbosity quiet` completo en verde.
