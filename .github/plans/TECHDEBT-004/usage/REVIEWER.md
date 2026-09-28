# Uso - REVIEWER (TECHDEBT-004)

| Ciclo | Tokens entrada | Tokens salida | Origen del dato | Llamadas a herramientas | Ficheros leídos (nº y mayores) | Comandos con salida grande | Observaciones |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | ~45k (mayoría caché) | ~6k | estimado | ~14 | ~5 (STATUS.md, 01-*.md, SqlConnectionProvider.cs, diff de tests, REVIEW.md) | `dotnet build --no-incremental` (x3, filtrado); `dotnet test` suite completa y filtro (quiet); red en copia `git archive HEAD` en scratchpad | Paso 01 APPROVED. Red y mutación AC1.3 reproducidas en copias aisladas para no tocar el repo. |
| 2 | ~40k (mayoría caché) | ~6k | estimado | ~12 | ~6 (STATUS.md, REVIEW.md, 02-*.md, Program.cs diff, 2 tests nuevos) | `dotnet build --no-incremental` (x2, filtrado); `dotnet test` suite completa x2 (quiet) y filtros; red en copia del árbol en scratchpad | Paso 02 APPROVED. Red reproducido revirtiendo `Program.cs` en copia aislada; suite repetida con la variable preestablecida para comprobar aislamiento. |
