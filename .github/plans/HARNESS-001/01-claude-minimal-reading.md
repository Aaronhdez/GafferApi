# Paso 01 (ciclo 1) - `CLAUDE.md` "Minimal Reading": OPT-005, OPT-006, OPT-007, OPT-018

Aplicación manual del operador. Ejecución según QH0; ubicación de OPT-018 según QH5; réplica en Copilot según QH1.

## Objetivo

Que BUILDER/REVIEWER decidan rojo/verde por código de salida (005), capturen solo el resumen de MSBuild al contar warnings (006), citen `STATUS.md` en vez de repetir salidas (007) y añadan miembros a ficheros existentes con `Edit` anclado (018).

## Fichero, sección y texto propuesto

Fichero: `CLAUDE.md`, sección `## Minimal Reading` (hoy un solo párrafo). Se mantiene el párrafo y se añaden estas viñetas debajo:

```markdown
- Red/green comes from the process exit code (`$LASTEXITCODE` in PowerShell, `$?` in bash), never from grepping status words: .NET/MSBuild console output is localized (e.g. `Correctas`/`Con error`, `Advertencia(s)`/`Errores`). Use `grep` only to extract failure details. (OPT-005)
- Warning counts: capture only the MSBuild summary, e.g. `dotnet build gaffer-api.sln --no-incremental -v q 2>&1 | grep -E "Warning\(s\)|Error\(s\)|Advertencia\(s\)|Errores"`; keep the full listing only when a criterion compares which warnings are new (initial baseline, final check). (OPT-006)
- Once a command result is summarized in `STATUS.md`, cite that row instead of re-running or re-quoting the full output in later turns. (OPT-007)
- To add several members to an existing file (e.g. test methods), use `Edit` anchored on a stable line (such as just before the closing brace of the class); never splice files with `head`/`tail`/`sed`. (OPT-018)
```

Si QH5 = b), la última viñeta va en `.claude/agents/builder.md`, `## Work`, como continuación del punto 4 ("Follow the double test loop...").

## Criterios de aceptación

| ID | Criterio |
| --- | --- |
| ACH1.1 | `grep -n "OPT-005\|OPT-006\|OPT-007" CLAUDE.md` → 3 líneas dentro de `## Minimal Reading`. |
| ACH1.2 | `grep -n "OPT-018" CLAUDE.md .claude/agents/builder.md` → 1 línea en la ubicación de QH5. |
| ACH1.3 | El párrafo original de "Minimal Reading" se conserva; ninguna otra sección de `CLAUDE.md` cambia (`git diff --stat CLAUDE.md` y lectura del diff). |
| ACH1.4 | Sin contradicción con `AGENTS.md` (lectura dirigida: `AGENTS.md` no define otra forma de decidir rojo/verde ni de contar warnings). |
| ACH1.5 | QH1 = b): el equivalente en `.github/copilot-instructions.md` existe (`grep`). QH1 = a): no aplica. |

## Cómo demostrarlo

- "Red": los `grep` de ACH1.1-ACH1.2 sin resultados antes de editar (registrar en `STATUS.md`). "Green": después.
- Efecto (no es AC): en el siguiente ciclo de TECHDEBT-004, `usage/BUILDER.md` sin relanzamientos por `grep` de literales de estado. OPTIMIZER lo anota en `Efecto observado`.
