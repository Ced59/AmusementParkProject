$ErrorActionPreference = 'Stop'
$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$envFile = Join-Path $scriptDirectory '.env.local'
$composeFile = Join-Path $scriptDirectory 'compose.local-prod.yml'
$projectName = 'amusementpark-local-prod'

& docker compose --project-name $projectName --env-file $envFile -f $composeFile down
