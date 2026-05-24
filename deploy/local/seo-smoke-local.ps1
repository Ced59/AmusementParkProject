$ErrorActionPreference = 'Stop'
$root = Resolve-Path (Join-Path $PSScriptRoot '../..')
Push-Location (Join-Path $root 'FRONT/AmusementPark')
try {
    $env:PUBLIC_BASE_URL = 'http://amusement.localhost:18080'
    npm run seo:ssr-smoke
}
finally {
    Pop-Location
}
