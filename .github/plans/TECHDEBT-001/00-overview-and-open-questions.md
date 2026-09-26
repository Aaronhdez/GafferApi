# TECHDEBT-001 - Resumen, alcance y preguntas abiertas

## Petición del operador (literal)

> "vale, genera la planificación para estos puntos de desuda técnica:
> Paquetes sobrantes o desalineados / Varios paquetes 9.x conviven con net10. / NSwag / Credenciales root/toor escritas en 4 sitios. Propón alguna mejora para esto. / Serialización: Usar Newtonsoft / Seguridad y respuestas HTTP: la autenticación por cookie está activada, pero no se exige autorización en ningún endpoint. Create devuelve 200 en lugar de 201. / Ficheros que no deberían estar versionados: los .log de la raíz y gaffer-api.sln.DotSettings.user. / Restos sin uso: LoggingStartup está vacío (la plantilla de arranque crea ahora su propio logger aparte) y quedan clases de resultado sin uso. / Tabla mal escrita: se llama Adresses; corregirlo requiere una migración."

Petición adicional (literal): "Regenera el cliente con nueva version 1.0.0, ajusta el proceso para que use Semantic Versioning"

## Objetivo

Saldar exactamente esos 8 puntos de deuda técnica sin cambiar comportamiento observable salvo donde el operador lo pide (201 en Create, tabla `Addresses`, enums como texto por Q4) y dejar el proceso de versionado del cliente en Semantic Versioning con versión 1.0.0. La regeneración del cliente la hace el operador (AGENTS.md). Línea base (dada por el ORCHESTRATOR, a revalidar por BUILDER): `dotnet test gaffer-api.sln` → 127/127 contra Docker `Gaffer.Database` (MariaDB, 3306, `GafferDB` y `GafferDB_Test`).

## Pasos y dependencias

| Paso | Fichero | Punto de deuda | Bloqueado por | Depende de |
| --- | --- | --- | --- | --- |
| 01 | `01-untracked-files-gitignore.md` | Ficheros que no deberían estar versionados | - | - |
| 02 | `02-unused-leftovers.md` | `LoggingStartup` vacío y clases sin uso | - | - |
| 03 | `03-packages-cleanup-and-alignment.md` | Paquetes sobrantes y 9.x en net10 | - | 02 |
| 04 | `04-nswag-net10-configuration.md` | NSwag en `Net90` con TFM net10 | - | 03 |
| 05 | `05-newtonsoft-only-serialization.md` | Serialización solo con Newtonsoft; enums como texto | - | 03, 04 (cambia `nswag.json`) |
| 06 | `06-create-returns-201.md` | Create devuelve 200 en vez de 201 | - | 05 |
| 07 | `07-authentication-authorization.md` | Autenticación por cookie sin autorización | - | 03, 06 |
| 08 | `08-database-credentials.md` | Credenciales en el código y la configuración versionada | - | 03 |
| 09 | `09-rename-adresses-table.md` | Tabla `Adresses` | - | 08 |
| 10 | `10-client-semantic-versioning.md` | Proceso de versionado SemVer del cliente, versión 1.0.0 | Q13 (solo publicación de cierre) | 04, 05, 06 |
| 11 | `11-final-refactor-and-regression.md` | Refactor transversal y regresión completa | - | 01-10 |

Orden elegido para minimizar el riesgo: limpieza sin comportamiento primero (01-04), después cambios de contrato HTTP (05-07), luego configuración de credenciales (08, que cambia cómo arrancan migraciones y tests), el cambio de esquema (09), que necesita el runner de migraciones ya configurado, y el versionado del cliente (10) cuando el contrato ya está cerrado.

Cada paso sigue el doble bucle de `.github/doc/work-guidelines.md`: aceptación → unitario → refactor con revalidación. Si el paso cambia comportamiento, BUILDER registra en `STATUS.md` la evidencia red y green (comando + resultado). Si es limpieza pura sin comportamiento, la evidencia es: build sin errores, sin advertencias nuevas y regresión completa en verde, con recuento antes/después.

BUILDER no empieza un paso con una pregunta bloqueante abierta; puede avanzar con los pasos no bloqueados. Si no queda ningún paso ejecutable, marca `BLOCKED`.

## Verificación de los hallazgos (PLANNER, solo lectura)

| Punto | Evidencia en el código | Confirmado |
| --- | --- | --- |
| Paquetes | `Gaffer.Api/Gaffer.Api.csproj`: `MySql.Data` 26.7.0 lo usa `SqlConnectionProvider`; `MySqlConnector` 2.6.1 no se usa en `Gaffer.Api` (solo `Gaffer.Api.Migrations`, que tiene su propia referencia). Sin uso en código: `ClosedXML` (solo `using DocumentFormat.OpenXml.*` sueltos en `IPlayerRepository.cs`, `SqlPlayerRepository.cs` y también en `Gaffer.Api.Tests/Unit/Commands/UpdatePlayerCommandHandlerShould.cs`), `AspNetCore.HealthChecks.*` (llamada comentada en `Startup`), `Microsoft.ApplicationInsights.AspNetCore`, `Microsoft.VisualStudio.Web.CodeGeneration.Design`. `Microsoft.AspNetCore.Authentication.JwtBearer` 9.0.9 sin uso (solo cookie). `using Microsoft.Identity.Client` suelto en `Program.cs`. Paquetes 9.x: JwtBearer 9.0.9, Mvc.NewtonsoftJson 9.0.18, `Microsoft.Extensions.Configuration.Binder/UserSecrets/DependencyInjection.Abstractions/Options` 9.0.9. Bloque de `PackageReference` comentado. Ítems `bin\Debug\net10.0\...` en `Gaffer.Api.csproj` (2) y `Gaffer.Api.Tests.csproj` (1). `Gaffer.Api.Tests.csproj` referencia `MySql.Data` pero ningún test usa sus tipos directamente. | Sí |
| NSwag | `Gaffer.Api.csproj` target `NSwag` usa `$(NSwagExe_Net90)`; `nswag.json` `"runtime": "Net90"` con `"targetFramework": "net10.0"`. `NSwag.MSBuild` 14.6.1 (caché NuGet local) define `NSwagExe_Net100`. El target solo corre en Windows con configuración distinta de Debug y escribe en `Gaffer.Api.Client`. | Sí |
| Credenciales | No son 4 sitios sino 8 apariciones en 7 ficheros: `Gaffer.Api/appsettings.Development.json`, `Gaffer.Api.Migrations/Program.cs` (x2), `Gaffer.Api.Tests/SqlTest.cs`, `Gaffer.Api.Tests/testSettings.json`, `Gaffer.Api.Tests/TestShared/TestDefaultValues.cs`, `Gaffer.Api.Tests/Integration/DatabaseStartupShould.cs` (x2, una apunta al puerto 1 a propósito), `Gaffer.Api.Tests/Unit/ConnectionProviders/SqlConnectionProviderShould.cs` (puerto 1 a propósito). | Sí (ampliado) |
| Serialización | `Startup.cs` L36-44 y `TestFixture.cs` L86-94: `AddJsonOptions` (camelCase + `JsonStringEnumConverter`) y `AddNewtonsoftJson` (`DateTimeZoneHandling.Utc`). `ExceptionHandlingMiddleware.WriteErrorResponse` usa `JsonConvert.SerializeObject` sin opciones (PascalCase; los tests leen `Status`, `Title`, `Extensions`). | Sí; comportamiento actual: ver "No verificado" |
| Seguridad/HTTP | `AuthenticationStartup` registra cookie; `Startup` llama `AddAuthorization`, `UseAuthentication`, `UseAuthorization`; ningún `[Authorize]`; `.RequireAuthorization()` comentado en `Startup` y `TestFixture`. `TestStartup` no usa autenticación. `PlayerController.Create` y `FacilitiesController.Create` devuelven `Task<Guid>` (200). `GafferApiClientTest.PostAsync` solo acepta 200; `FacilityFeaturesShould` L44 y L79 esperan 200 en Create; `PlayerControllerShould` L105 comprueba el `Guid` devuelto. `using Microsoft.AspNetCore.Authentication.Cookies` suelto en `RepositoriesStartup.cs`. | Sí |
| Ficheros versionados | Raíz: `build.log`, `test.log`, `facility-test.log`, `gaffer-api.sln.DotSettings.user`. `.gitignore` no tiene `*.log` ni `*.user`. | Sí (presencia en disco; que estén en el índice lo afirma el ORCHESTRATOR) |
| Restos | `LoggingStartup.ConfigureLogging` vacío y llamado en `Startup` L29; `Startup` L52-57 crea un `LoggerFactory` de arranque propio. Sin referencias (incluidos tests y `Gaffer.Api.Client`): `DeletePlayerResult`, `DeleteFacilityResult`, `UpdatePlayerResult` (`UpdatePlayerCommand : ICommand<UpdatePlayerResult>, ICommand`), `DeletePlayerRequest`, `ExceptionHandlingMiddleware.GetProblemDetails` (privado). | Sí |
| Tabla | `M0002_CreateFacilitiesSchema.cs`: `Create.Table("Adresses")`, FK `FK_Facilities_Addresses_Adresses`, trigger `TRG_Facilities_BeforeDelete` con `DELETE FROM Adresses`. Uso: `SqlFacilityRepository.cs` L66, L165, L238; `Gaffer.Api.Tests/TestShared/DbTableNames.cs` L7; `SqlFacilityRepositoryShould.cs` L68, L265, L337. Documento `.github/doc/features/facility-create.md` L25. Los tests no ejecutan migraciones: el esquema de `GafferDB_Test` lo crea el runner `Gaffer.Api.Migrations`. | Sí |

## No verificado

- Serializador activo hoy: se infiere (no se ha ejecutado) que MVC ya usa los formateadores de Newtonsoft, porque `AddNewtonsoftJson` sustituye los de System.Text.Json. Indicios: `UpdatePlayerRequest` usa campos públicos (System.Text.Json los ignora por defecto) y los tests leen `Gender`/`Type` con `ReadFromJsonAsync` sin conversor de enum a texto. Si es así, hoy la salida es camelCase con enums como entero, y `AddJsonOptions` no tiene efecto en los formateadores de MVC. El paso 05 lo confirma con tests de caracterización antes de cambiar nada.
- Que los 4 ficheros de la raíz estén en el índice de Git (no se ejecutó `git ls-files`).
- Línea base 127/127 y recuento de advertencias de build: los debe medir BUILDER antes de cambiar nada.
- Si al quitar paquetes se pierde la referencia implícita al framework ASP.NET Core: `Gaffer.Api` usa `Microsoft.NET.Sdk` (no `.Web`) y es probable que obtenga `Microsoft.AspNetCore.App` de forma transitiva desde paquetes como `Mvc.NewtonsoftJson`. Ver paso 03.
- Si `global.json` (`9.0.0`, `rollForward: latestMajor`) cuenta como "desalineado": ver Q2.

## Preguntas resueltas (respuestas del operador)

Respuesta literal: "1. Ok / 2. Ok / 3. Ok / 4. Ok, aunque los enums que tengan su valor textual. / 5. Obvialo / 6. Ok. / 7. ¿Motivo? / 8. Ok. / 9. Ok. / 10. Ok. / 11. Ok. / 12. Ok." Sobre Q7, después de que el ORCHESTRATOR explicara el motivo: "Ok, lo podemos quitar".

| ID | Respuesta literal | Significado aplicado | Estado |
| --- | --- | --- | --- |
| Q1 | "Ok" | a) Se quita `MySqlConnector` de `Gaffer.Api` y se mantiene `MySql.Data`. | Resuelta |
| Q2 | "Ok" | b) `global.json` fijado en 10.0.x (la versión instalada que mida BUILDER) con `rollForward: latestFeature`. | Resuelta |
| Q3 | "Ok" | b) BUILDER ejecuta `dotnet-nswag` Net100 hacia una carpeta temporal fuera del repositorio y registra solo el código de salida; nunca hace un build Release de `Gaffer.Api`. | Resuelta |
| Q4 | "Ok, aunque los enums que tengan su valor textual." | camelCase con Newtonsoft, fechas UTC y **enums serializados como texto** (p. ej. `"MALE"`, `"Stadium"`), no como entero. Es un cambio de contrato deliberado y rompe a los clientes actuales. La entrada con enteros se sigue aceptando solo si `StringEnumConverter` lo permite por defecto (`AllowIntegerValues = true`); se comprueba con un test, sin darlo por supuesto. El OpenAPI debe declarar los enums como texto. | Resuelta |
| Q5 | "Obvialo" | Fuera de alcance: `ExceptionHandlingMiddleware` sigue serializando ProblemDetails igual que hoy. | Resuelta |
| Q6 | "Ok." | a) `201` + `Location` a `GET .../{id}` + cuerpo = el `Guid` creado. | Resuelta |
| Q7 | "¿Motivo?" → "Ok, lo podemos quitar" | a) Se borra `LoggingStartup` y su llamada en `Startup`. | Resuelta |
| Q8 | "Ok." | a) Se quita la autenticación por cookie sin uso y JwtBearer; la API queda anónima de forma explícita. | Resuelta |
| Q9 | "Ok." | A + B + E en este plan; D (usuario no root) y el cambio de contraseña, acciones opcionales del operador. | Resuelta |
| Q10 | "Ok." | b) BUILDER ejecuta el runner solo en `GafferDB_Test`; el operador migra `GafferDB`. | Resuelta |
| Q11 | "Ok." | b) También se recrea la FK como `FK_Facilities_Addresses_Addresses`. | Resuelta |
| Q12 | "Ok." | b) `Down` reversible, revisado sin ejecutarlo; no se amplía el runner. | Resuelta |
| Q14 | "Q14. Opción B" | b) `Directory.Build.props` nuevo en la raíz con `GlobalPackageVersion` = `1.0.0` por defecto (solo si viene vacío; `/p:GlobalPackageVersion=` sigue siendo override) + target MSBuild de validación SemVer 2.0.0. `Gaffer.Api.Client.csproj` no cambia. Como la pregunta trataba la versión del paquete `Gaffer.Api.Client`, propiedad y target se condicionan a ese proyecto y no afectan a los otros 3 (ver paso 10, AC10.9-AC10.11 y Q16). | Resuelta |
| Q15 | "Q15. a)+c)" | a) `infoVersion` de `nswag.json` alineada con la versión del paquete (1.0.0) y sigue la misma regla SemVer. c) DOCUMENTER documenta la regla y el procedimiento en `doc/features/technical/` (p. ej. `api-client-versioning.md`) y añade una línea en `.github/doc/common/dev-stack.md`, "Build and Packaging Notes". | Resuelta |

## Preguntas abiertas (petición de versionado del cliente)

Verificado (solo lectura): `Gaffer.Api.Client.csproj` tiene `<Version>$(GlobalPackageVersion)</Version>` y `GeneratePackageOnBuild` solo en Release. `GlobalPackageVersion` no se define en ningún fichero del repositorio (no hay `Directory.Build.props`, `NuGet.config` ni workflows de CI). Si nadie pasa `/p:GlobalPackageVersion=...`, `Version` queda vacío y el SDK aplica su valor por defecto (1.0.0; inferido, no verificado). `nswag.json` tiene `infoVersion: "1.0.0"` e `infoTitle: "My Title"`. Ningún proyecto de `quionis-bff-api` ni de `quionis-frontend` referencia `Gaffer.Api.Client`. El proyecto cliente no referencia a `Gaffer.Api`, así que compilarlo o empaquetarlo por separado no dispara el target `NSwag`.

| ID | Paso | Pregunta | Opciones | Recomendación PLANNER (no vinculante) | Bloquea |
| --- | --- | --- | --- | --- | --- |
| Q13 | 10 (cierre) | ¿Cómo se publica y consume el paquete `Gaffer.Api.Client` (feed NuGet, carpeta local, referencia de proyecto) y quién pasa hoy `GlobalPackageVersion`? ¿Se publicó ya alguna vez una versión 1.0.0 (por el valor por defecto) que chocaría con la nueva 1.0.0? | a) El operador lo indica y el paso 10 lo documenta. b) La publicación queda fuera del plan: solo se fija la versión y la regla, y el operador publica como hasta ahora. | b): la publicación es acción del operador y no hay rastro de ella en el repositorio. Si ya existe una 1.0.0 publicada, el operador decide si usar otra versión o sustituirla. | No bloquea el cambio de configuración; sí la acción de cierre |
| Q16 | 10 | Un `Directory.Build.props` en la raíz se importa en los 4 proyectos (`Gaffer.Api`, `Gaffer.Api.Client`, `Gaffer.Api.Migrations`, `Gaffer.Api.Tests`). ¿La versión por defecto y la validación SemVer deben aplicarse solo a `Gaffer.Api.Client` o a todos? | a) Solo a `Gaffer.Api.Client` (condición `'$(MSBuildProjectName)' == 'Gaffer.Api.Client'`); los demás proyectos no cambian. b) A todos: todos los ensamblados llevan la misma versión y se validan (cambia `Version`/`AssemblyVersion`/`FileVersion` de API, Migrations y Tests). | a): Q14 trata la versión del paquete del cliente y b) añade un cambio no pedido. El paso 10 está redactado con a). | No bloquea: se aplica a) salvo que el operador elija b) antes de que BUILDER empiece el paso 10 |

Q14 y Q15 resueltas (ver tabla de preguntas resueltas).

## Preguntas (propuesta original, ya resueltas arriba)

| ID | Paso | Pregunta | Opciones | Recomendación PLANNER (no vinculante) | Bloquea |
| --- | --- | --- | --- | --- | --- |
| Q1 | 03 | ¿Qué driver MySQL se queda en `Gaffer.Api`? | a) Quitar solo `MySqlConnector` de `Gaffer.Api` (no se usa) y mantener `MySql.Data`. b) Migrar `SqlConnectionProvider` a `MySqlConnector` (el mismo que usa Migrations) y quitar `MySql.Data` de API y tests. | a): es limpieza sin cambio de comportamiento. b) cambia el driver de toda la persistencia y merece su propio plan. | 03 |
| Q2 | 03 | ¿Se alinea `global.json` (`9.0.0`, `rollForward: latestMajor`) con .NET 10? | a) Dejarlo. b) Fijarlo en `10.0.x` (p. ej. la versión instalada, 10.0.302 según BUGFIX-001) con `rollForward: latestFeature`. | b) si el operador y el equipo ya usan el SDK 10; si no, a). | 03 (solo esa parte) |
| Q3 | 04 | ¿Cómo se demuestra que NSwag funciona con `Net100` sin tocar `Gaffer.Api.Client`? | a) Solo comprobación estática (`Net100` en csproj y `nswag.json`) y el operador ejecuta la generación al cerrar. b) BUILDER ejecuta `dotnet-nswag` de `tools/Net100` con `/variables:Configuration=Debug,ClientOutputPath=<carpeta temporal fuera del repo>` (sobre un build Debug, porque `nswag.json` tiene `noBuild: true`) y registra solo el código de salida, sin revisar lo generado. | b): da evidencia ejecutable sin escribir ni auditar el cliente generado. Condición: BUILDER nunca ejecuta `dotnet build -c Release` sobre `Gaffer.Api` (el target escribiría en `Gaffer.Api.Client`). | 04 |
| Q4 | 05 | ¿Qué contrato JSON queda al usar solo Newtonsoft? | a) Conservar el comportamiento actual medido en el paso 05 (previsto: camelCase, enums como entero, fechas UTC). b) Enums como texto (lo que pretendía la configuración de System.Text.Json con `JsonStringEnumConverter`), añadiendo `StringEnumConverter` a Newtonsoft. | a): coincide con `nswag.json` (`defaultEnumHandling: Integer`) y con el cliente actual. b) cambia el contrato y obliga a regenerar el cliente. | 05 |
| Q5 | 05 | ¿Entra en el alcance el `JsonConvert.SerializeObject` directo de `ExceptionHandlingMiddleware` (ProblemDetails en PascalCase, `Extensions` anidado)? | a) Fuera de alcance: se deja igual. b) Serializar con los ajustes Newtonsoft de MVC (camelCase), lo que cambia el cuerpo de todos los errores y los tests que leen `Status`/`Title`/`Extensions`. | a): el operador pidió "usar Newtonsoft" y el middleware ya usa Newtonsoft. b) es un cambio de contrato de errores. | 05 (solo si se elige b) |
| Q6 | 06 | Forma de la respuesta 201 de `POST /players` y `POST /facilities`. | a) `201` + cabecera `Location` a `GET .../{id}` + cuerpo = el mismo `Guid` que hoy (`CreatedAtAction`). b) `201` + `Location`, sin cuerpo. c) `201` + cuerpo objeto (`{ "id": ... }`). | a): es el cambio mínimo; solo cambian el código y la cabecera. | 06 |
| Q7 | 02 | ¿Qué se hace con `LoggingStartup`? | a) Borrarlo y quitar la llamada en `Startup` (el logging lo dan `Host.CreateDefaultBuilder` y el logger de arranque de BUGFIX-001). b) Convertirlo en la configuración real de logging, moviendo ahí el `LoggerFactory` de arranque de `Startup.ConfigureServices`. | a): el operador lo describe como resto sin uso; b) añade diseño nuevo que nadie ha pedido. | 02 (solo `LoggingStartup`) |
| Q8 | 07 | ¿Qué política de autenticación y autorización se quiere? No lo dice la petición. | a) Quitar la autenticación por cookie sin uso (`AuthenticationStartup`, `AddAuthorization`, `UseAuthentication`, `UseAuthorization`, `.RequireAuthorization()` comentado, paquete JwtBearer): la API queda anónima de forma explícita. b) Exigir autorización con JWT Bearer: falta emisor, audiencia, claves y qué endpoints; requisitos que no existen hoy. c) Mantener cookie y exigir autorización: sin endpoint de login, todas las peticiones redirigen o devuelven 401 y la API queda inutilizable. d) Otro esquema (API key, etc.). | a) en este plan, y un plan aparte para la autenticación real (b o d) cuando haya requisitos. Si se elige b, el operador debe dar emisor, audiencia, origen de la clave, endpoints protegidos y cómo obtienen token los tests. | 07 |
| Q9 | 08 | Mejora para las credenciales (propuesta pedida por el operador). Ver tabla "Propuesta de credenciales". | Opciones A-E. | Combinar A + B + E ahora y D como acción del operador (detalle abajo). | 08 |
| Q10 | 08, 09 | ¿Puede BUILDER ejecutar el runner `Gaffer.Api.Migrations`? Hoy migra `GafferDB` (desarrollo) y `GafferDB_Test` en la misma ejecución. | a) Sí, en ambas BD. b) Solo en `GafferDB_Test` (el runner acepta la BD destino por configuración o argumento); el operador migra `GafferDB`. c) No; el operador ejecuta todas las migraciones. | b): los tests solo dependen de `GafferDB_Test` y la BD de desarrollo es del operador. | 09 (y la demostración de 08) |
| Q11 | 09 | ¿Se renombran también los objetos derivados con la errata? | a) Solo la tabla y el trigger (obligatorio: el cuerpo del trigger referencia la tabla por nombre). b) Además, recrear la FK `FK_Facilities_Addresses_Adresses` como `FK_Facilities_Addresses_Addresses`. | b): elimina la errata por completo en la misma migración. | 09 |
| Q12 | 09 | ¿La nueva migración necesita `Down()` reversible (volver a `Adresses` y recrear el trigger antiguo)? ¿Y se puede añadir al runner una opción para ejecutarlo (hoy solo hace `MigrateUp`)? | a) `Down` reversible y opción de bajada en el runner para demostrarlo. b) `Down` reversible, solo revisado sin ejecutar. c) Sin `Down`. | b): FluentMigrator espera un `Down` y así se puede volver atrás en local sin ampliar el runner. a) si el operador quiere evidencia ejecutable. | 09 |

### Propuesta de credenciales (Q9)

Hoy la contraseña del usuario `root` de MariaDB está escrita en 8 sitios, 5 de ellos solo en tests. Alternativas:

| Opción | Qué es | A favor | En contra |
| --- | --- | --- | --- |
| A. Una sola fuente por proyecto | API: `DatabaseConfig:ConnectionString` desde configuración (ya lo hace). Tests: una sola lectura (`ConnectionStrings:Database` de la configuración de test) que usan `TestFixture`, `BaseSqlTest`, `TestDefaultValues` y los tests de integración; las cadenas "inalcanzables" (puerto 1) se derivan de ella cambiando el puerto. Migrations: cadenas desde configuración o argumentos, no en el código. | Se pasa de 8 sitios a 1 por proyecto; sin herramientas nuevas. | Por sí sola no saca la contraseña del repositorio. |
| B. User Secrets en local + variables de entorno como override | `UserSecretsId` en `Gaffer.Api`, `Gaffer.Api.Tests` y `Gaffer.Api.Migrations`; `dotnet user-secrets set ...` una vez por máquina. Variables `DatabaseConfig__ConnectionString`, `ConnectionStrings__Database` (y las que defina Migrations) tienen prioridad y sirven para CI. | Mecanismo estándar de .NET; los secretos quedan fuera del repositorio; `Host.CreateDefaultBuilder` ya carga variables de entorno, y User Secrets en Development si existe `UserSecretsId`. | Paso de configuración inicial por desarrollador; los tests y Migrations tienen que añadir `AddUserSecrets`/`AddEnvironmentVariables` a mano. |
| C. Fichero local ignorado por Git | `appsettings.Local.json` / `testSettings.Local.json` / `.env` opcionales y en `.gitignore`, con una plantilla versionada sin contraseña. | Muy simple y visible. | Riesgo de subirlo por error si cambia `.gitignore`; formato no estándar para Migrations. |
| D. Usuario de BD no root | Usuario de aplicación (DML en `GafferDB`/`GafferDB_Test`) y usuario de migraciones (DDL, `CREATE DATABASE`). | Mínimo privilegio. | Lo crea el operador en el contenedor Docker; no elimina por sí solo la contraseña del código. |
| E. Ficheros versionados sin contraseña y fallo explícito | `appsettings.Development.json` y `testSettings.json` quedan sin `Pwd` (o sin la cadena); si falta la credencial, los tests con MySQL fallan con un mensaje claro que explica cómo configurarla. Nunca se omiten (AGENTS.md). | Imposible volver a subir la contraseña sin darse cuenta. | Un checkout limpio no pasa los tests hasta configurar el secreto. |

Recomendación PLANNER: A + B + E en este plan; D como acción opcional del operador (crear los usuarios en Docker y actualizar sus secretos). La contraseña actual seguirá en el historial de Git: si importa, el operador la cambia en su contenedor (acción del operador, fuera del alcance de los agentes). El paso 08 está redactado para la recomendación; si el operador elige otra combinación, PLANNER actualiza sus criterios antes de que BUILDER empiece.

## Acciones del operador (los agentes no las hacen)

| Cuándo | Acción |
| --- | --- |
| Antes del ciclo 1 | Crear la rama del plan (Q1-Q12, Q14 y Q15 ya están resueltas). |
| Antes de que BUILDER empiece el paso 10 | Opcional: responder Q16 si se quiere b); si no, se aplica a). |
| Antes de la publicación de cierre | Responder Q13. |
| Tras el paso 01 | `git rm --cached build.log test.log facility-test.log gaffer-api.sln.DotSettings.user` y commit (los ficheros se conservan en disco). |
| Paso 08 | Configurar sus User Secrets o variables de entorno con los comandos que BUILDER documente en `STATUS.md`; opcionalmente D y el cambio de contraseña del contenedor. |
| Paso 09 | Ejecutar el runner de migraciones en `GafferDB` (Q10 = b). |
| Al cerrar, tras aprobarse los pasos 05, 06 y 10 | Regenerar `Gaffer.Api.Client` con NSwag y versionarlo como **1.0.0** (petición del operador; versión por defecto en `Directory.Build.props` de la raíz, Q14 = b). El contrato OpenAPI cambia: 201 en Create y enums como texto. Empaquetar y publicar según Q13. Los futuros cambios siguen la regla SemVer del paso 10. Revisar los consumidores del cliente, que hoy esperan 200 en Create y enums como entero (no se han encontrado en `quionis-bff-api` ni en `quionis-frontend`). |

## Fuera de alcance (anotado, no se planifica)

- Propiedad `Reason` que nunca se asigna en `CannotDelete*/CannotUpdate*`.
- `CriteriaType.NONE`.
- Consultas N+1 en `SqlFacilityRepository`.
- `TestFixture` duplica el registro de DI y MVC de `Startup` (solo se sincroniza lo que exige cada paso).
- `CancellationToken.None` en controladores.
- `ThrowExceptionIfPlayerIsNotFound` sin `await`.
- Campo `logger` sin asignar en `ExceptionHandlingMiddleware`.
- `Program.SetConfigurationProviders` construye un `ConfigurationBuilder` que se descarta; `ProductContext.Environment` fijo a `"Development"`. Afectan a cómo se carga la configuración (relevante para Q9) pero no se piden. Si la opción elegida en Q9 no funciona por esto, BUILDER lo registra como bloqueo en vez de arreglarlo por su cuenta.
- `OptionsStartup` puede volcar la configuración completa, cadena de conexión incluida, a `startup-settings.log` si `EnableSaveStartupConfigurationToLocal=true` (hoy no está en los `appsettings`). Con `*.log` en `.gitignore` (paso 01) ese fichero ya no se versionaría.
- `using Azure.Identity` suelto en `Gaffer.Api.Migrations/Program.cs` (llega de forma transitiva desde FluentMigrator).
- `Microsoft.Extensions.ApiDescription.Server` (10.0.10): ya está alineado; no se evalúa si se usa.
- Artefactos generados de `Gaffer.Api.Client`: los regenera el operador.

## Restricciones

- No se tocan, versionan, auditan ni verifican los artefactos generados de `Gaffer.Api.Client`. BUILDER no ejecuta `dotnet build -c Release` sobre `Gaffer.Api` ni sobre la solución (el target `NSwag` escribiría en `Gaffer.Api.Client`).
- Los tests con MySQL se ejecutan contra Docker local (`Gaffer.Database`); nunca se omiten. Si Docker o la BD no están disponibles, `BLOCKED` con el error exacto.
- Los cambios de DI o MVC en `Gaffer.Api` se reflejan en el mismo incremento en `Gaffer.Api.Tests/TestFixture.cs`.
- Sin operaciones de Git por parte de los agentes (incluido `git rm --cached`).
- Máximo 6 ciclos.
- Si cambian referencias de paquetes (pasos 03, 07, 08), DOCUMENTER actualiza `.github/doc/common/dev-stack.md` / `test-stack.md` al cerrar (AGENTS.md, Documentation).
