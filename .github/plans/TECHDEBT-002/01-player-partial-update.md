# Paso 01 (ciclo 1) - Update parcial de Player conserva los campos omitidos

Skill aplicada: `api-and-interface-design` (cambio de contrato público: `UpdatePlayerRequest`).
Prerequisito: Q1 respondida para AC1.5 (el resto del paso no depende de Q1). Q2: se aplica a) (`PUT` parcial) salvo respuesta.

## Objetivo

`PUT /api/v1/players/{id}` actualiza solo los campos presentes en el cuerpo; los omitidos conservan su valor. En particular, enviar solo `firstName` no cambia `birthDate` ni `gender`.

## Contrato (antes → después)

| Campo de `UpdatePlayerRequest` | Hoy | Después |
| --- | --- | --- |
| Forma | Campos públicos | Propiedades `{ get; set; }` |
| `FirstName`, `LastName` | `string` | `string?` (ausente/`null`/`""` = sin cambio, como hoy) |
| `SecondLastName` | `string?` | `string?`, semántica según Q1 |
| `BirthDate` | `DateTime` (por defecto 0001-01-01) | `DateTime?` (ausente/`null` = sin cambio) |
| `Gender` | `Gender` (por defecto `MALE`) | `Gender?` (ausente/`null` = sin cambio) |

OpenAPI: los 5 campos quedan opcionales y anulables. El cliente NSwag lo regenera el operador al cerrar.

## Alcance de ficheros

| Fichero | Cambio |
| --- | --- |
| `Gaffer.Api/Controllers/PlayerController.cs` | `UpdatePlayerRequest` según la tabla; `Update` deja de convertir valores por defecto en strings. |
| `Gaffer.Api/Commands/UpdatePlayerCommandHandler.cs` | `UpdatePlayerCommand` con tipos anulables (recomendado `DateTime?`/`Gender?` en vez de `string`); `UpdatePlayer` solo aplica valores presentes. |
| `Gaffer.Api/Domain/Player.cs` | Solo si hace falta un setter tipado para la fecha/género. Los renombres de getters/setters son del paso 04 de TECHDEBT-003: no se adelantan. |
| `Gaffer.Api.Tests/Acceptance/PlayerFeaturesShould.cs` | Reforzar `UpdateAPlayer` y añadir escenarios. |
| `Gaffer.Api.Tests/Unit/Commands/UpdatePlayerCommandHandlerShould.cs` | Tests unitarios de campos omitidos. |
| `Gaffer.Api.Tests/Acceptance/OpenApiContractShould.cs` (o el test de contrato existente) | Aserción del esquema de `UpdatePlayerRequest`. |

## Criterios de aceptación

| ID | Criterio |
| --- | --- |
| AC1.1 | Crear un player con fecha y género conocidos (p. ej. `FEMALE`, fecha ≠ 0001-01-01) y enviar un `PUT` con cuerpo JSON **crudo** `{"firstName":"X"}` (vía `SendAsync`, no `UpdateAsync`, que serializa el objeto completo): 200 y el `GET` devuelve `firstName = "X"` y **los mismos** `lastName`, `secondLastName`, `birthDate` y `gender` que al crearlo. |
| AC1.2 | El mismo escenario con `birthDate` y `gender` a `null` explícito conserva ambos valores. |
| AC1.3 | Un `PUT` con todos los campos cambia todos (sin regresión del update completo). |
| AC1.4 | Unitario: `UpdatePlayerCommandHandler` con un comando que solo trae `FirstName` llama a `Update` con el `Player` cuyo resto de campos no cambia (fecha, género, apellidos). |
| AC1.5 | `SecondLastName` se comporta según la respuesta a Q1 (test de aceptación del caso elegido). |
| AC1.6 | El OpenAPI servido (`/swagger/v1/swagger.json`) declara `UpdatePlayerRequest.birthDate` y `.gender` como no requeridos y anulables. |
| AC1.7 | `PUT` a un id inexistente sigue devolviendo 404 con `Error.Players.Update.PlayerNotFound`. Suite completa en verde. |

## Doble bucle

1. **Aceptación**: AC1.1-AC1.3 y AC1.5 en `PlayerFeaturesShould`. Red esperado en AC1.1: `birthDate`/`gender` distintos de los creados → green.
2. **Unitario**: AC1.4 en `UpdatePlayerCommandHandlerShould`; AC1.6 en el test de contrato. Red → green.
3. **Refactor**: simplificar `UpdatePlayer` (sin conversiones string ↔ fecha/enum si el comando pasa a tipos anulables). Revalidar la suite completa.

## Cómo demostrarlo

- `dotnet test gaffer-api.sln --filter "FullyQualifiedName~PlayerFeaturesShould|FullyQualifiedName~UpdatePlayerCommandHandler|FullyQualifiedName~OpenApiContract"`: red y green en `STATUS.md`, con el mensaje del red de AC1.1.
- `dotnet test gaffer-api.sln --verbosity quiet` completo en verde (recuento antes/después).
- Diff de `PlayerController.cs`: sin `ToString()` de `BirthDate`/`Gender` sobre valores por defecto.
