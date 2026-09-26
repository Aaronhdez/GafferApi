# OPTIMIZATION - TECHDEBT-002

## Ciclo 1 (paso 01 - Update parcial de Player, veredicto APPROVED)

### usage-slice (medido:token-consumer)

`node .github/agent-harness/tools/usage-slice.mjs --file "%APPDATA%/Code/User/globalStorage/joynulabedinshokal.token-eater-dashboard/events-*.jsonl" --repo gaffer-api --agent claude-code --window <rol>=<inicio>/<fin>`

Las ventanas literales de `STATUS.md` (`BUILDER 2026-09-26T20:29:17Z/2026-09-26T20:35:13Z`, `REVIEWER 2026-09-26T20:36:13Z/2026-09-26T20:37:28Z`) devolvieron "sin datos". Bisección manual sobre el archivo de eventos (mismo `--repo`, ventanas más anchas) localizó los eventos reales exactamente 2 horas antes de lo registrado en `STATUS.md`:

| Ventana (real, -2h) | Eventos | Entrada nueva | Caché lectura | Caché escritura | Salida | Coste USD | Origen |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| BUILDER 2026-09-26T18:29:17Z/18:35:13Z | 2 | 4 | 210 286 | 166 368 | 719 | 5.36 | medido:token-consumer |
| REVIEWER 2026-09-26T18:36:13Z/18:37:28Z | 1 | 2 | 189 158 | 2 012 | 222 | 0.36 | medido:token-consumer |

Contraste con `medido:agent-result` (totales del ORCHESTRATOR, no desglosados en entrada/caché/salida): BUILDER 76 757 tokens / 52 llamadas / 409 s; REVIEWER 42 817 tokens / 12 llamadas / 104 s.

Los dos conjuntos son consistentes en la relación BUILDER≫REVIEWER en coste, pero el recuento de eventos del dashboard (2 y 1) es implausiblemente bajo frente a las 52 y 12 llamadas a herramienta reportadas por el ORCHESTRATOR; ventanas vecinas (18:35:13-18:36:13 y 18:37:28-18:42:00) muestran actividad adicional del mismo orden de magnitud de caché, lo que indica que `Inicio`/`Fin` no acotan con precisión el turno real (además del desfase de huso horario). Se usan ambos orígenes como referencia cruzada; ninguno se trata como cifra exacta por evento.

### Spike 1 - Desfase de huso horario en `Inicio`/`Fin` de `STATUS.md`

- **Valor y origen**: consulta literal con las ventanas de `STATUS.md` → "sin datos" en las cuatro combinaciones probadas (`--repo gaffer-api` y `--repo TECHDEBT-002`, con y sin `--agent claude-code`). Confirmado por bisección: los eventos reales están 2h antes (18:xx en vez de 20:xx), coincidiendo exactamente con el patrón de una zona horaria local (p.ej. CEST, UTC+2) escrita con sufijo `Z` (UTC) sin conversión.
- **Causa probable**: BUILDER/REVIEWER registran `Inicio`/`Fin` con la hora de reloj local del sistema operativo, etiquetada como si ya fuera UTC, en vez de convertir explícitamente a UTC.
- **Propuesta concreta (P1-1)**: en `CLAUDE.md` sección "Measured Usage (Claude)" (o en el prompt de `.claude/agents/builder.md` / `reviewer.md`, donde se instruye registrar `Inicio`/`Fin`), añadir instrucción explícita de obtener la hora ya convertida a UTC antes de escribirla en `STATUS.md` (p.ej. `Get-Date -AsUTC -Format o` en PowerShell, o equivalente), y no el reloj local sin convertir. Añadir además una nota tipo OPT-008: si las ventanas literales de `STATUS.md` devuelven "sin datos" con `--repo` correcto, reintentar desplazando ±el huso horario local conocido antes de recurrir a `medido:agent-result`.
- **Ahorro esperado**: elimina la bisección manual que este ciclo costó ~9 llamadas a `Bash`/`usage-slice` adicionales a OPTIMIZER (~15-20% de sus propias llamadas a herramienta) para localizar los datos; sin la corrección, cada ciclo futuro repetiría el mismo coste o degradaría a `medido:agent-result` (menos granular, sin desglose caché/entrada/salida).
- **Fuente**: `usage-slice.mjs` (10 invocaciones de bisección, esta sesión), `STATUS.md` líneas 27-31.

### Spike 2 - Caché de escritura elevada en BUILDER (166 368 tokens, ~79% de su coste medido)

- **Valor y origen**: BUILDER cache-write 166 368 vs. REVIEWER cache-write 2 012 en el mismo paso (medido:token-consumer, ventana corregida arriba). Coste USD de BUILDER (5.36) es ~15x el de REVIEWER (0.36) pese a que el read-cache de ambos es similar (210k vs 189k).
- **Causa probable**: `usage/BUILDER.md` (fila ciclo 1) autorreporta la causa: "volcado de esquema OpenAPI con verbosity detailed (filtrado con grep)" y "coste extra por descubrir que el swagger es 2.0 (nulabilidad implícita)" — BUILDER tuvo que investigar en caliente, con un volcado grande del documento OpenAPI, que NJsonSchema/NSwag trata Swagger 2.0 sin `x-nullable`, infiriendo nulabilidad de "no requerido". Ese hallazgo (documentado ahora en `REVIEW.md` hallazgo #1 y en `STATUS.md` "Notas del ciclo 1") no existía antes como conocimiento reutilizable, forzando el volcado verboso para descubrirlo.
- **Propuesta concreta (P1-2)**: registrar la regla de nulabilidad Swagger 2.0/NSwag ("propiedad no listada en `required` ⇒ anulable en el esquema servido por este proyecto, sin `x-nullable`; ver `OpenApiDocument.FromJsonAsync` + `JsonSchemaProperty.IsNullable(document.SchemaType)`") como nota reutilizable en `.github/doc/common/dev-stack.md` (leído por BUILDER/REVIEWER al inicio de ejecución). Así, cualquier paso futuro de este plan o de otros que toque contrato OpenAPI (p.ej. paso 03, filtro de Facilities) puede citar la regla sin repetir el volcado verboso del esquema.
- **Ahorro esperado**: evita repetir un volcado de esquema con `--verbosity detailed` (fuente confirmada del cache-write de 166k) en cada paso futuro que toque nulabilidad de contrato; impacto estimado proporcional al cache-write ya medido en este ciclo (~166k tokens) si se repitiera sin la nota.
- **Fuente**: `usage/BUILDER.md` fila ciclo 1; `REVIEW.md` hallazgo #1; `STATUS.md` "Notas del ciclo 1 (paso 01)".

### Cobertura de MEMORIES.md

Ninguna entrada existente (`OPT-001`..`OPT-008`) cubre el desfase horario de `Inicio`/`Fin` ni la nota de nulabilidad Swagger 2.0; ambas propuestas (P1-1, P1-2) son nuevas. `OPT-008` (atribución de `--repo` por `cwd`) es un problema relacionado pero distinto (aquí el `--repo` fue correcto; el fallo fue de huso horario en la ventana), por lo que no se actualiza su "Efecto observado".

### Brechas de datos

- No se pudo confirmar si el desfase de 2h es exactamente el huso horario del operador (no verificable desde este entorno); se infiere por coincidencia de patrón, no se mide directamente.
- El recuento de eventos del dashboard (2 BUILDER, 1 REVIEWER) es demasiado bajo para corresponder de forma fiable a 52/12 llamadas a herramienta; no se puede descartar solapamiento con turnos vecinos sin leer el JSONL crudo (fuera de alcance de OPTIMIZER). Cifras de caché/coste se reportan como orientativas, no como desglose exacto por rol.

### Estado de aprobación

Pendiente: sin decisión del operador todavía. Ninguna propuesta se ha aplicado ni registrado en `MEMORIES.md`.
