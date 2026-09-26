# STATUS - TECHDEBT-001

| Campo | Valor |
| --- | --- |
| Estado | DONE |
| Ciclo | 2 |
| Pasos completados | 01, 02, 03, 04, 05, 06, 07, 08, 09, 10, 11 |
| Pasos bloqueados | Ninguno (Q13 abierta: solo afecta a la publicación en el cierre del operador) |
| Bloqueos | Ninguno. Q16 sin respuesta → se aplica a) (solo `Gaffer.Api.Client`), decisión del operador al iniciar el ciclo 2. |

## Tiempos por agente

| Agente | Ciclo | Inicio (UTC) | Fin (UTC) |
| --- | --- | --- | --- |
| PLANNER | 0 | n/a | n/a |
| BUILDER | 1 | 2026-09-26T11:25:47Z | 2026-09-26T11:47:10Z |
| REVIEWER | 1 | 2026-09-26T11:49:00Z | 2026-09-26T11:52:25Z |
| BUILDER | 2 | 2026-09-26T12:09:34Z | 2026-09-26T12:12:08Z |
| REVIEWER | 2 | 2026-09-26T12:13:32Z | 2026-09-26T12:15:55Z |

## Prerrequisitos (ciclo 1)

| Comprobación | Resultado |
| --- | --- |
| Docker `Gaffer.Database` | Up, `0.0.0.0:3306->3306` (MariaDB) |
| `dotnet --version` / `--list-sdks` | 10.0.302 (también 8.0.423 y 9.0.316); runtimes 10.0.10 |
| Build base `dotnet build gaffer-api.sln --no-incremental -v q` | 0 errores, 85 advertencias (12 son NU1901/NU1903: `NuGet.Packaging`/`NuGet.Protocol` 6.11.0, `Microsoft.Build` 17.10.4) |
| Tests base `dotnet test gaffer-api.sln --verbosity quiet` | 127/127, 0 omitidos |
| Rama | `TECHDEBT-001` (creada por el operador) |

## Comandos y resultados

| Ciclo | Paso | Fase | Comando / filtro | Resultado |
| --- | --- | --- | --- | --- |
| 1 | 01 | Red | `git check-ignore --no-index -v build.log test.log facility-test.log gaffer-api.sln.DotSettings.user` | Sin salida, exit 1 |
| 1 | 01 | Green | Mismo comando tras añadir `*.log`, `*.user`, `*.DotSettings.user` | 4 reglas (`.gitignore:9:*.log` x3, `.gitignore:13:*.DotSettings.user`), exit 0 |
| 1 | 01 | AC1.2 | `git ls-files -ci --exclude-standard` | Solo los 4 ficheros; AC1.3: entradas previas intactas |
| 1 | 02 | AC2.1/2.2 | `grep -rn` de `DeletePlayerResult\|DeleteFacilityResult\|UpdatePlayerResult\|DeletePlayerRequest\|GetProblemDetails\|LoggingStartup\|ConfigureLogging` en `*.cs` (incluye `Gaffer.Api.Client` solo lectura) | Antes: solo las definiciones y la llamada en `Startup` L29. Después: 0 resultados |
| 1 | 02 | Regresión | build `--no-incremental` + `dotnet test gaffer-api.sln` | 0 errores / 85 advertencias; 127/127 (incluye `DatabaseStartupShould`, AC2.3) |
| 1 | 03 | AC3.1 | `grep` de `ClosedXML\|DocumentFormat.OpenXml\|XLWorkbook`, `HealthChecks`, `ApplicationInsights\|TelemetryClient`, `CodeGeneration`, `using MySqlConnector`, `Microsoft.Identity`, `JwtBearer`, `MySql` (tests) | Solo `using` sueltos de OpenXml (3 ficheros), `//services.AddHealthChecks` comentado y `using Microsoft.Identity.Client`; ningún uso real. Tests: 0 usos de `MySql` |
| 1 | 03 | Grupo 1 | Quitar ClosedXML, HealthChecks x3, ApplicationInsights, CodeGeneration.Design + `using` sueltos | Build: 1 error `CS0246 NuGet` en `PlayerControllerShould.cs` L9 (`using NuGet.Protocol.Core.Types` suelto, llegaba de CodeGeneration.Design). Quitado → 0 errores, 73 advertencias (desaparecen las 12 NU1901/NU1903) |
| 1 | 03 | Grupo 2 | Quitar `Microsoft.Extensions.*` 9.0.9 x4, `MySqlConnector`, bloque comentado, ítems `bin\Debug\...`; `Mvc.NewtonsoftJson` 9.0.18 → 10.0.10; tests sin `MySql.Data`; `global.json` 10.0.302 + `latestFeature` | `rm -rf bin` + build `--no-incremental`: 0 errores, 73 advertencias; `dotnet --version` = 10.0.302 (AC3.7); 127/127 |
| 1 | 03 | AC3.5 | `ls bin/Debug/net10.0/*.json` tras build limpio; `dotnet msbuild -getItem:Content` sobre el csproj de HEAD (copia temporal) y el nuevo | Tests: `testSettings.json` copiado. `appsettings*.json` **no** se copian a la salida ni antes ni ahora: en HEAD (`Microsoft.NET.Sdk`) solo eran ítems `None`; el único `Content` era el `bin\...\Asp.Versioning.Abstractions.dll`. Las copias previas en `Gaffer.Api/bin` eran restos. Sin cambio de comportamiento; no hizo falta `FrameworkReference` (llega de `Mvc.NewtonsoftJson`) |
| 1 | 03/07 | AC3.2/AC7.4 | `dotnet list gaffer-api.sln package` | Directos `Microsoft.*`: `Mvc.NewtonsoftJson` 10.0.10, `ApiDescription.Server` 10.0.10, `TestHost` 10.0.10, `NET.Test.Sdk` 17.14.0 (+ Migrations: `Configuration.*` 10.0.10). Ningún 9.x |
| 1 | 04 | AC4.2 | `grep NSwagExe_Net100 ~/.nuget/packages/nswag.msbuild/14.6.1/build/NSwag.MSBuild.props` | L8 definida |
| 1 | 04 | Red | `dotnet .../14.6.1/tools/Net100/dotnet-nswag.dll run nswag.json /variables:Configuration=Debug,ClientOutputPath=<scratchpad fuera del repo>` (desde `Gaffer.Api`) | exit 127: `The specified runtime in the document (Net90) differs from the current process runtime (Net100)` |
| 1 | 04 | Green | Mismo comando tras `Net100` en csproj y `nswag.json` (build Debug previo) | exit 0; carpeta temporal borrada sin revisar; `git status -- Gaffer.Api.Client` vacío; 0 `Net90` en los dos ficheros |
| 1 | 05 | AC5.1 green (antiguo) | Copia temporal de `JsonContractShould` con expectativas antiguas (entero), `--filter JsonContractLegacyCharacterization`, luego borrada | 7/7. JSON antiguo abajo |
| 1 | 05 | AC5.2 red | `--filter FullyQualifiedName~JsonContract` con el contrato nuevo, código sin cambiar | 7 fallos: `Expected ...Type to be JTokenType.String ... but found JTokenType.Integer` |
| 1 | 05 | AC5.6 red | `--filter FullyQualifiedName~StartupSerialization` | 2 fallos: `Converters ... found JsonStringEnumConverter`; `no StringEnumConverter` |
| 1 | 05 | Green | `Startup`/`TestFixture`: sin `AddJsonOptions`, `StringEnumConverter` en Newtonsoft; suite completa | 7 fallos de lectores STJ de tests (`GetAPlayer`, `UpdateAPlayer`, facilities x3, search x2) → `TestJson.Options` (`JsonStringEnumConverter`) → 138/138 |
| 1 | 05 | AC5.7 | `OpenApiContractShould` lee `/swagger/v1/swagger.json` del `Startup` real (`StartupTestServer`) | Green. Sonda: quitando temporalmente el `StringEnumConverter` de `Startup` → red (`type` distinto de `string`); restaurado. `nswag.json` `defaultEnumHandling: "String"` |
| 1 | 06 | Red | `--filter "FullyQualifiedName~PlayerFeaturesShould\|FullyQualifiedName~FacilityFeaturesShould\|FullyQualifiedName~PlayerControllerShould\|FullyQualifiedName~FacilitiesController"` | 4 fallos `Expected ... Created {value: 201}, but found OK {value: 200}`; unitario: `CS1061 'Guid' no contiene 'Result'` |
| 1 | 06 | Green | `CreatedAtAction(nameof(GetBy), new { id }, id)` + `[ProducesResponseType(typeof(Guid), 201)]`; `PostAsync` acepta 200 y 201 (también sirve a `POST players/search`) | Suite 142/142. OpenAPI servido: `/api/v1/players` y `/api/v1/facilities` `post.responses` = `201` (sin `200`) |
| 1 | 07 | Red | `--filter "FullyQualifiedName~StartupSecurity\|FullyQualifiedName~AnonymousAccess"` | `NotRegisterAnyAuthenticationScheme`: `found {"Cookies"}`. `ServeAnAnonymousRequest...` (caracterización) green |
| 1 | 07 | Green | Borrado `AuthenticationStartup`, `AddAuthorization`, `UseAuthentication/Authorization`, `RequireAuthorization` comentados (Startup y TestFixture), `using` Cookies, JwtBearer | 144/144. AC7.3 `grep -rnE "AddCookie\|CookieAuthenticationDefaults\|UseAuthentication\|UseAuthorization\|RequireAuthorization\|JwtBearer\|ConfigureAuthentication\|AddAuthorization"` (`*.cs`, `*.csproj`): 0 |
| 1 | 08 | AC8.1 antes | `git grep -nE "Pwd=\|Password=" -- ':!.github'` | 8 contraseñas reales en 7 ficheros (+ la aserción `NotContain("Pwd=")`) |
| 1 | 08 | Red | `--filter "FullyQualifiedName~TestConfigurationShould\|FullyQualifiedName~ApiDatabaseConfiguration"` | 8 fallos: precedencia (`differ at index 7`), orden de proveedores (1 en vez de 3), guía x4 (`no exception was thrown`), puerto (`found "3306"`), `appsettings.Development.json` con `Pwd`. `ReceiveTheConnectionStringFromTheEnvironment...` green desde el inicio (el host ya da prioridad a las variables) |
| 1 | 08 | AC8.4 | `dotnet test` sin variable ni User Secrets (`dotnet user-secrets list` = vacío) | 155 fallidos, 0 omitidos. `OneTimeSetUp: System.InvalidOperationException : The MySQL test connection string 'ConnectionStrings:Database' has no password. MySQL-backed tests are not skipped: configure it once per machine with dotnet user-secrets set "ConnectionStrings:Database" "Server=127.0.0.1;Port=3306;Database=GafferDB_Test;Uid=<user>;Pwd=<password>;" --project Gaffer.Api.Tests or override it with the environment variable ConnectionStrings__Database.` |
| 1 | 08 | AC8.2 | `dotnet user-secrets set "ConnectionStrings:Database" "<cadena>" --project Gaffer.Api.Tests` + `dotnet test gaffer-api.sln` | 155/155, 0 omitidos |
| 1 | 08 | AC8.6 | `dotnet run --project Gaffer.Api.Migrations --no-build` sin configuración (y con `-- --Target GafferDB_Test`) | Mensaje explicativo, exit 2 (ambos) |
| 1 | 08 | AC8.6 | `ConnectionStrings__GafferDB=<dev> ConnectionStrings__GafferDB_Test=<test> dotnet run --project Gaffer.Api.Migrations --no-build -- --Target GafferDB_Test` | exit 0; salida solo menciona `GafferDB_Test`; sin migraciones pendientes; `GafferDB.VersionInfo` MAX = 202608131010 antes y después |
| 1 | 08 | AC8.1 después | `grep -rnE "Pwd=\|Password="` (sin `bin/obj/.github/.git`) y `grep -rn <contraseña-anterior>` | 0 contraseñas reales: quedan plantillas `<password>`, valores ficticios de tests (`env-secret`, `a-secret`) y la aserción `NotContain("Pwd=")`. `<contraseña-anterior>`: 0 |
| 1 | 09 | Red | `--filter FullyQualifiedName~DatabaseSchema` (sin migrar) | 4 fallos: `{"Adresses"} differs`, trigger con `Adresses`, FK distinta, `Table 'GafferDB_Test.Addresses' doesn't exist` |
| 1 | 09 | AC9.5 prep | `docker exec Gaffer.Database mariadb ... GafferDB_Test` INSERT Facility `aaaaaaaa-...-0009`, `Adresses` `bbbbbbbb-...-0009`, enlace en `Facilities_Addresses` | Fila visible en `Adresses` |
| 1 | 09 | Migración | `ConnectionStrings__GafferDB_Test=<test> dotnet run --project Gaffer.Api.Migrations --no-build -- --Target GafferDB_Test` | exit 0: `202609261200: M0003_RenameAdressesToAddresses migrated` (DROP TRIGGER, DeleteForeignKey, RenameTable, CreateForeignKey, CREATE TRIGGER) |
| 1 | 09 | AC9.5/AC9.8 | `SELECT ... FROM GafferDB_Test.Addresses a JOIN Facilities_Addresses ...`; `SELECT MAX(Version)` en ambas BD | Fila `bbbbbbbb-...-0009 Probe St` enlazada a `aaaaaaaa-...-0009`. `GafferDB` = 202608131010 (sin tocar); `GafferDB_Test` = 202609261200 |
| 1 | 09 | Red unitario | `--filter "FullyQualifiedName~DatabaseSchema\|FullyQualifiedName~Facility"` tras migrar, código con `Adresses` | 48 fallos: `OneTimeSetUp: MySqlException : Table 'GafferDB_Test.Adresses' doesn't exist` |
| 1 | 09 | Green | `Adresses` → `Addresses` en `SqlFacilityRepository` (3), `SqlFacilityRepositoryShould` (3), `DbTableNames` | Mismo filtro 48/48; suite 159/159 |
| 1 | 09 | AC9.7 | `grep -rn Adresses --include=*.cs` | Solo `M0002`, `M0003` y `DatabaseSchemaShould` (la prueba que exige su ausencia) |
| 1 | 10 | Solo evaluación (sin cambios) | `dotnet msbuild Gaffer.Api.Client/Gaffer.Api.Client.csproj -getProperty:Version -getProperty:PackageVersion -getProperty:GlobalPackageVersion` | `Version` 1.0.0, `PackageVersion` 1.0.0, `GlobalPackageVersion` vacío (valor por defecto del SDK, ahora verificado). Paso no implementado: Q14/Q15 |
| 1 | 11 | AC11.1 | `dotnet build gaffer-api.sln --no-incremental -v q` | 0 errores, 70 advertencias (línea base 85). Sin tipos nuevos; las de ficheros tocados son previas (líneas desplazadas) |
| 1 | 11 | AC11.2 | `dotnet test gaffer-api.sln --verbosity quiet` (User Secrets) y con `ConnectionStrings__Database=<cadena>` tras quitar el secreto | 159/159, 0 omitidos, en ambos casos (127 base + 32 nuevos) |
| 1 | 11 | AC11.3 | Búsquedas de restos | 0 `Microsoft.*` 9.x; 0 `Net90`; 0 `AddJsonOptions`; `defaultEnumHandling: "String"`; 0 contraseñas; `Adresses` solo en M0002/M0003/test de ausencia; 0 símbolos de AC2.1; 0 autenticación; versión del cliente: aplazada (Q14) |
| 1 | 11 | AC11.4 | `git status --short`, `git diff --stat` | 37 modificados/borrados + 11 nuevos, todos del alcance de los pasos 01-09; nada en `Gaffer.Api.Client/` |
| 1 | REVIEWER | Build | `dotnet build gaffer-api.sln --no-incremental -v q` | 0 errores, 70 advertencias |
| 1 | REVIEWER | AC8.4 | `dotnet test gaffer-api.sln --no-build` sin variable ni User Secrets | 159 con error, 0 omitidos, mensaje de guía |
| 1 | REVIEWER | Regresión | `ConnectionStrings__Database=<cadena> dotnet test gaffer-api.sln --no-build --verbosity quiet` | 159/159, 0 omitidos |
| 1 | REVIEWER | Veredicto | Ver `REVIEW.md` | CHANGES_REQUESTED → NEEDS_CHANGES: paso 10 no demostrado (B1, Q14/Q15) |
| 2 | 10 | Prerrequisitos | `docker ps --filter name=Gaffer.Database`; `ls Directory.Build.*` en la raíz, `E:\Proyectos\Colaborativos\Gaffer`, `E:\Proyectos\Colaborativos`, `E:\Proyectos`, `E:\` | Up `0.0.0.0:3306`. Ningún `Directory.Build.props`/`.targets` en el repositorio ni en directorios superiores (AC10.11: no se pierde ningún import) |
| 2 | 10 | Antes AC10.1 | `dotnet msbuild Gaffer.Api.Client/Gaffer.Api.Client.csproj -getProperty:Version -getProperty:PackageVersion` | `Version` 1.0.0, `PackageVersion` 1.0.0 (valor por defecto del SDK, `GlobalPackageVersion` vacío) |
| 2 | 10 | Antes AC10.9 | `dotnet msbuild <csproj> -getProperty:Version -getProperty:PackageVersion -getProperty:AssemblyVersion -getProperty:FileVersion -getProperty:GlobalPackageVersion` (Api, Migrations, Tests) | Los 3: `{"Version":"1.0.0","PackageVersion":"1.0.0","AssemblyVersion":"","FileVersion":"","GlobalPackageVersion":""}` |
| 2 | 10 | Red AC10.3 | `dotnet msbuild Gaffer.Api.Client/Gaffer.Api.Client.csproj -t:ValidateClientSemVer -p:GlobalPackageVersion=1.0.0` | exit 1: `error MSB4057: El destino "ValidateClientSemVer" no existe en el proyecto` |
| 2 | 10 | Cambio | `Directory.Build.props` nuevo (raíz): `PropertyGroup` y `Target ValidateClientSemVer` condicionados a `'$(MSBuildProjectName)' == 'Gaffer.Api.Client'`; `GlobalPackageVersion` = 1.0.0 solo si viene vacío; `Error` si `$(Version)` no casa con la regex oficial de SemVer 2.0.0; `BeforeTargets="BeforeBuild;GenerateNuspec"` | Sin otras propiedades, `PackageReference` ni `Import` (AC10.11). `Gaffer.Api.Client.csproj` y `nswag.json` sin cambios en este ciclo |
| 2 | 10 | Green AC10.1 | Mismo comando que "Antes AC10.1" | `{"Version":"1.0.0","PackageVersion":"1.0.0"}` |
| 2 | 10 | AC10.2 | Mismo comando + `-p:GlobalPackageVersion=2.3.4` | `{"Version":"2.3.4","PackageVersion":"2.3.4"}` |
| 2 | 10 | Green AC10.3 | `dotnet msbuild Gaffer.Api.Client/Gaffer.Api.Client.csproj -t:ValidateClientSemVer -p:GlobalPackageVersion=<v> -v q` | `1.0`, `v1.0.0`, `1.0.0.0`: exit 1, `error : Gaffer.Api.Client version '<v>' is not a valid Semantic Version 2.0.0 (MAJOR.MINOR.PATCH[-prerelease][+build], e.g. 1.0.0). Set GlobalPackageVersion in Directory.Build.props or pass /p:GlobalPackageVersion=<version>.` `1.0.0`, `1.1.0-beta.1`, `2.0.0+build.5`: exit 0 |
| 2 | 10 | Enganches | `dotnet build Gaffer.Api.Client/Gaffer.Api.Client.csproj -p:GlobalPackageVersion=1.0` (Debug); `dotnet msbuild Gaffer.Api.Client/Gaffer.Api.Client.csproj -t:GenerateNuspec -p:NoBuild=true -p:GlobalPackageVersion=1.0` | Ambos exit 1 con el error SemVer (compilación y empaquetado protegidos); ningún `.nupkg` generado; sin builds Release |
| 2 | 10 | AC10.4 | `grep infoVersion Gaffer.Api/nswag.json` | L35 `"infoVersion": "1.0.0"` = versión evaluada (AC10.1). Verificado sin diff en este ciclo |
| 2 | 10 | AC10.9 después | Mismo comando que "Antes AC10.9" | Idéntico a la línea base en los 3 proyectos |
| 2 | 10 | AC10.10 | `dotnet msbuild Gaffer.Api/Gaffer.Api.csproj -t:ValidateClientSemVer -p:GlobalPackageVersion=v1.0.0` | exit 0, `0 Errores`, `0 Advertencia(s)`: el target existe en el proyecto (no hay MSB4057) y se omite por su condición |
| 2 | 10 | AC10.5 | `git status --short -- Gaffer.Api.Client Directory.Build.props Gaffer.Api/nswag.json` | `?? Directory.Build.props`; `M Gaffer.Api/nswag.json` (solo el cambio `Net100` del ciclo 1, paso 04); nada en `Gaffer.Api.Client/` |
| 2 | 10/11 | AC10.6/AC11.1 | `dotnet build gaffer-api.sln --no-incremental -v q -tl:off` (salida completa a scratchpad, agrupada por código) | exit 0: 0 errores, 70 advertencias (= ciclo 1; línea base 85). Tipos: CS8618, CS8625, CS8602, CS0414, CS8603, CS8601, CS8604, CS0169, CS9113 (todas previas); 0 menciones de `Directory.Build.props` |
| 2 | 10/11 | AC10.6/AC11.2 | `ConnectionStrings__Database=<cadena; contraseña leída del entorno del contenedor en la sesión, no escrita en disco> dotnet test gaffer-api.sln --no-build --verbosity quiet` | 159/159, 0 omitidos, 8 s (sin tests nuevos: la validación del paso 10 son comandos MSBuild) |
| 2 | 11 | AC11.3 | Búsquedas de restos | 0 `Microsoft.*` 9.x en csproj; 0 `Net90`; 0 `AddJsonOptions`; `defaultEnumHandling: "String"`; 0 contraseñas reales (`git grep Pwd=\|Password=` fuera de `.github` sin plantillas/ficticios/aserción); `<contraseña-anterior>` 0 fuera de `.github` (solo como término de búsqueda en ficheros del plan); `Adresses` solo M0002/M0003/`DatabaseSchemaShould`; 0 símbolos AC2.1; 0 autenticación; versión cliente 1.0.0 = `infoVersion` 1.0.0 |
| 2 | 11 | AC11.4 | `git status --short`, `git diff --stat` | 41 M + 2 D + 15 `??` (43 ficheros versionados cambiados); único nuevo del ciclo 2: `Directory.Build.props`; nada en `Gaffer.Api.Client/`; AC10.9 igual a la línea base |
| 2 | REVIEWER | Paso 10 | `dotnet msbuild` del cliente: `-getProperty` (por defecto y con `2.3.4`), `ValidateClientSemVer` con 8 versiones, red simulado con `-p:ImportDirectoryBuildProps=false`, `build`/`GenerateNuspec` con versión inválida; AC10.9 en los 3 proyectos con y sin el props; AC10.10 | Todo como BUILDER: 1.0.0; 2.3.4; inválidas exit 1 y válidas exit 0; MSB4057 sin el props; enganches exit 1; otros proyectos idénticos; AC10.10 exit 0 |
| 2 | REVIEWER | Build | `dotnet build gaffer-api.sln --no-incremental -v q` (resumen) | 0 errores, 70 advertencias |
| 2 | REVIEWER | Regresión | `ConnectionStrings__Database=<cadena> dotnet test gaffer-api.sln --no-build --verbosity quiet` | 159/159, 0 omitidos |
| 2 | REVIEWER | Veredicto | Ver `REVIEW.md` | APPROVED. O7: contraseña literal en ficheros del plan; REVIEWER la sustituyó en `STATUS.md`/`REVIEW.md`; queda en `00-*.md` L6 (operador) |

### JSON crudo (AC5.1 antes / AC5.2 después)

| Momento | `GET /api/v1.0/players/{id}` |
| --- | --- |
| Antes | `{"id":"75d5...","firstName":"Alexia","lastName":"Putellas","secondLastName":"Segura","birthDate":"1994-02-04T00:00:00Z","gender":1}` |
| Después | Mismo casing y fecha; `"gender":"FEMALE"` |

| Momento | `GET /api/v1.0/facilities/{id}` |
| --- | --- |
| Antes | `{"id":"cda0...","name":"Contract Arena","active":true,"buildDate":"2020-07-20T00:00:00Z","type":1,"capacity":18000,"address":{...},"stands":[]}` |
| Después | Mismo casing y fecha; `"type":"Arena"` |

- AC5.4: la entrada con enteros **sí** se acepta (`"gender": 1` → `FEMALE`, `"type": 1` → `Arena`); fijado en `StillAccept...SentAsInteger`.
- AC5.5: `PUT` con `UpdatePlayerRequest` (campos públicos) sigue actualizando (`UpdateAPlayerFromARequestBoundToPublicFields`).
- AC5.9: los tests de cuerpos de error (`Status`, `Title`, `Instance`, `Extensions`) siguen en verde sin tocarlos.
- AC5.7 (fragmento servido): `Gender` = `{"type":"string","x-enumNames":["MALE","FEMALE","OTHER"],"enum":["MALE","FEMALE","OTHER"]}`.

### Notas para REVIEWER

- Tests con MySQL: sin credencial fallan a propósito (AC8.4). Ejecutar con `ConnectionStrings__Database="Server=127.0.0.1;Port=3306;Database=GafferDB_Test;Uid=root;Pwd=<password>;" dotnet test gaffer-api.sln` o configurar el User Secret de abajo. BUILDER quitó el User Secret que creó para AC8.2, así que la máquina queda como estaba.
- `GafferDB_Test` ya está migrada a M0003 (202609261200). `M0003.Down` **no se ejecutó** (Q12 = b): revisar comparándolo con `M0002` (AC9.6).
- Cambios que no estaban nombrados uno a uno en el plan, pero sí dentro del alcance de sus pasos: `PlayerControllerShould.cs` L9 `using NuGet...` quitado (sin él no compilaba al quitar CodeGeneration.Design; paso 03). `SearchPlayersEndpointShould.cs` lee con `TestJson.Options` (paso 05, `Acceptance/*`). `TestConfiguration.cs` pasa a ser la fuente única de configuración de test (paso 08). Nuevos ficheros auxiliares: `TestShared/TestJson.cs`, `TestShared/StartupTestServer.cs`, `TestShared/EnvironmentVariableScope.cs`, `Unit/Startups/StartupServices.cs`.
- `AC8.5`: se comprueba con `Host.CreateDefaultBuilder` real (Development, content root `Gaffer.Api`); `Program.SetConfigurationProviders` no lo impide y no se ha tocado.
- Ciclo 2 (paso 10): único cambio de producto `Directory.Build.props` (raíz). El target se llama `ValidateClientSemVer`; valida `$(Version)` (de la que se deriva `PackageVersion`). No se ejecutó ningún build Release ni `dotnet pack`; el enganche a `GenerateNuspec` se probó con `-p:NoBuild=true` y versión inválida (falla antes de generar nada).
- Runner de Migrations: todas las `ConnectionStrings:<BD>` configuradas se migran (en el mismo orden que antes: `GafferDB`, `GafferDB_Test`); `--Target <BD>` limita a una. Sin configuración: exit 2.

## Acciones pendientes del operador

| Paso | Acción | Estado |
| --- | --- | --- |
| 01 | `git rm --cached build.log test.log facility-test.log gaffer-api.sln.DotSettings.user` | Pendiente |
| 08 | Configurar secretos (sin escribir la contraseña en el repositorio), una vez por máquina: `dotnet user-secrets set "DatabaseConfig:ConnectionString" "Server=127.0.0.1;Port=3306;Database=GafferDB;Uid=<user>;Pwd=<password>;" --project Gaffer.Api`; `dotnet user-secrets set "ConnectionStrings:Database" "Server=127.0.0.1;Port=3306;Database=GafferDB_Test;Uid=<user>;Pwd=<password>;" --project Gaffer.Api.Tests`; `dotnet user-secrets set "ConnectionStrings:GafferDB" "Server=127.0.0.1;Port=3306;Database=GafferDB;Uid=<user>;Pwd=<password>;" --project Gaffer.Api.Migrations`; `dotnet user-secrets set "ConnectionStrings:GafferDB_Test" "Server=127.0.0.1;Port=3306;Database=GafferDB_Test;Uid=<user>;Pwd=<password>;" --project Gaffer.Api.Migrations`. Variables de entorno equivalentes (tienen prioridad; útiles en CI): `DatabaseConfig__ConnectionString`, `ConnectionStrings__Database`, `ConnectionStrings__GafferDB`, `ConnectionStrings__GafferDB_Test`. Opcional: usuarios no root (D) y cambiar la contraseña del contenedor (la actual sigue en el historial de Git). | Pendiente |
| 09 | Migrar `GafferDB`: `dotnet run --project Gaffer.Api.Migrations -- --Target GafferDB` (con sus secretos configurados) | Pendiente |
| 10 | Responder Q13 (publicación) antes del cierre. Q14/Q15 respondidas; Q16 → a) por defecto | Pendiente (solo Q13) |
| Cierre (tras aprobarse 05, 06 y 10) | Regenerar `Gaffer.Api.Client` después de aprobarse 05 y 06, versionarlo como 1.0.0 (valor por defecto de `Directory.Build.props`, o `/p:GlobalPackageVersion=1.0.0`) y publicarlo según Q13 (AC10.8). El contrato cambia: 201 + `Location` en los dos Create y enums como texto. Revisar consumidores (esperan 200 y enums como entero). | Pendiente |
| Plan | Borrar `10-final-refactor-and-regression.md` (obsoleto, sustituido por el 11) | Pendiente |

## Notas de cierre

- La API queda anónima por decisión explícita (Q8 = a); una autenticación real necesita su propio plan.
- Contrato OpenAPI cambiado (paso 05 y 06): el operador regenera el cliente como 1.0.0.

## Para DOCUMENTER

| Tema | Detalle |
| --- | --- |
| Paquetes (`dev-stack.md`, `test-stack.md`) | `global.json` → 10.0.302 / `latestFeature`. `Gaffer.Api`: fuera ClosedXML, HealthChecks x3, ApplicationInsights, CodeGeneration.Design, JwtBearer, `Microsoft.Extensions.*` 9.0.9 x4, `MySqlConnector` (queda `MySql.Data`); `Mvc.NewtonsoftJson` 10.0.10. Tests: fuera `MySql.Data` directo. Migrations: `Microsoft.Extensions.Configuration.CommandLine/EnvironmentVariables/UserSecrets` 10.0.10. JSON solo Newtonsoft (la línea "System.Text.Json + Newtonsoft.Json" de `dev-stack.md` queda obsoleta; los tests usan System.Text.Json solo para leer respuestas). NSwag `Net100`. |
| Contrato HTTP | Create → 201 + `Location` + `Guid`; enums como texto (`MALE`/`FEMALE`/`OTHER`, `Stadium`/`Arena`/`Pitch`/`Training`); se siguen aceptando enteros en la entrada; sin autenticación. |
| Configuración | User Secrets / variables de entorno (tabla de acciones del operador); runner de Migrations con `--Target`. |
| Esquema | Tabla `Addresses`, FK `FK_Facilities_Addresses_Addresses`, M0003. `.github/doc/features/facility-create.md` L25 menciona `Adresses`. |
| SemVer (paso 10, AC10.7) | Destinos (Q15 = c): `doc/features/technical/` (p. ej. `api-client-versioning.md`) y una línea en `.github/doc/common/dev-stack.md`, "Build and Packaging Notes". Regla: cambio incompatible del contrato (códigos de estado, tipos, formato, eliminación) → MAJOR; compatible que añade (endpoint, propiedad opcional) → MINOR; corrección sin cambio de contrato → PATCH. Procedimiento: la versión del paquete `Gaffer.Api.Client` está en `Directory.Build.props` de la raíz (`GlobalPackageVersion`, por defecto 1.0.0, solo afecta a `Gaffer.Api.Client`); se cambia editando ese valor o con `/p:GlobalPackageVersion=<v>` (override); el target `ValidateClientSemVer` rompe compilación y empaquetado si la versión no es SemVer 2.0.0; `infoVersion` de `Gaffer.Api/nswag.json` debe coincidir con esa versión. |

## Documentación modificada

DOCUMENTER (basado solo en código aprobado, `STATUS.md` y `REVIEW.md`; ninguna contraseña real incluida).

### Creados

| Fichero | Contenido |
| --- | --- |
| `doc/features/api-contract-and-access.md` | Funcional: `Create` devuelve 201 + `Location`; enums como texto (entero de entrada aceptado); JSON solo Newtonsoft; API anónima. |
| `doc/features/technical/api-contract-and-access.md` | Técnico del punto anterior: overview, diagrama de secuencia, tabla de métodos, dependencias, ejemplos, cómo probarlo. |
| `doc/features/database-configuration.md` | Funcional: credenciales fuera de la configuración versionada (User Secrets / variables de entorno) para `Gaffer.Api`, `Gaffer.Api.Tests` y `Gaffer.Api.Migrations`; runner con `--Target` y exit 2 sin configuración. |
| `doc/features/technical/database-configuration.md` | Técnico del punto anterior: precedencia de fuentes, tabla de métodos (`TestConfiguration`, `Program`), comandos de ejemplo (sin contraseñas reales), cómo probarlo. |
| `doc/features/technical/api-client-versioning.md` | Técnico (Q15 = a+c): regla SemVer, `Directory.Build.props` (`GlobalPackageVersion`, `ValidateClientSemVer`), alineación con `infoVersion` de `nswag.json`, alcance limitado a `Gaffer.Api.Client`. |

### Actualizados (impacto directo del plan)

| Fichero | Cambio |
| --- | --- |
| `.github/doc/features/facility-create.md` | Diagrama: `INSERT Adresses` → `INSERT Addresses` (M0003); `Create` documentado como `201 Created` + `Location` (antes `200 OK`); tabla de métodos actualizada a `Task<ActionResult<Guid>>`; nota de origen y enlaces a los nuevos documentos. |
| `.github/doc/common/dev-stack.md` | `global.json` → `10.0.302`/`latestFeature`; JSON solo Newtonsoft (System.Text.Json ya no configurado en MVC); MySQL: `MySql.Data` en `Gaffer.Api`, `MySqlConnector` solo en `Gaffer.Api.Migrations`; "Build and Packaging Notes" con la regla y el enlace a `api-client-versioning.md` (paso 10, Q15). |
| `.github/doc/common/test-stack.md` | Acceso a datos en tests ya no referencia `MySql.Data` directo (se hace vía `SqlConnectionProvider` de `Gaffer.Api`); nota sobre tests MySQL nunca omitidos y enlace a `database-configuration.md`. |
| `.github/doc/common/dev-stack-documentation.md` | Quitadas filas de paquetes eliminados (HealthChecks x3, ClosedXML, ApplicationInsights, JwtBearer, `Configuration.Binder`/`DependencyInjection.Abstractions`/`Options` 9.0.9 de `Gaffer.Api`, CodeGeneration.Design); `Mvc.NewtonsoftJson` → 10.0.10; añadidas filas de `Microsoft.Extensions.Configuration.CommandLine/EnvironmentVariables/UserSecrets` 10.0.10 (`Gaffer.Api.Migrations`); notas de alcance por proyecto en `MySql.Data`/`MySqlConnector`/NSwag.MSBuild. |
| `.github/doc/common/test-stack-documentation.md` | Quitada la fila `MySql.Data` (ya no es referencia directa de `Gaffer.Api.Tests`); nota explicando el motivo. |

### Actualizados (impacto indirecto)

| Fichero | Cambio |
| --- | --- |
| `doc/features/database-connection-reliability.md` | Enlace añadido a `database-configuration.md` (la cadena de conexión ya no lleva contraseña versionada). |
| `doc/features/technical/database-connection-lifecycle.md` | Nota + enlace a `database-configuration.md` (mismo mecanismo de lectura de `DatabaseConfig:ConnectionString`, la fuente del valor cambió). |

### No documentado / fuera de alcance de DOCUMENTER

- Regeneración, versionado (1.0.0) y publicación real de `Gaffer.Api.Client`: acción de cierre pendiente del operador (Q13 abierta solo para la publicación); los artefactos generados no se auditan ni describen aquí (fuera de alcance de BUILDER/REVIEWER/DOCUMENTER).
- `git rm --cached` de los ficheros de log/`*.user` y migración de `GafferDB` (acciones pendientes del operador, no cambian documentación de producto).
- Borrado de `10-final-refactor-and-regression.md` (obsoleto): acción de limpieza del plan, no de documentación.
