# Uso - BUILDER (TECHDEBT-002)

| Ciclo | Tokens entrada | Tokens salida | Origen del dato | Llamadas a herramientas | Ficheros leídos (nº y mayores) | Comandos con salida grande | Observaciones |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | ~75k (mayoría caché) | ~14k | estimado | ~40 | ~14 (PlayerFeaturesShould.cs, UpdatePlayerCommandHandlerShould.cs, SwaggerStartup.cs, ExceptionHandlingMiddleware.cs) | `dotnet build` con 26 avisos preexistentes; volcado de esquema OpenAPI con verbosity detailed (filtrado con grep) | Paso 01. Sin Python en el entorno: ediciones con Edit. Coste extra por descubrir que el swagger es 2.0 (nulabilidad implícita). |
