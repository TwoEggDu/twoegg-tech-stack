# Cycle 10 synthetic diagnostic. Invoke at most once, only after fresh preflight PASS.
$ErrorActionPreference = 'Stop'
$repo = 'E:\workspace\TechStackShow'
$base = $repo + '\docs\agent-engineering-course\articles\36-dsh-cost-compaction-trace-cancellation-recovery\experiments'
$root = 'C:\Users\IGG\AppData\Local\Temp\dsh-a36-c10-caller-probe-20260907'
$exe = 'C:\Users\IGG\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\powershell\pwsh.exe'
$subject = $base + '\36-x01-recovery-cycle-10-subject.ps1'
$frozen = $base + '\preflight\36-x01-recovery-cycle-10-tests'
$utf8 = [System.Text.UTF8Encoding]::new($false)
$ownedRoot = $false
$process = $null
$launched = $false
$childPid = 'NOT_STARTED'
$timedOut = $false
$exitObserved = $false
$childExit = 'NOT_OBSERVED'
$killRequested = $false
function Write-Receipt([string]$name, [string]$value) {
    [System.IO.File]::WriteAllText(($root + '\' + $name), $value, $utf8)
}
function File-Sha([string]$path) {
    [System.Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData([System.IO.File]::ReadAllBytes($path)))
}
try {
    # Refuse all existing roots; never overwrite or select another root.
    if ([System.IO.Directory]::Exists($root) -or [System.IO.File]::Exists($root)) {
        throw 'Fixed diagnostic root already exists; no attempt or overwrite is allowed.'
    }
    [void][System.IO.Directory]::CreateDirectory($root)
    $ownedRoot = $true
    Write-Receipt 'started.kv' ("synthetic=true`ncaller_pid=$PID`ncaller_utc=$([DateTime]::UtcNow.ToString('o'))`ncaller_psversion=$($PSVersionTable.PSVersion)`ncaller_os=$([Environment]::OSVersion.VersionString)`ncaller_cwd=$((Get-Location).Path)`ncaller_executable=$([Environment]::ProcessPath)`n")
    [System.IO.File]::Copy(($frozen + '\invocation.txt'), ($root + '\invocation.txt'), $false)
    [System.IO.File]::Copy(($frozen + '\source-identity.kv'), ($root + '\source-identity.kv'), $false)
    if ((Get-Location).Path -cne $repo -or $PSCommandPath -cne ($base + '\36-x01-recovery-cycle-10-caller.ps1')) { throw 'Caller path or cwd mismatch.' }
    if ([Environment]::ProcessPath -ine $exe -or $PSVersionTable.PSVersion.ToString() -ne '7.6.5') { throw 'Caller executable or version mismatch.' }
    $identity = @{}
    foreach ($line in [System.IO.File]::ReadAllLines(($root + '\source-identity.kv'))) {
        if ($line.Length -eq 0) { continue }
        $cut = $line.IndexOf('=')
        if ($cut -lt 1) { throw 'Malformed provenance line.' }
        $key = $line.Substring(0, $cut)
        if ($identity.ContainsKey($key)) { throw 'Duplicate provenance key.' }
        $identity[$key] = $line.Substring($cut + 1)
    }
    $paths = [ordered]@{
        plan = $base + '\36-x01-recovery-cycle-10-plan.md'
        caller = $PSCommandPath
        subject = $subject
        invocation = $frozen + '\invocation.txt'
        executable = $exe
    }
    foreach ($label in $paths.Keys) {
        $path = $paths[$label]
        $hash = File-Sha $path
        $bytes = [System.IO.FileInfo]::new($path).Length
        $matches = $identity[($label + '.path')] -ceq $path -and $identity[($label + '.bytes')] -ceq $bytes.ToString([Globalization.CultureInfo]::InvariantCulture) -and $identity[($label + '.sha256')] -ceq $hash
        [System.IO.File]::AppendAllText(($root + '\started.kv'), ("actual.$label.path=$path`nactual.$label.bytes=$bytes`nactual.$label.sha256=$hash`nactual.$label.matches_frozen=$matches`n"), $utf8)
        if (-not $matches) { throw "Frozen provenance mismatch: $label" }
    }
    if ((File-Sha ($frozen + '\invocation.txt')) -cne (File-Sha ($root + '\invocation.txt')) -or (File-Sha ($frozen + '\source-identity.kv')) -cne (File-Sha ($root + '\source-identity.kv'))) { throw 'Copied provenance differs from frozen bytes.' }
    $expectedInvocation = "& '$exe' -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File '$PSCommandPath'"
    if ([System.IO.File]::ReadAllText(($root + '\invocation.txt')).TrimEnd([char[]]"`r`n") -cne $expectedInvocation) { throw 'Frozen invocation differs from actual configured invocation.' }
    $arguments = '-NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "' + $subject + '"'
    $info = [System.Diagnostics.ProcessStartInfo]::new()
    $info.FileName = $exe
    $info.Arguments = $arguments
    $info.WorkingDirectory = $repo
    $info.UseShellExecute = $false
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
    $info.StandardOutputEncoding = $utf8
    $info.StandardErrorEncoding = $utf8
    $info.CreateNoWindow = $true
    Write-Receipt 'child-command.txt' ("executable=$($info.FileName)`narguments=$($info.Arguments)`ncwd=$($info.WorkingDirectory)`nUseShellExecute=false`nRedirectStandardOutput=true`nRedirectStandardError=true`nCreateNoWindow=true`n")
    # All four prelaunch files are closed before the only Start call.
    $process = [System.Diagnostics.Process]::new()
    $process.StartInfo = $info
    $launched = $process.Start()
    if (-not $launched) { throw 'Process.Start did not report a launch.' }
    $childPid = $process.Id
    $exitObserved = $process.WaitForExit(10000)
    if (-not $exitObserved) {
        $timedOut = $true
        $killRequested = $true
        $process.Kill() # Only this owned child, never process-name matching or a tree kill.
        $exitObserved = $process.WaitForExit(2000)
        if (-not $exitObserved) { throw 'Owned child exit not observed after bounded termination wait; streams not read.' }
    }
    $childExit = $process.ExitCode
    # The reviewed subject has no descendants and bounded output; read only after exit.
    $stdout = $process.StandardOutput.ReadToEnd()
    $stderr = $process.StandardError.ReadToEnd()
    Write-Receipt 'stdout.txt' $stdout
    Write-Receipt 'stderr.txt' $stderr
    $stdoutBytes = [System.IO.FileInfo]::new(($root + '\stdout.txt')).Length
    $stderrBytes = [System.IO.FileInfo]::new(($root + '\stderr.txt')).Length
    $lines = $stdout -split '\r?\n'
    $complete = -not $timedOut -and $childExit -eq 23 -and $lines -ccontains 'SYNTHETIC_SUBJECT_START' -and $lines -ccontains 'SYNTHETIC_PARSE_CAUGHT' -and $lines -ccontains "child_pid=$childPid" -and $lines -ccontains 'child_psversion=7.6.5' -and -not $stdout.Contains('UNEXPECTED_PARSE_SUCCESS') -and $stderr.Length -gt 0 -and $stdoutBytes -lt 4096 -and $stderrBytes -lt 4096
    Write-Receipt 'terminal.kv' ("utc=$([DateTime]::UtcNow.ToString('o'))`nchild_pid=$childPid`nchild_started=$launched`ntimeout=$timedOut`nkill_requested=$killRequested`nexit_observed=$exitObserved`nchild_exit=$childExit`nstream_representation=DECODED_UTF8_TEXT`nstdout_bytes=$stdoutBytes`nstdout_sha256=$(File-Sha ($root + '\stdout.txt'))`nstderr_bytes=$stderrBytes`nstderr_sha256=$(File-Sha ($root + '\stderr.txt'))`nexpected_failure_complete=$complete`n")
    if ($complete) { exit 0 }
    exit 1
} catch {
    $actualError = $_.ToString() + "`nexception_type=" + $_.Exception.GetType().FullName + "`nerror_id=" + $_.FullyQualifiedErrorId + "`n"
    if ($launched -and -not $exitObserved) {
        try {
            if (-not $process.HasExited) { $killRequested = $true; $process.Kill() }
            $exitObserved = $process.WaitForExit(2000)
            if ($exitObserved) { $childExit = $process.ExitCode }
        } catch { $actualError += "termination_error=$($_.ToString())`n" }
    }
    if ($ownedRoot) {
        try { Write-Receipt 'caller-error.txt' $actualError } catch { [Console]::Error.WriteLine('caller-error.txt MISSING: ' + $_.ToString()) }
        try { Write-Receipt 'terminal.kv' ("utc=$([DateTime]::UtcNow.ToString('o'))`nchild_pid=$childPid`nchild_started=$launched`ntimeout=$timedOut`nkill_requested=$killRequested`nexit_observed=$exitObserved`nchild_exit=$childExit`nexpected_failure_complete=false`ncaller_exception=true`nstreams=INSPECT_FILES_NO_COMPLETENESS_CLAIM`n") } catch { [Console]::Error.WriteLine('terminal.kv MISSING: ' + $_.ToString()) }
    }
    [Console]::Error.WriteLine($actualError)
    exit 1
} finally {
    if ($null -ne $process) { $process.Dispose() }
}
