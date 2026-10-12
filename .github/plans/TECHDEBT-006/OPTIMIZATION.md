# OPTIMIZATION - TECHDEBT-006

## Resumen de ciclos

El detalle completo se conserva solo para los 2 ciclos más recientes (OPT-013); el resto está en el historial de Git.

| Ciclo | Paso | Veredicto | ID(s) de propuesta | Ahorro esperado | Estado de aprobación |
| --- | --- | --- | --- | --- | --- |
| 1 | 01 Limpieza, operación, CI, observabilidad, `null` en `stands` (10 puntos, antes 01+02) | APPROVED | P1-1, P1-2 (provisionales) | P1-1: 1 llamada colgada por incidente; P1-2: ~9 k tokens de contexto por llamada de BUILDER (estimación) | Aprobadas y aplicadas 2026-10-12: P1-1 = OPT-045, P1-2 = OPT-046 |
| 2 | 02 Forma RFC 7807 del error de validación | APPROVED | P2-1, P2-2 (provisionales) | P2-1: ~2-3 llamadas y ~5 k tokens por paso; P2-2: ~1-2 llamadas por ciclo (estimaciones) | Aprobadas 2026-10-12: P2-1 = OPT-047, P2-2 = OPT-048 (pendiente, sin aplicación permanente) |
| 3 | 03 `.editorconfig` de C#, `dotnet format` completo y verificación en CI | APPROVED | P3-1, P3-2 (provisionales) | P3-1: ventanas de datos fiables (sin ahorro de tokens); P3-2: ~2-3 llamadas y ~15 k tokens por verificación de formato masivo (estimación) | Pendiente de decisión del operador |

## Ciclo 2 - Paso 02 (BUILDER APPROVED por REVIEWER)

### Datos

`usage-slice` no se ejecuta: los ciclos 0 y 1 de este plan y el anterior ya devolvieron "sin datos" (OPT-031). Se citan `agent-result` (cota inferior, OPT-023) y `usage/*.md` (`estimado`). Ventanas de `STATUS.md`: BUILDER 12:52:12Z-13:01:32Z, REVIEWER 13:03:04Z-13:07:43Z. Sin `token-consumer` no se separa entrada nueva de caché.

| Rol | Tokens (`medido:agent-result`) | Llamadas | Duración | Tokens/llamada | Autoinforme (`estimado`) | Desvío |
| --- | --- | --- | --- | --- | --- | --- |
| BUILDER | 144 036 | 64 | 643 s | ~2 251 | ~130 000 (110 k + 20 k), ~45 llamadas | tokens -10 %; llamadas subestimadas en 19 (-30 %) |
| REVIEWER | 67 496 | 27 | 302 s | ~2 500 | ~97 000 (85 k + 12 k), ~24 llamadas | tokens +44 % sobre el total real; llamadas -3 |

| Ciclo | BUILDER tokens / llamadas / por llamada | REVIEWER tokens / llamadas / por llamada |
| --- | --- | --- |
| TD-006 c1 (10 puntos) | 177 602 / 102 / ~1 741 | 70 291 / 22 / ~3 195 |
| TD-006 c2 (paso 02, 9 tests nuevos, 197 rojos) | 144 036 / 64 / ~2 251 | 67 496 / 27 / ~2 500 |

### Hallazgos

| ID | Hallazgo | Evidencia | Causa probable |
| --- | --- | --- | --- |
| S1 | BUILDER: -19 % tokens y -37 % llamadas frente a c1 (144 036 / 64 frente a 177 602 / 102). Los tokens/llamada suben (+29 %): menos llamadas, cada una con contexto base similar. | Tabla anterior | Alcance distinto (un solo cambio transversal de forma de error frente a 10 puntos), así que no se atribuye todo a OPT-045/046. |
| S2 | REVIEWER: 27 llamadas (+5 frente a c1) con tokens totales casi iguales (-4 %); tokens/llamada -22 %. Sube el número de llamadas, no el contexto. | `usage/REVIEWER.md`; `REVIEW.md` L79-82 | Sonda de producción HEAD frente a árbol de trabajo (ver S3) más un heredoc fallido. |
| S3 | Coste de la sonda de REVIEWER: fichero temporal `ReviewerProbe.cs` solo en el worktree, `StartupTestServer` con BD real y con BD inalcanzable, 8 peticiones, ejecutada dos veces (diff y `HEAD`). `usage/REVIEWER.md` la cuenta como 2 de las 3 salidas grandes con 8 cuerpos JSON. | `REVIEW.md` L80; `usage/REVIEWER.md` ciclo 2 | Estimación sin medir: ~5-7 llamadas (crear sonda, 2 ejecuciones, lectura de cuerpos, limpieza) y ~10-15 k tokens de entrada que se retransmiten como caché; es la causa más probable de las +5 llamadas. El valor de la sonda fue real: confirmó "valores idénticos salvo la forma", base de AC2.5. Lo evitable es la mitad "antes" (HEAD), que BUILDER ya podía haber dejado registrada al caracterizar el rojo. |
| S4 | Heredoc de `REVIEW.md` falló por comillas (`bash: unexpected EOF`) y se rehízo con `Edit`. | `usage/REVIEWER.md` ciclo 2 | Texto con comillas dentro de heredoc; 1-2 llamadas perdidas y un bloque grande retransmitido dos veces. |
| S5 | Un `sed` por número de línea dañó un bloque en BUILDER (`{` sustituido), corregido con `Edit`. | `usage/BUILDER.md` ciclo 2 | Edición mecánica por línea sin anclaje estable; OPT-018 ya cubre el caso (`Edit` con ancla). ~2 llamadas, sin propuesta nueva. |

### Efecto de reglas ya aplicadas

| ID | Evidencia en este ciclo | Conclusión |
| --- | --- | --- |
| OPT-045 | Cero comandos colgados ni variables vacías en `usage/BUILDER.md` ciclo 2 (c1: 1 comando matado + `.tmp` borrado). Un solo ciclo, sin incidente. | Sin regresión; ausencia de incidente, no prueba de eficacia. Efecto en tokens: sin datos |
| OPT-046 | BUILDER leyó `02-*` entero, `00` por `grep`/`sed -n 36,48p` y `STATUS.md` por rangos; sin lecturas de 37 KB (c1: 1 salida de 37 KB). Llamadas 102 -> 64 y tokens 177 602 -> 144 036 (alcance distinto). | Cumplida. El ahorro de ~9 k tokens por llamada de c1 era una estimación; no aislable del cambio de alcance. Indicio favorable, sin datos de caché |
| OPT-043 | Sin volcados de `MEMORIES.md` en las salidas grandes de BUILDER ni REVIEWER | Sin regresión; sin datos de tokens |
| OPT-033 | Worktree en `HEAD`, diff de tests, 197 × Con error reproducido por REVIEWER, `worktree remove --force` | Seguida; mismo recuento que BUILDER |
| OPT-038 | Rojo `n × estado` por clase (187 + 5 + 4 + 1) reproducido por REVIEWER | Eficaz |

### Propuestas (aprobadas por el operador: "Aprueba P2-1 Y 2 y luego con el ciclo 3", 2026-10-12; P2-1 = OPT-047, P2-2 = OPT-048, pendientes)

| ID | Propuesta | Dónde | Ahorro esperado | Origen |
| --- | --- | --- | --- | --- |
| P2-1 | En pasos que cambian el cuerpo servido por producción, el AC pide a BUILDER registrar en `STATUS.md` el "antes" (valores por caso, tabla compacta de los N casos) durante el rojo. REVIEWER ejecuta solo la sonda del "después" y compara con esa tabla; la ejecución en `HEAD` solo se repite si la tabla no cubre un caso. | Plan (PLANNER lo incorpora a pasos de este tipo) y nota en `.github/doc/work-guidelines.md` ("Reproducing red during review") | ~2-3 llamadas y ~5 k tokens de entrada retransmitida por paso (estimación; el coste de la sonda no está medido) | S3 |
| P2-2 | REVIEWER escribe `REVIEW.md` con `Edit`/`Write`, nunca con heredoc cuando el texto lleva comillas o backticks. | `.claude/agents/reviewer.md` | ~1-2 llamadas por ciclo (estimación) | S4 |

Observación sin propuesta: S5 está cubierto por OPT-018.

### Compactación

`MEMORIES.md`: OPT-045 y OPT-046 siguen `aplicada` con efecto `sin datos` no declarado final (OPT-044), así que no se archivan. Se actualiza `Efecto observado`. Las filas `sin datos` de OPT-027, 029-031 siguen pendientes de decisión del operador.

## Ciclo 3 - Paso 03 (BUILDER APPROVED por REVIEWER)

### Datos

`usage-slice` no se ejecuta: ya van más de 2 ciclos seguidos sin datos (OPT-031). Se citan `agent-result` (cota inferior, OPT-023) y `usage/*.md` (`estimado`). Sin `token-consumer` no se separa entrada nueva de caché. Ventanas de `STATUS.md`: BUILDER 13:23:02Z-13:32:29Z; REVIEWER 13:33:04Z-16:36:55Z (ver S3).

| Rol | Tokens (`medido:agent-result`) | Llamadas | Duración | Tokens/llamada | Autoinforme (`estimado`) | Desvío |
| --- | --- | --- | --- | --- | --- | --- |
| BUILDER | 97 887 | 56 | 588 s | ~1 748 | ~108 000 (90 k + 18 k), ~40 llamadas | tokens +10 %; llamadas subestimadas en 16 (-29 %) |
| REVIEWER (1.ª ejecución, cortada por 429) | 91 035 (cota inferior) | 43 | 474 s | ~2 117 | ~124 000 (110 k + 14 k), ~32 llamadas | tokens +36 % sobre la 1.ª ejecución; llamadas -11. Reanudación sin totales (n/d) |

| Ciclo | BUILDER tokens / llamadas / por llamada | REVIEWER tokens / llamadas / por llamada |
| --- | --- | --- |
| TD-006 c1 (10 puntos) | 177 602 / 102 / ~1 741 | 70 291 / 22 / ~3 195 |
| TD-006 c2 (RFC 7807) | 144 036 / 64 / ~2 251 | 67 496 / 27 / ~2 500 |
| TD-006 c3 (formato masivo, 107 `.cs`) | 97 887 / 56 / ~1 748 | >= 91 035 / >= 43 / ~2 117 |

### Hallazgos

| ID | Hallazgo | Evidencia | Causa probable |
| --- | --- | --- | --- |
| S1 | BUILDER: ciclo más barato del plan (-32 % tokens y -13 % llamadas frente a c2; -45 % frente a c1) pese a tocar 107 ficheros `.cs` (+2125 / -3357). | Tabla anterior; `STATUS.md` filas ciclo 3 | El formato lo hace `dotnet format`, no BUILDER: salidas redirigidas a fichero y resumidas con un solo `grep -oE ... \| sort \| uniq -c` (0 comandos con salida grande), y el renombrado de 64 pares con `sed` guiado por una tabla de conflictos generada por bucle en vez de leer 31 ficheros (OPT-039). |
| S2 | REVIEWER: 43+ llamadas, la cifra más alta del plan (c1 22, c2 27), con 91 k tokens solo en la 1.ª ejecución. | Tabla anterior; `usage/REVIEWER.md` ciclo 3 | Coste de reproducibilidad (OPT-033) para un diff que no se puede leer: worktree en `HEAD` + `.editorconfig` + aplicar ×2 + verify + comparación fichero a fichero (65 idénticos, 42 solo renombrados). Dos salidas evitables: un `git diff --no-index` contaminado por `obj/` (~117 KB a fichero) y un bucle `awk` con escapes rotos (~15 KB de errores). Estimación sin medir: ~4-6 llamadas y ~20 k tokens de entrada retransmitida (~10-12 % del ciclo). |
| S3 | Corte por la API: `Fin` de REVIEWER = 16:36:55Z, 3 h 3 min 51 s después del `Inicio`; `REVIEW.md` estaba escrito a las 13:40Z (~7 min de trabajo real, coherente con los 474 s del `agent-result`). | `STATUS.md` fila de tiempos ciclo 3 | El 429 detuvo la ejecución tras el trabajo útil; la reanudación solo cerró `STATUS.md`/`usage`. Impacto en tokens: la reanudación recarga el contexto del subagente (coste real no medido, n/d), así que el total de REVIEWER es cota inferior. Impacto en datos: la ventana de 3 h contamina `usage-slice` y la duración (ver P3-1). Sin impacto en calidad: el veredicto es anterior al corte. |
| S4 | Autoinforme: BUILDER sobreestima tokens (+10 %) y subestima llamadas (-29 %, igual que c1 y c2); REVIEWER sobreestima tokens. | `usage/*.md` frente a `agent-result` | Sigue siendo `estimado`; el dato válido es `agent-result` (sin propuesta, como en c1 S5). |

### Efecto de reglas ya aplicadas (actualizado en `MEMORIES.md`)

| ID | Evidencia en este ciclo | Conclusión |
| --- | --- | --- |
| OPT-045 | Cero comandos colgados en BUILDER c2 y c3 (c1: 1) | Sin regresión en 2 ciclos; ausencia de incidente, no prueba de eficacia |
| OPT-046 | `03-*` entero, `00` solo por `grep Q19`, `STATUS.md` por rangos (el `cat` conjunto de 30 KB se persistió pero no se leyó). Tokens 177 602 -> 144 036 -> 97 887; llamadas 102 -> 64 -> 56 | Cumplida en c2 y c3; tendencia favorable, no aislable del alcance. Matiz: el `cat` conjunto aún se ejecuta, aunque no se retransmite |
| OPT-048 | REVIEWER escribió `REVIEW.md` sin fallo de heredoc; el fallo de shell del ciclo fue un `awk` roto (otro caso) | Sin incidente de heredoc en c3; sin datos de tokens |
| OPT-047 | No aplicable: el paso 03 no cambia el cuerpo servido por producción | Sin evidencia; sigue `pendiente` |
| OPT-033 | Worktree en `HEAD` con `.editorconfig`, aplicar ×2, verify y comparación; worktree eliminado | Seguida; mayor coste de REVIEWER del plan (S2) pero única forma de validar un diff de formato sin leerlo |
| OPT-038 / OPT-041 / OPT-043 | Rojo por tipo de diagnóstico reproducido; resumen único; sin volcados de `MEMORIES.md` | Eficaces |

### Visión de cierre del plan (ciclos 1 a 3)

| Ciclo | BUILDER tokens | REVIEWER tokens | Total (cota inferior) | Alcance |
| --- | --- | --- | --- | --- |
| 1 | 177 602 | 70 291 | 247 893 | 10 puntos |
| 2 | 144 036 | 67 496 | 211 532 | Contrato RFC 7807 |
| 3 | 97 887 | >= 91 035 | >= 188 922 | Formato masivo |
| Total | 419 525 | >= 228 822 | >= 648 347 | 3 ciclos de 6 posibles |

- Tendencia: BUILDER baja ciclo a ciclo (-45 % de c1 a c3), coherente con OPT-045/046 y con herramientas que sustituyen lectura (`dotnet format`, `sed` por tabla); REVIEWER sube en c3 por la verificación reproducible.
- Fusionar 01+02 originales (Q8) y cerrar en 3 de 6 ciclos es el patrón más barato; ahorro frente a ciclos separados del orden de 100-160 k tokens por ciclo evitado (estimación, no medida).
- Sin `token-consumer` en ningún ciclo: el plan entero queda sin separación entrada/caché; es el mayor hueco de datos.
- Pendiente del operador: aplicación permanente de OPT-047/048 en `.claude/agents/*.md`; declarar no medibles OPT-027/029-031 para poder archivarlas (OPT-044).

### Propuestas (provisionales, pendientes de aprobación del operador)

| ID | Propuesta | Dónde | Ahorro esperado | Origen |
| --- | --- | --- | --- | --- |
| P3-1 | Si un subagente se corta por un límite de la API o se reanuda, la fila `Fin` de `STATUS.md` registra la hora del último trabajo útil (p. ej. escritura de `REVIEW.md`), y la reanudación se anota en una fila aparte con `Inicio`/`Fin` propios. | ORCHESTRATOR (`/harness-run`) y `.claude/agents/reviewer.md` / `builder.md` | Ventanas fiables para `usage-slice` y duraciones comparables (no ahorra tokens; evita datos engañosos) | S3 |
| P3-2 | En la verificación reproducible de un diff de formato masivo, comparar solo ficheros rastreados (`git -C <worktree> status --porcelain` y `cmp` por fichero, o `git diff --no-index` acotado a `*.cs` y fuera de `bin/`/`obj/`), con el script definido una vez y probado en 2 ficheros antes de lanzarlo al conjunto. | `.claude/agents/reviewer.md` (nota bajo OPT-033) | ~2-3 llamadas y ~15 k tokens evitados por verificación (117 KB + 15 KB de salidas inútiles; estimación) | S2 |

### Compactación

`MEMORIES.md`: OPT-045/046 siguen `aplicada` con efecto no declarado final (OPT-044), así que no se archivan; se actualiza `Efecto observado`. OPT-047/048 siguen `pendiente`. P3-1/P3-2 no se registran hasta aprobación.
