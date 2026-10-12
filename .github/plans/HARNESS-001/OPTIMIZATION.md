# OPTIMIZATION - HARNESS-001

## Resumen de ciclos anteriores

| Ciclo | Paso | Veredicto | ID(s) de propuesta | Ahorro esperado | Estado de aprobación |
| --- | --- | --- | --- | --- | --- |
| 2 | Verificación agrupada 01-05 (re-verificación + compactación ACH4.4) | APPROVED (con R6 abierto) | P2-1 | Evita re-archivar/restaurar filas por criterio ambiguo (~11 k tokens por compactación repetida); no cuantificado | aprobada y aplicada (OPT-044) |

## Ciclo 1 - Verificación agrupada de los pasos 01-05 (NEEDS_CHANGES)

### Uso medido

| Rol | Tokens | Llamadas | Origen | Autoinforme del subagente (`usage/*.md`) |
| --- | --- | --- | --- | --- |
| BUILDER | 50 325 | 16 | `medido:agent-result` (cota inferior, OPT-023) | ~52 000 (45 000 + 7 000), `estimado`, 14 llamadas |
| REVIEWER | 48 333 | 12 | `medido:agent-result` (cota inferior, OPT-023) | ~41 000 (35 000 + 6 000), `estimado`, 10 llamadas |

`usage-slice`: no ejecutado. Los dos ciclos previos (TECHDEBT-004, ciclos 5 y 6) ya devolvieron "sin datos" (OPT-031); se omite la escalera de reintento. Brecha de datos: no hay medición de `token-consumer`, así que no se puede separar entrada nueva de caché. Según OPT-023, `agent-result` suele excluir la caché y debe leerse como cota inferior.

### Spikes

| Id | Valor y origen | Causa probable |
| --- | --- | --- |
| S1 | BUILDER: 1 comando con salida grande, `grep` de `MEMORIES.md` con filas completas (~8 KB; `usage/BUILDER.md`) | Las filas de `MEMORIES.md` miden 1-3 KB cada una (OPT-006, 013, 014, 021). El patrón se repite en cada verificación de OPT y se relee como caché. La compactación (ACH4.4) reduciría el problema de raíz pero no está aprobada. |
| S2 | REVIEWER ~ BUILDER (48 k vs 50 k) en un ciclo sin código ni build | Doble verificación por `grep` de los mismos 5 pasos; es esperable por diseño (REVIEWER repite la evidencia) y no es anómalo. |
| S3 | Brecha estimado vs medido: ≤ 25 % (BUILDER 52 k vs 50 k; REVIEWER 41 k vs 48 k) | Coincide con OPT-023: `agent-result` y el autoinforme "a ciegas" convergen, ambos probablemente por debajo del consumo real con caché. |

Observación de contenido (no coste): `CLAUDE.md` L46 (OPT-006) filtra con `Warning\(s\)|Error\(s\)`, pero la consola localizada en español imprime "Advertencia(s)" y "Errores" (la propia L45, OPT-005, lo reconoce). El filtro puede devolver vacío y forzar una segunda ejecución de build (coste de un build extra por ocurrencia).

### Propuestas

| ID prov. | Propuesta | Ámbito | Ahorro esperado | Fuente |
| --- | --- | --- | --- | --- |
| P1-1 | Ampliar el filtro de OPT-006 en `CLAUDE.md` L46 a `"Warning\(s\)\|Advertencia\(s\)\|Error\(s\)\|Errores"` para que funcione con consola en español, sin repetir el build. | `CLAUDE.md` ("Minimal Reading") | Evita un build repetido por ocurrencia cuando el filtro devuelve vacío (~1 build + salida). No cuantificado. | `STATUS.md` ciclo 1 (observación tras ACH5.5); `CLAUDE.md` L45-46 |
| P1-2 | Cuando BUILDER/REVIEWER verifiquen una OPT en `MEMORIES.md`, extraer solo la fila por ID y truncada: `grep "^| OPT-0NN" ... \| cut -c1-300`. Alternativa a medio plazo: aprobar la compactación ACH4.4. | `CLAUDE.md` ("Minimal Reading") o `builder.md`/`reviewer.md` | Evita ~8 KB de salida por verificación (~2 000 tokens, retransmitidos como caché en turnos posteriores). | `usage/BUILDER.md` ciclo 1 |

No se repiten propuestas ya en `MEMORIES.md`. ACH4.4 (compactación de `MEMORIES.md`) sigue pendiente de aprobación explícita del operador; no se ha ejecutado.

### Efecto observado de propuestas anteriores

| ID | Observación |
| --- | --- |
| OPT-023 | Confirmada de nuevo: sin `token-consumer`, las cifras de `agent-result` se etiquetan como cota inferior. |
| OPT-031 | Aplicada y utilizada en este ciclo: escalera omitida, ahorro de ~4 llamadas de `usage-slice`. |
| OPT-025 | Verificada como aplicada (ver abajo); efecto sin datos. |

### Registro en `MEMORIES.md` (aprobado por el operador: "haz el 1")

| ID | Comprobación | Resultado |
| --- | --- | --- |
| OPT-025 | `CLAUDE.md` L50 contiene la regla de sondas filtradas (cita OPT-025); `TECHDEBT-004/STATUS.md` L43 la declara aplicada | `pendiente` -> `aplicada` |
| OPT-026 | `.claude/agents/builder.md` L18 incluye la comprobación de colapso (OPT-026/029); `TECHDEBT-004/STATUS.md` L29; `STATUS-archive.md` existe en ese plan | `pendiente` -> `aplicada` |
| OPT-015 | Ya figuraba `aplicada` (no "parcial"); `.gitattributes` con `*.cs text eol=lf`. Sin cambios | sin cambios |
| OPT-028 | Era la fila "aplicada (parcial)": `.gitattributes` tiene `*.json text eol=lf` y `git ls-files --eol Gaffer.Api.Client/OpenApiSpecs.json` da `i/lf w/lf attr/text eol=lf`; no hace falta `git add --renormalize` | `aplicada (parcial)` -> `aplicada` |

Nota: el "parcial" que menciona la petición correspondía a OPT-028, no a OPT-015. No se ha ejecutado la compactación (ACH4.4).

## Ciclo 2 - Re-verificación 01-05 y compactación de `MEMORIES.md` (APPROVED)

### Uso medido

| Rol | Tokens | Llamadas | Tokens/llamada | Origen | Autoinforme (`usage/*.md`) | Ciclo 1 (tokens, tokens/llamada) |
| --- | --- | --- | --- | --- | --- | --- |
| BUILDER | 53 389 | 18 | ~2 966 | `medido:agent-result` (cota inferior, OPT-023) | ~48 000, `estimado`, 14 llamadas | 50 325, ~3 145 |
| REVIEWER | 51 965 | 18 | ~2 887 | `medido:agent-result` (cota inferior, OPT-023) | ~54 000, `estimado`, 14 llamadas | 48 333, ~4 028 |
| Compactación previa de `MEMORIES.md` (fuera de rol) | 44 292 | 4 | ~11 073 | `medido:agent-result` | n/a | n/a |

`usage-slice`: no ejecutado; el ciclo 1 de este plan y los dos ciclos previos de TECHDEBT-004 ya no devolvieron datos (OPT-031). Brecha: sin `token-consumer` no se separa entrada nueva de caché; las cifras son cota inferior. El autoinforme subestima en 10 % a BUILDER (48 k vs 53,4 k) y sobrestima ~4 % a REVIEWER.

### Spikes

| Id | Valor y origen | Causa probable |
| --- | --- | --- |
| S1 | La compactación costó 44 292 tokens en 4 llamadas (~11 k/llamada, 3,7x el coste por llamada de un rol) y es comparable a un ciclo entero de BUILDER | Se leyó y reescribió íntegro `MEMORIES.md` (~50 filas de 1-3 KB) en una sola sesión; es un coste puntual, que no se repetirá si las filas archivadas ya no se releen (OPT-014). |
| S2 | BUILDER +6 % y REVIEWER +7,5 % frente al ciclo 1, con 18 llamadas cada uno (+2/+6) | Alcance mayor: ciclo 2 añade ACH4.4 (verificación literal con `grep -vxFf` y `diff` de IDs). Tokens/llamada de REVIEWER cae 28 % (4 028 -> 2 887): lecturas por rangos funcionan. |
| S3 | BUILDER: 1 reintento por heredoc de Bash con muchas comillas invertidas (`usage/BUILDER.md`) | Escritura de Markdown con backticks vía heredoc; solucionado con `Edit`. Coincide con OPT-018 (usar `Edit` con ancla); sin propuesta nueva. |
| S4 | R6 (REVIEWER): la compactación archivó filas con efecto no definitivo | Ver análisis siguiente. |

### Análisis de R6

Hechos comprobados (`Grep` sobre `MEMORIES-archive.md` y `AGENTS.md`):
- OPT-001 a 004: `aplicada`, `Efecto observado` = `sin datos`. Son filas `legacy:` cuyo efecto nunca se medirá.
- OPT-013, 014, 017, 020, 021: `Estado` = `aplicada` en el resumen y en el archivo. Sus filas archivadas ya terminan en "Aplicada 2026-09-28 en ..." (013/014 en `optimizer.md`; 017/020 en `AGENTS.md` y `harness-run.md`; 021 en `AGENTS.md`, `builder.md`, `reviewer.md`). Verificado en este ciclo: `AGENTS.md` L43 (colapso de `STATUS.md`, OPT-021), L62 (OPT-017, OPT-020), L74-75 (OPT-014). El texto intermedio "sigue sin aplicarse"/"`Estado` se mantiene `pendiente`" es histórico y queda superado por la frase final de cada fila.
- Incoherencia real: filas completas OPT-025 a 031 con `Efecto: sin datos` siguen sin colapsar, mientras OPT-001 a 004 con el mismo valor sí se colapsaron. El criterio "efecto final" de `AGENTS.md` L75 no define si `sin datos` es final.

Registro: no se edita ninguna fila. `Estado` ya es `aplicada` en las cinco, y el archivo debe conservar las filas literalmente (`AGENTS.md` L75); reescribir su texto histórico violaría esa regla. No hay nada que actualizar en modo registro.

### Propuestas

| ID prov. | Propuesta | Ámbito | Ahorro esperado | Fuente |
| --- | --- | --- | --- | --- |
| P2-1 | Resolver R6 aclarando `AGENTS.md` L75: "`Efecto observado` final" = medido, o `sin datos` declarado por el operador como no medible (filas `legacy:` y optimizaciones de proceso sin métrica). Con esa lectura OPT-001 a 004 quedan correctamente archivadas, y OPT-025 a 031 con `sin datos` pasan a ser colapsables en la próxima compactación. Alternativa conservadora si el operador prefiere la regla estricta: restaurar OPT-001 a 004 a la tabla completa (4 filas, ~1,5 KB) y no colapsar ninguna `sin datos`. | `AGENTS.md` L75 (la edita el operador) y, en la alternativa, `MEMORIES.md`/`MEMORIES-archive.md` (OPTIMIZER, con aprobación) | Evita una segunda compactación correctiva (~44 k tokens medidos) y reduce `MEMORIES.md` en ~7 filas completas (~10 KB) para cada lectura íntegra de OPTIMIZER | R6 en `REVIEW.md`; `AGENTS.md` L75; S1 |

### Estado de propuestas del ciclo 1

| ID prov. | Estado |
| --- | --- |
| P1-1 (filtro de OPT-006 en español) | pendiente de decisión del operador; no registrada |
| P1-2 (extraer filas de OPT truncadas) | pendiente de decisión del operador; no registrada. Su urgencia baja: la compactación ya redujo el tamaño de `MEMORIES.md` (ACH4.4), lo que cubre la causa de raíz de S1 del ciclo 1 |

### Efecto observado de propuestas anteriores

| ID | Observación |
| --- | --- |
| OPT-013 / 014 / 017 / 020 / 021 | Aplicadas y verificadas en `AGENTS.md` (L43, L62, L74-75); el ciclo 2 usó el colapso jerárquico de `MEMORIES.md` por primera vez. Coste de la primera compactación: 44 292 tokens. Efecto sobre el coste de lectura de OPTIMIZER: se medirá en el ciclo 3 (esta sesión ya lee un `MEMORIES.md` compactado). |
| OPT-023 | Se mantiene: cifras `agent-result` como cota inferior. |
| OPT-031 | Aplicada de nuevo: escalera de `usage-slice` omitida. |

### Decisión del operador y registro (2026-10-12)

Operador: "APlica P2, P1-1 y P1-2 tu y luego procede a llamar al documenter". La sesión principal aplicó las tres.

| ID prov. | Estado | OPT | Aplicación |
| --- | --- | --- | --- |
| P1-1 | aprobada y aplicada | OPT-042 | `CLAUDE.md` L46 |
| P1-2 | aprobada y aplicada | OPT-043 | `CLAUDE.md` "Minimal Reading" |
| P2-1 | aprobada y aplicada | OPT-044 | `AGENTS.md` L75 |

R6 resuelto: con P2-1, `sin datos` declarado no medible es final, así que OPT-001 a 004 quedan bien archivadas. No se compactan más filas ahora.
