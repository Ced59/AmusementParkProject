$ErrorActionPreference = 'Stop'
$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$envFile = Join-Path $scriptDirectory '.env.local'
$composeFile = Join-Path $scriptDirectory 'compose.local-prod.yml'

& docker compose --env-file $envFile -f $composeFile down
