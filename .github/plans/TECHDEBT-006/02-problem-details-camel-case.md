# Paso 02 (ciclo 2) - `ProblemDetails` en camelCase con forma RFC 7807 (cambio de contrato)

Antes paso 03 (`03-problem-details-camel-case.md`); renumerado tras las respuestas del operador. Correspondencia: `AC2.N` = antiguo `AC3.N`, con la forma ajustada a Q10 = "Formato estándar RFC 7807".

Objetivo: que todas las respuestas de error (`ExceptionHandlingMiddleware` y el 400 de model binding de `ValidationStartup`) usen camelCase y la forma RFC 7807 que usa ASP.NET: propiedades estándar en camelCase y extensiones **aplanadas en la raíz** (`errorTranslationKey`, `errors`), **sin** objeto `extensions`. Es el **único paso con cambio de contrato** del plan. Skill de PLANNER aplicada: `api-and-interface-design`.

**Bloqueo**: ninguno. Q10 resuelta ("Formato estándar RFC 7807"). Q11 (claves dentro de `errors`) queda como pregunta no bloqueante: sin respuesta, a) camelCase por segmento. Q12 y Q13 con su valor por defecto a). Q14 resuelta ("MAJOR 3.0.0"): no bloquea el ciclo, pero el push requiere la acción del operador de `00`.

## Contrato (Q10 RFC 7807, Q11 a) por defecto, Q12 a), Q13 a))

| Antes (hoy) | Después |
| --- | --- |
| `{"Type":null,"Title":"...","Status":400,"Detail":"...","Instance":"/api/...","Extensions":{"ErrorTranslationKey":"Error.Players.Create.Validation","Errors":{"FirstName":["..."],"Address.Street":["..."],"Stands[1].StandName":["..."]}}}` | `{"type":null,"title":"...","status":400,"detail":"...","instance":"/api/...","errorTranslationKey":"Error.Players.Create.Validation","errors":{"firstName":["..."],"address.street":["..."],"stands[1].standName":["..."]}}` |
| Error sin clave de traducción (p. ej. 500 desconocido, 404 sin extensión): `"Extensions":{}` | Sin `errorTranslationKey` ni `errors` en la raíz; sin `extensions` |
| `Content-Type: application/json` | Sin cambios (Q13 a)) |

- Los **valores** no cambian: claves de traducción `Error.*`, estados, `title` y `detail`.
- Con Q11 b) las claves de `errors` se mantienen (`FirstName`, `Address.Street`, `Stands[1].StandName`).
- Nota sobre "como hace ASP.NET": el `ProblemDetails` nativo omite las propiedades `null` y usa `application/problem+json`. El operador dejó Q12 y Q13 con su valor por defecto (mantener `null` y `application/json`); BUILDER no los cambia salvo nueva respuesta.
- Un nombre de extensión no puede chocar con una propiedad estándar (`type`, `title`, `status`, `detail`, `instance`): hoy solo existen `ErrorTranslationKey` y `Errors`.

## Alcance de ficheros

| Fichero | Cambio |
| --- | --- |
| `Gaffer.Api/Shared/Exceptions/ExceptionToHttpResponseConverters.cs` | `Serialize` (L150): un único punto de serialización con camelCase y extensiones en la raíz (p. ej. un `JsonConverter` de Newtonsoft para `ProblemDetails` o un objeto plano equivalente; diseño de BUILDER, sin paquetes nuevos). |
| `Gaffer.Api/Shared/Exceptions/ExceptionHandlingMiddleware.cs`, `Gaffer.Api/Startups/ValidationStartup.cs` | Solo si hace falta para usar el mismo serializador (hoy ya llaman a `Serialize`). Con Q11 a): claves de campo de `ValidationStartup` (`FieldName`, L69) y del dominio (`DomainValidationErrors`) en camelCase por segmento, en un único punto. |
| Tests que leen el cuerpo de error con nombres en PascalCase (inventario de PLANNER, `grep -c '\["Extensions"\]\|\["Errors"\]\|\["Title"\]\|\["Status"\]'`): `Acceptance/PlayerFeaturesShould.cs` (10), `Acceptance/FacilityFeaturesShould.cs` (5), `Acceptance/SearchPlayersEndpointShould.cs` (3), `Acceptance/FacilityStandsFeaturesShould.cs` (2), `Acceptance/DatabaseFailureShould.cs` (1), `Unit/Exceptions/ExceptionHandlingMiddlewareShould.cs` (6), `Unit/Exceptions/ExceptionToHttpResponseConvertersShould.cs` (1); más los que deserializan con `TestJson.Options` y los tests nuevos del paso 01 (AC1.12, AC1.13) | Aserciones sobre los nombres exactos. |

Fuera: claves de traducción, códigos de estado, `/health/*` (texto plano), objeto `ProblemDetails` en memoria (los tests unitarios que leen `problemDetails.Extensions[...]` en C# no cambian), cliente generado (lo regenera el operador si AC2.6 lo exige).

## Criterios de aceptación

### Reparto por capa (OPT-037)

| Capa | Criterios | Casos exigidos |
| --- | --- | --- |
| Unitario (`Serialize`) | AC2.3 | **Matriz completa**: cada propiedad estándar, valor `null` (Q12), sin extensiones (no aparece ninguna clave extra ni `extensions`), `errorTranslationKey` sola, `errors` con clave simple, con punto (`Address.Street`) y con índice (`Stands[1].StandName`). |
| Middleware / model binding | AC2.4 | Un caso por camino: excepción convertida, excepción desconocida (500), 400 de model binding. |
| Aceptación (`TestFixture`) | AC2.1 | Un representante por tipo de error y recurso: 400 de validación (player, facility, `/stands`), 404 con clave, 400 de filtro no permitido, 500 por fallo de BD (`DatabaseFailureShould`). |
| Producción (`StartupTestServer`) | AC2.2 | Un 400 de dominio y un 400 de model binding. |

| ID | Criterio |
| --- | --- |
| AC2.1 | Aceptación: los cuerpos de error se leen como JSON **crudo** con nombres sensibles a mayúsculas (`JObject` o `JsonDocument`, no `TestJson.Options`) y contienen exactamente los nombres de la tabla de contrato: `errorTranslationKey` y `errors` en la raíz, ninguna propiedad `extensions`/`Extensions` y ninguna en PascalCase (`Title`, `Status`, `ErrorTranslationKey`, `Errors`). Rojo hoy → verde. |
| AC2.2 | Producción (`StartupTestServer`): mismo resultado para un 400 de dominio y un 400 de model binding (JSON mal formado). |
| AC2.3 | Unitario: matriz de la tabla de reparto en verde; un único punto de serialización de errores (`grep -rn "JsonConvert.SerializeObject" Gaffer.Api --include=*.cs` → solo `Serialize`, o 0 si se sustituye por un serializador configurado una vez). |
| AC2.4 | Middleware y `ValidationStartup` producen el mismo formato (un test por camino); el 500 desconocido mantiene `title` y `status` y no lleva `errorTranslationKey`. |
| AC2.5 | Los valores no cambian: claves `Error.*`, estados y textos de `title`/`detail` idénticos a los de antes en los tests existentes (solo cambia la ruta con que se leen). Tabla de tests ajustados en `STATUS.md` (fichero, nº de aserciones, antes → después). Ningún test se borra para ponerlo en verde. |
| AC2.6 | Contrato OpenAPI (OPT-032 + OPT-034): el diff toca `Startups/ValidationStartup.cs`, así que se compara el volcado de `ServedOpenApiDocumentDump` antes de editar y después. **Resultado esperado: sha256 idéntico** (no hay `ProducesResponseType` de error). Si **difiere**, BUILDER registra en `STATUS.md` el diff de esquemas y se activa el prerrequisito OPT-030 del operador (`00`). En ambos casos BUILDER no toca `Gaffer.Api.Client/`. |
| AC2.7 | Regresión: suite completa en verde (recuento antes/después), build 0 warnings, `git status` sin cambios en `Gaffer.Api.Client/`. |

## Doble bucle

1. **Aceptación**: AC2.1 y AC2.2 rojo (`n × estado` por clase: hoy fallan por nombres `Extensions`/PascalCase) → verde.
2. **Unitario**: AC2.3 y AC2.4 rojo → verde.
3. **Refactor**: un solo punto de serialización; AC2.5, AC2.6; revalidar suite y build.

## Cómo demostrarlo

- Reglas de `00`: rojo `n × estado` (OPT-038) con el comando de OPT-041; grep de usos de `Serialize`, `BuildValidationProblemDetails`, `FieldName` y `DomainValidationErrors` antes de cambiarlos (OPT-039).
- Un cuerpo de ejemplo antes/después por tipo de error en `STATUS.md` (sin datos sensibles).
- AC2.6: sha256 antes/después y, si difiere, rama OPT-030 anotada.

## Impacto fuera del código (para el operador y DOCUMENTER)

| Dónde | Impacto |
| --- | --- |
| `doc/features/error-handling.md`, `doc/features/technical/error-handling.md` (L107-111 muestran `"Title"` y `"Extensions": { "ErrorTranslationKey": ... }`) | DOCUMENTER los reescribe con la forma nueva tras `APPROVED`. |
| Otros documentos con `Extensions`/`ErrorTranslationKey` en ejemplos: `doc/features/player-update.md`, `player-search.md`, `facility-update.md`, `input-validation.md`; `doc/features/technical/facility-stands.md`, `facility-update.md`, `input-validation.md`, `player-search-and-persistence.md`, `player-update.md` | DOCUMENTER los actualiza (impacto indirecto). |
| `doc/features/technical/api-client-versioning.md` | Nota de la versión 3.0.0 (Q14). |
| `quionis-frontend` | En `src/` no hay lecturas del formato de error (solo `App.vue` de andamiaje): sin adaptación visible. |
| `quionis-bff-api` | El directorio no devolvió ficheros a `Glob`: no verificable desde este plan; lo comprueba el operador. |
| Cliente `Gaffer.Api.Client` | Si el OpenAPI no cambia, el código generado no cambia, pero se publica como 3.0.0 por el cambio en el cable (Q14). |
