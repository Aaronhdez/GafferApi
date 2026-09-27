# Paso 05 (ciclo 5) - CI con GitHub Actions

Decisiones del operador (literales): "GH Actions + NSwag/paquete", MariaDB "latest", NSwag "Generar y fallar si difiere". El workflow corre en push y PR a `main`: build (0 warnings), migraciones con `Gaffer.Api.Migrations`, tests contra un servicio `mariadb:latest`, build Release que genera el cliente NSwag y falla si difiere del versionado, y empaquetado del `.nupkg` como artefacto sin publicar.

**Bloqueo**: Q17 (runners), Q18 (evidencia del run) y Q19 (cliente regenerado antes del run). Q20-Q22 tienen valor por defecto.

## Objetivo

Un workflow en `.github/workflows/` que valida en cada push/PR a `main` lo que los pasos 01-04 dejan en local, sin publicar nada y sin que ningún agente regenere ni verifique la salida generada.

## Diseño (Q17 = a), recomendación)

| Job | Runner | Pasos |
| --- | --- | --- |
| `build-and-test` | `ubuntu-latest`, servicio `mariadb:latest` (puerto 3306, health check, credenciales según Q21) | checkout; `actions/setup-dotnet` con `global-json-file: global.json`; `dotnet restore`; `dotnet build gaffer-api.sln --no-restore -warnaserror` (Q20; Debug, así que el target NSwag no corre); migraciones con `pwsh ./scripts/run-migrations.ps1` y `ConnectionStrings__GafferDB_Test` (Q22); `dotnet test gaffer-api.sln --no-build` con `ConnectionStrings__Database` (misma BD de test; nombres de claves según `scripts/setup-local-environment.ps1`) |
| `client-and-package` | `windows-latest`, `needs: build-and-test` | checkout; setup-dotnet; `dotnet build gaffer-api.sln -c Release` (el target NSwag regenera `Gaffer.Api.Client/GafferApiClient.cs` y `OpenApiSpecs.json`, y `GeneratePackageOnBuild` crea el `.nupkg`); `git diff --exit-code -- Gaffer.Api.Client/GafferApiClient.cs Gaffer.Api.Client/OpenApiSpecs.json` (falla si difiere; con `.gitattributes` del paso 01 el fin de línea no genera diferencias falsas); `actions/upload-artifact` de `Gaffer.Api.Client/bin/Release/*.nupkg` (y `.symbols.nupkg`) |

- Triggers: `push` y `pull_request` con `branches: [main]`. Sin `workflow_dispatch` ni publicación (no hay requisito).
- Permisos mínimos del token (`contents: read`).
- `mariadb:latest` por decisión del operador; el riesgo (versión cambiante) queda anotado, no se fija versión.

## Alcance de ficheros

| Fichero | Cambio |
| --- | --- |
| `.github/workflows/ci.yml` (nombre a elección de BUILDER) | Nuevo. |
| `scripts/run-migrations.ps1` | Solo si no funciona sin interacción en Linux/CI (se justifica en `STATUS.md`). |
| `Gaffer.Api.Tests/TestConfiguration.cs` | Solo si las claves de conexión por variable de entorno no bastan en CI (se justifica). |

Fuera: `Gaffer.Api.csproj` (target NSwag; solo con Q17 = b)); salida generada de `Gaffer.Api.Client`; `Directory.Build.props`; documentación de stack (cierre).

## Criterios de aceptación

| ID | Criterio |
| --- | --- |
| AC5.1 | El YAML es válido: se parsea sin errores (y `actionlint` sin errores si está instalado; si no, se registra como no disponible). Triggers, jobs, `needs`, servicio `mariadb:latest` y permisos como en el diseño. |
| AC5.2 | Reproducción local del job `build-and-test` (sin push): los mismos comandos (`restore`, `build -warnaserror`, migraciones con `run-migrations.ps1` sobre `GafferDB_Test`, `test`) en local contra el Docker del operador → 0 warnings, migraciones con código 0, suite en verde. Si `act` está disponible, ejecución del job Linux con `act` y su resultado; si no, se registra. |
| AC5.3 | El job `client-and-package` **no se ejecuta en local** (el build Release regeneraría la salida generada, fuera de alcance de BUILDER). Se demuestra solo con el run real (AC5.4). |
| AC5.4 | Run real en GitHub (Q18 = a)): el operador sube la rama/abre PR tras BUILDER y anota en `STATUS.md` la URL del run, el commit y el resultado de cada job. Ambos jobs en verde y el artefacto `.nupkg` presente en el run. Sin esta evidencia el AC queda **no demostrado** y el paso no puede quedar `APPROVED`. |
| AC5.5 | El control "fallar si difiere" puede fallar: evidencia de un run en rojo por diferencia del cliente (p. ej. Q19 = b): run anterior a la regeneración del operador), con el paso `git diff --exit-code` como causa. Si el operador no hace ese run, se registra como no demostrado (no bloquea AC5.4). |
| AC5.6 | Ningún secreto real en el YAML (solo credenciales de un contenedor efímero según Q21) y ningún paso publica paquetes (`grep -n "nuget push\|dotnet nuget" .github/workflows/*.yml` → 0). |
| AC5.7 | Regresión local: suite completa y build 0 warnings sin cambios respecto al ciclo 4. |

## Doble bucle

Paso de infraestructura sin lógica de producto:

1. **Aceptación**: AC5.4 (red: no existe workflow → no hay run; o AC5.5 con el cliente desfasado) → green con el run del operador.
2. **Unitario**: AC5.1-AC5.2 (validación estática y reproducción local de comandos).
3. **Refactor**: limpieza del YAML (caché de NuGet opcional), revalidar AC5.1-AC5.2.

## Cómo demostrarlo

- `STATUS.md`: comandos locales de AC5.2 con código de salida; resultado de `actionlint`/`act` o "no disponible".
- `STATUS.md`: URL(s) del run, commit y resultado por job aportados por el operador (AC5.4, AC5.5). REVIEWER los contrasta con el YAML del diff; no los da por demostrados sin URL.
