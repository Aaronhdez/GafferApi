# Usage - BUILDER

| Ciclo | Tokens entrada | Tokens salida | Origen del dato | Llamadas a herramientas | Ficheros leídos (nº y mayores) | Comandos con salida grande | Observaciones |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | ~150 000 | ~25 000 | estimado | ~70 | ~20 (`00-overview` + `01-*` + `STATUS.md` en una salida de 37 KB persistida y leída entera; `FacilityFeaturesShould.cs` por rangos ~500 líneas; `SqlFacilityRepositoryShould.cs` por rangos) | 1 (`cat` de 3 ficheros del plan: 37 KB); 1 comando bloqueado por un `cat > $S.tmp` sin variable (matado; `.tmp` borrado) | Paso 01 (10 puntos): rojos 3 × CS1729, 6 × 500, 3 × CS1503 + 1 × CS0266, cableado 1 × Con error; suite 665 → 688; swagger sha256 idéntico; clase health check 5,3 s → 0,7 s. Script de resumen del rojo reutilizado (OPT-041). |
