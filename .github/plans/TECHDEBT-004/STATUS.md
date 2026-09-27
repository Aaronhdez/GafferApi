# STATUS - TECHDEBT-004

| Campo | Valor |
| --- | --- |
| Estado | PLANNED |
| Ciclo | 0 |
| Pasos completados | Ninguno |
| Pasos bloqueados | 02 hasta confirmar Q3; 04 hasta Q9, Q10, Q12; 05 hasta Q17, Q18, Q19 (ver `00-overview-and-open-questions.md`) |
| Siguiente | Aprobación del plan por el operador |
| Bloqueos | Ninguno para el ciclo 1 (salvo que Q8 = c), que obliga a renumerar antes de empezar) |

## Secuenciación (un paso = un ciclo)

En el ciclo N, BUILDER implementa solo el paso N (más las correcciones de REVIEWER del ciclo anterior, si las hay) y REVIEWER solo aprueba el paso de ese ciclo. El ciclo 6 es la única reserva para `NEEDS_CHANGES`; cómo se combina una corrección con el paso siguiente es Q0.

| Ciclo | Paso previsto | Veredicto |
| --- | --- | --- |
| 1 | 01 - TreatWarningsAsErrors (`Gaffer.Api`) + `.gitattributes` + cierre de conexión con `try/finally` | - |
| 2 | 02 - Entorno desde variable de entorno (`Development` por defecto) | - |
| 3 | 03 - Migración a `MySqlConnector` + cancelación en curso | - |
| 4 | 04 - CRUD de gradas + lock en transacción | - |
| 5 | 05 - CI con GitHub Actions | - |
| 6 | Reserva | - |

## Tiempos por agente

| Agente | Ciclo | Inicio (UTC) | Fin (UTC) |
| --- | --- | --- | --- |
| PLANNER | 0 | n/a | n/a |

## Prerrequisitos

| Comprobación | Resultado |
| --- | --- |
| Línea base (dato del operador, 2026-09-27, `main`) | `dotnet build --no-incremental`: 0 errores, 0 warnings. `dotnet test`: 269/269 contra Docker `Gaffer.Database` (MariaDB 12.3.2). BUILDER la revalida al inicio del ciclo 1. |

## Comandos y resultados

| Ciclo | Paso | Fase | Comando / filtro | Resultado |
| --- | --- | --- | --- | --- |

## Documentación modificada

Pendiente (DOCUMENTER, tras `APPROVED` y aprobación del operador). Incluye `dev-stack.md`/`dev-stack-documentation.md` por el cambio de driver (Q7).
