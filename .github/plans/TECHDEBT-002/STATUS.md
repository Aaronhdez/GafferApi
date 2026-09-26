# STATUS - TECHDEBT-002

| Campo | Valor |
| --- | --- |
| Estado | APPROVED |
| Ciclo | 1 |
| Pasos completados | 01 (aprobado por REVIEWER en el ciclo 1) |
| Pasos bloqueados | Ninguno. Q0-Q6 y Q4.1 resueltas (Q0, Q2, Q5 por default; Q1 = reemplazo de `secondLastName` según 2ª respuesta del operador; Q4.1 = `"Internal Server Error"`). |
| Bloqueos | Ninguno de entorno. |
| Aprobación del plan | Aprobado por el operador el 2026-09-26: "I approve the plan". Listo para el ciclo 1 (BUILDER, paso 01). |

## Secuenciación (un paso = un ciclo)

En el ciclo N, BUILDER implementa solo el paso N (más las correcciones de REVIEWER del ciclo anterior, si las hay) y REVIEWER solo aprueba el paso de ese ciclo. Los ciclos 5 y 6 son reserva para `NEEDS_CHANGES`; cómo se combina una corrección con el paso siguiente: Q0 = b) (default aplicado; ver `00-overview-and-open-questions.md`).

| Ciclo | Paso previsto | Veredicto |
| --- | --- | --- |
| 1 | 01 - Update parcial de Player | APPROVED |
| 2 | 02 - Fallos de infraestructura → 500 (Q3 = a) | - |
| 3 | 03 - Filtro no permitido en Facilities → 400 | - |
| 4 | 04 - Log en el middleware | - |
| 5 | Reserva | - |
| 6 | Reserva | - |

## Tiempos por agente

| Agente | Ciclo | Inicio (UTC) | Fin (UTC) |
| --- | --- | --- | --- |
| PLANNER | 0 | n/a | n/a |
| BUILDER | 1 | 2026-09-26T20:29:17Z | 2026-09-26T20:35:13Z |
| REVIEWER | 1 | 2026-09-26T20:36:13Z | 2026-09-26T20:37:28Z |

## Prerrequisitos

| Comprobación | Resultado |
| --- | --- |
| Línea base (dato del operador, a revalidar por BUILDER) | Build 0 errores; `dotnet test` 159/159 contra Docker `Gaffer.Database` (3306) |
| Ciclo 1 - Docker | `docker ps`: `Gaffer.Database 0.0.0.0:3306->3306/tcp` activo |
| Ciclo 1 - línea base revalidada | `dotnet build gaffer-api.sln --verbosity quiet`: 0 errores; `dotnet test gaffer-api.sln --verbosity quiet --no-build`: 159/159 |

## Comandos y resultados

Filtro F1 = `dotnet test gaffer-api.sln --filter "FullyQualifiedName~PlayerFeaturesShould|FullyQualifiedName~UpdatePlayerCommandHandler|FullyQualifiedName~OpenApiContract"`.

| Ciclo | Paso | Fase | Comando / filtro | Resultado |
| --- | --- | --- | --- | --- |
| 1 | 01 | Aceptación red | `dotnet test ... --filter FullyQualifiedName~PlayerFeaturesShould` | 5 fallos / 12. AC1.1 `UpdateAPlayer`: "Expected player.BirthDate to be <1994-02-04>, but found <0001-01-01 00:00:00.000>". AC1.2: 400 "Error converting value {null} to type 'System.DateTime'" / "Cannot convert null value to Gaffer.Api.Shared.Gender". AC1.5(a): birthDate 0001-01-01; AC1.5(b)/(c): "Expected player!.SecondLastName to be <null>, but found \"Doe\"". AC1.3 y AC1.7 en verde desde el inicio (guardas de regresión). |
| 1 | 01 | Contrato red (AC1.6) | `--filter FullyQualifiedName~OpenApiContract` | 2 fallos: "Expected required {\"firstName\", \"lastName\", \"birthDate\", \"gender\"} to not contain \"birthDate\"" (idem `gender`). |
| 1 | 01 | Unitario red (AC1.4) | `dotnet build gaffer-api.sln --verbosity quiet` | Red de compilación: CS0029 en `UpdatePlayerCommandHandlerShould.cs` (DateTime/Gender → string) por el comando tipado. |
| 1 | 01 | Green | F1 | 26/26. |
| 1 | 01 | Control negativo AC1.6 | `UpdatePlayerRequest` revertido temporalmente a `DateTime`/`Gender` no anulables; `--filter ~DeclareUpdatePlayerRequestFieldAsOptionalAndNullable` | 2/2 fallan ("RequiredProperties {\"birthDate\", \"gender\"} to not contain ..."); restaurado, F1 26/26. |
| 1 | 01 | Refactor + suite completa | `dotnet build gaffer-api.sln --verbosity quiet`; `dotnet test gaffer-api.sln --no-build --verbosity quiet` | 0 errores; 171/171 (159 + 12 nuevos). |
| 1 | 01 | Revisión (REVIEWER) | `docker ps`; `dotnet build gaffer-api.sln --verbosity quiet`; F1 `--no-build`; `dotnet test gaffer-api.sln --no-build --verbosity quiet` | Docker activo; 0 errores; F1 26/26; suite 171/171, 0 omitidos. Veredicto APPROVED (ver `REVIEW.md`). |

## Notas del ciclo 1 (paso 01)

| Tema | Detalle |
| --- | --- |
| Contrato | `UpdatePlayerRequest`: propiedades `string?` ×3, `DateTime?`, `Gender?`. `UpdatePlayerCommand`: `DateTime?`/`Gender?` (sin conversiones string). Controller sin `ToString()` de `BirthDate`/`Gender`; eliminado `using System.Globalization`. |
| Handler | Solo aplica valores presentes; `SecondLastName` siempre se asigna (`null` si ausente/`null`/`""`, Q1). |
| `Player.cs` | `SetSecondLastName(string?)`; sobrecargas tipadas `SetBirthDateName(DateTime)` y `SetGender(Gender)`. Sin renombres (paso 04 de TECHDEBT-003). Setters string antiguos se conservan (usados por `PlayerShould`). |
| AC1.6 | El documento servido es Swagger 2.0 (`definitions`), sin `x-nullable`. El test usa la semántica de NSwag/NJsonSchema: `OpenApiDocument.FromJsonAsync` + `JsonSchemaProperty.IsNullable(document.SchemaType)` (en Swagger 2.0, no requerido ⇒ anulable) y `RequiredProperties` sin el campo. |
| Tests ajustados | `UpdatePlayerCommandHandlerShould.ThrowExceptionWhenPlayerNotFound` ahora hace `await` y espera `CannotUpdatePlayerNotFoundException` (antes no esperaba la aserción). Ningún test existente dependía de conservar `secondLastName`. |
| Fuera de alcance | Cliente NSwag no regenerado (operador). Los avisos de compilación (26) son preexistentes en otros ficheros (CS8618/CS8604/CS8602/CS8625). |

## Documentación modificada

Pendiente (DOCUMENTER).
