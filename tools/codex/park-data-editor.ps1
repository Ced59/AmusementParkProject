[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('SaveAccountCredential', 'ClearAccountCredential', 'RegisterAccount', 'CreateToken', 'SaveToken', 'ClearToken', 'Status', 'SearchParks', 'ExportPark', 'Preview', 'Apply', 'PreviewDeletion', 'ApplyDeletion', 'Completeness', 'ImportPhoto', 'UpdatePhotoMetadata', 'ResolveFacebookPublication', 'PublishFacebook', 'RetryFacebookPublication', 'RevokeCurrent')]
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

    [ValidateRange(1, 1000000)]
    [int]$Page = 1,

    [ValidateRange(1, 50)]
    [int]$PageSize = 50,

    [string]$JsonPath,

    [string]$ReceiptPath,

    [string]$OutputPath,

    [ValidateSet('ParkBasics', 'ParkAudience', 'ParkLocation', 'ParkAdministration', 'ParkDescriptions', 'ParkHomeFeature', 'References', 'Zones', 'Items', 'Images', 'OpeningHours', 'Pricing', 'History')]
    [string[]]$Sections = @(),

    [ValidateRange(30, 900)]
    [int]$ExportTimeoutSeconds = 600,

    [switch]$AllowWarnings,

    [switch]$ProjectForPublication,

    [string]$SourceUrl,

    [ValidateSet('LOGO', 'PARK', 'PARK_ITEM', 'STANDALONE_ATTRACTION')]
    [string]$Category,

    [ValidateSet('PARK', 'PARK_ITEM', 'STANDALONE_ATTRACTION')]
    [string]$OwnerType,

    [string]$OwnerId,

    [string]$ImageId,

    [string]$PublicationId,

    [string]$Url,

    [string]$Message,

    [ValidateRange(1, 1000000)]
    [int]$ImagePage = 1,

    [ValidateRange(1, 24)]
    [int]$ImagePageSize = 6,

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

    Add-Type -AssemblyName System.Net.Http
    $token = Get-ParkDataEditorToken
    $uri = (Get-NormalizedApiBaseUrl $ApiBaseUrl) + $RelativePath.TrimStart('/')
    $client = [System.Net.Http.HttpClient]::new()
    $deadlineUtc = [DateTime]::UtcNow.AddMinutes(10)
    try {
        $client.Timeout = [TimeSpan]::FromSeconds(300)
        $client.DefaultRequestHeaders.ConnectionClose = $true
        while ($true) {
            $request = [System.Net.Http.HttpRequestMessage]::new(
                [System.Net.Http.HttpMethod]::new($Method),
                $uri)
            try {
                $request.Headers.Authorization = [System.Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer', $token)
                $request.Headers.ExpectContinue = $false
                if ($null -ne $Body) {
                    $json = $Body | ConvertTo-Json -Depth 100 -Compress
                    $request.Content = [System.Net.Http.StringContent]::new(
                        $json,
                        [Text.Encoding]::UTF8,
                        'application/json')
                }

                $response = $client.SendAsync($request).GetAwaiter().GetResult()
                try {
                    $responseBody = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
                    if ([int]$response.StatusCode -eq 429 -and [DateTime]::UtcNow -lt $deadlineUtc) {
                        $retryAfterSeconds = 5
                        $retryAfterHeader = $response.Headers.RetryAfter
                        if ($null -ne $retryAfterHeader -and $null -ne $retryAfterHeader.Delta) {
                            $retryAfterSeconds = [Math]::Max(1, [int][Math]::Ceiling($retryAfterHeader.Delta.TotalSeconds))
                        }
                        elseif ($null -ne $retryAfterHeader -and $null -ne $retryAfterHeader.Date) {
                            $retryAfterSeconds = [Math]::Max(
                                1,
                                [int][Math]::Ceiling(($retryAfterHeader.Date.UtcDateTime - [DateTime]::UtcNow).TotalSeconds))
                        }

                        Write-Verbose "The park data editor API is busy. Retrying after $retryAfterSeconds seconds."
                        Start-Sleep -Seconds $retryAfterSeconds
                        continue
                    }
                    if (-not $response.IsSuccessStatusCode) {
                        throw "Park data editor request failed with HTTP $([int]$response.StatusCode): $responseBody"
                    }
                    if ([string]::IsNullOrWhiteSpace($responseBody)) {
                        return $null
                    }

                    return $responseBody | ConvertFrom-Json
                }
                finally {
                    $response.Dispose()
                }
            }
            finally {
                $request.Dispose()
            }
        }
    }
    finally {
        $client.Dispose()
    }
}

function Wait-ParkDataEditorAvailability {
    param([int]$TimeoutSeconds = 600)

    $deadlineUtc = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    while ($true) {
        $status = Invoke-ParkDataEditorJsonApi `
            -Method GET `
            -RelativePath 'park-data-editor/operations/status' `
            -Body $null
        if (-not [bool]$status.isBusy -and [bool]$status.canStartResourceIntensiveOperation) {
            return $status
        }

        if ([DateTime]::UtcNow -ge $deadlineUtc) {
            throw "The park data editor remained busy for $TimeoutSeconds seconds."
        }

        $pollIntervalSeconds = [Math]::Max(
            5,
            [Math]::Max([int]$status.recommendedPollIntervalSeconds, [int]$status.retryAfterSeconds))
        Write-Verbose "Another park data editor operation is active. Checking again after $pollIntervalSeconds seconds."
        Start-Sleep -Seconds $pollIntervalSeconds
    }
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

function Assert-ParkDataDeletionRequest {
    param(
        [object]$Request,
        [string]$TargetParkId
    )

    if ([string]::IsNullOrWhiteSpace($TargetParkId)) {
        throw 'ParkId is required for a controlled deletion.'
    }

    $allowedRequestProperties = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($propertyName in @('targetParkId', 'createIfMissing', 'replaceCollections', 'document')) {
        $allowedRequestProperties.Add($propertyName) | Out-Null
    }
    foreach ($property in $Request.PSObject.Properties) {
        if (-not $allowedRequestProperties.Contains($property.Name)) {
            throw "A controlled deletion request cannot contain the '$($property.Name)' property."
        }
    }

    $targetParkProperty = $Request.PSObject.Properties['targetParkId']
    $createIfMissingProperty = $Request.PSObject.Properties['createIfMissing']
    $replaceCollectionsProperty = $Request.PSObject.Properties['replaceCollections']
    $documentProperty = $Request.PSObject.Properties['document']
    if ($null -eq $targetParkProperty -or [string]::IsNullOrWhiteSpace([string]$targetParkProperty.Value) -or
        -not [string]::Equals(([string]$targetParkProperty.Value).Trim(), $TargetParkId.Trim(), [StringComparison]::Ordinal) -or
        $null -eq $createIfMissingProperty -or $createIfMissingProperty.Value -isnot [bool] -or [bool]$createIfMissingProperty.Value -or
        $null -eq $replaceCollectionsProperty -or $replaceCollectionsProperty.Value -isnot [bool] -or [bool]$replaceCollectionsProperty.Value -or
        $null -eq $documentProperty -or $null -eq $documentProperty.Value) {
        throw 'A controlled deletion must target ParkId explicitly, disable createIfMissing and replaceCollections, and contain a document.'
    }

    $document = $documentProperty.Value
    $modeProperty = $document.PSObject.Properties['mode']
    $supprProperty = $document.PSObject.Properties['suppr']
    if ($null -eq $modeProperty -or -not [string]::Equals([string]$modeProperty.Value, 'merge', [StringComparison]::Ordinal) -or
        $null -eq $supprProperty) {
        throw "A controlled deletion document must use mode 'merge' and contain suppr."
    }

    $allowedDocumentProperties = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($propertyName in @('documentType', 'schemaVersion', 'mode', 'metadata', 'identity', 'suppr')) {
        $allowedDocumentProperties.Add($propertyName) | Out-Null
    }
    foreach ($property in $document.PSObject.Properties) {
        if (-not $allowedDocumentProperties.Contains($property.Name)) {
            throw "A controlled deletion document cannot contain the '$($property.Name)' mutation section."
        }
    }

    $identityProperty = $document.PSObject.Properties['identity']
    if ($null -ne $identityProperty -and $null -ne $identityProperty.Value) {
        $identityParkIdProperty = $identityProperty.Value.PSObject.Properties['parkId']
        if ($null -eq $identityParkIdProperty -or
            -not [string]::Equals(([string]$identityParkIdProperty.Value).Trim(), $TargetParkId.Trim(), [StringComparison]::Ordinal)) {
            throw 'The controlled deletion identity.parkId must match ParkId.'
        }
    }

    if ($supprProperty.Value -isnot [Array]) {
        throw 'A controlled deletion suppr value must be an array.'
    }

    $entries = @($supprProperty.Value)
    if ($entries.Count -lt 1 -or $entries.Count -gt 100) {
        throw 'A controlled deletion must contain between 1 and 100 explicit suppr entries.'
    }

    $allowedEntityTypes = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($entityType in @('Image', 'ParkItem', 'ParkZone')) {
        $allowedEntityTypes.Add($entityType) | Out-Null
    }
    $seenTargets = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($entry in $entries) {
        if ($entry -isnot [PSCustomObject]) {
            throw 'Every controlled deletion entry must be an object with entityType and id.'
        }

        $entryProperties = @($entry.PSObject.Properties)
        $entityTypeProperty = $entry.PSObject.Properties['entityType']
        $idProperty = $entry.PSObject.Properties['id']
        if ($entryProperties.Count -ne 2 -or $null -eq $entityTypeProperty -or $null -eq $idProperty -or
            -not $allowedEntityTypes.Contains([string]$entityTypeProperty.Value) -or
            [string]::IsNullOrWhiteSpace([string]$idProperty.Value)) {
            throw 'Every controlled deletion entry must contain only a supported entityType (Image, ParkItem or ParkZone) and a non-empty id.'
        }

        $targetKey = "$([string]$entityTypeProperty.Value):$(([string]$idProperty.Value).Trim())"
        if (-not $seenTargets.Add($targetKey)) {
            throw "The controlled deletion target '$targetKey' is duplicated in the request."
        }
    }

    return $entries.Count
}

function Assert-ControlledDeletionResult {
    param(
        [object]$Result,
        [object]$Request,
        [int]$ExpectedDeletionCount,
        [bool]$RequireApplied
    )

    if (-not [bool]$Result.canApply -or @($Result.errors).Count -gt 0 -or @($Result.warnings).Count -gt 0) {
        throw 'The controlled deletion contains an error or warning and cannot continue.'
    }
    if ($RequireApplied -and -not [bool]$Result.isApplied) {
        throw 'The controlled deletion was not applied.'
    }
    if ([int]$Result.counts.deleted -ne $ExpectedDeletionCount) {
        throw "The controlled deletion expected $ExpectedDeletionCount deletions but the API reported $($Result.counts.deleted)."
    }

    $expectedTargets = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($entry in @($Request.document.suppr)) {
        $expectedTargets.Add("$([string]$entry.entityType):$(([string]$entry.id).Trim())") | Out-Null
    }

    $actualTargets = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($change in @($Result.changes)) {
        if ([string]::Equals([string]$change.changeType, 'Unchanged', [StringComparison]::Ordinal)) {
            continue
        }
        if (-not [string]::Equals([string]$change.changeType, 'Deleted', [StringComparison]::Ordinal) -or
            [string]::IsNullOrWhiteSpace([string]$change.entityType) -or
            [string]::IsNullOrWhiteSpace([string]$change.entityId)) {
            throw 'The controlled deletion preview contains an unexpected mutation.'
        }

        $actualTargets.Add("$([string]$change.entityType):$(([string]$change.entityId).Trim())") | Out-Null
    }

    if ($actualTargets.Count -ne $expectedTargets.Count) {
        throw 'The controlled deletion result does not contain exactly the requested targets.'
    }
    foreach ($target in $expectedTargets) {
        if (-not $actualTargets.Contains($target)) {
            throw "The controlled deletion target '$target' is missing from the API result."
        }
    }
}

function Wait-ParkGraphExportJob {
    param([object]$InitialSnapshot, [int]$TimeoutSeconds)

    $snapshot = $InitialSnapshot
    $deadlineUtc = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    while ([string]$snapshot.status -in @('Queued', 'Running')) {
        if ([DateTime]::UtcNow -ge $deadlineUtc) {
            throw "The park export job timed out after $TimeoutSeconds seconds."
        }

        Start-Sleep -Seconds 5
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

    $systemCurlPath = Join-Path $env:SystemRoot 'System32\curl.exe'
    $curlCommand = Get-Command 'curl.exe' -ErrorAction SilentlyContinue
    $curlPath = if (Test-Path -LiteralPath $systemCurlPath -PathType Leaf) {
        $systemCurlPath
    }
    elseif ($null -ne $curlCommand) {
        [string]$curlCommand.Source
    }
    else {
        $null
    }
    if ([string]::IsNullOrWhiteSpace($curlPath)) {
        throw 'curl.exe is required for resumable park exports.'
    }

    $partialPath = $DestinationPath + '.partial'
    $chunkPath = $partialPath + '.chunk'
    if (Test-Path -LiteralPath $partialPath -PathType Leaf) {
        Remove-Item -LiteralPath $partialPath -Force
    }
    if (Test-Path -LiteralPath $chunkPath -PathType Leaf) {
        Remove-Item -LiteralPath $chunkPath -Force
    }

    # Keep ranges below the production proxy's observed early-close threshold.
    $chunkSize = 64 * 1024
    while ($true) {
        $beforeLength = if (Test-Path -LiteralPath $partialPath -PathType Leaf) {
            (Get-Item -LiteralPath $partialPath).Length
        }
        else {
            0
        }
        if ($beforeLength -eq $ExpectedLength) {
            break
        }
        if ($beforeLength -gt $ExpectedLength) {
            throw "The resumed park export is larger than expected ($beforeLength bytes instead of $ExpectedLength)."
        }

        $rangeEnd = [Math]::Min($beforeLength + $chunkSize - 1, $ExpectedLength - 1)
        $expectedChunkLength = $rangeEnd - $beforeLength + 1
        $range = "$beforeLength-$rangeEnd"
        $chunkComplete = $false
        for ($attempt = 1; $attempt -le 5; $attempt++) {
            if (Test-Path -LiteralPath $chunkPath -PathType Leaf) {
                Remove-Item -LiteralPath $chunkPath -Force
            }

            & $curlPath `
                --fail `
                --location `
                --silent `
                --show-error `
                --http1.1 `
                --header 'Accept-Encoding: identity' `
                --connect-timeout 15 `
                --speed-limit 64 `
                --speed-time 60 `
                --max-time 120 `
                --range $range `
                --output $chunkPath `
                --url $Url
            $curlExitCode = $LASTEXITCODE
            $actualChunkLength = if (Test-Path -LiteralPath $chunkPath -PathType Leaf) {
                (Get-Item -LiteralPath $chunkPath).Length
            }
            else {
                0
            }

            if ($actualChunkLength -eq $expectedChunkLength) {
                $chunkComplete = $true
                break
            }
            if ($actualChunkLength -gt $expectedChunkLength) {
                throw "The park export range $range returned $actualChunkLength bytes instead of $expectedChunkLength."
            }

            Write-Verbose "Park export range $range attempt $attempt ended with curl code $curlExitCode at $actualChunkLength / $expectedChunkLength bytes."
            if ($attempt -lt 5) {
                Start-Sleep -Seconds ([Math]::Min($attempt * 2, 8))
            }
        }

        if (-not $chunkComplete) {
            throw "The park export range $range could not be downloaded completely after 5 attempts."
        }

        $destinationStream = [IO.File]::Open($partialPath, [IO.FileMode]::Append, [IO.FileAccess]::Write, [IO.FileShare]::None)
        $chunkStream = [IO.File]::OpenRead($chunkPath)
        try {
            $chunkStream.CopyTo($destinationStream)
        }
        finally {
            $chunkStream.Dispose()
            $destinationStream.Dispose()
        }
        Remove-Item -LiteralPath $chunkPath -Force
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
            'Pricing',
            'History'
        )
    }
    else {
        @($RequestedSections)
    }

    $partialPath = $resolvedOutputPath + '.partial'
    try {
        Wait-ParkDataEditorAvailability -TimeoutSeconds $TimeoutSeconds | Out-Null
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
        $chunkPath = $resolvedOutputPath + '.partial.chunk'
        if (Test-Path -LiteralPath $chunkPath -PathType Leaf) {
            Remove-Item -LiteralPath $chunkPath -Force
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

function ConvertTo-ImageCategoryDtoValue {
    param([string]$Value)

    switch ($Value.Trim()) {
        'Avatar' { return 'AVATAR' }
        'AVATAR' { return 'AVATAR' }
        'Logo' { return 'LOGO' }
        'LOGO' { return 'LOGO' }
        'Park' { return 'PARK' }
        'PARK' { return 'PARK' }
        'Attraction' { return 'PARK_ITEM' }
        'ParkItem' { return 'PARK_ITEM' }
        'PARK_ITEM' { return 'PARK_ITEM' }
        'Operator' { return 'OPERATOR' }
        'OPERATOR' { return 'OPERATOR' }
        'Manufacturer' { return 'MANUFACTURER' }
        'MANUFACTURER' { return 'MANUFACTURER' }
        'Founder' { return 'FOUNDER' }
        'FOUNDER' { return 'FOUNDER' }
        'VideoThumbnail' { return 'VIDEO_THUMBNAIL' }
        'VIDEO_THUMBNAIL' { return 'VIDEO_THUMBNAIL' }
        'StandaloneAttraction' { return 'STANDALONE_ATTRACTION' }
        'STANDALONE_ATTRACTION' { return 'STANDALONE_ATTRACTION' }
        'Comment' { return 'COMMENT' }
        'COMMENT' { return 'COMMENT' }
        default { throw "Unsupported image category '$Value'." }
    }
}

function ConvertTo-ImageOwnerTypeDtoValue {
    param([string]$Value)

    switch ($Value.Trim()) {
        'None' { return 'NONE' }
        'NONE' { return 'NONE' }
        'Park' { return 'PARK' }
        'PARK' { return 'PARK' }
        'User' { return 'USER' }
        'USER' { return 'USER' }
        'Attraction' { return 'PARK_ITEM' }
        'ParkItem' { return 'PARK_ITEM' }
        'PARK_ITEM' { return 'PARK_ITEM' }
        'ParkOperator' { return 'PARK_OPERATOR' }
        'PARK_OPERATOR' { return 'PARK_OPERATOR' }
        'AttractionManufacturer' { return 'ATTRACTION_MANUFACTURER' }
        'ATTRACTION_MANUFACTURER' { return 'ATTRACTION_MANUFACTURER' }
        'ParkFounder' { return 'PARK_FOUNDER' }
        'PARK_FOUNDER' { return 'PARK_FOUNDER' }
        'Video' { return 'VIDEO' }
        'VIDEO' { return 'VIDEO' }
        'StandaloneAttraction' { return 'STANDALONE_ATTRACTION' }
        'STANDALONE_ATTRACTION' { return 'STANDALONE_ATTRACTION' }
        'CommentDraft' { return 'COMMENT_DRAFT' }
        'COMMENT_DRAFT' { return 'COMMENT_DRAFT' }
        'Comment' { return 'COMMENT' }
        'COMMENT' { return 'COMMENT' }
        default { throw "Unsupported image owner type '$Value'." }
    }
}

function Assert-CompleteImageMetadata {
    param([object]$Metadata)

    $requiredProperties = @(
        'category',
        'ownerType',
        'ownerId',
        'isCurrent',
        'description',
        'geoLocation',
        'altTexts',
        'captions',
        'credits',
        'tagIds',
        'isPublished',
        'sourceUrl'
    )
    foreach ($propertyName in $requiredProperties) {
        if ($null -eq $Metadata.PSObject.Properties[$propertyName]) {
            throw "MetadataJsonPath must contain the complete '$propertyName' field so existing image metadata is not cleared accidentally."
        }
    }

    foreach ($propertyName in @('category', 'ownerType', 'ownerId', 'description', 'sourceUrl')) {
        if ([string]::IsNullOrWhiteSpace([string]$Metadata.$propertyName)) {
            throw "MetadataJsonPath field '$propertyName' cannot be empty."
        }
    }

    $expectedLanguages = @('de', 'en', 'es', 'fr', 'it', 'nl', 'pl', 'pt')
    foreach ($propertyName in @('altTexts', 'captions', 'credits')) {
        $localizedValues = @($Metadata.$propertyName)
        $actualLanguages = @($localizedValues | ForEach-Object { [string]$_.languageCode } | Sort-Object -Unique)
        $missingLanguages = @($expectedLanguages | Where-Object { $_ -notin $actualLanguages })
        $unexpectedLanguages = @($actualLanguages | Where-Object { $_ -notin $expectedLanguages })
        $emptyLanguages = @($localizedValues | Where-Object { [string]::IsNullOrWhiteSpace([string]$_.value) } | ForEach-Object { [string]$_.languageCode })
        if ($localizedValues.Count -ne 8 -or $missingLanguages.Count -gt 0 -or
            $unexpectedLanguages.Count -gt 0 -or $emptyLanguages.Count -gt 0) {
            throw "MetadataJsonPath field '$propertyName' must contain one non-empty value for each of de, en, es, fr, it, nl, pl and pt."
        }
    }
}

function Assert-ImageMetadataIdentity {
    param([object]$Metadata, [string]$TargetImageId)

    $identityValues = @()
    foreach ($propertyName in @('imageId', 'id')) {
        if ($null -ne $Metadata.PSObject.Properties[$propertyName] -and
            -not [string]::IsNullOrWhiteSpace([string]$Metadata.$propertyName)) {
            $identityValues += [string]$Metadata.$propertyName
        }
    }

    if ($identityValues.Count -eq 0) {
        throw 'MetadataJsonPath must contain imageId or id from the exported image so the target identity can be verified.'
    }

    foreach ($identityValue in $identityValues) {
        if (-not [string]::Equals($identityValue, $TargetImageId, [StringComparison]::OrdinalIgnoreCase)) {
            throw "MetadataJsonPath targets image '$identityValue', not route image '$TargetImageId'."
        }
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
    try {
        $client.DefaultRequestHeaders.Authorization = [System.Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer', $token)
        $uploadFileName = switch ($ContentType) {
            'image/jpeg' { 'codex-import.jpg' }
            'image/png' { 'codex-import.png' }
            'image/webp' { 'codex-import.webp' }
            default { throw "Unsupported image content type $ContentType." }
        }
        $uri = (Get-NormalizedApiBaseUrl $ApiBaseUrl) + 'park-data-editor/images'
        $deadlineUtc = [DateTime]::UtcNow.AddMinutes(10)
        while ($true) {
            Wait-ParkDataEditorAvailability | Out-Null
            $multipart = [System.Net.Http.MultipartFormDataContent]::new()
            $stream = [IO.File]::OpenRead($Path)
            $fileContent = [System.Net.Http.StreamContent]::new($stream)
            $response = $null
            try {
                $fileContent.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::new($ContentType)
                $multipart.Add($fileContent, 'File', $uploadFileName)
                $multipart.Add([System.Net.Http.StringContent]::new($Category), 'Category')
                $multipart.Add([System.Net.Http.StringContent]::new(([string]$WithWatermark).ToLowerInvariant()), 'WithWatermark')
                $multipart.Add([System.Net.Http.StringContent]::new(([string]$IsPublished).ToLowerInvariant()), 'IsPublished')
                if (-not [string]::IsNullOrWhiteSpace($Description)) {
                    $multipart.Add([System.Net.Http.StringContent]::new($Description), 'Description')
                }

                $response = $client.PostAsync($uri, $multipart).GetAwaiter().GetResult()
                $responseBody = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
                if ($response.IsSuccessStatusCode) {
                    return $responseBody | ConvertFrom-Json
                }
                if ([int]$response.StatusCode -ne 429 -or [DateTime]::UtcNow -ge $deadlineUtc) {
                    throw "Image upload failed with HTTP $([int]$response.StatusCode): $responseBody"
                }
            }
            finally {
                if ($null -ne $response) {
                    $response.Dispose()
                }
                $fileContent.Dispose()
                $stream.Dispose()
                $multipart.Dispose()
            }

            Start-Sleep -Seconds 5
        }
    }
    finally {
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
            Write-Warning "The image was uploaded as $uploadedImageId but a later step failed. Inspect the orphan and request explicit authorization before using the controlled deletion workflow."
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
    'Status' {
        Invoke-ParkDataEditorJsonApi `
            -Method GET `
            -RelativePath 'park-data-editor/operations/status' `
            -Body $null
    }
    'SearchParks' {
        $relativePath = "park-data-editor/parks?page=$Page&size=$PageSize"
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
        Wait-ParkDataEditorAvailability | Out-Null
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

        Wait-ParkDataEditorAvailability | Out-Null
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
    'PreviewDeletion' {
        $resolvedJsonPath = Resolve-RequiredFile -Path $JsonPath -ParameterName 'JsonPath'
        $jsonBody = [IO.File]::ReadAllText($resolvedJsonPath, [Text.Encoding]::UTF8) | ConvertFrom-Json
        $expectedDeletionCount = Assert-ParkDataDeletionRequest -Request $jsonBody -TargetParkId $ParkId

        Wait-ParkDataEditorAvailability | Out-Null
        $preview = Invoke-ParkDataEditorJsonApi -Method POST -RelativePath 'admin/park-graph-upserts/preview' -Body $jsonBody
        $previewOutputPath = if ([string]::IsNullOrWhiteSpace($OutputPath)) {
            Get-DefaultOutputPath -InputPath $resolvedJsonPath -Suffix 'deletion-preview'
        } else { $OutputPath }
        Write-JsonFile -Value $preview -Path $previewOutputPath

        try {
            Assert-ControlledDeletionResult `
                -Result $preview `
                -Request $jsonBody `
                -ExpectedDeletionCount $expectedDeletionCount `
                -RequireApplied $false
        }
        catch {
            throw "$($_.Exception.Message) Inspect $previewOutputPath."
        }

        $effectiveReceiptPath = if ([string]::IsNullOrWhiteSpace($ReceiptPath)) {
            Get-DefaultOutputPath -InputPath $resolvedJsonPath -Suffix 'deletion-preview-receipt'
        } else { $ReceiptPath }
        $receipt = @{
            schemaVersion = 1
            workflow = 'controlled-deletion'
            apiBaseUrl = $ApiBaseUrl
            targetParkId = $ParkId.Trim()
            jsonPath = $resolvedJsonPath
            jsonSha256 = (Get-FileHash -LiteralPath $resolvedJsonPath -Algorithm SHA256).Hash
            createdAtUtc = [DateTime]::UtcNow.ToString('o')
            operationId = $preview.operationId
            canApply = [bool]$preview.canApply
            errorCount = @($preview.errors).Count
            warningCount = @($preview.warnings).Count
            expectedDeletionCount = $expectedDeletionCount
        }
        Write-JsonFile -Value $receipt -Path $effectiveReceiptPath

        [PSCustomObject]@{ PreviewPath = $previewOutputPath; ReceiptPath = $effectiveReceiptPath; Result = $preview }
    }
    'ApplyDeletion' {
        $resolvedJsonPath = Resolve-RequiredFile -Path $JsonPath -ParameterName 'JsonPath'
        $resolvedReceiptPath = Resolve-RequiredFile -Path $ReceiptPath -ParameterName 'ReceiptPath'
        $jsonBody = [IO.File]::ReadAllText($resolvedJsonPath, [Text.Encoding]::UTF8) | ConvertFrom-Json
        $expectedDeletionCount = Assert-ParkDataDeletionRequest -Request $jsonBody -TargetParkId $ParkId
        $receipt = [IO.File]::ReadAllText($resolvedReceiptPath, [Text.Encoding]::UTF8) | ConvertFrom-Json
        $currentHash = (Get-FileHash -LiteralPath $resolvedJsonPath -Algorithm SHA256).Hash
        $receiptAge = [DateTime]::UtcNow - [DateTime]::Parse($receipt.createdAtUtc).ToUniversalTime()
        $workflowProperty = $receipt.PSObject.Properties['workflow']
        $targetParkProperty = $receipt.PSObject.Properties['targetParkId']
        $expectedCountProperty = $receipt.PSObject.Properties['expectedDeletionCount']
        if ($receipt.schemaVersion -ne 1 -or $null -eq $workflowProperty -or $workflowProperty.Value -ne 'controlled-deletion' -or
            $receipt.apiBaseUrl -ne $ApiBaseUrl -or $null -eq $targetParkProperty -or $targetParkProperty.Value -ne $ParkId.Trim() -or
            $receipt.jsonSha256 -ne $currentHash -or -not $receipt.canApply -or
            $receipt.errorCount -gt 0 -or $receipt.warningCount -gt 0 -or
            $null -eq $expectedCountProperty -or [int]$expectedCountProperty.Value -ne $expectedDeletionCount -or
            $receiptAge.TotalMinutes -gt 30 -or $receiptAge.TotalSeconds -lt 0) {
            throw 'The controlled deletion Preview receipt is invalid, stale, targets another park or API, or does not match the current JSON. Run PreviewDeletion again.'
        }

        Wait-ParkDataEditorAvailability | Out-Null
        $apply = Invoke-ParkDataEditorJsonApi -Method POST -RelativePath 'admin/park-graph-upserts/apply' -Body $jsonBody
        $applyOutputPath = if ([string]::IsNullOrWhiteSpace($OutputPath)) {
            Get-DefaultOutputPath -InputPath $resolvedJsonPath -Suffix 'deletion-apply'
        } else { $OutputPath }
        Write-JsonFile -Value $apply -Path $applyOutputPath

        try {
            Assert-ControlledDeletionResult `
                -Result $apply `
                -Request $jsonBody `
                -ExpectedDeletionCount $expectedDeletionCount `
                -RequireApplied $true
        }
        catch {
            throw "$($_.Exception.Message) Inspect $applyOutputPath."
        }

        [PSCustomObject]@{ ApplyPath = $applyOutputPath; Result = $apply }
    }
    'Completeness' {
        if ([string]::IsNullOrWhiteSpace($ParkId)) {
            throw 'ParkId is required for Completeness.'
        }
        $projectionQuery = if ($ProjectForPublication) { '?projectForPublication=true' } else { '' }
        Invoke-ParkDataEditorJsonApi -Method GET `
            -RelativePath "park-data-editor/parks/$([Uri]::EscapeDataString($ParkId))/data-completeness$projectionQuery" `
            -Body $null
    }
    'ImportPhoto' {
        Import-ParkPhoto
    }
    'UpdatePhotoMetadata' {
        if ([string]::IsNullOrWhiteSpace($ImageId) -or [string]::IsNullOrWhiteSpace($MetadataJsonPath)) {
            throw 'ImageId and MetadataJsonPath are required for UpdatePhotoMetadata.'
        }

        $resolvedMetadataPath = Resolve-RequiredFile -Path $MetadataJsonPath -ParameterName 'MetadataJsonPath'
        $metadata = [IO.File]::ReadAllText($resolvedMetadataPath, [Text.Encoding]::UTF8) | ConvertFrom-Json
        Assert-ImageMetadataIdentity -Metadata $metadata -TargetImageId $ImageId
        Assert-CompleteImageMetadata -Metadata $metadata
        $metadata.PSObject.Properties.Remove('imageId')
        $metadata.PSObject.Properties.Remove('id')
        Set-JsonProperty -Object $metadata -Name 'category' -Value (ConvertTo-ImageCategoryDtoValue -Value ([string]$metadata.category))
        Set-JsonProperty -Object $metadata -Name 'ownerType' -Value (ConvertTo-ImageOwnerTypeDtoValue -Value ([string]$metadata.ownerType))
        Wait-ParkDataEditorAvailability | Out-Null
        Invoke-ParkDataEditorJsonApi -Method PUT `
            -RelativePath "park-data-editor/images/$([Uri]::EscapeDataString($ImageId))/metadata" `
            -Body $metadata
    }
    'ResolveFacebookPublication' {
        if ([string]::IsNullOrWhiteSpace($Url)) {
            throw 'Url is required for ResolveFacebookPublication.'
        }

        $encodedUrl = [Uri]::EscapeDataString($Url.Trim())
        Invoke-ParkDataEditorJsonApi `
            -Method GET `
            -RelativePath "park-data-editor/social-publications/facebook/draft?url=$encodedUrl&page=$ImagePage&size=$ImagePageSize" `
            -Body $null
    }
    'PublishFacebook' {
        if ([string]::IsNullOrWhiteSpace($Url)) {
            throw 'Url is required for PublishFacebook.'
        }

        Wait-ParkDataEditorAvailability | Out-Null
        $publicationMessage = if ([string]::IsNullOrWhiteSpace($Message)) { $null } else { $Message.Trim() }
        $previewImageId = if ([string]::IsNullOrWhiteSpace($ImageId)) { $null } else { $ImageId.Trim() }
        Invoke-ParkDataEditorJsonApi `
            -Method POST `
            -RelativePath 'park-data-editor/social-publications/facebook' `
            -Body @{
                network = 'Facebook'
                url = $Url.Trim()
                message = $publicationMessage
                previewImageId = $previewImageId
            }
    }
    'RetryFacebookPublication' {
        if ([string]::IsNullOrWhiteSpace($ParkId) -or [string]::IsNullOrWhiteSpace($PublicationId)) {
            throw 'ParkId and PublicationId are required for RetryFacebookPublication.'
        }

        Wait-ParkDataEditorAvailability | Out-Null
        Invoke-ParkDataEditorJsonApi `
            -Method POST `
            -RelativePath "park-data-editor/parks/$([Uri]::EscapeDataString($ParkId.Trim()))/social-preview/publications/$([Uri]::EscapeDataString($PublicationId.Trim()))/retry" `
            -Body @{}
    }
    'RevokeCurrent' {
        Invoke-ParkDataEditorJsonApi -Method DELETE -RelativePath 'park-data-editor/tokens/current' -Body $null | Out-Null
        if (Test-Path -LiteralPath $script:CredentialPath -PathType Leaf) {
            Remove-Item -LiteralPath $script:CredentialPath -Force
        }
        Write-Output 'The current token was revoked on the server and removed locally.'
    }
}
