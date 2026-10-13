[CmdletBinding(DefaultParameterSetName = 'Phase')]
param(
  [Parameter(Mandatory = $true, ParameterSetName = 'Phase')]
  [ValidateSet('RED', 'GREEN')]
  [string]$Phase,

  [Parameter(Mandatory = $true, ParameterSetName = 'Phase')]
  [string]$ReceiptRoot,

  [Parameter(ParameterSetName = 'Phase')]
  [string]$OrchestratorPath,

  [Parameter(Mandatory = $true, ParameterSetName = 'Worker')]
  [switch]$InternalWorker,

  [Parameter(Mandatory = $true, ParameterSetName = 'Worker')]
  [ValidateSet('RED', 'GREEN')]
  [string]$WorkerPhase,

  [Parameter(Mandatory = $true, ParameterSetName = 'Worker')]
  [string]$TestTemporaryRoot,

  [Parameter(ParameterSetName = 'Worker')]
  [string]$WorkerOrchestratorPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$Utf8NoBom = [System.Text.UTF8Encoding]::new($false)
$SchemaPrefix = '36-x01-recovery-cycle-4-orchestrator-tests'

function Write-TestText {
  param([string]$Path, [AllowEmptyString()][string]$Text)
  [System.IO.File]::WriteAllText($Path, $Text, $Utf8NoBom)
}

function Get-TestFileIdentity {
  param([string]$Path)
  $exists = Test-Path -LiteralPath $Path -PathType Leaf
  return [ordered]@{
    path = $Path
    exists = $exists
    byte_count = if ($exists) { ([System.IO.FileInfo](Get-Item -LiteralPath $Path)).Length } else { $null }
    sha256 = if ($exists) { (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToUpperInvariant() } else { $null }
  }
}

function Format-TestDisplayCommand {
  param([string]$Executable, [string[]]$ArgumentVector)
  $quoted = @($ArgumentVector | ForEach-Object { "'" + $_.Replace("'", "''") + "'" })
  return "& '" + $Executable.Replace("'", "''") + "'" + $(if ($quoted.Count -gt 0) { ' ' + [string]::Join(' ', $quoted) } else { '' })
}

function Assert-TestCondition {
  param([bool]$Condition, [string]$Message)
  if (-not $Condition) {
    throw [System.InvalidOperationException]::new($Message)
  }
}

function Test-OrderedVector {
  param([object[]]$Expected, [object[]]$Actual)
  $expectedArray = [object[]]@($Expected)
  $actualArray = [object[]]@($Actual)
  if ($expectedArray.Count -ne $actualArray.Count) {
    return $false
  }
  for ($index = 0; $index -lt $expectedArray.Count; $index++) {
    if ([string]$expectedArray[$index] -cne [string]$actualArray[$index]) {
      return $false
    }
  }
  return $true
}

function Invoke-RedWorker {
  param([string]$TemporaryRoot)
  [System.IO.Directory]::CreateDirectory($TemporaryRoot) | Out-Null

  $suppliedVector = [string[]]@('alpha', 'value with spaces', 'omega')
  $defectiveReceiptVector = [string[]]@()
  $argumentFailureObserved = ($suppliedVector.Count -gt 0 -and $defectiveReceiptVector.Count -eq 0)

  $nullMemberList = [string[]]$null
  $joinFailureObserved = $false
  $joinFailureType = $null
  $joinFailureMessage = $null
  try {
    [string]::Join('|', $nullMemberList) | Out-Null
  } catch {
    $joinFailureObserved = $true
    $joinFailureType = $_.Exception.GetType().FullName
    $joinFailureMessage = $_.Exception.Message
  }

  $payload = [ordered]@{
    schema_version = "$SchemaPrefix-red-worker-v1"
    mode = 'RED'
    attempt_identity = '36-X01-RECOVERY-CYCLE-4-ORCHESTRATOR-TEST-RED'
    status = 'EXPECTED_FAIL'
    failure_code = 'EXPECTED_DEFECT_WITNESSES_OBSERVED'
    failure_message = 'Both deliberate Cycle 4 RED witnesses reproduced their specified failure shapes.'
    historical_orchestration_source = 'MISSING_NOT_PROVABLE'
    witness_is_historical_reconstruction = $false
    supplied_argument_vector = $suppliedVector
    supplied_argument_count = $suppliedVector.Count
    defective_receipt_argument_vector = $defectiveReceiptVector
    defective_receipt_argument_count = $defectiveReceiptVector.Count
    argument_vector_drop_failure_observed = $argumentFailureObserved
    zero_member_input_was_null = ($null -eq $nullMemberList)
    defective_join_failure_observed = $joinFailureObserved
    defective_join_failure_type = $joinFailureType
    defective_join_failure_message = $joinFailureMessage
    dsh_access_count = 0
    launcher_access_count = 0
    corepack_access_count = 0
    pnpm_access_count = 0
    vitest_access_count = 0
    collector_access_count = 0
  }
  [Console]::Out.WriteLine(($payload | ConvertTo-Json -Depth 8 -Compress))
  if ($argumentFailureObserved -and $joinFailureObserved) {
    exit 41
  }
  exit 42
}

function Invoke-GreenWorker {
  param([string]$TemporaryRoot, [string]$CandidatePath)
  if ([string]::IsNullOrWhiteSpace($CandidatePath)) {
    throw 'GREEN requires WorkerOrchestratorPath.'
  }
  $candidateFull = [System.IO.Path]::GetFullPath($CandidatePath)
  if (-not (Test-Path -LiteralPath $candidateFull -PathType Leaf)) {
    throw "GREEN candidate is missing: $candidateFull"
  }
  [System.IO.Directory]::CreateDirectory($TemporaryRoot) | Out-Null
  . $candidateFull -LibraryMode

  $nativeCommand = Get-Command -Name 'Invoke-Cycle4CapturedProcess' -CommandType Function -ErrorAction Stop
  $parameterNames = [string[]]@($nativeCommand.Parameters.Keys)
  $parameterExact = ($parameterNames -contains 'ArgumentVector' -and -not ($parameterNames -contains 'args'))

  $childScript = Join-Path $TemporaryRoot 'argument-child.ps1'
  $childReceipt = Join-Path $TemporaryRoot 'argument-child-received.json'
  $childText = @'
param([string]$OutputPath)
$Utf8NoBom = [System.Text.UTF8Encoding]::new($false)
$received = [string[]]@($args)
$json = [ordered]@{ received_argument_vector = $received; received_argument_count = $received.Count } | ConvertTo-Json -Depth 4
[System.IO.File]::WriteAllText($OutputPath, $json, $Utf8NoBom)
'@
  Write-TestText -Path $childScript -Text $childText

  $pwshPath = [System.IO.Path]::GetFullPath((Get-Process -Id $PID).Path)
  $payloadVector = [string[]]@('alpha', 'value with spaces', 'omega')
  $nativeVector = [string[]]@('-NoLogo', '-NoProfile', '-File', $childScript, $childReceipt) + $payloadVector
  $nativeRecord = Invoke-Cycle4CapturedProcess -Executable $pwshPath -ArgumentVector $nativeVector -WorkingDirectory $TemporaryRoot -TimeoutSeconds 30
  Assert-TestCondition -Condition ($nativeRecord.exit_code -eq 0) -Message 'The safe local argument child exited nonzero.'
  Assert-TestCondition -Condition (Test-Path -LiteralPath $childReceipt -PathType Leaf) -Message 'The safe local argument child did not write its receipt.'
  $received = Get-Content -Raw -LiteralPath $childReceipt | ConvertFrom-Json
  $receiptVector = [object[]]@($nativeRecord.argument_vector)
  $childVector = [object[]]@($received.received_argument_vector)
  $receiptVectorExact = Test-OrderedVector -Expected $nativeVector -Actual $receiptVector
  $childVectorExact = Test-OrderedVector -Expected $payloadVector -Actual $childVector

  $closureRoot = Join-Path $TemporaryRoot 'closure'
  $nestedRoot = Join-Path $closureRoot 'launcher'
  [System.IO.Directory]::CreateDirectory($nestedRoot) | Out-Null
  $nestedManifestPath = Join-Path $nestedRoot 'manifest.json'
  $nestedManifest = Write-Cycle4SelfExcludingManifest -Root $nestedRoot -ManifestPath $nestedManifestPath -SchemaVersion '36-x01-recovery-cycle-4-test-nested-manifest-v1' -Mode 'GREEN' -AttemptIdentity '36-X01-RECOVERY-CYCLE-4-ORCHESTRATOR-TEST-GREEN' -Status 'PASS' -FailureCode $null -FailureMessage $null
  $nestedVerification = Test-Cycle4SelfExcludingManifest -Root $nestedRoot -ManifestPath $nestedManifestPath

  $topManifestPath = Join-Path $closureRoot 'manifest.json'
  $topManifest = Write-Cycle4SelfExcludingManifest -Root $closureRoot -ManifestPath $topManifestPath -SchemaVersion '36-x01-recovery-cycle-4-test-top-manifest-v1' -Mode 'GREEN' -AttemptIdentity '36-X01-RECOVERY-CYCLE-4-ORCHESTRATOR-TEST-GREEN' -Status 'PASS' -FailureCode $null -FailureMessage $null
  $topVerification = Test-Cycle4SelfExcludingManifest -Root $closureRoot -ManifestPath $topManifestPath
  $topPaths = [string[]]@($topManifest.files | ForEach-Object { $_.relative_path })
  $topIncludesNested = ($topPaths -contains 'launcher/manifest.json')
  $topExcludesSelf = -not ($topPaths -contains 'manifest.json')

  $allPass = ($parameterExact -and $receiptVectorExact -and $childVectorExact -and
    $nativeRecord.argument_count -eq $nativeVector.Count -and
    $nestedManifest.file_count -eq 0 -and @($nestedManifest.files).Count -eq 0 -and
    $nestedVerification.count_match -and $nestedVerification.elements_match -and $nestedVerification.pass -and
    $topIncludesNested -and $topExcludesSelf -and $topVerification.count_match -and
    $topVerification.elements_match -and $topVerification.pass)

  $payload = [ordered]@{
    schema_version = "$SchemaPrefix-green-worker-v1"
    mode = 'GREEN'
    attempt_identity = '36-X01-RECOVERY-CYCLE-4-ORCHESTRATOR-TEST-GREEN'
    status = if ($allPass) { 'PASS' } else { 'FAIL' }
    failure_code = if ($allPass) { $null } else { 'GREEN_ASSERTION_FAILED' }
    failure_message = if ($allPass) { $null } else { 'At least one corrected helper assertion failed.' }
    historical_orchestration_source = 'MISSING_NOT_PROVABLE'
    witness_is_historical_reconstruction = $false
    candidate_path = $candidateFull
    native_helper_parameter_names = $parameterNames
    native_helper_argument_vector_parameter_exact = $parameterExact
    intended_native_argument_vector = $nativeVector
    intended_native_argument_count = $nativeVector.Count
    receipt_argument_vector = $receiptVector
    receipt_argument_count = $nativeRecord.argument_count
    receipt_argument_vector_exact = $receiptVectorExact
    child_payload_argument_vector = $childVector
    child_payload_argument_count = [int]$received.received_argument_count
    child_payload_argument_vector_exact = $childVectorExact
    native_child_exit_code = $nativeRecord.exit_code
    nested_manifest = $nestedManifest
    nested_manifest_verification = $nestedVerification
    nested_zero_member_defined_array = (@($nestedManifest.files).Count -eq 0)
    top_manifest = $topManifest
    top_manifest_verification = $topVerification
    top_includes_nested_manifest = $topIncludesNested
    top_excludes_only_self = $topExcludesSelf
    count_first_element_comparison_pass = ($nestedVerification.count_match -and $nestedVerification.elements_match -and $topVerification.count_match -and $topVerification.elements_match)
    dsh_access_count = 0
    launcher_access_count = 0
    corepack_access_count = 0
    pnpm_access_count = 0
    vitest_access_count = 0
    target_spec_access_count = 0
    collector_access_count = 0
  }
  [Console]::Out.WriteLine(($payload | ConvertTo-Json -Depth 14 -Compress))
  if ($allPass) { exit 0 }
  exit 43
}

function Write-TestPhaseManifest {
  param([string]$Root, [string]$PhaseName, [string]$AttemptIdentity, [string]$Status, [AllowNull()][string]$FailureCode, [AllowNull()][string]$FailureMessage)
  $manifestPath = Join-Path $Root 'manifest.json'
  $rootPrefix = $Root.TrimEnd([char[]]@('\', '/')) + [System.IO.Path]::DirectorySeparatorChar
  $files = [object[]]@(Get-ChildItem -LiteralPath $Root -File -Recurse -Force |
      Where-Object { -not $_.FullName.Equals($manifestPath, [System.StringComparison]::OrdinalIgnoreCase) } |
      Sort-Object FullName |
      ForEach-Object {
        [ordered]@{
          relative_path = $_.FullName.Substring($rootPrefix.Length).Replace('\', '/')
          byte_count = $_.Length
          sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToUpperInvariant()
        }
      })
  $manifest = [ordered]@{
    schema_version = "$SchemaPrefix-$($PhaseName.ToLowerInvariant())-manifest-v1"
    mode = $PhaseName
    attempt_identity = $AttemptIdentity
    status = $Status
    failure_code = $FailureCode
    failure_message = $FailureMessage
    self_excluding = $true
    excluded_relative_path = 'manifest.json'
    file_count = $files.Count
    files = $files
  }
  Write-TestText -Path $manifestPath -Text ($manifest | ConvertTo-Json -Depth 10)
}

if ($PSCmdlet.ParameterSetName -eq 'Worker') {
  if ($WorkerPhase -eq 'RED') {
    Invoke-RedWorker -TemporaryRoot ([System.IO.Path]::GetFullPath($TestTemporaryRoot))
  }
  Invoke-GreenWorker -TemporaryRoot ([System.IO.Path]::GetFullPath($TestTemporaryRoot)) -CandidatePath $WorkerOrchestratorPath
}

$receiptFull = [System.IO.Path]::GetFullPath($ReceiptRoot).TrimEnd([char[]]@('\', '/'))
$attemptIdentity = "36-X01-RECOVERY-CYCLE-4-ORCHESTRATOR-TEST-$Phase"
$tempFull = Join-Path ([System.IO.Path]::GetTempPath()) "36-x01-recovery-cycle-4-orchestrator-tests-$($Phase.ToLowerInvariant())"
$scriptFull = [System.IO.Path]::GetFullPath($PSCommandPath)
$pwshFull = [System.IO.Path]::GetFullPath((Get-Process -Id $PID).Path)
$expectedExit = if ($Phase -eq 'RED') { 41 } else { 0 }
$finalExit = 44
$phaseStatus = 'FAIL'
$failureCode = 'TEST_PHASE_NOT_COMPLETED'
$failureMessage = 'The test phase did not reach a closed result.'
$cleanupAttempted = $false
$cleanupError = $null
$processRecord = $null
$workerPayload = $null

if (Test-Path -LiteralPath $receiptFull) {
  throw "ReceiptRoot already exists; one-shot retry is forbidden: $receiptFull"
}
if (Test-Path -LiteralPath $tempFull) {
  throw "TestTemporaryRoot already exists; cleanup ownership is not provable: $tempFull"
}
[System.IO.Directory]::CreateDirectory($receiptFull) | Out-Null

$outerVector = [string[]]@('-NoLogo', '-NoProfile', '-File', $scriptFull, '-Phase', $Phase, '-ReceiptRoot', $receiptFull)
if ($Phase -eq 'GREEN') {
  $outerVector += [string[]]@('-OrchestratorPath', [System.IO.Path]::GetFullPath($OrchestratorPath))
}
Write-TestText -Path (Join-Path $receiptFull 'exact-test-command.txt') -Text ((Format-TestDisplayCommand -Executable $pwshFull -ArgumentVector $outerVector) + "`r`n")

$stdoutPath = Join-Path $receiptFull 'stdout.txt'
$stderrPath = Join-Path $receiptFull 'stderr.txt'
Write-TestText -Path $stdoutPath -Text ''
Write-TestText -Path $stderrPath -Text ''

try {
  $workerVector = [string[]]@('-NoLogo', '-NoProfile', '-File', $scriptFull, '-InternalWorker', '-WorkerPhase', $Phase, '-TestTemporaryRoot', $tempFull)
  if ($Phase -eq 'GREEN') {
    $workerVector += [string[]]@('-WorkerOrchestratorPath', [System.IO.Path]::GetFullPath($OrchestratorPath))
  }
  $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
  $startInfo.FileName = $pwshFull
  $startInfo.WorkingDirectory = (Get-Location).Path
  $startInfo.UseShellExecute = $false
  $startInfo.CreateNoWindow = $true
  $startInfo.RedirectStandardOutput = $true
  $startInfo.RedirectStandardError = $true
  $startInfo.StandardOutputEncoding = $Utf8NoBom
  $startInfo.StandardErrorEncoding = $Utf8NoBom
  foreach ($argument in $workerVector) {
    $startInfo.ArgumentList.Add($argument)
  }
  $process = [System.Diagnostics.Process]::new()
  $process.StartInfo = $startInfo
  $startedUtc = [DateTime]::UtcNow
  $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
  $started = $false
  $timedOut = $false
  $exitCode = $null
  $stdout = ''
  $stderr = ''
  try {
    $started = $process.Start()
    if (-not $started) { throw 'Internal test worker did not start.' }
    $stdoutTask = $process.StandardOutput.ReadToEndAsync()
    $stderrTask = $process.StandardError.ReadToEndAsync()
    if (-not $process.WaitForExit(60000)) {
      $timedOut = $true
      $process.Kill($true)
    }
    $process.WaitForExit()
    $stdout = $stdoutTask.GetAwaiter().GetResult()
    $stderr = $stderrTask.GetAwaiter().GetResult()
    if (-not $timedOut) { $exitCode = $process.ExitCode }
  } finally {
    $stopwatch.Stop()
    $endedUtc = [DateTime]::UtcNow
    Write-TestText -Path $stdoutPath -Text $stdout
    Write-TestText -Path $stderrPath -Text $stderr
    $processRecord = [ordered]@{
      schema_version = "$SchemaPrefix-$($Phase.ToLowerInvariant())-process-v1"
      mode = $Phase
      attempt_identity = $attemptIdentity
      status = if ($null -ne $exitCode -and $exitCode -eq $expectedExit) { 'EXPECTED_EXIT' } else { 'FAIL' }
      failure_code = if ($null -ne $exitCode -and $exitCode -eq $expectedExit) { $null } else { 'WORKER_EXIT_MISMATCH' }
      failure_message = if ($null -ne $exitCode -and $exitCode -eq $expectedExit) { $null } else { "Expected worker exit $expectedExit; observed $exitCode." }
      executable_absolute_path = $pwshFull
      argument_vector = $workerVector
      argument_count = $workerVector.Count
      display_command = Format-TestDisplayCommand -Executable $pwshFull -ArgumentVector $workerVector
      working_directory = $startInfo.WorkingDirectory
      started = $started
      started_utc = $startedUtc.ToString('o')
      ended_utc = $endedUtc.ToString('o')
      duration_milliseconds = $stopwatch.ElapsedMilliseconds
      timeout_seconds = 60
      timed_out = $timedOut
      exit_code = $exitCode
      stdout = Get-TestFileIdentity -Path $stdoutPath
      stderr = Get-TestFileIdentity -Path $stderrPath
      streams_merged = $false
    }
    $process.Dispose()
  }

  if (-not [string]::IsNullOrWhiteSpace($stdout)) {
    $workerPayload = $stdout.Trim() | ConvertFrom-Json
  }
  if ($Phase -eq 'RED') {
    $expectedWitnesses = ($null -ne $workerPayload -and
      $workerPayload.status -ceq 'EXPECTED_FAIL' -and
      $workerPayload.argument_vector_drop_failure_observed -eq $true -and
      $workerPayload.supplied_argument_count -gt 0 -and
      $workerPayload.defective_receipt_argument_count -eq 0 -and
      $workerPayload.zero_member_input_was_null -eq $true -and
      $workerPayload.defective_join_failure_observed -eq $true -and
      $workerPayload.historical_orchestration_source -ceq 'MISSING_NOT_PROVABLE' -and
      $workerPayload.witness_is_historical_reconstruction -eq $false)
    if ($exitCode -ne 41 -or -not $expectedWitnesses) {
      throw 'RED did not produce both exact expected deliberate witness failures.'
    }
    $phaseStatus = 'EXPECTED_FAIL'
    $failureCode = 'EXPECTED_DEFECT_WITNESSES_OBSERVED'
    $failureMessage = 'RED intentionally exits nonzero after both frozen witness failures are observed.'
    $finalExit = 41
  } else {
    $greenPass = ($exitCode -eq 0 -and $null -ne $workerPayload -and $workerPayload.status -ceq 'PASS' -and
      $workerPayload.native_helper_argument_vector_parameter_exact -eq $true -and
      $workerPayload.receipt_argument_vector_exact -eq $true -and
      $workerPayload.child_payload_argument_vector_exact -eq $true -and
      $workerPayload.nested_zero_member_defined_array -eq $true -and
      $workerPayload.top_includes_nested_manifest -eq $true -and
      $workerPayload.top_excludes_only_self -eq $true -and
      $workerPayload.count_first_element_comparison_pass -eq $true)
    if (-not $greenPass) {
      throw 'GREEN corrected helper assertions did not all pass.'
    }
    $phaseStatus = 'PASS'
    $failureCode = $null
    $failureMessage = $null
    $finalExit = 0
  }
} catch {
  $phaseStatus = 'FAIL'
  $failureCode = 'TEST_PHASE_FAILURE'
  $failureMessage = $_.Exception.Message
  $finalExit = 44
} finally {
  if (Test-Path -LiteralPath $tempFull -PathType Container) {
    $expectedTempFull = [System.IO.Path]::GetFullPath((Join-Path ([System.IO.Path]::GetTempPath()) "36-x01-recovery-cycle-4-orchestrator-tests-$($Phase.ToLowerInvariant())"))
    if (-not $tempFull.Equals($expectedTempFull, [System.StringComparison]::OrdinalIgnoreCase)) {
      $cleanupError = "Refused cleanup because the exact test temporary root changed: $tempFull"
    } else {
      $cleanupAttempted = $true
      try {
        Remove-Item -LiteralPath $tempFull -Recurse -Force -ErrorAction Stop
      } catch {
        $cleanupError = $_.Exception.Message
      }
    }
  }
  $tempAbsent = -not (Test-Path -LiteralPath $tempFull)
  $cleanupPass = ($tempAbsent -and $null -eq $cleanupError)
  if (-not $cleanupPass) {
    $phaseStatus = 'FAIL'
    $failureCode = 'TEST_TEMPORARY_ROOT_CLEANUP_FAILED'
    $failureMessage = $cleanupError
    $finalExit = 45
  }

  Write-TestText -Path (Join-Path $receiptFull 'exit-code.txt') -Text ([string]$finalExit + "`r`n")
  Write-TestText -Path (Join-Path $receiptFull 'process.json') -Text ($processRecord | ConvertTo-Json -Depth 12)
  $summary = [ordered]@{
    schema_version = "$SchemaPrefix-$($Phase.ToLowerInvariant())-summary-v1"
    mode = $Phase
    attempt_identity = $attemptIdentity
    status = $phaseStatus
    failure_code = $failureCode
    failure_message = $failureMessage
    historical_orchestration_source = 'MISSING_NOT_PROVABLE'
    witness_is_historical_reconstruction = $false
    expected_process_exit_code = $expectedExit
    observed_process_exit_code = if ($null -ne $processRecord) { $processRecord.exit_code } else { $null }
    receipt_root_previously_absent = $true
    test_temporary_root_previously_absent = $true
    test_temporary_root = $tempFull
    worker = $workerPayload
    claim_ceiling = 'HELPER_VERIFICATION_NOT_LAUNCHER_PREVALIDATION_NOT_DSH_RUNTIME_EVIDENCE'
  }
  Write-TestText -Path (Join-Path $receiptFull 'test-summary.json') -Text ($summary | ConvertTo-Json -Depth 16)
  $cleanup = [ordered]@{
    schema_version = "$SchemaPrefix-$($Phase.ToLowerInvariant())-cleanup-v1"
    mode = $Phase
    attempt_identity = $attemptIdentity
    status = if ($cleanupPass) { 'PASS' } else { 'FAIL' }
    failure_code = if ($cleanupPass) { $null } else { 'TEST_TEMPORARY_ROOT_CLEANUP_FAILED' }
    failure_message = $cleanupError
    cleanup_scope = 'EXACT_TEST_TEMPORARY_ROOT_ONLY'
    test_temporary_root = $tempFull
    cleanup_attempted = $cleanupAttempted
    other_cleanup_attempted = $false
    temporary_root_absent_after = $tempAbsent
    pass = $cleanupPass
  }
  Write-TestText -Path (Join-Path $receiptFull 'cleanup-receipt.json') -Text ($cleanup | ConvertTo-Json -Depth 8)
  $expectedReceiptNames = [string[]]@(
    'cleanup-receipt.json',
    'exact-test-command.txt',
    'exit-code.txt',
    'process.json',
    'stderr.txt',
    'stdout.txt',
    'test-summary.json'
  )
  $actualReceiptNames = [string[]]@(Get-ChildItem -LiteralPath $receiptFull -File -Force | Sort-Object Name | ForEach-Object { $_.Name })
  if (-not (Test-OrderedVector -Expected $expectedReceiptNames -Actual $actualReceiptNames)) {
    $phaseStatus = 'FAIL'
    $failureCode = 'TEST_RECEIPT_TREE_MISMATCH'
    $failureMessage = 'The phase did not contain exactly the required seven self-excluded receipt members.'
    $finalExit = 46
    $summary.status = $phaseStatus
    $summary.failure_code = $failureCode
    $summary.failure_message = $failureMessage
    Write-TestText -Path (Join-Path $receiptFull 'exit-code.txt') -Text ([string]$finalExit + "`r`n")
    Write-TestText -Path (Join-Path $receiptFull 'test-summary.json') -Text ($summary | ConvertTo-Json -Depth 16)
  }
  Write-TestPhaseManifest -Root $receiptFull -PhaseName $Phase -AttemptIdentity $attemptIdentity -Status $phaseStatus -FailureCode $failureCode -FailureMessage $failureMessage
}

exit $finalExit
