# REVIEW - TECHDEBT-002

## Ciclo 1 - Paso 01 (Update parcial de Player)

Veredicto: **APPROVED** (estado del plan → `APPROVED` para el paso 01).

### Alcance revisado

| Elemento | Resultado |
| --- | --- |
| `git status` | Modificados: `PlayerController.cs`, `UpdatePlayerCommandHandler.cs`, `Player.cs`, `PlayerFeaturesShould.cs`, `UpdatePlayerCommandHandlerShould.cs`, `OpenApiContractShould.cs`, ficheros del plan; nuevo `usage/BUILDER.md`. Todo dentro del alcance del paso 01. Sin cambios en `Gaffer.Api.Client` (no verificado su output generado, fuera de alcance). |
| DI / `TestFixture.cs` | Sin handlers nuevos; `UpdatePlayerCommandHandler` ya registrado (`TestFixture.cs:118`, `ActionsStartup.cs:21`). No requiere cambios. |
| MySQL | Acceptance usa `SqlPlayerRepository` real (`TestFixture.cs:110`) contra Docker `Gaffer.Database` (3306), activo. Ningún test omitido. |

### Ejecución del REVIEWER

| Comando | Resultado |
| --- | --- |
| `docker ps` | `Gaffer.Database 0.0.0.0:3306->3306/tcp` |
| `dotnet build gaffer-api.sln --verbosity quiet` | 0 errores |
| F1: `dotnet test gaffer-api.sln --no-build --filter "FullyQualifiedName~PlayerFeaturesShould\|FullyQualifiedName~UpdatePlayerCommandHandler\|FullyQualifiedName~OpenApiContract"` | 26/26 superados, 0 omitidos |
| `dotnet test gaffer-api.sln --no-build --verbosity quiet` | 171/171 superados, 0 omitidos (línea base 159 + 12 nuevos) |

### Criterios de aceptación

| ID | Evidencia | Red → green | Estado |
| --- | --- | --- | --- |
| AC1.1 | `PlayerFeaturesShould.UpdateAPlayer`: PUT crudo `{"firstName":"X"}` vía `SendAsync`; GET verifica `lastName`, `birthDate`, `gender` originales (FEMALE, 1994-02-04) y `secondLastName = null`. | Red en STATUS ("Expected player.BirthDate to be <1994-02-04>, but found <0001-01-01>") → green | Demostrado |
| AC1.2 | `KeepBirthDateAndGenderWhenUpdatedWithExplicitNulls`. | Red 400 "Error converting value {null} to type 'System.DateTime'" / Gender → green | Demostrado |
| AC1.3 | `UpdateEveryFieldWhenAllFieldsAreSent` (incluye `secondLastName` distinto). | Guarda de regresión (verde desde el inicio, registrado) | Demostrado |
| AC1.4 | `UpdateAPlayerWhileBeingExecuted`, `KeepOmittedFieldsAndClearSecondLastNameWhenOnlyFirstNameIsSent` (a), `ReplaceTheSecondLastNameAndKeepTheRest` ("Nuevo"/""/null) (b)(c). | Red de compilación CS0029 (comando tipado) → green | Demostrado |
| AC1.5 | `ReplaceTheSecondLastNameOnUpdate` ×3 (crudo, "Nuevo"/null/""), resto de campos sin cambio. | Red (b)(c) "SecondLastName to be <null>, but found \"Doe\"" → green | Demostrado |
| AC1.6 | `OpenApiContractShould.DeclareUpdatePlayerRequestFieldAsOptionalAndNullable` (birthDate, gender) sobre `/swagger/v1/swagger.json`: no en `RequiredProperties` y `IsNullable(SchemaType)` = true. | Red 2 fallos + control negativo (revertir tipos → 2/2 fallan) → green | Demostrado |
| AC1.7 | `ReturnNotFoundWhenUpdatingAnUnknownPlayer`: 404 + `Error.Players.Update.PlayerNotFound`; suite 171/171. | Guarda de regresión | Demostrado |

Diff de `PlayerController.cs`: sin `ToString()` de `BirthDate`/`Gender`; `using System.Globalization` eliminado. Cumple "Cómo demostrarlo".

### Hallazgos

| # | Severidad | Hallazgo |
| --- | --- | --- |
| 1 | No bloqueante | AC1.6: el documento es Swagger 2.0 sin `x-nullable`; la nulabilidad se demuestra por la regla implícita de NJsonSchema (no requerido ⇒ anulable en Swagger 2.0). Coherente con cómo NSwag generará el cliente; si en el futuro se migra a OpenAPI 3, el test debería revisar `nullable`. |
| 2 | No bloqueante | `Player.SetSecondLastName(string? commandFirstName)` mantiene un nombre de parámetro engañoso y se conservan los setters string (`SetBirthDateName(string)`, `SetGender(string)`) solo usados por `PlayerShould`. Renombres/limpieza pertenecen a TECHDEBT-003 paso 04; correcto no adelantarlos. |
| 3 | No bloqueante (preexistente) | `PlayerFeaturesShould.ThrowExceptionIfPlayerIsNotFound` no hace `await` de la aserción `ThrowAsync` (patrón preexistente). La cobertura real del 404 la aporta ahora `ReturnNotFoundWhenUpdatingAnUnknownPlayer`. |
| 4 | Informativo | Cambio de contrato visible para clientes: un PUT que omita `secondLastName` lo borra (decisión Q1 del operador). El cliente NSwag lo regenera el operador. |

Sin defectos bloqueantes.
