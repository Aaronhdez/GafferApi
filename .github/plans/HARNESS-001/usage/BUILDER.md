# Usage - BUILDER

| Ciclo | Tokens entrada | Tokens salida | Origen del dato | Llamadas a herramientas | Ficheros leídos (nº y mayores) | Comandos con salida grande | Observaciones |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | ~45 000 | ~7 000 | estimado | 14 | 8 (pasos 01-05 completos ~15 KB; `STATUS.md`; resto por `grep`/`sed` acotado) | 1 (`grep` de `MEMORIES.md` con filas completas OPT-006/013/014/021, ~8 KB) | Solo verificación por `grep`; sin build/tests (no hay código). Sin "red" previo: cambios ya versionados por el operador. |
| 2 | ~40 000 | ~8 000 | estimado | 14 | 2 completos (`STATUS.md` ~12 KB, `REVIEW.md` ~7 KB); ACs de 01-05 extraídos por `grep` | 0 (`grep`/`diff` filtrados; `git diff -U0` recortado a 140 columnas) | Solo nueva verificación por `grep`; sin build/tests (no hay código). BUILDER no editó ficheros del harness. Sin colapso: el ciclo 1 es `NEEDS_CHANGES`. 1 reintento: un heredoc de Bash con muchas comillas invertidas falló y se rehízo con `Edit` |
