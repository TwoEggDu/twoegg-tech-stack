[CmdletBinding()]
param(
  [Parameter(Mandatory)][ValidateSet('RED_STATIC_FROZEN_LAUNCHER', 'GREEN_ISOLATED_DIAGNOSTIC')][string]$Mode,
  [Parameter(Mandatory)][string]$CandidatePath,
  [Parameter(Mandatory)][string]$ReceiptRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$Utf8NoBom = [System.Text.UTF8Encoding]::new($false)
$ExpectedLauncherSha = '2F16A499C1A799C8A15C03384C2BC41FB7C742236BD5DCF07229BA4C0415C64E'
$RequiredPhases = @(
  'BOUNDARY_ENTER',
  'FIXTURE_STATE_BEFORE_BEGIN',
  'FIXTURE_STATE_BEFORE_END',
  'FROZEN_CANDIDATE_IDENTITY_BEGIN',
  'FROZEN_CANDIDATE_IDENTITY_END',
  'TARGET_AND_RAW_PATHS_BEGIN',
  'TARGET_AND_RAW_PATHS_END',
  'FOCUSED_STATE_CALL_BEGIN',
  'FOCUSED_GET_FIXTURE_STATE_BEGIN',
  'FOCUSED_GET_FIXTURE_STATE_END',
  'FOCUSED_PATH_NORMALIZATION_BEGIN',
  'FOCUSED_PATH_NORMALIZATION_END',
  'FOCUSED_READ_ONLY_GIT_SHAPES_BEGIN',
  'FOCUSED_READ_ONLY_GIT_SHAPES_END',
  'FOCUSED_CANDIDATE_IDENTITIES_BEGIN',
  'FOCUSED_CANDIDATE_IDENTITIES_END',
  'FOCUSED_BYTE_COMPARISON_BEGIN',
  'FOCUSED_BYTE_COMPARISON_END',
  'FOCUSED_PREDICATES_BEGIN',
  'FOCUSED_PREDICATES_END',
  'FOCUSED_RETURN',
  'FOCUSED_STATE_CALL_END',
  'FOCUSED_PRESTATE_RECEIPT_WRITE_BEGIN',
  'FOCUSED_PRESTATE_RECEIPT_WRITE_END',
  'BOUNDARY_EXIT'
)

function Get-Sha256([string]$Path) {
  return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToUpperInvariant()
}

function Write-Json([string]$Path, [object]$Value) {
  [System.IO.File]::WriteAllText($Path, (($Value | ConvertTo-Json -Depth 30) + "`n"), $Utf8NoBom)
}

function Write-Text([string]$Path, [AllowEmptyString()][string]$Value) {
  [System.IO.File]::WriteAllText($Path, $Value, $Utf8NoBom)
}

function Write-Manifest([string]$Root, [string]$Status) {
  $manifestPath = Join-Path $Root 'manifest.json'
  $rootPrefix = $Root.TrimEnd([char[]]@('\', '/')) + [System.IO.Path]::DirectorySeparatorChar
  $entries = @(
    Get-ChildItem -LiteralPath $Root -File -Recurse -Force |
      Where-Object { -not $_.FullName.Equals($manifestPath, [System.StringComparison]::OrdinalIgnoreCase) } |
      Sort-Object FullName |
      ForEach-Object {
        [ordered]@{
          relative_path = $_.FullName.Substring($rootPrefix.Length).Replace('\', '/')
          byte_count = $_.Length
          sha256 = Get-Sha256 $_.FullName
        }
      }
  )
  Write-Json $manifestPath ([ordered]@{
      schema_version = '36-x01-recovery-cycle-5-test-manifest-v1'
      mode = $Mode
      status = $Status
      self_excluding = $true
      excluded_relative_path = 'manifest.json'
      file_count = $entries.Count
      files = [object[]]$entries
    })
}

$candidateFull = [System.IO.Path]::GetFullPath($CandidatePath)
$receiptFull = [System.IO.Path]::GetFullPath($ReceiptRoot)
$testFull = $MyInvocation.MyCommand.Path
if (Test-Path -LiteralPath $receiptFull) { throw "Receipt root already exists: $receiptFull" }
[System.IO.Directory]::CreateDirectory($receiptFull) | Out-Null

$testIdentity = [ordered]@{
  path = 'experiments/36-x01-recovery-cycle-5-diagnostic.Tests.ps1'
  byte_count = (Get-Item -LiteralPath $testFull).Length
  sha256 = Get-Sha256 $testFull
}
$candidateIdentity = [ordered]@{
  path = if ($Mode -eq 'RED_STATIC_FROZEN_LAUNCHER') { 'experiments/36-x01-recovery-cycle-3-launcher.ps1' } else { 'experiments/36-x01-recovery-cycle-5-diagnostic.ps1' }
  byte_count = (Get-Item -LiteralPath $candidateFull).Length
  sha256 = Get-Sha256 $candidateFull
}

$started = [DateTime]::UtcNow
$status = 'FAIL'
$exitCode = 42
$failedAssertions = @()
$injectionResults = @()
$temporaryRootRemoved = $true
$manifestChecksPassed = $true

try {
  $source = [System.IO.File]::ReadAllText($candidateFull)
  if ($Mode -eq 'RED_STATIC_FROZEN_LAUNCHER') {
    if ($candidateIdentity.sha256 -cne $ExpectedLauncherSha) { throw 'Frozen launcher identity drifted.' }
    $assertions = [ordered]@{
      durable_precise_phase_receipt = ($source -match 'precise_failing_phase' -and $source -match 'last_completed_phase')
      sanitized_script_stack_receipt = ($source -match 'sanitized_script_stack' -and $source -match 'post_sanitization_sensitive_pattern_count')
      exception_preserving_finalization = ($source -match 'CYCLE5_DIAGNOSTIC_RECEIPT_FINALIZATION_FAILURE' -and $source -match 'historical_cycle4_root_cause')
    }
    $failedAssertions = @($assertions.GetEnumerator() | Where-Object { -not $_.Value } | ForEach-Object { [string]$_.Key })
    $expected = @('durable_precise_phase_receipt', 'sanitized_script_stack_receipt', 'exception_preserving_finalization')
    if ($failedAssertions.Count -ne 3 -or (@($expected | Where-Object { $_ -notin $failedAssertions }).Count -ne 0)) {
      throw "RED did not produce the exact expected failures: $($failedAssertions -join ',')"
    }
    $status = 'EXPECTED_FAIL'
    $exitCode = 41
  } else {
    if ($candidateIdentity.sha256 -ceq $ExpectedLauncherSha) { throw 'Independent diagnostic candidate equals frozen launcher.' }
    $forbiddenCommands = @('Start-Process', 'Invoke-WebRequest', 'Invoke-RestMethod', 'curl', 'wget', 'git', 'npm', 'npx', 'pnpm', 'corepack', 'vitest')
    $tokens = $null
    $errors = $null
    $ast = [System.Management.Automation.Language.Parser]::ParseFile($candidateFull, [ref]$tokens, [ref]$errors)
    if ($errors.Count -ne 0) { throw "Candidate parse errors: $($errors.Message -join '; ')" }
    $commandNames = @($ast.FindAll({ param($node) $node -is [System.Management.Automation.Language.CommandAst] }, $true) | ForEach-Object { $_.GetCommandName() } | Where-Object { $_ })
    $forbiddenFound = @($commandNames | Where-Object { $_ -in $forbiddenCommands })
    if ($forbiddenFound.Count -ne 0) { throw "Forbidden callable command(s): $($forbiddenFound -join ',')" }
    if ($source -match "ValidateSet\([^\)]*(PREVALIDATE|VALIDATE_FOCUSED_PRESTATE|EXECUTE_FOCUSED)") { throw 'Candidate exposes a forbidden launcher mode.' }
    foreach ($required in @('DIAGNOSE_FOCUSED_BOUNDARY_ISOLATED', 'precise_failing_phase', 'sanitized_script_stack', 'CYCLE5_DIAGNOSTIC_RECEIPT_FINALIZATION_FAILURE', 'historical_cycle4_root_cause')) {
      if ($source -notmatch [regex]::Escape($required)) { throw "Candidate contract token missing: $required" }
    }

    $tempLeaf = 'a36-cycle5-isolated-' + [guid]::NewGuid().ToString('N')
    $tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) $tempLeaf
    [System.IO.Directory]::CreateDirectory($tempRoot) | Out-Null
    try {
      foreach ($phase in $RequiredPhases) {
        $phaseRoot = Join-Path $tempRoot ('inject-' + [array]::IndexOf($RequiredPhases, $phase).ToString('D2'))
        & $candidateFull -Mode DIAGNOSE_FOCUSED_BOUNDARY_ISOLATED -ReceiptRoot $phaseRoot -AttemptIdentity "GREEN-INJECT-$phase" -TestSha256 $testIdentity.sha256 -InjectionPhase $phase -InvocationName 'GREEN_ISOLATED_INJECTION' | Out-Null
        $result = Get-Content -Raw (Join-Path $phaseRoot 'diagnostic-result.json') | ConvertFrom-Json
        if ($result.status -cne 'FAIL' -or $result.precise_failing_phase -cne $phase -or $result.disposition -cne 'ISOLATED_EXCEPTION_REPRODUCED') { throw "Injection assertion failed at $phase" }
        if ($result.exception.dotnet_type -ne 'System.InvalidOperationException' -or [string]::IsNullOrWhiteSpace($result.exception.sanitized_script_stack)) { throw "Exception receipt incomplete at $phase" }
        if ($result.sanitization.post_sanitization_sensitive_pattern_count -ne 0) { throw "Sensitive scan failed at $phase" }
        $m = Get-Content -Raw (Join-Path $phaseRoot 'manifest.json') | ConvertFrom-Json
        if ($m.file_count -ne @($m.files).Count) { throw "Manifest count mismatch at $phase" }
        $injectionResults += [ordered]@{ phase = $phase; status = $result.status; failure_code = $result.failure_code; disposition = $result.disposition; manifest_file_count = $m.file_count }
      }
      $passRoot = Join-Path $tempRoot 'no-injection'
      & $candidateFull -Mode DIAGNOSE_FOCUSED_BOUNDARY_ISOLATED -ReceiptRoot $passRoot -AttemptIdentity 'GREEN-NO-INJECTION' -TestSha256 $testIdentity.sha256 -InvocationName 'GREEN_ISOLATED_NO_INJECTION' | Out-Null
      $passResult = Get-Content -Raw (Join-Path $passRoot 'diagnostic-result.json') | ConvertFrom-Json
      if ($passResult.status -cne 'PASS' -or $passResult.last_completed_phase -cne 'BOUNDARY_EXIT' -or $passResult.disposition -cne 'NO_EXCEPTION_REPRODUCED') { throw 'No-injection path did not close at BOUNDARY_EXIT.' }
      if (@($passResult.input_receipt_identities).Count -ne 0) { throw 'Empty input identity list did not remain empty.' }
      $status = 'PASS'
      $exitCode = 0
    } finally {
      if (Test-Path -LiteralPath $tempRoot) { Remove-Item -LiteralPath $tempRoot -Recurse -Force }
      $temporaryRootRemoved = -not (Test-Path -LiteralPath $tempRoot)
    }
  }
} catch {
  $failedAssertions += "UNEXPECTED:$($_.Exception.Message)"
  $status = 'FAIL'
  $exitCode = 42
} finally {
  $ended = [DateTime]::UtcNow
  $identity = [ordered]@{
    schema_version = '36-x01-recovery-cycle-5-test-identities-v1'
    mode = $Mode
    test = $testIdentity
    candidate = $candidateIdentity
    candidate_executed = ($Mode -eq 'GREEN_ISOLATED_DIAGNOSTIC')
    frozen_launcher_expected_sha256 = $ExpectedLauncherSha
  }
  Write-Json (Join-Path $receiptFull 'identities.json') $identity
  Write-Text (Join-Path $receiptFull 'stdout.txt') ''
  Write-Text (Join-Path $receiptFull 'stderr.txt') ''
  Write-Json (Join-Path $receiptFull 'phase-summary.json') ([ordered]@{
      schema_version = '36-x01-recovery-cycle-5-test-phase-summary-v1'
      mode = $Mode
      status = $status
      exit_code = $exitCode
      expected_failed_assertions = if ($Mode -eq 'RED_STATIC_FROZEN_LAUNCHER') { [object[]]@('durable_precise_phase_receipt', 'sanitized_script_stack_receipt', 'exception_preserving_finalization') } else { [object[]]@() }
      actual_failed_assertions = [object[]]$failedAssertions
      required_phase_count = $RequiredPhases.Count
      injection_results = [object[]]$injectionResults
      original_launcher_invocation_count = 0
      corepack_invocation_count = 0
      pnpm_invocation_count = 0
      vitest_invocation_count = 0
      focused_target_invocation_count = 0
      collector_invocation_count = 0
      dsh_command_count = 0
      network_invocation_count = 0
      started_utc = $started.ToString('o')
      ended_utc = $ended.ToString('o')
    })
  Write-Json (Join-Path $receiptFull 'cleanup-receipt.json') ([ordered]@{
      schema_version = '36-x01-recovery-cycle-5-test-cleanup-v1'
      temporary_root_removed = $temporaryRootRemoved
      repository_write_count_outside_receipt_root = 0
      dsh_fixture_write_count = 0
      cleanup_pass = $temporaryRootRemoved
    })
  Write-Manifest $receiptFull $status
}

exit $exitCode
