# Uso - BUILDER (TECHDEBT-003)

| Ciclo | Tokens entrada | Tokens salida | Origen del dato | Llamadas a herramientas | Ficheros leídos (nº y mayores) | Comandos con salida grande | Observaciones |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | ~60k (mayoría caché) | ~12k | estimado | ~30 | ~8 (00-overview-and-open-questions.md, SqlConnectionProvider.cs, SqlConnectionProviderShould.cs, DatabaseStartupShould.cs) | `dotnet build --no-incremental` (x4, filtrado con grep); `dotnet test` suite completa (x3, `--verbosity quiet`) | Paso 01. Coste extra: sonda en scratchpad para descubrir que `MySqlConnection.DisposeAsync` (MySql.Data 26.7) no lanza `Component.Disposed`. Sin Python ni pwsh en Git Bash: ediciones con sed/Edit (cuidado con CRLF). |
