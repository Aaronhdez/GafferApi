# Paso 05 (ciclo 5) - Health check con comprobación de BD (se mantiene el fallback a Dummy)

Antes paso 04 (`04-database-health-check.md`); renumerado por OPT-036. Correspondencia: `AC5.N` = antiguo `AC4.N` (mismo texto salvo la referencia al ayudante de AC3a.0 en AC5.6).

Objetivo: que el estado de la base de datos sea visible. Si la BD no responde al arrancar con `Enabled = true`, `DatabaseStartup.ConfigureDatabase` sigue registrando `DummyConnectionProvider` (Q12 "Mantener Dummy"), pero ahora `/health/ready` lo informa como `Unhealthy` (503). Skill: `security-and-hardening` (el endpoint es anónimo: no expone excepciones, cadena de conexión ni versiones).

**Bloqueo**: ninguno por preguntas (Q11 y Q12 respondidas; Q13, Q14 y Q15 con valor por defecto (a)). Usa el ayudante de AC3a.0 (paso 03a).

## Contrato

| Ruta | Situación | Respuesta |
| --- | --- | --- |
| `GET /health/live` | Siempre que el proceso atienda; no consulta la BD | 200 `Healthy` |
| `GET /health/ready` | `Enabled = true`, `SqlConnectionProvider` registrado y BD alcanzable | 200 `Healthy` |
| | `Enabled = true`, `SqlConnectionProvider` registrado y BD inalcanzable o comprobación > 5 s | 503 `Unhealthy` |
| | `Enabled = true` y **Dummy registrado por fallback** (la sonda de arranque falló) | 503 `Unhealthy`, **aunque la BD vuelva** (la API no se recupera sin reinicio, Q12) |
| | `Enabled = false` o sin cadena (Dummy intencionado, Q13 a)) | 200 `Degraded` |

- Cuerpo de texto (`Healthy`/`Degraded`/`Unhealthy`), sin paquete nuevo (health checks del framework compartido de ASP.NET Core). Fuera de `/api/v{version}` y fuera del OpenAPI/NSwag (Q14 a)): el cliente no cambia y el job `client-and-package` no se ve afectado (Q15 a)).
- La comprobación no puede basarse en `IDatabaseConnectionProvider.TestConnection()` del proveedor registrado, porque `DummyConnectionProvider.TestConnection()` devuelve `true` (L5). Tampoco basta con sondear la cadena configurada: tras un fallback daría 200 si la BD vuelve mientras la API sigue usando Dummy. `DatabaseStartup` debe dejar registrado el motivo del proveedor elegido (Sql, Dummy por configuración, Dummy por fallback); la comprobación de BD solo sondea cuando el proveedor es Sql (diseño de BUILDER).
- Política de registro sin cambios: `DatabaseStartupShould` L75, L86 y L110 siguen fijando el fallback actual.

## Alcance de ficheros

| Fichero | Cambio |
| --- | --- |
| `Gaffer.Api/Startups/DatabaseStartup.cs` | Registrar el motivo del proveedor elegido; registro de la comprobación de BD. Sin cambiar qué proveedor se elige. |
| `Gaffer.Api/Startup.cs` | `AddHealthChecks` y mapeo de `/health/live` y `/health/ready`. |
| `Gaffer.Api/Shared/` o `Gaffer.Api/Infrastructure/` (fichero nuevo) | Comprobación de BD (`IHealthCheck`). |
| Tests: `Integration/DatabaseStartupShould.cs` (aserciones nuevas sobre el motivo registrado; las existentes no cambian), `Acceptance/` (nuevo `HealthCheckShould.cs` con `StartupTestServer`, OPT-022), `Acceptance/OpenApiContractShould.cs`, `Unit/` (comprobación de BD), `TestFixture.cs` y `Integration/TestServerDependenciesShould.cs` si cambia el registro de servicios | Tests. |

Fuera: `.github/workflows/ci.yml` (Q15 a)); cliente generado.

## Criterios de aceptación

| ID | Criterio |
| --- | --- |
| AC5.1 | Aceptación, BD disponible (Docker local): `GET /health/live` → 200 `Healthy` y `GET /health/ready` → 200 `Healthy`. |
| AC5.2 | Aceptación, fallback (`Enabled = true`, cadena a un puerto cerrado, como `DatabaseStartupShould` L75): el host arranca, el `IDatabaseConnectionProvider` resuelto es `DummyConnectionProvider` (como hoy) y `/health/ready` → 503 `Unhealthy` (**no** 200, aunque `DummyConnectionProvider.TestConnection()` devuelva `true`); `/health/live` → 200. |
| AC5.3 | Aceptación, BD deshabilitada (`DatabaseConfig:Enabled = false`, como el job `client-and-package`) y sin cadena: el host arranca y `/health/ready` → 200 `Degraded` en ambos casos. |
| AC5.4 | Unitario, fallback irrecuperable: con el motivo "Dummy por fallback", la comprobación devuelve `Unhealthy` aunque una sonda de BD simulada responda bien (sin simular la caída y vuelta de `Quionis_DB`). Con "Dummy por configuración" → `Degraded`; con Sql y sonda correcta → `Healthy`; con Sql y sonda fallida → `Unhealthy`. |
| AC5.5 | Seguridad y tiempo: el cuerpo de `/health/*` no contiene la cadena de conexión, el usuario, el host ni el texto de excepciones (aserción sobre el cuerpo en AC5.2 y en el caso Sql inalcanzable de AC5.4); con Sql registrado y una sonda que no responde, la comprobación termina en ≤ 5 s con `Unhealthy` y respeta el token de cancelación (unitario con sonda bloqueada; tiempo medido en `STATUS.md`). |
| AC5.6 | Contrato: el OpenAPI servido no contiene rutas `/health` y es idéntico antes y después (Q14 a)). Cliente generado sin tocar. **Procedimiento (OPT-032 + OPT-034)**: si el diff no toca controladores (`Gaffer.Api/Controllers/`), DTOs de petición/respuesta ni `Startup.cs`/`Startups/*Startup.cs`, bastan `OpenApiContractShould` y `FacilitiesControllerContractShould` en verde, sin captura manual. Si los toca (el alcance prevé cambios en `Startup.cs` y `Startups/DatabaseStartup.cs`), se compara el volcado del ayudante de AC3a.0 (`StartupTestServer`, OPT-022) antes y después, incluida la ausencia de `/health`; no con `dotnet run` ni con una sonda temporal. `STATUS.md` indica qué rama se aplicó y por qué. |
| AC5.7 | Integración: `DatabaseStartupShould` sigue en verde sin cambiar sus aserciones actuales (L75, L86, L110) y añade la del motivo registrado en los tres casos (Sql, Dummy por configuración, Dummy por fallback). |
| AC5.8 | Regresión: suite completa en verde (recuento antes/después), build 0 warnings, `TestServerDependenciesShould` en verde, `grep -rn "CancellationToken.None" Gaffer.Api --include=*.cs` → 0. |

## Doble bucle

1. **Aceptación**: AC5.1-AC5.3 rojo (404 de ruta inexistente) → verde.
2. **Unitario**: AC5.4, AC5.5 y AC5.7 rojo → verde; AC5.6 comparación.
3. **Refactor**: separar la comprobación de BD del registro; revalidar suite y build.

## Cómo demostrarlo

- Filtro de los tests del paso en rojo y verde; tiempos observados de AC5.5 en `STATUS.md`.
- AC5.6 según su procedimiento, con el ayudante de AC3a.0.
- Suite completa y build `--no-incremental -v q`.

## Notas

- Con Q14 a) no hay cambio de cliente. Con Q15 a) no cambia el workflow.
- Q12 mantiene el estado irrecuperable tras un fallback: readiness lo hace visible, pero recuperarse exige reiniciar la API.
