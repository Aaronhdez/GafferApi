# Paso 01 (ciclo 1) - Limpieza, operación, CI, observabilidad y `null` en `stands` (sin contrato)

Objetivo: retirar código muerto o redundante (4, 6, 8, 9), cerrar la deuda de operación y observabilidad (7, 5, log del health check, test de 5 s), corregir el elemento `null` dentro de `stands` (hoy previsiblemente 500) y añadir el test HTTP "4 gradas + 5 enviadas". **Sin cambio del contrato OpenAPI**; los únicos cambios observables son los logs nuevos y que un 500 pasa a 400. Fusiona los grupos orientativos 1 y 2 de ORCHESTRATOR y lo que quedó del antiguo paso 02 tras Q8 "Dejarlo como está" (ver `00`, justificación). Skill de BUILDER para AC1.12-AC1.14: `security-and-hardening` (entrada no válida).

**Bloqueo**: ninguno. Q1-Q7 y Q9 aplican su valor por defecto de `00`. Con Q3 = c) no se toca `ci.yml`; con Q4 = a) no se toca código para el punto 5. Q8 = "Dejarlo como está": **no se cambian `TestFixture` ni el registro MVC** (OPT-035 sigue sin abordarse); los casos divergentes se demuestran en producción con `StartupTestServer`, como en TECHDEBT-005.

## Alcance de ficheros

| Fichero | Cambio | Punto |
| --- | --- | --- |
| `Gaffer.Api/Program.cs` | Quitar `SetConfigurationProviders`, su llamada (L31), `ConfigurationBuilderExtensions.AddDevelopmentSettings` y el `config` sin uso de `CreateProductContext`. Con Q4 = b): aviso de arranque. | 4, 5 |
| `Gaffer.Api/Infrastructure/SqlFacilityRepository.cs` | `Delete` sin `DeleteStands`/`DeleteAddress` (solo si AC1.3 lo confirma); `CriteriaType.NONE` de L214. | 6, 9 |
| `Gaffer.Api/Shared/Repositories/IPlayerRepository.cs`, `Gaffer.Api/Commands/DeleteFacilityCommandHandler.cs`, `DeletePlayerCommandHandler.cs`, `UpdatePlayerCommandHandler.cs` | Con Q2 = a): quitar `NONE` y sus asignaciones. | 9 |
| `Gaffer.Api/Infrastructure/DatabaseHealthCheck.cs`, `Gaffer.Api/Startups/DatabaseStartup.cs` | `ILogger` en la comprobación y en su factoría de registro (Q5). | Log |
| `.github/workflows/ci.yml` | Solo con Q3 = a) o b): L17 con la etiqueta elegida. | 7 |
| `Gaffer.Api/Controllers/FacilitiesController.cs` | Mapeo de `Stands` (L55, L85) sin `NullReferenceException` ante un elemento `null`; la ausencia llega al dominio/handler, que responde según Q9 a). Sin atributos, rutas ni DTO nuevos. | `null` |
| `Gaffer.Api/Domain/Facility.cs`, `Commands/CreateFacilityCommandHandler.cs`, `Commands/UpdateFacilityCommandHandler.cs` | Solo si hace falta para producir el error de Q9 a) con el formato existente (`DomainValidationErrors`/`ValidationFailedException`). | `null` |
| Tests: `Acceptance/Features_Refactor/*` (se borran con Q1), `Acceptance/SearchPlayersEndpointShould.cs` (casos migrados con Q1 = a)), `Unit/Repositories/SqlFacilityRepositoryShould.cs` (caracterización del borrado y `minimumCalls` de L496), `Unit/HealthChecks/DatabaseHealthCheckShould.cs` (log y Q6), `Integration/HostingEnvironmentShould.cs` (Q4 = b)), `Integration/DatabaseStartupShould.cs`, `Acceptance/FacilityFeaturesShould.cs` (4+5 y `null`), `Acceptance/` con `StartupTestServer` (`null` en producción), `Unit/Domain/FacilityShould.cs`, `Unit/Commands/*FacilityCommandHandlerShould.cs`, y `TestFixture.cs`/`Integration/TestServerDependenciesShould.cs` **solo** si cambia un registro de DI (logger del health check) | Tests. | Todos |

Fuera: el registro MVC de `TestFixture` y `Startup` (Q8), DTO y atributos, otros controladores, formato de errores (paso 02), cliente generado.

## Criterios de aceptación

### Reparto por capa del `null` en `stands` (OPT-037)

| Capa | Criterios | Casos exigidos |
| --- | --- | --- |
| Dominio / unitario | AC1.14 | Matriz completa: `null` en posición primera, intermedia y última; create y update; combinado con otro error de grada (todos los errores en una respuesta, TECHDEBT-005 Q18). |
| Handlers | AC1.14 | Un caso por operación (create, update). |
| Aceptación (`TestFixture`) | AC1.12, AC1.13 | Un representante por operación para `null`; un caso 4+5. |
| Producción (`StartupTestServer`) | AC1.12 | `null` en create y update (caso divergente: el 500 se da en producción). |

| ID | Criterio |
| --- | --- |
| AC1.1 | (4) `grep -rn "SetConfigurationProviders\|AddDevelopmentSettings" Gaffer.Api --include=*.cs` → 0; `CreateProductContext` sin `ConfigurationBuilder`. `HostingEnvironmentShould` sigue en verde sin cambiar sus aserciones y un test (existente o nuevo) demuestra que el host sigue leyendo un valor de `appsettings.json` antes y después (caracterización verde-verde). |
| AC1.2 | (5) Con Q4 = b): un test demuestra que, sin `ASPNETCORE_ENVIRONMENT` o en blanco, el arranque registra un `Warning` que nombra la variable y el valor aplicado (`Development`), y que con la variable definida no se registra; el entorno resultante no cambia (aserciones de `HostingEnvironmentShould` intactas). Con Q4 = a): sin cambio de código; `STATUS.md` lo anota para DOCUMENTER. |
| AC1.3 | (6) Caracterización con la BD real **antes** de quitar nada: un test de integración/repositorio crea un facility con dirección y gradas, ejecuta solo `DELETE FROM Facilities WHERE Id = @Id` y comprueba que quedan 0 filas del facility en `Stands`, `Facilities_Stands`, `Addresses` y `Facilities_Addresses`, y que otro facility no se ve afectado. Verde antes del cambio = permiso para AC1.4. **Si sale rojo**: no se quita nada, `STATUS.md` registra el resultado `n × estado` y el punto queda `BLOCKED` para decisión del operador (el resto del paso sigue). |
| AC1.4 | (6) Con AC1.3 en verde: `Delete` ejecuta solo el borrado de `Facilities` dentro de su transacción; `DeleteStands`/`DeleteAddress` desaparecen si quedan sin uso. Los tests existentes de borrado de facility (repositorio y aceptación `DELETE /facilities/{id}`) siguen en verde; `PassItsTokenToTheProviderWhenDeleting` ajusta `minimumCalls` al nuevo número de llamadas (anotado en `STATUS.md`). El test de AC1.3 queda permanente (fija la dependencia del trigger y las FK). |
| AC1.5 | (8) Con Q1 = a): cada caso de `SearchPlayersFeatureShould` tiene equivalente por HTTP (tabla caso → test en `STATUS.md`): por id = `PlayerFeaturesShould.GetAPlayer`; sin filtros, filtro `Id`, `FirstName` y `LastName` = tests nuevos en `SearchPlayersEndpointShould` con `GET` y filtros de query. Los nuevos se ejecutan en verde; `Acceptance/Features_Refactor/` no existe (`Glob` → 0). Con Q1 = b): solo el borrado y la tabla de cobertura que queda (repositorio/controlador). |
| AC1.6 | (9) Con Q2 = a): `grep -rn "CriteriaType.NONE\|\bNONE\b" Gaffer.Api Gaffer.Api.Tests --include=*.cs` → 0 (antes, el grep de usos de OPT-039 anotado con 5 coincidencias en producto). Tests de búsqueda, actualización y borrado de players y facilities en verde sin cambiar aserciones (el SQL no cambia porque el criterio único usa `WHERE`). |
| AC1.7 | (7) Con Q3 = a) o b): `grep -n "image:" .github/workflows/ci.yml` muestra la etiqueta elegida y ninguna `latest`; el resto del workflow sin cambios (`git diff --stat` solo L17). La ejecución real en GitHub Actions es acción del operador (`00`). Con Q3 = c): `ci.yml` sin cambios. |
| AC1.8 | (Log) Unitario con un `ILogger` sustituto o de captura: con motivo `Sql`, la sonda que **lanza** produce un log del nivel de Q5 con la excepción; la que **agota el tiempo** y la que devuelve **`false`** producen un log sin excepción; `Healthy`, `Degraded` (configuración) y `Unhealthy` por fallback no registran nada (Q5 a)). El `HealthCheckResult` y el cuerpo HTTP no cambian (aserciones de seguridad actuales de `DatabaseHealthCheckShould` intactas). El mensaje del log no incluye la cadena de conexión. |
| AC1.9 | (5 s) Con Q6 = a): ningún test espera el `DefaultTimeout` real; la comprobación de "abandona por tiempo" usa un timeout inyectado corto y `DefaultTimeout == 5 s` se afirma sin esperar. Tiempo de la clase `DatabaseHealthCheckShould` antes/después en `STATUS.md` (debe bajar unos 5 s). |
| AC1.10 | Contrato (OPT-032 + OPT-034): el diff toca `Program.cs`, `Startups/DatabaseStartup.cs` y `Controllers/FacilitiesController.cs`, así que se compara el volcado de `ServedOpenApiDocumentDump` antes de editar y después: sha256 idéntico (incluye `required` de `Address` y `maxItems` de `Stands`). `OpenApiContractShould` y `FacilitiesControllerContractShould` en verde. `STATUS.md` indica la rama. |
| AC1.11 | Regresión: suite completa en verde con recuento antes/después explicado (665 − 5 de `Features_Refactor` + nuevos); build `--no-incremental` 0 warnings; `TestServerDependenciesShould` en verde; `git diff --stat` sin cambios en el registro MVC de `TestFixture.cs` (Q8); `git status` sin cambios en `Gaffer.Api.Client/`. |
| AC1.12 | (`null`) Caracterización previa en `STATUS.md`: estado y cuerpo de producción (`StartupTestServer`) para `POST /facilities` y `PUT /facilities/{id}` con `"stands": [null]` y con `null` entre dos gradas válidas. Después del cambio: 400 con `Error.Facilities.Create.Validation` / `Error.Facilities.Update.Validation` y `Errors["Stands[i]"]` (Q9 a)) **en producción** y con `TestFixture`; nada se guarda (create) ni cambia (update: `GET` posterior igual al anterior). |
| AC1.13 | (4+5) Test HTTP nuevo: facility con 4 gradas; `PUT /facilities/{id}` con 5 gradas → 400 con `Errors.Stands` (decisión del operador "confirmo el 400", TECHDEBT-005 ciclo 4) y el facility sin cambios (`GET` posterior). Como `TestFixture` evalúa `[MaxLength(4)]` y producción no (Q8), el mismo caso se ejecuta también con `StartupTestServer` para demostrar el 400 del dominio. |
| AC1.14 | (`null`) Unitario/dominio: la matriz de la tabla de reparto en verde; los handlers no llaman al repositorio con un `null` (create) ni guardan (update). |

## Doble bucle

1. **Aceptación**: AC1.1, AC1.3 y AC1.5 son caracterización (verde antes y después); AC1.2 (Q4 = b)) y los nuevos HTTP de AC1.5 rojo → verde si el comportamiento falta, o verde inmediato registrado como caracterización si ya existe. AC1.12 rojo (500 en producción) → verde; AC1.13 rojo o verde inmediato (si ya da 400, se registra como caracterización con el origen del 400 en cada pipeline).
2. **Unitario**: AC1.8 y AC1.14 rojo → verde; AC1.9 y AC1.6 con la suite afectada.
3. **Refactor**: quitar código muerto (AC1.1), borrados (AC1.4) y `NONE` (AC1.6); revalidar suite, build y AC1.10.

## Cómo demostrarlo

- Reglas de `00`: rojo `n × estado` (OPT-038) con el comando definido una vez (OPT-041); grep de usos antes de cambiar `CriteriaType`, `DatabaseHealthCheck` o `ConfigurationBuilderExtensions` (OPT-039).
- AC1.3: salida filtrada del test con la BD real y conteo por tabla en `STATUS.md`.
- AC1.5: tabla caso → test.
- AC1.10: sha256 antes/después.
- Suite completa y build `--no-incremental -v q`.

## Notas

- Con 10 puntos, es el paso más ancho del plan (fusión pedida para ahorrar ciclos; ver `00`). BUILDER puede ordenar los puntos como prefiera dentro del doble bucle, pero AC1.3 va antes que AC1.4 y la caracterización de AC1.12 antes de tocar el controlador. Los tests nuevos leen el cuerpo de error como los existentes; el paso 02 cambia los nombres a camelCase.
- Q7 (`/health/ready` anónimo y conexión por petición) no tiene criterio en este paso.
