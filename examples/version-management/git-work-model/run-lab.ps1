[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$LabRoot,
    [Parameter(Mandatory = $true)][string]$EvidenceDirectory
)

$ErrorActionPreference = 'Stop'
$labPath = [IO.Path]::GetFullPath($LabRoot)
$evidencePath = [IO.Path]::GetFullPath($EvidenceDirectory)
if ((Test-Path -LiteralPath $labPath) -or (Test-Path -LiteralPath $evidencePath)) {
    throw 'Both output paths must be new. Existing directories are never removed or reused.'
}
foreach ($name in @('GIT_DIR', 'GIT_WORK_TREE', 'GIT_INDEX_FILE', 'GIT_CONFIG_COUNT', 'GIT_AUTHOR_NAME', 'GIT_AUTHOR_EMAIL', 'GIT_COMMITTER_NAME', 'GIT_COMMITTER_EMAIL')) {
    if ([Environment]::GetEnvironmentVariable($name, 'Process')) {
        throw "Unset $name before running this isolated lab."
    }
}
$null = New-Item -ItemType Directory -Path $labPath
$null = New-Item -ItemType Directory -Path $evidencePath
$utf8 = New-Object System.Text.UTF8Encoding($false)
$emptyConfig = Join-Path $labPath 'empty.gitconfig'
$emptyTemplate = Join-Path $labPath 'empty-template'
[IO.File]::WriteAllText($emptyConfig, '', $utf8)
$null = New-Item -ItemType Directory -Path $emptyTemplate
$savedGlobal = [Environment]::GetEnvironmentVariable('GIT_CONFIG_GLOBAL', 'Process')
$savedNoSystem = [Environment]::GetEnvironmentVariable('GIT_CONFIG_NOSYSTEM', 'Process')
$env:GIT_CONFIG_GLOBAL = $emptyConfig
$env:GIT_CONFIG_NOSYSTEM = '1'
$records = New-Object 'System.Collections.Generic.List[object]'
$checks = New-Object 'System.Collections.Generic.List[string]'
$snapshots = [ordered]@{}
$passed = $false
$failure = $null

function Public-Text([string]$Value) {
    return $Value.Replace($labPath, '[LAB]').Replace($labPath.Replace('\', '/'), '[LAB]')
}

function Lab-Git([string]$Directory, [string[]]$GitArguments) {
    $previousPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $lines = @(& git -C $Directory @GitArguments 2>&1 | ForEach-Object { $_.ToString() })
        $gitExit = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $previousPreference
    }
    $output = $lines -join "`n"
    $records.Add([ordered]@{
        directory = Public-Text $Directory
        arguments = @($GitArguments | ForEach-Object { Public-Text $_ })
        exit_code = $gitExit
        output = Public-Text $output
    })
    if ($gitExit -ne 0) { throw "Git failed ($gitExit): $($GitArguments -join ' ')" }
    return $output
}

function Check([bool]$Condition, [string]$Label) {
    if (-not $Condition) { throw "Assertion failed: $Label" }
    $checks.Add($Label)
}

function Write-Rules([string]$Content) {
    [IO.File]::WriteAllText((Join-Path $developer 'skill.rules'), $Content, $utf8)
    $records.Add([ordered]@{ action = 'write_utf8_no_bom_lf'; path = '[LAB]/developer/skill.rules'; content = $Content })
}

function Snapshot([string]$Directory) {
    return [ordered]@{
        worktree = [IO.File]::ReadAllText((Join-Path $Directory 'skill.rules')).TrimEnd("`r", "`n")
        index = Lab-Git $Directory @('show', ':skill.rules')
        head = Lab-Git $Directory @('show', 'HEAD:skill.rules')
        status = Lab-Git $Directory @('status', '--short')
        head_commit = Lab-Git $Directory @('rev-parse', 'HEAD')
        remote_tracking = Lab-Git $Directory @('rev-parse', 'origin/main')
    }
}

$developer = Join-Path $labPath 'developer'
$receiver = Join-Path $labPath 'receiver'
$baselineText = "damage=100`ncharge=off"
$candidateText = "damage=80`ncharge=off"
$workingText = "damage=80`ncharge=on"
try {
    $gitVersion = Lab-Git $labPath @('--version')
    $null = Lab-Git $labPath @('init', '--bare', '--initial-branch=main', "--template=$emptyTemplate", 'shared.git')
    $null = Lab-Git $labPath @('init', '--initial-branch=main', "--template=$emptyTemplate", 'developer')
    foreach ($setting in @(@('user.name', 'Version Management Lab'), @('user.email', 'lab@example.invalid'), @('core.autocrlf', 'false'), @('core.eol', 'lf'), @('commit.gpgsign', 'false'))) {
        $null = Lab-Git $developer @('config', '--local', $setting[0], $setting[1])
    }
    $null = Lab-Git $developer @('remote', 'add', 'origin', (Join-Path $labPath 'shared.git'))
    Write-Rules ($baselineText + "`n")
    $null = Lab-Git $developer @('add', '--', 'skill.rules')
    $null = Lab-Git $developer @('commit', '-m', 'Baseline')
    $baseline = Lab-Git $developer @('rev-parse', 'HEAD')
    $null = Lab-Git $developer @('push', '-u', 'origin', 'main')
    $null = Lab-Git $labPath @('clone', '--no-hardlinks', "--template=$emptyTemplate", 'shared.git', 'receiver')
    $null = Lab-Git $receiver @('config', '--local', 'core.autocrlf', 'false')
    $null = Lab-Git $receiver @('config', '--local', 'core.eol', 'lf')
    $snapshots['baseline_receiver'] = Snapshot $receiver
    Check ($snapshots['baseline_receiver'].status -eq '') 'receiver starts clean'
    Check ($snapshots['baseline_receiver'].head -eq $baselineText) 'receiver starts at baseline content'

    Write-Rules ($candidateText + "`n")
    $null = Lab-Git $developer @('add', '--', 'skill.rules')
    Write-Rules ($workingText + "`n")
    $snapshots['staged_then_edited'] = Snapshot $developer
    $unstagedDiff = Lab-Git $developer @('diff', '--', 'skill.rules')
    $stagedDiff = Lab-Git $developer @('diff', '--cached', '--', 'skill.rules')
    Check ($snapshots['staged_then_edited'].status -eq 'MM skill.rules') 'both index and worktree differ from their comparison bases'
    Check ($snapshots['staged_then_edited'].index -eq $candidateText) 'index retains staged damage change without charge change'
    Check ($snapshots['staged_then_edited'].head -eq $baselineText) 'HEAD retains baseline before commit'
    Check ($snapshots['staged_then_edited'].worktree -eq $workingText) 'worktree includes later charge edit'
    Check ($unstagedDiff.Contains('+charge=on') -and -not $unstagedDiff.Contains('+damage=80')) 'unstaged diff isolates the later charge edit'
    Check ($stagedDiff.Contains('+damage=80') -and -not $stagedDiff.Contains('+charge=on')) 'cached diff isolates the staged damage edit'

    $null = Lab-Git $developer @('commit', '-m', 'Reduce damage to 80')
    $candidate = Lab-Git $developer @('rev-parse', 'HEAD')
    $snapshots['committed_not_pushed'] = Snapshot $developer
    $remoteBefore = Lab-Git $developer @('ls-remote', '--heads', 'origin', 'refs/heads/main')
    Check ($snapshots['committed_not_pushed'].status -eq ' M skill.rules') 'commit leaves the unstaged charge edit in place'
    Check ($snapshots['committed_not_pushed'].head -eq $candidateText) 'commit records staged content only'
    Check (($remoteBefore -split '\s+')[0] -eq $baseline) 'remote remains at baseline before push'
    Check ($snapshots['committed_not_pushed'].remote_tracking -eq $baseline) 'developer remote-tracking reference still records baseline before push'

    $null = Lab-Git $developer @('push', 'origin', 'main')
    $remoteAfter = Lab-Git $developer @('ls-remote', '--heads', 'origin', 'refs/heads/main')
    $snapshots['receiver_before_fetch'] = Snapshot $receiver
    Check (($remoteAfter -split '\s+')[0] -eq $candidate) 'remote points to candidate after push'
    Check ($snapshots['receiver_before_fetch'].head_commit -eq $baseline) 'push does not move receiver HEAD'
    Check ($snapshots['receiver_before_fetch'].worktree -eq $baselineText) 'push does not rewrite receiver files'
    Check ($snapshots['receiver_before_fetch'].remote_tracking -eq $baseline) 'receiver remote-tracking reference remains stale before fetch'

    $null = Lab-Git $receiver @('fetch', 'origin')
    $snapshots['receiver_after_fetch'] = Snapshot $receiver
    Check ($snapshots['receiver_after_fetch'].remote_tracking -eq $candidate) 'fetch refreshes receiver origin/main'
    Check ($snapshots['receiver_after_fetch'].head_commit -eq $baseline) 'fetch leaves receiver HEAD unchanged'
    Check ($snapshots['receiver_after_fetch'].worktree -eq $baselineText) 'fetch leaves receiver files unchanged'

    $null = Lab-Git $receiver @('-c', 'advice.detachedHead=false', 'switch', '--detach', $candidate)
    $snapshots['receiver_at_candidate'] = Snapshot $receiver
    $snapshots['developer_final'] = Snapshot $developer
    Check ($snapshots['receiver_at_candidate'].status -eq '') 'receiver is clean at the selected candidate'
    Check ($snapshots['receiver_at_candidate'].head_commit -eq $candidate) 'receiver checks out the exact candidate commit'
    Check ($snapshots['receiver_at_candidate'].worktree -eq $candidateText) 'receiver gets damage fix without unfinished charge edit'
    Check ($snapshots['developer_final'].worktree -eq $workingText) 'developer retains unfinished charge edit after sharing'
    $passed = $true
} catch {
    $failure = $_.Exception.Message
    throw
} finally {
    $receipt = [ordered]@{
        passed = $passed
        failure = $failure
        recorded_at_utc = [DateTime]::UtcNow.ToString('o')
        git_version = $gitVersion
        powershell_version = $PSVersionTable.PSVersion.ToString()
        os_version = [Environment]::OSVersion.VersionString
        scope = 'Local bare remote, two worktrees, text file only. No network, hosting platform or engine validation.'
        config_isolation = 'Empty process-local global config, no system config, empty init template; only lab repository config is written.'
        baseline_commit = $baseline
        candidate_commit = $candidate
        snapshots = $snapshots
        passed_checks = @($checks.ToArray())
    }
    $receiptJson = ($receipt | ConvertTo-Json -Depth 12).Replace("`r`n", "`n")
    $commandsJson = (ConvertTo-Json -InputObject $records.ToArray() -Depth 8).Replace("`r`n", "`n")
    [IO.File]::WriteAllText((Join-Path $evidencePath 'receipt.json'), $receiptJson + "`n", $utf8)
    [IO.File]::WriteAllText((Join-Path $evidencePath 'commands.json'), $commandsJson + "`n", $utf8)
    [Environment]::SetEnvironmentVariable('GIT_CONFIG_GLOBAL', $savedGlobal, 'Process')
    [Environment]::SetEnvironmentVariable('GIT_CONFIG_NOSYSTEM', $savedNoSystem, 'Process')
}
Write-Output "PASS: $($checks.Count) checks. Evidence: $evidencePath"
