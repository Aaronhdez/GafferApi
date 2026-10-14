# Uso - BUILDER

| Ciclo | Tokens entrada | Tokens salida | Origen del dato | Llamadas a herramientas | Ficheros leídos (nº y mayores) | Comandos con salida grande | Observaciones |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | ~90k (mayoría caché) | ~14k | estimado | ~38 | ~14 (mayores: `SqlPlayerRepositoryShould.cs` ~330 líneas, `01-value-criteria.md`, `SqlPlayerRepository.cs`) | Ninguno sin filtrar (suite con TRX y `uniq -c`; build/format filtrados) | Bloqueo B1 (Docker/MySQL caído): 700 tests fallan en `OneTimeSetUp`; evidencia de SQL mediante sonda ad hoc en scratchpad |
| 1 (reanudación) | ~45k (mayoría caché) | ~6k | estimado | ~20 | 4 (`STATUS.md`, `01-value-criteria.md`, `SqlPlayerRepository.cs`, `usage/BUILDER.md`) | Ninguno sin filtrar (3 ejecuciones de suite a fichero en scratchpad + `grep Total:`) | B1 resuelto; línea base AC1.1 en copia `git archive HEAD` (699); refactor LINQ D-LINQ; 723 antes/después |
