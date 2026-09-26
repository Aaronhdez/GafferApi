# Paso 09 - Renombrar la tabla `Adresses` a `Addresses`

Prerrequisitos resueltos:
- Q10 = b: BUILDER ejecuta el runner solo en `GafferDB_Test`; el operador migra `GafferDB`.
- Q11 = b: también se renombra la FK.
- Q12 = b: `Down` reversible, revisado sin ejecutarlo; no se amplía el runner.

Depende del paso 08 (el runner de Migrations lee su configuración y permite elegir la BD destino).

## Objetivo

Una migración **nueva** (no se edita `M0002`) renombra `Adresses` a `Addresses`, recrea el trigger `TRG_Facilities_BeforeDelete` para que borre de `Addresses` y recrea la FK como `FK_Facilities_Addresses_Addresses`. El código y los tests usan `Addresses`. Los datos existentes se conservan.

## Alcance de ficheros

| Fichero | Cambio |
| --- | --- |
| `Gaffer.Api.Migrations/Migrations/M0003_RenameAdressesToAddresses.cs` (nuevo; versión `[Migration]` mayor que `202608131010`) | `Up`: `Rename.Table("Adresses").To("Addresses")`; `DROP TRIGGER IF EXISTS TRG_Facilities_BeforeDelete` y crearlo de nuevo con `DELETE FROM Addresses`; borrar `FK_Facilities_Addresses_Adresses` y crear `FK_Facilities_Addresses_Addresses` con la misma columna, tabla destino y `ON DELETE CASCADE`. `Down`: lo inverso (tabla, trigger y FK como en `M0002`). |
| `Gaffer.Api/Infrastructure/SqlFacilityRepository.cs` | `Adresses` → `Addresses` (L66, L165, L238). |
| `Gaffer.Api.Tests/TestShared/DbTableNames.cs` | `AddressesTable = "Addresses"`. |
| `Gaffer.Api.Tests/Unit/Repositories/SqlFacilityRepositoryShould.cs` | `Adresses` → `Addresses` (L68, L265, L337). |
| `Gaffer.Api.Migrations/Migrations/M0002_CreateFacilitiesSchema.cs` | **Sin cambios** (ya está aplicada). |
| `Gaffer.Api.Tests/Integration/...` (nuevo, p. ej. `DatabaseSchemaShould.cs`) | Test de esquema contra `GafferDB_Test`. |

`.github/doc/features/facility-create.md` L25 menciona `Adresses`: lo revisa DOCUMENTER al cerrar, no BUILDER.

## Criterios de aceptación

| ID | Criterio |
| --- | --- |
| AC9.1 | En `GafferDB_Test`, `information_schema.TABLES` tiene `Addresses` y no tiene `Adresses`. |
| AC9.2 | El trigger `TRG_Facilities_BeforeDelete` existe y su cuerpo (`information_schema.TRIGGERS.ACTION_STATEMENT`) referencia `Addresses` y no `Adresses`. |
| AC9.3 | Al borrar una facility se borran su fila de `Addresses` y sus `Stands` (tests de borrado de `FacilityFeaturesShould`/`SqlFacilityRepositoryShould` en verde, más un test que borra directamente en `Facilities` y comprueba que `Addresses` queda vacío para esa facility). |
| AC9.4 | `information_schema.REFERENTIAL_CONSTRAINTS` tiene `FK_Facilities_Addresses_Addresses` con `REFERENCED_TABLE_NAME = 'Addresses'` y `DELETE_RULE = 'CASCADE'`, y no tiene `FK_Facilities_Addresses_Adresses`. |
| AC9.5 | Los datos se conservan: una fila insertada en `Adresses` antes de migrar sigue en `Addresses` después, con el mismo `Id`, y enlazada desde `Facilities_Addresses`. |
| AC9.6 | `Down` devuelve el esquema al estado de `M0002` (tabla, trigger y FK antiguos). No se ejecuta (Q12 = b): REVIEWER lo revisa comparándolo con `M0002` y `STATUS.md` lo anota como "no ejecutado". |
| AC9.7 | Ninguna aparición de `Adresses` en `*.cs` fuera de `M0002` y de la nueva migración. |
| AC9.8 | BUILDER ejecuta el runner solo en `GafferDB_Test`; `GafferDB` no se toca (lo comprueba `SELECT MAX(Version) FROM GafferDB.VersionInfo`, que no cambia) y queda como acción del operador. |
| AC9.9 | `dotnet test gaffer-api.sln` en verde contra `GafferDB_Test` ya migrada, 0 omitidos. |

## Doble bucle

1. **Aceptación**: tests AC9.1-AC9.3 contra `GafferDB_Test` sin migrar → red (la tabla se llama `Adresses`). Crear la migración, ejecutar el runner en `GafferDB_Test` y cambiar el repositorio → green. Para AC9.5, insertar la fila antes de ejecutar el runner y comprobarla después (evidencia manual con consulta SQL si no se puede automatizar; registrarla).
2. **Unitario**: `SqlFacilityRepositoryShould` con `Addresses` (red mientras el repositorio siga usando `Adresses` tras migrar → green).
3. **Refactor**: AC9.6 (revisión de `Down`), AC9.7 y suite completa.

## Cómo demostrarlo

- Salida del runner (`dotnet run --project Gaffer.Api.Migrations` solo contra `GafferDB_Test`), consultas a `information_schema` y `dotnet test gaffer-api.sln --filter "FullyQualifiedName~DatabaseSchema|FullyQualifiedName~Facility"` red → green en `STATUS.md`.
- Suite completa en verde.
- Acción del operador pendiente: ejecutar el runner en `GafferDB`.
