# OPTIMIZATION - TECHDEBT-004

## Ciclo 1 - Paso 01 (TreatWarningsAsErrors + .gitattributes + cierre de conexión)

### Uso medido (`usage-slice`)

`usage-slice` sí devolvió datos con las ventanas literales de `STATUS.md` y `--repo gaffer-api` (a diferencia de TECHDEBT-003, que tuvo vacío total de telemetría todo el plan):

```
node .github/agent-harness/tools/usage-slice.mjs --file "%APPDATA%/Code/User/globalStorage/joynulabedinshokal.token-eater-dashboard/events-*.jsonl" --repo gaffer-api --window BUILDER=2026-09-27T17:30:59Z/2026-09-27T17:34:46Z --window REVIEWER=2026-09-27T17:36:05Z/2026-09-27T17:38:43Z
```

| Ventana | Eventos | Entrada nueva | Caché lectura | Caché escritura | Salida | Coste USD | Origen |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| BUILDER | 20 | 40 | 782 319 | 39 471 | 4 300 | 2,24 | medido:token-consumer |
| REVIEWER | 8 | 16 | 211 424 | 19 767 | 133 | 0,70 | medido:token-consumer |

Totales: BUILDER 826 130 tokens; REVIEWER 231 340 tokens. En ambos, la caché (lectura+escritura) es >99,99 % del tráfico (BUILDER 99,995 %; REVIEWER 99,993 %); la entrada fresca es casi nula (40 y 16 tokens respectivamente) — todo el coste es re-transmisión de contexto ya visto, no contenido nuevo.

Comparación con las otras dos fuentes disponibles para este ciclo:

| Fuente | BUILDER (tokens) | REVIEWER (tokens) | Ratio vs. `medido:token-consumer` |
| --- | ---: | ---: | --- |
| `medido:token-consumer` (usage-slice) | 826 130 | 231 340 | referencia |
| `medido:agent-result` (ORCHESTRATOR) | 61 231 | 41 898 | BUILDER 13,49x por debajo; REVIEWER 5,52x por debajo |
| `estimado` (`usage/*.md`, autoinforme del subagente) | ~64 000 (~55k+~9k) | ~51 000 (~45k+~6k) | BUILDER 12,9x por debajo; REVIEWER 4,5x por debajo |

### Spikes detectados

| Spike | Valor / origen | Causa probable |
| --- | --- | --- |
| S1 | Brecha estimado/medido y medido:agent-result/medido:token-consumer, ambas de la misma magnitud: el autoinforme de BUILDER (~64k) y el total pasado por el ORCHESTRATOR (61 231) coinciden entre sí dentro de un 5%, pero ambos infravaloran el total realmente medido por `usage-slice` (826 130) en ~13x. Mismo patrón en REVIEWER (~51k / 41 898 vs. 231 340, ~5x) | `medido:agent-result` no es una medición independiente de la caché: su valor casi coincide con el autoinforme "a ciegas" del propio subagente (que no ve caché, per `CLAUDE.md` "Measured Usage"), lo que indica que el total que el harness pasa al ORCHESTRATOR excluye — o subestima gravemente — la lectura/escritura de caché acumulada turno a turno. Es la primera vez en este repositorio que se dispone de `medido:token-consumer` y `medido:agent-result` simultáneamente para el mismo ciclo, así que es la primera confirmación directa de esta brecha (antes solo se sospechaba por la nota del ORCHESTRATOR sobre discrepancia de `tool_uses`) |
| S2 | Discrepancia de recuento de "llamadas": 32 (`tool_uses`, `medido:agent-result`) vs. ~22 (autoinforme `usage/BUILDER.md`, `estimado`) vs. 20 (`eventos`, `usage-slice`) para la misma sesión de BUILDER | Las tres cifras cuentan unidades distintas (invocaciones de herramienta individuales vs. turnos de API que pueden agrupar varias) y no son intercambiables; no hay evidencia de un problema real de reintentos o llamadas duplicadas — el propio `STATUS.md` documenta ~11 comandos distintos más las llamadas de lectura/edición de ficheros, coherente con cualquiera de los tres rangos |
| S3 (no es una regresión, es una confirmación positiva) | Caché por evento de BUILDER este ciclo: (782 319+39 471)/20 ≈ 41 090 tokens/evento. El único punto de comparación histórico equivalente (sesión de un solo paso, ya con `OPT-006`/`OPT-007` aplicados de forma informal) es TECHDEBT-001 ciclo 2: 696 170 caché / 17 eventos ≈ 40 951 tokens/evento (`MEMORIES.md`, efecto de `OPT-007`) | La cifra de este ciclo (41 090) es prácticamente idéntica a la de TECHDEBT-001 ciclo 2 (40 951), pese a ser un plan distinto y una sesión con más aserciones (5 tests nuevos + mutación AC1.3 con 3 builds de mutación). Esto es evidencia cruzada de que la práctica informal de `OPT-006` (resumen de builds) y `OPT-007` (no reabrir salidas ya resumidas en `STATUS.md`) se sostiene por tercer plan consecutivo sin necesidad de haberse formalizado aún en `CLAUDE.md`/`builder.md` |

No se repiten propuestas ya registradas en `MEMORIES.md`. S1 es un hallazgo nuevo (brecha de fiabilidad de la fuente `medido:agent-result`, no cubierto por `OPT-008`/`OPT-009`/`OPT-011`, que tratan ausencia de datos de `usage-slice`, no su discrepancia con `agent-result` cuando ambos existen). S3 no es una propuesta nueva: es un dato de refuerzo para `OPT-006`/`OPT-007` (ambos `pendiente` desde TECHDEBT-001), registrado aquí porque no se puede editar `MEMORIES.md` en este modo de análisis.

### Propuestas

| ID | Propuesta | Ámbito | Ahorro esperado | Fuente |
| --- | --- | --- | --- | --- |
| P1-1 | Añadir una nota en `CLAUDE.md`, sección "Measured Usage (Claude)", advirtiendo que `medido:agent-result` (el total que el ORCHESTRATOR pasa desde el resultado de `Agent`) puede excluir o subestimar gravemente la caché de lectura/escritura acumulada turno a turno — en esta primera comparación directa con `medido:token-consumer` para el mismo ciclo, la brecha fue de 13,5x (BUILDER) y 5,5x (REVIEWER), y el valor de `agent-result` coincidió, en cambio, con el autoinforme "a ciegas" del propio subagente. Instruir a OPTIMIZER a etiquetar explícitamente cualquier cifra de `agent-result` como cota inferior, no como equivalente de `token-consumer`, cuando ambas fuentes estén disponibles para el mismo ciclo | `CLAUDE.md` (sección "Measured Usage (Claude)") | Evita que un futuro ciclo sin `usage-slice` disponible (fallback a `agent-result`) se interprete como "más barato" de lo que realmente es, sesgando la priorización de optimizaciones; no cuantificable en tokens (es una corrección de interpretación, no de coste) | `medido:token-consumer` (tabla de arriba) vs. `medido:agent-result` (totales pasados por el ORCHESTRATOR para este ciclo) vs. `usage/BUILDER.md`/`usage/REVIEWER.md` (autoinforme `estimado`) |

### Brechas de datos

- No hay ciclo previo de este plan con el que comparar crecimiento entre ciclos (este es el ciclo 1 de TECHDEBT-004).
- No es posible aislar, dentro de los 826 130 tokens de BUILDER, qué fracción corresponde específicamente a la mutación de prueba de AC1.3 (3 builds adicionales) frente al resto del paso 01; `usage-slice` no desglosa por comando/turno, solo por ventana temporal completa.
- La cifra de S3 (paridad de caché/evento con TECHDEBT-001 ciclo 2) es indicativa, no concluyente: son dos planes distintos con alcance de paso distinto (5 tests + mutación vs. 1 paso simple); se necesitaría una tercera medición con alcance más comparable para confirmar que la práctica informal de `OPT-006`/`OPT-007` explica la paridad y no es coincidencia.
- Sin la ventana de PLANNER (ciclo 0) medida con `usage-slice`, no se puede comparar el coste de planificación de este plan con el patrón de replanificación de `OPT-017`/`OPT-020` (TECHDEBT-003).

### Estado de aprobación

P1-1: **Aprobada**. Operador, instrucción verbatim "opción b y luego a" (interpretada por el ORCHESTRATOR como aprobación de P1-1), 2026-09-27. Registrada como `OPT-023` en `.github/agent-harness/MEMORIES.md` (`Estado: pendiente`, `Efecto observado: sin datos`); la aplicación a `CLAUDE.md` queda pendiente de que el operador la lleve a cabo (OPTIMIZER no edita `CLAUDE.md`).

S3 (refuerzo de `OPT-006`/`OPT-007`, no es una propuesta nueva) no se registra como fila propia en `MEMORIES.md`; ya está reflejado como evidencia cruzada de dato adicional para esas dos entradas existentes (ver arriba).

## Ciclo 2 - Paso 02 (Entorno desde variable de entorno)

### Uso medido (`usage-slice`)

Ventanas literales de `STATUS.md` (`Inicio`/`Fin` del ciclo 2), sin necesidad de bisección (a diferencia de `OPT-009`/`OPT-011`):

```
node .github/agent-harness/tools/usage-slice.mjs --file "%APPDATA%/Code/User/globalStorage/joynulabedinshokal.token-eater-dashboard/events-*.jsonl" --repo gaffer-api --window BUILDER=2026-09-27T17:54:07Z/2026-09-27T17:57:48Z --window REVIEWER=2026-09-27T18:00:01Z/2026-09-27T18:02:40Z
```

| Ventana | Eventos | Entrada nueva | Caché lectura | Caché escritura | Salida | Coste USD | Origen |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| BUILDER | 16 | 32 | 547 048 | 33 796 | 3 353 | 1,71 | medido:token-consumer |
| REVIEWER | 7 | 14 | 179 222 | 20 584 | 96 | 0,66 | medido:token-consumer |

Totales: BUILDER 584 229 tokens; REVIEWER 199 916 tokens. Caché (lectura+escritura) sigue siendo casi todo el tráfico: BUILDER 99,42 % (580 844/584 229), REVIEWER 99,95 % (199 806/199 916); entrada fresca prácticamente nula en ambos (32 y 14 tokens).

### Comparación con ciclo 1 y con las otras fuentes

| Métrica | Ciclo 1 | Ciclo 2 | Variación |
| --- | ---: | ---: | --- |
| BUILDER total (`medido:token-consumer`) | 826 130 | 584 229 | −29,3 % |
| REVIEWER total (`medido:token-consumer`) | 231 340 | 199 916 | −13,6 % |
| BUILDER caché/evento | 821 790/20 ≈ 41 090 | 580 844/16 ≈ 36 303 | −11,7 % |
| REVIEWER caché/evento | 231 191/8 ≈ 28 899 | 199 806/7 ≈ 28 544 | −1,2 % |
| BUILDER `medido:agent-result` (ORCHESTRATOR) | 61 231 | 63 572 | +3,8 % |
| REVIEWER `medido:agent-result` (ORCHESTRATOR) | 41 898 | 43 454 | +3,7 % |
| Brecha BUILDER `token-consumer`/`agent-result` | 13,49x | 584 229/63 572 ≈ 9,19x | brecha se estrecha |
| Brecha REVIEWER `token-consumer`/`agent-result` | 5,52x | 199 916/43 454 ≈ 4,60x | brecha se estrecha |

### Spikes / hallazgos detectados

| Hallazgo | Valor / origen | Causa probable |
| --- | --- | --- |
| H1 (positivo, refuerzo de `OPT-006`/`OPT-007`) | Tokens medidos totales bajan ciclo 1→2 en ambos roles (−29,3 % BUILDER, −13,6 % REVIEWER) pese a que el paso 02 tiene más criterios de aceptación nuevos (7 AC) que el paso 01 medido en aislamiento comparable, y BUILDER siguió sin `sed -i` (`usage/BUILDER.md`: "Sin `sed -i`") | Tercera confirmación consecutiva (tras TECHDEBT-001 ciclo 2 y TECHDEBT-003 ciclos 3-5) de que la práctica informal de referenciar `STATUS.md` en vez de reabrir salidas completas (`OPT-006`/`OPT-007`) se sostiene también en este plan, ahora con dos ciclos propios comparables (mismo plan, mismo patrón de un paso por ciclo) |
| H2 (refuerzo de `OPT-023`, no nueva) | La brecha `medido:token-consumer` vs. `medido:agent-result` persiste (BUILDER 9,19x, REVIEWER 4,60x) pero se estrecha frente al ciclo 1 (13,49x, 5,52x) | No hay suficiente dato para atribuir el estrechamiento a una causa concreta (¿menor volumen absoluto de caché relativiza el "peso" no capturado por `agent-result`? ¿variación normal entre pasos?); se registra como dato de refuerzo de `OPT-023`, no como propuesta nueva — ver "Brechas de datos" |
| H3 (menor, dato nuevo) | `usage/BUILDER.md` ciclo 2: "Un script de actualización falló (sin Python en PATH) y se rehizo con Edit" — un intento de edición mecánica vía script (previsiblemente Python) falló por falta de ese intérprete en el `PATH` de este entorno Windows/Git Bash, obligando a repetir el cambio con la herramienta `Edit` | El entorno de este repositorio (Git Bash sobre Windows, ver notas de entorno) no garantiza Python en el `PATH`; un intento de automatizar una edición mecánica sin comprobar antes la disponibilidad del intérprete cuesta como mínimo una llamada a herramienta fallida más el diagnóstico y la repetición con `Edit` |
| H4 (refuerzo de `OPT-021`, no nueva) | `STATUS.md` de este plan ya alcanza 131 líneas tras solo 2 de los 6 ciclos posibles; el detalle íntegro del paso 01 (ya `APPROVED`, sin correcciones previstas) sigue sin compactarse | Mismo patrón que `OPT-021` (TECHDEBT-003 ciclo 4-5): sin compactación, `STATUS.md` seguirá creciendo linealmente con cada ciclo aprobado, y BUILDER/REVIEWER de ciclos futuros (03-06) leerán cada vez más detalle histórico irrelevante para el paso en curso |

No se repiten como filas nuevas en `MEMORIES.md` los hallazgos que ya son refuerzo de propuestas existentes (H1→`OPT-006`/`OPT-007`; H2→`OPT-023`; H4→`OPT-021`). Solo H3 es un hallazgo nuevo sin entrada previa.

### Propuestas

| ID | Propuesta | Ámbito | Ahorro esperado | Fuente |
| --- | --- | --- | --- | --- |
| P2-1 | Añadir una nota breve en `CLAUDE.md` (sección "Minimal Reading" o una nueva nota de entorno) o en la skill `incremental-implementation`/`source-driven-development` de BUILDER: en este repositorio, el shell disponible es Git Bash sobre Windows y no garantiza Python (ni otros intérpretes) en el `PATH`; antes de generar un script de automatización para una edición mecánica (renombrados, inserciones repetidas, etc.), comprobar la disponibilidad del intérprete (p. ej. `command -v python`) o usar directamente la herramienta `Edit`/`sed` de Git Bash, evitando un intento fallido y su repetición | `CLAUDE.md` (nota de entorno) o skill de BUILDER (`incremental-implementation` / `source-driven-development`) | Evita al menos 1 llamada a herramienta fallida + el diagnóstico y la repetición por ciclo en el que se intente este patrón; magnitud exacta no cuantificable con los datos de este ciclo (un solo incidente, sin desglose por llamada en `usage-slice`) | `usage/BUILDER.md` ciclo 2 ("Un script de actualización falló (sin Python en PATH) y se rehizo con Edit") |

### Brechas de datos

- No es posible aislar, dentro de los tokens medidos de BUILDER/REVIEWER de este ciclo, qué fracción corresponde al intento fallido de script (H3) frente al resto del paso 02; `usage-slice` no desglosa por comando/turno.
- H2 (estrechamiento de la brecha `token-consumer`/`agent-result` de 13,49x/5,52x a 9,19x/4,60x) no tiene causa confirmada; se necesitaría un tercer ciclo con volumen de caché similar al ciclo 1 para saber si el estrechamiento depende del volumen absoluto de caché o es ruido entre pasos.
- Sin una tercera medición de `usage-slice` en este plan con alcance más comparable al paso 01 (mutaciones de prueba adicionales), no se puede separar limpiamente el efecto de "menos alcance" del efecto de la práctica `OPT-006`/`OPT-007` en la caída de caché/evento (H1).

### Estado de aprobación

P2-1: **Aprobada**. Operador, instrucción verbatim "aprueba P2-1 y luego ejecuta el ciclo 3", 2026-09-28. Registrada como `OPT-024` en `.github/agent-harness/MEMORIES.md` (`Estado: pendiente`, `Efecto observado: sin datos`); la aplicación a `CLAUDE.md` o a la skill de BUILDER queda pendiente de que el operador la lleve a cabo (OPTIMIZER no edita `CLAUDE.md` ni skills).

## Ciclo 3 - Paso 03 (Migración a MySqlConnector + cancelación en curso)

### Uso medido (`usage-slice`)

Ventanas literales de `STATUS.md` (`Inicio`/`Fin` del ciclo 3):

```
node .github/agent-harness/tools/usage-slice.mjs --file "%APPDATA%/Code/User/globalStorage/joynulabedinshokal.token-eater-dashboard/events-*.jsonl" --repo gaffer-api --window BUILDER=2026-09-28T15:46:28Z/2026-09-28T15:52:56Z --window REVIEWER=2026-09-28T15:54:28Z/2026-09-28T15:58:13Z
```

| Ventana | Eventos | Entrada nueva | Caché lectura | Caché escritura | Salida | Coste USD | Origen |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| BUILDER | 22 | 44 | 1 001 565 | 48 483 | 4 670 | 2,76 | medido:token-consumer |
| REVIEWER | 12 | 24 | 418 794 | 33 976 | 277 | 1,29 | medido:token-consumer |

Totales: BUILDER 1 054 762 tokens; REVIEWER 453 071 tokens. Caché (lectura+escritura) sigue siendo casi todo el tráfico: BUILDER 99,55 % (1 050 048/1 054 762), REVIEWER 99,93 % (452 770/453 071); entrada fresca casi nula (44 y 24 tokens).

Totales pasados por el ORCHESTRATOR para este ciclo (`medido:agent-result`, cota inferior por `OPT-023`): BUILDER 71 949 tokens / 39 `tool_uses` / 472 267 ms; REVIEWER 55 025 tokens / 18 `tool_uses` / 256 987 ms.

### Comparación con los ciclos 1 y 2

| Métrica | Ciclo 1 | Ciclo 2 | Ciclo 3 | Variación C2→C3 |
| --- | ---: | ---: | ---: | --- |
| BUILDER total (`medido:token-consumer`) | 826 130 | 584 229 | 1 054 762 | +80,6 % |
| REVIEWER total (`medido:token-consumer`) | 231 340 | 199 916 | 453 071 | +126,6 % |
| BUILDER caché/evento | ≈41 090 | ≈36 303 | ≈47 729 | +31,5 % |
| REVIEWER caché/evento | ≈28 899 | ≈28 544 | ≈37 731 | +32,2 % |
| BUILDER `medido:agent-result` | 61 231 | 63 572 | 71 949 | +13,2 % |
| REVIEWER `medido:agent-result` | 41 898 | 43 454 | 55 025 | +26,6 % |
| Brecha BUILDER `token-consumer`/`agent-result` | 13,49x | 9,19x | 14,66x | se ensancha de nuevo, máximo del plan |
| Brecha REVIEWER `token-consumer`/`agent-result` | 5,52x | 4,60x | 8,23x | se ensancha de nuevo, máximo del plan |
| `STATUS.md` (líneas, fin de ciclo) | n/d (no leído por BUILDER en ciclo 1) | 131 (H4, ciclo 2) | 171 (este fichero, actual) | +30,5 % |

### Hallazgos

| ID | Valor / origen | Causa probable |
| --- | --- | --- |
| H1 (spike, revierte la tendencia de H1/ciclo 2) | El total medido sube en ambos roles frente al ciclo 2 (BUILDER +80,6 %, REVIEWER +126,6 %) y es el máximo de los tres ciclos del plan, pese a que el paso 03 tiene un número de AC similar al paso 02 (7 vs. 7). El caché/evento también sube (BUILDER +31,5 %, REVIEWER +32,2 %), revirtiendo la caída observada en el ciclo 2 (refuerzo positivo de `OPT-006`/`OPT-007`) | La subida de caché/evento coincide en magnitud (+30-32 %) con el crecimiento de `STATUS.md` en el mismo intervalo (131→171 líneas, +30,5 %), fichero que BUILDER y REVIEWER leen al iniciar el ciclo (`usage/BUILDER.md`/`usage/REVIEWER.md` ciclo 3: `STATUS.md` entre los ficheros leídos, igual que en el ciclo 2 pero no en el ciclo 1, cuando el fichero aún era pequeño). Es el tercer plan (tras TECHDEBT-002/TECHDEBT-003) en el que el crecimiento sin compactar de `STATUS.md` correlaciona con el coste por evento; refuerza `OPT-021` (pendiente, sin aplicar) con una magnitud casi idéntica a la del crecimiento del propio fichero |
| H2 (refuerzo de `OPT-023`, no nueva) | La brecha `medido:token-consumer`/`medido:agent-result` se ensancha de nuevo tras el estrechamiento del ciclo 2: BUILDER 9,19x→14,66x, REVIEWER 4,60x→8,23x — ambas cifras son el máximo del plan hasta ahora, superando incluso al ciclo 1 | Confirma que el estrechamiento del ciclo 2 (H2, ciclo 2) no era una tendencia sostenida sino variación entre pasos; no hay causa confirmada del propio ensanchamiento más allá de que a más caché total, mayor la fracción no capturada por `agent-result` (coherente con la naturaleza de `agent-result` como cota inferior, `OPT-023`) |
| H3 (dato nuevo, menor) | `usage/BUILDER.md` ciclo 3: "sonda en scratchpad (`dotnet run`, la 1.ª ejecución volcó una traza: filtrar con grep desde el inicio)" — la primera ejecución de la sonda de la AC3.6 (consola `net10.0` fuera del repo) volcó una traza sin filtrar antes de que BUILDER empezara a filtrar con `grep` | Mismo patrón de fondo que `OPT-006` (salidas de comando sin resumir que se retransmiten como caché) pero en un contexto distinto: sondas ad-hoc de `dotnet run`/`dotnet fsi` (ya usadas en `OPT-012`/`OPT-019` para caracterizar el driver), no builds/tests del propio repo. Ninguna entrada de `MEMORIES.md` cubre explícitamente el filtrado de sondas ad-hoc desde la primera ejecución |
| H4 (observación, no cuantificable) | REVIEWER repitió los 2 tests nuevos 7 veces y reprodujo build+tests en una copia `git archive HEAD` dos veces, más `dotnet list package --include-transitive`; BUILDER repitió el filtro 3 veces (AC3.2/AC3.3, ambos dependientes del reloj, Q6 = a)) | Verificación de repetibilidad proporcional al riesgo real (tests basados en temporizador de 100 ms / límite de 2 s, `REVIEW.md` R3-2); no hay evidencia de que sea un patrón evitable — se registra como posible contribuyente al total medido, no como hallazgo accionable |

No se repiten como propuesta nueva H1 (→ refuerzo de `OPT-021`), H2 (→ refuerzo de `OPT-023`) ni H4 (sin propuesta clara). Solo H3 da lugar a una propuesta nueva.

### Propuestas

| ID | Propuesta | Ámbito | Ahorro esperado | Fuente |
| --- | --- | --- | --- | --- |
| P3-1 | Extender el principio de `OPT-006` (filtrar salidas de comando grandes en vez de retransmitirlas íntegras) a las sondas ad-hoc de `dotnet run`/`dotnet fsi` en scratchpad (el mismo patrón de sonda ya usado en `OPT-012`/`OPT-019`): instruir a BUILDER para redirigir/filtrar la salida de una sonda temporal desde su primera ejecución (p. ej. `2>&1 \| tail -n 40` o un `grep` del resultado/excepción esperada), en vez de ejecutarla sin filtro y filtrar solo a partir de la segunda ejecución | Skill `debugging-and-error-recovery` o `source-driven-development` de BUILDER (`.claude/agents/builder.md`), o sección "Minimal Reading" de `CLAUDE.md` | Evita al menos una traza completa sin filtrar retransmitida como caché en los turnos posteriores de la sesión de BUILDER; magnitud exacta no aislable (`usage-slice` no desglosa por comando, un solo incidente este ciclo) | `usage/BUILDER.md` ciclo 3 ("sonda en scratchpad (dotnet run, la 1.ª ejecución volcó una traza: filtrar con grep desde el inicio)") |

### Brechas de datos

- No es posible aislar, dentro de los 1 054 762 tokens de BUILDER, qué fracción corresponde específicamente a la traza sin filtrar de la sonda (H3/P3-1) frente al resto del paso 03; `usage-slice` no desglosa por comando/turno.
- La correlación entre el crecimiento de `STATUS.md` (+30,5 %) y el crecimiento de caché/evento (+31,5 % BUILDER, +32,2 % REVIEWER) es sugerente pero no se puede confirmar como causal con los datos disponibles: haría falta un ciclo en el que `STATUS.md` se compacte (aplicación de `OPT-021`) manteniendo un alcance de paso comparable, para aislar el efecto del fichero del efecto de la complejidad propia del paso (migración de driver + sonda externa, más elaborada que el paso 02).
- El ensanchamiento de la brecha `token-consumer`/`agent-result` a un máximo del plan (14,66x BUILDER, 8,23x REVIEWER) no tiene causa aislada más allá de la hipótesis de que escala con el volumen total de caché; se necesitaría un cuarto ciclo con volumen de caché similar al ciclo 2 (el más bajo del plan) para comprobar si la brecha vuelve a estrecharse o si el ensanchamiento es monótono.

### Estado de aprobación

Pendiente de checkpoint humano (este es el resultado del ciclo 3, aún no presentado al operador en este análisis).
