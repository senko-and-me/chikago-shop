[CmdletBinding()]
param(
    [string]$DotnetPath,
    [string]$GodotPath
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot

function Resolve-Executable {
    param(
        [string]$ExplicitPath,
        [string[]]$CommandNames,
        [string[]]$CandidatePaths,
        [string]$DisplayName
    )

    if ($ExplicitPath) {
        if (-not (Test-Path -LiteralPath $ExplicitPath -PathType Leaf)) {
            throw "$DisplayName not found at explicit path: $ExplicitPath"
        }
        return (Resolve-Path -LiteralPath $ExplicitPath).Path
    }

    foreach ($commandName in $CommandNames) {
        $command = Get-Command $commandName -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($command) { return $command.Source }
    }

    foreach ($candidatePath in $CandidatePaths) {
        if ($candidatePath -and (Test-Path -LiteralPath $candidatePath -PathType Leaf)) {
            return (Resolve-Path -LiteralPath $candidatePath).Path
        }
    }

    throw "$DisplayName executable was not found. Pass its path explicitly."
}

function Invoke-ConsoleStep {
    param(
        [string]$Name,
        [string]$FilePath,
        [string[]]$Arguments
    )

    Write-Host "[RUN ] $Name"
    & $FilePath @Arguments
    $exitCode = $LASTEXITCODE
    if ($exitCode -ne 0) {
        Write-Host "[FAIL] $Name (exit $exitCode)" -ForegroundColor Red
        exit $exitCode
    }
    Write-Host "[PASS] $Name" -ForegroundColor Green
}

function Invoke-GodotStep {
    param(
        [string]$Name,
        [string]$FilePath,
        [string[]]$Arguments
    )

    Write-Host "[RUN ] $Name"
    $process = Start-Process -FilePath $FilePath -ArgumentList $Arguments -WorkingDirectory $projectRoot -Wait -NoNewWindow -PassThru
    if ($process.ExitCode -ne 0) {
        Write-Host "[FAIL] $Name (exit $($process.ExitCode))" -ForegroundColor Red
        exit $process.ExitCode
    }
    Write-Host "[PASS] $Name" -ForegroundColor Green
}

try {
    $dotnetCandidates = @()
    if ($env:DOTNET_ROOT) { $dotnetCandidates += (Join-Path $env:DOTNET_ROOT 'dotnet.exe') }
    if ($env:ProgramFiles) { $dotnetCandidates += (Join-Path $env:ProgramFiles 'dotnet\dotnet.exe') }
    if (${env:ProgramFiles(x86)}) { $dotnetCandidates += (Join-Path ${env:ProgramFiles(x86)} 'dotnet\dotnet.exe') }

    $resolvedDotnet = Resolve-Executable -ExplicitPath $DotnetPath -CommandNames @('dotnet') `
        -CandidatePaths $dotnetCandidates -DisplayName '.NET'

    $godotCandidates = @()
    if ($env:GODOT_PATH) { $godotCandidates += $env:GODOT_PATH }
    $vscodeSettingsPath = Join-Path $projectRoot '.vscode\settings.json'
    if (Test-Path -LiteralPath $vscodeSettingsPath -PathType Leaf) {
        $settings = Get-Content -LiteralPath $vscodeSettingsPath -Raw -Encoding UTF8 | ConvertFrom-Json
        $configuredGodot = $settings.'godotTools.editorPath.godot4'
        if ($configuredGodot) { $godotCandidates += $configuredGodot }
    }

    $resolvedGodot = Resolve-Executable -ExplicitPath $GodotPath -CommandNames @('godot', 'godot4') `
        -CandidatePaths $godotCandidates -DisplayName 'Godot'

    Write-Host "Using .NET: $resolvedDotnet"
    Write-Host "Using Godot: $resolvedGodot"

    Invoke-ConsoleStep -Name 'dotnet build' -FilePath $resolvedDotnet `
        -Arguments @('build', (Join-Path $projectRoot 'ChikagoShop.csproj'))
    Invoke-GodotStep -Name 'Godot import' -FilePath $resolvedGodot `
        -Arguments @('--headless', '--editor', '--path', $projectRoot, '--quit')
    Invoke-GodotStep -Name 'Godot ProjectTest' -FilePath $resolvedGodot `
        -Arguments @('--headless', '--path', $projectRoot, '--script', 'res://tests/ProjectTest.cs')

    Write-Host '[PASS] Verification completed' -ForegroundColor Green
    exit 0
}
catch {
    Write-Host "[FAIL] $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
