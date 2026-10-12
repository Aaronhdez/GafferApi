# Paso 03a (ciclo 3) - Validación de dominio de Facility y dirección (create/update)

Objetivo: `POST /api/v1/facilities` y `PUT /api/v1/facilities/{id}` rechazan con 400 los datos inválidos de la facility y de su dirección sin escribir en la BD. Las reglas viven en los caminos de creación de `Facility` y `Address` (Q3 "Solo dominio", Q16), con la excepción, la conversión y el formato del paso 02 (`DomainValidationErrors`, `DomainValidationException`, `ValidationFailedException`, `ValidationErrorKeys`). Las gradas (anidadas y sub-recurso `/stands`) son el paso 03b. Incluye el ayudante permanente de volcado del `swagger.json` (OPT-034). Skills: `api-and-interface-design`, `security-and-hardening`.

Origen: división del antiguo paso 03 (OPT-036, opción del operador "Facility / gradas"). La parte "y la corrección de la divergencia" de esa opción **no se aplica**: P2-2 / OPT-035, "Obviemos este punto". No se cambian `TestFixture` ni `Startup` por la divergencia `AddControllers`/`AddMvcCore`.

**Bloqueo**: paso 02 `APPROVED` (cumplido, ciclo 2). Q8, Q16 respondidas; Q18-Q21 con valor por defecto (a)). Q23 respondida ("Sí, incluirlo", 2026-10-12): sin bloqueos por preguntas.

## Contrato

| Objeto de dominio (petición) | Campo | Regla (Q8) |
| --- | --- | --- |
| `Facility` (`CreateFacilityRequest`, `UpdateFacilityRequest`) | `Name` | Obligatorio; 1-100 caracteres; no solo espacios |
| | `BuildDate` | ≥ 1800-01-01; ≤ hoy (UTC) |
| | `Type` | Valor definido del enum |
| | `Capacity` | ≥ 0, sin máximo |
| | `Address` | Obligatoria (ya lo exige el dominio, `Facility.cs` L52) |
| `Address` (`Create/UpdateFacilityAddressRequest`) | `Street`, `City`, `Region`, `Country` | Obligatorio; 1-100; no solo espacios |
| | `StreetNumber`, `PostalCode` | Obligatorio; 1-20; no solo espacios; sin formato por país |

- Claves (Q4): `Error.Facilities.Create.Validation` (POST), `Error.Facilities.Update.Validation` (PUT; **cambia** respecto a hoy, que devuelve la clave de create en el update para la dirección ausente).
- `Errors` (Q18 a)): todos los campos inválidos de facility y dirección; la dirección como `Address.<Campo>`.
- Orden (Q21 a)): `PUT /facilities/{id}` construye la facility dentro del lock (`UpdateFacilityCommandHandler` L36, L44-65) → id inexistente = 404 aunque el cuerpo sea inválido; el error de validación dentro del lock deshace la transacción.
- Atributos existentes (Q20 a)): `[Required]` en `Address` y `[MaxLength(4)]` en `Stands` se mantienen; sin atributos nuevos ni `ProducesResponseType(400)`.
- `FacilityValidationException` se reutiliza o se sustituye por la excepción del paso 02, manteniendo el `Detail` actual de dirección ausente (y de máximo de gradas, que verifica 03b).
- Rehidratación (Q16): la lectura desde la BD (`SqlFacilityRepository` L26, L73, L83) no valida.
- Divergencia conocida no corregida (OPT-035): el host de `TestFixture` (`AddControllers`) evalúa `[Required]`, `[MaxLength(4)]` y el "required" implícito de las propiedades de referencia no anulables; producción (`AddMvcCore`) no. Hoy `POST` sin dirección: producción 500 (`NullReferenceException` en `FacilitiesController.cs` L49/L79, antes de llegar al dominio), tests 400 (`STATUS.md` ciclo 2, tabla AC2.0).

## Alcance de ficheros

| Fichero | Cambio |
| --- | --- |
| `Gaffer.Api.Tests/` (fichero nuevo, p. ej. `Acceptance/ServedOpenApiDocumentDump.cs`) | Ayudante permanente `[Explicit]` de volcado del `swagger.json` (OPT-034, AC3a.0). |
| `Gaffer.Api/Domain/Facility.cs` | Reglas de `Facility` y `Address` en los caminos de creación (constructor o factoría, Q16). |
| `Gaffer.Api/Commands/CreateFacilityCommandHandler.cs`, `UpdateFacilityCommandHandler.cs` | Operación de la clave; acumulación de errores (Q18 a)). |
| `Gaffer.Api/Shared/Exceptions/ExceptionToHttpResponseConverters.cs`, `ValidationFailedException.cs` | Claves `Error.Facilities.Create.Validation` / `Error.Facilities.Update.Validation`. |
| `Gaffer.Api/Startups/ValidationStartup.cs` | Registrar `FacilitiesController.Create/Update` para el formato de Q19 a) (explicitado: lo exige AC3a.5 y el diseño del paso 02 lo dejó para el paso 03). |
| `Gaffer.Api/Controllers/FacilitiesController.cs` | (Q23 "Sí, incluirlo") **Solo** el mapeo de `Create`/`Update` tolera la dirección ausente (sin `NullReferenceException`); sin atributos, rutas ni DTO nuevos. El dominio devuelve 400 `Error.Facilities.Create/Update.Validation`. |
| `Gaffer.Api/Infrastructure/SqlFacilityRepository.cs` | Solo si hace falta un camino de rehidratación sin validar (Q16). |
| Tests: `Acceptance/FacilityFeaturesShould.cs`, `Unit/Domain/` (Facility/Address), `Unit/Commands/` (handlers de facility), `Unit/Exceptions/`, `Integration/SqlFacilityRepositoryShould.cs` (Q16), `Acceptance/OpenApiContractShould.cs`; builders/valores de test | `FacilityFeaturesShould` L118 (dirección ausente) pasa a exigir 400 con la clave exacta. |

Fuera: reglas de `Stand`, gradas anidadas y `/stands` (03b); `TestFixture.cs` y `Startup.cs` por la divergencia (OPT-035); migraciones; datos guardados; cliente generado.

## Criterios de aceptación

Numeración: `AC3a.N` conserva el sufijo del antiguo `AC3.N` (correspondencia en `00`). AC3.3 y AC3.6 pasan enteros a 03b.

| ID | Criterio |
| --- | --- |
| AC3a.0 | (OPT-034) Ayudante permanente: test `[Explicit]` de nombre estable en `Gaffer.Api.Tests` que escribe el `swagger.json` servido por `StartupTestServer` (OPT-022) en la ruta indicada por una variable de entorno (nombre elegido por BUILDER y anotado en `STATUS.md` con el comando exacto). Sin la variable falla con un mensaje claro y no escribe nada. No se ejecuta en la suite normal (el recuento de `dotnet test` se anota antes/después, incluido si aparece como omitido). Se crea **antes** de cualquier cambio de producto y sirve para la captura "antes" de AC3a.8; dos ejecuciones sobre el mismo árbol dan el mismo sha256. Lo reutilizan 03b (AC3b.8) y 05 (AC5.6); no se crea ni se borra ninguna sonda temporal. |
| AC3a.1 | Aceptación `POST /facilities`: cada regla incumplida de facility y dirección (un caso por regla, incluidos vacío, solo espacios, longitud máx. + 1, `BuildDate` 1799-12-31 y mañana, `Type` no definido, `Capacity` negativa, dirección ausente) → 400 `Error.Facilities.Create.Validation` con el campo correcto (`Address.<Campo>` en la dirección); `GET /facilities` no cambia. |
| AC3a.2 | Aceptación `PUT /facilities/{id}`: igual que AC3a.1 con `Error.Facilities.Update.Validation` (incluida la dirección ausente, que hoy devuelve la clave de create); el `GET` de la facility (campos, dirección y gradas) no cambia. `PUT` inválido sobre un id inexistente → 404 (Q21 a)). Las reglas de fusión de TECHDEBT-003 y el lock de TECHDEBT-004 siguen en verde. |
| AC3a.4 | Límites aceptados: longitudes máximas exactas (100 y 20), `Capacity = 0`, `BuildDate` = 1800-01-01 y = hoy (UTC) → 201/204 y el `GET` devuelve lo enviado. |
| AC3a.5 | Formato y seguridad: varios campos inválidos de facility y dirección → todos en `Errors`; el cuerpo no contiene el valor rechazado; errores de model binding en `POST`/`PUT /facilities` (JSON mal formado, enum de texto desconocido) → formato de Q4 con la clave de la operación (Q19 a)). |
| AC3a.11 | (OPT-035) Evidencia de producción en los casos divergentes: dirección ausente y campos de texto de facility/dirección ausentes o `null` (required implícito o `[Required]` en el host de `TestFixture`), en `POST` y `PUT`. Se demuestran con una prueba de aceptación sobre `StartupTestServer` (pipeline de producción, `AddMvcCore`) o sobre el host real: 400 con la clave y el campo de Q3/Q4/Q8, nunca 500 ni `NullReferenceException`, y sin escritura. Los tests con `TestFixture` solos **no** bastan. Si el mensaje por campo difiere entre hosts (model binding frente a dominio), se registra en `STATUS.md`; clave y campo deben coincidir. |
| AC3a.7 | Unitario: reglas de `Facility` y `Address` (válidos, inválidos y límites por campo); handlers de create/update: con datos inválidos no escriben (el update deshace la transacción); conversión con las claves de create/update en `ExceptionToHttpResponseConvertersShould` o en el test del 400 de validación. |
| AC3a.10 | (Q16) Datos antiguos: se inserta directamente por SQL una facility que incumple las reglas (p. ej. `Name` de 150 caracteres, `Capacity` negativa, `BuildDate` 1700-01-01, dirección con `PostalCode` de 30 caracteres) y: `GET /facilities/{id}` y la búsqueda → 200 con los valores guardados; `SqlFacilityRepository` la lee sin excepción (integración); un `PUT /facilities/{id}` con todos los campos válidos → 204. |
| AC3a.8 | Contrato: el `swagger.json` servido es idéntico antes y después (Q20 a)); `FacilitiesControllerContractShould` L76-81 sin cambios. Si hay diferencias, se registran y se activa la acción del operador de `00` (Q10). Cliente generado sin tocar. **Procedimiento (OPT-032 + OPT-034)**: si el diff no toca controladores, DTOs ni `Startup.cs`/`Startups/*Startup.cs`, bastan `OpenApiContractShould` y `FacilitiesControllerContractShould` en verde. Si los toca (previsto: `Startups/ValidationStartup.cs` y `FacilitiesController.cs`, Q23), se compara el volcado del ayudante de AC3a.0 antes y después (`cmp`/sha256); no con `dotnet run` ni con una sonda temporal. `STATUS.md` indica la rama aplicada y por qué. |
| AC3a.9 | Regresión: suite completa en verde (recuento antes/después, tests adaptados listados en `STATUS.md`), build 0 warnings, `grep -rn "CancellationToken.None" Gaffer.Api --include=*.cs` → 0. |

## Doble bucle

0. **Preparación**: AC3a.0 (ayudante) y captura "antes" de AC3a.8.
1. **Aceptación**: AC3a.1, AC3a.2, AC3a.4, AC3a.5 y AC3a.11 rojo → verde; AC3a.10 caracterización (verde antes y después).
2. **Unitario**: AC3a.7 rojo → verde; AC3a.8 comparación.
3. **Refactor**: reglas de texto compartidas (longitud, solo espacios) entre `Player`, `Facility` y `Address`; revalidar suite y build.

## Cómo demostrarlo

- Filtro de los tests del paso en rojo y verde; AC3a.11 con su filtro propio sobre `StartupTestServer` (sin `dotnet run`).
- AC3a.8 según su procedimiento, con el ayudante de AC3a.0.
- Suite completa y build `--no-incremental -v q`.
- OPT-036: calcular las fechas de calendario de los casos (`BuildDate` límite, "hoy" y "mañana" UTC) antes de escribir los tests y anotarlas en `STATUS.md`.
- Documentar el ayudante en `.github/doc/common/test-stack.md` (OPT-034) no lo hace BUILDER (fuera de su matriz): lo hace DOCUMENTER al cierre del plan (cambio de herramienta de test) o el operador.
