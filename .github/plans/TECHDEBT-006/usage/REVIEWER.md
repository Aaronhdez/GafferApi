# Usage - REVIEWER

| Ciclo | Tokens entrada | Tokens salida | Origen del dato | Llamadas a herramientas | Ficheros leídos (nº y mayores) | Comandos con salida grande | Observaciones |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | ~70 000 | ~9 000 | estimado | ~20 | ~6 (`STATUS.md` + `01-*` en un `cat`; diff completo del producto; diffs de 4 ficheros de test) | 1 (diff completo del producto, ~300 líneas) | Paso 01 APPROVED; rojo reproducido en worktree (compilación + 8 × Con error); swagger HEAD vs árbol idéntico; suite 688/688. |
| 2 | ~85 000 | ~12 000 | estimado | ~24 | ~5 (`STATUS.md` 1-106 + `02-*` en un `cat`; diff del producto; `ProblemJson.cs`; `StartupTestServer.cs`; rangos de `ValidationStartup.cs`) | 3 (STATUS + plan del paso, ~200 líneas; 2 ejecuciones de la sonda con 8 cuerpos JSON) | Paso 02 APPROVED; rojo en worktree 197 × Con error; sonda de producción antes/después con valores idénticos; swagger igual; suite 697/697. Primer heredoc de `REVIEW.md` falló por las comillas (`bash: unexpected EOF`); se rehízo con `Edit`. |
