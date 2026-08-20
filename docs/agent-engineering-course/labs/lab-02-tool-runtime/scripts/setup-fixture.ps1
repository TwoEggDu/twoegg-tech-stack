param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('first', 'second')]
    [string]$RunLabel
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Get-Sha256Hex {
    param([byte[]]$Bytes)
    $algorithm = [System.Security.Cryptography.SHA256]::Create()
    try {
        return ([System.BitConverter]::ToString($algorithm.ComputeHash($Bytes))).Replace('-', '')
    }
    finally {
        $algorithm.Dispose()
    }
}

function Assert-ExactFixture {
    param(
        [string]$Path,
        [int]$ExpectedLength,
        [string]$ExpectedSha256
    )
    $bytes = [System.IO.File]::ReadAllBytes($Path)
    $actualSha256 = Get-Sha256Hex -Bytes $bytes
    if ($bytes.Length -ne $ExpectedLength -or $actualSha256 -ne $ExpectedSha256) {
        throw "Fixture mismatch: path=$Path length=$($bytes.Length) sha256=$actualSha256"
    }
    Write-Output "FIXTURE_OK name=$([System.IO.Path]::GetFileName($Path)) bytes=$($bytes.Length) sha256=$actualSha256"
}

function Test-ContainedFullPath {
    param([string]$Parent, [string]$Candidate)
    $resolvedParent = [System.IO.Path]::GetFullPath($Parent).TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    $resolvedCandidate = [System.IO.Path]::GetFullPath($Candidate)
    $parentPrefix = $resolvedParent + [System.IO.Path]::DirectorySeparatorChar
    return $resolvedCandidate.StartsWith($parentPrefix, [System.StringComparison]::OrdinalIgnoreCase)
}

function Remove-FailedLinkOnly {
    param([string]$Path)
    if (Test-Path -LiteralPath $Path) {
        $item = Get-Item -LiteralPath $Path -Force
        if (($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -eq 0) {
            if ($item.PSIsContainer -and @(Get-ChildItem -LiteralPath $Path -Force).Count -eq 0) {
                [System.IO.Directory]::Delete($Path, $false)
                return
            }
            throw "Refusing to remove failed link path that is not an empty directory or reparse point: $Path"
        }
        [System.IO.Directory]::Delete($Path, $false)
    }
}

$labRoot = [System.IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$artifactRoot = Join-Path $labRoot 'artifacts'
[System.IO.Directory]::CreateDirectory($artifactRoot) | Out-Null

$tempParent = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
$rootName = 'agent-engineering-lab-02-' + [System.Guid]::NewGuid().ToString('D')
$runRoot = [System.IO.Path]::GetFullPath((Join-Path $tempParent $rootName))
if (-not (Test-ContainedFullPath -Parent $tempParent -Candidate $runRoot)) {
    throw "Unsafe temp root classification: $runRoot"
}
if ([System.IO.Path]::GetFileName($runRoot) -notlike 'agent-engineering-lab-02-*' -or $runRoot -eq $tempParent) {
    throw "Unsafe temp root name or parent: $runRoot"
}

$allowed = Join-Path $runRoot 'allowed'
$outside = Join-Path $runRoot 'outside'
$spills = Join-Path $runRoot 'spills'
$linkPath = Join-Path $allowed 'link-out'
[System.IO.Directory]::CreateDirectory($allowed) | Out-Null
[System.IO.Directory]::CreateDirectory($outside) | Out-Null
[System.IO.Directory]::CreateDirectory($spills) | Out-Null

$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllBytes((Join-Path $runRoot '.lab-02-owned'), $utf8NoBom.GetBytes("lab-02-owned`n"))
[System.IO.File]::WriteAllBytes((Join-Path $allowed 'small.txt'), $utf8NoBom.GetBytes("alpha`nbeta`n"))
$largeBytes = New-Object byte[] 1024
for ($index = 0; $index -lt $largeBytes.Length; $index++) { $largeBytes[$index] = 0x4C }
[System.IO.File]::WriteAllBytes((Join-Path $allowed 'large.txt'), $largeBytes)
[System.IO.File]::WriteAllBytes((Join-Path $outside 'secret.txt'), $utf8NoBom.GetBytes("outside-secret`n"))

Assert-ExactFixture -Path (Join-Path $allowed 'small.txt') -ExpectedLength 11 -ExpectedSha256 'E49C81E2D2F84E259D40E2FB8192F3BCD198B355184845D76D8F58807D0D78EE'
Assert-ExactFixture -Path (Join-Path $allowed 'large.txt') -ExpectedLength 1024 -ExpectedSha256 '26AD8132E3B544CAEFD85B30BF36DF8D012DC7245C9D2224E0F9F50A2AC55A61'
Assert-ExactFixture -Path (Join-Path $outside 'secret.txt') -ExpectedLength 15 -ExpectedSha256 'A532F53598B8BB67609FD55670AA58B9A1DD5F3F77E9C4FA44321533C85BAF6B'

$specDll = Join-Path $labRoot 'tests\ToolRuntimeLab.Specs\bin\Release\net10.0\ToolRuntimeLab.Specs.dll'
if (-not (Test-Path -LiteralPath $specDll -PathType Leaf)) {
    throw "Link verifier executable is missing: $specDll"
}

$linkKind = $null
$verifyJson = $null
$junctionError = $null
$symlinkError = $null

try {
    Write-Output "LINK_ATTEMPT kind=JUNCTION path=$linkPath target=$outside"
    New-Item -ItemType Junction -Path $linkPath -Target $outside -ErrorAction Stop | Out-Null
    $verifyOutput = & dotnet $specDll --verify-link $linkPath --allow-root $allowed --run-root $runRoot 2>&1
    if ($LASTEXITCODE -ne 0) { throw "Junction verifier exit=$LASTEXITCODE output=$($verifyOutput -join ' | ')" }
    $verifyJson = ($verifyOutput | Select-Object -Last 1) | ConvertFrom-Json
    $linkKind = 'JUNCTION'
}
catch {
    $junctionError = $_.Exception.Message
    Write-Output "LINK_ATTEMPT_FAILED kind=JUNCTION error=$junctionError"
    Remove-FailedLinkOnly -Path $linkPath
}

if ($null -eq $linkKind) {
    try {
        Write-Output "LINK_ATTEMPT kind=SYMLINK path=$linkPath target=$outside"
        New-Item -ItemType SymbolicLink -Path $linkPath -Target $outside -ErrorAction Stop | Out-Null
        $verifyOutput = & dotnet $specDll --verify-link $linkPath --allow-root $allowed --run-root $runRoot 2>&1
        if ($LASTEXITCODE -ne 0) { throw "Symlink verifier exit=$LASTEXITCODE output=$($verifyOutput -join ' | ')" }
        $verifyJson = ($verifyOutput | Select-Object -Last 1) | ConvertFrom-Json
        $linkKind = 'SYMLINK'
    }
    catch {
        $symlinkError = $_.Exception.Message
        Write-Output "LINK_ATTEMPT_FAILED kind=SYMLINK error=$symlinkError"
        Remove-FailedLinkOnly -Path $linkPath
    }
}

$statePath = Join-Path $artifactRoot ("run-state-{0}.json" -f $RunLabel)
if ($null -eq $linkKind) {
    $failureState = [ordered]@{
        run_label = $RunLabel
        status = 'FAILED'
        run_root = $runRoot
        junction_error = $junctionError
        symlink_error = $symlinkError
    } | ConvertTo-Json -Depth 4
    [System.IO.File]::WriteAllText($statePath, $failureState + "`n", $utf8NoBom)
    throw 'Both junction and symlink setup failed; no synthetic link disposition was created.'
}

$state = [ordered]@{
    run_label = $RunLabel
    status = 'READY'
    run_root = $runRoot
    allow_root = $allowed
    outside_root = $outside
    spill_root = $spills
    link_path = $linkPath
    link_kind = $linkKind
    link_final_target = [string]$verifyJson.final_target
    final_target_outside_allow_root = [bool]$verifyJson.outside_allow_root
    final_target_inside_run_root = [bool]$verifyJson.inside_run_root
    junction_error = $junctionError
    symlink_error = $symlinkError
}
[System.IO.File]::WriteAllText($statePath, (($state | ConvertTo-Json -Depth 4) + "`n"), $utf8NoBom)

Write-Output "SETUP_READY run_label=$RunLabel run_root=$runRoot"
Write-Output "LINK_DISPOSITION kind=$linkKind final_target=$($verifyJson.final_target) outside_allow_root=$($verifyJson.outside_allow_root) inside_run_root=$($verifyJson.inside_run_root)"
Write-Output "STATE_PATH $statePath"
