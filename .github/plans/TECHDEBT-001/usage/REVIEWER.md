# Usage - REVIEWER

| Ciclo | Tokens entrada | Tokens salida | Origen del dato | Llamadas a herramientas | Ficheros leídos (nº y mayores) | Comandos con salida grande | Observaciones |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | ~110000 (acumulado de contexto, incluye caché) | ~12000 | estimado | ~28 | ~20; mayores: `00-overview-and-open-questions.md` (26 KB), `STATUS.md`, diff completo de tests (~600 líneas) | `git diff` de producto y de tests (~900 líneas en total); `dotnet list package` | Veredicto NEEDS_CHANGES por paso 10 bloqueado (Q14/Q15); 159/159 reverificado |
| 2 | ~70000 (acumulado de contexto, incluye caché) | ~7000 | estimado | ~15 | ~6; mayores: `STATUS.md` + `REVIEW.md` + pasos 10/11 (salida única de 45 KB) | Build de la solución (filtrada a resumen, OPT-006); `dotnet test` (filtrado) | APPROVED; solo `Directory.Build.props` cambió en producto desde el ciclo 1; contraseña literal redactada en STATUS/REVIEW (O7) |
