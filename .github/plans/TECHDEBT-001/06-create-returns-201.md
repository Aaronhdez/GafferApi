# Paso 06 - Create devuelve 201 Created

Prerrequisito: Q6 resuelta (a: `201` + `Location` + cuerpo = `Guid` creado). Depende del paso 05 (misma configuración MVC en `Startup`/`TestFixture`).

## Objetivo

`POST /api/v{version}/players` y `POST /api/v{version}/facilities` responden `201 Created` con la forma que diga Q6. El resto de endpoints no cambia. El contrato OpenAPI refleja el 201 para que el operador regenere el cliente.

## Alcance de ficheros

| Fichero | Cambio |
| --- | --- |
| `Gaffer.Api/Controllers/PlayerController.cs` | `Create` devuelve 201 (`CreatedAtAction(nameof(GetBy), new { id, version }, id)` o equivalente) y declara `[ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]`. |
| `Gaffer.Api/Controllers/FacilitiesController.cs` | Igual para `Create`. |
| `Gaffer.Api.Tests/TestShared/GafferApiClientTest.cs` | `PostAsync` acepta 201 como éxito (hoy solo 200). |
| `Gaffer.Api.Tests/Acceptance/PlayerFeaturesShould.cs`, `Gaffer.Api.Tests/Acceptance/FacilityFeaturesShould.cs` | Tests de 201 + `Location`; `FacilityFeaturesShould` L44 y L79 pasan de esperar 200 a 201. |
| `Gaffer.Api.Tests/Unit/Controllers/PlayerControllerShould.cs` (+ equivalente de Facilities si existe) | Adaptar al nuevo tipo de retorno. |

Fuera de alcance: `Gaffer.Api.Client` (lo regenera el operador).

## Criterios de aceptación

| ID | Criterio |
| --- | --- |
| AC6.1 | `POST /api/v1.0/players` con un cuerpo válido → `201 Created` (hoy 200). |
| AC6.2 | `POST /api/v1.0/facilities` con un cuerpo válido → `201 Created` (hoy 200). |
| AC6.3 | La cabecera `Location` apunta a `GET /api/v1.0/{players|facilities}/{id}` y un `GET` a esa URL devuelve 200 con el recurso creado. |
| AC6.4 | El cuerpo es el `Guid` creado (se lee igual que hoy). |
| AC6.5 | Los errores de validación de Create (400/422 de `FacilityFeaturesShould`) y el resto de códigos (GET/PUT/DELETE/search) no cambian. |
| AC6.6 | El documento OpenAPI que sirve la API (`/swagger/v1/swagger.json` en el `TestServer`) declara `201` para las dos operaciones Create. Se comprueba leyendo el documento servido, no `Gaffer.Api.Client`. |
| AC6.7 | `dotnet test gaffer-api.sln` en verde, 0 omitidos. |

## Doble bucle

1. **Aceptación**: tests AC6.1-AC6.3 y los cambios de L44/L79. Red (`expected 201 but found 200`) → green. `GafferApiClientTest.PostAsync` se adapta en el mismo paso (los tests de jugador que crean datos fallarán en rojo mientras tanto; anotarlo).
2. **Unitario**: `PlayerControllerShould` (y Facilities) comprueba que `Create` devuelve un resultado 201 con el id y la ruta de `GetBy`. Red → green.
3. **Refactor**: si hay duplicación entre los dos controladores, extraerla solo si es evidente; suite completa.

## Cómo demostrarlo

- `dotnet test gaffer-api.sln --filter "FullyQualifiedName~PlayerFeaturesShould|FullyQualifiedName~FacilityFeaturesShould|FullyQualifiedName~PlayerControllerShould|FullyQualifiedName~FacilitiesController"`: red y green en `STATUS.md`.
- Test o comando que lea el OpenAPI servido (AC6.6).
- Suite completa en verde.
- Nota de cierre en `STATUS.md`: el contrato OpenAPI cambia; el operador regenera `Gaffer.Api.Client` como 1.0.0 (paso 10) y revisa a sus consumidores.
