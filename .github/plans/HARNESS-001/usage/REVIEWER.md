# Usage - REVIEWER

| Ciclo | Tokens entrada | Tokens salida | Origen del dato | Llamadas a herramientas | Ficheros leídos (nº y mayores) | Comandos con salida grande | Observaciones |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | ~35 000 | ~6 000 | estimado | 10 | 9 (pasos 01-05, `00-*.md` ~10 KB, `STATUS.md` ~12 KB; resto por `grep` acotado) | 0 | Solo verificación por `grep`; red reproducido desde `5352828^`; sin build/tests (no hay código). |
| 2 | ~45 000 | ~9 000 | estimado | 14 | 3 por rangos (`STATUS.md` L1-36 y L93-172, `REVIEW.md` ~7 KB, AC de 01-05 por `grep`) | 0 (diff/grep de `MEMORIES` en scratchpad, salida truncada con `cut`) | Revisión con `grep`; ACH4.4 verificada con `grep -vxFf` (filas literales) y `diff` de IDs; red con `git show HEAD:`; sin build/tests (no hay código). |
