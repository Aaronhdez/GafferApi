# OPTIMIZATION - TECHDEBT-006

## Resumen de ciclos

El detalle completo se conserva solo para los 2 ciclos más recientes (OPT-013); el resto está en el historial de Git.

| Ciclo | Paso | Veredicto | ID(s) de propuesta | Ahorro esperado | Estado de aprobación |
| --- | --- | --- | --- | --- | --- |
| 1 | 01 Limpieza, operación, CI, observabilidad, `null` en `stands` (10 puntos, antes 01+02) | APPROVED | P1-1, P1-2 (provisionales) | P1-1: 1 llamada colgada por incidente; P1-2: ~9 k tokens de contexto por llamada de BUILDER (estimación) | Pendiente de decisión del operador |

## Ciclo 1 - Paso 01 (BUILDER APPROVED por REVIEWER)

### Datos

`usage-slice` con `--repo gaffer-api` y las ventanas de `STATUS.md` (BUILDER 12:27:34Z-12:43:14Z, REVIEWER 12:43:43Z-12:47:05Z): "sin datos" en `claude-code` y `copilot`. Es el tercer ciclo seguido sin datos (OPT-031 recomendaba omitir la escalera; se ejecutó una sola vez por la petición explícita). No se reintenta. Brecha: sin `token-consumer` no se separa entrada nueva de caché; las cifras de `agent-result` son cota inferior (OPT-023).

| Rol | Tokens (`medido:agent-result`) | Llamadas | Duración | Tokens/llamada | Autoinforme (`usage/*.md`, `estimado`) | Desvío |
| --- | --- | --- | --- | --- | --- | --- |
| BUILDER | 177 602 | 102 | 958 s | ~1 741 | ~175 000 (150 k + 25 k), ~70 llamadas | tokens -1 %; llamadas subestimadas en 32 (-31 %) |
| REVIEWER | 70 291 | 22 | 219 s | ~3 195 | ~79 000 (70 k + 9 k), ~20 llamadas | tokens +12 % sobre el total real; llamadas -2 |

Comparativa con TECHDEBT-005 (`medido:agent-result`, mismo origen):

| Ciclo | BUILDER tokens / llamadas / por llamada | REVIEWER tokens / llamadas / por llamada |
| --- | --- | --- |
| TD-005 c3 (03a, 161 tests) | 177 920 / 86 / ~2 069 | 70 446 / 29 / ~2 429 |
| TD-005 c4 (03b, 63 tests) | 173 693 / 87 / ~1 997 | 111 762 / 32 / ~3 493 |
| TD-005 c5 (paso menor) | 97 593 / 37 / ~2 638 | 64 496 / 23 / ~2 804 |
| TD-006 c1 (10 puntos) | 177 602 / 102 / ~1 741 | 70 291 / 22 / ~3 195 |

### Hallazgos

| ID | Hallazgo | Evidencia | Causa probable |
| --- | --- | --- | --- |
| S1 | Agrupar pasos no encareció el ciclo en tokens: 177 602 frente a 173 693-177 920 de un paso grande, con 10 puntos (AC1.1-AC1.14) en lugar de uno. Sube el máximo de llamadas (102 frente a 87) pero baja el coste por llamada (~1 741 frente a ~1 997-2 069). | Tabla anterior; `STATUS.md` ciclo 1 | El contexto base (plan, TestFixture, convenciones) se paga una vez en vez de dos o tres. Las 15 llamadas extra son trabajo de contenido (6 rojos distintos, 2 ficheros de producto más, volcado swagger). Contrafactual (estimación, no medida): un ciclo adicional cuesta al menos ~160 k tokens (TD-005 c5: 97,6 k + 64,5 k), así que fundir 01+02 ahorra del orden de 100-160 k tokens frente a dos ciclos. |
| S2 | REVIEWER: 22 llamadas, 3 195 tokens/llamada, 70 291 total. | `usage/REVIEWER.md`; `REVIEW.md` L10 | Menos llamadas que en c4 (32) y total igual al de c3 pese a 10 puntos. El tokens/llamada es +14 % sobre c5 (2 804) porque abrió el diff de producto (~300 líneas) y 4 diffs de tests completos, además del worktree de OPT-033. OPT-040 cumplido a medias (`diff --stat` y grep NUnit, pero diffs de test completos). |
| S3 | Errata de BUILDER: `cat > $S.tmp` con `$S` vacío dejó un comando colgado (esperando stdin), hubo que matarlo y borrar el `.tmp`. | `usage/BUILDER.md` ciclo 1 | Variable de scratchpad no definida en un shell sin estado (cada llamada de Bash reinicia el entorno). Coste medible solo parcialmente: 1 llamada colgada más la recuperación (~2-3 llamadas, estimación) y parte de los +216 s de duración frente a TD-005 c4 (958 s frente a 742 s); el resto se explica por las 15 llamadas extra. |
| S4 | Lectura única de 37 KB (`00-overview` + `01-*` + `STATUS.md`) en una salida persistida, leída entera. | `usage/BUILDER.md` ciclo 1, "Comandos con salida grande" | Plan de 10 puntos con cuestionario `00` extenso; BUILDER lee todo en vez de los AC. Con caché, ese bloque (~9 k tokens, estimación) se retransmite en cada una de las ~100 llamadas posteriores. Es el mayor candidato a reducción de caché en este ciclo, pero no verificable sin `token-consumer`. |
| S5 | Autoinforme de BUILDER subestima llamadas en un 31 % (70 frente a 102). | `usage/BUILDER.md` frente a `agent-result` | El autoinforme es `estimado`; los subagentes no ven su uso. Ya se sabía (TD-005 c4: -29). No se propone cambio: el dato válido es siempre `agent-result`. |

### Efecto de reglas ya aplicadas (actualizado en `MEMORIES.md`)

| ID | Evidencia en este ciclo | Conclusión |
| --- | --- | --- |
| OPT-033 | `REVIEW.md` L21-23: worktree en `HEAD`, aplicación del diff de tests, build y filtro, `git worktree remove --force`; rojo 3 × CS1729, 3 × CS1503, 1 × CS0266 y 8 × Con error | Seguida sin improvisar; efecto en tokens no aislable |
| OPT-038 | Rojo por clase `n × estado` en `STATUS.md`; REVIEWER reprodujo los mismos recuentos | Eficaz |
| OPT-039 | Grep de usos de 4 tipos antes de cambiar firmas; primer verde 240 × Correctas, suite 688/688 | Eficaz (primera vez demostrable) |
| OPT-040 | 22 llamadas, ~3 195 por llamada (c4: 3 493; c5: 2 804) | Cumplimiento parcial; aún mejor que sin la regla |
| OPT-041 | `red.sh` definido una vez y reutilizado | Eficaz; en BUILDER la mejora se ve en tokens/llamada (-13 % frente a c4) no en el número de llamadas, que depende del alcance |
| OPT-043 | Sin volcados de `MEMORIES.md` en las salidas grandes de BUILDER | Sin regresión; sin datos de tokens |
| OPT-042 | El filtro se usó con éxito en los builds (`0 Advertencia(s), 0 Errores`) | Sin fallo observado; efecto sin datos |

### Propuestas (provisionales, no registradas; requieren aprobación)

| ID | Propuesta | Dónde | Ahorro esperado | Origen |
| --- | --- | --- | --- | --- |
| P1-1 | Al escribir en el scratchpad desde Bash, definir la ruta en la misma llamada y protegerla (`S="<ruta>"; : "${S:?}"` antes de cualquier redirección) o crear el fichero con `Write`; nunca depender de una variable de una llamada anterior (cada llamada reinicia el shell). | `.claude/agents/builder.md` y nota en `CLAUDE.md` "Minimal Reading" (lo aplica el operador) | Evita un comando colgado y su recuperación (~2-3 llamadas por incidente, estimación) | S3 |
| P1-2 | En planes con `00-*.md` extenso, BUILDER no hace `cat` de `00` + `01` + `STATUS.md`; lee `01-*` entero y de `00` solo las decisiones aplicables (`grep -n "^## \|Q[0-9]"` y rangos) y de `STATUS.md` solo el resumen y el último ciclo. | `.claude/agents/builder.md` (lo aplica el operador) | Hasta ~9 k tokens de contexto retransmitido por llamada en ciclos grandes (estimación); ~0,9 M tokens de caché de lectura en un ciclo de 100 llamadas, solo orientativo | S4 |

Observación sin propuesta: agrupar cambios en un ciclo (petición del operador) ha resultado neutro en tokens totales y favorable frente a ciclos separados (S1). Condición a vigilar: el máximo de llamadas por ciclo sube; si un ciclo supera ~120 llamadas conviene partirlo, porque cada llamada adicional re-lee un contexto mayor. Sin datos para fijar el umbral con rigor.

### Compactación

`MEMORIES.md`: no se compacta nada en este ciclo. OPT-027, 029, 030 y 031 siguen con `sin datos` y OPT-044 define "final" solo si se declara no medible; hace falta que el operador lo declare. Pendiente de decisión.
