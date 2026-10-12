# STATUS archive - TECHDEBT-005

Detalle verbatim de ciclos APPROVED colapsados en el índice de `STATUS.md` (OPT-026, OPT-029).

## Ciclo 1 - comandos y resultados

| Ciclo | Comando | Resultado |
| --- | --- | --- |
| 1 | `docker ps` (preflight) | `Quionis_DB` Up, `0.0.0.0:3306->3306` |
| 1 | Línea base: `dotnet build gaffer-api.sln --no-incremental -v q` | exit 0; 0 Advertencia(s), 0 Errores |
| 1 | Línea base: `dotnet test gaffer-api.sln --no-build -v q` | exit 0; 322/322 superados, 0 omitidos |
| 1 | Línea base AC1.5: `dotnet run --project Gaffer.Api --no-build --no-launch-profile` (`DatabaseConfig__Enabled=false`, `ASPNETCORE_URLS=http://localhost:5099`) + `curl /swagger/v1/swagger.json` | 21622 bytes, sha256 `54c730af...22d26` |
| 1 | Rojo AC1.1: `dotnet test Gaffer.Api.Tests --filter "FullyQualifiedName~FacilityRepositoryContractShould"` | exit 1; 2/2 con error: `Expected ... to be equal to {"Save"}, but {"Save", "Update"} contains 1 item(s) too many` (interfaz y clase) |
| 1 | Verde AC1.1/AC1.2: `dotnet test Gaffer.Api.Tests --filter "FullyQualifiedName~FacilityRepositoryContractShould\|FullyQualifiedName~SqlFacilityRepositoryShould"` | exit 0; 23/23 |
| 1 | Refactor, AC1.6: `dotnet build gaffer-api.sln --no-incremental -v q` | exit 0; 0 Advertencia(s), 0 Errores |
| 1 | Refactor, AC1.6: `dotnet test gaffer-api.sln --no-build -v q` | exit 0; 324/324 superados (322 + 2 nuevos), 0 omitidos |
| 1 | AC1.4/AC1.5: `dotnet test ... --filter "MakeAConcurrent\|UpdateFacilityCommandHandlerShould\|FacilityFeaturesShould\|FacilityStandsFeaturesShould\|OpenApiContractShould\|FacilitiesControllerContractShould"` (FullyQualifiedName~) | exit 0; 63/63 |
| 1 | AC1.3: `grep -n "Update(" Gaffer.Api/Infrastructure/SqlFacilityRepository.cs` | L122 (cref del `<summary>`), L208 `public async Task Update(Guid id, Func<Facility, Facility> buildUpdated, ...)`; la escritura sin lock se llama `WriteFacility` (L125 `private`), único llamador L216 dentro de la transacción con `LockFacility` |
| 1 | AC1.3: `grep -rn "\.Update(" Gaffer.Api --include=*.cs` | `UpdateFacilityCommandHandler.cs:36` (sobrecarga con lock) y `UpdatePlayerCommandHandler.cs:15` (`IPlayerRepository`, fuera de alcance) |
| 1 | AC1.6: `grep -rn "CancellationToken.None" Gaffer.Api --include=*.cs \| wc -l` | 0 |
| 1 | AC1.5 después: mismo `dotnet run` + `curl`, `cmp swagger-before.json swagger-after.json` | exit 0; idénticos (sha256 igual) |
| 1 | REVIEWER: `dotnet build gaffer-api.sln --no-incremental -v q` | exit 0; 0 Advertencia(s) |
| 1 | REVIEWER: `dotnet test gaffer-api.sln --no-build -v q` (`Quionis_DB` Up) | exit 0; 324/324, 0 omitidos |
| 1 | REVIEWER: filtro AC1.1/1.2/1.4/1.5 (8 patrones `FullyQualifiedName~`) | exit 0; 84/84 |
| 1 | REVIEWER: rojo AC1.1 en worktree `HEAD` + solo diff de tests (scratchpad, eliminado) | exit 1; 2/2 con error `{"Save", "Update"} contains 1 item(s) too many` |
| 1 | REVIEWER: greps AC1.3 y `CancellationToken.None` | Coinciden con BUILDER; 0 `CancellationToken.None` |
| 1 | REVIEWER: veredicto | APPROVED (desviación AC1.5 aceptada; ver `REVIEW.md`) |

## Ciclo 1 (BUILDER) - detalle

| Tema | Detalle |
| --- | --- |
| Paso | 01, Q1 = a) (por defecto) |
| Cambios de producto | `IFacilityRepository.cs`: eliminado `Task Update(Facility, CancellationToken)`; `<summary>` de la sobrecarga indica que es la única vía de actualización. `SqlFacilityRepository.cs`: `public Update(Facility, ...)` → `private WriteFacility(Facility, ...)` con `<summary>` (se une a la transacción ambiente del lock); L216 la llama. Cuerpo sin cambios (mantiene su `ExecuteInTransactionAsync`, que con transacción ambiente se ejecuta en línea). |
| Cambios de tests | `SqlFacilityRepositoryShould.cs`: los 3 tests (L255, L309, L431) invocan `Update(id, _ => updated, token)`; aserciones intactas; `minimumCalls: 7` sin cambios, comentario del recuento ajustado. Nueva clase `FacilityRepositoryContractShould` (mismo fichero, sin BD, `using System.Reflection`): `OnlyAcceptAWholeFacilityForCreationSoEveryUpdateGoesThroughTheLock` (interfaz) y `ExposeNoPublicFacilityWriteWithoutTheIdAndTheBuildFunction` (clase). |
| Aceptación (caracterización) | `FacilityFeaturesShould` y `FacilityStandsFeaturesShould` en verde antes (suite base 322/322) y después (63/63 filtro AC1.4). |
| Desviación | AC1.5: el `swagger.json` servido se capturó arrancando `Gaffer.Api` (mismo `Startup`) con `dotnet run` y Dummy, no con `StartupTestServer`, para no añadir un test fuera de plan. `OpenApiContractShould` y `FacilitiesControllerContractShould` (que sí usan el servidor de pruebas) en verde. |
| DI / `TestFixture` | Sin registros nuevos; no aplica. |
| Cliente generado | No tocado; sin build Release. |
| Bloqueos | Ninguno |

