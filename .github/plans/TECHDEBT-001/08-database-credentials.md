# Paso 08 - Credenciales de base de datos fuera del código versionado

Prerrequisito: Q9 resuelta ("Ok." → A + B + E; D y el cambio de contraseña, acciones opcionales del operador) y Q10 resuelta ("Ok." → BUILDER ejecuta el runner solo en `GafferDB_Test`). Depende del paso 03 (paquetes `Microsoft.Extensions.*` finales).

## Objetivo

La contraseña de la BD no aparece en ningún fichero versionado de código ni de configuración. Cada proyecto lee la cadena de conexión de una sola fuente de configuración (User Secrets en local, variables de entorno como override). Los tests con MySQL siguen ejecutándose contra el Docker local sin omitirse.

## Alcance de ficheros

| Fichero | Cambio |
| --- | --- |
| `Gaffer.Api/Gaffer.Api.csproj` | `UserSecretsId`. |
| `Gaffer.Api/appsettings.Development.json` | `DatabaseConfig:ConnectionString` sin contraseña (sin `Pwd` o vacía, según decida BUILDER con la opción E). `Enabled` no cambia. |
| `Gaffer.Api.Migrations/Program.cs`, `Gaffer.Api.Migrations/Gaffer.Api.Migrations.csproj` | Cadenas de conexión desde configuración (User Secrets + variables de entorno + argumentos), no en el código. La BD destino se puede elegir (p. ej. solo `GafferDB_Test`), porque BUILDER solo migra esa BD (Q10 = b). Añadir los paquetes `Microsoft.Extensions.Configuration.*` que hagan falta en 10.0.x (Migrations no usa el framework compartido de ASP.NET Core). |
| `Gaffer.Api.Tests/Gaffer.Api.Tests.csproj`, `Gaffer.Api.Tests/testSettings.json` | `UserSecretsId`; `ConnectionStrings:Database` sin contraseña. |
| `Gaffer.Api.Tests/TestFixture.cs` | La configuración de test añade User Secrets y variables de entorno después de `testSettings.json`; si la cadena final no tiene credenciales, falla en `OneTimeSetUp` con un mensaje que explica cómo configurarla (no omite tests). |
| `Gaffer.Api.Tests/TestShared/TestDefaultValues.cs`, `Gaffer.Api.Tests/SqlTest.cs` | Leer la cadena de la configuración de test (una sola fuente), no de un literal. |
| `Gaffer.Api.Tests/Integration/DatabaseStartupShould.cs`, `Gaffer.Api.Tests/Unit/ConnectionProviders/SqlConnectionProviderShould.cs` | Las cadenas "alcanzable" e "inalcanzable" (puerto 1, `Connection Timeout=2`) se derivan de la cadena de test, cambiando solo el puerto y el timeout. |
| `Gaffer.Api.Tests/Unit/...` o `Integration/...` (nuevo, p. ej. `TestConfigurationShould.cs`) | Test de la precedencia de la configuración. |

## Criterios de aceptación

| ID | Criterio |
| --- | --- |
| AC8.1 | Una búsqueda de `Pwd=` y `Password=` en los ficheros versionados, sin contar `.github/`, `bin/` ni `obj/`, no devuelve contraseñas reales (hoy: 8 apariciones en 7 ficheros). |
| AC8.2 | Con la credencial configurada por User Secrets en `Gaffer.Api.Tests`, `dotnet test gaffer-api.sln` pasa con el mismo recuento, 0 omitidos. |
| AC8.3 | Con la variable `ConnectionStrings__Database` definida, esta tiene prioridad sobre `testSettings.json` y User Secrets (test de precedencia red → green). |
| AC8.4 | Sin credencial (User Secrets y variable no configurados), un test con MySQL **falla** (no se omite) con un mensaje que nombra la clave que falta y cómo configurarla. Se demuestra una vez y se registra en `STATUS.md`; luego se restaura la configuración. |
| AC8.5 | API: con `DatabaseConfig__ConnectionString` definida como variable de entorno, `DatabaseStartup.ConfigureDatabase` recibe esa cadena (test de configuración con `ConfigurationBuilder` que añade JSON + variables de entorno en el mismo orden que el host). No se arregla `Program.SetConfigurationProviders` (fuera de alcance): si impide esta precedencia, BUILDER lo registra como bloqueo. |
| AC8.6 | Migrations: sin cadenas en el código; `dotnet run --project Gaffer.Api.Migrations` con la configuración dada y apuntando solo a `GafferDB_Test` termina con código 0 y no toca `GafferDB` (sin migraciones pendientes antes del paso 09). Sin configuración termina con un error explicativo y código distinto de 0. |
| AC8.7 | `STATUS.md` documenta los comandos exactos para el operador (`dotnet user-secrets set ... --project ...` para los tres proyectos y los nombres de las variables de entorno), sin escribir la contraseña. |
| AC8.8 | Los cambios de configuración de `Gaffer.Api` se reflejan en `TestFixture` en el mismo incremento. |

## Doble bucle

1. **Aceptación**: AC8.4 (red controlado: falla sin credencial con el mensaje esperado) y AC8.2 (green con User Secrets).
2. **Unitario**: tests de precedencia AC8.3 y AC8.5 red → green.
3. **Refactor**: derivar las cadenas inalcanzables de la de test, quitar literales duplicados; AC8.1 + suite completa.

## Cómo demostrarlo

- Búsqueda de AC8.1 antes (8 resultados) y después (0) en `STATUS.md`.
- Salida de AC8.4 (mensaje de fallo, sin contraseña) y de la suite completa con User Secrets.
- Salida y código de salida del runner de Migrations (AC8.6).
- Acción del operador pendiente: configurar sus secretos; opcionalmente crear usuarios no root (opción D) y cambiar la contraseña del contenedor, porque la actual sigue en el historial de Git.
