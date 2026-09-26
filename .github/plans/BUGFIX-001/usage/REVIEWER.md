# Uso - REVIEWER

| Ciclo | Tokens entrada | Tokens salida | Origen del dato | Llamadas a herramientas | Ficheros leídos (nº y mayores) | Comandos con salida grande | Observaciones |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | n/a | n/a | n/a (el subagente no ve su consumo) | estimado ~14 | estimado 16 (mayores: diff completo, plan 00-05, `STATUS.md`, `DatabaseStartupShould.cs`) | 1 build para listar advertencias de ficheros tocados (filtrado con grep); `dotnet test` siempre filtrado | Evidencia red tomada de `STATUS.md` (REVIEWER no revierte código). Green re-ejecutado de forma independiente. |
