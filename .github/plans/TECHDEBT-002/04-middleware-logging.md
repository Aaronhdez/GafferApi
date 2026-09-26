# Paso 04 (ciclo 4) - ExceptionHandlingMiddleware registra las excepciones

Q6 resuelta: excepciones mapeadas a 400 → nivel `Error`; mapeadas a 404 → no se registran. El diccionario solo mapea a 400 y 404, así que no hay más casos. Sin preguntas abiertas para este paso.

## Objetivo

El middleware recibe `ILogger<ExceptionHandlingMiddleware>` por constructor, registra las excepciones no mapeadas (500) con `LogError` incluyendo la excepción, registra las mapeadas a 400 con nivel `Error`, no registra las mapeadas a 404 y corrige el título `"Unkwnown error"` → `"Unknown error"`. Se añade el test que falta de "excepción desconocida → 500".

## Alcance de ficheros

| Fichero | Cambio |
| --- | --- |
| `Gaffer.Api/Shared/Exceptions/ExceptionHandlingMiddleware.cs` | Constructor primario `(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)`; quitar el campo sin asignar (CS0169) y el comentario; logs; errata. |
| `Gaffer.Api.Tests/Unit/Exceptions/ExceptionHandlingMiddlewareShould.cs` (creado en el paso 03) | Tests de 500 y de log con un logger de test propio (sin paquetes nuevos). |
| `Gaffer.Api.Tests/Acceptance/DatabaseFailureShould.cs` (del paso 02) | Aserción de 500 + log con la BD inalcanzable, si el host de test permite capturar logs (proveedor de log de test). |
| `Gaffer.Api.Tests/TestFixture.cs` | Solo si el registro del logging del host de test necesita cambios (AGENTS.md: mismo incremento). |

## Criterios de aceptación

| ID | Criterio |
| --- | --- |
| AC4.1 | Unitario: una `InvalidOperationException` no mapeada produce 500, `application/json`, `Title = "Unknown error"`, `Instance` = ruta. |
| AC4.2 | Unitario: esa excepción se registra **una vez** con nivel `Error` y la instancia de la excepción adjunta. |
| AC4.3 | Q6. (a) Una excepción mapeada a 400 (p. ej. `PlayerSearchFilterNotPermittedException`) produce **una** entrada de log de nivel `Error` desde `ExceptionHandlingMiddleware`; la respuesta (400 y ProblemDetails) no cambia. (b) Una excepción mapeada a 404 (p. ej. `PlayerNotFoundException`) produce **cero** entradas de log de cualquier nivel desde el middleware; la respuesta (404) no cambia. |
| AC4.4 | Aceptación: con la BD inalcanzable, `GET /players/{id}` devuelve 500 `"Unknown error"` y el proveedor de log de test recibe una entrada `Error` de `ExceptionHandlingMiddleware`. |
| AC4.5 | `dotnet build` ya no emite CS0169 para `ExceptionHandlingMiddleware.logger`; `grep -rn "Unkwnown" Gaffer.Api Gaffer.Api.Tests` → 0. |
| AC4.6 | Suite completa en verde; los tests existentes que leen `Title` de errores siguen en verde (ninguno depende de la errata; si alguno lo hiciera, se registra). |

## Doble bucle

1. **Aceptación**: AC4.4. Red: sin entrada de log → green.
2. **Unitario**: AC4.1 (puede estar green salvo el título: red por la errata), AC4.2, AC4.3. Red → green.
3. **Refactor**: simplificar `HandleExceptionAsync` (`TryGetValue` en vez de `ContainsKey` + indexador; el diccionario no se copia en cada excepción). Revalidar la suite.

## Cómo demostrarlo

- `dotnet test gaffer-api.sln --filter "FullyQualifiedName~ExceptionHandlingMiddleware|FullyQualifiedName~DatabaseFailure"`: red y green en `STATUS.md`.
- `dotnet build gaffer-api.sln --no-incremental -v q`: recuento de advertencias antes/después (baja al menos el CS0169 de este campo).
- Suite completa en verde.

## Notas

- El cambio de `"Unkwnown error"` a `"Unknown error"` es visible en el cuerpo de los 500 (cambio de contrato menor, pedido por el operador).
- La serialización de ProblemDetails (PascalCase) no cambia (Q5 de TECHDEBT-001, fuera de alcance).
