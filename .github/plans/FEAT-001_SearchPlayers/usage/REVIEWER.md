# Uso - REVIEWER

| Ciclo | Tokens entrada | Tokens salida | Origen del dato | Llamadas a herramientas | Ficheros leídos (nº y mayores) | Comandos con salida grande | Observaciones |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | ~60k (mayoría caché) | ~8k | estimado | ~16 | 5 (`STATUS.md` ~110 líneas, `01-value-criteria.md`, `SqlPlayerRepositoryQueryShould.cs` 143 líneas, `SqlPlayerRepositoryShould.cs` líneas 224-315, `REVIEW.md`) | Ninguno sin filtrar (2 suites completas a fichero en scratchpad + `grep Total:`; diff completo solo de producto) | Línea base y rojo reproducidos en un único `git worktree` de `HEAD`; `git worktree remove` falló por ruta larga, resuelto con `rm -rf` + `git worktree prune` |
