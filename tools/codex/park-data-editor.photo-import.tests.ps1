param([string]$ClientPath = (Join-Path $PSScriptRoot 'park-data-editor.ps1'))

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$tokens = $null
$parseErrors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile($ClientPath, [ref]$tokens, [ref]$parseErrors)
if ($parseErrors.Count -gt 0) { throw 'The photo import client must parse successfully.' }
foreach ($name in @('Import-ParkPhoto', 'Resolve-RequiredFile', 'Test-AllowedImageOwnership',
                    'Get-ImageContentType', 'Get-NormalizedApiBaseUrl', 'Set-JsonProperty')) {
    $functionAst = $ast.Find({
        param($node)
        $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq $name
    }, $true)
    if ($null -eq $functionAst) { throw "Missing function $name." }
    Invoke-Expression $functionAst.Extent.Text
}

$testDirectory = Join-Path ([IO.Path]::GetTempPath()) ('park-photo-test-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($testDirectory) | Out-Null
$localPath = Join-Path $testDirectory 'reviewed-photo.jpg'
$script:testBytes = [byte[]]@(0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3, 4, 5, 6, 7, 8)
[IO.File]::WriteAllBytes($localPath, $script:testBytes)
$script:MaximumImageBytes = 10 * 1024 * 1024
$script:MaximumProductionImageUploadBytes = 960 * 1024
$SourceUrl = 'https://archive.example/official-document.swf'
$ApiBaseUrl = 'https://amusement-parks.fun/api/'
$Category = 'PARK_ITEM'
$OwnerType = 'PARK_ITEM'
$OwnerId = 'item-1'
$Description = 'Historical attraction photograph'
$SetAsCurrent = $false
$IsPublished = $false
$MetadataJsonPath = ''
$FilePath = $localPath
$script:photoUploadCount = 0
$script:photoDownloadCount = 0
$script:photoRequests = @()
$script:failPhotoUpload = $false
$script:uploadedPath = $null
$script:downloadedPath = $null

function curl.exe {
    $script:photoDownloadCount++
    $arguments = @($args)
    $index = [Array]::IndexOf($arguments, '--output')
    if ($index -lt 0 -or $arguments[-1] -ne $SourceUrl) { throw 'Unexpected download request.' }
    $script:downloadedPath = [string]$arguments[$index + 1]
    [IO.File]::WriteAllBytes($script:downloadedPath, $script:testBytes)
    $global:LASTEXITCODE = 0
}

function Invoke-MultipartImageUpload {
    param([string]$Path, [string]$ContentType)
    $script:photoUploadCount++
    $script:uploadedPath = $Path
    if ($ContentType -ne 'image/jpeg') { throw 'The file signature must determine its image type.' }
    if ($script:failPhotoUpload) { throw 'Simulated upload failure.' }
    return [pscustomobject]@{ id = 'uploaded-photo' }
}

function Invoke-ParkDataEditorJsonApi {
    param([string]$Method, [string]$RelativePath, [object]$Body)
    $script:photoRequests += [pscustomobject]@{ method = $Method; path = $RelativePath; body = $Body }
    return $Body
}

function Assert-PhotoRejectedBeforeUpload {
    param([string]$Label)
    $countBefore = $script:photoUploadCount
    $requestsBefore = $script:photoRequests.Count
    $message = ''
    try { Import-ParkPhoto | Out-Null }
    catch { $message = $_.Exception.Message }
    if (-not $message -or $script:photoUploadCount -ne $countBefore -or $script:photoRequests.Count -ne $requestsBefore) {
        throw "Invalid photo must fail before mutation: $Label."
    }
}

try {
    $originalHash = (Get-FileHash -LiteralPath $localPath -Algorithm SHA256).Hash
    $result = Import-ParkPhoto
    if ($script:photoDownloadCount -ne 0 -or $script:uploadedPath -ne $localPath -or
        (Get-FileHash -LiteralPath $localPath -Algorithm SHA256).Hash -ne $originalHash) {
        throw 'A reviewed local photo must be uploaded unchanged without downloading or deleting the original.'
    }
    if ($script:photoRequests.Count -ne 2 -or
        $script:photoRequests[0].method -ne 'POST' -or $script:photoRequests[0].path -ne 'park-data-editor/images/links' -or
        $script:photoRequests[0].body.imageId -ne 'uploaded-photo' -or
        $script:photoRequests[1].method -ne 'PUT' -or $script:photoRequests[1].path -ne 'park-data-editor/images/uploaded-photo/metadata' -or
        $result.sourceUrl -ne $SourceUrl -or $result.ownerId -ne $OwnerId -or
        $result.isPublished -ne $false -or $result.isCurrent -ne $false) {
        throw 'Local imports must retain the official linking, metadata, provenance and visibility workflow.'
    }

    $script:failPhotoUpload = $true
    $message = ''
    try { Import-ParkPhoto | Out-Null } catch { $message = $_.Exception.Message }
    if ($message -ne 'Simulated upload failure.' -or
        (Get-FileHash -LiteralPath $localPath -Algorithm SHA256).Hash -ne $originalHash) {
        throw 'A failed upload must preserve the supplied local file.'
    }
    $script:failPhotoUpload = $false

    $MetadataJsonPath = Join-Path $testDirectory 'metadata.json'
    [IO.File]::WriteAllText($MetadataJsonPath, '{"description":"Reviewed caption","sourceUrl":"https://wrong.example","isPublished":true}')
    $result = Import-ParkPhoto
    if ($result.sourceUrl -ne $SourceUrl -or $result.description -ne 'Reviewed caption' -or $result.isPublished -ne $false) {
        throw 'Supplied metadata must preserve reviewed copy and the explicit source and private-publication flags.'
    }
    $MetadataJsonPath = ''

    foreach ($invalidUrl in @('', 'file:///tmp/photo.jpg', 'https://user:password@example.test/photo.jpg')) {
        $SourceUrl = $invalidUrl
        Assert-PhotoRejectedBeforeUpload 'invalid source provenance'
    }
    $SourceUrl = 'https://archive.example/official-document.swf'
    $Category = 'LOGO'
    Assert-PhotoRejectedBeforeUpload 'invalid owner/category pair'
    $Category = 'PARK_ITEM'
    $FilePath = Join-Path $testDirectory 'missing.jpg'
    Assert-PhotoRejectedBeforeUpload 'missing local file'

    $FilePath = Join-Path $testDirectory 'invalid.jpg'
    [IO.File]::WriteAllBytes($FilePath, [byte[]]@())
    Assert-PhotoRejectedBeforeUpload 'empty local file'
    [IO.File]::WriteAllText($FilePath, '<html>Not an image</html>')
    Assert-PhotoRejectedBeforeUpload 'unsupported content'
    [IO.File]::WriteAllBytes($FilePath, [byte[]]::new($script:MaximumProductionImageUploadBytes + 1))
    Assert-PhotoRejectedBeforeUpload 'production size limit'
    $ApiBaseUrl = 'https://staging.example/api/'
    [IO.File]::WriteAllBytes($FilePath, [byte[]]::new($script:MaximumImageBytes + 1))
    Assert-PhotoRejectedBeforeUpload 'absolute size limit'
    $ApiBaseUrl = 'https://amusement-parks.fun/api/'

    $FilePath = ''
    $result = Import-ParkPhoto
    if ($script:photoDownloadCount -ne 1 -or $script:uploadedPath -ne $script:downloadedPath -or
        (Test-Path -LiteralPath $script:downloadedPath) -or $result.sourceUrl -ne $SourceUrl) {
        throw 'Remote imports must keep downloading, uploading and removing only their temporary file.'
    }
    Write-Output 'Park data editor local and remote photo import tests passed.'
}
finally {
    Remove-Item -LiteralPath $testDirectory -Recurse -Force
}
