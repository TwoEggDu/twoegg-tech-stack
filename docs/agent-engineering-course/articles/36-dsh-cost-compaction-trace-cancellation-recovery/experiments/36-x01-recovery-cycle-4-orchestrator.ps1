[CmdletBinding(DefaultParameterSetName = 'Run')]
param(
  [Parameter(Mandatory = $true, ParameterSetName = 'Run')]
  [string]$ReceiptRoot,

  [Parameter(Mandatory = $true, ParameterSetName = 'Library')]
  [switch]$LibraryMode
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:Cycle4Utf8NoBom = [System.Text.UTF8Encoding]::new($false)
$script:Cycle4Mode = 'VALIDATE_FOCUSED_PRESTATE'
$script:Cycle4AttemptIdentity = '36-X01-RECOVERY-CYCLE-4-PREVALIDATION-ATTEMPT-1'
$script:Cycle4PinnedFixtureRoot = 'C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814'
$script:Cycle4PinnedFixtureHead = 'cd5ef8148158c3a752a658978873241fdf8e2bbc'
$script:Cycle4PinnedFixtureTag = 'dsh-v0.1.2-alpha.1'
$script:Cycle4PinnedCorepackPath = 'C:\nvm4w\nodejs\corepack.cmd'
$script:Cycle4PinnedCorepackSha256 = '920630A1D1EC47AEDEA7345E3C868ECDC07E191373497BBF47FBBF5942FBAD4F'
$script:Cycle4PinnedLauncherSha256 = '2F16A499C1A799C8A15C03384C2BC41FB7C742236BD5DCF07229BA4C0415C64E'
$script:Cycle4PinnedHarnessSha256 = 'D5DFBEC5286309C4BFB1027BA490892992144D49040C6A8E2AF201B842A0199B'
$script:Cycle4PinnedCollectorSha256 = 'D04A8929EA32CF004D011D0241474C759143B75A8E88DEDBE8DC8E09080A5045'
$script:Cycle4TargetSpecRelative = 'packages/compaction/compaction-basic/tests/36-x01-long-session-compaction-cancel-resume.spec.ts'
$script:Cycle4RuntimeSourceRoot = Join-Path ([System.IO.Path]::GetTempPath()) 'dsh-article36-x01-artifacts'
$script:Cycle4GitExecutable = $null

function Write-Cycle4Text {
  param([string]$Path, [AllowEmptyString()][string]$Text)
  [System.IO.File]::WriteAllText($Path, $Text, $script:Cycle4Utf8NoBom)
}

function Write-Cycle4Json {
  param([string]$Path, [object]$Value, [int]$Depth = 18)
  Write-Cycle4Text -Path $Path -Text ($Value | ConvertTo-Json -Depth $Depth)
}

function Get-Cycle4StringSha256 {
  param([AllowEmptyString()][string]$Value)
  $bytes = $script:Cycle4Utf8NoBom.GetBytes($Value)
  $sha = [System.Security.Cryptography.SHA256]::Create()
  try {
    return ([System.BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-', '')
  } finally {
    $sha.Dispose()
  }
}

function Get-Cycle4FileIdentity {
  param([string]$Path)
  $exists = Test-Path -LiteralPath $Path -PathType Leaf
  return [ordered]@{
    path = $Path
    exists = $exists
    byte_count = if ($exists) { ([System.IO.FileInfo](Get-Item -LiteralPath $Path)).Length } else { $null }
    sha256 = if ($exists) { (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToUpperInvariant() } else { $null }
  }
}

function Format-Cycle4DisplayCommand {
  param([string]$Executable, [string[]]$ArgumentVector)
  $snapshot = [string[]]@($ArgumentVector)
  $quoted = @($snapshot | ForEach-Object { "'" + $_.Replace("'", "''") + "'" })
  return "& '" + $Executable.Replace("'", "''") + "'" + $(if ($quoted.Count -gt 0) { ' ' + [string]::Join(' ', $quoted) } else { '' })
}

function Test-Cycle4OrderedRecords {
  param([object[]]$Expected, [object[]]$Actual)
  $expectedArray = [object[]]@($Expected)
  $actualArray = [object[]]@($Actual)
  $countMatch = ($expectedArray.Count -eq $actualArray.Count)
  $elementsMatch = $countMatch
  if ($countMatch) {
    for ($index = 0; $index -lt $expectedArray.Count; $index++) {
      $expectedRecord = $expectedArray[$index]
      $actualRecord = $actualArray[$index]
      if ([string]$expectedRecord.relative_path -cne [string]$actualRecord.relative_path -or
          [int64]$expectedRecord.byte_count -ne [int64]$actualRecord.byte_count -or
          [string]$expectedRecord.sha256 -cne [string]$actualRecord.sha256) {
        $elementsMatch = $false
        break
      }
    }
  }
  return [ordered]@{
    expected_count = $expectedArray.Count
    actual_count = $actualArray.Count
    count_match = $countMatch
    elements_match = $elementsMatch
    pass = ($countMatch -and $elementsMatch)
  }
}

function Get-Cycle4ManifestEntries {
  param([string]$Root, [string]$ManifestPath)
  $rootFull = [System.IO.Path]::GetFullPath($Root).TrimEnd([char[]]@('\', '/'))
  $manifestFull = [System.IO.Path]::GetFullPath($ManifestPath)
  $rootPrefix = $rootFull + [System.IO.Path]::DirectorySeparatorChar
  $entries = [object[]]@(Get-ChildItem -LiteralPath $rootFull -File -Recurse -Force |
      Where-Object { -not $_.FullName.Equals($manifestFull, [System.StringComparison]::OrdinalIgnoreCase) } |
      Sort-Object FullName |
      ForEach-Object {
        [ordered]@{
          relative_path = $_.FullName.Substring($rootPrefix.Length).Replace('\', '/')
          byte_count = $_.Length
          sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToUpperInvariant()
        }
      })
  return $entries
}

function Write-Cycle4SelfExcludingManifest {
  param(
    [string]$Root,
    [string]$ManifestPath,
    [string]$SchemaVersion,
    [string]$Mode,
    [string]$AttemptIdentity,
    [string]$Status,
    [AllowNull()][string]$FailureCode,
    [AllowNull()][string]$FailureMessage,
    [object]$AdditionalFields
  )
  $rootFull = [System.IO.Path]::GetFullPath($Root).TrimEnd([char[]]@('\', '/'))
  $manifestFull = [System.IO.Path]::GetFullPath($ManifestPath)
  if (-not (Test-Path -LiteralPath $rootFull -PathType Container)) {
    [System.IO.Directory]::CreateDirectory($rootFull) | Out-Null
  }
  $entries = [object[]]@(Get-Cycle4ManifestEntries -Root $rootFull -ManifestPath $manifestFull)
  $manifest = [ordered]@{
    schema_version = $SchemaVersion
    mode = $Mode
    attempt_identity = $AttemptIdentity
    status = $Status
    failure_code = $FailureCode
    failure_message = $FailureMessage
    self_excluding = $true
    excluded_relative_path = 'manifest.json'
    file_count = $entries.Count
    files = $entries
  }
  if ($AdditionalFields -is [System.Collections.IDictionary]) {
    foreach ($entry in $AdditionalFields.GetEnumerator()) {
      $manifest[[string]$entry.Key] = $entry.Value
    }
  }
  Write-Cycle4Json -Path $manifestFull -Value $manifest -Depth 14
  return $manifest
}

function Test-Cycle4SelfExcludingManifest {
  param([string]$Root, [string]$ManifestPath)
  $manifestFull = [System.IO.Path]::GetFullPath($ManifestPath)
  if (-not (Test-Path -LiteralPath $manifestFull -PathType Leaf)) {
    return [ordered]@{ expected_count = 0; actual_count = 0; count_match = $false; elements_match = $false; pass = $false; failure = 'MANIFEST_MISSING' }
  }
  $manifest = Get-Content -Raw -LiteralPath $manifestFull | ConvertFrom-Json
  $expected = [object[]]@($manifest.files)
  $actual = [object[]]@(Get-Cycle4ManifestEntries -Root $Root -ManifestPath $manifestFull)
  $comparison = Test-Cycle4OrderedRecords -Expected $expected -Actual $actual
  $comparison['declared_file_count'] = [int]$manifest.file_count
  $comparison['declared_count_matches_expected_array'] = ([int]$manifest.file_count -eq $expected.Count)
  $comparison['pass'] = ($comparison.count_match -and $comparison.elements_match -and $comparison.declared_count_matches_expected_array)
  return $comparison
}

function Invoke-Cycle4CapturedProcess {
  param(
    [string]$Executable,
    [string[]]$ArgumentVector,
    [string]$WorkingDirectory,
    [int]$TimeoutSeconds
  )
  $executableFull = [System.IO.Path]::GetFullPath($Executable)
  if (-not [System.IO.Path]::IsPathRooted($executableFull) -or -not (Test-Path -LiteralPath $executableFull -PathType Leaf)) {
    throw "Captured process executable must be one existing absolute file: $executableFull"
  }
  $argumentSnapshot = [string[]]@($ArgumentVector)
  $workingFull = [System.IO.Path]::GetFullPath($WorkingDirectory)
  if (-not (Test-Path -LiteralPath $workingFull -PathType Container)) {
    throw "Captured process working directory is missing: $workingFull"
  }

  $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
  $startInfo.FileName = $executableFull
  $startInfo.WorkingDirectory = $workingFull
  $startInfo.UseShellExecute = $false
  $startInfo.CreateNoWindow = $true
  $startInfo.RedirectStandardOutput = $true
  $startInfo.RedirectStandardError = $true
  $startInfo.StandardOutputEncoding = $script:Cycle4Utf8NoBom
  $startInfo.StandardErrorEncoding = $script:Cycle4Utf8NoBom
  foreach ($argument in $argumentSnapshot) {
    $startInfo.ArgumentList.Add($argument)
  }

  $process = [System.Diagnostics.Process]::new()
  $process.StartInfo = $startInfo
  $startedUtc = [DateTime]::UtcNow
  $endedUtc = $null
  $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
  $started = $false
  $timedOut = $false
  $exitCode = $null
  $stdoutText = ''
  $stderrText = ''
  try {
    $started = $process.Start()
    if (-not $started) {
      throw "Process.Start returned false for $executableFull"
    }
    $stdoutTask = $process.StandardOutput.ReadToEndAsync()
    $stderrTask = $process.StandardError.ReadToEndAsync()
    if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
      $timedOut = $true
      $process.Kill($true)
    }
    $process.WaitForExit()
    $stdoutText = $stdoutTask.GetAwaiter().GetResult()
    $stderrText = $stderrTask.GetAwaiter().GetResult()
    if (-not $timedOut) {
      $exitCode = $process.ExitCode
    }
  } finally {
    $stopwatch.Stop()
    $endedUtc = [DateTime]::UtcNow
    $process.Dispose()
  }
  return [ordered]@{
    executable_absolute_path = $executableFull
    argument_vector = $argumentSnapshot
    argument_count = $argumentSnapshot.Count
    display_command = Format-Cycle4DisplayCommand -Executable $executableFull -ArgumentVector $argumentSnapshot
    working_directory = $workingFull
    started = $started
    started_utc = $startedUtc.ToString('o')
    ended_utc = $endedUtc.ToString('o')
    duration_milliseconds = $stopwatch.ElapsedMilliseconds
    timeout_seconds = $TimeoutSeconds
    timed_out = $timedOut
    exit_code = $exitCode
    stdout_text = $stdoutText
    stdout_byte_count = $script:Cycle4Utf8NoBom.GetByteCount($stdoutText)
    stdout_sha256 = Get-Cycle4StringSha256 -Value $stdoutText
    stdout_base64 = [Convert]::ToBase64String($script:Cycle4Utf8NoBom.GetBytes($stdoutText))
    stderr_text = $stderrText
    stderr_byte_count = $script:Cycle4Utf8NoBom.GetByteCount($stderrText)
    stderr_sha256 = Get-Cycle4StringSha256 -Value $stderrText
    stderr_base64 = [Convert]::ToBase64String($script:Cycle4Utf8NoBom.GetBytes($stderrText))
    streams_merged = $false
  }
}

function Invoke-Cycle4Git {
  param([string[]]$ArgumentVector, [string]$WorkingDirectory)
  if ([string]::IsNullOrWhiteSpace($script:Cycle4GitExecutable)) {
    throw 'The absolute Git executable has not been captured.'
  }
  return Invoke-Cycle4CapturedProcess -Executable $script:Cycle4GitExecutable -ArgumentVector $ArgumentVector -WorkingDirectory $WorkingDirectory -TimeoutSeconds 30
}

function Get-Cycle4FixtureState {
  param([string]$FixtureRoot, [string]$Stage)
  $rootFull = [System.IO.Path]::GetFullPath($FixtureRoot).TrimEnd([char[]]@('\', '/'))
  $headCommand = Invoke-Cycle4Git -ArgumentVector @('-C', $rootFull, 'rev-parse', 'HEAD') -WorkingDirectory $rootFull
  $tagCommand = Invoke-Cycle4Git -ArgumentVector @('-C', $rootFull, 'describe', '--tags', '--exact-match', 'HEAD') -WorkingDirectory $rootFull
  $unstagedCommand = Invoke-Cycle4Git -ArgumentVector @('-C', $rootFull, 'diff', '--quiet', '--') -WorkingDirectory $rootFull
  $stagedCommand = Invoke-Cycle4Git -ArgumentVector @('-C', $rootFull, 'diff', '--cached', '--quiet', '--') -WorkingDirectory $rootFull
  $statusCommand = Invoke-Cycle4Git -ArgumentVector @('-C', $rootFull, 'status', '--porcelain=v1', '--untracked-files=all') -WorkingDirectory $rootFull
  $head = $headCommand.stdout_text.Trim()
  $tag = $tagCommand.stdout_text.Trim()
  $statusNormalized = ($statusCommand.stdout_text -replace "`r`n?", "`n").Replace('\', '/').TrimEnd("`n")
  $statusLines = [string[]]@(if ([string]::IsNullOrEmpty($statusNormalized)) { @() } else { @($statusNormalized -split "`n") })
  $gitRecords = [object[]]@($headCommand, $tagCommand, $unstagedCommand, $stagedCommand, $statusCommand)
  $vectorsNonempty = @($gitRecords | Where-Object { $_.argument_count -le 0 }).Count -eq 0
  return [ordered]@{
    stage = $Stage
    fixture_root = $rootFull
    git_executable_absolute_path = $script:Cycle4GitExecutable
    head_command = $headCommand
    head = $head
    head_matches = ($headCommand.exit_code -eq 0 -and $head -ceq $script:Cycle4PinnedFixtureHead)
    tag_command = $tagCommand
    exact_tag = $tag
    tag_matches = ($tagCommand.exit_code -eq 0 -and $tag -ceq $script:Cycle4PinnedFixtureTag)
    unstaged_diff_command = $unstagedCommand
    unstaged_diff_empty = ($unstagedCommand.exit_code -eq 0)
    staged_diff_command = $stagedCommand
    staged_diff_empty = ($stagedCommand.exit_code -eq 0)
    status_command = $statusCommand
    status_normalized_text = $statusNormalized
    status_normalized_lines = $statusLines
    status_empty = ($statusCommand.exit_code -eq 0 -and $statusLines.Count -eq 0)
    all_five_argument_vectors_nonempty = $vectorsNonempty
    strict_clean = ($vectorsNonempty -and $headCommand.exit_code -eq 0 -and $tagCommand.exit_code -eq 0 -and
      $head -ceq $script:Cycle4PinnedFixtureHead -and $tag -ceq $script:Cycle4PinnedFixtureTag -and
      $unstagedCommand.exit_code -eq 0 -and $stagedCommand.exit_code -eq 0 -and
      $statusCommand.exit_code -eq 0 -and $statusLines.Count -eq 0)
  }
}

if ($LibraryMode) {
  return
}

$script:Cycle4ReceiptRoot = [System.IO.Path]::GetFullPath($ReceiptRoot).TrimEnd([char[]]@('\', '/'))
$script:Cycle4ExperimentsRoot = [System.IO.Path]::GetFullPath((Split-Path -Parent $PSCommandPath))
$script:Cycle4FixtureRoot = [System.IO.Path]::GetFullPath($script:Cycle4PinnedFixtureRoot).TrimEnd([char[]]@('\', '/'))
$script:Cycle4TargetSpecPath = [System.IO.Path]::GetFullPath((Join-Path $script:Cycle4FixtureRoot $script:Cycle4TargetSpecRelative))
$script:Cycle4HarnessPath = Join-Path $script:Cycle4ExperimentsRoot '36-x01-recovery-cycle-2-instrumentation.ts'
$script:Cycle4CollectorPath = Join-Path $script:Cycle4ExperimentsRoot '36-x01-recovery-cycle-2-collector.ps1'
$script:Cycle4LauncherPath = Join-Path $script:Cycle4ExperimentsRoot '36-x01-recovery-cycle-3-launcher.ps1'
$script:Cycle4RawRoot = Join-Path $script:Cycle4ExperimentsRoot 'raw\36-x01\recovery-cycle-3'
$script:Cycle4LauncherReceiptRoot = Join-Path $script:Cycle4ReceiptRoot 'launcher'
$script:Cycle4OriginalProcessPath = $env:PATH
$script:Cycle4Status = 'FAIL'
$script:Cycle4FailureCode = 'ORCHESTRATOR_NOT_COMPLETED'
$script:Cycle4FailureMessage = 'The Cycle 4 orchestrator did not reach a closed result.'
$script:Cycle4ExitCode = 90
$script:Cycle4RootOwned = $false
$script:Cycle4CopyAttempted = $false
$script:Cycle4CopyCompleted = $false
$script:Cycle4LauncherInvocationCount = 0
$script:Cycle4LauncherProcess = $null
$script:Cycle4LauncherValidation = $null
$script:Cycle4CleanReceipt = $null
$script:Cycle4CopyReceipt = $null
$script:Cycle4RemovalReceipt = $null
$script:Cycle4FinalFixtureState = $null
$script:Cycle4FrozenBefore = $null
$script:Cycle4FrozenAfter = $null

function New-Cycle4OwnedReceipt {
  param([string]$LeafSchema, [string]$Status, [AllowNull()][string]$FailureCode, [AllowNull()][string]$FailureMessage)
  return [ordered]@{
    schema_version = "36-x01-recovery-cycle-4-$LeafSchema-v1"
    mode = $script:Cycle4Mode
    attempt_identity = $script:Cycle4AttemptIdentity
    status = $Status
    failure_code = $FailureCode
    failure_message = $FailureMessage
  }
}

function Get-Cycle4FrozenIdentities {
  return [ordered]@{
    launcher = Get-Cycle4FileIdentity -Path $script:Cycle4LauncherPath
    harness = Get-Cycle4FileIdentity -Path $script:Cycle4HarnessPath
    collector = Get-Cycle4FileIdentity -Path $script:Cycle4CollectorPath
  }
}

function Test-Cycle4FrozenIdentities {
  param([object]$Identities)
  return ($Identities.launcher.exists -and $Identities.launcher.sha256 -ceq $script:Cycle4PinnedLauncherSha256 -and
    $Identities.harness.exists -and $Identities.harness.sha256 -ceq $script:Cycle4PinnedHarnessSha256 -and
    $Identities.collector.exists -and $Identities.collector.sha256 -ceq $script:Cycle4PinnedCollectorSha256)
}

try {
  if (Test-Path -LiteralPath $script:Cycle4ReceiptRoot) {
    throw "ReceiptRoot already exists; Cycle 4 retry is forbidden: $($script:Cycle4ReceiptRoot)"
  }
  [System.IO.Directory]::CreateDirectory($script:Cycle4ReceiptRoot) | Out-Null
  $script:Cycle4RootOwned = $true

  if (-not $script:Cycle4FixtureRoot.Equals($script:Cycle4PinnedFixtureRoot, [System.StringComparison]::OrdinalIgnoreCase) -or
      -not (Test-Path -LiteralPath $script:Cycle4FixtureRoot -PathType Container)) {
    throw 'The pinned DSH fixture root is missing or changed.'
  }
  $gitApplications = @(Get-Command -Name 'git.exe' -All -CommandType Application -ErrorAction SilentlyContinue)
  if ($gitApplications.Count -lt 1) {
    throw 'No absolute git.exe application could be resolved.'
  }
  $script:Cycle4GitExecutable = [System.IO.Path]::GetFullPath($gitApplications[0].Source)
  if (-not [System.IO.Path]::IsPathRooted($script:Cycle4GitExecutable) -or -not (Test-Path -LiteralPath $script:Cycle4GitExecutable -PathType Leaf)) {
    throw 'The selected git.exe identity is not one existing absolute file.'
  }

  $script:Cycle4FrozenBefore = Get-Cycle4FrozenIdentities
  $cleanState = Get-Cycle4FixtureState -FixtureRoot $script:Cycle4FixtureRoot -Stage 'CLEAN_BEFORE_COPY'
  $cleanPredicates = [ordered]@{
    fixture_root_exact = $script:Cycle4FixtureRoot.Equals($script:Cycle4PinnedFixtureRoot, [System.StringComparison]::OrdinalIgnoreCase)
    pinned_head_and_exact_tag = ($cleanState.head_matches -and $cleanState.tag_matches)
    all_five_git_argument_vectors_nonempty = $cleanState.all_five_argument_vectors_nonempty
    unstaged_and_staged_tracked_diffs_empty = ($cleanState.unstaged_diff_empty -and $cleanState.staged_diff_empty)
    full_porcelain_status_empty = $cleanState.status_empty
    target_spec_absent = -not (Test-Path -LiteralPath $script:Cycle4TargetSpecPath)
    runtime_source_root_absent = -not (Test-Path -LiteralPath $script:Cycle4RuntimeSourceRoot)
    cycle3_raw_root_absent = -not (Test-Path -LiteralPath $script:Cycle4RawRoot)
    launcher_harness_collector_hashes_exact = Test-Cycle4FrozenIdentities -Identities $script:Cycle4FrozenBefore
  }
  $cleanPass = @($cleanPredicates.Values | Where-Object { -not $_ }).Count -eq 0
  $script:Cycle4CleanReceipt = New-Cycle4OwnedReceipt -LeafSchema 'clean-before-copy' -Status $(if ($cleanPass) { 'PASS' } else { 'FAIL' }) -FailureCode $(if ($cleanPass) { $null } else { 'CLEAN_BEFORE_COPY_MISMATCH' }) -FailureMessage $(if ($cleanPass) { $null } else { 'One or more strict clean-before-copy predicates failed.' })
  $script:Cycle4CleanReceipt['captured_git_executable_absolute_path'] = $script:Cycle4GitExecutable
  $script:Cycle4CleanReceipt['fixture_state'] = $cleanState
  $script:Cycle4CleanReceipt['frozen_identities'] = $script:Cycle4FrozenBefore
  $script:Cycle4CleanReceipt['predicates'] = $cleanPredicates
  $script:Cycle4CleanReceipt['pass'] = $cleanPass
  Write-Cycle4Json -Path (Join-Path $script:Cycle4ReceiptRoot 'clean-before-copy.json') -Value $script:Cycle4CleanReceipt
  if (-not $cleanPass) {
    throw 'CLEAN_BEFORE_COPY_MISMATCH'
  }

  $sourceBefore = Get-Cycle4FileIdentity -Path $script:Cycle4HarnessPath
  if (-not $sourceBefore.exists -or $sourceBefore.sha256 -cne $script:Cycle4PinnedHarnessSha256 -or (Test-Path -LiteralPath $script:Cycle4TargetSpecPath)) {
    throw 'The exact source candidate or absent destination copy precondition failed.'
  }
  $script:Cycle4CopyAttempted = $true
  [System.IO.File]::Copy($script:Cycle4HarnessPath, $script:Cycle4TargetSpecPath, $false)
  $script:Cycle4CopyCompleted = $true

  $sourceAfter = Get-Cycle4FileIdentity -Path $script:Cycle4HarnessPath
  $targetAfter = Get-Cycle4FileIdentity -Path $script:Cycle4TargetSpecPath
  $statusAfterCopy = Invoke-Cycle4Git -ArgumentVector @('-C', $script:Cycle4FixtureRoot, 'status', '--porcelain=v1', '--untracked-files=all') -WorkingDirectory $script:Cycle4FixtureRoot
  $lsFilesAfterCopy = Invoke-Cycle4Git -ArgumentVector @('-C', $script:Cycle4FixtureRoot, 'ls-files', '--error-unmatch', '--', $script:Cycle4TargetSpecRelative) -WorkingDirectory $script:Cycle4FixtureRoot
  $checkIgnoreAfterCopy = Invoke-Cycle4Git -ArgumentVector @('-C', $script:Cycle4FixtureRoot, 'check-ignore', '-q', '--', $script:Cycle4TargetSpecRelative) -WorkingDirectory $script:Cycle4FixtureRoot
  $unstagedAfterCopy = Invoke-Cycle4Git -ArgumentVector @('-C', $script:Cycle4FixtureRoot, 'diff', '--quiet', '--') -WorkingDirectory $script:Cycle4FixtureRoot
  $stagedAfterCopy = Invoke-Cycle4Git -ArgumentVector @('-C', $script:Cycle4FixtureRoot, 'diff', '--cached', '--quiet', '--') -WorkingDirectory $script:Cycle4FixtureRoot
  $copyStatus = ($statusAfterCopy.stdout_text -replace "`r`n?", "`n").Replace('\', '/').TrimEnd("`n")
  $copyStatusLines = [string[]]@(if ([string]::IsNullOrEmpty($copyStatus)) { @() } else { @($copyStatus -split "`n") })
  $expectedCopyStatus = '?? ' + $script:Cycle4TargetSpecRelative.Replace('\', '/')
  $sourceBytes = [System.IO.File]::ReadAllBytes($script:Cycle4HarnessPath)
  $targetBytes = [System.IO.File]::ReadAllBytes($script:Cycle4TargetSpecPath)
  $copyPredicates = [ordered]@{
    copy_attempted_once = $script:Cycle4CopyAttempted
    exact_source_and_destination_paths = ($sourceAfter.path -ceq $script:Cycle4HarnessPath -and $targetAfter.path -ceq $script:Cycle4TargetSpecPath)
    source_hash_exact = ($sourceAfter.sha256 -ceq $script:Cycle4PinnedHarnessSha256)
    source_unchanged = ($sourceBefore.byte_count -eq $sourceAfter.byte_count -and $sourceBefore.sha256 -ceq $sourceAfter.sha256)
    destination_exists = $targetAfter.exists
    source_destination_byte_count_equal = ($sourceAfter.byte_count -eq $targetAfter.byte_count)
    source_destination_sha_equal = ($sourceAfter.sha256 -ceq $targetAfter.sha256)
    source_destination_bytes_equal = ([Convert]::ToBase64String($sourceBytes) -ceq [Convert]::ToBase64String($targetBytes))
    porcelain_exactly_one_target_line = ($statusAfterCopy.exit_code -eq 0 -and $copyStatusLines.Count -eq 1 -and $copyStatusLines[0] -ceq $expectedCopyStatus)
    target_untracked = ($lsFilesAfterCopy.exit_code -eq 1)
    target_not_ignored = ($checkIgnoreAfterCopy.exit_code -eq 1)
    tracked_diffs_remain_empty = ($unstagedAfterCopy.exit_code -eq 0 -and $stagedAfterCopy.exit_code -eq 0)
    runtime_and_cycle3_raw_absent = (-not (Test-Path -LiteralPath $script:Cycle4RuntimeSourceRoot) -and -not (Test-Path -LiteralPath $script:Cycle4RawRoot))
  }
  $copyPass = @($copyPredicates.Values | Where-Object { -not $_ }).Count -eq 0
  $script:Cycle4CopyReceipt = New-Cycle4OwnedReceipt -LeafSchema 'target-spec-copy' -Status $(if ($copyPass) { 'PASS' } else { 'FAIL' }) -FailureCode $(if ($copyPass) { $null } else { 'TARGET_SPEC_COPY_MISMATCH' }) -FailureMessage $(if ($copyPass) { $null } else { 'The exact target-spec copy contract failed.' })
  $script:Cycle4CopyReceipt['copy_attempted'] = $script:Cycle4CopyAttempted
  $script:Cycle4CopyReceipt['copy_completed'] = $script:Cycle4CopyCompleted
  $script:Cycle4CopyReceipt['source_before'] = $sourceBefore
  $script:Cycle4CopyReceipt['source_after'] = $sourceAfter
  $script:Cycle4CopyReceipt['destination_after'] = $targetAfter
  $script:Cycle4CopyReceipt['expected_porcelain_line'] = $expectedCopyStatus
  $script:Cycle4CopyReceipt['normalized_porcelain_lines'] = $copyStatusLines
  $script:Cycle4CopyReceipt['status_command'] = $statusAfterCopy
  $script:Cycle4CopyReceipt['ls_files_command'] = $lsFilesAfterCopy
  $script:Cycle4CopyReceipt['check_ignore_command'] = $checkIgnoreAfterCopy
  $script:Cycle4CopyReceipt['unstaged_diff_command'] = $unstagedAfterCopy
  $script:Cycle4CopyReceipt['staged_diff_command'] = $stagedAfterCopy
  $script:Cycle4CopyReceipt['predicates'] = $copyPredicates
  $script:Cycle4CopyReceipt['pass'] = $copyPass
  Write-Cycle4Json -Path (Join-Path $script:Cycle4ReceiptRoot 'target-spec-copy-receipt.json') -Value $script:Cycle4CopyReceipt
  if (-not $copyPass) {
    throw 'TARGET_SPEC_COPY_MISMATCH'
  }

  $pwshFull = [System.IO.Path]::GetFullPath((Get-Process -Id $PID).Path)
  $launcherVector = [string[]]@(
    '-NoLogo', '-NoProfile', '-File', $script:Cycle4LauncherPath,
    '-Mode', $script:Cycle4Mode,
    '-FixtureRoot', $script:Cycle4FixtureRoot,
    '-ReceiptRoot', $script:Cycle4LauncherReceiptRoot,
    '-AttemptIdentity', $script:Cycle4AttemptIdentity,
    '-ExpectedCorepackPath', $script:Cycle4PinnedCorepackPath,
    '-ExpectedCorepackSha256', $script:Cycle4PinnedCorepackSha256
  )
  $script:Cycle4LauncherInvocationCount++
  $script:Cycle4LauncherProcess = Invoke-Cycle4CapturedProcess -Executable $pwshFull -ArgumentVector $launcherVector -WorkingDirectory $script:Cycle4FixtureRoot -TimeoutSeconds 180
  if ($script:Cycle4LauncherProcess.timed_out -or $script:Cycle4LauncherProcess.exit_code -ne 0) {
    throw "LAUNCHER_EXIT_FAILURE:$($script:Cycle4LauncherProcess.exit_code)"
  }

  $launcherSummaryPath = Join-Path $script:Cycle4LauncherReceiptRoot 'prevalidation-summary.json'
  $launcherAuditPath = Join-Path $script:Cycle4LauncherReceiptRoot 'bare-command-audit.json'
  $launcherManifestPath = Join-Path $script:Cycle4LauncherReceiptRoot 'manifest.json'
  $pnpmProcessPath = Join-Path $script:Cycle4LauncherReceiptRoot 'pnpm-version-process.json'
  $vitestProcessPath = Join-Path $script:Cycle4LauncherReceiptRoot 'vitest-version-process.json'
  foreach ($requiredPath in @($launcherSummaryPath, $launcherAuditPath, $launcherManifestPath, $pnpmProcessPath, $vitestProcessPath)) {
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
      throw "LAUNCHER_REQUIRED_RECEIPT_MISSING:$requiredPath"
    }
  }
  $launcherSummary = Get-Content -Raw -LiteralPath $launcherSummaryPath | ConvertFrom-Json
  $launcherAudit = Get-Content -Raw -LiteralPath $launcherAuditPath | ConvertFrom-Json
  $pnpmProcess = Get-Content -Raw -LiteralPath $pnpmProcessPath | ConvertFrom-Json
  $vitestProcess = Get-Content -Raw -LiteralPath $vitestProcessPath | ConvertFrom-Json
  $launcherManifest = Get-Content -Raw -LiteralPath $launcherManifestPath | ConvertFrom-Json
  $launcherManifestVerification = Test-Cycle4SelfExcludingManifest -Root $script:Cycle4LauncherReceiptRoot -ManifestPath $launcherManifestPath
  $launcherExpectedMembers = [string[]]@(
    'bare-command-audit.json', 'cleanup-receipt.json', 'corepack-identity.json', 'exact-launcher-command.txt',
    'focused-poststate.json', 'focused-prestate.json', 'frozen-candidate-identities.json', 'launcher-identity.json',
    'package-manager.json', 'path-scope.json', 'pnpm-version-command.txt', 'pnpm-version-exit-code.txt',
    'pnpm-version-process.json', 'pnpm-version-stderr.txt', 'pnpm-version-stdout.txt', 'prevalidation-summary.json',
    'vitest-resolution.json', 'vitest-version-command.txt', 'vitest-version-exit-code.txt', 'vitest-version-process.json',
    'vitest-version-stderr.txt', 'vitest-version-stdout.txt', 'working-directory.json'
  )
  $launcherActualMembers = [string[]]@(Get-ChildItem -LiteralPath $script:Cycle4LauncherReceiptRoot -File -Force | Where-Object { $_.Name -cne 'manifest.json' } | Sort-Object Name | ForEach-Object { $_.Name })
  $launcherTreeExact = ($launcherExpectedMembers.Count -eq $launcherActualMembers.Count)
  if ($launcherTreeExact) {
    for ($index = 0; $index -lt $launcherExpectedMembers.Count; $index++) {
      if ($launcherExpectedMembers[$index] -cne $launcherActualMembers[$index]) { $launcherTreeExact = $false; break }
    }
  }
  $pnpmVector = [string[]]@($pnpmProcess.argument_vector)
  $vitestVector = [string[]]@($vitestProcess.argument_vector)
  $pnpmVectorExact = ($pnpmVector.Count -eq 2 -and $pnpmVector[0] -ceq 'pnpm' -and $pnpmVector[1] -ceq '--version')
  $vitestVectorExact = ($vitestVector.Count -eq 4 -and $vitestVector[0] -ceq 'pnpm' -and $vitestVector[1] -ceq 'exec' -and $vitestVector[2] -ceq 'vitest' -and $vitestVector[3] -ceq '--version')
  $pnpmOutput = (Get-Content -Raw -LiteralPath (Join-Path $script:Cycle4LauncherReceiptRoot 'pnpm-version-stdout.txt') -ErrorAction Stop).Trim()
  $vitestOutput = (Get-Content -Raw -LiteralPath (Join-Path $script:Cycle4LauncherReceiptRoot 'vitest-version-stdout.txt') -ErrorAction Stop).Trim()
  $launcherPredicates = [ordered]@{
    launcher_invoked_exactly_once = ($script:Cycle4LauncherInvocationCount -eq 1)
    launcher_exit_zero = ($script:Cycle4LauncherProcess.exit_code -eq 0 -and -not $script:Cycle4LauncherProcess.timed_out)
    launcher_summary_pass = ($launcherSummary.status -ceq 'PASS' -and $launcherSummary.mode -ceq $script:Cycle4Mode)
    launcher_source_hash_exact = ($launcherSummary.launcher_source_sha256 -ceq $script:Cycle4PinnedLauncherSha256)
    launcher_child_process_count_two = (@($launcherSummary.actual_processes).Count -eq 2 -and [int]$launcherAudit.actual_invocation_count -eq 2)
    prevalidation_probe_count_two = ([int]$launcherSummary.prevalidation_probe_count -eq 2)
    focused_command_count_zero = ([int]$launcherSummary.focused_command_count -eq 0)
    target_spec_execution_count_zero = ([int]$launcherSummary.target_spec_execution_count -eq 0)
    collector_invocation_count_zero = ([int]$launcherAudit.launcher_source_collector_execution_ast_count -eq 0)
    bare_corepack_invocation_count_zero = ([int]$launcherSummary.bare_corepack_invocation_count -eq 0)
    exact_absolute_corepack_processes = ($pnpmProcess.executable_absolute_path -ieq $script:Cycle4PinnedCorepackPath -and $vitestProcess.executable_absolute_path -ieq $script:Cycle4PinnedCorepackPath)
    exact_probe_argument_vectors = ($pnpmVectorExact -and $vitestVectorExact)
    pnpm_output_exact = ($pnpmOutput -ceq '11.7.0')
    vitest_output_valid = ($vitestOutput -match '(?i)vitest[/\s]+4\.1\.8')
    launcher_nested_manifest_exact = ($launcherManifest.file_count -eq 23 -and $launcherManifestVerification.pass)
    launcher_receipt_tree_exact = $launcherTreeExact
    target_runtime_raw_boundaries = ($launcherSummary.target_spec_present -eq $true -and $launcherSummary.runtime_source_root_absent -eq $true -and $launcherSummary.cycle3_raw_absent -eq $true)
  }
  $launcherPass = @($launcherPredicates.Values | Where-Object { -not $_ }).Count -eq 0
  $script:Cycle4LauncherValidation = [ordered]@{
    predicates = $launcherPredicates
    nested_manifest_verification = $launcherManifestVerification
    expected_member_count = $launcherExpectedMembers.Count
    actual_member_count = $launcherActualMembers.Count
    expected_members = $launcherExpectedMembers
    actual_members = $launcherActualMembers
    pnpm_output = $pnpmOutput
    vitest_output = $vitestOutput
    pass = $launcherPass
  }
  if (-not $launcherPass) {
    throw 'LAUNCHER_RECEIPT_VALIDATION_FAILED'
  }

  $script:Cycle4Status = 'PASS'
  $script:Cycle4FailureCode = $null
  $script:Cycle4FailureMessage = $null
  $script:Cycle4ExitCode = 0
} catch {
  $script:Cycle4Status = 'FAIL'
  $script:Cycle4FailureCode = 'CYCLE4_ORCHESTRATION_FAILURE'
  $script:Cycle4FailureMessage = $_.Exception.Message
  $script:Cycle4ExitCode = 90
} finally {
  if ($script:Cycle4RootOwned) {
    if ($null -eq $script:Cycle4CleanReceipt) {
      $script:Cycle4CleanReceipt = New-Cycle4OwnedReceipt -LeafSchema 'clean-before-copy' -Status 'FAIL' -FailureCode $script:Cycle4FailureCode -FailureMessage $script:Cycle4FailureMessage
      $script:Cycle4CleanReceipt['pass'] = $false
      Write-Cycle4Json -Path (Join-Path $script:Cycle4ReceiptRoot 'clean-before-copy.json') -Value $script:Cycle4CleanReceipt
    }
    if ($null -eq $script:Cycle4CopyReceipt) {
      $copyStatus = if ($script:Cycle4CopyAttempted) { 'FAIL' } else { 'NOT_ATTEMPTED' }
      $script:Cycle4CopyReceipt = New-Cycle4OwnedReceipt -LeafSchema 'target-spec-copy' -Status $copyStatus -FailureCode $script:Cycle4FailureCode -FailureMessage $script:Cycle4FailureMessage
      $script:Cycle4CopyReceipt['copy_attempted'] = $script:Cycle4CopyAttempted
      $script:Cycle4CopyReceipt['copy_completed'] = $script:Cycle4CopyCompleted
      $script:Cycle4CopyReceipt['source_path'] = $script:Cycle4HarnessPath
      $script:Cycle4CopyReceipt['destination_path'] = $script:Cycle4TargetSpecPath
      $script:Cycle4CopyReceipt['destination_absent'] = -not (Test-Path -LiteralPath $script:Cycle4TargetSpecPath)
      $script:Cycle4CopyReceipt['pass'] = $false
      Write-Cycle4Json -Path (Join-Path $script:Cycle4ReceiptRoot 'target-spec-copy-receipt.json') -Value $script:Cycle4CopyReceipt
    }

    $existedBeforeRemoval = Test-Path -LiteralPath $script:Cycle4TargetSpecPath -PathType Leaf
    $removalAttempted = $false
    $removalError = $null
    if ($existedBeforeRemoval) {
      $expectedTarget = [System.IO.Path]::GetFullPath((Join-Path $script:Cycle4PinnedFixtureRoot $script:Cycle4TargetSpecRelative))
      if (-not $script:Cycle4TargetSpecPath.Equals($expectedTarget, [System.StringComparison]::OrdinalIgnoreCase)) {
        $removalError = 'Exact target-spec cleanup path changed; deletion was refused.'
      } else {
        $removalAttempted = $true
        try {
          [System.IO.File]::Delete($script:Cycle4TargetSpecPath)
        } catch {
          $removalError = $_.Exception.Message
        }
      }
    }
    $targetAbsentAfter = -not (Test-Path -LiteralPath $script:Cycle4TargetSpecPath)
    $removalPass = ($targetAbsentAfter -and $null -eq $removalError)
    $script:Cycle4RemovalReceipt = New-Cycle4OwnedReceipt -LeafSchema 'target-spec-removal' -Status $(if ($removalPass) { 'PASS' } else { 'FAIL' }) -FailureCode $(if ($removalPass) { $null } else { 'TARGET_SPEC_REMOVAL_FAILED' }) -FailureMessage $removalError
    $script:Cycle4RemovalReceipt['target_spec_path'] = $script:Cycle4TargetSpecPath
    $script:Cycle4RemovalReceipt['existed_before_removal'] = $existedBeforeRemoval
    $script:Cycle4RemovalReceipt['removal_attempted'] = $removalAttempted
    $script:Cycle4RemovalReceipt['removal_method'] = if ($removalAttempted) { 'SYSTEM_IO_FILE_DELETE_EXACT_LITERAL_PATH' } else { 'NOT_ATTEMPTED_TARGET_ABSENT' }
    $script:Cycle4RemovalReceipt['other_cleanup_attempted'] = $false
    $script:Cycle4RemovalReceipt['removal_error'] = $removalError
    $script:Cycle4RemovalReceipt['target_absent_after'] = $targetAbsentAfter
    $script:Cycle4RemovalReceipt['pass'] = $removalPass
    Write-Cycle4Json -Path (Join-Path $script:Cycle4ReceiptRoot 'target-spec-removal-receipt.json') -Value $script:Cycle4RemovalReceipt

    try {
      if ($null -ne $script:Cycle4GitExecutable -and (Test-Path -LiteralPath $script:Cycle4FixtureRoot -PathType Container)) {
        $script:Cycle4FinalFixtureState = Get-Cycle4FixtureState -FixtureRoot $script:Cycle4FixtureRoot -Stage 'FINAL_AFTER_EXACT_REMOVAL'
      }
    } catch {
      if ($null -eq $removalError) { $removalError = $_.Exception.Message }
    }
    $script:Cycle4FrozenAfter = Get-Cycle4FrozenIdentities
    $pathRestored = ($env:PATH -ceq $script:Cycle4OriginalProcessPath)
    $finalFixturePass = ($null -ne $script:Cycle4FinalFixtureState -and $script:Cycle4FinalFixtureState.strict_clean)
    $frozenUnchanged = ($null -ne $script:Cycle4FrozenBefore -and $null -ne $script:Cycle4FrozenAfter -and
      $script:Cycle4FrozenBefore.launcher.sha256 -ceq $script:Cycle4FrozenAfter.launcher.sha256 -and
      $script:Cycle4FrozenBefore.harness.sha256 -ceq $script:Cycle4FrozenAfter.harness.sha256 -and
      $script:Cycle4FrozenBefore.collector.sha256 -ceq $script:Cycle4FrozenAfter.collector.sha256 -and
      (Test-Cycle4FrozenIdentities -Identities $script:Cycle4FrozenAfter))
    $cleanupPass = ($removalPass -and $finalFixturePass -and $targetAbsentAfter -and
      -not (Test-Path -LiteralPath $script:Cycle4RuntimeSourceRoot) -and
      -not (Test-Path -LiteralPath $script:Cycle4RawRoot) -and $pathRestored -and $frozenUnchanged)
    if (-not $cleanupPass -and $script:Cycle4Status -eq 'PASS') {
      $script:Cycle4Status = 'FAIL'
      $script:Cycle4FailureCode = 'CYCLE4_CLEANUP_OR_FINAL_STATE_FAILURE'
      $script:Cycle4FailureMessage = 'Exact removal, final fixture, PATH, or frozen-identity proof failed.'
      $script:Cycle4ExitCode = 90
    }
    $cleanupReceipt = New-Cycle4OwnedReceipt -LeafSchema 'cleanup' -Status $(if ($cleanupPass) { 'PASS' } else { 'FAIL' }) -FailureCode $(if ($cleanupPass) { $null } else { 'CYCLE4_CLEANUP_OR_FINAL_STATE_FAILURE' }) -FailureMessage $(if ($cleanupPass) { $null } else { $removalError })
    $cleanupReceipt['cleanup_scope'] = 'EXACT_TARGET_SPEC_ONLY'
    $cleanupReceipt['target_spec_removal'] = $script:Cycle4RemovalReceipt
    $cleanupReceipt['other_cleanup_attempted'] = $false
    $cleanupReceipt['final_fixture_state'] = $script:Cycle4FinalFixtureState
    $cleanupReceipt['target_spec_absent'] = $targetAbsentAfter
    $cleanupReceipt['runtime_source_root_absent'] = -not (Test-Path -LiteralPath $script:Cycle4RuntimeSourceRoot)
    $cleanupReceipt['cycle3_raw_root_absent'] = -not (Test-Path -LiteralPath $script:Cycle4RawRoot)
    $cleanupReceipt['process_path_before'] = $script:Cycle4OriginalProcessPath
    $cleanupReceipt['process_path_after'] = $env:PATH
    $cleanupReceipt['process_path_restored'] = $pathRestored
    $cleanupReceipt['frozen_identities_before'] = $script:Cycle4FrozenBefore
    $cleanupReceipt['frozen_identities_after'] = $script:Cycle4FrozenAfter
    $cleanupReceipt['frozen_identities_unchanged'] = $frozenUnchanged
    $cleanupReceipt['pass'] = $cleanupPass
    Write-Cycle4Json -Path (Join-Path $script:Cycle4ReceiptRoot 'cleanup-receipt.json') -Value $cleanupReceipt

    if ($script:Cycle4LauncherInvocationCount -eq 0 -and -not (Test-Path -LiteralPath $script:Cycle4LauncherReceiptRoot)) {
      [System.IO.Directory]::CreateDirectory($script:Cycle4LauncherReceiptRoot) | Out-Null
      Write-Cycle4SelfExcludingManifest -Root $script:Cycle4LauncherReceiptRoot -ManifestPath (Join-Path $script:Cycle4LauncherReceiptRoot 'manifest.json') -SchemaVersion '36-x01-recovery-cycle-4-empty-launcher-manifest-v1' -Mode $script:Cycle4Mode -AttemptIdentity $script:Cycle4AttemptIdentity -Status 'FAIL' -FailureCode $script:Cycle4FailureCode -FailureMessage $script:Cycle4FailureMessage -AdditionalFields ([ordered]@{ launcher_invoked = $false }) | Out-Null
    }
    $nestedManifestPath = Join-Path $script:Cycle4LauncherReceiptRoot 'manifest.json'
    $nestedManifestPresent = Test-Path -LiteralPath $nestedManifestPath -PathType Leaf
    $nestedManifestVerification = if ($nestedManifestPresent) { Test-Cycle4SelfExcludingManifest -Root $script:Cycle4LauncherReceiptRoot -ManifestPath $nestedManifestPath } else { $null }
    if ((-not $nestedManifestPresent -or -not $nestedManifestVerification.pass) -and $script:Cycle4Status -eq 'PASS') {
      $script:Cycle4Status = 'FAIL'
      $script:Cycle4FailureCode = 'NESTED_LAUNCHER_MANIFEST_FAILURE'
      $script:Cycle4FailureMessage = 'The launcher subtree manifest is missing or mismatched.'
      $script:Cycle4ExitCode = 90
    }

    $launcherChildCount = if ($null -ne $script:Cycle4LauncherValidation -and $script:Cycle4LauncherValidation.pass) { 2 } else { 0 }
    $prevalidationProbeCount = if ($null -ne $script:Cycle4LauncherValidation -and $script:Cycle4LauncherValidation.pass) { 2 } else { 0 }
    $pnpmOutputFinal = if ($null -ne $script:Cycle4LauncherValidation) { $script:Cycle4LauncherValidation.pnpm_output } else { $null }
    $vitestOutputFinal = if ($null -ne $script:Cycle4LauncherValidation) { $script:Cycle4LauncherValidation.vitest_output } else { $null }
    $summary = New-Cycle4OwnedReceipt -LeafSchema 'attempt-summary' -Status $script:Cycle4Status -FailureCode $script:Cycle4FailureCode -FailureMessage $script:Cycle4FailureMessage
    $summary['historical_orchestration_source'] = 'MISSING_NOT_PROVABLE'
    $summary['independent_cycle4_candidate'] = $true
    $summary['orchestrator_source'] = Get-Cycle4FileIdentity -Path $PSCommandPath
    $summary['orchestrator_invocation_count'] = 1
    $summary['launcher_invocation_count'] = $script:Cycle4LauncherInvocationCount
    $summary['launcher_process'] = $script:Cycle4LauncherProcess
    $summary['launcher_validation'] = $script:Cycle4LauncherValidation
    $summary['launcher_child_process_count'] = $launcherChildCount
    $summary['prevalidation_probe_count'] = $prevalidationProbeCount
    $summary['focused_command_count'] = 0
    $summary['target_spec_execution_count'] = 0
    $summary['collector_invocation_count'] = 0
    $summary['bare_corepack_invocation_count'] = 0
    $summary['pnpm_version_output'] = $pnpmOutputFinal
    $summary['vitest_version_output'] = $vitestOutputFinal
    $summary['clean_before_copy_pass'] = ($null -ne $script:Cycle4CleanReceipt -and $script:Cycle4CleanReceipt.pass)
    $summary['target_spec_copy_pass'] = ($null -ne $script:Cycle4CopyReceipt -and $script:Cycle4CopyReceipt.pass)
    $summary['target_spec_removal_pass'] = $script:Cycle4RemovalReceipt.pass
    $summary['cleanup_pass'] = $cleanupPass
    $summary['nested_manifest_present'] = $nestedManifestPresent
    $summary['nested_manifest_verification'] = $nestedManifestVerification
    $summary['target_spec_absent_final'] = $targetAbsentAfter
    $summary['runtime_source_root_absent_final'] = -not (Test-Path -LiteralPath $script:Cycle4RuntimeSourceRoot)
    $summary['cycle3_raw_root_absent_final'] = -not (Test-Path -LiteralPath $script:Cycle4RawRoot)
    $summary['fixture_pinned_and_clean_final'] = $finalFixturePass
    $summary['frozen_identities_unchanged'] = $frozenUnchanged
    $summary['claim_ceiling'] = 'LAUNCHER_ONLY_PREVALIDATION_NOT_DSH_RUNTIME_EVIDENCE'
    $summary['target_experiment_authorized'] = $false
    $summary['retry_authorized'] = $false
    $summary['next_allowed_gate'] = if ($script:Cycle4Status -eq 'PASS') { 'REVIEW_RECHECK' } else { 'NONE' }
    $summary['blocker'] = if ($script:Cycle4Status -eq 'PASS') { 'NONE' } else { 'HUMAN_DECISION_REQUIRED' }
    Write-Cycle4Json -Path (Join-Path $script:Cycle4ReceiptRoot 'attempt-summary.json') -Value $summary -Depth 22

    Write-Cycle4SelfExcludingManifest -Root $script:Cycle4ReceiptRoot -ManifestPath (Join-Path $script:Cycle4ReceiptRoot 'manifest.json') -SchemaVersion '36-x01-recovery-cycle-4-attempt-manifest-v1' -Mode $script:Cycle4Mode -AttemptIdentity $script:Cycle4AttemptIdentity -Status $script:Cycle4Status -FailureCode $script:Cycle4FailureCode -FailureMessage $script:Cycle4FailureMessage -AdditionalFields ([ordered]@{ nested_launcher_manifest_present = $nestedManifestPresent; launcher_invoked = ($script:Cycle4LauncherInvocationCount -eq 1); retry_authorized = $false }) | Out-Null
  }
}

exit $script:Cycle4ExitCode
