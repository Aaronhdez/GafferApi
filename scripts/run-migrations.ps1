<#
.SYNOPSIS
    Ejecuta las migraciones de FluentMigrator (Gaffer.Api.Migrations) de forma no interactiva.

.DESCRIPTION
    Pensado para agentes y CI. No pide ni escribe credenciales: usa las cadenas de conexión ya
    configuradas en User Secrets de Gaffer.Api.Migrations (ver setup-local-environment.ps1) o en
    variables de entorno ConnectionStrings__<Base> (tienen prioridad sobre User Secrets).

    Por defecto solo migra GafferDB_Test. Migrar GafferDB (o todas) exige -AllowNonTestDatabase:
    según el harness (TECHDEBT-001 Q10), los agentes solo migran GafferDB_Test y el operador
    migra GafferDB.

    Códigos de salida:
      0  migraciones aplicadas
      1  fallo aplicando migraciones (conexión, SQL...)
      2  falta la cadena de conexión de la base de datos objetivo
      3  uso no permitido (objetivo no de test sin -AllowNonTestDatabase)

.EXAMPLE
    ./scripts/run-migrations.ps1
    Migra GafferDB_Test (uso habitual de BUILDER/REVIEWER).

.EXAMPLE
    ./scripts/run-migrations.ps1 -Target GafferDB -AllowNonTestDatabase
    Migra GafferDB (solo el operador).

.EXAMPLE
    ./scripts/run-migrations.ps1 -Target All -AllowNonTestDatabase
    Migra todas las bases con cadena de conexión configurada.
#>
[CmdletBinding()]
param(
    [ValidateSet("GafferDB_Test", "GafferDB", "All")]
    [string]$Target = "GafferDB_Test",
    [switch]$AllowNonTestDatabase
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot

if ($Target -ne "GafferDB_Test" -and -not $AllowNonTestDatabase) {
    Write-Error "El objetivo '$Target' no es la base de test. Añade -AllowNonTestDatabase (solo el operador)." -ErrorAction Continue
    exit 3
}

$runnerArgs = @("run", "--project", (Join-Path $repoRoot "Gaffer.Api.Migrations"), "--")
if ($Target -ne "All") {
    $runnerArgs += @("--Target", $Target)
}

Write-Host "==> dotnet $($runnerArgs -join ' ')" -ForegroundColor Cyan
& dotnet @runnerArgs
exit $LASTEXITCODE
