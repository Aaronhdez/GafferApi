# OPTIMIZATION - BUGFIX-001

## Ciclo 1 - OPTIMIZER

### Uso medido (`usage-slice.mjs`, ventanas de `STATUS.md`)

Comando: `node .github/agent-harness/tools/usage-slice.mjs --file "%APPDATA%/Code/User/globalStorage/joynulabedinshokal.token-eater-dashboard/events-*.jsonl" --repo gaffer-api --window BUILDER=2026-09-26T10:22:59Z/2026-09-26T10:35:05Z --window REVIEWER=2026-09-26T10:36:17Z/2026-09-26T10:38:50Z`

| Ventana | Agente | Eventos | Entrada nueva | Caché lectura | Caché escritura | Salida | Coste USD | Origen |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| BUILDER (10:22:59–10:35:05Z) | claude-code | 63 | 126 | 5.587.805 | 128.942 | 18.432 | 12.18 | `medido:token-consumer` |
| REVIEWER (10:36:17–10:38:50Z) | claude-code | 12 | 24 | 515.809 | 47.705 | 4.125 | 1.98 | `medido:token-consumer` |

Cotejo con `medido:agent-result` (totales del ORCHESTRATOR): BUILDER `subagent_tokens=149.111`, `tool_uses=98`; REVIEWER `subagent_tokens=63.394`, `tool_uses=15`. La cifra de `agent-result` (149k/63k) es del orden de 40x menor que el total bruto de `token-consumer` (5.73M/0.59M) porque `agent-result` refleja el tamaño de contexto de la última llamada, mientras que `token-consumer` suma la lectura de caché repetida en cada uno de los turnos de la conversación. Ambas fuentes coinciden en la proporción BUILDER/REVIEWER (~2,35x en `agent-result`, ~9,7x en `token-consumer` en tokens totales, ~6,1x en coste). El recuento de eventos también difiere entre fuentes (63 vs 98 en BUILDER; 12 vs 15 en REVIEWER): brecha de datos, ver más abajo.

### Spikes detectados

| # | Valor | Origen | Causa probable |
| --- | --- | --- | --- |
| S1 | Entrada nueva ínfima (126 y 24 tokens) frente a caché de lectura masiva (5,59M y 0,52M) | `medido:token-consumer` | Normal en una conversación agente única y larga con prompt caching: cada turno reenvía el historial acumulado, que se resuelve casi todo por caché. No es un fallo puntual, escala con el número de turnos. |
| S2 | BUILDER: 1 ejecución roja de tests repetida por un `grep` en inglés (`Passed`/`Failed`) contra salida de consola en español (`Correctas`/`Con error`) | `usage/BUILDER.md` fila única, observación textual | Los mensajes de `dotnet test` dependen de la configuración regional (`es-ES`); un patrón de grep fijado en inglés no encuentra coincidencias y obliga a repetir el comando para diagnosticar. |
| S3 | Paso 02: verificación en dos fases (`Green parcial` con 4/6, luego `Green final` tras el paso 04) | `STATUS.md` filas paso 02 (líneas 41-42) | Los AC2.1/AC2.2 de `DatabaseStartupShould` no pueden ponerse en verde sin el bug 4 (`Parameters`), corregido en el paso 04. El plan documenta la dependencia inversa (04→02) pero no advierte que 02 también depende de 04 para su verificación completa, lo que exige una comprobación adicional después de completar 04. |
| S4 | Desviación de alcance: 2 ficheros fuera de la lista de ficheros del paso 02 tocados por `NUnit1032` (`Features_Refactor/PlayerFeatureShould.cs`, `Unit/Repositories/SqlFacilityRepositoryShould.cs`) | `STATUS.md` "Desviación de alcance"; `REVIEW.md` F1 | Introducir `IDisposable`/`IAsyncDisposable` en `SqlConnectionProvider` activa un analizador de severidad error en fixtures existentes que crean el proveedor en `[SetUp]`. Aceptada por REVIEWER, coste de tokens bajo (+1 línea x2), pero exige que BUILDER decida un remedio ad hoc y REVIEWER lo valide como desviación. |

### Propuestas

**P1-1 (principal) — APROBADA (registrada como OPT-005 en `MEMORIES.md`).** Sustituir el grepeo de literales de estado ("Passed"/"Failed" o su equivalente en español) por la comprobación del código de salida del proceso (`$LASTEXITCODE` en PowerShell / `$?` en Bash) para decidir rojo/verde, reservando `grep` solo para extraer detalle de fallos cuando el código de salida sea distinto de cero. Esto es independiente de la configuración regional de la consola de .NET y evita la repetición observada en S2.
- Ámbito: `CLAUDE.md` sección "Minimal Reading" (o el skill `debugging-and-error-recovery` que usa BUILDER en fallos de test/build), añadiendo la instrucción de usar el código de salida como criterio primario.
- Ahorro esperado: evita como mínimo la repetición de 1 ejecución de test completa por ciclo cuando ocurre este patrón; con ~88.700 tokens de caché medios por evento en BUILDER (5,59M / 63), eliminar 1-2 eventos de ese tipo ahorraría del orden de 90k-180k tokens de caché (~2-3% de la ventana BUILDER) y el tiempo de diagnóstico asociado. Estimado, no medido de forma aislada.
- Fuente: `usage/BUILDER.md` (observación textual, `estimado`), `usage-slice.mjs` (coste/caché de la ventana BUILDER, `medido:token-consumer`).

**P1-2 — RECHAZADA (checkpoint ciclo 1, operador no la seleccionó).** Cuando un paso posterior deba corregir un defecto que bloquea el criterio de aceptación "verde" de un paso anterior (dependencia inversa), que PLANNER lo declare explícitamente en el paso anterior (no solo en el posterior) con una nota "Green diferido hasta el paso N", para que BUILDER planifique una sola pasada de verificación combinada en vez de un ciclo parcial + uno final.
- Ámbito: skill `planning-and-task-breakdown` / plantilla de paso de PLANNER.
- Ahorro esperado: bajo pero no nulo; evita una invocación de test adicional por cada par de pasos con esta dependencia cruzada. Sin datos aislados para cuantificar (afecta 1 de 4 pasos en este ciclo).
- Fuente: `STATUS.md` filas del paso 02 (`medido:transcript` no consultado; evidencia en el propio `STATUS.md`, no en `usage-slice`).

**P1-3 (menor, informativa) — RECHAZADA (checkpoint ciclo 1, operador no la seleccionó).** Cuando un paso introduce `IDisposable`/`IAsyncDisposable` en un tipo ya usado por fixtures existentes, que PLANNER anote en el propio paso la posibilidad de que `NUnit1032` fuerce tocar `[TearDown]` en fixtures fuera de la lista de ficheros, autorizando de antemano ese único tipo de ajuste mínimo. Reduce la deliberación de BUILDER y la verificación de alcance de REVIEWER (F1) a una comprobación de checklist en vez de un análisis desde cero.
- Ámbito: plantilla de paso de PLANNER (nota de alcance).
- Ahorro esperado: bajo, principalmente en turnos de deliberación/verificación, no en tokens de caché medibles con los datos actuales.
- Fuente: `REVIEW.md` hallazgo F1, `STATUS.md` "Desviación de alcance".

### Brechas de datos

- El número de eventos de `token-consumer` (63 BUILDER / 12 REVIEWER) no coincide con `tool_uses` de `agent-result` (98 / 15); no se puede determinar si la diferencia es agregación de eventos por turno o llamadas no instrumentadas por el dashboard. No se dispone de una tercera fuente (`medido:transcript`) para dirimirlo en este ciclo.
- No hay desglose medido de tokens por paso (01-05) dentro de la ventana BUILDER; el spike S3 se apoya solo en la evidencia narrativa de `STATUS.md`, no en una ventana `usage-slice` separada por paso.
- `usage/BUILDER.md` y `usage/REVIEWER.md` no registran huella de tokens propia (correcto conforme a la limitación conocida: el subagente no ve su consumo), así que toda cifra de tokens de esta sección proviene de `token-consumer` o `agent-result`, nunca del propio subagente.

### Memorias

Ninguna propuesta de este ciclo coincide con `MEMORIES.md` (OPT-001 a OPT-004, todas de planes anteriores y de alcance de flujo de trabajo/documentación, no de patrones de test/CLI). En modo análisis no se registró ninguna fila: sin aprobación del operador todavía.

**Registro (ciclo 1, modo registro).** El operador aprobó únicamente P1-1 ("a") en el checkpoint del ciclo 1; P1-2 ("b") y P1-3 ("c") quedaron fuera de la selección y se marcan rechazadas. P1-1 se registró como `OPT-005` en `MEMORIES.md` con Estado `pendiente` y Efecto observado `sin datos`.
