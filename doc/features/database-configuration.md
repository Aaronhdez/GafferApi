# Database Configuration and Credentials

## Overview

`Gaffer.Api`, `Gaffer.Api.Tests`, and `Gaffer.Api.Migrations` no longer ship a database password in versioned configuration files. Each project reads its MySQL connection string(s) from .NET User Secrets (per-machine, local) or environment variables (which take precedence and are suitable for CI); versioned `appsettings.Development.json`/`testSettings.json` keep everything except the password. If a password is missing, the affected component fails immediately with a message explaining how to configure it — it never silently skips database-backed work.

### Who it is for

- Developers setting up the repository on a new machine.
- Operators running `Gaffer.Api.Migrations` against a specific database (development or test).
- CI pipelines that need to inject credentials without touching the repository.

### Use cases

- A developer clones the repository and configures their own MySQL password once via User Secrets, without ever committing it.
- CI sets `ConnectionStrings__Database` (or the equivalent for the API/Migrations) as a pipeline secret/environment variable, which overrides any local file or User Secret.
- An operator runs the migration runner against a single named database (e.g. only `GafferDB_Test`) with `--Target`, instead of migrating every configured database.

### Behaviour

- **`Gaffer.Api`**: `DatabaseConfig:ConnectionString` (User Secrets ID `gaffer-api-3c01a179-00b4-4838-9f7c-d95ec4b117fb`) or the `DatabaseConfig__ConnectionString` environment variable. `appsettings.Development.json` keeps `Uid=root` but no `Pwd=`.
- **`Gaffer.Api.Tests`**: a single connection string key, `ConnectionStrings:Database`, read by `TestConfiguration` from `testSettings.json` → User Secrets (`gaffer-api-tests-9423e01b-aca7-40a3-9649-8746b49b2c6e`) → environment variables, in that precedence order (later sources win). If the resolved string has no `Pwd`/`Password`, every test run fails at `OneTimeSetUp` with an explanatory `InvalidOperationException` instead of running against an unauthenticated connection or skipping.
- **`Gaffer.Api.Migrations`**: any non-empty `ConnectionStrings:<DatabaseName>` entry (User Secrets ID `gaffer-api-migrations-e73209ac-b174-4df6-958e-bd246bbe0cf9`, then environment variables, then command-line arguments) is migrated; `--Target <DatabaseName>` restricts the run to a single database. With nothing configured (or an unknown `--Target`), the runner prints a message naming the missing key and exits with code `2`.

### Limitations

- The password used previously is still present in the Git history; changing the database's actual password is an operator action, not something a checkout of this repository does automatically.
- A clean checkout cannot run MySQL-backed tests or migrations until the operator configures a secret or environment variable once; this is deliberate (AGENTS.md: MySQL-backed tests are never skipped by default).
- `Gaffer.Api.Migrations` creates the target database (`CREATE DATABASE IF NOT EXISTS`) using the configuration key name as the database name, without validating it against the `Database=` value inside the connection string itself.

## See also

- Technical detail and exact commands: [`doc/features/technical/database-configuration.md`](./technical/database-configuration.md).
- Database connection lifecycle at runtime (once a connection string is resolved): [`doc/features/technical/database-connection-lifecycle.md`](./technical/database-connection-lifecycle.md).
