<#
.SYNOPSIS
    Prepara un entorno local de gaffer-api: secretos de BD, migraciones y verificación con tests.

.DESCRIPTION
    1. Comprueba el SDK de .NET y, si hay Docker, arranca el contenedor de BD si está parado.
    2. Guarda las cadenas de conexión en User Secrets (nunca en ficheros versionados):
         - Gaffer.Api             -> DatabaseConfig:ConnectionString  (GafferDB)
         - Gaffer.Api.Tests       -> ConnectionStrings:Database       (GafferDB_Test)
         - Gaffer.Api.Migrations  -> ConnectionStrings:GafferDB y ConnectionStrings:GafferDB_Test
    3. Verifica los secretos: existe cada clave, apunta a la base correcta y tiene contraseña
       (no vacía ni el texto de ejemplo "<password>").
    4. Aplica las migraciones sobre GafferDB y GafferDB_Test.
    5. Ejecuta la batería de tests.

    Con -CheckOnly solo ejecuta la verificación del paso 3 (no pide contraseña ni cambia nada)
    y termina con código 1 si algún secreto falta o es incorrecto.

    En CI puede omitirse este script y usar variables de entorno equivalentes:
    DatabaseConfig__ConnectionString, ConnectionStrings__Database,
    ConnectionStrings__GafferDB, ConnectionStrings__GafferDB_Test.

.EXAMPLE
    ./scripts/setup-local-environment.ps1
    Pide la contraseña y usa los valores por defecto (127.0.0.1:3306, usuario root).

.EXAMPLE
    ./scripts/setup-local-environment.ps1 -DbUser gaffer -DbHost 10.0.0.5 -SkipTests

.EXAMPLE
    ./scripts/setup-local-environment.ps1 -CheckOnly
    Solo comprueba que los 4 secretos están bien configurados.
#>
[CmdletBinding()]
param(
    [string]$DbHost = "127.0.0.1",
    [int]$DbPort = 3306,
    [string]$DbUser = "root",
    [SecureString]$DbPassword,
    [string]$ContainerName = "Gaffer.Database",
    [string]$Database = "GafferDB",
    [string]$TestDatabase = "GafferDB_Test",
    [switch]$SkipMigrations,
    [switch]$SkipTests,
    [switch]$CheckOnly
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot

function Invoke-Step([string]$Description, [scriptblock]$Command) {
    Write-Host "==> $Description" -ForegroundColor Cyan
    & $Command
    if ($LASTEXITCODE -ne 0) { throw "Falló: $Description (código $LASTEXITCODE)" }
}

function Get-ConnectionString([string]$DatabaseName, [string]$Password) {
    "Server=$DbHost;Port=$DbPort;Database=$DatabaseName;Uid=$DbUser;Pwd=$Password;"
}

$secrets = @(
    @{ Project = "Gaffer.Api";            Key = "DatabaseConfig:ConnectionString";  Db = $Database },
    @{ Project = "Gaffer.Api.Tests";      Key = "ConnectionStrings:Database";       Db = $TestDatabase },
    @{ Project = "Gaffer.Api.Migrations"; Key = "ConnectionStrings:$Database";      Db = $Database },
    @{ Project = "Gaffer.Api.Migrations"; Key = "ConnectionStrings:$TestDatabase";  Db = $TestDatabase }
)

# Devuelve $true si todos los secretos existen, apuntan a su base y tienen contraseña real.
# Nunca imprime contraseñas.
function Test-Secrets {
    $allOk = $true
    foreach ($group in ($secrets | Group-Object { $_.Project })) {
        $configured = @{}
        foreach ($line in (dotnet user-secrets list --project $group.Name 2>$null)) {
            if ($line -match '^(?<key>[^=]+?) = (?<value>.*)$') { $configured[$Matches.key] = $Matches.value }
        }
        foreach ($secret in $group.Group) {
            $value = $configured[$secret.Key]
            $problem =
                if (-not $value) { "no existe" }
                elseif ($value -notmatch "(?i)Database=$([regex]::Escape($secret.Db))(;|$)") { "no apunta a Database=$($secret.Db)" }
                elseif ($value -notmatch '(?i)Pwd=([^;]+)') { "no tiene contraseña" }
                elseif ($Matches[1] -eq '<password>') { "tiene el texto de ejemplo '<password>'" }
            if ($problem) {
                Write-Host "  [X] $($secret.Project) -> $($secret.Key): $problem" -ForegroundColor Red
                $allOk = $false
            } else {
                Write-Host "  [OK] $($secret.Project) -> $($secret.Key)" -ForegroundColor Green
            }
        }
    }
    return $allOk
}

# --- 1. Prerrequisitos -------------------------------------------------------
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "No se encuentra 'dotnet'. Instala el SDK indicado en global.json."
}
Push-Location $repoRoot
try {
    if ($CheckOnly) {
        Write-Host "==> Verificando secretos" -ForegroundColor Cyan
        if (-not (Test-Secrets)) {
            Write-Host "Hay secretos incorrectos. Ejecuta este script sin -CheckOnly para configurarlos." -ForegroundColor Red
            exit 1
        }
        Write-Host "Secretos correctos." -ForegroundColor Green
        exit 0
    }

    Invoke-Step "Versión del SDK de .NET (según global.json)" { dotnet --version }

    if (Get-Command docker -ErrorAction SilentlyContinue) {
        $state = docker inspect -f "{{.State.Running}}" $ContainerName 2>$null
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "No existe el contenedor '$ContainerName'. Se asume una BD accesible en ${DbHost}:${DbPort}."
            $global:LASTEXITCODE = 0
        } elseif ($state -ne "true") {
            Invoke-Step "Arrancando contenedor $ContainerName" { docker start $ContainerName | Out-Null }
        } else {
            Write-Host "==> Contenedor $ContainerName en ejecución" -ForegroundColor Cyan
        }
    } else {
        Write-Warning "Docker no está disponible. Se asume una BD accesible en ${DbHost}:${DbPort}."
    }

    # --- 2. Secretos ---------------------------------------------------------
    if (-not $DbPassword) {
        $DbPassword = Read-Host "Contraseña de MySQL para '$DbUser'" -AsSecureString
    }
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($DbPassword)
    try {
        $plainPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
    } finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }

    foreach ($secret in $secrets) {
        $value = Get-ConnectionString $secret.Db $plainPassword
        Invoke-Step "User Secret $($secret.Key) en $($secret.Project)" {
            dotnet user-secrets set $secret.Key $value --project $secret.Project | Out-Null
        }
    }
    $plainPassword = $null

    Write-Host "==> Verificando secretos" -ForegroundColor Cyan
    if (-not (Test-Secrets)) { throw "Los secretos no quedaron bien configurados." }

    # --- 3. Migraciones ------------------------------------------------------
    if (-not $SkipMigrations) {
        Invoke-Step "Migraciones en $Database y $TestDatabase" {
            dotnet run --project Gaffer.Api.Migrations
        }
    }

    # --- 4. Verificación -----------------------------------------------------
    if (-not $SkipTests) {
        Invoke-Step "Tests (MySQL incluido, sin omitir)" {
            dotnet test gaffer-api.sln --nologo -v q
        }
    }

    Write-Host "Entorno listo." -ForegroundColor Green
} finally {
    Pop-Location
}
