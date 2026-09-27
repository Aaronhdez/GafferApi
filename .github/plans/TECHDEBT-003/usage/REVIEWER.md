# Uso - REVIEWER (TECHDEBT-003)

| Ciclo | Tokens entrada | Tokens salida | Origen del dato | Llamadas a herramientas | Ficheros leídos (nº y mayores) | Comandos con salida grande | Observaciones |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | ~45k (mayoría caché) | ~6k | estimado | ~14 | ~5 (git diff completo, 01-sql-connection-provider-lifecycle.md, STATUS.md, 00-overview (grep)) | `git diff` completo (reescritura de fichero por EOL, ~340 líneas); `dotnet test`/`dotnet build` filtrados con grep | Red de AC1.2 reproducido en copia `git archive HEAD` en scratchpad (sin tocar el árbol). Veredicto APPROVED; F1 EOL (CRLF + `\r` final) no bloqueante. |
