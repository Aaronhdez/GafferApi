# Paso 05 - Serialización solo con Newtonsoft y enums como texto

Prerrequisitos resueltos:
- Q4: "Ok, aunque los enums que tengan su valor textual." → camelCase, fechas UTC y enums como texto.
- Q5: "Obvialo" → `ExceptionHandlingMiddleware` fuera de alcance.

Depende de los pasos 03 (`Mvc.NewtonsoftJson` ya en 10.0.x) y 04 (este paso también cambia `nswag.json`).

Decisión del operador: "Serialización: Usar Newtonsoft".

## Objetivo

Que MVC tenga una sola configuración JSON, la de Newtonsoft, en `Startup` y en `TestFixture`, con propiedades en camelCase, fechas UTC y **enums serializados como su nombre** (`Gender`: p. ej. `"MALE"`; `FacilityType`: p. ej. `"Stadium"`). Es un cambio de contrato deliberado y rompe a los clientes actuales: el operador regenera el cliente como 1.0.0 (paso 10 y cierre).

## Alcance de ficheros

| Fichero | Cambio |
| --- | --- |
| `Gaffer.Api/Startup.cs` | Quitar `AddJsonOptions(...)` y los `using System.Text.Json*`. `AddNewtonsoftJson`: mantener `DateTimeZoneHandling.Utc` y añadir `StringEnumConverter` (Newtonsoft). Nombres de miembro de enum tal como están en el código (sin estrategia de nombres adicional). |
| `Gaffer.Api.Tests/TestFixture.cs` | El mismo cambio en el mismo incremento (lección de AGENTS.md). |
| `Gaffer.Api/nswag.json` | `defaultEnumHandling`: `Integer` → `String`, para que la generación del cliente coincida con el contrato (configuración, no salida generada). |
| `Gaffer.Api/Startups/SwaggerStartup.cs` | Solo si AC5.7 no se cumple por sí solo: configurar los documentos OpenAPI para que declaren los enums como texto. |
| `Gaffer.Api.Tests/TestShared/*`, `Gaffer.Api.Tests/Acceptance/*` | Las lecturas de respuestas con `ReadFromJsonAsync` (System.Text.Json en el lado del test) necesitan opciones con conversor de enums a texto; si no, fallarán al leer `"MALE"`. Adaptarlas sin cambiar lo que comprueban. |
| `Gaffer.Api.Tests/Acceptance/...` (nuevo, p. ej. `JsonContractShould.cs`) | Tests de caracterización y del nuevo contrato. |
| `Gaffer.Api.Tests/Unit/...` (nuevo, p. ej. `StartupSerializationShould.cs`) | Test de configuración de `Startup`. |

Fuera de alcance (Q5): `ExceptionHandlingMiddleware` y el formato de los cuerpos de error.

## Criterios de aceptación

| ID | Criterio |
| --- | --- |
| AC5.1 | Caracterización del comportamiento **antiguo**, antes de cambiar nada: tests que leen el JSON crudo (`ReadAsStringAsync` + `JObject`) de `GET /api/v1.0/players/{id}` y `GET /api/v1.0/facilities/{id}` fijan el casing de propiedades, el formato de `gender`/`type` y el de `birthDate`/`buildDate`, con los valores que devuelve hoy la API (previsto: camelCase y enums como entero). Se ejecutan en verde contra el código actual y el JSON crudo de ejemplo se copia en `STATUS.md`. |
| AC5.2 | Nuevo contrato de salida: el test de AC5.1 se cambia para esperar `gender` = `"MALE"`/`"FEMALE"`/... y `type` = `"Stadium"`/`"Arena"`/... (nombre exacto del miembro). Red contra el código actual (entero) → green. Casing y formato de fechas no cambian respecto a AC5.1. |
| AC5.3 | Entrada con texto: `POST /api/v1.0/players` con `"gender": "FEMALE"` y `POST /api/v1.0/facilities` con `"type": "Arena"` crean el recurso con ese valor (comprobado con un `GET`). |
| AC5.4 | Entrada con entero: se comprueba (no se da por supuesto) si `"gender": 1` y `"type": 1` se siguen aceptando con `StringEnumConverter` por defecto (`AllowIntegerValues = true`). BUILDER registra el resultado en `STATUS.md` y lo deja fijado en un test. Si **no** se aceptan, BUILDER no cambia el conversor para forzarlo: lo anota como hallazgo para el checkpoint humano. |
| AC5.5 | Caracterización de entrada: `PUT /api/v1.0/players/{id}` con `UpdatePlayerRequest` (campos públicos, no propiedades) sigue actualizando el jugador (protege frente a una vuelta accidental a System.Text.Json, que ignora campos). |
| AC5.6 | Tras `new Startup(config).ConfigureServices(services)` (con `DatabaseConfig:Enabled=false`) y construir el proveedor: `IOptions<Microsoft.AspNetCore.Mvc.JsonOptions>` no tiene conversores ni `PropertyNamingPolicy` configurados por la aplicación; `IOptions<MvcNewtonsoftJsonOptions>` tiene `DateTimeZoneHandling.Utc` y un `StringEnumConverter`. Red hoy → green. |
| AC5.7 | El documento OpenAPI servido por la API (`/swagger/v1/swagger.json` en el `TestServer`) declara `Gender` y `FacilityType` como enums de tipo `string` con los nombres de los miembros. Se lee el documento servido, no `Gaffer.Api.Client`. `nswag.json` tiene `defaultEnumHandling: "String"`. |
| AC5.8 | Ni `Startup.cs` ni `TestFixture.cs` contienen `AddJsonOptions` ni `using System.Text.Json`. |
| AC5.9 | Los cuerpos de error no cambian (tests que leen `Status`, `Title`, `Detail`, `Instance`, `Extensions` en verde sin tocarlos). |
| AC5.10 | `dotnet test gaffer-api.sln` en verde, 0 omitidos. |

## Doble bucle

1. **Aceptación**: AC5.1 en verde con el código actual (captura del comportamiento antiguo) → cambiar las expectativas a texto (AC5.2, red) → implementar → green. Después AC5.3, AC5.4 y AC5.5.
2. **Unitario**: AC5.6 red → green.
3. **Refactor**: quitar `using` sobrantes y unificar las opciones de lectura JSON de los tests si están duplicadas; suite completa.

## Cómo demostrarlo

- `dotnet test gaffer-api.sln --filter "FullyQualifiedName~JsonContract|FullyQualifiedName~StartupSerialization"`: green del comportamiento antiguo, red del nuevo contrato y green final en `STATUS.md`, con un JSON crudo de antes y otro de después.
- Resultado de AC5.4 (entero aceptado o no).
- Fragmento del OpenAPI servido para `Gender` (AC5.7).
- Suite completa en verde.
- Nota de cierre: el contrato cambia (enums como texto). El cliente se regenera como 1.0.0 (acción del operador).
