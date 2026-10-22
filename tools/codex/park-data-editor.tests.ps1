Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$clientPath = Join-Path $PSScriptRoot 'park-data-editor.ps1'
$tokens = $null
$parseErrors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile($clientPath, [ref]$tokens, [ref]$parseErrors)
if ($parseErrors.Count -gt 0) {
    throw "The park data editor client has PowerShell parse errors: $($parseErrors.Message -join '; ')"
}

$functionAst = $ast.Find(
    {
        param($node)
        return $node -is [Management.Automation.Language.FunctionDefinitionAst] -and
            $node.Name -eq 'New-ParkGraphExportRequest'
    },
    $true)
if ($null -eq $functionAst) {
    throw 'New-ParkGraphExportRequest was not found.'
}

Invoke-Expression $functionAst.Extent.Text

$singleSectionRequest = New-ParkGraphExportRequest `
    -TargetParkId 'park-1' `
    -RequestedSections @('Images')
$singleSectionJson = $singleSectionRequest | ConvertTo-Json -Depth 10 -Compress
if ($singleSectionJson -notmatch '"sections":\["Images"\]') {
    throw "A single requested export section must remain a JSON array. Actual JSON: $singleSectionJson"
}

$defaultRequest = New-ParkGraphExportRequest `
    -TargetParkId 'park-1' `
    -RequestedSections @()
if (@($defaultRequest.sections).Count -ne 14) {
    throw "The default export must retain all 14 sections. Actual count: $(@($defaultRequest.sections).Count)"
}

Write-Output 'Park data editor client serialization tests passed.'

$waitFunctionAst = $ast.Find(
    {
        param($node)
        return $node -is [Management.Automation.Language.FunctionDefinitionAst] -and
            $node.Name -eq 'Wait-ParkGraphExportJob'
    },
    $true)
if ($null -eq $waitFunctionAst) {
    throw 'Wait-ParkGraphExportJob was not found.'
}
Invoke-Expression $waitFunctionAst.Extent.Text

function New-TestExportSnapshot {
    param([string]$Status, [string]$DownloadUrl = '', [long]$ContentLength = 120)

    return [pscustomobject]@{
        jobId = 'existing-job'
        status = $Status
        downloadUrl = $DownloadUrl
        contentLength = $ContentLength
        error = ''
        message = 'Test snapshot'
    }
}

function Reset-ExportTest {
    param([object[]]$Responses = @())

    $script:exportResponses = [Collections.Generic.Queue[object]]::new()
    foreach ($response in $Responses) {
        $script:exportResponses.Enqueue($response)
    }
    $script:exportPollCount = 0
    $script:exportWaitCount = 0
}

function Start-Sleep {
    param([int]$Seconds)

    if ($Seconds -ne 5) {
        throw 'Export polling must retain the five-second interval.'
    }
    $script:exportWaitCount++
}

function Invoke-ParkDataEditorJsonApi {
    param([string]$Method, [string]$RelativePath, [object]$Body)

    if ($Method -ne 'GET' -or
        $RelativePath -ne 'admin/park-graph-upserts/bulk/export-jobs/existing-job' -or
        $null -ne $Body) {
        throw 'Export recovery must only read the existing job.'
    }
    $script:exportPollCount++
    return $script:exportResponses.Dequeue()
}

$downloadable = New-TestExportSnapshot -Status 'Completed' -DownloadUrl 'https://example.test/export'
Reset-ExportTest -Responses @($downloadable)
$result = Wait-ParkGraphExportJob -InitialSnapshot (New-TestExportSnapshot -Status 'Completed') -TimeoutSeconds 30
if ($result.downloadUrl -ne $downloadable.downloadUrl -or $script:exportPollCount -ne 1 -or $script:exportWaitCount -ne 1) {
    throw 'An immediately completed export must obtain its download metadata from the existing job.'
}

Reset-ExportTest
$result = Wait-ParkGraphExportJob -InitialSnapshot $downloadable -TimeoutSeconds 30
if ($result.downloadUrl -ne $downloadable.downloadUrl -or $script:exportPollCount -ne 0 -or $script:exportWaitCount -ne 0) {
    throw 'An already downloadable export must not be polled again.'
}

Reset-ExportTest -Responses @((New-TestExportSnapshot -Status 'Running'), $downloadable)
$result = Wait-ParkGraphExportJob -InitialSnapshot (New-TestExportSnapshot -Status 'Queued') -TimeoutSeconds 30
if ($result.status -ne 'Completed' -or $script:exportPollCount -ne 2 -or $script:exportWaitCount -ne 2) {
    throw 'Queued and running exports must keep their existing polling behavior.'
}

foreach ($incomplete in @(
    (New-TestExportSnapshot -Status 'Completed'),
    (New-TestExportSnapshot -Status 'Completed' -DownloadUrl 'https://example.test/export' -ContentLength 0)
)) {
    Reset-ExportTest -Responses @($incomplete)
    $message = ''
    try {
        Wait-ParkGraphExportJob -InitialSnapshot $incomplete -TimeoutSeconds 30 | Out-Null
    }
    catch {
        $message = $_.Exception.Message
    }
    if ($message -ne 'The completed park export job did not provide a valid download URL and content length.' -or
        $script:exportPollCount -ne 1 -or $script:exportWaitCount -ne 1) {
        throw 'A completed job that remains incomplete after one refresh must fail without repeated retries.'
    }
}

Reset-ExportTest
$message = ''
try {
    Wait-ParkGraphExportJob -InitialSnapshot (New-TestExportSnapshot -Status 'Completed') -TimeoutSeconds 0 | Out-Null
}
catch {
    $message = $_.Exception.Message
}
if ($message -notlike '*timed out*' -or $script:exportPollCount -ne 0 -or $script:exportWaitCount -ne 0) {
    throw 'The existing timeout must also bound the initial completed-job refresh.'
}

Reset-ExportTest
$message = ''
try {
    Wait-ParkGraphExportJob -InitialSnapshot (New-TestExportSnapshot -Status 'Failed') -TimeoutSeconds 30 | Out-Null
}
catch {
    $message = $_.Exception.Message
}
if ($message -notlike "*ended with status 'Failed'*" -or $script:exportPollCount -ne 0) {
    throw 'Failed jobs must remain terminal and must not be retried.'
}

Write-Output 'Park data editor completed-export recovery tests passed.'

& (Join-Path $PSScriptRoot 'park-data-editor.photo-import.tests.ps1')

$actionSwitch = $ast.Find({
    param($node)
    return $node -is [Management.Automation.Language.SwitchStatementAst] -and
        $node.Condition.Extent.Text -eq '$Action'
}, $true)
$searchClause = @($actionSwitch.Clauses | Where-Object { $_.Item1.Value -eq 'SearchStandaloneAttractions' })
if ($searchClause.Count -ne 1) {
    throw 'Expected one standalone-attraction search action.'
}
$searchBody = $searchClause[0].Item2.Extent.Text
$searchAction = [scriptblock]::Create($searchBody.Substring(1, $searchBody.Length - 2))
$script:standaloneSearchRequests = [Collections.Generic.List[string]]::new()
function Invoke-ParkDataEditorJsonApi {
    param([string]$Method, [string]$RelativePath, [object]$Body)
    if ($Method -ne 'GET' -or $null -ne $Body) {
        throw 'Standalone discovery must be read-only.'
    }
    $script:standaloneSearchRequests.Add($RelativePath)
}
$Page = 2
$PageSize = 25
$Query = 'DreamFly & Porto'
$CountryCode = ' pt '
& $searchAction
$Query = ' '
$CountryCode = ''
& $searchAction
if ($script:standaloneSearchRequests.Count -ne 2 -or
    $script:standaloneSearchRequests[0] -ne 'park-data-editor/standalone-attractions?page=2&size=25&query=DreamFly%20%26%20Porto&countryCode=PT' -or
    $script:standaloneSearchRequests[1] -ne 'park-data-editor/standalone-attractions?page=2&size=25') {
    throw 'Standalone discovery must preserve pagination, encode filters and omit empty optional parameters.'
}
Write-Output 'Park data editor standalone-discovery tests passed.'
