# Uso - REVIEWER (TECHDEBT-003)

| Ciclo | Tokens entrada | Tokens salida | Origen del dato | Llamadas a herramientas | Ficheros leídos (nº y mayores) | Comandos con salida grande | Observaciones |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | ~45k (mayoría caché) | ~6k | estimado | ~14 | ~5 (git diff completo, 01-sql-connection-provider-lifecycle.md, STATUS.md, 00-overview (grep)) | `git diff` completo (reescritura de fichero por EOL, ~340 líneas); `dotnet test`/`dotnet build` filtrados con grep | Red de AC1.2 reproducido en copia `git archive HEAD` en scratchpad (sin tocar el árbol). Veredicto APPROVED; F1 EOL (CRLF + `\r` final) no bloqueante. |
| 2 | ~70k (mayoría caché) | ~7k | estimado | ~16 | ~6 (diff de tests ~33KB vía fichero persistido, STATUS.md, 02-facility-update-stable-stand-ids.md, REVIEW.md, diff de producto) | `git diff` de tests (~33KB); `dotnet build` filtrado con grep; `dotnet test` con `tail` | Red reproducido en copia `git archive HEAD` (12 fallos). Veredicto APPROVED; H1 (`guid` vs `uuid`) y H2 (concurrencia del límite de 4) no bloqueantes. |
