[CmdletBinding()]
param(
  [Parameter(Mandatory)][ValidateSet('DIAGNOSE_FOCUSED_BOUNDARY_ISOLATED')][string]$Mode,
  [Parameter(Mandatory)][string]$ReceiptRoot,
  [Parameter(Mandatory)][string]$AttemptIdentity,
  [Parameter(Mandatory)][ValidatePattern('^[A-Fa-f0-9]{64}$')][string]$TestSha256,
  [string]$Cycle4InputRoot,
  [string]$InjectionPhase = '',
  [string]$InvocationName = 'CYCLE5_DIAGNOSTIC_PREVALIDATION'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$Utf8NoBom = [System.Text.UTF8Encoding]::new($false)
$AllPhases = @(
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

function Get-StringSha256([string]$Value) {
  $bytes = $Utf8NoBom.GetBytes($Value)
  $sha = [System.Security.Cryptography.SHA256]::Create()
  try { return ([BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-', '') } finally { $sha.Dispose() }
}

function Write-Json([string]$Path, [object]$Value) {
  [System.IO.File]::WriteAllText($Path, (($Value | ConvertTo-Json -Depth 40) + "`n"), $Utf8NoBom)
}

function Write-Text([string]$Path, [AllowEmptyString()][string]$Value) {
  [System.IO.File]::WriteAllText($Path, $Value, $Utf8NoBom)
}

function Get-RelativePathToken([string]$Path) {
  $full = [System.IO.Path]::GetFullPath($Path)
  $workspace = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../../..')).TrimEnd('\')
  if ($full.StartsWith($workspace, [System.StringComparison]::OrdinalIgnoreCase)) {
    return '<WORKSPACE>/' + $full.Substring($workspace.Length).TrimStart('\').Replace('\', '/')
  }
  return '<EXTERNAL_SOURCE>/' + [System.IO.Path]::GetFileName($full)
}

function Sanitize-Text([AllowNull()][string]$Text, [hashtable]$Roots) {
  if ($null -eq $Text) { return [ordered]@{ value = ''; replacement_count = 0 } }
  $value = $Text
  $count = 0
  foreach ($entry in $Roots.GetEnumerator() | Sort-Object { $_.Value.Length } -Descending) {
    if ([string]::IsNullOrWhiteSpace([string]$entry.Value)) { continue }
    $escaped = [regex]::Escape([string]$entry.Value)
    $matches = ([regex]::Matches($value, $escaped, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)).Count
    if ($matches -gt 0) {
      $value = [regex]::Replace($value, $escaped, [string]$entry.Key, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
      $count += $matches
    }
  }
  return [ordered]@{ value = $value.Replace('\', '/'); replacement_count = $count }
}

function Get-SensitiveCount([string[]]$Values) {
  $pattern = '(?i)(authorization|bearer|token|password|secret|api_key|private_key)|[a-z][a-z0-9+.-]*://[^/\s:@]+:[^/\s@]+@'
  $count = 0
  foreach ($value in $Values) { if ($null -ne $value) { $count += ([regex]::Matches($value, $pattern)).Count } }
  return $count
}

function Write-Manifest([string]$Root, [string]$Status, [string]$Disposition) {
  $manifestPath = Join-Path $Root 'manifest.json'
  $prefix = $Root.TrimEnd([char[]]@('\', '/')) + [System.IO.Path]::DirectorySeparatorChar
  $entries = @(
    Get-ChildItem -LiteralPath $Root -File -Recurse -Force |
      Where-Object { -not $_.FullName.Equals($manifestPath, [System.StringComparison]::OrdinalIgnoreCase) } |
      Sort-Object FullName |
      ForEach-Object {
        [ordered]@{
          relative_path = $_.FullName.Substring($prefix.Length).Replace('\', '/')
          byte_count = $_.Length
          sha256 = Get-Sha256 $_.FullName
        }
      }
  )
  Write-Json $manifestPath ([ordered]@{
      schema_version = '36-x01-recovery-cycle-5-diagnostic-manifest-v1'
      attempt_identity = $AttemptIdentity
      status = $Status
      disposition = $Disposition
      self_excluding = $true
      excluded_relative_path = 'manifest.json'
      file_count = $entries.Count
      files = [object[]]$entries
    })
}

function Invoke-Phase([string]$Phase) {
  $script:CurrentPhase = $Phase
  if ($InjectionPhase -ceq $Phase) {
    $exception = [System.InvalidOperationException]::new("Injected isolated diagnostic failure at phase $Phase")
    $exception.Data['FailureCode'] = "CYCLE5_ISOLATED_$Phase"
    throw $exception
  }
  $script:LastCompletedPhase = $Phase
}

function Get-IsolatedFocusedState([hashtable]$Adapter) {
  Invoke-Phase 'FOCUSED_GET_FIXTURE_STATE_BEGIN'
  $fixture = $Adapter.fixture_state
  Invoke-Phase 'FOCUSED_GET_FIXTURE_STATE_END'
  Invoke-Phase 'FOCUSED_PATH_NORMALIZATION_BEGIN'
  $targetRelative = ([string]$Adapter.target_relative).Replace('\', '/')
  $rawRelative = ([string]$Adapter.raw_relative).Replace('\', '/')
  Invoke-Phase 'FOCUSED_PATH_NORMALIZATION_END'
  Invoke-Phase 'FOCUSED_READ_ONLY_GIT_SHAPES_BEGIN'
  $lsFiles = [object[]]@($Adapter.ls_files)
  $checkIgnore = $Adapter.check_ignore
  Invoke-Phase 'FOCUSED_READ_ONLY_GIT_SHAPES_END'
  Invoke-Phase 'FOCUSED_CANDIDATE_IDENTITIES_BEGIN'
  $candidateIdentities = $Adapter.candidate_identities
  Invoke-Phase 'FOCUSED_CANDIDATE_IDENTITIES_END'
  Invoke-Phase 'FOCUSED_BYTE_COMPARISON_BEGIN'
  $identityMatches = ($candidateIdentities.launcher_sha256 -ceq '2F16A499C1A799C8A15C03384C2BC41FB7C742236BD5DCF07229BA4C0415C64E')
  Invoke-Phase 'FOCUSED_BYTE_COMPARISON_END'
  Invoke-Phase 'FOCUSED_PREDICATES_BEGIN'
  $predicates = [ordered]@{
    fixture_head_matches = ($fixture.head -ceq 'cd5ef8148158c3a752a658978873241fdf8e2bbc')
    fixture_clean = [bool]$fixture.clean
    target_relative_normalized = ($targetRelative -notmatch '\\')
    raw_relative_normalized = ($rawRelative -notmatch '\\')
    ls_files_explicit_array = ($lsFiles -is [array])
    check_ignore_shape_valid = ($null -ne $checkIgnore.exit_code)
    candidate_identity_matches = $identityMatches
  }
  Invoke-Phase 'FOCUSED_PREDICATES_END'
  $result = [ordered]@{ fixture = $fixture; target_relative = $targetRelative; raw_relative = $rawRelative; ls_files = [object[]]$lsFiles; check_ignore = $checkIgnore; candidate_identities = $candidateIdentities; predicates = $predicates; all_predicates_pass = (@($predicates.Values | Where-Object { -not $_ }).Count -eq 0) }
  Invoke-Phase 'FOCUSED_RETURN'
  return $result
}

$receiptFull = [System.IO.Path]::GetFullPath($ReceiptRoot)
if (Test-Path -LiteralPath $receiptFull) { throw "Receipt root already exists: $receiptFull" }
[System.IO.Directory]::CreateDirectory($receiptFull) | Out-Null
$workspaceRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../../..')).TrimEnd('\')
$temporaryRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('a36-cycle5-isolated-' + [guid]::NewGuid().ToString('N'))
$roots = @{
  '<WORKSPACE>' = $workspaceRoot
  '<ISOLATED_ROOT>' = $temporaryRoot
  '<RECEIPT_ROOT>' = $receiptFull
  '<USER_PROFILE>' = [string]$env:USERPROFILE
  '<TEMP>' = [System.IO.Path]::GetTempPath().TrimEnd('\')
}
$started = [DateTime]::UtcNow
$script:CurrentPhase = 'NOT_STARTED'
$script:LastCompletedPhase = 'NONE'
$caught = $null
$status = 'FAIL'
$disposition = 'AMBIGUOUS_DIAGNOSTIC_FAILURE'
$hypothesisResult = 'FALSIFIED_FOR_ROOT_CAUSE_ATTRIBUTION'
$inputIdentities = @()
$adapter = $null
$focusedState = $null
$temporaryRootRemoved = $false
$candidateFull = $MyInvocation.MyCommand.Path
$candidateSha = Get-Sha256 $candidateFull

try {
  [System.IO.Directory]::CreateDirectory($temporaryRoot) | Out-Null
  if (-not [string]::IsNullOrWhiteSpace($Cycle4InputRoot)) {
    $inputRootFull = [System.IO.Path]::GetFullPath($Cycle4InputRoot)
    $manifestPath = Join-Path $inputRootFull 'manifest.json'
    $manifest = Get-Content -Raw -LiteralPath $manifestPath | ConvertFrom-Json
    $consumed = @('manifest.json', 'attempt-summary.json', 'launcher/launcher-identity.json', 'launcher/corepack-identity.json', 'launcher/package-manager.json')
    foreach ($relative in $consumed) {
      $path = Join-Path $inputRootFull $relative
      $actualSha = Get-Sha256 $path
      if ($relative -ne 'manifest.json') {
        $entry = @($manifest.files | Where-Object { $_.relative_path -ceq $relative })
        if ($entry.Count -ne 1 -or $entry[0].sha256 -cne $actualSha) { throw "Cycle 4 input manifest mismatch: $relative" }
      }
      $inputIdentities += [ordered]@{ relative_path = "cycle4-attempt-1/$relative"; byte_count = (Get-Item -LiteralPath $path).Length; sha256 = $actualSha }
    }
    $attempt = Get-Content -Raw -LiteralPath (Join-Path $inputRootFull 'attempt-summary.json') | ConvertFrom-Json
    $launcherIdentity = Get-Content -Raw -LiteralPath (Join-Path $inputRootFull 'launcher/launcher-identity.json') | ConvertFrom-Json
    $corepackIdentity = Get-Content -Raw -LiteralPath (Join-Path $inputRootFull 'launcher/corepack-identity.json') | ConvertFrom-Json
    $packageManager = Get-Content -Raw -LiteralPath (Join-Path $inputRootFull 'launcher/package-manager.json') | ConvertFrom-Json
    if ($attempt.launcher_process.exit_code -ne 90 -or $launcherIdentity.source.sha256 -cne '2F16A499C1A799C8A15C03384C2BC41FB7C742236BD5DCF07229BA4C0415C64E') { throw 'Cycle 4 input shape is not the frozen failed attempt.' }
    $adapter = [ordered]@{
      fixture_state = [ordered]@{ head = 'cd5ef8148158c3a752a658978873241fdf8e2bbc'; tag = 'dsh-v0.1.2-alpha.1'; clean = [bool]$attempt.fixture_pinned_and_clean_final }
      target_relative = 'packages/runtime/tests/agent-runtime-recovery-cycle-3.spec.ts'
      raw_relative = 'docs/agent-engineering-course/articles/36/experiments/raw/36-x01-recovery-cycle-3'
      ls_files = [object[]]@()
      check_ignore = [ordered]@{ exit_code = 0; stdout = [object[]]@(); stderr = [object[]]@() }
      candidate_identities = [ordered]@{ launcher_sha256 = $launcherIdentity.source.sha256; corepack_sha256 = $corepackIdentity.resolved_sha256; package_json_sha256 = $packageManager.package_json.sha256; package_manager = $packageManager.declared_package_manager }
    }
  } else {
    $adapter = [ordered]@{
      fixture_state = [ordered]@{ head = 'cd5ef8148158c3a752a658978873241fdf8e2bbc'; tag = 'dsh-v0.1.2-alpha.1'; clean = $true }
      target_relative = 'packages/runtime/tests/agent-runtime-recovery-cycle-3.spec.ts'
      raw_relative = 'docs/agent-engineering-course/articles/36/experiments/raw/36-x01-recovery-cycle-3'
      ls_files = [object[]]@()
      check_ignore = [ordered]@{ exit_code = 0; stdout = [object[]]@(); stderr = [object[]]@() }
      candidate_identities = [ordered]@{ launcher_sha256 = '2F16A499C1A799C8A15C03384C2BC41FB7C742236BD5DCF07229BA4C0415C64E'; corepack_sha256 = '920630A1D1EC47AEDEA7345E3C868ECDC07E191373497BBF47FBBF5942FBAD4F'; package_json_sha256 = '552FE07638754047FB3DFD29D61DDE835C2A5A618DA8B766A711BC15EAA334E2'; package_manager = 'pnpm@11.7.0' }
    }
  }

  Invoke-Phase 'BOUNDARY_ENTER'
  Invoke-Phase 'FIXTURE_STATE_BEFORE_BEGIN'
  $fixtureStateBefore = $adapter.fixture_state
  Invoke-Phase 'FIXTURE_STATE_BEFORE_END'
  Invoke-Phase 'FROZEN_CANDIDATE_IDENTITY_BEGIN'
  $frozenCandidateIdentity = $adapter.candidate_identities
  Invoke-Phase 'FROZEN_CANDIDATE_IDENTITY_END'
  Invoke-Phase 'TARGET_AND_RAW_PATHS_BEGIN'
  $targetAndRaw = [ordered]@{ target = '<PINNED_FIXTURE>/' + $adapter.target_relative; raw = '<WORKSPACE>/' + $adapter.raw_relative }
  Invoke-Phase 'TARGET_AND_RAW_PATHS_END'
  Invoke-Phase 'FOCUSED_STATE_CALL_BEGIN'
  $focusedState = Get-IsolatedFocusedState $adapter
  Invoke-Phase 'FOCUSED_STATE_CALL_END'
  Invoke-Phase 'FOCUSED_PRESTATE_RECEIPT_WRITE_BEGIN'
  $modeledReceipt = [ordered]@{ fixture_state_before = $fixtureStateBefore; frozen_candidate_identity = $frozenCandidateIdentity; target_and_raw = $targetAndRaw; focused_state = $focusedState }
  $modeledJson = $modeledReceipt | ConvertTo-Json -Depth 20
  if ([string]::IsNullOrWhiteSpace($modeledJson)) { throw 'Modeled focused-prestate serialization was empty.' }
  Invoke-Phase 'FOCUSED_PRESTATE_RECEIPT_WRITE_END'
  Invoke-Phase 'BOUNDARY_EXIT'
  $status = 'PASS'
  $disposition = 'NO_EXCEPTION_REPRODUCED'
  $hypothesisResult = 'FALSIFIED_FOR_ROOT_CAUSE_ATTRIBUTION'
} catch {
  $caught = $_
  $status = 'FAIL'
  if (-not [string]::IsNullOrWhiteSpace($InjectionPhase) -and $script:CurrentPhase -ceq $InjectionPhase) {
    $disposition = 'ISOLATED_EXCEPTION_REPRODUCED'
    $hypothesisResult = 'SUPPORTED_ONLY_IN_ISOLATED_INJECTION'
  } else {
    $disposition = 'AMBIGUOUS_DIAGNOSTIC_FAILURE'
    $hypothesisResult = 'FALSIFIED_FOR_ROOT_CAUSE_ATTRIBUTION'
  }
} finally {
  if (Test-Path -LiteralPath $temporaryRoot) { Remove-Item -LiteralPath $temporaryRoot -Recurse -Force }
  $temporaryRootRemoved = -not (Test-Path -LiteralPath $temporaryRoot)
}

$ended = [DateTime]::UtcNow
$rawMessage = if ($null -eq $caught) { 'NO_EXCEPTION' } else { [string]$caught.Exception.Message }
$rawStack = if ($null -eq $caught) { 'NO_EXCEPTION' } else { [string]$caught.ScriptStackTrace }
$sanitizedMessage = Sanitize-Text $rawMessage $roots
$sanitizedStack = Sanitize-Text $rawStack $roots
$tokenizedArgs = [object[]]@('-Mode', $Mode, '-ReceiptRoot', '<RECEIPT_ROOT>', '-AttemptIdentity', $AttemptIdentity, '-TestSha256', $TestSha256.ToUpperInvariant(), '-Cycle4InputRoot', $(if ([string]::IsNullOrWhiteSpace($Cycle4InputRoot)) { '' } else { '<WORKSPACE>/docs/agent-engineering-course/articles/36-dsh-cost-compaction-trace-cancellation-recovery/experiments/preflight/36-x01-recovery-cycle-4-orchestrator/attempt-1' }), '-InjectionPhase', $InjectionPhase, '-InvocationName', $InvocationName)
$sensitiveCount = Get-SensitiveCount @($sanitizedMessage.value, $sanitizedStack.value, $InvocationName, ($tokenizedArgs -join ' '))
if ($sensitiveCount -ne 0) {
  $status = 'FAIL'
  $disposition = 'AMBIGUOUS_DIAGNOSTIC_FAILURE'
  $hypothesisResult = 'FALSIFIED_FOR_ROOT_CAUSE_ATTRIBUTION'
  $sanitizedMessage = [ordered]@{ value = 'UNSAFE_TEXT_NOT_PERSISTED'; replacement_count = $sanitizedMessage.replacement_count }
  $sanitizedStack = [ordered]@{ value = 'UNSAFE_TEXT_NOT_PERSISTED'; replacement_count = $sanitizedStack.replacement_count }
}
$failureCode = if ($null -eq $caught) { 'NONE' } elseif ($caught.Exception.Data.Contains('FailureCode')) { [string]$caught.Exception.Data['FailureCode'] } else { 'CYCLE5_UNEXPECTED_DIAGNOSTIC_FAILURE' }
$exceptionType = if ($null -eq $caught) { 'NONE' } else { $caught.Exception.GetType().FullName }
$fqid = if ($null -eq $caught) { 'NONE' } else { [string]$caught.FullyQualifiedErrorId }

try {
  $diagnostic = [ordered]@{
    schema_version = '36-x01-recovery-cycle-5-diagnostic-result-v1'
    attempt_identity = $AttemptIdentity
    status = $status
    claim_ceiling = if ($disposition -eq 'ISOLATED_EXCEPTION_REPRODUCED') { 'ISOLATED_REPRODUCTION_CONSISTENT_WITH_CYCLE4_WINDOW_HISTORICAL_CAUSE_NOT_PROVABLE' } elseif ($disposition -eq 'NO_EXCEPTION_REPRODUCED') { 'NO_EXCEPTION_REPRODUCED_HISTORICAL_CAUSE_NOT_PROVABLE' } else { 'AMBIGUOUS_DIAGNOSTIC_FAILURE_HISTORICAL_CAUSE_NOT_PROVABLE' }
    last_completed_phase = $script:LastCompletedPhase
    precise_failing_phase = if ($null -eq $caught) { 'NONE' } else { $script:CurrentPhase }
    failure_code = $failureCode
    exception = [ordered]@{ dotnet_type = $exceptionType; fully_qualified_error_id = $fqid; sanitized_message = $sanitizedMessage.value; sanitized_script_stack = $sanitizedStack.value }
    invocation = [ordered]@{
      name = $InvocationName
      candidate_path = Get-RelativePathToken $candidateFull
      candidate_sha256 = $candidateSha
      test_sha256 = $TestSha256.ToUpperInvariant()
      executable_file_identity = [ordered]@{ kind = 'CURRENT_POWERSHELL_HOST'; version = $PSVersionTable.PSVersion.ToString(); path = '<POWERSHELL_HOST>' }
      ordered_tokenized_argument_vector = $tokenizedArgs
      argument_count = $tokenizedArgs.Count
      tokenized_working_directory = '<WORKSPACE>'
      started_utc = $started.ToString('o')
      ended_utc = $ended.ToString('o')
      duration_milliseconds = [int][Math]::Max(0, ($ended - $started).TotalMilliseconds)
      timeout_seconds = 60
      exit_code = 0
    }
    input_receipt_identities = [object[]]$inputIdentities
    execution_counts = [ordered]@{ original_launcher = 0; corepack = 0; pnpm = 0; vitest = 0; focused_target = 0; collector = 0; dsh_command = 0; network = 0 }
    cleanup = [ordered]@{ pass = $temporaryRootRemoved; isolated_root_token = '<ISOLATED_ROOT>'; isolated_root_identity_sha256 = Get-StringSha256 $temporaryRoot; temporary_root_absent = $temporaryRootRemoved; repository_write_count_outside_receipt_root = 0; dsh_fixture_write_count = 0 }
    sanitization = [ordered]@{ root_replacement_count = ($sanitizedMessage.replacement_count + $sanitizedStack.replacement_count); post_sanitization_sensitive_pattern_count = $sensitiveCount; unsafe_text_persisted = $false }
    injection = [ordered]@{ enabled = (-not [string]::IsNullOrWhiteSpace($InjectionPhase)); phase = $InjectionPhase }
    hypothesis_result = $hypothesisResult
    disposition = $disposition
    historical_cycle4_root_cause = 'NOT_PROVABLE'
    new_launcher_attempt_authorized = $false
    lab_execute_authorized = $false
  }
  Write-Json (Join-Path $receiptFull 'diagnostic-result.json') $diagnostic
  Write-Json (Join-Path $receiptFull 'source-input-identities.json') ([ordered]@{ schema_version = '36-x01-recovery-cycle-5-source-input-identities-v1'; candidate = [ordered]@{ path = Get-RelativePathToken $candidateFull; byte_count = (Get-Item -LiteralPath $candidateFull).Length; sha256 = $candidateSha }; test = [ordered]@{ path = 'experiments/36-x01-recovery-cycle-5-diagnostic.Tests.ps1'; sha256 = $TestSha256.ToUpperInvariant() }; cycle4_inputs = [object[]]$inputIdentities })
  Write-Json (Join-Path $receiptFull 'exact-invocation.json') ([ordered]@{ schema_version = '36-x01-recovery-cycle-5-exact-invocation-v1'; executable = '<POWERSHELL_HOST>'; ordered_tokenized_argument_vector = $tokenizedArgs; argument_count = $tokenizedArgs.Count; working_directory = '<WORKSPACE>'; original_launcher_invocation_count = 0 })
  Write-Text (Join-Path $receiptFull 'stdout.txt') ''
  Write-Text (Join-Path $receiptFull 'stderr.txt') ''
  Write-Json (Join-Path $receiptFull 'cleanup-receipt.json') ([ordered]@{ schema_version = '36-x01-recovery-cycle-5-cleanup-v1'; temporary_root_absent = $temporaryRootRemoved; isolated_root_token = '<ISOLATED_ROOT>'; repository_write_count_outside_receipt_root = 0; dsh_fixture_write_count = 0; cleanup_pass = $temporaryRootRemoved })
  Write-Manifest $receiptFull $status $disposition
} catch {
  $emergency = [ordered]@{
    schema_version = '36-x01-recovery-cycle-5-emergency-receipt-v1'
    status = 'FAIL'
    failure_code = 'CYCLE5_DIAGNOSTIC_RECEIPT_FINALIZATION_FAILURE'
    original_failure_code = $failureCode
    original_precise_failing_phase = if ($null -eq $caught) { 'NONE' } else { $script:CurrentPhase }
    historical_cycle4_root_cause = 'NOT_PROVABLE'
    new_launcher_attempt_authorized = $false
  }
  Write-Json (Join-Path $receiptFull 'emergency-receipt.json') $emergency
  throw
}
