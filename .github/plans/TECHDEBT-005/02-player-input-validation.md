# Paso 02 (ciclo 2) - Validación de dominio de Player (y conversión común del 400)

Objetivo: `POST /api/v1/players` y `PUT /api/v1/players/{id}` rechazan con 400 los datos inválidos sin escribir en la BD. Las reglas viven en el dominio (`Player`: constructor y métodos `Set*`) y el middleware convierte la excepción (Q3 = b)). Este paso fija la excepción de validación, su conversión al formato de Q4 y, según Q19, el formato de los errores de model binding, que reutiliza el paso 03. Skills: `api-and-interface-design` (errores consistentes), `security-and-hardening` (no reflejar la entrada, límites de longitud).

**Bloqueo**: ninguno. Respuestas del operador: Q3, Q4, Q5, Q6, Q16 ("Validar solo al crear/actualizar") y Q17 ("Ausente/null no cambia"). Q18, Q19, Q21 y Q22 tienen valor por defecto (a)).

## Contrato

| Petición | Campo | Regla (Q5/Q6/Q22) |
| --- | --- | --- |
| `CreatePlayerRequest` | `FirstName`, `LastName` | Obligatorio; 1-100 caracteres; no solo espacios |
| | `SecondLastName` | Opcional; si llega: 1-100 caracteres, no vacío ni solo espacios |
| | `BirthDate` | Obligatoria; ≥ 1900-01-01; ≤ hoy (UTC); edad mínima 16 años (`BirthDate.AddYears(16) ≤ hoy UTC`, Q22) |
| | `Gender` | Valor definido del enum |
| `UpdatePlayerRequest` | `FirstName`, `LastName`, `BirthDate`, `Gender` | Ausente/`null` = no cambia. Si llega: misma regla que en create; `""` o solo espacios → 400 (hoy `""` se ignora) |
| | `SecondLastName` | Q17: ausente/`null` = no cambia (hoy lo borra); `""` o solo espacios → 400 (hoy `""` lo borra); otro valor → reglas de create. Sin forma nueva de borrarlo |

- Mecanismo (Q3): `Player` lanza una excepción de validación desde el camino de creación (constructor o factoría que usa `CreatePlayerCommandHandler`) y desde los `Set*` (PUT parcial). El PUT modifica el dominio a través de los setters, no con lógica duplicada en el controlador ni en el handler.
- Rehidratación (Q16): la lectura desde la BD (`SqlPlayerRepository` L22, Dapper) no valida; un player guardado que incumpla las reglas se sigue leyendo y buscando sin error. En el PUT solo se validan los campos enviados, así que un player antiguo con un campo inválido no enviado se puede actualizar.
- Cambio de comportamiento respecto a TECHDEBT-002 (Q17): tabla y tests afectados en `00`, sección "Cambio de comportamiento respecto a TECHDEBT-002".
- Respuesta (Q4): 400 `ProblemDetails` con `Extensions.ErrorTranslationKey` = `Error.Players.Create.Validation` (POST) o `Error.Players.Update.Validation` (PUT) y `Extensions.Errors` (campo → mensajes, Q18 a): todos los campos inválidos de la petición). Los mensajes no incluyen el valor recibido. La clave depende de la operación, no del tipo de excepción: el handler o la excepción aportan la operación (diseño de BUILDER).
- Model binding (Q19 a)): JSON mal formado, enum como texto desconocido y tipos incorrectos devuelven 400 con el mismo formato y la clave de la operación, mediante `InvalidModelStateResponseFactory` (configuración de la respuesta; sin atributos en los DTO), con mensajes genéricos sin el valor recibido.
- Orden (Q21 a)): en el `PUT` el handler lee el player primero, así que un id inexistente da 404 aunque el cuerpo sea inválido. Los errores de model binding (Q19) siguen siendo 400 antes del handler.
- Sin DataAnnotations nuevos en `CreatePlayerRequest`/`UpdatePlayerRequest` y sin `ProducesResponseType(400)` (no se pidió): el OpenAPI no debe cambiar (Q10).

## Alcance de ficheros

| Fichero | Cambio |
| --- | --- |
| `Gaffer.Api/Domain/Player.cs` | Reglas en el constructor (o factoría de creación, según Q16) y en `Set*`; excepción de validación con errores por campo. |
| `Gaffer.Api/Commands/CreatePlayerCommandHandler.cs`, `UpdatePlayerCommandHandler.cs` | Aportar la operación a la excepción; en el PUT, pasar al setter todo valor no `null` (hoy se ignoran `""` en L39-47) y, para `SecondLastName`, llamar al setter solo si no es `null` (hoy siempre, L49-50); no persistir si algún campo es inválido. |
| `Gaffer.Api/Infrastructure/SqlPlayerRepository.cs` | Solo si hace falta un camino de rehidratación sin validar (Q16). |
| `Gaffer.Api/Shared/Exceptions/` (`ExceptionToHttpResponseConverters.cs` y helper nuevo si hace falta) | Conversión de la excepción de validación a `ProblemDetails` con `ErrorTranslationKey` y `Errors`; reutilizable en el paso 03. |
| `Gaffer.Api/Startup.cs` (o `Startups/*`) | Solo Q19 a): `InvalidModelStateResponseFactory`. |
| Tests: `Acceptance/PlayerFeaturesShould.cs` (incluidos `UpdateAPlayer` L102-116 y `ReplaceTheSecondLastNameOnUpdate` L150-161, Q17), `Unit/Domain/` (Player), `Unit/Commands/UpdatePlayerCommandHandlerShould.cs` (L66-101, Q17) y `CreatePlayerCommandHandlerShould.cs`, `Integration/` del repositorio de players (AC2.9), `Unit/Exceptions/`, `Acceptance/OpenApiContractShould.cs`; `TestFixture.cs`/`TestServerDependenciesShould.cs` si cambia la configuración de MVC o DI; builders/valores de test (`TestDefaultValues`) que creen players inválidos según las reglas nuevas. | Tests. |

Fuera: facility y gradas (paso 03); cliente generado (Q10); datos ya guardados.

## Criterios de aceptación

| ID | Criterio |
| --- | --- |
| AC2.0 | Caracterización previa (antes de cambiar código), en `STATUS.md`: estado y cuerpo actuales de `POST`/`PUT` de player con nombre vacío, campo obligatorio ausente, fecha futura, `Gender` inválido como texto y como número no definido (p. ej. 99), y JSON mal formado; y del `POST` de facility sin dirección. Indica qué casos rechaza el model binding y cuáles llegan al handler. Resuelve los dos primeros puntos de "No verificado" de `00`. |
| AC2.1 | Aceptación `POST`: cada regla de Q5 incumplida (un caso por regla: nombre vacío, solo espacios, 101 caracteres en cada campo de texto, `SecondLastName: ""`, `BirthDate` 1899-12-31, mañana, y un día menos de 16 años, `Gender` número no definido) → 400 con `ErrorTranslationKey = Error.Players.Create.Validation`, el campo correcto en `Errors`, y el número de filas de `Players` no cambia. |
| AC2.2 | Aceptación `POST` en los límites: longitud 100, `BirthDate` = 1900-01-01 y = exactamente 16 años antes de hoy (UTC), `SecondLastName` ausente → 201 y el `GET` devuelve los valores enviados. |
| AC2.3 | Aceptación `PUT` parcial: un campo enviado inválido (`FirstName: ""`, `LastName: "   "`, `BirthDate` con menos de 16 años, `Gender` no definido) junto a otro válido → 400 `Error.Players.Update.Validation` y el `GET` del player no cambia (tampoco el campo válido). `PUT` con campos válidos y el resto ausente → mismo resultado que hoy (tests de TECHDEBT-002 en verde salvo los casos que cambia Q17, registrados en `STATUS.md`). `PUT` inválido sobre un id inexistente → 404 (Q21 a)). |
| AC2.4 | (Q17) `SecondLastName` en el PUT, aceptación sobre un player con segundo apellido: ausente (p. ej. `{"firstName":"X"}`) y `null` → 200 y el `GET` devuelve el segundo apellido original; `""` y solo espacios → 400 `Error.Players.Update.Validation` y no cambia ningún campo; `{}` → 200 y el `GET` es idéntico al de antes (no-op real). Los 4 tests de `00` ("Cambio de comportamiento respecto a TECHDEBT-002") se adaptan a esta semántica, cada cambio se lista en `STATUS.md` (antes → después) y no se debilita ninguna otra aserción. No existe ningún camino nuevo para borrar el segundo apellido. |
| AC2.5 | Formato (Q4, Q18, Q19): `ErrorTranslationKey` y `Errors` presentes; varios campos inválidos en una petición → todos en `Errors`; el cuerpo no contiene el valor rechazado (una cadena de 101 caracteres no aparece en la respuesta); JSON mal formado y `Gender` como texto desconocido → 400 con la clave de la operación y sin el valor recibido (Q19 a)). |
| AC2.6 | Contrato: el `swagger.json` servido es idéntico antes y después del paso. Si no lo es, se registra la diferencia en `STATUS.md` y se activa la acción del operador de `00` (Q10). La salida generada del cliente no se toca. **Procedimiento (OPT-032)**: si el diff del paso no toca controladores (`Gaffer.Api/Controllers/`), DTOs de petición/respuesta ni `Startup.cs`/`Startups/*Startup.cs`, el criterio se demuestra con `OpenApiContractShould` y `FacilitiesControllerContractShould` en verde, sin captura manual. Si los toca (p. ej. `InvalidModelStateResponseFactory` de Q19 a) en `Startup`), se compara el `swagger.json` servido mediante `StartupTestServer` (OPT-022) antes y después; no con `dotnet run`. `STATUS.md` indica qué rama se aplicó y por qué. |
| AC2.7 | Unitario: reglas de `Player` (constructor y cada `Set*`, casos válidos e inválidos por campo y en los límites, con un reloj o fecha de referencia controlable para "hoy" y los 16 años); handler de update: con un campo inválido no llama a `IPlayerRepository.Update`; conversión de la excepción a `ProblemDetails` con cada clave. |
| AC2.9 | (Q16) Datos antiguos: se inserta directamente por SQL en `Quionis_DB` un player que incumple las reglas (p. ej. `FirstName` de 150 caracteres, `SecondLastName` vacío y `BirthDate` con menos de 16 años) y: `GET /players/{id}` → 200 con los valores guardados; `POST /players/search` lo devuelve; `SqlPlayerRepository.Get` lo devuelve sin excepción (integración); un `PUT` que solo envía `LastName` válido → 200 y el resto de campos guardados no cambia. |
| AC2.8 | Regresión: suite completa en verde (recuento antes/después, con los tests adaptados listados en `STATUS.md`), incluidos `FacilityFeaturesShould` L101-153 y `SearchPlayersEndpointShould`; build 0 warnings; `TestServerDependenciesShould` en verde. |

## Doble bucle

1. **Aceptación**: AC2.0 (caracterización); AC2.1-AC2.5 rojo → verde; AC2.9 (caracterización: en verde antes del cambio y debe seguir en verde después).
2. **Unitario**: AC2.7 rojo → verde; AC2.6 comparación.
3. **Refactor**: un único punto de construcción del 400 de validación (excepción de dominio y model binding) y una excepción/colección de errores reutilizable por `Facility`, `Address` y `Stand` en el paso 03; revalidar suite y build.

## Cómo demostrarlo

- Tabla de AC2.0 en `STATUS.md`; filtro de los tests del paso en rojo y verde.
- AC2.6 según su procedimiento (OPT-032): tests de contrato en verde o comparación con `StartupTestServer`.
- Suite completa y build `--no-incremental -v q`.

## Notas

- "Hoy" se evalúa en UTC; los tests no dependen del día de ejecución (fecha de referencia controlable o fechas relativas a `DateTime.UtcNow.Date`).
- Si el diseño de Q16 exige cambiar la firma pública de `Player` (p. ej. factoría), se mantiene la compatibilidad con Dapper en la rehidratación.
- Documentación: el cambio de Q17 deja obsoletos `doc/features/player-update.md` y `doc/features/technical/player-update.md` (líneas en `00`); los actualiza DOCUMENTER tras `APPROVED`, no BUILDER.
