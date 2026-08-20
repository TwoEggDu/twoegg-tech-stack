param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('first', 'second')]
    [string]$RunLabel
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Test-ContainedPath {
    param([string]$Parent, [string]$Candidate)
    $resolvedParent = [System.IO.Path]::GetFullPath($Parent).TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    $resolvedCandidate = [System.IO.Path]::GetFullPath($Candidate)
    $parentPrefix = $resolvedParent + [System.IO.Path]::DirectorySeparatorChar
    return $resolvedCandidate.StartsWith($parentPrefix, [System.StringComparison]::OrdinalIgnoreCase)
}

$labRoot = [System.IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$artifactRoot = Join-Path $labRoot 'artifacts'
$statePath = Join-Path $artifactRoot ("run-state-{0}.json" -f $RunLabel)
if (-not (Test-Path -LiteralPath $statePath -PathType Leaf)) { throw "Run state missing: $statePath" }
$state = Get-Content -Raw -LiteralPath $statePath | ConvertFrom-Json
if ($state.status -ne 'READY') { throw "Run state is not cleanup eligible: $($state.status)" }

$tempParent = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
$runRoot = [System.IO.Path]::GetFullPath([string]$state.run_root)
$sentinel = Join-Path $runRoot '.lab-02-owned'
if ($runRoot -eq $tempParent) { throw "Refusing to delete temp parent: $runRoot" }
if (-not (Test-ContainedPath -Parent $tempParent -Candidate $runRoot)) { throw "Run root is outside temp parent: $runRoot" }
if ([System.IO.Path]::GetFileName($runRoot) -notlike 'agent-engineering-lab-02-*') { throw "Run root prefix mismatch: $runRoot" }
if (-not (Test-Path -LiteralPath $sentinel -PathType Leaf)) { throw "Sentinel missing: $sentinel" }

$rootItem = Get-Item -LiteralPath $runRoot -Force
if (($rootItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
    throw "Run root itself is a reparse point: $runRoot"
}

$traceName = if ($RunLabel -eq 'first') { 'observation-first.jsonl' } else { 'observation.jsonl' }
$tracePath = Join-Path $artifactRoot $traceName
$spillEvidenceRoot = Join-Path $artifactRoot ("spills\{0}" -f $RunLabel)
$viewPath = Join-Path $artifactRoot ("result-views-{0}.json" -f $RunLabel)
if (-not (Test-Path -LiteralPath $tracePath -PathType Leaf)) { throw "Trace evidence missing before cleanup: $tracePath" }
if (-not (Test-Path -LiteralPath $viewPath -PathType Leaf)) { throw "Result-view evidence missing before cleanup: $viewPath" }
if (-not (Test-Path -LiteralPath $spillEvidenceRoot -PathType Container)) { throw "Spill evidence missing before cleanup: $spillEvidenceRoot" }

$linkPath = [System.IO.Path]::GetFullPath([string]$state.link_path)
$expectedLink = [System.IO.Path]::GetFullPath((Join-Path $runRoot 'allowed\link-out'))
if ($linkPath -ne $expectedLink -or -not (Test-ContainedPath -Parent $runRoot -Candidate $linkPath)) {
    throw "Link path failed cleanup classification: $linkPath"
}
if (Test-Path -LiteralPath $linkPath) {
    $linkItem = Get-Item -LiteralPath $linkPath -Force
    if (($linkItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -eq 0) {
        throw "Expected link path is not a reparse point: $linkPath"
    }
    $finalTarget = [System.IO.Path]::GetFullPath([string]$state.link_final_target)
    if (-not (Test-ContainedPath -Parent $runRoot -Candidate $finalTarget)) {
        throw "Link final target is outside owned run root: $finalTarget"
    }
    [System.IO.Directory]::Delete($linkPath, $false)
}

$remainingReparsePoints = @(Get-ChildItem -LiteralPath $runRoot -Recurse -Force | Where-Object {
    ($_.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0
})
if ($remainingReparsePoints.Count -ne 0) {
    throw "Refusing recursive delete because reparse points remain: $($remainingReparsePoints.FullName -join ', ')"
}

Write-Output "CLEANUP_GUARD_PASS run_label=$RunLabel run_root=$runRoot temp_parent=$tempParent sentinel=$sentinel"
Remove-Item -LiteralPath $runRoot -Recurse -Force
if (Test-Path -LiteralPath $runRoot) { throw "Cleanup target still exists: $runRoot" }
Write-Output "CLEANUP_COMPLETE run_label=$RunLabel removed=$runRoot"
