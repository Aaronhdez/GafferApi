# Paso 04 (ciclo 4) - "Hierarchical Summary": OPT-013 + OPT-014 + OPT-021 (consolidadas)

Aplicación manual del operador en `AGENTS.md`; compactación única de `MEMORIES.md` por OPTIMIZER con aprobación. Ejecución según QH0; archivo según QH2.

## Objetivo

Que `STATUS.md` (021), `OPTIMIZATION.md` (013) y `MEMORIES.md` (014) no crezcan sin límite: detalle completo solo para lo reciente o pendiente; lo cerrado, en una fila de índice; el detalle íntegro, en el historial de Git.

## Fichero, secciones y texto propuesto

### `AGENTS.md`, `## Plans and Evidence` (OPT-013 + OPT-021)

Nueva viñeta al final de la sección:

```markdown
- Hierarchical summary: `STATUS.md` and `OPTIMIZATION.md` keep full detail only for the current and the previous cycle. Older cycles collapse into one row of an index table at the top of the file (`Ciclo | Paso | Veredicto | AC / IDs | Referencia`), where `Referencia` is the commit that holds the full detail. BUILDER compacts `STATUS.md` when it starts a cycle; OPTIMIZER compacts `OPTIMIZATION.md` when it starts its analysis. (OPT-013, OPT-021)
```

Cláusula opcional (sugerencia de PLANNER, no forma parte de las OPT aprobadas; el operador decide): "Operator answers quoted verbatim, open blockers and `Documentación modificada` are never collapsed."

### `AGENTS.md`, `## Usage and Optimization Memory` (OPT-014)

1. Cambiar "`.github/agent-harness/MEMORIES.md` is append-only and outside plans." por "`.github/agent-harness/MEMORIES.md` is append-only, except for the approved compaction below, and outside plans."
2. Nueva viñeta:

```markdown
- `MEMORIES.md` compaction: rows whose `Estado` is `aplicada` or `revertida` and whose `Efecto observado` is final collapse into a summary table at the top of the file (`ID | Plan | Problema (1 línea) | Estado`); `pendiente` rows keep full detail. Full rows remain in Git history. Only OPTIMIZER compacts, after explicit operator approval. (OPT-014)
```

(Con QH2 = b), añadir `MEMORIES-archive.md` al alcance de OPTIMIZER en la tabla de roles; no recomendado.)

### Compactación inicial de `MEMORIES.md`

Tras editar `AGENTS.md` y cuando las OPT de los pasos 01-03 estén en `aplicada`, OPTIMIZER (modo registro, con aprobación explícita) aplica la regla por primera vez. OPT-001 a OPT-004 ya están `aplicada` con "sin datos": candidatas a la tabla resumen.

## Criterios de aceptación

| ID | Criterio |
| --- | --- |
| ACH4.1 | `grep -n "OPT-013, OPT-021" AGENTS.md` → 1 línea en `## Plans and Evidence`. |
| ACH4.2 | `grep -n "OPT-014" AGENTS.md` → 1 línea en `## Usage and Optimization Memory`, y la frase "append-only" incluye la excepción (sin contradicción interna; lectura dirigida). |
| ACH4.3 | La regla no contradice los permisos: BUILDER ya puede editar `STATUS.md`; OPTIMIZER, `OPTIMIZATION.md` y `MEMORIES.md` (con aprobación). Ningún rol nuevo edita ficheros nuevos (QH2 = a)). |
| ACH4.4 | Compactación inicial (si el operador la aprueba): `MEMORIES.md` con tabla resumen al inicio, filas `pendiente` intactas, IDs sin huecos ni duplicados (`grep -o "OPT-[0-9]*"` contado antes y después). |
| ACH4.5 | Si QH1 = b): equivalentes en `.claude/agents/builder.md`/`optimizer.md` y ficheros de Copilot que remitan a `AGENTS.md`. |

## Cómo demostrarlo

- `grep` antes/después en `STATUS.md`; recuento de IDs de `MEMORIES.md` antes/después.
- Efecto (no es AC): coste de registro de OPTIMIZER entre ciclos (serie 37 922 → 45 006 → 50 800 tokens de TECHDEBT-003) y tamaño de `STATUS.md` en TECHDEBT-004.
