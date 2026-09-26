# STATUS - TECHDEBT-001

| Campo | Valor |
| --- | --- |
| Estado | PLANNED |
| Ciclo | 0 |
| Pasos completados | Ninguno |
| Bloqueos | Pendiente de aprobación humana del plan. Q1-Q12 resueltas; Q13-Q15 abiertas (solo bloquean el paso 10 y la acción de cierre). |

## Tiempos por agente

| Agente | Ciclo | Inicio (UTC) | Fin (UTC) |
| --- | --- | --- | --- |
| PLANNER | 0 | n/a | n/a |

## Prerrequisitos

Pendiente (BUILDER, ciclo 1): Docker `Gaffer.Database`, `dotnet --version`, línea base de build (errores/advertencias) y de tests (127 esperados).

## Comandos y resultados

| Ciclo | Paso | Fase | Comando / filtro | Resultado |
| --- | --- | --- | --- | --- |

## Acciones pendientes del operador

| Paso | Acción | Estado |
| --- | --- | --- |
| 01 | `git rm --cached build.log test.log facility-test.log gaffer-api.sln.DotSettings.user` | Pendiente |
| 08 | Configurar User Secrets o variables de entorno (comandos que documente BUILDER); opcional: usuario no root y cambio de contraseña del contenedor | Pendiente |
| 09 | Ejecutar el runner de migraciones en `GafferDB` | Pendiente |
| Cierre (tras aprobarse 05, 06 y 10) | Regenerar `Gaffer.Api.Client`, versionarlo como 1.0.0 y publicarlo según Q13 (el contrato cambia: 201 en Create y enums como texto) | Pendiente |
| Plan | Borrar `10-final-refactor-and-regression.md` (obsoleto, sustituido por el 11) | Pendiente |

## Documentación modificada

Pendiente (DOCUMENTER).
