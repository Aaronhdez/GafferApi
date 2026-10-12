# OPTIMIZATION - TECHDEBT-005

## Resumen de ciclos

El detalle completo se conserva solo para los 2 ciclos más recientes (OPT-013); el resto está en el historial de Git.

| Ciclo | Paso | Veredicto | ID(s) de propuesta | Ahorro esperado | Estado de aprobación |
| --- | --- | --- | --- | --- | --- |
| 1 | 01 `Update(Facility)` fuera del contrato | APPROVED | P1-1 (OPT-032), P1-2 (OPT-033) | 3-5 llamadas BUILDER + 2-5 REVIEWER por paso | Aprobadas 2026-10-11 |
| 2 | 02 Validación de dominio de Player | APPROVED | P2-1 (OPT-034), P2-2 (OPT-035), P2-3 (OPT-036) | ~12-16 llamadas BUILDER (P2-1) en pasos 03-04 | P2-1 y P2-3 aprobadas 2026-10-12; P2-2 no abordada por el operador |
| 3 | 03a Validación de Facility y dirección | APPROVED | P3-1 (OPT-037), P3-2 (OPT-038), P3-3 (OPT-039) | P3-1: ~15-25 llamadas BUILDER en 03b (estimación) | Aprobadas 2026-10-12 |
| 4 | 03b Validación de gradas y `/stands` | APPROVED | P4-1 (OPT-040), P4-2 (OPT-041) | P4-1: ~1 000 tokens/llamada de REVIEWER (estimación) | Aprobadas 2026-10-12 |
| 5 | 05 Health check de base de datos | APPROVED | Sin propuestas nuevas | n/a | n/a |

## Ciclo 4 - Paso 03b (validación de gradas y `/stands`) - APPROVED

### Uso medido

`usage-slice`: no ejecutado. Los ciclos 1-3 de este plan ya devolvieron "sin datos" (OPT-031); se cita `usage/*.md` como `estimado` y los totales del ORCHESTRATOR como `medido:agent-result` (cota inferior, OPT-023). Sin `token-consumer` no se separa entrada nueva de caché.

| Rol | Tokens | Tool uses | Duración | Tokens/llamada | Autoinforme `usage/*.md` (`estimado`) |
| --- | --- | --- | --- | --- | --- |
| BUILDER | 173 693 (ciclo 3: 177 920; -2,4 %) | 87 (ciclo 3: 86; +1) | 742 s (ciclo 3: 760 s) | ~1 997 (ciclo 3: ~2 069) | ~170 000 + ~28 000, 58 llamadas (subestima las llamadas en 29) |
| REVIEWER | 111 762 (ciclo 3: 70 446; +58,7 %) | 32 (ciclo 3: 29; +3) | 282 s (ciclo 3: 234 s) | ~3 493 (ciclo 3: ~2 429; +44 %) | ~75 000 + ~11 000, 24 llamadas (subestima en 8) |

### Spikes

| # | Spike | Valor / origen | Causa probable |
| --- | --- | --- | --- |
| S1 | BUILDER no baja de ~87 llamadas pese a un paso con 63 tests nuevos (574 a 637) frente a 161 en 03a | 87 usos, 173 k; `git status` | El coste por llamada es constante (~2 000) y el número de llamadas se mantiene con ~60 % menos de tests. Entonces el volumen de tests no era el factor dominante de las llamadas: pesan los ficheros de producto (11 modificados), los dos rojos (aceptación y compilación), un tercer rojo de producción del sub-recurso tras un fallo propio del test (`"Id"` mayúsculas, 4 × `ArgumentNullException`; `STATUS.md` fila 4) y tres pasadas de resumen del rojo para obtener el `n × estado` (`usage/BUILDER.md`). Descomposición estimada, no medida. |
| S2 | Tokens/llamada de REVIEWER sube a ~3 493 (+44 %) | 111 762 / 32 | El número de llamadas apenas cambia (+3); crece el contexto por llamada: diff completo de producto, diff de tests (~600 líneas en dos llamadas), `StandShould.cs` entero y `REVIEW.md` del ciclo 3 (`usage/REVIEWER.md`). Con contexto acumulado en caché, cada llamada posterior re-lee un contexto más grande. Atenuante: el rojo se reprodujo sobre `git archive HEAD` sin worktree (más barato que el worktree). |
| S3 | Un fallo propio de test en el rojo de producción | `STATUS.md` fila 4 (segunda) | Lectura de `id` sensible a mayúsculas; ~2-3 llamadas extra (estimación). |

### Efecto observado de OPT-037, OPT-038 y OPT-039

| ID | Evidencia | Efecto |
| --- | --- | --- |
| OPT-037 | `REVIEW.md` ciclo 4: "Reparto por capa (OPT-037): cumple" (matriz solo en `StandShould`; un caso por regla en handlers; aceptación con un representante por tipo; producción solo casos divergentes). Tests nuevos 63 frente a 161 (-61 %); AC3b.1 rojo 3/3 con un representante por regla. | Aplicada y cumplida. Ahorro de BUILDER no observable: 87 frente a 86 llamadas y -2,4 % de tokens, frente a ~15-25 llamadas esperadas. El alcance cambió a la vez (gradas son más pequeñas que facility+dirección), así que no se puede aislar. Sí redujo carga de suite y de rojo (32/199 frente a 106/154 de rojos agrupados). |
| OPT-038 | `STATUS.md` tabla "Rojo (OPT-038), por clase de test"; REVIEWER reprodujo 32/199 (mismo recuento) y no halló imprecisión (en el ciclo 3 hubo una). | Aplicada y eficaz sobre su objetivo (cero correcciones de REVIEWER). Coste: tres pasadas de resumen en BUILDER (~2-3 llamadas, estimación). |
| OPT-039 | `REVIEW.md` ciclo 4: "grep de usos registrado; sin cambios de firma ni de nulabilidad de tipos compartidos". Verde 350/350 al primer intento. | Aplicada. Eficacia no demostrable: el paso no cambió firmas compartidas, así que no se puso a prueba. El primer verde limpio es coherente pero no atribuible. |

BUILDER sigue en ~87 llamadas porque las tres reglas atacan el volumen de tests, el rojo mal descrito y el primer verde fallido, y ninguna ataca el grueso (cambios de producto, rojos múltiples, resúmenes). El ahorro esperado de OPT-037 estaba sobreestimado.

### Propuestas

| ID | Propuesta | Ámbito | Ahorro esperado | Fuente |
| --- | --- | --- | --- | --- |
| P4-1 | REVIEWER lee el diff de tests por `git diff --stat` + `grep -n "public .*Task\|\[Fact\|\[Theory"` de los nombres nuevos y solo abre por rango los que justifican un AC; el diff completo solo para producto. | `.claude/agents/reviewer.md` (lo aplica el operador) | ~500-1 000 tokens/llamada de REVIEWER (estimación) | S2 |
| P4-2 | BUILDER define una sola vez el comando de resumen del rojo (`... \| grep -oE ... \| sort \| uniq -c` por clase) y lo reutiliza; evita las tres pasadas. Aplicable al paso 05. | Plan paso 05 / `builder.md` | ~2-3 llamadas de BUILDER por rojo | S1 |

**Propuesta principal: P4-1.**

### Brechas de datos

- `token-consumer` sin datos por sexto ciclo seguido en este plan; `agent-result` es cota inferior. La descomposición de S1 y el ahorro de P4-1/P4-2 son estimaciones.
- El efecto de OPT-037 no se separa del cambio de alcance entre 03a y 03b.

**Aprobación (operador, 2026-10-12, "Apruebo el P4-1 y 2, y confirmo el 400, luego lanza el paso 5"):** P4-1 registrada como OPT-040 y P4-2 como OPT-041, ambas `pendiente`; el ORCHESTRATOR las pasa a REVIEWER y BUILDER en el ciclo 5 y el operador las aplica a mano en `reviewer.md`/`builder.md`.

## Ciclo 5 - Paso 05 (health check de base de datos) - APPROVED

### Uso medido

`usage-slice`: no ejecutado (OPT-031; los ciclos 1-4 ya devolvieron "sin datos"). Totales del ORCHESTRATOR como `medido:agent-result` (cota inferior, OPT-023); `usage/*.md` como `estimado`. Sin `token-consumer` no se separa entrada nueva de caché.

| Rol | Tokens | Tool uses | Duración | Tokens/llamada | Autoinforme `usage/*.md` (`estimado`) |
| --- | --- | --- | --- | --- | --- |
| BUILDER | 97 593 (ciclo 4: 173 693; -43,8 %) | 37 (ciclo 4: 87; -57 %) | 488 s (ciclo 4: 742 s) | ~2 638 (ciclo 4: ~1 997) | ~95 000 + ~16 000, 30 llamadas (subestima en 7) |
| REVIEWER | 64 496 (ciclo 4: 111 762; -42,3 %) | 23 (ciclo 4: 32; -28 %) | 267 s (ciclo 4: 282 s) | ~2 804 (ciclo 4: ~3 493; -19,7 %) | ~70 000 + ~10 000, 17 llamadas (subestima en 6) |

### Spikes

| # | Spike | Valor / origen | Causa probable |
| --- | --- | --- | --- |
| S1 | Ninguno al alza: es el ciclo más barato del plan para ambos roles | BUILDER 97 k / 37; REVIEWER 64 k / 23 | Paso menor (suite 637 a 665, +28 tests; 4 × 404 y 3 × CS0246 de rojo; verde al primer intento) y reglas OPT-037/038/039/041 ya aplicadas. |
| S2 | Tokens/llamada de BUILDER sube a ~2 638 (+32 %) | 97 593 / 37 | Menos llamadas con más contexto acumulado por llamada (`STATUS.md` completo de 116 líneas antes de compactar, `05-*`, varios ficheros de arranque). El coste total baja porque el recuento de llamadas cae más que lo que sube el coste por llamada. |
| S3 | Heredoc largo fallido en Git Bash (REVIEWER, también en ciclos 3-4 para BUILDER) | `usage/REVIEWER.md` ciclo 5 | Repetición del patrón; se resolvió con `Write` a scratchpad + `cat >>`. Coste: 1-2 llamadas. |
| S4 | AC5.5 mide 5009-5015 ms con timeout de 5 s | `STATUS.md` observación 1; `usage/REVIEWER.md` | No es un coste de tokens: riesgo de prueba sensible al tiempo, aceptado por REVIEWER. Pendiente de decisión del operador. |

### Efecto observado de OPT-040 y OPT-041

| ID | Evidencia | Efecto |
| --- | --- | --- |
| OPT-040 | `REVIEW.md` ciclo 5: `git diff --stat` + `git status`, diff completo solo de producto, inventario de tests por `grep` de `[Test]`/`[TestCase`/`public .*Task`, rangos abiertos solo para AC5.2, AC5.4, AC5.5. REVIEWER 6 ficheros leídos y 17 llamadas (autoinforme) frente a 24 y 7. | Aplicada y eficaz. Tokens/llamada -19,7 % (3 493 a 2 804), alineado con las ~500-1 000 tokens/llamada esperadas (~700 observadas). El total (-42 %) incluye el efecto del alcance menor. Defecto del texto registrado: citaba `[Fact]`/`[Theory]` (xUnit); el proyecto usa NUnit y el ORCHESTRATOR lo adaptó a `\[Test\]\|\[TestCase`. Corregido en MEMORIES.md. |
| OPT-041 | `STATUS.md` fila 5 "Resumen del rojo (OPT-041)": comando único para tests y para compilación, reutilizado en los dos rojos; `usage/BUILDER.md`: "comando único de OPT-041". | Aplicada y eficaz sobre su objetivo: una definición en lugar de tres pasadas (~2-3 llamadas ahorradas, estimación). No aislable del menor alcance. |

### Propuestas

Ninguna nueva. Las dos observaciones menores (heredocs largos en Git Bash, AC5.5 sensible al tiempo) no justifican una regla nueva: OPT-024 y el uso de `Write` ya cubren lo primero, y lo segundo es una decisión del operador.

**Propuesta principal: ninguna nueva.** Pendiente de aplicación permanente por el operador: OPT-033, OPT-035, OPT-037 a OPT-041 en `reviewer.md`/`builder.md`/plantilla de plan, según su ámbito.

### Brechas de datos

- `token-consumer` sin datos por séptimo ciclo seguido en este plan; `agent-result` es cota inferior. Ahorros de OPT-040/041 estimados y confundidos con el cambio de alcance.

## Cierre del plan: tendencia de los ciclos 1-5

| Ciclo | Paso | BUILDER tokens / llamadas | REVIEWER tokens / llamadas | Suite (tests) |
| --- | --- | --- | --- | --- |
| 1 | 01 | n/d en este archivo (detalle en Git) | n/d | 322 a 324 |
| 2 | 02 | 165 359 / 80 | 98 414 / 39 | 324 a 413 |
| 3 | 03a | 177 920 / 86 | 70 446 / 29 | 413 a 574 |
| 4 | 03b | 173 693 / 87 | 111 762 / 32 | 574 a 637 |
| 5 | 05 | 97 593 / 37 | 64 496 / 23 | 637 a 665 |

Tendencia: BUILDER estuvo plano en ~165-178 k y 80-87 llamadas durante los ciclos 2-4 (coste ~2 000 tokens/llamada constante, el recuento de llamadas manda) y cayó 44 % en el ciclo 5. REVIEWER oscila (98 k, 70 k, 112 k, 64 k) según el tamaño del diff de tests y del método de rojo; el ciclo 5 es su mínimo.

| OPT | Ahorro real | Valoración |
| --- | --- | --- |
| OPT-034 (ayudante swagger) | Sin sonda ni host duplicado, sha256 estable en ciclos 3-5; ~6-8 llamadas evitadas | Real, no visible en totales |
| OPT-038 (rojo cuantitativo) | Cero correcciones de REVIEWER en ciclos 4-5 (hubo una en el ciclo 3) | Real en calidad, coste ~2-3 llamadas |
| OPT-040 (REVIEWER por `--stat` + `grep`) | -19,7 % tokens/llamada | Real, medida parcial |
| OPT-041 (resumen de rojo único) | ~2-3 llamadas BUILDER por rojo | Real, estimada |
| OPT-036 (dividir pasos) | Riesgo menor (APPROVED en primer ciclo en 3, 4 y 5); coste nulo en 03a | Real en riesgo, no en coste |
| OPT-037 (pirámide de pruebas) | 161 a 63 tests; ahorro de BUILDER no aislable | Aplicada, ahorro no demostrado |
| OPT-039 (grep de usos antes de cambiar firma) | No puesta a prueba | Sin evidencia |
| OPT-033, OPT-035 | Sin aplicar por el operador | Pendiente |

Conclusión: el ahorro más claro llega en el ciclo 5 por la combinación de un paso pequeño con OPT-037/038/040/041. La mayor palanca restante habría sido ajustar el tamaño del paso por llamadas esperadas (~85) en vez de por feature; el paso 05 lo confirma indirectamente. Sin datos de `token-consumer` no se puede separar caché de entrada nueva en ningún ciclo.
