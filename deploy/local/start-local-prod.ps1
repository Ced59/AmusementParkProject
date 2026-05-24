param(
    [switch]$Build,
    [switch]$Detached = $true
)

$ErrorActionPreference = 'Stop'
$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$envFile = Join-Path $scriptDirectory '.env.local'
$exampleFile = Join-Path $scriptDirectory '.env.local.example'
$composeFile = Join-Path $scriptDirectory 'compose.local-prod.yml'
$projectName = 'amusementpark-local-prod'

if (-not (Test-Path $envFile)) {
    Copy-Item $exampleFile $envFile
    Write-Host "Created $envFile from example. Review it if needed."
}

$arguments = @('compose', '--project-name', $projectName, '--env-file', $envFile, '-f', $composeFile, 'up')

if ($Detached) {
    $arguments += '-d'
}

if ($Build) {
    $arguments += '--build'
}

& docker @arguments
