# Paso 01 (ciclo 1) - Update parcial de Player conserva los campos omitidos

Skill aplicada: `api-and-interface-design` (cambio de contrato público: `UpdatePlayerRequest`).
Decisiones (ver `00-overview-and-open-questions.md`): Q1 = respuesta literal del operador ("el second last name puede ser null, si viene en la actualización, actualiza al valor, si no, dejalo null"): `secondLastName` se **reemplaza** siempre; Q2 = a) (`PUT` parcial, default aplicado). Sin preguntas abiertas para este paso.

## Objetivo

`PUT /api/v1/players/{id}` actualiza solo los campos presentes en el cuerpo; los omitidos conservan su valor. En particular, enviar solo `firstName` no cambia `birthDate` ni `gender`. **Excepción (Q1)**: `secondLastName` se reemplaza siempre: con valor se guarda ese valor; ausente, `null` o `""` se guarda `null`.

## Contrato (antes → después)

| Campo de `UpdatePlayerRequest` | Hoy | Después |
| --- | --- | --- |
| Forma | Campos públicos | Propiedades `{ get; set; }` |
| `FirstName`, `LastName` | `string` | `string?` (ausente/`null`/`""` = sin cambio, como hoy) |
| `SecondLastName` | `string?` (`null`/`""` = sin cambio; no se puede borrar) | `string?` con semántica de **reemplazo** (Q1): valor no vacío → se guarda ese valor; ausente/`null`/`""` → se guarda `null`. `""` ≡ `null` por la equivalencia actual (`IsNullOrEmpty`); sin recorte de espacios (no existe hoy). |
| `BirthDate` | `DateTime` (por defecto 0001-01-01) | `DateTime?` (ausente/`null` = sin cambio) |
| `Gender` | `Gender` (por defecto `MALE`) | `Gender?` (ausente/`null` = sin cambio) |

OpenAPI: los 5 campos quedan opcionales y anulables. El cliente NSwag lo regenera el operador al cerrar.

Efecto para clientes: un update que omita `secondLastName` lo deja a `null` (decisión del operador, Q1).

## Alcance de ficheros

| Fichero | Cambio |
| --- | --- |
| `Gaffer.Api/Controllers/PlayerController.cs` | `UpdatePlayerRequest` según la tabla; `Update` deja de convertir valores por defecto en strings. |
| `Gaffer.Api/Commands/UpdatePlayerCommandHandler.cs` | `UpdatePlayerCommand` con tipos anulables (recomendado `DateTime?`/`Gender?` en vez de `string`); `UpdatePlayer` aplica solo valores presentes, salvo `SecondLastName`, que siempre se asigna (`null` si viene vacío o ausente). |
| `Gaffer.Api/Domain/Player.cs` | Solo si hace falta un setter tipado para la fecha/género o que `SetSecondLastName` acepte `null`. Los renombres de getters/setters son del paso 04 de TECHDEBT-003: no se adelantan. |
| `Gaffer.Api.Tests/Acceptance/PlayerFeaturesShould.cs` | Reforzar `UpdateAPlayer` y añadir escenarios. |
| `Gaffer.Api.Tests/Unit/Commands/UpdatePlayerCommandHandlerShould.cs` | Tests unitarios de campos omitidos y de `SecondLastName`. |
| `Gaffer.Api.Tests/Acceptance/OpenApiContractShould.cs` (o el test de contrato existente) | Aserción del esquema de `UpdatePlayerRequest`. |

## Criterios de aceptación

| ID | Criterio |
| --- | --- |
| AC1.1 | Crear un player con fecha y género conocidos (p. ej. `FEMALE`, fecha ≠ 0001-01-01) y `secondLastName` no vacío, y enviar un `PUT` con cuerpo JSON **crudo** `{"firstName":"X"}` (vía `SendAsync`, no `UpdateAsync`, que serializa el objeto completo): 200 y el `GET` devuelve `firstName = "X"`, **los mismos** `lastName`, `birthDate` y `gender` que al crearlo, y `secondLastName = null` (Q1: ausente → `null`). |
| AC1.2 | El mismo escenario con `birthDate` y `gender` a `null` explícito conserva ambos valores. |
| AC1.3 | Un `PUT` con todos los campos (incluido `secondLastName` no vacío distinto del original) cambia todos (sin regresión del update completo). |
| AC1.4 | Unitarios de `UpdatePlayerCommandHandler` sobre un `Player` con `SecondLastName` no vacío: (a) comando que solo trae `FirstName` → llama a `Update` con el `Player` con `FirstName` nuevo, `LastName`, fecha y género sin cambios, y `SecondLastName = null`; (b) comando con `SecondLastName = "Nuevo"` → `SecondLastName = "Nuevo"` y el resto sin cambios; (c) comando con `SecondLastName = ""` → `SecondLastName = null`. |
| AC1.5 | Q1, por HTTP con cuerpo crudo, partiendo de un player con `secondLastName` no vacío (p. ej. `TestDefaultValues.DefaultSecondLastName`): (a) `{"secondLastName":"Nuevo"}` → 200 y el `GET` devuelve `secondLastName = "Nuevo"`; (b) `{"secondLastName":null}` → 200 y `secondLastName = null`; (c) `{"secondLastName":""}` → 200 y `secondLastName = null`. En los tres casos `firstName`, `lastName`, `birthDate` y `gender` no cambian. |
| AC1.6 | El OpenAPI servido (`/swagger/v1/swagger.json`) declara `UpdatePlayerRequest.birthDate` y `.gender` como no requeridos y anulables. |
| AC1.7 | `PUT` a un id inexistente sigue devolviendo 404 con `Error.Players.Update.PlayerNotFound`. Suite completa en verde. |

## Doble bucle

1. **Aceptación**: AC1.1-AC1.3 y AC1.5 en `PlayerFeaturesShould`. Red esperado en AC1.1: `birthDate`/`gender` distintos de los creados y `secondLastName` conservado; red esperado en AC1.5(b)/(c): `secondLastName` conserva el valor original (hoy `null`/`""` se ignoran) → green.
2. **Unitario**: AC1.4 en `UpdatePlayerCommandHandlerShould`; AC1.6 en el test de contrato. Red → green.
3. **Refactor**: simplificar `UpdatePlayer` (sin conversiones string ↔ fecha/enum si el comando pasa a tipos anulables). Revalidar la suite completa.

## Cómo demostrarlo

- `dotnet test gaffer-api.sln --filter "FullyQualifiedName~PlayerFeaturesShould|FullyQualifiedName~UpdatePlayerCommandHandler|FullyQualifiedName~OpenApiContract"`: red y green en `STATUS.md`, con el mensaje del red de AC1.1.
- `dotnet test gaffer-api.sln --verbosity quiet` completo en verde (recuento antes/después).
- Diff de `PlayerController.cs`: sin `ToString()` de `BirthDate`/`Gender` sobre valores por defecto.
- Si algún test existente dependía de conservar `secondLastName` en un update sin ese campo, BUILDER lo ajusta a Q1 y lo registra en `STATUS.md`.
