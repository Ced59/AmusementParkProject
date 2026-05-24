param(
    [switch]$Build,
    [switch]$Detached = $true,
    [switch]$SkipPortCheck
)

$ErrorActionPreference = 'Stop'
$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$envFile = Join-Path $scriptDirectory '.env.local'
$exampleFile = Join-Path $scriptDirectory '.env.local.example'
$composeFile = Join-Path $scriptDirectory 'compose.local-prod.yml'
$projectName = 'amusementpark-local-prod'

function Update-LegacyDefaultLocalEnvironmentValues {
    param([string]$Path)

    $content = Get-Content -Raw -Path $Path
    $updated = $content

    $replacements = [ordered]@{
        'PUBLIC_BASE_URL=http://amusement.localhost:8080' = 'PUBLIC_BASE_URL=http://amusement.localhost:18080'
        'NPM_HTTP_PORT=8080' = 'NPM_HTTP_PORT=18080'
        'NPM_ADMIN_PORT=8181' = 'NPM_ADMIN_PORT=18181'
        'NPM_HTTPS_PORT=8443' = 'NPM_HTTPS_PORT=18443'
        'SSR_DIRECT_PORT=4000' = 'SSR_DIRECT_PORT=14000'
        'MONGO_PORT=27017' = 'MONGO_PORT=27018'
        'MINIO_API_PORT=9000' = 'MINIO_API_PORT=19000'
        'MINIO_CONSOLE_PORT=9001' = 'MINIO_CONSOLE_PORT=19001'
        'MATOMO_HTTP_PORT=8080' = 'MATOMO_HTTP_PORT=18082'
        '# GOOGLE_REDIRECT_URI=http://amusement.localhost:8080/api/auth/external/google/callback' = '# GOOGLE_REDIRECT_URI=http://amusement.localhost:18080/api/auth/external/google/callback'
    }

    foreach ($entry in $replacements.GetEnumerator()) {
        $updated = $updated.Replace($entry.Key, $entry.Value)
    }

    if ($updated -notmatch '(?m)^MATOMO_HTTP_PORT=') {
        $updated = $updated.TrimEnd() + [Environment]::NewLine + 'MATOMO_HTTP_PORT=18082' + [Environment]::NewLine
    }

    if ($updated -ne $content) {
        Set-Content -Path $Path -Value $updated -NoNewline
        Write-Host "Updated legacy local-prod default ports in $Path to avoid common dev port conflicts."
    }
}

function Get-EnvValue {
    param(
        [string]$Path,
        [string]$Name,
        [string]$DefaultValue
    )

    $pattern = '^' + [regex]::Escape($Name) + '=(.*)$'
    foreach ($line in Get-Content -Path $Path) {
        if ($line -match $pattern) {
            return $Matches[1].Trim()
        }
    }

    return $DefaultValue
}

function Test-PortAvailableOnLoopback {
    param([int]$Port)

    $listener = $null
    try {
        $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Parse('127.0.0.1'), $Port)
        $listener.Start()
        return $true
    }
    catch {
        return $false
    }
    finally {
        if ($null -ne $listener) {
            $listener.Stop()
        }
    }
}

function Test-PortPublishedByCurrentComposeProject {
    param(
        [int]$Port,
        [string[]]$PublishedPorts
    )

    $escapedPort = [regex]::Escape($Port.ToString())
    $pattern = "(127\.0\.0\.1|0\.0\.0\.0|\[::\]):$escapedPort->"
    foreach ($publishedPort in $PublishedPorts) {
        if ($publishedPort -match $pattern) {
            return $true
        }
    }

    return $false
}

function Assert-LocalPortsAreAvailable {
    param([string]$Path)

    $portChecks = @(
        @{ Name = 'NPM_HTTP_PORT'; Label = 'Nginx Proxy Manager HTTP entrypoint'; Default = '18080' },
        @{ Name = 'NPM_ADMIN_PORT'; Label = 'Nginx Proxy Manager admin UI'; Default = '18181' },
        @{ Name = 'NPM_HTTPS_PORT'; Label = 'Nginx Proxy Manager HTTPS entrypoint'; Default = '18443' },
        @{ Name = 'SSR_DIRECT_PORT'; Label = 'Angular SSR direct debug port'; Default = '14000' },
        @{ Name = 'MONGO_PORT'; Label = 'MongoDB optional local inspection port'; Default = '27018' },
        @{ Name = 'MINIO_API_PORT'; Label = 'MinIO optional local API port'; Default = '19000' },
        @{ Name = 'MINIO_CONSOLE_PORT'; Label = 'MinIO console port'; Default = '19001' },
        @{ Name = 'MATOMO_HTTP_PORT'; Label = 'Matomo direct local UI/tracker port'; Default = '18082' }
    )

    $projectPorts = @()
    try {
        $projectPorts = & docker ps --filter "label=com.docker.compose.project=$projectName" --format '{{.Ports}}' 2>$null
    }
    catch {
        $projectPorts = @()
    }

    $conflicts = New-Object System.Collections.Generic.List[string]

    foreach ($portCheck in $portChecks) {
        $rawValue = Get-EnvValue -Path $Path -Name $portCheck.Name -DefaultValue $portCheck.Default
        $port = 0
        if (-not [int]::TryParse($rawValue, [ref]$port)) {
            throw "Invalid $($portCheck.Name) value '$rawValue' in $Path. It must be a TCP port number."
        }

        if (-not (Test-PortAvailableOnLoopback -Port $port) -and -not (Test-PortPublishedByCurrentComposeProject -Port $port -PublishedPorts $projectPorts)) {
            $conflicts.Add("- $($portCheck.Name)=$port ($($portCheck.Label))")
        }
    }

    if ($conflicts.Count -gt 0) {
        $message = @(
            'One or more local-prod ports are already used by another process/container:',
            ($conflicts -join [Environment]::NewLine),
            '',
            'Edit deploy\local\.env.local and change the conflicting value(s), or stop the process/container already using them.',
            'The local-prod stack intentionally uses non-standard defaults so it can coexist with your usual dev Mongo/MinIO/API/front containers.',
            'Use -SkipPortCheck only if you know the ports are already owned by this compose project.'
        ) -join [Environment]::NewLine

        throw $message
    }
}

if (-not (Test-Path $envFile)) {
    Copy-Item $exampleFile $envFile
    Write-Host "Created $envFile from example. Review it if needed."
}

Update-LegacyDefaultLocalEnvironmentValues -Path $envFile

if (-not $SkipPortCheck) {
    Assert-LocalPortsAreAvailable -Path $envFile
}

$arguments = @('compose', '--project-name', $projectName, '--env-file', $envFile, '-f', $composeFile, 'up')

if ($Detached) {
    $arguments += '-d'
}

if ($Build) {
    $arguments += '--build'
}

& docker @arguments
