# Paso 03 (ciclo 3) - `.editorconfig` de C#, `dotnet format` completo y verificación en CI

Antes paso 04 (`04-csharp-editorconfig.md`); renumerado tras las respuestas del operador. Correspondencia: `AC3.N` = antiguo `AC4.N`, ajustados a Q17 = "Formato completo + CI".

Objetivo: añadir un `.editorconfig` raíz para C#, aplicar **una vez** `dotnet format` completo (whitespace + estilo) a la solución sin `Gaffer.Api.Client`, y añadir al CI la verificación `--verify-no-changes`. Debe convivir con `TreatWarningsAsErrors` en `Gaffer.Api` y con `-warnaserror` del CI: el formato no puede romper el build ni cambiar el comportamiento ni el swagger. Ciclo final pedido por el operador.

**Bloqueo**: ninguno. Q17 resuelta. Q15, Q16 y Q18 con su valor por defecto a): convenciones derivadas del código, reglas de estilo como `suggestion` sin `EnforceCodeStyleInBuild`, exclusión del cliente con `generated_code = true` + `--exclude`.

## Comandos de formato (se fijan una vez y son los mismos en local y en CI)

| Uso | Comando |
| --- | --- |
| Aplicar | `dotnet format whitespace gaffer-api.sln --exclude Gaffer.Api.Client/` y `dotnet format style gaffer-api.sln --severity <S> --exclude Gaffer.Api.Client/` |
| Verificar (local y CI) | Los mismos con `--verify-no-changes` |

- `<S>`: con Q16 a) las reglas son `suggestion`, y `dotnet format style` solo corrige por defecto desde `warning`; para que "estilo" tenga efecto, `<S>` = `info`. BUILDER lo confirma con la ayuda de la herramienta (`dotnet format style --help`) y anota en `STATUS.md` el comando exacto; el CI usa **exactamente** el mismo.
- No se ejecuta `dotnet format analyzers` (no es "whitespace + estilo"; el build ya está a 0 warnings).

## Alcance de ficheros

| Fichero | Cambio |
| --- | --- |
| `.editorconfig` (raíz, nuevo, `root = true`) | Convenciones (Q15 a)), severidades (Q16 a)), sección `[Gaffer.Api.Client/**.cs]` con `generated_code = true` (Q18 a)). |
| `*.cs` de `Gaffer.Api`, `Gaffer.Api.Migrations`, `Gaffer.Api.Tests` | Solo los cambios producidos por los comandos de formato. Ningún cambio manual, salvo los imprescindibles para que el build siga a 0 warnings tras el formato (cada uno anotado en `STATUS.md` con su motivo). |
| `.github/workflows/ci.yml` | Job `build-and-test`: pasos de verificación tras `dotnet restore`/build, antes de los tests. El resto del workflow sin cambios. |

Fuera: cualquier fichero dentro de `Gaffer.Api.Client/`; `Directory.Build.props`/`*.csproj` (sin `EnforceCodeStyleInBuild`, Q16 a)); renombrados de símbolos.

## Criterios de aceptación

| ID | Criterio |
| --- | --- |
| AC3.1 | Inventario (Q15 a)): `STATUS.md` contiene las convenciones con su recuento (un `grep` por convención: namespaces file-scoped frente a bloque, llaves, `var`, prefijo de campos privados, orden de `using`, indentación, finales de línea) y cada regla del `.editorconfig` cita la convención mayoritaria de la que sale. Las convenciones sin mayoría clara quedan sin regla y se listan como pendientes para el operador. |
| AC3.2 | `.editorconfig` en la raíz con `root = true`; `[*.cs]` con `indent_style = space`, `indent_size = 4`, `end_of_line = lf` (coherente con `.gitattributes`), `charset` e `insert_final_newline` según el inventario; reglas de estilo con severidad `suggestion`; sección del cliente con `generated_code = true`. |
| AC3.3 | Línea base antes de formatear (en `STATUS.md`): recuento de la suite, build `--no-incremental` (warnings/errores), build `--no-incremental -warnaserror` y sha256 del volcado de `ServedOpenApiDocumentDump`. |
| AC3.4 | Formato aplicado con los comandos de la tabla; después, ambos comandos con `--verify-no-changes` → exit 0. `git diff --stat` (nº de ficheros y líneas) en `STATUS.md`. |
| AC3.5 | Convivencia con el build tras el formato: `dotnet build gaffer-api.sln --no-incremental -v q` → 0 warnings y 0 errores, y `dotnet build gaffer-api.sln --no-incremental -warnaserror -v q` (equivalente al CI, sin Release) → 0 errores. Mismos recuentos que AC3.3. |
| AC3.6 | Suite completa en verde tras el formato con el **mismo recuento** que en AC3.3 (no hay tests nuevos ni borrados); `TestServerDependenciesShould` en verde. |
| AC3.7 | Swagger (OPT-032 + OPT-034): el formato toca controladores, DTO y `Startups/`, así que se compara el volcado de `ServedOpenApiDocumentDump` de AC3.3 con el de después: **sha256 idéntico**. Si difiere, se deshace la regla responsable y se anota; no se acepta un cambio de contrato en este paso. |
| AC3.8 | Exclusión del cliente: `git status --porcelain Gaffer.Api.Client` vacío tras todos los comandos; la salida de `dotnet format` no lista ficheros de `Gaffer.Api.Client`. |
| AC3.9 | CI: `ci.yml` añade en `build-and-test` los dos comandos de verificación de la tabla (mismo texto que en `STATUS.md`); `git diff .github/workflows/ci.yml` solo muestra esas líneas. La ejecución real en GitHub Actions es acción del operador. |
| AC3.10 | Reproducibilidad (para REVIEWER): en un `git worktree` de `HEAD` (receta de `work-guidelines.md`), copiar solo el `.editorconfig` nuevo, ejecutar los mismos comandos de aplicar y comparar con el árbol de trabajo: sin diferencias en `*.cs` salvo los cambios manuales listados en `STATUS.md`. |

## Doble bucle

Sin comportamiento nuevo: la aceptación es caracterización.

1. **Aceptación**: AC3.3 (línea base) y AC3.1 (inventario).
2. **Unitario**: no aplica (no hay lógica nueva); se registra como "no aplica" en `STATUS.md`.
3. **Refactor**: AC3.2, AC3.4 (formato) y AC3.9; revalidar AC3.5-AC3.8.

## Cómo demostrarlo

- Inventario con los comandos `grep` y recuentos (AC3.1).
- Builds con el filtro de `CLAUDE.md` (OPT-006/OPT-042) y exit code; comandos de formato con exit code.
- `git diff --stat`, `git status --porcelain Gaffer.Api.Client`, sha256 antes/después.
- REVIEWER (OPT-040): abre el diff completo solo de `.editorconfig`, `ci.yml` y los cambios manuales; el resto lo cubre AC3.10 (reproducibilidad), no la lectura línea a línea.

## Notas

- El diff es grande y mecánico. Si el estilo con `<S>` = `info` produce cambios que rompen el build o el swagger y no se resuelven quitando la regla responsable, BUILDER lo registra y el ORCHESTRATOR lo eleva al checkpoint (hay 3 ciclos de reserva).
- Tras este paso, cualquier cambio futuro debe pasar `dotnet format --verify-no-changes` en CI.
- DOCUMENTER (tras `APPROVED`) añade el `.editorconfig` y los comandos de formato a `dev-stack.md` y `doc/features/technical/ci-github-actions.md`.
