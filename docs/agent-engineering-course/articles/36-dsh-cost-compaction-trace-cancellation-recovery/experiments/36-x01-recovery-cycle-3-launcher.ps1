[CmdletBinding()]
param(
  [Parameter(Mandatory = $true)]
  [ValidateSet('PREVALIDATE', 'VALIDATE_FOCUSED_PRESTATE', 'EXECUTE_FOCUSED')]
  [string]$Mode,

  [Parameter(Mandatory = $true)]
  [string]$FixtureRoot,

  [Parameter(Mandatory = $true)]
  [string]$ReceiptRoot,

  [Parameter(Mandatory = $true)]
  [string]$AttemptIdentity,

  [string]$ExpectedCorepackPath,
  [string]$ExpectedCorepackSha256
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$PinnedFixtureRoot = 'C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814'
$PinnedFixtureHead = 'cd5ef8148158c3a752a658978873241fdf8e2bbc'
$PinnedFixtureTag = 'dsh-v0.1.2-alpha.1'
$PinnedPackageManager = 'pnpm@11.7.0'
$PinnedCorepackPath = 'C:\nvm4w\nodejs\corepack.cmd'
$PinnedCorepackSha256 = '920630A1D1EC47AEDEA7345E3C868ECDC07E191373497BBF47FBBF5942FBAD4F'
$PinnedHarnessSha256 = 'D5DFBEC5286309C4BFB1027BA490892992144D49040C6A8E2AF201B842A0199B'
$PinnedCollectorSha256 = 'D04A8929EA32CF004D011D0241474C759143B75A8E88DEDBE8DC8E09080A5045'
$TargetSpecRelative = 'packages/compaction/compaction-basic/tests/36-x01-long-session-compaction-cancel-resume.spec.ts'
$RuntimeSourceRoot = Join-Path ([System.IO.Path]::GetTempPath()) 'dsh-article36-x01-artifacts'
$Utf8NoBom = [System.Text.UTF8Encoding]::new($false)

$script:ReceiptRootFull = $null
$script:OwnsReceiptRoot = $false
$script:AttemptIdentity = $AttemptIdentity
$script:FixtureRootFull = $null
$script:FixtureBinFull = $null
$script:ResolvedCorepackPath = $null
$script:OriginalProcessPath = $env:PATH
$script:MutatedProcessPath = $null
$script:PathWasMutated = $false
$script:PathWasRestored = $false
$script:UserPathBefore = [Environment]::GetEnvironmentVariable('PATH', 'User')
$script:MachinePathBefore = [Environment]::GetEnvironmentVariable('PATH', 'Machine')
$script:ProcessRecords = [System.Collections.Generic.List[object]]::new()
$script:Status = 'FAIL'
$script:FailureCode = 'LAUNCHER_NOT_COMPLETED'
$script:FailureMessage = 'The launcher did not reach a closed result.'
$script:ExitCode = 90
$script:ResolverCompletedBeforePathMutation = $false
$script:PackageManagerValidatedBeforePathMutation = $false
$script:FixtureStateBefore = $null
$script:FixtureStateAfter = $null
$script:FrozenCandidateIdentityBefore = $null
$script:FrozenCandidateIdentityAfter = $null
$script:VitestResolution = $null
$script:LauncherIdentity = $null
$script:CorepackIdentity = $null
$script:PackageManagerReceipt = $null
$script:FocusedPrestate = $null
$script:FocusedPoststate = $null
$script:PrevalidationProbeCount = 0
$script:FocusedCommandCount = 0

function Write-Utf8Text {
  param([string]$Path, [AllowEmptyString()][string]$Text)
  [System.IO.File]::WriteAllText($Path, $Text, $Utf8NoBom)
}

function Write-JsonReceipt {
  param([string]$Name, [object]$Value)
  if ($Value -is [System.Collections.IDictionary]) {
    if (-not $Value.Contains('schema_version')) {
      $schemaLeaf = [System.IO.Path]::GetFileNameWithoutExtension($Name).Replace('_', '-').Replace(' ', '-').ToLowerInvariant()
      $Value['schema_version'] = "36-x01-recovery-cycle-3-$schemaLeaf-v2"
    }
    if (-not $Value.Contains('mode')) {
      $Value['mode'] = $Mode
    }
    if (-not $Value.Contains('attempt_identity')) {
      $Value['attempt_identity'] = $script:AttemptIdentity
    }
  }
  $json = $Value | ConvertTo-Json -Depth 12
  Write-Utf8Text -Path (Join-Path $script:ReceiptRootFull $Name) -Text $json
}

function Get-StringSha256 {
  param([AllowEmptyString()][string]$Value)
  $bytes = $Utf8NoBom.GetBytes($Value)
  $sha = [System.Security.Cryptography.SHA256]::Create()
  try {
    return ([System.BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-', '')
  } finally {
    $sha.Dispose()
  }
}

function Get-FileIdentity {
  param([string]$Path)
  $exists = Test-Path -LiteralPath $Path -PathType Leaf
  return [ordered]@{
    path = $Path
    exists = $exists
    byte_count = if ($exists) { ([System.IO.FileInfo](Get-Item -LiteralPath $Path)).Length } else { $null }
    sha256 = if ($exists) { (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToUpperInvariant() } else { $null }
  }
}

function Test-PathInside {
  param([string]$Candidate, [string]$Root)
  $candidateFull = [System.IO.Path]::GetFullPath($Candidate)
  $rootFull = [System.IO.Path]::GetFullPath($Root).TrimEnd([char[]]@('\', '/'))
  if ($candidateFull.Equals($rootFull, [System.StringComparison]::OrdinalIgnoreCase)) {
    return $true
  }
  $rootPrefix = $rootFull + [System.IO.Path]::DirectorySeparatorChar
  return $candidateFull.StartsWith($rootPrefix, [System.StringComparison]::OrdinalIgnoreCase)
}

function Stop-Closed {
  param([string]$Code, [string]$Message)
  $exception = [System.InvalidOperationException]::new($Message)
  $exception.Data['FailureCode'] = $Code
  throw $exception
}

function Format-DisplayCommand {
  param([string]$Executable, [string[]]$ArgumentVector)
  $quotedExecutable = "'" + $Executable.Replace("'", "''") + "'"
  $quotedArguments = @($ArgumentVector | ForEach-Object { "'" + $_.Replace("'", "''") + "'" })
  return '& ' + $quotedExecutable + $(if ($quotedArguments.Count -gt 0) { ' ' + [string]::Join(' ', $quotedArguments) } else { '' })
}

function Invoke-ReadOnlyGit {
  param([string[]]$ArgumentVector)
  if (@($ArgumentVector | Where-Object { $_ -match '[\s"]' }).Count -ne 0) {
    Stop-Closed 'UNSAFE_GIT_ARGUMENT' 'A frozen read-only Git argument unexpectedly requires native quoting.'
  }
  $gitCommands = @(Get-Command -Name 'git.exe' -All -CommandType Application -ErrorAction SilentlyContinue)
  if ($gitCommands.Count -lt 1) {
    Stop-Closed 'GIT_APPLICATION_MISSING' 'git.exe could not be resolved for fixture identity checks.'
  }
  $gitPath = [System.IO.Path]::GetFullPath($gitCommands[0].Source)
  $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
  $startInfo.FileName = $gitPath
  $startInfo.Arguments = [string]::Join(' ', $ArgumentVector)
  $startInfo.WorkingDirectory = $script:FixtureRootFull
  $startInfo.UseShellExecute = $false
  $startInfo.CreateNoWindow = $true
  $startInfo.RedirectStandardOutput = $true
  $startInfo.RedirectStandardError = $true
  $startInfo.StandardOutputEncoding = $Utf8NoBom
  $startInfo.StandardErrorEncoding = $Utf8NoBom
  $process = [System.Diagnostics.Process]::new()
  $process.StartInfo = $startInfo
  try {
    if (-not $process.Start()) {
      Stop-Closed 'GIT_PROCESS_START_FAILED' 'A frozen read-only Git process did not start.'
    }
    $stdoutTask = $process.StandardOutput.ReadToEndAsync()
    $stderrTask = $process.StandardError.ReadToEndAsync()
    $process.WaitForExit()
    $stdoutText = $stdoutTask.GetAwaiter().GetResult()
    $stderrText = $stderrTask.GetAwaiter().GetResult()
    $stdoutBytes = $Utf8NoBom.GetBytes($stdoutText)
    $stderrBytes = $Utf8NoBom.GetBytes($stderrText)
    return [ordered]@{
      executable_absolute_path = $gitPath
      argument_vector = @($ArgumentVector)
      display_command = Format-DisplayCommand -Executable $gitPath -ArgumentVector $ArgumentVector
      working_directory = $script:FixtureRootFull
      exit_code = $process.ExitCode
      stdout_text = $stdoutText
      stdout_byte_count = $stdoutBytes.Length
      stdout_sha256 = Get-StringSha256 -Value $stdoutText
      stdout_base64 = [Convert]::ToBase64String($stdoutBytes)
      stderr_text = $stderrText
      stderr_byte_count = $stderrBytes.Length
      stderr_sha256 = Get-StringSha256 -Value $stderrText
      stderr_base64 = [Convert]::ToBase64String($stderrBytes)
      streams_merged = $false
    }
  } finally {
    $process.Dispose()
  }
}

function Get-FixtureState {
  param([string]$Root)
  $headCommand = Invoke-ReadOnlyGit -ArgumentVector @('-C', $Root, 'rev-parse', 'HEAD')
  $tagCommand = Invoke-ReadOnlyGit -ArgumentVector @('-C', $Root, 'describe', '--tags', '--exact-match', 'HEAD')
  $unstagedDiffCommand = Invoke-ReadOnlyGit -ArgumentVector @('-C', $Root, 'diff', '--quiet', '--')
  $stagedDiffCommand = Invoke-ReadOnlyGit -ArgumentVector @('-C', $Root, 'diff', '--cached', '--quiet', '--')
  $statusCommand = Invoke-ReadOnlyGit -ArgumentVector @('-C', $Root, 'status', '--porcelain=v1', '--untracked-files=all')
  $head = $headCommand.stdout_text.Trim()
  $tag = $tagCommand.stdout_text.Trim()
  $statusNormalized = ($statusCommand.stdout_text -replace "`r`n?", "`n").Replace('\', '/').TrimEnd("`n")
  $statusLines = if ([string]::IsNullOrEmpty($statusNormalized)) { @() } else { @($statusNormalized -split "`n") }
  return [ordered]@{
    fixture_root = $Root
    head_command = $headCommand
    head = $head
    expected_head = $PinnedFixtureHead
    head_matches = ($headCommand.exit_code -eq 0 -and $head -ceq $PinnedFixtureHead)
    tag_command = $tagCommand
    exact_tag = $tag
    expected_exact_tag = $PinnedFixtureTag
    tag_matches = ($tagCommand.exit_code -eq 0 -and $tag -ceq $PinnedFixtureTag)
    unstaged_diff_command = $unstagedDiffCommand
    unstaged_diff_empty = ($unstagedDiffCommand.exit_code -eq 0)
    staged_diff_command = $stagedDiffCommand
    staged_diff_empty = ($stagedDiffCommand.exit_code -eq 0)
    status_command = $statusCommand
    status_unnormalized_text = $statusCommand.stdout_text
    status_normalized_text = $statusNormalized
    status_normalized_lines = $statusLines
    status_empty = ($statusCommand.exit_code -eq 0 -and $statusLines.Count -eq 0)
    strict_clean = ($headCommand.exit_code -eq 0 -and $tagCommand.exit_code -eq 0 -and
      $head -ceq $PinnedFixtureHead -and $tag -ceq $PinnedFixtureTag -and
      $unstagedDiffCommand.exit_code -eq 0 -and $stagedDiffCommand.exit_code -eq 0 -and
      $statusCommand.exit_code -eq 0 -and $statusLines.Count -eq 0)
  }
}

function Get-ExactFocusedFixtureState {
  param(
    [ValidateSet('PRE', 'POST')]
    [string]$Stage,
    [string]$TargetSpecPath,
    [string]$Cycle3RawRoot
  )
  $fixtureState = Get-FixtureState -Root $script:FixtureRootFull
  $targetRelative = $TargetSpecRelative.Replace('\', '/')
  $expectedStatusLine = "?? $targetRelative"
  $targetFull = [System.IO.Path]::GetFullPath($TargetSpecPath)
  $expectedTargetFull = [System.IO.Path]::GetFullPath((Join-Path $script:FixtureRootFull $TargetSpecRelative))
  $targetPathExact = $targetFull.Equals($expectedTargetFull, [System.StringComparison]::OrdinalIgnoreCase)
  $targetExists = Test-Path -LiteralPath $targetFull -PathType Leaf
  $lsFilesCommand = Invoke-ReadOnlyGit -ArgumentVector @('-C', $script:FixtureRootFull, 'ls-files', '--error-unmatch', '--', $targetRelative)
  $checkIgnoreCommand = Invoke-ReadOnlyGit -ArgumentVector @('-C', $script:FixtureRootFull, 'check-ignore', '-q', '--', $targetRelative)
  $experimentsRoot = Split-Path -Parent $PSCommandPath
  $sourcePath = [System.IO.Path]::GetFullPath((Join-Path $experimentsRoot '36-x01-recovery-cycle-2-instrumentation.ts'))
  $expectedSourcePath = $sourcePath
  $collectorPath = [System.IO.Path]::GetFullPath((Join-Path $experimentsRoot '36-x01-recovery-cycle-2-collector.ps1'))
  $sourceIdentity = Get-FileIdentity -Path $sourcePath
  $targetIdentity = Get-FileIdentity -Path $targetFull
  $collectorIdentity = Get-FileIdentity -Path $collectorPath
  $sourceBytes = if ($sourceIdentity.exists) { [System.IO.File]::ReadAllBytes($sourcePath) } else { [byte[]]@() }
  $targetBytes = if ($targetIdentity.exists) { [System.IO.File]::ReadAllBytes($targetFull) } else { [byte[]]@() }
  $bytesEqual = ($sourceIdentity.exists -and $targetIdentity.exists -and
    [Convert]::ToBase64String($sourceBytes) -ceq [Convert]::ToBase64String($targetBytes))
  $runtimeSourceAbsent = -not (Test-Path -LiteralPath $RuntimeSourceRoot)
  $runtimeSourceAbsenceRequired = -not ($Stage -eq 'POST' -and $Mode -eq 'EXECUTE_FOCUSED')
  $cycle3RawAbsent = -not (Test-Path -LiteralPath $Cycle3RawRoot)
  $statusExact = ($fixtureState.status_command.exit_code -eq 0 -and
    $fixtureState.status_normalized_lines.Count -eq 1 -and
    $fixtureState.status_normalized_lines[0] -ceq $expectedStatusLine)
  $sourcePathExact = $sourcePath.Equals($expectedSourcePath, [System.StringComparison]::OrdinalIgnoreCase)
  $predicates = [ordered]@{
    fixture_root_exact = $script:FixtureRootFull.Equals($PinnedFixtureRoot, [System.StringComparison]::OrdinalIgnoreCase)
    pinned_head_and_exact_tag = ($fixtureState.head_matches -and $fixtureState.tag_matches)
    unstaged_and_staged_tracked_diffs_empty = ($fixtureState.unstaged_diff_empty -and $fixtureState.staged_diff_empty)
    full_porcelain_exactly_one_expected_line = $statusExact
    target_exact_file_untracked_nonignored = ($targetPathExact -and $targetExists -and $lsFilesCommand.exit_code -eq 1 -and $checkIgnoreCommand.exit_code -eq 1)
    source_candidate_exact = ($sourcePathExact -and $sourceIdentity.exists -and $sourceIdentity.sha256 -ceq $PinnedHarnessSha256)
    target_bytes_count_and_sha_match_source = ($bytesEqual -and $sourceIdentity.byte_count -eq $targetIdentity.byte_count -and $targetIdentity.sha256 -ceq $PinnedHarnessSha256)
    frozen_collector_exact = ($collectorIdentity.exists -and $collectorIdentity.sha256 -ceq $PinnedCollectorSha256)
    runtime_source_and_cycle3_raw_state_acceptable = ((-not $runtimeSourceAbsenceRequired -or $runtimeSourceAbsent) -and $cycle3RawAbsent)
    corepack_package_and_vitest_identity_exact = ($null -ne $script:CorepackIdentity -and
      $script:CorepackIdentity.design_path_matches -and $script:CorepackIdentity.design_sha256_matches -and
      $null -ne $script:PackageManagerReceipt -and $script:PackageManagerReceipt.exact_match -and
      $script:PackageManagerReceipt.vitest_cmd_exists)
  }
  $allPass = @($predicates.Values | Where-Object { -not $_ }).Count -eq 0
  return [ordered]@{
    schema_version = '36-x01-recovery-cycle-3-focused-state-v2'
    stage = $Stage
    fixture_state = $fixtureState
    expected_status_line = $expectedStatusLine
    raw_status_text = $fixtureState.status_command.stdout_text
    raw_status_byte_count = $fixtureState.status_command.stdout_byte_count
    raw_status_sha256 = $fixtureState.status_command.stdout_sha256
    raw_status_base64 = $fixtureState.status_command.stdout_base64
    normalized_status_text = $fixtureState.status_normalized_text
    normalized_status_lines = @($fixtureState.status_normalized_lines)
    target_relative_path = $targetRelative
    target_absolute_path = $targetFull
    expected_target_absolute_path = $expectedTargetFull
    ls_files_command = $lsFilesCommand
    check_ignore_command = $checkIgnoreCommand
    source_candidate = $sourceIdentity
    expected_source_absolute_path = $expectedSourcePath
    target_spec = $targetIdentity
    source_target_byte_for_byte_equal = $bytesEqual
    collector = $collectorIdentity
    runtime_source_root = $RuntimeSourceRoot
    runtime_source_root_absent = $runtimeSourceAbsent
    runtime_source_absence_required = $runtimeSourceAbsenceRequired
    cycle3_raw_root = $Cycle3RawRoot
    cycle3_raw_root_absent = $cycle3RawAbsent
    corepack_identity = $script:CorepackIdentity
    package_manager_identity = $script:PackageManagerReceipt
    vitest_resolution_if_available = $script:VitestResolution
    focused_command_count = $script:FocusedCommandCount
    target_spec_execution_count = $script:FocusedCommandCount
    predicates = $predicates
    all_predicates_pass = $allPass
  }
}

function Get-FrozenCandidateIdentity {
  $experimentsRoot = Split-Path -Parent $PSCommandPath
  $harnessPath = Join-Path $experimentsRoot '36-x01-recovery-cycle-2-instrumentation.ts'
  $collectorPath = Join-Path $experimentsRoot '36-x01-recovery-cycle-2-collector.ps1'
  $harness = Get-FileIdentity -Path $harnessPath
  $collector = Get-FileIdentity -Path $collectorPath
  return [ordered]@{
    harness = $harness
    expected_harness_sha256 = $PinnedHarnessSha256
    harness_matches = ($harness.exists -and $harness.sha256 -ceq $PinnedHarnessSha256)
    collector = $collector
    expected_collector_sha256 = $PinnedCollectorSha256
    collector_matches = ($collector.exists -and $collector.sha256 -ceq $PinnedCollectorSha256)
  }
}

function Initialize-ReceiptSet {
  $textReceipts = @(
    'exact-launcher-command.txt',
    'pnpm-version-command.txt',
    'pnpm-version-stdout.txt',
    'pnpm-version-stderr.txt',
    'pnpm-version-exit-code.txt',
    'vitest-version-command.txt',
    'vitest-version-stdout.txt',
    'vitest-version-stderr.txt',
    'vitest-version-exit-code.txt'
  )
  foreach ($name in $textReceipts) {
    Write-Utf8Text -Path (Join-Path $script:ReceiptRootFull $name) -Text ''
  }
  foreach ($name in @(
      'working-directory.json',
      'launcher-identity.json',
      'corepack-identity.json',
      'package-manager.json',
      'frozen-candidate-identities.json',
      'focused-prestate.json',
      'focused-poststate.json',
      'vitest-resolution.json',
      'pnpm-version-process.json',
      'vitest-version-process.json',
      'path-scope.json',
      'bare-command-audit.json',
      'prevalidation-summary.json',
      'cleanup-receipt.json'
    )) {
    Write-JsonReceipt -Name $name -Value ([ordered]@{ status = 'NOT_COMPLETED' })
  }
}

function Invoke-CapturedProcess {
  param(
    [string]$ReceiptPrefix,
    [string]$Executable,
    [string[]]$ArgumentVector,
    [int]$TimeoutSeconds,
    [ValidateSet('PROBE', 'FOCUSED')]
    [string]$InvocationKind
  )

  if (-not [System.IO.Path]::IsPathRooted($Executable)) {
    Stop-Closed 'BARE_EXECUTABLE_REJECTED' "An actual invocation executable was not absolute: $Executable"
  }
  $executableFull = [System.IO.Path]::GetFullPath($Executable)
  if (-not $executableFull.Equals($script:ResolvedCorepackPath, [System.StringComparison]::OrdinalIgnoreCase)) {
    Stop-Closed 'COREPACK_IDENTITY_NOT_CAPTURED' "Invocation did not target the captured Corepack path: $executableFull"
  }
  if (@($ArgumentVector | Where-Object { $_ -match '[\s"]' }).Count -ne 0) {
    Stop-Closed 'UNSAFE_NATIVE_ARGUMENT' 'The frozen argument vector unexpectedly requires native quoting.'
  }

  $commandText = Format-DisplayCommand -Executable $executableFull -ArgumentVector $ArgumentVector
  Write-Utf8Text -Path (Join-Path $script:ReceiptRootFull "$ReceiptPrefix-command.txt") -Text ($commandText + "`r`n")

  $stdoutPath = Join-Path $script:ReceiptRootFull "$ReceiptPrefix-stdout.txt"
  $stderrPath = Join-Path $script:ReceiptRootFull "$ReceiptPrefix-stderr.txt"
  Write-Utf8Text -Path $stdoutPath -Text ''
  Write-Utf8Text -Path $stderrPath -Text ''

  $startedUtc = [DateTime]::UtcNow
  $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
  $process = [System.Diagnostics.Process]::new()
  $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
  $startInfo.FileName = $executableFull
  $startInfo.Arguments = [string]::Join(' ', $ArgumentVector)
  $startInfo.WorkingDirectory = $script:FixtureRootFull
  $startInfo.UseShellExecute = $false
  $startInfo.CreateNoWindow = $true
  $startInfo.RedirectStandardOutput = $true
  $startInfo.RedirectStandardError = $true
  $startInfo.StandardOutputEncoding = $Utf8NoBom
  $startInfo.StandardErrorEncoding = $Utf8NoBom
  $process.StartInfo = $startInfo

  $started = $false
  $timedOut = $false
  $exitCode = $null
  $stdoutText = ''
  $stderrText = ''
  try {
    $started = $process.Start()
    if (-not $started) {
      Stop-Closed 'PROCESS_START_FAILED' "Process.Start returned false for $ReceiptPrefix."
    }
    if ($InvocationKind -eq 'PROBE') {
      $script:PrevalidationProbeCount++
    } else {
      $script:FocusedCommandCount++
    }
    $stdoutTask = $process.StandardOutput.ReadToEndAsync()
    $stderrTask = $process.StandardError.ReadToEndAsync()
    if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
      $timedOut = $true
      $killTreeMethod = $process.GetType().GetMethod('Kill', [Type[]]@([bool]))
      if ($null -ne $killTreeMethod) {
        $killTreeMethod.Invoke($process, @($true)) | Out-Null
      } else {
        $process.Kill()
      }
    }
    $process.WaitForExit()
    $stdoutText = $stdoutTask.GetAwaiter().GetResult()
    $stderrText = $stderrTask.GetAwaiter().GetResult()
    if (-not $timedOut) {
      $exitCode = $process.ExitCode
    }
  } catch {
    $stderrText = $_.Exception.ToString()
    throw
  } finally {
    $stopwatch.Stop()
    Write-Utf8Text -Path $stdoutPath -Text $stdoutText
    Write-Utf8Text -Path $stderrPath -Text $stderrText
    Write-Utf8Text -Path (Join-Path $script:ReceiptRootFull "$ReceiptPrefix-exit-code.txt") -Text ($(if ($null -eq $exitCode) { 'NOT_AVAILABLE' } else { [string]$exitCode }) + "`r`n")
    $endedUtc = [DateTime]::UtcNow
    $stdoutIdentity = Get-FileIdentity -Path $stdoutPath
    $stderrIdentity = Get-FileIdentity -Path $stderrPath
    $record = [ordered]@{
      receipt_prefix = $ReceiptPrefix
      invocation_kind = $InvocationKind
      executable_absolute_path = $executableFull
      executable_is_absolute = [System.IO.Path]::IsPathRooted($executableFull)
      executable_matches_captured_corepack = $executableFull.Equals($script:ResolvedCorepackPath, [System.StringComparison]::OrdinalIgnoreCase)
      argument_vector = @($ArgumentVector)
      native_arguments = $startInfo.Arguments
      display_command = $commandText
      working_directory = $startInfo.WorkingDirectory
      started = $started
      started_utc = $startedUtc.ToString('o')
      ended_utc = $endedUtc.ToString('o')
      duration_milliseconds = $stopwatch.ElapsedMilliseconds
      timeout_seconds = $TimeoutSeconds
      timed_out = $timedOut
      exit_code = $exitCode
      stdout = $stdoutIdentity
      stderr = $stderrIdentity
      streams_merged = $false
    }
    $script:ProcessRecords.Add($record)
    Write-JsonReceipt -Name "$ReceiptPrefix-process.json" -Value $record
    $process.Dispose()
  }

  if ($timedOut) {
    Stop-Closed 'PROCESS_TIMEOUT' "$ReceiptPrefix exceeded its $TimeoutSeconds second cap."
  }
  return [ordered]@{
    exit_code = $exitCode
    stdout = $stdoutText
    stderr = $stderrText
  }
}

function Invoke-ExactVersionProbes {
  $pnpmProbe = Invoke-CapturedProcess -ReceiptPrefix 'pnpm-version' -Executable $script:ResolvedCorepackPath -ArgumentVector @('pnpm', '--version') -TimeoutSeconds 60 -InvocationKind 'PROBE'
  $normalizedPnpmVersion = ($pnpmProbe.stdout -replace "`r`n?", "`n").TrimEnd("`n")
  if ($pnpmProbe.exit_code -ne 0) {
    Stop-Closed 'PNPM_VERSION_EXIT_NONZERO' "Absolute Corepack pnpm --version exited $($pnpmProbe.exit_code)."
  }
  if ($normalizedPnpmVersion -cne '11.7.0') {
    Stop-Closed 'PNPM_VERSION_OUTPUT_MISMATCH' "Expected exact pnpm version 11.7.0 after newline normalization, observed '$normalizedPnpmVersion'."
  }

  $vitestProbe = Invoke-CapturedProcess -ReceiptPrefix 'vitest-version' -Executable $script:ResolvedCorepackPath -ArgumentVector @('pnpm', 'exec', 'vitest', '--version') -TimeoutSeconds 60 -InvocationKind 'PROBE'
  $normalizedVitestVersion = ($vitestProbe.stdout -replace "`r`n?", "`n").TrimEnd("`n")
  if ($vitestProbe.exit_code -ne 0) {
    Stop-Closed 'VITEST_VERSION_EXIT_NONZERO' "Absolute Corepack pnpm exec vitest --version exited $($vitestProbe.exit_code)."
  }
  if ($normalizedVitestVersion -notmatch '(?im)\bvitest(?:/|\s+v?)?\d+\.\d+\.\d+') {
    Stop-Closed 'VITEST_VERSION_OUTPUT_INVALID' 'Vitest probe stdout did not identify a Vitest semantic version.'
  }
}

function Write-SelfExcludingManifest {
  $manifestPath = Join-Path $script:ReceiptRootFull 'manifest.json'
  $files = @(Get-ChildItem -LiteralPath $script:ReceiptRootFull -File -Recurse -Force |
      Where-Object { -not $_.FullName.Equals($manifestPath, [System.StringComparison]::OrdinalIgnoreCase) } |
      Sort-Object FullName)
  $rootPrefix = $script:ReceiptRootFull.TrimEnd([char[]]@('\', '/')) + [System.IO.Path]::DirectorySeparatorChar
  $entries = @($files | ForEach-Object {
      [ordered]@{
        relative_path = $_.FullName.Substring($rootPrefix.Length).Replace('\', '/')
        byte_count = $_.Length
        sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToUpperInvariant()
      }
    })
  $manifest = [ordered]@{
    schema_version = '36-x01-recovery-cycle-3-launcher-manifest-v1'
    self_excluding = $true
    excluded_relative_path = 'manifest.json'
    generated_utc = [DateTime]::UtcNow.ToString('o')
    file_count = $entries.Count
    files = $entries
  }
  Write-JsonReceipt -Name 'manifest.json' -Value $manifest
}

try {
  $script:FixtureRootFull = [System.IO.Path]::GetFullPath($FixtureRoot).TrimEnd([char[]]@('\', '/'))
  $script:ReceiptRootFull = [System.IO.Path]::GetFullPath($ReceiptRoot).TrimEnd([char[]]@('\', '/'))
  if (Test-Path -LiteralPath $script:ReceiptRootFull) {
    throw [System.InvalidOperationException]::new("ReceiptRoot already exists; a retry is forbidden: $($script:ReceiptRootFull)")
  }
  [System.IO.Directory]::CreateDirectory($script:ReceiptRootFull) | Out-Null
  $script:OwnsReceiptRoot = $true
  Initialize-ReceiptSet

  if ([string]::IsNullOrWhiteSpace($script:AttemptIdentity)) {
    Stop-Closed 'ATTEMPT_IDENTITY_MISSING' 'AttemptIdentity must be a nonempty frozen orchestration identity.'
  }
  $launcherArguments = @('-Mode', $Mode, '-FixtureRoot', $script:FixtureRootFull, '-ReceiptRoot', $script:ReceiptRootFull, '-AttemptIdentity', $script:AttemptIdentity)
  if ($Mode -eq 'EXECUTE_FOCUSED') {
    $launcherArguments += @('-ExpectedCorepackPath', $ExpectedCorepackPath, '-ExpectedCorepackSha256', $ExpectedCorepackSha256)
  }
  $launcherCommand = Format-DisplayCommand -Executable ([System.IO.Path]::GetFullPath($PSCommandPath)) -ArgumentVector $launcherArguments
  Write-Utf8Text -Path (Join-Path $script:ReceiptRootFull 'exact-launcher-command.txt') -Text ($launcherCommand + "`r`n")

  $launcherFile = Get-FileIdentity -Path ([System.IO.Path]::GetFullPath($PSCommandPath))
  $script:LauncherIdentity = [ordered]@{
    schema_version = '36-x01-recovery-cycle-3-launcher-identity-v1'
    mode = $Mode
    source = $launcherFile
  }
  Write-JsonReceipt -Name 'launcher-identity.json' -Value $script:LauncherIdentity

  if (-not $script:FixtureRootFull.Equals($PinnedFixtureRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    Stop-Closed 'FIXTURE_ROOT_MISMATCH' "FixtureRoot is not the pinned fixture: $($script:FixtureRootFull)"
  }
  if (-not (Test-Path -LiteralPath $script:FixtureRootFull -PathType Container)) {
    Stop-Closed 'FIXTURE_ROOT_MISSING' "Pinned fixture is missing: $($script:FixtureRootFull)"
  }
  if (Test-PathInside -Candidate $script:ReceiptRootFull -Root $script:FixtureRootFull) {
    Stop-Closed 'RECEIPT_ROOT_INSIDE_FIXTURE' 'ReceiptRoot must remain outside the pinned fixture.'
  }

  $resolverStartedUtc = [DateTime]::UtcNow
  $resolvedCommands = @(Get-Command -Name 'corepack.cmd' -All -CommandType Application -ErrorAction SilentlyContinue)
  $resolverEndedUtc = [DateTime]::UtcNow
  $resolverEntries = @($resolvedCommands | ForEach-Object {
      [ordered]@{
        name = $_.Name
        command_type = [string]$_.CommandType
        source = $_.Source
        path = $_.Path
        definition = $_.Definition
      }
    })
  $resolvedPath = if ($resolvedCommands.Count -eq 1) { [System.IO.Path]::GetFullPath($resolvedCommands[0].Source) } else { $null }
  $resolvedExists = ($null -ne $resolvedPath -and (Test-Path -LiteralPath $resolvedPath -PathType Leaf))
  $resolvedHash = if ($resolvedExists) { (Get-FileHash -LiteralPath $resolvedPath -Algorithm SHA256).Hash.ToUpperInvariant() } else { $null }
  $resolvedIsAbsolute = ($null -ne $resolvedPath -and [System.IO.Path]::IsPathRooted($resolvedPath))
  $resolvedOutsideFixture = ($resolvedIsAbsolute -and -not (Test-PathInside -Candidate $resolvedPath -Root $script:FixtureRootFull))
  $script:ResolverCompletedBeforePathMutation = (-not $script:PathWasMutated)
  $corepackIdentity = [ordered]@{
    schema_version = '36-x01-recovery-cycle-3-corepack-identity-v2'
    resolver_command = "Get-Command -Name 'corepack.cmd' -All -CommandType Application"
    resolver_started_utc = $resolverStartedUtc.ToString('o')
    resolver_ended_utc = $resolverEndedUtc.ToString('o')
    resolver_completed_before_path_mutation = $script:ResolverCompletedBeforePathMutation
    process_path_sha256_at_resolution = Get-StringSha256 -Value $script:OriginalProcessPath
    resolver_count = $resolvedCommands.Count
    resolver_entries = $resolverEntries
    resolved_absolute_path = $resolvedPath
    resolved_is_absolute = $resolvedIsAbsolute
    resolved_file_exists = $resolvedExists
    resolved_outside_fixture = $resolvedOutsideFixture
    resolved_sha256 = $resolvedHash
    design_expected_path = $PinnedCorepackPath
    design_expected_sha256 = $PinnedCorepackSha256
    design_path_matches = ($null -ne $resolvedPath -and $resolvedPath.Equals($PinnedCorepackPath, [System.StringComparison]::OrdinalIgnoreCase))
    design_sha256_matches = ($null -ne $resolvedHash -and $resolvedHash -ceq $PinnedCorepackSha256)
    focused_expected_path = if ($Mode -eq 'EXECUTE_FOCUSED') { $ExpectedCorepackPath } else { $null }
    focused_expected_sha256 = if ($Mode -eq 'EXECUTE_FOCUSED') { $ExpectedCorepackSha256 } else { $null }
  }
  $script:CorepackIdentity = $corepackIdentity
  Write-JsonReceipt -Name 'corepack-identity.json' -Value $corepackIdentity

  if ($resolvedCommands.Count -ne 1) {
    Stop-Closed 'COREPACK_RESOLVER_COUNT_MISMATCH' "Expected exactly one Corepack application, observed $($resolvedCommands.Count)."
  }
  if (-not $resolvedIsAbsolute -or -not $resolvedExists -or -not $resolvedOutsideFixture) {
    Stop-Closed 'COREPACK_RESOLVER_IDENTITY_INVALID' 'Resolved Corepack must be one existing absolute file outside the fixture.'
  }
  if (-not $corepackIdentity.design_path_matches -or -not $corepackIdentity.design_sha256_matches) {
    Stop-Closed 'COREPACK_DESIGN_IDENTITY_MISMATCH' 'Resolved Corepack path or SHA-256 differs from the frozen Cycle 3 design identity.'
  }
  if ($Mode -eq 'EXECUTE_FOCUSED') {
    if ([string]::IsNullOrWhiteSpace($ExpectedCorepackPath) -or [string]::IsNullOrWhiteSpace($ExpectedCorepackSha256)) {
      Stop-Closed 'FOCUSED_EXPECTED_IDENTITY_MISSING' 'Focused mode requires the fresh-reviewed Corepack path and SHA-256.'
    }
    $expectedFocusedFull = [System.IO.Path]::GetFullPath($ExpectedCorepackPath)
    if (-not $resolvedPath.Equals($expectedFocusedFull, [System.StringComparison]::OrdinalIgnoreCase) -or -not $resolvedHash.Equals($ExpectedCorepackSha256.ToUpperInvariant(), [System.StringComparison]::Ordinal)) {
      Stop-Closed 'FOCUSED_EXPECTED_IDENTITY_MISMATCH' 'Freshly resolved Corepack does not match the reviewed focused-mode identity.'
    }
  }
  $script:ResolvedCorepackPath = $resolvedPath

  $packageJsonPath = Join-Path $script:FixtureRootFull 'package.json'
  if (-not (Test-Path -LiteralPath $packageJsonPath -PathType Leaf)) {
    Stop-Closed 'PACKAGE_JSON_MISSING' "Fixture package.json is missing: $packageJsonPath"
  }
  $packageJsonText = [System.IO.File]::ReadAllText($packageJsonPath)
  $packageJson = $packageJsonText | ConvertFrom-Json
  $declaredPackageManager = [string]$packageJson.packageManager
  $script:FixtureBinFull = [System.IO.Path]::GetFullPath((Join-Path $script:FixtureRootFull 'node_modules\.bin'))
  $vitestShimPath = [System.IO.Path]::GetFullPath((Join-Path $script:FixtureBinFull 'vitest.cmd'))
  $vitestShimExists = Test-Path -LiteralPath $vitestShimPath -PathType Leaf
  $packageManagerReceipt = [ordered]@{
    schema_version = '36-x01-recovery-cycle-3-package-manager-v2'
    package_json = Get-FileIdentity -Path $packageJsonPath
    declared_package_manager = $declaredPackageManager
    required_package_manager = $PinnedPackageManager
    exact_match = ($declaredPackageManager -ceq $PinnedPackageManager)
    fixture_bin_absolute_path = $script:FixtureBinFull
    vitest_cmd_absolute_path = $vitestShimPath
    vitest_cmd_exists = $vitestShimExists
    validated_before_path_mutation = (-not $script:PathWasMutated)
  }
  $script:PackageManagerReceipt = $packageManagerReceipt
  Write-JsonReceipt -Name 'package-manager.json' -Value $packageManagerReceipt
  if (-not $packageManagerReceipt.exact_match) {
    Stop-Closed 'PACKAGE_MANAGER_MISMATCH' "Expected exact $PinnedPackageManager, observed $declaredPackageManager."
  }
  if (-not $vitestShimExists) {
    Stop-Closed 'FIXTURE_VITEST_SHIM_MISSING' "Fixture Vitest shim is missing: $vitestShimPath"
  }
  $script:PackageManagerValidatedBeforePathMutation = (-not $script:PathWasMutated)

  $script:FixtureStateBefore = Get-FixtureState -Root $script:FixtureRootFull
  if ($Mode -eq 'PREVALIDATE' -and -not $script:FixtureStateBefore.strict_clean) {
    Stop-Closed 'STRICT_CLEAN_PREVALIDATION_PRESTATE_MISMATCH' 'PREVALIDATE requires pinned HEAD/tag, empty full status, and empty unstaged/staged tracked diffs.'
  }
  $script:FrozenCandidateIdentityBefore = Get-FrozenCandidateIdentity
  if (-not $script:FrozenCandidateIdentityBefore.harness_matches -or -not $script:FrozenCandidateIdentityBefore.collector_matches) {
    Stop-Closed 'FROZEN_CANDIDATE_IDENTITY_MISMATCH' 'Frozen harness or collector SHA-256 did not match the Cycle 3 plan.'
  }

  $targetSpecPath = [System.IO.Path]::GetFullPath((Join-Path $script:FixtureRootFull $TargetSpecRelative))
  $experimentsRoot = Split-Path -Parent $PSCommandPath
  $cycle3RawRoot = Join-Path $experimentsRoot 'raw\36-x01\recovery-cycle-3'
  if ($Mode -eq 'PREVALIDATE') {
    if (Test-Path -LiteralPath $targetSpecPath) {
      Stop-Closed 'TARGET_SPEC_PRESENT_DURING_PREVALIDATE' 'PREVALIDATE requires the temporary target spec to remain absent.'
    }
    if (Test-Path -LiteralPath $RuntimeSourceRoot) {
      Stop-Closed 'RUNTIME_SOURCE_ROOT_PRESENT_DURING_PREVALIDATE' 'PREVALIDATE requires the test-owned runtime source root to remain absent.'
    }
    if (Test-Path -LiteralPath $cycle3RawRoot) {
      Stop-Closed 'CYCLE3_RAW_PRESENT_DURING_PREVALIDATE' 'PREVALIDATE requires the Cycle 3 raw destination to remain absent.'
    }
  } else {
    $script:FocusedPrestate = Get-ExactFocusedFixtureState -Stage 'PRE' -TargetSpecPath $targetSpecPath -Cycle3RawRoot $cycle3RawRoot
    Write-JsonReceipt -Name 'focused-prestate.json' -Value $script:FocusedPrestate
    if (-not $script:FocusedPrestate.all_predicates_pass) {
      Stop-Closed 'FOCUSED_PRESTATE_MISMATCH' 'The exact shared one-untracked-spec focused prestate predicate failed.'
    }
  }

  $script:MutatedProcessPath = $script:FixtureBinFull + [System.IO.Path]::PathSeparator + $script:OriginalProcessPath
  $env:PATH = $script:MutatedProcessPath
  $script:PathWasMutated = $true

  $vitestCommands = @(Get-Command -Name 'vitest.cmd' -All -CommandType Application -ErrorAction SilentlyContinue)
  $vitestEntries = @($vitestCommands | ForEach-Object {
      [ordered]@{ name = $_.Name; command_type = [string]$_.CommandType; source = $_.Source; path = $_.Path; definition = $_.Definition }
    })
  $vitestSelectedPath = if ($vitestCommands.Count -gt 0) { [System.IO.Path]::GetFullPath($vitestCommands[0].Source) } else { $null }
  $vitestResolvesInsideFixtureBin = ($null -ne $vitestSelectedPath -and (Test-PathInside -Candidate $vitestSelectedPath -Root $script:FixtureBinFull))
  $vitestSelectedMatchesShim = ($null -ne $vitestSelectedPath -and $vitestSelectedPath.Equals($vitestShimPath, [System.StringComparison]::OrdinalIgnoreCase))
  $script:VitestResolution = [ordered]@{
    schema_version = '36-x01-recovery-cycle-3-vitest-resolution-v1'
    resolver_command = "Get-Command -Name 'vitest.cmd' -All -CommandType Application"
    resolver_count = $vitestCommands.Count
    resolver_entries = $vitestEntries
    selected_absolute_path = $vitestSelectedPath
    expected_fixture_shim = $vitestShimPath
    selected_inside_fixture_bin = $vitestResolvesInsideFixtureBin
    selected_matches_fixture_shim = $vitestSelectedMatchesShim
    resolved_after_process_path_prepend = $script:PathWasMutated
  }
  Write-JsonReceipt -Name 'vitest-resolution.json' -Value $script:VitestResolution
  if ($vitestCommands.Count -eq 0 -or -not $vitestResolvesInsideFixtureBin -or -not $vitestSelectedMatchesShim) {
    Stop-Closed 'VITEST_RESOLUTION_NOT_FIXTURE_LOCAL' 'vitest.cmd did not resolve to the fixture-local .bin shim after process PATH prepend.'
  }

  Write-JsonReceipt -Name 'working-directory.json' -Value ([ordered]@{
      schema_version = '36-x01-recovery-cycle-3-working-directory-v1'
      launcher_current_location = (Get-Location).Path
      process_working_directory = $script:FixtureRootFull
      all_actual_invocations_use_fixture = $true
    })

  switch ($Mode) {
    'PREVALIDATE' {
      Invoke-ExactVersionProbes
      if ($script:PrevalidationProbeCount -ne 2 -or $script:FocusedCommandCount -ne 0) {
        Stop-Closed 'PREVALIDATION_INVOCATION_COUNT_INVALID' 'PREVALIDATE must execute exactly two probes and zero focused commands.'
      }
    }
    'VALIDATE_FOCUSED_PRESTATE' {
      $probeFailure = $null
      try {
        Invoke-ExactVersionProbes
      } catch {
        $probeFailure = $_.Exception
      }
      $script:FocusedPoststate = Get-ExactFocusedFixtureState -Stage 'POST' -TargetSpecPath $targetSpecPath -Cycle3RawRoot $cycle3RawRoot
      Write-JsonReceipt -Name 'focused-poststate.json' -Value $script:FocusedPoststate
      if ($null -ne $probeFailure) {
        throw $probeFailure
      }
      if (-not $script:FocusedPoststate.all_predicates_pass) {
        Stop-Closed 'FOCUSED_POSTSTATE_MISMATCH' 'The exact shared focused poststate predicate failed after the two probes.'
      }
      if ($script:PrevalidationProbeCount -ne 2 -or $script:FocusedCommandCount -ne 0) {
        Stop-Closed 'FOCUSED_PRESTATE_VALIDATION_COUNT_INVALID' 'VALIDATE_FOCUSED_PRESTATE must execute exactly two probes and zero focused commands.'
      }
    }
    'EXECUTE_FOCUSED' {
      $focusedProcessFailure = $null
      $focusedResult = $null
      try {
        $focusedArguments = @('pnpm', 'exec', 'vitest', 'run', $TargetSpecRelative.Replace('\', '/'), '--reporter=verbose')
        $focusedResult = Invoke-CapturedProcess -ReceiptPrefix 'focused' -Executable $script:ResolvedCorepackPath -ArgumentVector $focusedArguments -TimeoutSeconds 180 -InvocationKind 'FOCUSED'
      } catch {
        $focusedProcessFailure = $_.Exception
      }
      $script:FocusedPoststate = Get-ExactFocusedFixtureState -Stage 'POST' -TargetSpecPath $targetSpecPath -Cycle3RawRoot $cycle3RawRoot
      Write-JsonReceipt -Name 'focused-poststate.json' -Value $script:FocusedPoststate
      if ($null -ne $focusedProcessFailure) {
        throw $focusedProcessFailure
      }
      if (-not $script:FocusedPoststate.all_predicates_pass) {
        Stop-Closed 'FOCUSED_POSTSTATE_MISMATCH' 'The exact shared focused poststate predicate failed immediately after the focused process.'
      }
      if ($script:FocusedCommandCount -ne 1 -or $script:PrevalidationProbeCount -ne 0) {
        Stop-Closed 'FOCUSED_INVOCATION_COUNT_INVALID' 'EXECUTE_FOCUSED must execute exactly one focused command and zero version probes.'
      }
      if ($focusedResult.exit_code -ne 0) {
        Stop-Closed 'FOCUSED_EXIT_NONZERO' "The one frozen focused command exited $($focusedResult.exit_code)."
      }
    }
  }

  $script:Status = 'PASS'
  $script:FailureCode = $null
  $script:FailureMessage = $null
  $script:ExitCode = 0
} catch {
  $script:Status = 'FAIL'
  $script:ExitCode = if ($script:ExitCode -eq 0) { 90 } else { $script:ExitCode }
  $script:FailureCode = if ($_.Exception.Data.Contains('FailureCode')) { [string]$_.Exception.Data['FailureCode'] } else { 'UNHANDLED_LAUNCHER_ERROR' }
  $script:FailureMessage = $_.Exception.Message
} finally {
  if ($script:PathWasMutated) {
    $env:PATH = $script:OriginalProcessPath
    $script:PathWasRestored = ($env:PATH -ceq $script:OriginalProcessPath)
  } else {
    $script:PathWasRestored = $true
  }

  if ($script:OwnsReceiptRoot -and $null -ne $script:ReceiptRootFull -and (Test-Path -LiteralPath $script:ReceiptRootFull -PathType Container)) {
    try {
      if ($null -ne $script:FixtureRootFull -and (Test-Path -LiteralPath $script:FixtureRootFull -PathType Container)) {
        $script:FixtureStateAfter = Get-FixtureState -Root $script:FixtureRootFull
      }
      $script:FrozenCandidateIdentityAfter = Get-FrozenCandidateIdentity

      $targetSpecPathFinal = if ($null -ne $script:FixtureRootFull) { [System.IO.Path]::GetFullPath((Join-Path $script:FixtureRootFull $TargetSpecRelative)) } else { $null }
      $targetSpecAbsent = ($null -ne $targetSpecPathFinal -and -not (Test-Path -LiteralPath $targetSpecPathFinal))
      $runtimeSourceRootAbsent = -not (Test-Path -LiteralPath $RuntimeSourceRoot)
      $experimentsRootFinal = Split-Path -Parent $PSCommandPath
      $cycle3RawRootFinal = Join-Path $experimentsRootFinal 'raw\36-x01\recovery-cycle-3'
      $cycle3RawAbsent = -not (Test-Path -LiteralPath $cycle3RawRootFinal)

      $allActualExecutablesAbsolute = @($script:ProcessRecords | Where-Object { -not $_.executable_is_absolute }).Count -eq 0
      $allActualExecutablesCaptured = @($script:ProcessRecords | Where-Object { -not $_.executable_matches_captured_corepack }).Count -eq 0
      $bareInvocationCount = @($script:ProcessRecords | Where-Object { -not $_.executable_is_absolute }).Count
      $targetSpecExecutionCount = @($script:ProcessRecords | Where-Object { $_.invocation_kind -eq 'FOCUSED' }).Count
      $forbiddenArgumentCount = @($script:ProcessRecords | Where-Object {
          @($_.argument_vector | Where-Object { $_ -in @('install', 'add', 'update', 'upgrade', 'prepare', 'enable') }).Count -gt 0
        }).Count
      $sourceTokens = $null
      $sourceParseErrors = $null
      $sourceAst = [System.Management.Automation.Language.Parser]::ParseFile($PSCommandPath, [ref]$sourceTokens, [ref]$sourceParseErrors)
      $sourceCommandAsts = @($sourceAst.FindAll({ param($node) $node -is [System.Management.Automation.Language.CommandAst] }, $true))
      $sourceCommandNames = @($sourceCommandAsts | ForEach-Object { $_.GetCommandName() })
      $sourceFunctionDefinitions = @($sourceAst.FindAll({ param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] }, $true))
      $sourceBareCorepackCommandCount = @($sourceCommandNames | Where-Object { $_ -in @('corepack', 'corepack.cmd') }).Count
      $sourceText = [System.IO.File]::ReadAllText($PSCommandPath)
      $sourceResolverOffset = $sourceText.IndexOf("Get-Command -Name 'corepack.cmd'")
      $sourcePathMutationOffset = $sourceText.IndexOf('$env:PATH = $script:MutatedProcessPath')
      $sourcePrevalidateStart = $sourceText.IndexOf("'PREVALIDATE' {")
      $sourceValidateFocusedStart = $sourceText.IndexOf("'VALIDATE_FOCUSED_PRESTATE' {", $sourcePrevalidateStart)
      $sourceFocusedStart = $sourceText.IndexOf("'EXECUTE_FOCUSED' {", $sourceValidateFocusedStart)
      $sourcePrevalidateBlock = if ($sourcePrevalidateStart -ge 0 -and $sourceValidateFocusedStart -gt $sourcePrevalidateStart) {
        $sourceText.Substring($sourcePrevalidateStart, $sourceValidateFocusedStart - $sourcePrevalidateStart)
      } else {
        ''
      }
      $sourceValidateFocusedBlock = if ($sourceValidateFocusedStart -ge 0 -and $sourceFocusedStart -gt $sourceValidateFocusedStart) {
        $sourceText.Substring($sourceValidateFocusedStart, $sourceFocusedStart - $sourceValidateFocusedStart)
      } else {
        ''
      }
      $sourcePrevalidateInvokeCount = ([regex]::Matches($sourcePrevalidateBlock, 'Invoke-ExactVersionProbes')).Count
      $sourcePrevalidateContainsFocusedArguments = ($sourcePrevalidateBlock -match "'run'|--reporter=verbose|TargetSpecRelative")
      $sourceValidateFocusedInvokeCount = ([regex]::Matches($sourceValidateFocusedBlock, 'Invoke-ExactVersionProbes')).Count
      $sourceValidateFocusedContainsTargetArguments = ($sourceValidateFocusedBlock -match "'run'|--reporter=verbose|Invoke-CapturedProcess.+FOCUSED")
      $sourceSharedFocusedPrestateCallCount = @($sourceCommandAsts | Where-Object {
          $_.GetCommandName() -eq 'Get-ExactFocusedFixtureState' -and $_.Extent.Text -match "-Stage\s+'PRE'"
        }).Count
      $sourceFocusedStateValidatorDefinitionCount = @($sourceFunctionDefinitions | Where-Object { $_.Name -eq 'Get-ExactFocusedFixtureState' }).Count
      $sourceCollectorExecutionAstCount = @($sourceCommandNames | Where-Object { $_ -match 'collector' }).Count

      Write-JsonReceipt -Name 'path-scope.json' -Value ([ordered]@{
          schema_version = '36-x01-recovery-cycle-3-path-scope-v2'
          scope = 'CURRENT_LAUNCHER_PROCESS_ONLY'
          fixture_bin_prepended_only = $script:FixtureBinFull
          process_path_before = $script:OriginalProcessPath
          process_path_before_sha256 = Get-StringSha256 -Value $script:OriginalProcessPath
          process_path_during = $script:MutatedProcessPath
          process_path_during_sha256 = if ($null -ne $script:MutatedProcessPath) { Get-StringSha256 -Value $script:MutatedProcessPath } else { $null }
          path_was_mutated = $script:PathWasMutated
          path_restored = $script:PathWasRestored
          user_path_before_sha256 = Get-StringSha256 -Value ([string]$script:UserPathBefore)
          user_path_after_sha256 = Get-StringSha256 -Value ([string][Environment]::GetEnvironmentVariable('PATH', 'User'))
          user_path_unchanged = ([string]$script:UserPathBefore -ceq [string][Environment]::GetEnvironmentVariable('PATH', 'User'))
          machine_path_before_sha256 = Get-StringSha256 -Value ([string]$script:MachinePathBefore)
          machine_path_after_sha256 = Get-StringSha256 -Value ([string][Environment]::GetEnvironmentVariable('PATH', 'Machine'))
          machine_path_unchanged = ([string]$script:MachinePathBefore -ceq [string][Environment]::GetEnvironmentVariable('PATH', 'Machine'))
        })

      Write-JsonReceipt -Name 'frozen-candidate-identities.json' -Value ([ordered]@{
          schema_version = '36-x01-recovery-cycle-3-frozen-identities-v2'
          before = $script:FrozenCandidateIdentityBefore
          after = $script:FrozenCandidateIdentityAfter
          unchanged = ($null -ne $script:FrozenCandidateIdentityBefore -and $null -ne $script:FrozenCandidateIdentityAfter -and
            $script:FrozenCandidateIdentityBefore.harness.sha256 -ceq $script:FrozenCandidateIdentityAfter.harness.sha256 -and
            $script:FrozenCandidateIdentityBefore.collector.sha256 -ceq $script:FrozenCandidateIdentityAfter.collector.sha256)
        })

      Write-JsonReceipt -Name 'bare-command-audit.json' -Value ([ordered]@{
          schema_version = '36-x01-recovery-cycle-3-bare-command-audit-v2'
          actual_invocation_count = $script:ProcessRecords.Count
          bare_corepack_invocation_count = $bareInvocationCount
          all_actual_executables_absolute = $allActualExecutablesAbsolute
          all_actual_executables_match_pre_path_capture = $allActualExecutablesCaptured
          prevalidation_probe_count = $script:PrevalidationProbeCount
          focused_command_count = $script:FocusedCommandCount
          target_spec_execution_count = $targetSpecExecutionCount
          forbidden_install_update_argument_vector_count = $forbiddenArgumentCount
          resolver_lookup_is_not_execution = $true
          launcher_source_parse_error_count = $sourceParseErrors.Count
          launcher_source_direct_bare_corepack_command_ast_count = $sourceBareCorepackCommandCount
          launcher_source_resolver_before_path_mutation = ($sourceResolverOffset -ge 0 -and $sourceResolverOffset -lt $sourcePathMutationOffset)
          launcher_source_prevalidate_probe_callsite_count = $sourcePrevalidateInvokeCount
          launcher_source_prevalidate_contains_focused_arguments = $sourcePrevalidateContainsFocusedArguments
          launcher_source_validate_focused_prestate_probe_dispatch_count = $sourceValidateFocusedInvokeCount
          launcher_source_validate_focused_prestate_contains_target_arguments = $sourceValidateFocusedContainsTargetArguments
          launcher_source_shared_focused_prestate_validator_definition_count = $sourceFocusedStateValidatorDefinitionCount
          launcher_source_shared_focused_prestate_pre_callsite_count = $sourceSharedFocusedPrestateCallCount
          launcher_source_collector_execution_ast_count = $sourceCollectorExecutionAstCount
          actual_invocations = @($script:ProcessRecords | ForEach-Object {
              [ordered]@{ executable_absolute_path = $_.executable_absolute_path; argument_vector = $_.argument_vector; invocation_kind = $_.invocation_kind }
            })
        })

      $cleanupReceipt = [ordered]@{
        schema_version = '36-x01-recovery-cycle-3-cleanup-v2'
        mode = $Mode
        deletion_attempted = $false
        deletion_needed = ($Mode -ne 'PREVALIDATE' -and -not $targetSpecAbsent)
        cleanup_note = switch ($Mode) {
          'PREVALIDATE' { 'Strict launcher-only prevalidation creates no target spec or runtime source root; no cleanup deletion is authorized or needed.' }
          'VALIDATE_FOCUSED_PRESTATE' { 'The launcher preserves the exact orchestration-owned untracked target spec; the Attempt 2 orchestrator removes only that file after launcher exit.' }
          'EXECUTE_FOCUSED' { 'The launcher preserves the exact Lab-owned untracked target spec; the Lab Engineer owns post-capture cleanup.' }
        }
        temporary_target_spec = $targetSpecPathFinal
        temporary_target_spec_absent = $targetSpecAbsent
        temporary_target_spec_present = (-not $targetSpecAbsent)
        runtime_source_root = $RuntimeSourceRoot
        runtime_source_root_absent = $runtimeSourceRootAbsent
        cycle3_raw_root = $cycle3RawRootFinal
        cycle3_raw_root_absent = $cycle3RawAbsent
        fixture_before = $script:FixtureStateBefore
        fixture_after = $script:FixtureStateAfter
        strict_clean_required = ($Mode -eq 'PREVALIDATE')
        strict_clean_preserved = if ($Mode -eq 'PREVALIDATE') {
          ($null -ne $script:FixtureStateBefore -and $null -ne $script:FixtureStateAfter -and
            $script:FixtureStateBefore.strict_clean -and $script:FixtureStateAfter.strict_clean)
        } else {
          $null
        }
        exact_one_spec_prestate_required = ($Mode -ne 'PREVALIDATE')
        exact_one_spec_prestate_preserved = if ($Mode -ne 'PREVALIDATE') {
          ($null -ne $script:FocusedPrestate -and $null -ne $script:FocusedPoststate -and
            $script:FocusedPrestate.all_predicates_pass -and $script:FocusedPoststate.all_predicates_pass)
        } else {
          $null
        }
        fixture_contract_preserved = if ($Mode -eq 'PREVALIDATE') {
          ($null -ne $script:FixtureStateBefore -and $null -ne $script:FixtureStateAfter -and
            $script:FixtureStateBefore.strict_clean -and $script:FixtureStateAfter.strict_clean -and $targetSpecAbsent)
        } else {
          ($null -ne $script:FocusedPrestate -and $null -ne $script:FocusedPoststate -and
            $script:FocusedPrestate.all_predicates_pass -and $script:FocusedPoststate.all_predicates_pass -and -not $targetSpecAbsent)
        }
        process_path_restored = $script:PathWasRestored
      }
      Write-JsonReceipt -Name 'cleanup-receipt.json' -Value $cleanupReceipt

      $postconditionsPass = switch ($Mode) {
        'PREVALIDATE' {
          $targetSpecAbsent -and $runtimeSourceRootAbsent -and $cycle3RawAbsent -and
          $script:PrevalidationProbeCount -eq 2 -and $script:FocusedCommandCount -eq 0
        }
        'VALIDATE_FOCUSED_PRESTATE' {
          (-not $targetSpecAbsent) -and $runtimeSourceRootAbsent -and $cycle3RawAbsent -and
          $script:PrevalidationProbeCount -eq 2 -and $script:FocusedCommandCount -eq 0 -and
          $null -ne $script:FocusedPrestate -and $null -ne $script:FocusedPoststate -and
          $script:FocusedPrestate.all_predicates_pass -and $script:FocusedPoststate.all_predicates_pass
        }
        'EXECUTE_FOCUSED' {
          (-not $targetSpecAbsent) -and $cycle3RawAbsent -and
          $script:FocusedCommandCount -eq 1 -and $script:PrevalidationProbeCount -eq 0 -and
          $null -ne $script:FocusedPrestate -and $null -ne $script:FocusedPoststate -and
          $script:FocusedPrestate.all_predicates_pass -and $script:FocusedPoststate.all_predicates_pass
        }
      }
      if ($script:Status -eq 'PASS' -and (-not $cleanupReceipt.fixture_contract_preserved -or -not $postconditionsPass -or $bareInvocationCount -ne 0 -or -not $allActualExecutablesCaptured -or $forbiddenArgumentCount -ne 0 -or $sourceParseErrors.Count -ne 0 -or $sourceBareCorepackCommandCount -ne 0 -or $sourcePrevalidateInvokeCount -ne 1 -or $sourcePrevalidateContainsFocusedArguments -or $sourceValidateFocusedInvokeCount -ne 1 -or $sourceValidateFocusedContainsTargetArguments -or $sourceFocusedStateValidatorDefinitionCount -ne 1 -or $sourceSharedFocusedPrestateCallCount -ne 1 -or $sourceCollectorExecutionAstCount -ne 0 -or -not ($sourceResolverOffset -ge 0 -and $sourceResolverOffset -lt $sourcePathMutationOffset))) {
        $script:Status = 'FAIL'
        $script:FailureCode = 'POSTCONDITION_FAILURE'
        $script:FailureMessage = 'One or more closed launcher postconditions failed.'
        $script:ExitCode = 90
      }

      Write-JsonReceipt -Name 'prevalidation-summary.json' -Value ([ordered]@{
          schema_version = '36-x01-recovery-cycle-3-prevalidation-summary-v2'
          mode = $Mode
          status = $script:Status
          failure_code = $script:FailureCode
          failure_message = $script:FailureMessage
          launcher_source_sha256 = if ($null -ne $script:LauncherIdentity) { $script:LauncherIdentity.source.sha256 } else { $null }
          resolver_completed_before_path_mutation = $script:ResolverCompletedBeforePathMutation
          package_manager_validated_before_path_mutation = $script:PackageManagerValidatedBeforePathMutation
          resolved_corepack_absolute_path = $script:ResolvedCorepackPath
          process_path_scope = 'CURRENT_LAUNCHER_PROCESS_ONLY'
          process_path_restored = $script:PathWasRestored
          fixture_vitest_resolution = $script:VitestResolution
          actual_processes = @($script:ProcessRecords)
          prevalidation_probe_count = $script:PrevalidationProbeCount
          focused_command_count = $script:FocusedCommandCount
          target_spec_execution_count = $targetSpecExecutionCount
          bare_corepack_invocation_count = $bareInvocationCount
          target_spec_absent = $targetSpecAbsent
          target_spec_present = (-not $targetSpecAbsent)
          runtime_source_root_absent = $runtimeSourceRootAbsent
          cycle3_raw_absent = $cycle3RawAbsent
          fixture_contract_preserved = $cleanupReceipt.fixture_contract_preserved
          strict_clean_required = $cleanupReceipt.strict_clean_required
          exact_one_spec_prestate_required = $cleanupReceipt.exact_one_spec_prestate_required
          frozen_harness_collector_preserved = ($null -ne $script:FrozenCandidateIdentityAfter -and $script:FrozenCandidateIdentityAfter.harness_matches -and $script:FrozenCandidateIdentityAfter.collector_matches)
          claim_ceiling = 'LAUNCHER_ONLY_PREVALIDATION_NOT_DSH_RUNTIME_EVIDENCE'
          target_experiment_authorized = $false
        })

      Write-SelfExcludingManifest
    } catch {
      $script:Status = 'FAIL'
      $script:ExitCode = 90
      $script:FailureCode = 'RECEIPT_FINALIZATION_FAILURE'
      $script:FailureMessage = $_.Exception.Message
    }
  }
}

exit $script:ExitCode
