# Paso 02 (ciclo 2) - `CLAUDE.md` "Measured Usage (Claude)": OPT-008, OPT-009, OPT-011

Aplicación manual del operador. Ejecución según QH0; réplica en Copilot según QH1.

## Objetivo

Una secuencia única de reintentos de `usage-slice` antes de pasar a `medido:agent-result` (008 → 009 → 011) y `Inicio`/`Fin` de REVIEWER convertidos a UTC con un comando explícito (009).

## Ficheros, secciones y texto propuesto

### `CLAUDE.md`, `## Measured Usage (Claude)`

Debajo del punto `1. medido:token-consumer: ...`, sub-viñetas indentadas:

```markdown
   - If `--repo <repo>` returns no data for every window, retry once with `--repo <plan-ID>` (the dashboard labels events with the session `cwd` folder name). (OPT-008)
   - If there is still no data, retry once with the windows shifted back by the local UTC offset (e.g. −2 h for CEST): a shifted cluster means `Inicio`/`Fin` were written in local time with a `Z` suffix. (OPT-009)
   - Stop as soon as a wide window shows no event within ±30 min of the literal window (a telemetry gap, not a time-zone shift) and go to `medido:agent-result`. (OPT-011)
```

### `.claude/agents/reviewer.md`, `## Work`

- Punto 1: `Record \`Inicio\` (ISO 8601 UTC) for this cycle in \`STATUS.md\`.` → `Record \`Inicio\` (ISO 8601 UTC, \`date -u +%Y-%m-%dT%H:%M:%SZ\`) for this cycle in \`STATUS.md\`.` (mismo formato que `.claude/agents/builder.md` L18, que ya lo tiene).
- Punto 6: `Record \`Fin\` ...` → añadir el mismo comando.
- En PowerShell el equivalente es `Get-Date -AsUTC -Format "yyyy-MM-ddTHH:mm:ssZ"` (PowerShell 7.1+); el operador elige uno.

## Criterios de aceptación

| ID | Criterio |
| --- | --- |
| ACH2.1 | `grep -n "OPT-008\|OPT-009\|OPT-011" CLAUDE.md` → 3 líneas bajo el punto 1 de `## Measured Usage (Claude)`, en ese orden. |
| ACH2.2 | `grep -n "date -u" .claude/agents/reviewer.md .claude/agents/builder.md` → reviewer con el comando en `Inicio` y `Fin`; builder sin cambios (ya lo tenía). |
| ACH2.3 | El orden de preferencia 1-2-3 de la sección no cambia; ninguna otra sección cambia (`git diff`). |
| ACH2.4 | QH1 = b): `.github/agents/reviewer.agent.md` con el mismo comando. QH1 = a): no aplica. |

## Cómo demostrarlo

- `grep` antes (sin resultados) y después, registrado en `STATUS.md`.
- Efecto (no es AC): en el siguiente análisis de OPTIMIZER, número de llamadas de `usage-slice` hasta el primer dato o hasta el corte.
