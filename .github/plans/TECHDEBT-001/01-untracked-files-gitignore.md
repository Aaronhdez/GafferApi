# Paso 01 - Ficheros que no deberían estar versionados

Prerrequisito: ninguno. Sin preguntas abiertas.

## Objetivo

Que Git ignore los logs y los ficheros de usuario del IDE. BUILDER solo cambia `.gitignore`; sacar los ficheros del índice (`git rm --cached`) es una acción del operador.

## Alcance de ficheros

| Fichero | Cambio |
| --- | --- |
| `.gitignore` | Añadir patrones para `*.log`, `*.user` y `*.DotSettings.user`, sin quitar las entradas existentes. |

Fuera del alcance de BUILDER: borrar los ficheros del disco y cualquier operación de Git.

## Criterios de aceptación

| ID | Criterio |
| --- | --- |
| AC1.1 | `git check-ignore --no-index -v build.log test.log facility-test.log gaffer-api.sln.DotSettings.user` informa de una regla de `.gitignore` para cada uno de los 4 ficheros (hoy no informa de ninguna). |
| AC1.2 | Ningún fichero versionado que no sea `.log`/`.user` pasa a estar ignorado: `git ls-files -ci --exclude-standard` devuelve solo esos 4 ficheros (o ninguno si el operador ya ejecutó `git rm --cached`). |
| AC1.3 | Las entradas actuales de `.gitignore` (`bin/`, `obj/`, `/packages/`, `riderModule.iml`, `/_ReSharper.Caches/`, `.idea`) siguen presentes. |
| AC1.4 | `STATUS.md` registra la acción pendiente del operador: `git rm --cached build.log test.log facility-test.log gaffer-api.sln.DotSettings.user`. |

## Doble bucle

1. **Aceptación**: ejecutar AC1.1 antes del cambio (red: sin coincidencias, código de salida 1) y después (green).
2. **Unitario**: no aplica (no hay código); AC1.2 hace de comprobación de que no se ignora nada más.
3. **Refactor**: no aplica. `dotnet build gaffer-api.sln -v q` sin cambios en errores ni advertencias.

## Cómo demostrarlo

- Salida de `git check-ignore --no-index -v ...` antes y después, y de `git ls-files -ci --exclude-standard`, en `STATUS.md`.
