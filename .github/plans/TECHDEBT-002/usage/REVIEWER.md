# Uso - REVIEWER (TECHDEBT-002)

| Ciclo | Tokens entrada | Tokens salida | Origen del dato | Llamadas a herramientas | Ficheros leídos (nº y mayores) | Comandos con salida grande | Observaciones |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | ~45k (mayoría caché) | ~5k | estimado | ~11 | ~5 (STATUS.md, 01-player-partial-update.md, diff completo de código/tests) | `git diff` de código y tests (~350 líneas); `dotnet build/test` filtrados con `tail`/`grep` | Paso 01 APPROVED. Sin reproducir el red (requeriría revertir código de producto); se acepta la evidencia red de BUILDER con mensajes y control negativo. |
