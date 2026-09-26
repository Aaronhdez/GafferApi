# REVIEW - TECHDEBT-001

## Ciclo 1

| Campo | Valor |
| --- | --- |
| Veredicto | CHANGES_REQUESTED |
| Estado resultante | NEEDS_CHANGES |
| Motivo | Paso 10 (AC10.1-AC10.8) sin implementar ni demostrar (Q14/Q15 abiertas); AC11.3/AC11.4 dependen de él. Pasos 01-09 y 11 (salvo lo que depende de 10) demostrados sin defectos bloqueantes. |

### Verificación independiente (REVIEWER)

| Comprobación | Comando | Resultado |
| --- | --- | --- |
| Rama / estado | `git branch --show-current`, `git status --short` | `TECHDEBT-001`; 37 M/D + 11 nuevos (ficheros y carpetas), todos en el alcance de 01-09; nada en `Gaffer.Api.Client/` |
| Docker | `docker ps --filter name=Gaffer.Database` | Up, `0.0.0.0:3306->3306` |
| SDK | `dotnet --version` | 10.0.302 (AC3.7) |
| Build | `dotnet build gaffer-api.sln --no-incremental -v q` | 0 errores, 70 advertencias (línea base 85) |
| Tests sin credencial (AC8.4) | `env -u ConnectionStrings__Database dotnet test gaffer-api.sln --no-build` (User Secrets vacíos) | 159 con error, 0 omitidos; `OneTimeSetUp: InvalidOperationException : The MySQL test connection string 'ConnectionStrings:Database' has no password ... dotnet user-secrets set ... --project Gaffer.Api.Tests or ... ConnectionStrings__Database` |
| Tests con credencial por variable (AC8.3/AC11.2) | `ConnectionStrings__Database=<cadena, contraseña no escrita en disco> dotnet test gaffer-api.sln --no-build --verbosity quiet` | 159/159, 0 omitidos, 8 s |
| AC1.1/1.2 | `git ls-files -ci --exclude-standard` | Solo `build.log`, `facility-test.log`, `gaffer-api.sln.DotSettings.user`, `test.log` |
| AC2.1/2.2 | `grep` de los símbolos de AC2.1 + `LoggingStartup`/`ConfigureLogging` en `*.cs` | 0 |
| AC3.2/AC7.4 | `dotnet list gaffer-api.sln package` | Ningún `Microsoft.*` 9.x directo |
| AC3.3 | `grep "bin\\Debug\|<!--" */*.csproj` | 0 |
| AC4.1 / AC5.7 | `grep Net90\|Net100\|defaultEnumHandling` en `nswag.json`/csproj | `Net100` x2, `defaultEnumHandling: "String"`, 0 `Net90` |
| AC5.8 | `grep AddJsonOptions\|using System.Text.Json` en `Startup.cs`/`TestFixture.cs` | 0 |
| AC7.3 | `grep` de `AddCookie\|CookieAuthenticationDefaults\|UseAuthentication\|UseAuthorization\|RequireAuthorization\|JwtBearer\|AddAuthorization\|AddAuthentication` (`Gaffer.Api`, `Gaffer.Api.Tests`, `*.cs`/`*.csproj`) | 0 |
| AC8.1 | `git grep -nE "Pwd=\|Password=" -- ':!.github'`, `git grep <contraseña-anterior>`, mismo `grep` en no versionados nuevos | Solo plantillas `<password>`, la aserción `NotContain("Pwd=")` y valores ficticios (`env-secret`, `a-secret`); `<contraseña-anterior>`: 0 |
| AC9.1/9.8 | `information_schema.TABLES` y `MAX(Version)` vía `docker exec` | `GafferDB_Test`: `Addresses`, versión 202609261200. `GafferDB`: `Adresses`, 202608131010 (sin tocar) |
| AC9.4 (índices) | `information_schema.STATISTICS` de `Facilities_Addresses` | `IX_Facilities_Addresses_AddressId` explícito: no queda índice con la errata |
| AC9.7 | `grep -rl Adresses --include=*.cs` | `M0002`, `M0003`, `DatabaseSchemaShould` (ver O3) |
| UserSecretsId API | `obj/Debug/net10.0/Gaffer.Api.AssemblyInfo.cs` | `UserSecretsIdAttribute` generado pese a quitar el paquete `UserSecrets` 9.0.9 (llega del framework) |
| Cliente generado | `git status --short -- Gaffer.Api.Client` | Vacío (no se revisó el contenido, fuera de alcance) |

### Criterios de aceptación

| Paso | Criterios | Estado | Evidencia red → green |
| --- | --- | --- | --- |
| 01 | AC1.1-AC1.4 | Demostrado | `check-ignore` sin salida → 4 reglas (STATUS); `ls-files -ci` reverificado; acción `git rm --cached` en STATUS |
| 02 | AC2.1-AC2.5 | Demostrado | Limpieza: búsquedas antes/después, build 0 errores, 127/127 (STATUS); reverificado 0 símbolos |
| 03 | AC3.1-AC3.7 | Demostrado con nota O1 | Búsquedas de uso antes de quitar (STATUS); build/tests reverificados; AC3.5: `testSettings.json` en salida; `appsettings*.json` nunca se copiaban a la salida de tests (BUILDER lo comprobó con `-getItem:Content` sobre HEAD) |
| 04 | AC4.1-AC4.5 | Demostrado | Red `dotnet-nswag` Net100 exit 127 (runtime Net90) → green exit 0 hacia carpeta temporal (STATUS); `Net90` = 0 reverificado |
| 05 | AC5.1-AC5.10 | Demostrado | Caracterización antigua 7/7 (enteros) y JSON crudo antes/después en STATUS; red 7 fallos `Integer` → green; `StartupSerializationShould` red 2 → green; AC5.4 enteros aceptados (`StillAccept...`); AC5.5 `UpdateAPlayerFromARequestBoundToPublicFields`; AC5.7 `OpenApiContractShould` contra `Startup` real con sonda red |
| 06 | AC6.1-AC6.7 | Demostrado | Red 4 fallos `found OK` + `CS1061` → green; `ReturnCreatedWithTheLocationOfTheCreatedPlayer`, `ReturnTheLocationOfTheCreatedFacility` (Location + GET 200); `DeclareCreatedAsTheSuccessResponseOfCreate` (201 sin 200); unitario `CreatedAtActionResult` |
| 07 | AC7.1-AC7.5 | Demostrado | `NotRegisterAnyAuthenticationScheme` red `{"Cookies"}` → green; AC7.2 `AnonymousAccessShould` (caracterización, verde antes y después) |
| 08 | AC8.1-AC8.8 | Demostrado | Red 8 fallos (precedencia, orden de proveedores, guía x4, puerto, `Pwd` en Development) → green; AC8.4 reproducido por REVIEWER; AC8.5 con `Host.CreateDefaultBuilder` real (verde desde el inicio, caracterización); AC8.6 exit 2 sin config / exit 0 con `--Target GafferDB_Test` (STATUS); AC8.7 comandos en STATUS sin contraseña; AC8.8 `TestFixture` usa `TestConfiguration` |
| 09 | AC9.1-AC9.9 | Demostrado | Red 4 fallos de `DatabaseSchemaShould` → migración → red 48 (`Adresses doesn't exist`) → green 48/48 y 159/159; AC9.5 fila `bbbbbbbb-...-0009` conservada; AC9.6 `Down` revisado: ver abajo |
| 10 | AC10.1-AC10.8 | **No demostrado** | Sin implementar: Q14/Q15 abiertas. Solo evaluación previa (`Version`/`PackageVersion` = 1.0.0, `GlobalPackageVersion` vacío) |
| 11 | AC11.1, AC11.2, AC11.5, AC11.6 | Demostrado | Build 70 ≤ 85; 159/159 = 127 + 32; acciones del operador y notas para DOCUMENTER en STATUS |
| 11 | AC11.3, AC11.4 | Parcial | Todas las comprobaciones en verde salvo la versión del cliente (aplazada Q14) y el csproj del cliente (paso 10 no hecho) |

### AC9.6 - Revisión de `M0003.Down` frente a `M0002` (no ejecutado, Q12 = b)

| Objeto | `M0002` | `M0003.Down` | Coincide |
| --- | --- | --- | --- |
| Tabla | `Adresses` | `Rename.Table("Addresses").To("Adresses")` (conserva filas) | Sí |
| FK | `FK_Facilities_Addresses_Adresses`, `Facilities_Addresses.AddressId` → `Adresses.Id`, `Cascade` | Igual | Sí |
| Trigger | `TRG_Facilities_BeforeDelete` borra `Stands` y `Adresses` | Cuerpo idéntico con `Adresses` (`CreateTriggerSql("Adresses")`) | Sí |
| Orden | - | Drop trigger → drop FK → rename → FK → trigger | Correcto (el FK se quita antes de renombrar) |

### Hallazgos

| ID | Severidad | Hallazgo | Acción |
| --- | --- | --- | --- |
| B1 | Bloqueante | Paso 10 sin implementar: AC10.1-AC10.8 sin evidencia; AC11.3 (versión del cliente) y AC11.4 (csproj del cliente) parciales. Causa externa: Q14 y Q15 sin respuesta del operador. | Operador responde Q14/Q15; BUILDER implementa el paso 10 en el ciclo 2 y repite AC11.3/AC11.4 |
| O1 | No bloqueante | AC3.5 pide `appsettings*.json` en `Gaffer.Api.Tests/bin/Debug/net10.0/`; no están ni estaban en HEAD (solo ítems `None` en `Microsoft.NET.Sdk`). El criterio partía de una premisa falsa; no hay regresión. | Ninguna; anotado |
| O2 | No bloqueante | Sin credencial fallan los 159 tests, incluidos los unitarios puros, porque el `SetUpFixture` raíz falla en `OneTimeSetUp`. Cumple AC8.4 (falla, no omite) pero impide correr unitarios sin MySQL configurado. | Decisión del operador; posible mejora fuera de este plan |
| O3 | No bloqueante | AC9.7 literal excluye `Adresses` fuera de `M0002`/`M0003`, pero `DatabaseSchemaShould` lo necesita para exigir su ausencia (AC9.1/AC9.4). Contradicción del plan, no defecto. | Ninguna |
| O4 | No bloqueante | Runner de Migrations: el nombre de BD usado en `CREATE DATABASE` es la clave de configuración (`ConnectionStrings:<Nombre>`), interpolado sin validar y sin cotejar con `Database=` de la cadena. Configuración controlada por el operador; mismo patrón que antes. | Opcional en un plan futuro |
| O5 | No bloqueante | Existen `10-client-semantic-versioning.md` y `10-final-refactor-and-regression.md` (obsoleto, sustituido por `11-*`). | Acción del operador ya registrada en STATUS |
| O6 | Informativo | Contrato HTTP cambiado (201 + `Location`, enums como texto): los consumidores del cliente deben revisarse al regenerarlo. | Acción de cierre del operador ya registrada |

### Revisión de código (resumen)

- `TestFixture.cs`: refleja los cambios de MVC de `Startup` (sin `AddJsonOptions`, `StringEnumConverter`, sin autenticación) y la configuración única (`TestConfiguration`). No hay handlers nuevos que registrar.
- `ExceptionHandlingMiddleware`: solo se quita `GetProblemDetails` privado sin uso; cuerpos de error sin cambios (Q5).
- `TestConfiguration.RequireDatabaseConnectionString` nunca incluye el valor configurado en el mensaje; `DatabaseStartupShould` sigue exigiendo que el log no contenga `Pwd=`.
- `EnvironmentVariableScope` restaura el valor previo; las clases que lo usan son `[NonParallelizable]`.
- `CreatedAtAction(nameof(GetBy), new { id }, id)` genera `Location` `/api/v1.0/{recurso}/{id}` (verificado por tests de aceptación).

## Ciclo 2

| Campo | Valor |
| --- | --- |
| Veredicto | APPROVED |
| Estado resultante | APPROVED |
| Motivo | B1 resuelto: paso 10 implementado (Q14 = b, Q15 = a + c, Q16 = a por defecto) y AC10.1-AC10.11 demostrados con red → green; AC11.1-AC11.6 completos. Sin defectos bloqueantes. Q13 solo afecta a la publicación de cierre del operador. |

### Verificación independiente (REVIEWER, ciclo 2)

| Comprobación | Comando | Resultado |
| --- | --- | --- |
| Cambios desde el ciclo 1 | `find` de ficheros modificados después de 2026-09-26T11:52:25Z (sin `bin/obj/.git`) | Producto: solo `Directory.Build.props` (nuevo). Resto: ficheros del plan y `MEMORIES.md`. Los pasos 01-09 siguen como se aprobaron en el ciclo 1 |
| Docker | `docker ps --filter name=Gaffer.Database` | Up, `0.0.0.0:3306->3306` |
| AC10.1 | `dotnet msbuild Gaffer.Api.Client/Gaffer.Api.Client.csproj -getProperty:Version -getProperty:PackageVersion` | `1.0.0` / `1.0.0` |
| AC10.2 | Mismo + `-p:GlobalPackageVersion=2.3.4` | `2.3.4` / `2.3.4` |
| AC10.3 red (simulado) | `-t:ValidateClientSemVer -p:ImportDirectoryBuildProps=false` | exit 1, `MSB4057 ... "ValidateClientSemVer" no existe` (confirma el red de BUILDER sin tocar ficheros) |
| AC10.3 green | `-t:ValidateClientSemVer -p:GlobalPackageVersion=<v> -v q` | `1.0`, `v1.0.0`, `1.0.0.0`, además `01.0.0` y `1.0.0-`: exit 1 con el error SemVer. `1.0.0`, `1.1.0-beta.1`, `2.0.0+build.5`: exit 0 |
| Enganches | `dotnet build <cliente> -p:GlobalPackageVersion=1.0`; `dotnet msbuild <cliente> -t:GenerateNuspec -p:NoBuild=true -p:GlobalPackageVersion=v1` | Ambos exit 1 con el error SemVer; ningún `.nupkg` nuevo; sin builds Release |
| AC10.4 | `grep infoVersion Gaffer.Api/nswag.json`; `git diff -U0` | L35 `"1.0.0"` = AC10.1; el diff de `nswag.json` solo contiene `Net100` (paso 04) y `defaultEnumHandling` (paso 05) |
| AC10.5 | `git status --short -- Gaffer.Api.Client` | Vacío (contenido generado no revisado, fuera de alcance) |
| AC10.9 | `-getProperty:Version,PackageVersion,AssemblyVersion,FileVersion,GlobalPackageVersion` en Api, Migrations y Tests, con y sin `-p:ImportDirectoryBuildProps=false` | Idéntico en los 3: `1.0.0`, `1.0.0`, vacío, vacío, vacío |
| AC10.10 | `dotnet msbuild Gaffer.Api/Gaffer.Api.csproj -t:ValidateClientSemVer -p:GlobalPackageVersion=v1.0.0 -v q` | exit 0, 0 errores (target omitido por su condición) |
| AC10.11 | Lectura de `Directory.Build.props` | Solo `GlobalPackageVersion` condicionado + target; sin `PackageReference` ni `Import`. Ningún props/targets superior (registrado por BUILDER) |
| Referencias al cliente | `grep Gaffer.Api.Client --include=*.csproj` fuera del cliente | Ninguna `ProjectReference`: la propiedad no se propaga a otros proyectos |
| AC10.6/AC11.1 | `dotnet build gaffer-api.sln --no-incremental -v q` (resumen, OPT-006) | 0 errores, 70 advertencias (= ciclo 1; base 85) |
| AC10.6/AC11.2 | `ConnectionStrings__Database=<cadena; contraseña leída del contenedor en la sesión, no escrita en disco> dotnet test gaffer-api.sln --no-build --verbosity quiet` | 159/159, 0 omitidos, 8 s |
| AC10.7/AC10.8/AC11.5/AC11.6 | Lectura de `STATUS.md` | Regla SemVer, procedimiento y destinos (Q15 = c) en "Para DOCUMENTER"; regenerar/versionar 1.0.0/publicar según Q13 en "Acciones pendientes" |

### Criterios de aceptación (ciclo 2)

| Paso | Criterios | Estado | Evidencia red → green |
| --- | --- | --- | --- |
| 01-09 | Todos | Demostrado (ciclo 1) | Sin cambios de producto desde entonces; regresión 159/159 reverificada |
| 10 | AC10.1-AC10.11 | Demostrado | Red: target inexistente (MSB4057, BUILDER y reproducido por REVIEWER); green: casos válidos/inválidos; antes/después de AC10.1 y AC10.9 en STATUS y reverificados |
| 11 | AC11.1-AC11.6 | Demostrado | Build 70 ≤ 85; 159/159; AC11.3 versión cliente 1.0.0 = `infoVersion`; AC11.4 solo `Directory.Build.props` nuevo en el ciclo, nada en `Gaffer.Api.Client/` |

### Hallazgos (ciclo 2)

| ID | Severidad | Hallazgo | Acción |
| --- | --- | --- | --- |
| B1 | Resuelto | Paso 10 implementado y demostrado | - |
| O7 | No bloqueante | La contraseña real del contenedor aparecía literal (como término de búsqueda de AC8.1/AC11.3) en `STATUS.md` y `REVIEW.md`, y sigue en `00-overview-and-open-questions.md` L6 (ya en HEAD). AC8.1 excluye `.github`, pero estos ficheros se versionan. REVIEWER la sustituyó por `<contraseña-anterior>` en `STATUS.md` y `REVIEW.md`. | Operador: sustituirla también en `00-*.md` antes del commit, y cambiar la contraseña del contenedor (ya estaba en la lista de acciones) |
| O8 | Informativo | Q16 sin responder: se aplica a) (solo `Gaffer.Api.Client`), tal como permite el plan. Q13 abierta solo para la publicación de cierre. | Ninguna para este plan |
| O1-O6 | Sin cambios | Ver ciclo 1 | - |

### Revisión de código (ciclo 2)

- `Directory.Build.props`: la condición `'$(MSBuildProjectName)' == 'Gaffer.Api.Client'` está en el `PropertyGroup` y en el `Target`; el valor por defecto solo se aplica si viene vacío (se mantiene el override). La regex es la oficial de semver.org. Se valida `$(Version)`, así que también cubre un `/p:Version=` directo.
- Enganche `BeforeTargets="BeforeBuild;GenerateNuspec"`: cubre `build`, `pack` y `pack --no-build`.
- Sin handlers nuevos: no hacen falta cambios en `TestFixture.cs`. La validación del paso 10 son comandos MSBuild, no tests (el plan lo permite, doble bucle del paso 10).
