# Paso 05 (ciclo 5) - Notas de stack: OPT-010, OPT-012, OPT-022

Decisión del operador (literal): "las de docs de stack (.github/doc/common) van al plan".

**Bloqueo**: QH3 (qué rol las aplica; ningún rol tiene permiso claro según `AGENTS.md`). **Dependencia**: OPT-012 necesita la evidencia de `TECHDEBT-004` AC3.6; orden según QH4 (recomendación: tras `TECHDEBT-004` `DONE`).

## Objetivo

Registrar como nota reutilizable en la documentación de stack tres hechos que costaron sondeos en caliente.

## Ficheros, secciones y texto propuesto

### OPT-010 - `.github/doc/common/dev-stack.md`, `## Frameworks and Core Components`, sub-viñeta de "OpenAPI/Swagger via NSwag"

```markdown
  - Nullability note (OPT-010): NJsonSchema/NSwag treats the Swagger 2.0 document served by this project without `x-nullable`, inferring a property as nullable when it is not listed in `required` (see `OpenApiDocument.FromJsonAsync` + `JsonSchemaProperty.IsNullable(document.SchemaType)`). Contract tests can assert nullability from `required` without dumping the schema with verbose output. Source: `.github/plans/TECHDEBT-002/`, cycle 1.
```

### OPT-012 - `.github/doc/common/test-stack.md`, `## Testing Strategy Notes` (texto a completar con la evidencia)

La propuesta aprobada nombra `MySql.Data.MySqlClient.MySqlException`, que deja de ser cierta tras `TECHDEBT-004` paso 03. Texto con el hueco que rellena quien aplique la nota a partir de `TECHDEBT-004/STATUS.md` AC3.6:

```markdown
- Driver failures (connection or query) surface as `<tipo observado en TECHDEBT-004 AC3.6, p. ej. MySqlConnector.MySqlException>`; use that type in `ThrowExactlyAsync<T>` assertions for repository failures instead of probing with ad hoc scripts. Source: `.github/plans/TECHDEBT-004/STATUS.md`, cycle 3 (OPT-012).
```

No se inventa el tipo: si AC3.6 no lo registra, la nota no se aplica y se anota.

### OPT-022 - `.github/doc/common/test-stack.md`, `## Testing Strategy Notes`

```markdown
- To start a full test server and fetch the routes it actually serves (e.g. `/swagger/*/swagger.json`), use `Gaffer.Api.Tests/TestShared/StartupTestServer.cs`, not the internal `TestServer` of `TestFixture`, which returns 404 for the Swagger routes. Source: `.github/plans/TECHDEBT-003/`, cycle 5 (OPT-022).
```

(La OPT admite `test-stack-documentation.md` o `test-stack.md`; se recomienda `test-stack.md`, porque `*-documentation.md` es la tabla de paquetes y URLs oficiales.)

## Criterios de aceptación

| ID | Criterio |
| --- | --- |
| ACH5.1 | `grep -n "OPT-010" .github/doc/common/dev-stack.md` → 1 línea bajo la mención de NSwag. |
| ACH5.2 | `grep -n "OPT-012" .github/doc/common/test-stack.md` → 1 línea con el tipo de excepción de `TECHDEBT-004` AC3.6 (no `MySql.Data`). |
| ACH5.3 | `grep -n "OPT-022" .github/doc/common/test-stack.md` → 1 línea. |
| ACH5.4 | No duplica ni contradice lo que DOCUMENTER de TECHDEBT-004 haya escrito sobre el driver (lectura dirigida de `dev-stack.md` L20-30 y `test-stack.md`); `git diff --stat` solo con estas líneas. |
| ACH5.5 | Lo aplica el rol que fije QH3 dentro de su permiso (o el operador). |

## Cómo demostrarlo

- `grep` antes/después en `STATUS.md`; `git diff --stat .github/doc/common/`.
