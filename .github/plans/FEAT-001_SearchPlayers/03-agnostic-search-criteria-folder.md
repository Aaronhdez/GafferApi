# Paso 03 (ciclo 3) - Refactor: criterios de búsqueda agnósticos en su propia carpeta

Objetivo: que los tipos de criterio (`ValueCriteria`, `IntervalCriteria`, `OrderCriteria`, sus enums y el contenedor) no dependan de ninguna entidad de dominio ni repositorio y vivan aislados en su propia carpeta (Q9 a): `Gaffer.Api/Shared/Criteria/`, namespace `Gaffer.Api.Shared.Criteria`). **Sin cambio de comportamiento**: es el bucle de refactor del plan; la aceptación es caracterización en verde antes y después.

**Bloqueo**: ninguno. Q8 a), Q9 a) y Q10 a) por defecto (no respondidas explícitamente). Q5 b2) (operador): `IntervalCriteria<T>` ya es genérico y agnóstico de entidad desde el paso 02; este paso solo lo mueve.

## Alcance de ficheros

| Fichero | Cambio |
| --- | --- |
| `Gaffer.Api/Shared/Criteria/*.cs` (nuevo) | Un fichero por tipo: `ValueCriteria`, `ValueCriteriaType`, `IntervalCriteria<T>` (Q5 b2)) y su abstracción no genérica del paso 02, `OrderCriteria`, `OrderDirection`, contenedor (Q9). Sin `using` de `Gaffer.Api.Domain`, `Gaffer.Api.Infrastructure`, `Gaffer.Api.Shared.Repositories`, `Gaffer.Api.Commands`/`Queries` ni Dapper. |
| `Gaffer.Api/Shared/Repositories/IPlayerRepository.cs` | Queda con `IPlayerRepository` y las excepciones de Player; `Get` recibe el contenedor agnóstico; desaparece `PlayerSearchCriteria` (Q9 a)). |
| `SearchCriteria` y `CriteriaType` heredados (Facility, Q8 a)) | Se mueven **sin cambios** de `IPlayerRepository.cs` a un fichero propio en `Gaffer.Api/Shared/Repositories/` (mismo namespace, Facility no cambia). Con Q8 c) no se tocan. |
| `Gaffer.Api/Infrastructure/SqlPlayerRepository.cs`, handlers de Player y sus tests | Solo `using`/nombres. Con Q10 b): además, extracción del constructor SQL a `Gaffer.Api/Infrastructure/` con las listas blancas inyectadas por el repositorio. |

Fuera: comportamiento, SQL generado, controladores, DTO, Facility (sin diff), cliente generado.

## Criterios de aceptación

| AC | Criterio verificable | Capa | Cómo demostrarlo |
| --- | --- | --- | --- |
| AC3.1 | Caracterización **antes** de editar: suite completa en verde y recuento anotado (incluye todos los tests de 01-02). | Aceptación | `dotnet test gaffer-api.sln --verbosity quiet`; fila en `STATUS.md`. |
| AC3.2 | Aislamiento: todos los tipos de criterio están en `Gaffer.Api/Shared/Criteria/` y ninguno de esos ficheros referencia dominio, repositorios, infraestructura, comandos/queries ni Dapper. | Refactor | `grep -rnE "Gaffer\.Api\.(Domain\|Infrastructure\|Shared\.Repositories\|Commands\|Queries)\|Dapper\|Player\|Facility" Gaffer.Api/Shared/Criteria` → 0 resultados; `Glob` de la carpeta. |
| AC3.3 | Agnosticismo de la firma: `IPlayerRepository.Get` recibe el contenedor agnóstico; `PlayerSearchCriteria` y cualquier definición de criterio desaparecen de `IPlayerRepository.cs`. | Refactor | `grep -rn "PlayerSearchCriteria" Gaffer.Api Gaffer.Api.Tests --include=*.cs` → 0; `grep -n "class \|enum " Gaffer.Api/Shared/Repositories/IPlayerRepository.cs` solo muestra la interfaz y las excepciones. |
| AC3.4 | Facility intacta (Q8 a)): sin diff en `SqlFacilityRepository.cs`, `IFacilityRepository.cs`, `DeleteFacilityCommandHandler.cs`, `SearchFacilitiesQueryHandler.cs`, `FacilitiesController.cs` ni en sus tests; `SearchCriteria`/`CriteriaType` heredados con el mismo contenido que antes. | Refactor | `git diff --stat` (sin esos ficheros); `git diff -M` muestra el movimiento de los tipos heredados sin cambios de contenido. |
| AC3.5 | Comportamiento idéntico: los tests de `SqlPlayerRepositoryShould`, `SqlPlayerRepositoryQueryShould`, handlers de Player, `SearchPlayersEndpointShould` y `PlayerFeaturesShould` pasan **sin cambiar sus aserciones** (solo `using`/nombres de tipo); mismo recuento que AC3.1. | Aceptación + unitario | `git diff` de tests limitado a `using`/nombres; `dotnet test` con el mismo recuento. |
| AC3.6 | Contrato y calidad: `OpenApiContractShould` en verde, sin diff en `Gaffer.Api/Controllers/`; build 0 warnings; `dotnet format ... --verify-no-changes` en verde; suite completa en verde. | Refactor | Comandos y resultados en `STATUS.md`. |
| AC3.8 | Estilo LINQ (D-LINQ) conservado: sin bucles imperativos en la construcción de criterios (en `SqlPlayerRepository` y, con Q10 b), en el constructor extraído) ni en los tipos de `Gaffer.Api/Shared/Criteria/`; sin paquetes nuevos. | Refactor | `grep -rnE "\b(for\|foreach\|while)\s*\(" Gaffer.Api/Infrastructure/SqlPlayerRepository.cs Gaffer.Api/Shared/Criteria` (y el fichero extraído si Q10 b)) → 0; `git diff --stat -- '*.csproj'` vacío. |
| AC3.7 | Solo con Q10 b): el constructor SQL extraído no conoce `Player` ni la tabla (recibe tabla y listas blancas) y los tests de SQL de 01-02 siguen pasando sin cambiar sus aserciones. Con Q10 a): no aplica (anotar "N/A, Q10 a)"). | Refactor | `grep` del fichero extraído sin `Player`; `dotnet test`. |
