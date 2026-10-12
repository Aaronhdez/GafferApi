# Paso 03 (ciclo 3) - `.editorconfig` de C#, reformateo completo y verificación en CI

Antes paso 04 (`04-csharp-editorconfig.md`); renumerado tras las respuestas del operador. El contenido del `.editorconfig` lo fijó el operador por bloques el 2026-10-12 ("Para configurar el editroconfig, prefiero hacerlo paso a paso" → "Te pregunto por bloques"; decisiones literales en `00`, Q15-Q18). Sustituye a la versión anterior de este paso, que derivaba las convenciones del código.

Objetivo: añadir el `.editorconfig` raíz decidido por el operador, aplicarlo una vez a `Gaffer.Api`, `Gaffer.Api.Tests` y `Gaffer.Api.Migrations` (formato, estilo y renombrado de campos privados) y añadir al CI la verificación `dotnet format --verify-no-changes --severity info`. Sin cambio de comportamiento ni de contrato; debe convivir con `TreatWarningsAsErrors` en `Gaffer.Api` y con `-warnaserror` en el CI.

**Bloqueo**: ninguno por preguntas. Q19 (consolidada, `00`) no bloquea: sin respuesta, se aplican sus opciones (i) a) y (ii) a), que ya figuran en el contenido de abajo.

## Contenido exacto del `.editorconfig` (raíz)

Cada bloque cita la decisión del operador. Las líneas marcadas `Q19` dependen de la pregunta consolidada; las demás son literales de las decisiones.

```ini
root = true

# Bloque 1 - Espaciado "Sí, tal cual" + charset "utf-8 sin BOM"
[*.{cs,json,yml,yaml,md}]
charset = utf-8
end_of_line = lf
insert_final_newline = true
trim_trailing_whitespace = true
indent_style = space

[*.{json,yml,yaml,md}]
indent_size = 2

[*.cs]
indent_size = 4
# Sin límite de longitud de línea: no se define ninguna opción de longitud.

# Bloque 1 - Llaves "K&R": llave al final de línea en todo
csharp_new_line_before_open_brace = none
csharp_new_line_before_else = false
csharp_new_line_before_catch = false
csharp_new_line_before_finally = false

# Bloque 1 - Namespaces "File-scoped"
csharp_style_namespace_declarations = file_scoped

# Bloque 2 - "var siempre"
csharp_style_var_for_built_in_types = true
csharp_style_var_when_type_is_apparent = true
csharp_style_var_elsewhere = true

# Bloque 2 - "Permitir en una línea"
csharp_prefer_braces = when_multiline

# Bloque 2 - using "Fuera, System primero", sin línea en blanco entre grupos
csharp_using_directive_placement = outside_namespace
dotnet_sort_system_directives_first = true
dotnet_separate_import_directive_groups = false

# Bloque 2 - C# moderno: solo "Primary constructors" y "Expression-bodied" (cuando caben en una línea)
csharp_style_prefer_primary_constructors = true
csharp_style_expression_bodied_methods = when_on_single_line
csharp_style_expression_bodied_constructors = when_on_single_line
csharp_style_expression_bodied_operators = when_on_single_line
csharp_style_expression_bodied_properties = when_on_single_line
csharp_style_expression_bodied_indexers = when_on_single_line
csharp_style_expression_bodied_accessors = when_on_single_line
csharp_style_expression_bodied_lambdas = when_on_single_line
csharp_style_expression_bodied_local_functions = when_on_single_line

# Bloque 4 - "suggestion + CI format": solo las reglas decididas, como suggestion (Q19 (i) a): lista blanca)
dotnet_analyzer_diagnostic.category-Style.severity = none
dotnet_diagnostic.IDE0055.severity = suggestion   # formato (llaves, espaciado, orden de using)
dotnet_diagnostic.IDE0161.severity = suggestion   # namespace file-scoped
dotnet_diagnostic.IDE0007.severity = suggestion   # var
dotnet_diagnostic.IDE0011.severity = suggestion   # llaves
dotnet_diagnostic.IDE0065.severity = suggestion   # posición de using
dotnet_diagnostic.IDE0290.severity = suggestion   # primary constructors
dotnet_diagnostic.IDE0021.severity = suggestion   # expression-bodied: constructores
dotnet_diagnostic.IDE0022.severity = suggestion   # métodos
dotnet_diagnostic.IDE0023.severity = suggestion   # operadores de conversión
dotnet_diagnostic.IDE0024.severity = suggestion   # operadores
dotnet_diagnostic.IDE0025.severity = suggestion   # propiedades
dotnet_diagnostic.IDE0026.severity = suggestion   # indexadores
dotnet_diagnostic.IDE0027.severity = suggestion   # accesores
dotnet_diagnostic.IDE0053.severity = suggestion   # lambdas
dotnet_diagnostic.IDE0061.severity = suggestion   # funciones locales
dotnet_diagnostic.IDE1006.severity = suggestion   # nombres
# Collection expressions y pattern matching/null "no se activan": quedan en none por la línea de categoría
# (no se fija la preferencia contraria, para que dotnet format no deshaga los usos existentes).

# Bloque 3 - Nombres
dotnet_naming_style.pascal_case.capitalization = pascal_case
dotnet_naming_style.camel_case.capitalization = camel_case
dotnet_naming_style.interface_prefix.capitalization = pascal_case
dotnet_naming_style.interface_prefix.required_prefix = I

dotnet_naming_symbols.interfaces.applicable_kinds = interface
dotnet_naming_symbols.constants.applicable_kinds = field
dotnet_naming_symbols.constants.required_modifiers = const
dotnet_naming_symbols.static_readonly_fields.applicable_kinds = field
dotnet_naming_symbols.static_readonly_fields.required_modifiers = static, readonly
dotnet_naming_symbols.private_fields.applicable_kinds = field
dotnet_naming_symbols.private_fields.applicable_accessibilities = private
dotnet_naming_symbols.types_and_members.applicable_kinds = class, struct, enum, delegate, method, property, event, field
dotnet_naming_symbols.parameters_and_locals.applicable_kinds = parameter, local

dotnet_naming_rule.interfaces_begin_with_i.symbols = interfaces
dotnet_naming_rule.interfaces_begin_with_i.style = interface_prefix
dotnet_naming_rule.interfaces_begin_with_i.severity = suggestion
dotnet_naming_rule.constants_pascal.symbols = constants
dotnet_naming_rule.constants_pascal.style = pascal_case
dotnet_naming_rule.constants_pascal.severity = suggestion
dotnet_naming_rule.static_readonly_pascal.symbols = static_readonly_fields
dotnet_naming_rule.static_readonly_pascal.style = pascal_case
dotnet_naming_rule.static_readonly_pascal.severity = suggestion
dotnet_naming_rule.private_fields_camel.symbols = private_fields
dotnet_naming_rule.private_fields_camel.style = camel_case
dotnet_naming_rule.private_fields_camel.severity = suggestion
dotnet_naming_rule.parameters_and_locals_camel.symbols = parameters_and_locals
dotnet_naming_rule.parameters_and_locals_camel.style = camel_case
dotnet_naming_rule.parameters_and_locals_camel.severity = suggestion
dotnet_naming_rule.types_and_members_pascal.symbols = types_and_members
dotnet_naming_rule.types_and_members_pascal.style = pascal_case
dotnet_naming_rule.types_and_members_pascal.severity = suggestion
# Async: "Sin regla" (no hay regla de sufijo Async).

# Bloque 4 - Alcance "Todos menos el cliente"
[Gaffer.Api.Client/**.cs]
generated_code = true
```

- Prioridad de nombres: Roslyn ordena las reglas por especificidad (modificadores `const`/`static readonly` antes que `private`), así que un `private const` o un `private static readonly` queda en PascalCase y un `private readonly` de instancia en camelCase. BUILDER lo confirma en AC3.6 (ningún `const` ni `static readonly` renombrado a camelCase).
- Fuera del `.editorconfig`: `.sln`, `.csproj`, `.props` y `.ps1` (no se decidieron y no los toca `dotnet format`).
- BUILDER puede corregir la **sintaxis** de una línea si la herramienta la rechaza (registrándolo en `STATUS.md`), pero no cambiar ninguna decisión.

## Comandos (los mismos en local y en CI)

| Uso | Comando |
| --- | --- |
| Aplicar | `dotnet format gaffer-api.sln --severity info --exclude Gaffer.Api.Client/` (whitespace + style + analyzers, según Bloque 4) |
| Verificar (local y CI) | `dotnet format gaffer-api.sln --verify-no-changes --severity info --exclude Gaffer.Api.Client/` |

- Con Q19 (i) a), si el subcomando `analyzers` aplica correcciones de reglas `CA*` de nivel info no decididas, BUILDER las lista en `STATUS.md` (regla, nº de ficheros) y las desactiva una a una (`dotnet_diagnostic.CAxxxx.severity = none`) antes de aplicar, para que el diff solo contenga lo decidido.
- `dotnet format` no aplica de forma fiable los renombrados de IDE1006: los 71 campos `_camelCase` se renombran a mano (ver orden de trabajo).

## Orden de trabajo

1. Línea base (AC3.1) **antes de tocar nada**, incluido el volcado del swagger.
2. Crear `.editorconfig` y aplicar el comando de formato (whitespace, K&R, file-scoped, `var`, using, primary constructors, expression-bodied, charset sin BOM).
3. Renombrar los campos privados `_camelCase` que queden, guiado por `grep` (OPT-039): por cada campo, `grep -rn "\b_<nombre>\b"` en los tres proyectos, renombrar declaración y usos en el mismo fichero y usar `this.` donde el nuevo nombre coincida con un parámetro o local (p. ej. constructores clásicos `this.logger = logger;`). **Cadenas por reflexión**: `Unit/ConnectionProviders/SqlConnectionProviderShould.cs` L424 usa `GetField("_sqlConnection", ...)`; se actualiza la cadena junto con el campo (el compilador no lo detecta).
4. Volver a aplicar el formato y ejecutar la verificación hasta exit 0.
5. Añadir el paso del CI y revalidar (AC3.4-AC3.10).

## Alcance de ficheros

| Fichero | Cambio |
| --- | --- |
| `.editorconfig` (raíz, nuevo) | Contenido de arriba. |
| `*.cs` de `Gaffer.Api`, `Gaffer.Api.Tests`, `Gaffer.Api.Migrations` | Cambios producidos por `dotnet format` y renombrado de campos privados (con `this.` donde haga falta y la cadena de reflexión de L424). Ningún otro cambio manual, salvo los imprescindibles para mantener el build a 0 warnings (cada uno en `STATUS.md` con su motivo). |
| `.github/workflows/ci.yml` | Job `build-and-test`: un paso con el comando de verificación, tras `dotnet restore`/build y antes de los tests. El resto del workflow sin cambios. |

Fuera: cualquier fichero dentro de `Gaffer.Api.Client/`; `Directory.Build.props`/`*.csproj` (sin `EnforceCodeStyleInBuild`); renombrados distintos de los campos privados; cambios de lógica.

## Criterios de aceptación

| ID | Criterio |
| --- | --- |
| AC3.1 | Línea base en `STATUS.md` antes de editar: suite (697 tests en verde), build `--no-incremental` (0 warnings), build `--no-incremental -warnaserror` (0 errores), sha256 del volcado de `ServedOpenApiDocumentDump`, nº de ficheros `*.cs` con BOM y nº de campos privados `_camelCase` (comandos de AC3.6 y AC3.7) en los tres proyectos. |
| AC3.2 | `.editorconfig` en la raíz con el contenido de este paso (diff del fichero contra el bloque de arriba vacío, salvo correcciones de sintaxis anotadas). |
| AC3.3 | `dotnet format gaffer-api.sln --verify-no-changes --severity info --exclude Gaffer.Api.Client/` → **exit 0** tras el reformateo. |
| AC3.4 | Build tras el reformateo: `dotnet build gaffer-api.sln --no-incremental -v q` → 0 warnings y 0 errores, y `dotnet build gaffer-api.sln --no-incremental -warnaserror -v q` → 0 errores (filtro de `CLAUDE.md`). Sin `CS9124`/`CS9107` (parámetros de primary constructor capturados y usados para inicializar campos) ni `CS1717` (autoasignación tras renombrar); si aparecen, se resuelven según Q19 (ii). |
| AC3.5 | Suite completa en verde con **697 tests**, el mismo recuento que AC3.1 (ningún test nuevo ni borrado); `TestServerDependenciesShould` y `SqlConnectionProviderShould` (reflexión de L424) en verde. |
| AC3.6 | Nombres: `grep -rnE "private\s+(readonly\s+)?[A-Za-z0-9_<>,?\[\] ]+\s+_[a-z]\w*\s*[;=]" Gaffer.Api Gaffer.Api.Tests Gaffer.Api.Migrations --include=*.cs` → 0 y `grep -rnE "\b_[a-z][A-Za-z0-9]*\b" ... --include=*.cs` → 0 en las tres capas (antes: 71 campos); ningún `const` ni `static readonly` pasa a camelCase (`grep` de ambos modificadores, recuento antes/después igual). |
| AC3.7 | Formato observable: `grep -rlI $'^\xEF\xBB\xBF' Gaffer.Api Gaffer.Api.Tests Gaffer.Api.Migrations --include=*.cs \| wc -l` → 0 (antes ~40); `grep -rnE "^namespace [A-Za-z0-9_.]+\s*$" ... --include=*.cs` → 0 (antes 3 de bloque). K&R, `var`, using y expression-bodied los verifica AC3.3. |
| AC3.8 | Swagger (OPT-032 + OPT-034): el reformateo toca controladores, DTO y `Startups/`, así que se compara el volcado de `ServedOpenApiDocumentDump` de AC3.1 con el de después: **sha256 idéntico**. Si difiere, se deshace la causa y se anota; no se acepta ningún cambio de contrato en este paso. |
| AC3.9 | Migraciones "Sí, solo formato": `DatabaseSchemaShould` en verde; `pwsh ./scripts/run-migrations.ps1` contra `GafferDB_Test` → exit 0; los atributos `[Migration(...)]` de M0001-M0003 son idénticos antes/después (`grep -rn "\[Migration(" Gaffer.Api.Migrations`); `git diff --word-diff -w` de M0001-M0003 no muestra cambios dentro de literales de cadena (SQL) ni en nombres de tabla, columna, FK o trigger. |
| AC3.10 | Cliente excluido: `git status --porcelain Gaffer.Api.Client` vacío tras todos los comandos y la salida de `dotnet format` no lista ficheros de `Gaffer.Api.Client`. |
| AC3.11 | CI: `ci.yml` añade en `build-and-test` el paso `dotnet format gaffer-api.sln --verify-no-changes --severity info --exclude Gaffer.Api.Client/` (mismo texto que AC3.3); `git diff .github/workflows/ci.yml` solo muestra esas líneas. La ejecución en GitHub Actions es acción del operador. |
| AC3.12 | Reproducibilidad (para REVIEWER): en un `git worktree` de `HEAD` (receta de `work-guidelines.md`) se copian solo el `.editorconfig` y los renombrados manuales listados en `STATUS.md`, se ejecuta el comando de aplicar y el resultado coincide con el árbol de trabajo (`git diff --no-index --stat` vacío en `*.cs`). |

## Doble bucle

Sin comportamiento nuevo: la aceptación es caracterización.

1. **Aceptación**: AC3.1 (línea base); la verificación de AC3.3 en rojo antes de reformatear (exit ≠ 0, nº de ficheros a cambiar) es el rojo del paso.
2. **Unitario**: no aplica (no hay lógica nueva); se registra como "no aplica" en `STATUS.md`.
3. **Refactor**: orden de trabajo 2-5; revalidar AC3.3-AC3.12.

## Cómo demostrarlo

- Comandos de AC3.1, AC3.6, AC3.7 y AC3.9 con recuentos antes/después en `STATUS.md`.
- Builds con el filtro de `CLAUDE.md` (OPT-006/OPT-042) y exit code; `dotnet format` con exit code.
- Lista de renombrados manuales (campo antiguo → nuevo, fichero, usos) y de cambios manuales para el build.
- REVIEWER (OPT-040): abre completos solo `.editorconfig`, `ci.yml`, M0001-M0003 y los cambios manuales; el resto lo cubren AC3.3 y AC3.12, no la lectura línea a línea (el diff de K&R afecta a casi todo el código).

## Recomendación para el operador (commits)

- Hacer el commit del reformateo **separado de cualquier cambio funcional** (idealmente: un commit con `.editorconfig` + reformateo + renombrados, y otro con el paso del CI), y no mezclarlo con correcciones de otros ciclos. Opcional: añadir ese commit a un `.git-blame-ignore-revs` para que `git blame` lo salte (acción del operador; no está en el alcance de los agentes).

## Notas

- El diff es grande (K&R y charset afectan a casi todos los ficheros). Si una regla decidida rompe el build o el swagger y no se resuelve sin cambiar la decisión, BUILDER lo registra y el ORCHESTRATOR lo eleva al checkpoint (quedan 3 ciclos de reserva: 4-6).
- DOCUMENTER (tras `APPROVED`) añade el `.editorconfig`, las convenciones y el paso de `dotnet format` del CI a `dev-stack.md` y `doc/features/technical/ci-github-actions.md`.
