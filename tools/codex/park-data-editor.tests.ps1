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
