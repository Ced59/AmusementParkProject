[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('SaveAccountCredential', 'ClearAccountCredential', 'RegisterAccount', 'CreateToken', 'SaveToken', 'ClearToken', 'SearchParks', 'ExportPark', 'Preview', 'Apply', 'Completeness', 'ImportPhoto', 'RevokeCurrent')]
    [string]$Action,

    [string]$ApiBaseUrl = 'https://amusement-parks.fun/api/',

    [Parameter(ValueFromPipeline = $true)]
    [string]$SecretValue,

    [string]$AccountEmail,

    [ValidateLength(3, 80)]
    [string]$TokenLabel = 'Codex autonomous park data editor',

    [ValidateRange(1, 90)]
    [int]$ExpiresInDays = 30,

    [string]$ParkId,

    [string]$Query,

    [string]$JsonPath,

    [string]$ReceiptPath,

    [string]$OutputPath,

    [ValidateSet('ParkBasics', 'ParkAudience', 'ParkLocation', 'ParkAdministration', 'ParkDescriptions', 'ParkHomeFeature', 'References', 'Zones', 'Items', 'Images', 'OpeningHours', 'History')]
    [string[]]$Sections = @(),

    [ValidateRange(30, 900)]
    [int]$ExportTimeoutSeconds = 600,

    [switch]$AllowWarnings,

    [string]$SourceUrl,

    [ValidateSet('LOGO', 'PARK', 'PARK_ITEM', 'STANDALONE_ATTRACTION')]
    [string]$Category,

    [ValidateSet('PARK', 'PARK_ITEM', 'STANDALONE_ATTRACTION')]
    [string]$OwnerType,

    [string]$OwnerId,

    [string]$Description,

    [string]$MetadataJsonPath,

    [bool]$WithWatermark = $false,

    [bool]$SetAsCurrent = $true,

    [bool]$IsPublished = $true
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:MaximumImageBytes = 10 * 1024 * 1024
$script:CredentialDirectory = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'AmusementParkProject\Codex'
$script:CredentialPath = Join-Path $script:CredentialDirectory 'park-data-editor-token.clixml'
$script:AccountCredentialPath = Join-Path $script:CredentialDirectory 'park-data-editor-account.clixml'

function Get-NormalizedApiBaseUrl {
    param([string]$Value)

    $uri = [Uri]$Value
    if ($uri.Scheme -ne 'https' -and $uri.Host -ne 'localhost') {
        throw 'The API base URL must use HTTPS, except for localhost.'
    }

    return $Value.TrimEnd('/') + '/'
}

function Save-ParkDataEditorToken {
    param([string]$PlainTextToken)

    if ([string]::IsNullOrWhiteSpace($PlainTextToken) -or -not $PlainTextToken.StartsWith('apf_pde_', [StringComparison]::Ordinal)) {
        throw 'A valid park data editor token must be provided through the pipeline.'
    }

    [System.IO.Directory]::CreateDirectory($script:CredentialDirectory) | Out-Null
    $secureToken = ConvertTo-SecureString $PlainTextToken -AsPlainText -Force
    $credential = [System.Management.Automation.PSCredential]::new('PARK_DATA_EDITOR', $secureToken)
    $credential | Export-Clixml -LiteralPath $script:CredentialPath -Force
    Write-Output 'Token stored with Windows user encryption. Its value was not printed.'
}

function Save-ParkDataEditorAccountCredential {
    param([string]$Email, [string]$Password)

    if ([string]::IsNullOrWhiteSpace($Email) -or [string]::IsNullOrWhiteSpace($Password)) {
        throw 'AccountEmail and a password provided through the pipeline are required.'
    }

    try {
        $mailAddress = [System.Net.Mail.MailAddress]::new($Email.Trim())
    }
    catch {
        throw 'AccountEmail must be a valid email address.'
    }

    [System.IO.Directory]::CreateDirectory($script:CredentialDirectory) | Out-Null
    $securePassword = ConvertTo-SecureString $Password -AsPlainText -Force
    $credential = [System.Management.Automation.PSCredential]::new($mailAddress.Address.ToLowerInvariant(), $securePassword)
    $credential | Export-Clixml -LiteralPath $script:AccountCredentialPath -Force
    Write-Output 'Account credential stored with Windows user encryption. Its password was not printed.'
}

function Get-ParkDataEditorAccountCredential {
    if (-not (Test-Path -LiteralPath $script:AccountCredentialPath -PathType Leaf)) {
        throw 'No local park data editor account credential is stored. Run SaveAccountCredential first.'
    }

    $credential = Import-Clixml -LiteralPath $script:AccountCredentialPath
    if ($credential -isnot [System.Management.Automation.PSCredential]) {
        throw 'The local account credential store is invalid.'
    }

    return $credential
}

function Get-ParkDataEditorToken {
    if (-not (Test-Path -LiteralPath $script:CredentialPath -PathType Leaf)) {
        throw 'No local park data editor token is stored. Run SaveToken first.'
    }

    $credential = Import-Clixml -LiteralPath $script:CredentialPath
    if ($credential -isnot [System.Management.Automation.PSCredential]) {
        throw 'The local token store is invalid.'
    }

    return $credential.GetNetworkCredential().Password
}

function Invoke-ParkDataEditorJsonApi {
    param(
        [ValidateSet('GET', 'POST', 'PUT', 'DELETE')]
        [string]$Method,
        [string]$RelativePath,
        [object]$Body
    )

    $token = Get-ParkDataEditorToken
    $headers = @{ Authorization = "Bearer $token" }
    $uri = (Get-NormalizedApiBaseUrl $ApiBaseUrl) + $RelativePath.TrimStart('/')
    $parameters = @{
        Method = $Method
        Uri = $uri
        Headers = $headers
        UseBasicParsing = $true
    }
    if ($null -ne $Body) {
        $parameters.ContentType = 'application/json; charset=utf-8'
        $parameters.Body = $Body | ConvertTo-Json -Depth 100 -Compress
    }

    return Invoke-RestMethod @parameters
}

function Invoke-AnonymousJsonApi {
    param([string]$RelativePath, [object]$Body)

    $uri = (Get-NormalizedApiBaseUrl $ApiBaseUrl) + $RelativePath.TrimStart('/')
    return Invoke-RestMethod -Method POST -Uri $uri -UseBasicParsing `
        -ContentType 'application/json; charset=utf-8' `
        -Body ($Body | ConvertTo-Json -Depth 20 -Compress)
}

function Register-ParkDataEditorAccount {
    $credential = Get-ParkDataEditorAccountCredential
    $password = $credential.GetNetworkCredential().Password
    $response = Invoke-AnonymousJsonApi -RelativePath 'users' -Body @{
        email = $credential.UserName
        password = $password
        verifyPassword = $password
        preferredLanguage = 'FR'
        preferredMeasurementSystem = 'Metric'
    }

    Write-Output $response
}

function New-ParkDataEditorToken {
    $credential = Get-ParkDataEditorAccountCredential
    $login = Invoke-AnonymousJsonApi -RelativePath 'auth/login' -Body @{
        email = $credential.UserName
        password = $credential.GetNetworkCredential().Password
    }
    $jwt = [string]$login.token
    if ([string]::IsNullOrWhiteSpace($jwt)) {
        throw 'The login response did not contain a JWT.'
    }

    $uri = (Get-NormalizedApiBaseUrl $ApiBaseUrl) + 'park-data-editor/tokens'
    $created = Invoke-RestMethod -Method POST -Uri $uri -UseBasicParsing `
        -Headers @{ Authorization = "Bearer $jwt" } `
        -ContentType 'application/json; charset=utf-8' `
        -Body (@{ label = $TokenLabel; expiresInDays = $ExpiresInDays } | ConvertTo-Json -Compress)
    $plainTextToken = [string]$created.plainTextToken
    if ([string]::IsNullOrWhiteSpace($plainTextToken)) {
        throw 'The token creation response did not contain the one-time token secret.'
    }

    Save-ParkDataEditorToken -PlainTextToken $plainTextToken | Out-Null
    return [PSCustomObject]@{
        Id = [string]$created.token.id
        Label = [string]$created.token.label
        DisplayPrefix = [string]$created.token.displayPrefix
        ExpiresAtUtc = [string]$created.token.expiresAtUtc
        StoredLocally = $true
    }
}

function Resolve-RequiredFile {
    param([string]$Path, [string]$ParameterName)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        throw "$ParameterName is required."
    }

    return (Resolve-Path -LiteralPath $Path -ErrorAction Stop).Path
}

function Write-JsonFile {
    param([object]$Value, [string]$Path)

    $json = $Value | ConvertTo-Json -Depth 100
    [IO.File]::WriteAllText($Path, $json, [System.Text.UTF8Encoding]::new($false))
}

function Get-DefaultOutputPath {
    param([string]$InputPath, [string]$Suffix)

    $directory = [IO.Path]::GetDirectoryName($InputPath)
    $name = [IO.Path]::GetFileNameWithoutExtension($InputPath)
    return [IO.Path]::Combine($directory, "$name.$Suffix.json")
}

function Wait-ParkGraphExportJob {
    param([object]$InitialSnapshot, [int]$TimeoutSeconds)

    $snapshot = $InitialSnapshot
    $deadlineUtc = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    while ([string]$snapshot.status -in @('Queued', 'Running')) {
        if ([DateTime]::UtcNow -ge $deadlineUtc) {
            throw "The park export job timed out after $TimeoutSeconds seconds."
        }

        Start-Sleep -Milliseconds 500
        $jobId = [Uri]::EscapeDataString([string]$snapshot.jobId)
        $snapshot = Invoke-ParkDataEditorJsonApi -Method GET `
            -RelativePath "admin/park-graph-upserts/bulk/export-jobs/$jobId" `
            -Body $null
    }

    if ([string]$snapshot.status -ne 'Completed') {
        $reason = if ([string]::IsNullOrWhiteSpace([string]$snapshot.error)) {
            [string]$snapshot.message
        }
        else {
            [string]$snapshot.error
        }
        throw "The park export job ended with status '$($snapshot.status)': $reason"
    }

    if ([string]::IsNullOrWhiteSpace([string]$snapshot.downloadUrl) -or [long]$snapshot.contentLength -le 0) {
        throw 'The completed park export job did not provide a valid download URL and content length.'
    }

    return $snapshot
}

function Invoke-ResumableFileDownload {
    param([string]$Url, [string]$DestinationPath, [long]$ExpectedLength)

    $curl = Get-Command 'curl.exe' -ErrorAction SilentlyContinue
    if ($null -eq $curl) {
        throw 'curl.exe is required for resumable park exports.'
    }

    $partialPath = $DestinationPath + '.partial'
    if (Test-Path -LiteralPath $partialPath -PathType Leaf) {
        Remove-Item -LiteralPath $partialPath -Force
    }

    $attempt = 0
    $stagnantAttempts = 0
    while ($true) {
        $attempt++
        $beforeLength = if (Test-Path -LiteralPath $partialPath -PathType Leaf) {
            (Get-Item -LiteralPath $partialPath).Length
        }
        else {
            0
        }

        & $curl.Source `
            --fail `
            --location `
            --silent `
            --connect-timeout 30 `
            --max-time 120 `
            --continue-at - `
            --output $partialPath `
            --url $Url
        $curlExitCode = $LASTEXITCODE

        $afterLength = if (Test-Path -LiteralPath $partialPath -PathType Leaf) {
            (Get-Item -LiteralPath $partialPath).Length
        }
        else {
            0
        }

        if ($afterLength -gt $ExpectedLength) {
            throw "The resumed park export is larger than expected ($afterLength bytes instead of $ExpectedLength)."
        }
        if ($afterLength -eq $ExpectedLength) {
            break
        }

        if ($afterLength -gt $beforeLength) {
            $stagnantAttempts = 0
        }
        else {
            $stagnantAttempts++
        }

        Write-Verbose "Park export download attempt $attempt ended with curl code $curlExitCode at $afterLength / $ExpectedLength bytes."
        if ($stagnantAttempts -ge 5 -or $attempt -ge 200) {
            throw "The park export download could not progress after $attempt attempts ($afterLength / $ExpectedLength bytes)."
        }
    }

    return $partialPath
}

function Export-ParkGraph {
    param([string]$TargetParkId, [string]$DestinationPath, [string[]]$RequestedSections, [int]$TimeoutSeconds)

    $resolvedOutputPath = [IO.Path]::GetFullPath($DestinationPath)
    $outputDirectory = [IO.Path]::GetDirectoryName($resolvedOutputPath)
    if (-not [string]::IsNullOrWhiteSpace($outputDirectory)) {
        [IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
    }

    $effectiveSections = if (@($RequestedSections).Count -eq 0) {
        @(
            'ParkBasics',
            'ParkAudience',
            'ParkLocation',
            'ParkAdministration',
            'ParkDescriptions',
            'ParkHomeFeature',
            'References',
            'Zones',
            'Items',
            'Images',
            'OpeningHours',
            'History'
        )
    }
    else {
        @($RequestedSections)
    }

    $job = Invoke-ParkDataEditorJsonApi -Method POST `
        -RelativePath 'admin/park-graph-upserts/bulk/export-jobs' `
        -Body @{
            selectionMode = 'explicit'
            parkIds = @($TargetParkId)
            sections = $effectiveSections
        }
    $completedJob = Wait-ParkGraphExportJob -InitialSnapshot $job -TimeoutSeconds $TimeoutSeconds
    $partialPath = Invoke-ResumableFileDownload `
        -Url ([string]$completedJob.downloadUrl) `
        -DestinationPath $resolvedOutputPath `
        -ExpectedLength ([long]$completedJob.contentLength)

    try {
        $bulkDocument = [IO.File]::ReadAllText($partialPath, [Text.Encoding]::UTF8) | ConvertFrom-Json
        $parkDocuments = @($bulkDocument.parks)
        if ($parkDocuments.Count -ne 1) {
            throw "The park export returned $($parkDocuments.Count) park documents instead of one."
        }

        $exportedParkId = [string]$parkDocuments[0].identity.parkId
        if (-not [string]::Equals($exportedParkId, $TargetParkId, [StringComparison]::OrdinalIgnoreCase)) {
            throw "The park export returned '$exportedParkId' instead of '$TargetParkId'."
        }

        Write-JsonFile -Value $parkDocuments[0] -Path $resolvedOutputPath
    }
    finally {
        if (Test-Path -LiteralPath $partialPath -PathType Leaf) {
            Remove-Item -LiteralPath $partialPath -Force
        }
    }

    return $resolvedOutputPath
}

function Set-JsonProperty {
    param([object]$Object, [string]$Name, [object]$Value)

    if ($null -eq $Object.PSObject.Properties[$Name]) {
        $Object | Add-Member -MemberType NoteProperty -Name $Name -Value $Value
    }
    else {
        $Object.$Name = $Value
    }
}

function Test-AllowedImageOwnership {
    $allowed = switch ($Category) {
        'LOGO' { $OwnerType -eq 'PARK' }
        'PARK' { $OwnerType -eq 'PARK' }
        'PARK_ITEM' { $OwnerType -eq 'PARK_ITEM' }
        'STANDALONE_ATTRACTION' { $OwnerType -eq 'STANDALONE_ATTRACTION' }
        default { $false }
    }

    if (-not $allowed) {
        throw "Category $Category cannot be linked to owner type $OwnerType."
    }
}

function Get-ImageContentType {
    param([string]$Path)

    $stream = [IO.File]::OpenRead($Path)
    try {
        $header = New-Object byte[] 12
        $read = $stream.Read($header, 0, $header.Length)
    }
    finally {
        $stream.Dispose()
    }

    if ($read -ge 3 -and $header[0] -eq 0xFF -and $header[1] -eq 0xD8 -and $header[2] -eq 0xFF) {
        return 'image/jpeg'
    }

    if ($read -ge 8 -and $header[0] -eq 0x89 -and $header[1] -eq 0x50 -and $header[2] -eq 0x4E -and $header[3] -eq 0x47) {
        return 'image/png'
    }

    if ($read -ge 12 -and [Text.Encoding]::ASCII.GetString($header, 0, 4) -eq 'RIFF' -and [Text.Encoding]::ASCII.GetString($header, 8, 4) -eq 'WEBP') {
        return 'image/webp'
    }

    throw 'The downloaded file is not a supported JPEG, PNG or WebP image.'
}

function Invoke-MultipartImageUpload {
    param([string]$Path, [string]$ContentType)

    Add-Type -AssemblyName System.Net.Http
    $token = Get-ParkDataEditorToken
    $client = [System.Net.Http.HttpClient]::new()
    $multipart = [System.Net.Http.MultipartFormDataContent]::new()
    $stream = [IO.File]::OpenRead($Path)
    $fileContent = [System.Net.Http.StreamContent]::new($stream)
    try {
        $client.DefaultRequestHeaders.Authorization = [System.Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer', $token)
        $fileContent.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::new($ContentType)
        $uploadFileName = switch ($ContentType) {
            'image/jpeg' { 'codex-import.jpg' }
            'image/png' { 'codex-import.png' }
            'image/webp' { 'codex-import.webp' }
            default { throw "Unsupported image content type $ContentType." }
        }
        $multipart.Add($fileContent, 'File', $uploadFileName)
        $multipart.Add([System.Net.Http.StringContent]::new($Category), 'Category')
        $multipart.Add([System.Net.Http.StringContent]::new(([string]$WithWatermark).ToLowerInvariant()), 'WithWatermark')
        $multipart.Add([System.Net.Http.StringContent]::new(([string]$IsPublished).ToLowerInvariant()), 'IsPublished')
        if (-not [string]::IsNullOrWhiteSpace($Description)) {
            $multipart.Add([System.Net.Http.StringContent]::new($Description), 'Description')
        }

        $uri = (Get-NormalizedApiBaseUrl $ApiBaseUrl) + 'park-data-editor/images'
        $response = $client.PostAsync($uri, $multipart).GetAwaiter().GetResult()
        $responseBody = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        if (-not $response.IsSuccessStatusCode) {
            throw "Image upload failed with HTTP $([int]$response.StatusCode): $responseBody"
        }

        return $responseBody | ConvertFrom-Json
    }
    finally {
        $fileContent.Dispose()
        $stream.Dispose()
        $multipart.Dispose()
        $client.Dispose()
    }
}

function Import-ParkPhoto {
    if ([string]::IsNullOrWhiteSpace($SourceUrl) -or [string]::IsNullOrWhiteSpace($Category) -or
        [string]::IsNullOrWhiteSpace($OwnerType) -or [string]::IsNullOrWhiteSpace($OwnerId)) {
        throw 'SourceUrl, Category, OwnerType and OwnerId are required for ImportPhoto.'
    }

    $sourceUri = [Uri]$SourceUrl
    if (-not $sourceUri.IsAbsoluteUri -or $sourceUri.Scheme -notin @('http', 'https') -or
        -not [string]::IsNullOrWhiteSpace($sourceUri.UserInfo)) {
        throw 'SourceUrl must be an absolute public HTTP(S) URL without embedded credentials.'
    }

    Test-AllowedImageOwnership
    $temporaryPath = Join-Path ([IO.Path]::GetTempPath()) ("amusementpark-codex-" + [Guid]::NewGuid().ToString('N') + '.image')
    $uploadedImageId = $null
    try {
        & curl.exe --fail --location --max-redirs 5 --proto '=http,https' --proto-redir '=http,https' `
            --connect-timeout 15 --max-time 90 --max-filesize $script:MaximumImageBytes `
            --silent --show-error --output $temporaryPath -- $SourceUrl
        if ($LASTEXITCODE -ne 0) {
            throw "Image download failed with curl exit code $LASTEXITCODE."
        }

        $fileInfo = Get-Item -LiteralPath $temporaryPath
        if ($fileInfo.Length -le 0 -or $fileInfo.Length -gt $script:MaximumImageBytes) {
            throw 'The downloaded image must be between 1 byte and 10 MB.'
        }

        $contentType = Get-ImageContentType -Path $temporaryPath
        $upload = Invoke-MultipartImageUpload -Path $temporaryPath -ContentType $contentType
        $uploadedImageId = [string]$upload.id
        if ([string]::IsNullOrWhiteSpace($uploadedImageId)) {
            throw 'The upload response did not contain an image id.'
        }

        $linkBody = @{
            imageId = $uploadedImageId
            ownerType = $OwnerType
            ownerId = $OwnerId
            description = $Description
            setAsCurrent = $SetAsCurrent
        }
        $linkedImage = Invoke-ParkDataEditorJsonApi -Method POST -RelativePath 'park-data-editor/images/links' -Body $linkBody

        if ([string]::IsNullOrWhiteSpace($MetadataJsonPath)) {
            $metadata = @{
                category = $Category
                ownerType = $OwnerType
                ownerId = $OwnerId
                isCurrent = $SetAsCurrent
                description = $Description
                altTexts = @()
                captions = @()
                credits = @()
                tagIds = @()
                isPublished = $IsPublished
                sourceUrl = $SourceUrl
            }
        }
        else {
            $resolvedMetadataPath = Resolve-RequiredFile -Path $MetadataJsonPath -ParameterName 'MetadataJsonPath'
            $metadata = [IO.File]::ReadAllText($resolvedMetadataPath, [Text.Encoding]::UTF8) | ConvertFrom-Json
            Set-JsonProperty -Object $metadata -Name 'category' -Value $Category
            Set-JsonProperty -Object $metadata -Name 'ownerType' -Value $OwnerType
            Set-JsonProperty -Object $metadata -Name 'ownerId' -Value $OwnerId
            Set-JsonProperty -Object $metadata -Name 'isCurrent' -Value $SetAsCurrent
            Set-JsonProperty -Object $metadata -Name 'isPublished' -Value $IsPublished
            Set-JsonProperty -Object $metadata -Name 'sourceUrl' -Value $SourceUrl
        }

        $updatedImage = Invoke-ParkDataEditorJsonApi -Method PUT `
            -RelativePath "park-data-editor/images/$([Uri]::EscapeDataString($uploadedImageId))/metadata" `
            -Body $metadata
        return $updatedImage
    }
    catch {
        if (-not [string]::IsNullOrWhiteSpace($uploadedImageId)) {
            Write-Warning "The image was uploaded as $uploadedImageId but a later step failed. The token cannot delete it; an administrator must inspect the orphan before cleanup."
        }

        throw
    }
    finally {
        if (Test-Path -LiteralPath $temporaryPath) {
            Remove-Item -LiteralPath $temporaryPath -Force
        }
    }
}

$ApiBaseUrl = Get-NormalizedApiBaseUrl $ApiBaseUrl

switch ($Action) {
    'SaveAccountCredential' {
        Save-ParkDataEditorAccountCredential -Email $AccountEmail -Password $SecretValue
    }
    'ClearAccountCredential' {
        if (Test-Path -LiteralPath $script:AccountCredentialPath -PathType Leaf) {
            Remove-Item -LiteralPath $script:AccountCredentialPath -Force
        }
        Write-Output 'Local account credential removed. This does not delete, block or change the server account.'
    }
    'RegisterAccount' {
        Register-ParkDataEditorAccount
    }
    'CreateToken' {
        New-ParkDataEditorToken
    }
    'SaveToken' {
        Save-ParkDataEditorToken -PlainTextToken $SecretValue
    }
    'ClearToken' {
        if (Test-Path -LiteralPath $script:CredentialPath -PathType Leaf) {
            Remove-Item -LiteralPath $script:CredentialPath -Force
        }
        Write-Output 'Local token removed. This does not revoke it on the server.'
    }
    'SearchParks' {
        $relativePath = 'park-data-editor/parks?page=1&size=50'
        if (-not [string]::IsNullOrWhiteSpace($Query)) {
            $relativePath += '&query=' + [Uri]::EscapeDataString($Query)
        }
        Invoke-ParkDataEditorJsonApi -Method GET -RelativePath $relativePath -Body $null
    }
    'ExportPark' {
        if ([string]::IsNullOrWhiteSpace($ParkId) -or [string]::IsNullOrWhiteSpace($OutputPath)) {
            throw 'ParkId and OutputPath are required for ExportPark.'
        }
        Export-ParkGraph `
            -TargetParkId $ParkId `
            -DestinationPath $OutputPath `
            -RequestedSections $Sections `
            -TimeoutSeconds $ExportTimeoutSeconds
    }
    'Preview' {
        $resolvedJsonPath = Resolve-RequiredFile -Path $JsonPath -ParameterName 'JsonPath'
        $jsonBody = [IO.File]::ReadAllText($resolvedJsonPath, [Text.Encoding]::UTF8) | ConvertFrom-Json
        $preview = Invoke-ParkDataEditorJsonApi -Method POST -RelativePath 'admin/park-graph-upserts/preview' -Body $jsonBody
        $previewOutputPath = if ([string]::IsNullOrWhiteSpace($OutputPath)) {
            Get-DefaultOutputPath -InputPath $resolvedJsonPath -Suffix 'preview'
        } else { $OutputPath }
        Write-JsonFile -Value $preview -Path $previewOutputPath

        $effectiveReceiptPath = if ([string]::IsNullOrWhiteSpace($ReceiptPath)) {
            Get-DefaultOutputPath -InputPath $resolvedJsonPath -Suffix 'preview-receipt'
        } else { $ReceiptPath }
        $receipt = @{
            schemaVersion = 1
            apiBaseUrl = $ApiBaseUrl
            jsonPath = $resolvedJsonPath
            jsonSha256 = (Get-FileHash -LiteralPath $resolvedJsonPath -Algorithm SHA256).Hash
            createdAtUtc = [DateTime]::UtcNow.ToString('o')
            operationId = $preview.operationId
            canApply = [bool]$preview.canApply
            errorCount = @($preview.errors).Count
            warningCount = @($preview.warnings).Count
            warningsApproved = [bool]$AllowWarnings
        }
        Write-JsonFile -Value $receipt -Path $effectiveReceiptPath

        if (-not $receipt.canApply -or $receipt.errorCount -gt 0) {
            throw "Preview is not applicable. Inspect $previewOutputPath."
        }
        if ($receipt.warningCount -gt 0 -and -not $receipt.warningsApproved) {
            throw "Preview contains warnings. Inspect them, then rerun Preview with -AllowWarnings only if every warning is non-blocking."
        }

        [PSCustomObject]@{ PreviewPath = $previewOutputPath; ReceiptPath = $effectiveReceiptPath; Result = $preview }
    }
    'Apply' {
        $resolvedJsonPath = Resolve-RequiredFile -Path $JsonPath -ParameterName 'JsonPath'
        $resolvedReceiptPath = Resolve-RequiredFile -Path $ReceiptPath -ParameterName 'ReceiptPath'
        $receipt = [IO.File]::ReadAllText($resolvedReceiptPath, [Text.Encoding]::UTF8) | ConvertFrom-Json
        $currentHash = (Get-FileHash -LiteralPath $resolvedJsonPath -Algorithm SHA256).Hash
        $receiptAge = [DateTime]::UtcNow - [DateTime]::Parse($receipt.createdAtUtc).ToUniversalTime()
        if ($receipt.schemaVersion -ne 1 -or $receipt.apiBaseUrl -ne $ApiBaseUrl -or
            $receipt.jsonSha256 -ne $currentHash -or -not $receipt.canApply -or
            $receipt.errorCount -gt 0 -or ($receipt.warningCount -gt 0 -and -not $receipt.warningsApproved) -or
            $receiptAge.TotalMinutes -gt 30 -or $receiptAge.TotalSeconds -lt 0) {
            throw 'The Preview receipt is invalid, stale, targets another API, or does not match the current JSON. Run Preview again.'
        }

        $jsonBody = [IO.File]::ReadAllText($resolvedJsonPath, [Text.Encoding]::UTF8) | ConvertFrom-Json
        $apply = Invoke-ParkDataEditorJsonApi -Method POST -RelativePath 'admin/park-graph-upserts/apply' -Body $jsonBody
        $applyOutputPath = if ([string]::IsNullOrWhiteSpace($OutputPath)) {
            Get-DefaultOutputPath -InputPath $resolvedJsonPath -Suffix 'apply'
        } else { $OutputPath }
        Write-JsonFile -Value $apply -Path $applyOutputPath
        if (-not $apply.isApplied -or @($apply.errors).Count -gt 0) {
            throw "Apply did not complete successfully. Inspect $applyOutputPath."
        }
        [PSCustomObject]@{ ApplyPath = $applyOutputPath; Result = $apply }
    }
    'Completeness' {
        if ([string]::IsNullOrWhiteSpace($ParkId)) {
            throw 'ParkId is required for Completeness.'
        }
        Invoke-ParkDataEditorJsonApi -Method GET `
            -RelativePath "park-data-editor/parks/$([Uri]::EscapeDataString($ParkId))/data-completeness" `
            -Body $null
    }
    'ImportPhoto' {
        Import-ParkPhoto
    }
    'RevokeCurrent' {
        Invoke-ParkDataEditorJsonApi -Method DELETE -RelativePath 'park-data-editor/tokens/current' -Body $null | Out-Null
        if (Test-Path -LiteralPath $script:CredentialPath -PathType Leaf) {
            Remove-Item -LiteralPath $script:CredentialPath -Force
        }
        Write-Output 'The current token was revoked on the server and removed locally.'
    }
}
