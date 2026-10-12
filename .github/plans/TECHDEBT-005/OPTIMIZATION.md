# OPTIMIZATION - TECHDEBT-005

## Resumen de ciclos

El detalle completo se conserva solo para los 2 ciclos más recientes (OPT-013); el resto está en el historial de Git.

| Ciclo | Paso | Veredicto | ID(s) de propuesta | Ahorro esperado | Estado de aprobación |
| --- | --- | --- | --- | --- | --- |
| 1 | 01 `Update(Facility)` fuera del contrato | APPROVED | P1-1 (OPT-032), P1-2 (OPT-033) | 3-5 llamadas BUILDER + 2-5 REVIEWER por paso | Aprobadas 2026-10-11 |
| 2 | 02 Validación de dominio de Player | APPROVED | P2-1 (OPT-034), P2-2 (OPT-035), P2-3 (OPT-036) | ~12-16 llamadas BUILDER (P2-1) en pasos 03-04 | P2-1 y P2-3 aprobadas 2026-10-12; P2-2 no abordada por el operador |
| 3 | 03a Validación de Facility y dirección | APPROVED | P3-1, P3-2, P3-3 | P3-1: ~15-25 llamadas BUILDER en 03b (estimación) | Pendiente de decisión del operador |

## Ciclo 2 - Paso 02 (validación de dominio de Player) - APPROVED

### Uso medido

`usage-slice` ejecutado una vez (ventanas de `STATUS.md`, `--repo gaffer-api`): "sin datos" en ambas ventanas, claude-code y copilot. Cuarto ciclo sin datos; sin escalera de reintento (OPT-031). Sin `token-consumer` no se separa entrada nueva de caché.

| Rol | Tokens | Tool uses | Duración | Origen | Autoinforme `usage/*.md` (`estimado`) |
| --- | --- | --- | --- | --- | --- |
| BUILDER | 165 359 (ciclo 1: 59 467; x2,8) | 80 (ciclo 1: 29; x2,8) | 771 s (ciclo 1: 257 s; x3,0) | `medido:agent-result` (cota inferior, OPT-023) | ~150 000 + ~25 000, 55 llamadas (subestima las llamadas en 25) |
| REVIEWER | 98 414 (ciclo 1: 39 989; x2,5) | 39 (ciclo 1: 19; x2,1) | 497 s (ciclo 1: 161 s; x3,1) | `medido:agent-result` (cota inferior, OPT-023) | ~70 000 + ~9 000, 27 llamadas (subestima en 12) |

Los tokens por llamada son estables (BUILDER ~2 070 en ciclo 2 frente a ~2 050 en ciclo 1; REVIEWER ~2 520 frente a ~2 100): el coste crece con el número de llamadas, no con lecturas grandes.

### Spikes

| # | Spike | Valor / origen | Causa probable |
| --- | --- | --- | --- |
| S1 | BUILDER x2,8 en tokens y llamadas | 165 k / 80 usos frente a 59 k / 29 | Alcance del paso, no lecturas: 12+ ficheros de producto/test, 89 tests nuevos (324 a 413), caracterización AC2.0 de 13 casos, dos rojos (aceptación y compilación), cambio de comportamiento Q17 con 4 tests adaptados, `ValidationStartup` nuevo y ~13 invocaciones de build/test en `STATUS.md`. Lecturas contenidas: 16 ficheros, el mayor `00-overview` ~17 KB. |
| S2 | Primer verde fallido | `STATUS.md`: 3 errores (aserción `Contain([])` del helper; test de 29-feb mal planteado) | Fallos de test propios; un ciclo extra build+test (~3-4 llamadas). |
| S3 | Sonda `ZzCycle2ProbeShould.cs` creada y borrada | `STATUS.md` fila AC2.0+AC2.6 | Efecto de OPT-032 rama (b): el AC pide volcado con `StartupTestServer`, pero no hay ayudante reutilizable: ~6-8 llamadas (estimación). Los pasos 03 y 04 la repetirían. |
| S4 | Rojo en worktree por REVIEWER (OPT-033 sin aplicar) | `STATUS.md`: dos filas de REVIEWER | Segunda improvisación de la receta: ~5-8 de las 39 llamadas (estimación). |
| S5 | Pruebas manuales REVIEWER con `dotnet run` + curl | `STATUS.md` fila "pipeline de producción"; 2 arranques descartados | Lo no redundante era confirmar `ConfigureValidationResponses` bajo `AddMvcCore` real, porque `TestFixture` usa `AddControllers`. Los 2 arranques descartados cuestan ~4-6 llamadas (estimación). |
| S6 | `cat` combinado de 4 ficheros del plan (29,7 KB, persistido) | `usage/REVIEWER.md` | REVIEWER relee `STATUS.md`, `REVIEW.md` y `usage` completos cada ciclo; OPT-007 aplicable con lectura por rangos. |

### Propuestas

| ID | Propuesta | Ámbito | Ahorro esperado | Fuente |
| --- | --- | --- | --- | --- |
| P2-1 | Ayudante permanente `[Explicit]` que escribe el `swagger.json` servido por `StartupTestServer` a una ruta por variable de entorno; documentarlo en `test-stack.md`. | Plan (paso 03) y `test-stack.md` | ~12-16 llamadas de BUILDER en pasos 03-04 | `STATUS.md` fila AC2.0+AC2.6; S3 |
| P2-2 | Decidir si `TestFixture` pasa a `AddMvcCore` como producción, o limitar la prueba de producción de REVIEWER. | Decisión del operador / plan (03-04) | ~8-12 llamadas de REVIEWER por paso (estimación) | S5 |
| P2-3 | Limitar el tamaño del paso 03: dividir en 03a (Facility) y 03b (gradas) si prevé más de ~50 tests nuevos o ~10 ficheros. | Agente PLANNER / `03-*.md` | No reduce el total; un `NEEDS_CHANGES` costaría ~80-100 k en lugar de ~165 k | S1, S2 |

**Aprobación (operador, 2026-10-12):** P2-1 registrada como OPT-034; P2-3 como OPT-036; P2-2 como OPT-035 sin aplicación ("Obviemos este punto"; `Estado` queda `pendiente` por falta de estado "descartada").

## Ciclo 3 - Paso 03a (validación de Facility y dirección) - APPROVED

### Uso medido

`usage-slice`: no ejecutado. Los ciclos 1 y 2 de este plan (y HARNESS-001) ya devolvieron "sin datos", así que se cita `usage/*.md` como `estimado` y `agent-result` como `medido` sin repetir la escalera (OPT-031). Sin `token-consumer` no se separa entrada nueva de caché.

| Rol | Tokens | Tool uses | Duración | Tokens/llamada | Origen | Autoinforme `usage/*.md` (`estimado`) |
| --- | --- | --- | --- | --- | --- | --- |
| BUILDER | 177 920 (ciclo 2: 165 359; +7,6 %) | 86 (ciclo 2: 80; +7,5 %) | 760 s (ciclo 2: 771 s) | ~2 069 (ciclo 2: ~2 067) | `medido:agent-result` (cota inferior, OPT-023) | ~180 000 + ~30 000, 62 llamadas (subestima en 24) |
| REVIEWER | 70 446 (ciclo 2: 98 414; -28 %) | 29 (ciclo 2: 39; -26 %) | 234 s (ciclo 2: 497 s; -53 %) | ~2 429 (ciclo 2: ~2 523) | `medido:agent-result` (cota inferior, OPT-023) | ~80 000 + ~10 000, 30 llamadas (acierta) |

Tokens por llamada idénticos a los ciclos 1 y 2: BUILDER cuesta ~2 070 tokens por llamada en tres ciclos. El coste depende solo del número de llamadas.

### Spikes

| # | Spike | Valor / origen | Causa probable |
| --- | --- | --- | --- |
| S1 | BUILDER sigue en 178 k / 86 usos pese a la división (OPT-036) | +7,6 % frente al ciclo 2 | La división se hizo por feature, no por tamaño. 03a cerró con 161 tests nuevos (413 a 574; el umbral de OPT-036 era ~50 tests o ~10 ficheros) y 13 ficheros modificados más 2 nuevos (`git status`). El paso 02 tenía 89 tests y 12+ ficheros: 03a es mayor. Ver análisis más abajo. |
| S2 | La misma matriz de 34 casos inválidos se prueba en tres capas | `STATUS.md` "Tests nuevos por criterio": AC3a.1 (34 en create), AC3a.2 (34 en update), AC3a.7 (34 en `FacilityShould`), AC3a.11 (16 × POST/PUT en producción) | Cada capa exige rojo y verde con su propia lista. Es el origen principal de los 161 tests y del rojo de aceptación de 106/132 que BUILDER tuvo que agrupar con `sort \| uniq -c`. El dominio ya cubre cada regla; aceptación solo necesita demostrar el cableado HTTP. |
| S3 | Primer verde fallido | `STATUS.md`: 2 × CS8602 en `UpdateFacilityCommandHandlerShould` (L76/L228, `command.Address` pasó a anulable) y 1 test (`ThrowExceptionWhenAddressIsMissing` esperaba `NullReferenceException`) | Consecuencia previsible del cambio de nulabilidad de `Address` en el comando; un grep de usos de `command.Address` antes de editar lo habría adelantado. Un ciclo extra build+test (~3-4 llamadas). Repetición del patrón de S2 del ciclo 2. |
| S4 | La tabla de rojo de BUILDER subestimaba el fallo | BUILDER: "16 + 16 `FacilityValidationInTheProductionPipelineShould` (todos **500**)"; REVIEWER: 32 casos = 18 × 500, 7 × 201, 7 × 200 (`STATUS.md` fila REVIEWER rojo; `REVIEW.md` ciclo 3) | BUILDER generalizó a partir del primer fallo (o de una muestra) en lugar de agrupar el estado obtenido por test. No cambió el veredicto (el rojo existe), pero la evidencia de rojo debe ser cuantitativa; REVIEWER pagó la corrección con una ejecución en worktree. El error es de procedimiento: el AC pide demostrar el rojo, no cómo resumirlo. |
| S5 | REVIEWER repite el worktree de `HEAD` (OPT-033 sin aplicar) | `STATUS.md` filas "REVIEWER rojo" y "rojo unitario" | Tercera improvisación (ciclos 1, 2, 3). Menor coste que en el ciclo 2: 29 llamadas frente a 39. |
| S6 | Sin spikes de lecturas | 18 ficheros leídos por BUILDER y 8 por REVIEWER, todos por rangos o `grep`; `00-overview` por `grep` de Q3-Q23 | OPT-007/018/024/025 sin regresión. |

### Por qué BUILDER sigue por encima de 170 k

Evidencia: 80 → 86 llamadas con ~2 070 tokens/llamada constantes. Solo el recuento de llamadas explica el coste.

| Contribuyente | Llamadas estimadas | Origen |
| --- | --- | --- |
| Líneas base y preflight (docker, build, test) | ~4 | `STATUS.md` filas 1-3 |
| Ayudante AC3a.0: crear, ejecutar sin variable, 2 capturas "antes", 1 "después" | ~6-7 | `STATUS.md` filas AC3a.0 y AC3a.8 |
| Rojo de aceptación + agrupación 106 nombres + rojo unitario por compilación | ~5-6 | filas "Rojo" |
| Escribir 161 tests en 5 ficheros de test (aceptación, `FacilityShould`, 2 handlers, converters, repositorio) | ~18-22 | `git status`: 7 ficheros de test |
| Cambios de producto (6 ficheros + refactor `Player.CheckName`) | ~12-14 | `git status` |
| Primer verde fallido (S3) + segundo verde + refactor | ~6-8 | filas "Verde" |
| AC3a.9 (build, test, grep) y lecturas | ~12-15 | filas AC3a.9 y `usage/BUILDER.md` |

Conclusión: OPT-034 eliminó la sonda (~6-8 llamadas), pero el paso añadió 161 tests y una matriz triplicada (S2), que consumieron lo ahorrado. La división de OPT-036 no bajó el coste porque 03a sigue siendo el paso más grande del plan (más grande que el paso 02). Si 03b conserva el mismo patrón (gradas anidadas y `/stands`), se esperan ~80-90 llamadas otra vez.

### Propuestas

| ID | Propuesta | Ámbito | Ahorro esperado | Fuente |
| --- | --- | --- | --- | --- |
| P3-1 | Pirámide de pruebas explícita para pasos de validación: la matriz completa de casos inválidos va en la capa de dominio (`FacilityShould`/`StandShould`) y en un caso por regla en los handlers; los tests de aceptación cubren un representante por tipo de regla (texto vacío, máximo+1, ausente, rango, enum, fecha) en create y update, más la capa de producción (`StartupTestServer`) para ausente/null. PLANNER lo fija en los AC del paso 03b y 05 (no reduce el contrato probado: las reglas siguen cubiertas en dominio). | Agente PLANNER / `03b-*.md` y plantilla de plan | ~15-25 llamadas de BUILDER en 03b (estimación): menos tests de aceptación (de ~70 a ~20), rojo agrupado más corto, menos tiempo de suite | S1, S2; `STATUS.md` "Tests nuevos por criterio" |
| P3-2 | Evidencia de rojo cuantitativa: en la fila de rojo de `STATUS.md`, BUILDER agrupa por resultado obtenido (`... \| grep -oE "Status code[^,]*\|Expected[^,]*" \| sort \| uniq -c` o equivalente) y escribe `n × estado` por clase de test; nunca "todos". Añadirlo a `.claude/agents/builder.md` y `.github/doc/work-guidelines.md`. | `.claude/agents/builder.md` (el operador la aplica) | Evita 1 corrección de REVIEWER (no medible en tokens) y reduce el riesgo de aceptar un rojo mal descrito | S4; `REVIEW.md` ciclo 3 |
| P3-3 | Antes de editar un tipo compartido (p. ej. cambio de nulabilidad de `Address` en el comando), `grep -rn` de sus usos en producto y tests y adaptar en el mismo paso, evitando el primer verde fallido. | `.claude/agents/builder.md` | ~3-4 llamadas por cambio de firma (S3) | `STATUS.md` fila "Verde (primer intento)" |

**Propuesta principal: P3-1.** No se registra nada en `MEMORIES.md` sin la aprobación del operador.

### Brechas de datos

- `token-consumer` sin datos por quinto ciclo seguido en este plan; `agent-result` es cota inferior (OPT-023). La descomposición de llamadas de BUILDER es una estimación a partir de las filas de `STATUS.md` y `git status`, no una medición.
- El ahorro de OPT-034 no es medible en totales: el alcance del paso cambió a la vez (89 → 161 tests).

### Efecto observado de memorias anteriores

| ID | Observación |
| --- | --- |
| OPT-034 | Aplicada. El ayudante `ServedOpenApiDocumentDump` (clase `[Explicit]`, `GAFFER_SWAGGER_DUMP_PATH`) existe y no aparece como omitido (413 antes y después). BUILDER lo usó en 4 ejecuciones (sin variable, 2 "antes", 1 "después") y REVIEWER en 3 (HEAD, actual, sin variable); sin sonda ni host duplicado, sha256 idéntico a la captura del ciclo 2 (`be49f0ab...a5a6bfe`) y a lo largo de HEAD y árbol actual. Evita las ~6-8 llamadas de la sonda del ciclo 2; el ahorro no se ve en los totales por el mayor alcance. Pendiente: documentarlo en `test-stack.md` (observación 6 de BUILDER). |
| OPT-036 | Aplicada por PLANNER (03a Facility + dirección; 03b gradas y `/stands`; health check al paso 05). Efecto en coste: nulo, BUILDER +7,6 % (177 920 frente a 165 359) porque 03a superó los umbrales del propio OPT-036 (161 tests y 15 ficheros frente a ~50 y ~10). Efecto en riesgo: positivo, APPROVED en el primer ciclo, sin `NEEDS_CHANGES`. REVIEWER bajó a 70 446 (-28 %). El umbral del tamaño no se ha comprobado antes de dividir; ver P3-1. |
| OPT-035 | Sin aplicación del operador, pero REVIEWER no usó `dotnet run` ni arranques descartados: AC3a.11 (`FacilityValidationInTheProductionPipelineShould`, `StartupTestServer`) cubre la divergencia de `AddMvcCore` en tests. 39 → 29 llamadas de REVIEWER; no se atribuye a OPT-035 al no estar aplicada. |
| OPT-033 | Sin aplicar por el operador; REVIEWER repitió el worktree (S5). Sigue `pendiente`. |
| OPT-032 | Rama (b) aplicada otra vez, ahora con el ayudante: sin desviación que justificar (`FacilitiesControllerContractShould` en verde). |
| OPT-031 | Aplicada: sin llamada a `usage-slice`. |
| OPT-007 | Mejor: REVIEWER leyó `STATUS.md` y `03a-*` en un solo `cat`; sin relecturas persistidas de decenas de KB. |
