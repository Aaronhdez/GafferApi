# Uso - BUILDER

| Ciclo | Tokens entrada | Tokens salida | Origen del dato | Llamadas a herramientas | Ficheros leídos (nº y mayores) | Comandos con salida grande | Observaciones |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | n/a | n/a | n/a (el subagente no ve su consumo) | estimado ~70 | estimado ~25 (mayores: `SqlFacilityRepository.cs`, `ExceptionToHttpResponseConverters.cs`, `SqlPlayerRepositoryShould.cs`, `TestFixture.cs`, plan 00-05) | 1 build sin filtrar (warnings CS+NU); el resto de `dotnet test` filtrado con grep | La consola de .NET está en español ("Correctas", "Con error") y un primer grep en inglés perdió una ejecución red que hubo que repetir. El error del analizador NUnit1032 obligó a una desviación de alcance mínima. No hay Python en el entorno. |
