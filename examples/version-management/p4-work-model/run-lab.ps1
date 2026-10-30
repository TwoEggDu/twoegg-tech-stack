[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$LabRoot,
    [Parameter(Mandatory = $true)][string]$EvidenceDirectory,
    [Parameter(Mandatory = $true)][string]$P4dPath
)
$ErrorActionPreference = 'Stop'
$labPath = [IO.Path]::GetFullPath($LabRoot)
$evidencePath = [IO.Path]::GetFullPath($EvidenceDirectory)
$serverBinary = (Resolve-Path -LiteralPath $P4dPath).Path
if ($labPath -match '[^\x20-\x7E]') { throw 'Use an ASCII lab path for this non-Unicode server exercise.' }
if ((Test-Path -LiteralPath $labPath) -or (Test-Path -LiteralPath $evidencePath)) { throw 'Output directories must be new.' }
$null = New-Item -ItemType Directory -Path $labPath
$null = New-Item -ItemType Directory -Path $evidencePath
$bootstrapRoot = Join-Path $labPath 'server-init'
$serverRoot = Join-Path $bootstrapRoot '.p4root'
$rootA = Join-Path $labPath 'workspace-a'
$rootB = Join-Path $labPath 'workspace-b'
foreach ($directory in @($bootstrapRoot,$rootA,$rootB)) { $null = New-Item -ItemType Directory -Path $directory }
$utf8 = New-Object System.Text.UTF8Encoding($false)
$records = New-Object 'System.Collections.Generic.List[object]'
$checks = New-Object 'System.Collections.Generic.List[string]'
$snapshots = [ordered]@{}
$passed = $false
$failure = $null
$serverProcess = $null
$serverStopped = $false
$savedEnvironment = @{}
$isolation = @{ PATH=((Split-Path $serverBinary)+';'+$env:PATH); P4INITROOT=''; P4CONFIG='.vm-lab-disabled'; P4ENVIRO=(Join-Path $labPath 'p4env'); P4TICKETS=(Join-Path $labPath 'tickets'); P4TRUST=(Join-Path $labPath 'trust'); P4PASSWD=''; P4CHARSET='none'; P4IGNORE='.vm-lab-ignore'; P4HOST='vm-lab-host' }
foreach ($name in $isolation.Keys) { $savedEnvironment[$name]=[Environment]::GetEnvironmentVariable($name,'Process'); [Environment]::SetEnvironmentVariable($name,$isolation[$name],'Process') }

function Public-Text([string]$Value) {
    $value = [regex]::Replace($Value,[regex]::Escape($labPath),'[LAB]','IgnoreCase')
    $value = [regex]::Replace($value,[regex]::Escape($labPath.Replace('\','/')),'[LAB]','IgnoreCase')
    $value = [regex]::Replace($value,[regex]::Escape((Get-Location).Path),'[INVOCATION]','IgnoreCase')
    if ($env:COMPUTERNAME) { $value = $value.Replace($env:COMPUTERNAME,'[HOST]') }
    if ($labPassword) { $value = $value.Replace($labPassword,'[REDACTED]') }
    return $value
}
function Lab-P4([string]$Client, [string[]]$Arguments, [string]$Form='') {
    $actual = @('-p',$endpoint,'-u','vm-lab','-c',$Client) + $Arguments
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        if ($Form) { $lines = @($Form | & p4 @actual 2>&1 | ForEach-Object { $_.ToString() }) }
        else { $lines = @(& p4 @actual 2>&1 | ForEach-Object { $_.ToString() }) }
        $toolExit = $LASTEXITCODE
    } finally { $ErrorActionPreference = $previous }
    $output = $lines -join "`n"
    $records.Add([ordered]@{ tool='p4'; arguments=@($actual | ForEach-Object { Public-Text $_ }); stdin=Public-Text $Form; exit_code=$toolExit; output=Public-Text $output })
    if ($toolExit -ne 0) { throw "p4 failed ($toolExit): $($Arguments -join ' '): $output" }
    return $output.TrimEnd("`r","`n")
}
function Check([bool]$Condition,[string]$Label) { if (-not $Condition) { throw "Assertion failed: $Label" }; $checks.Add($Label) }
function Write-Rules([string]$Root,[string]$Content) {
    [IO.File]::WriteAllText((Join-Path $Root 'skill.rules'),$Content+"`n",$utf8)
    $records.Add([ordered]@{ action='write_utf8_no_bom_lf'; path=Public-Text (Join-Path $Root 'skill.rules'); content=$Content+"`n" })
}
function Client-Form([string]$Name,[string]$Root) {
    return "Client: $Name`nOwner: vm-lab`nHost:`nRoot: $Root`nOptions: noallwrite noclobber nocompress unlocked nomodtime normdir`nSubmitOptions: submitunchanged`nLineEnd: unix`nView:`n`t//depot/... //$Name/...`n"
}
function File-Text([string]$Root) { return [IO.File]::ReadAllText((Join-Path $Root 'skill.rules')).TrimEnd("`r","`n") }

$baselineText = "damage=100`ncharge=off"
$shelfText = "damage=80`ncharge=off"
$continuedText = "damage=80`ncharge=on"
try {
    $clientVersion = (& p4 -V) -join "`n"
    if ($LASTEXITCODE -ne 0) { throw 'Cannot read P4 client version.' }
    $serverVersion = (& $serverBinary -V) -join "`n"
    if ($LASTEXITCODE -ne 0) { throw 'Cannot read P4 server version.' }
    # The official init command creates the first user offline. No existing server is contacted.
    $initOutput = (& p4 -u vm-lab -d $bootstrapRoot init -C1 -n) -join "`n"
    if ($LASTEXITCODE -ne 0) { throw 'Cannot initialize the isolated personal server.' }
    $records.Add([ordered]@{ tool='p4'; arguments=@('-u','vm-lab','-d','[LAB]/server-init','init','-C1','-n'); exit_code=0; output=Public-Text $initOutput })
    $labPassword = 'Vm!'+[Guid]::NewGuid().ToString('N')+'zZ9'
    $resetOutput = (("$labPassword`n$labPassword" | & $serverBinary -r $serverRoot --reset-password vm-lab -s) -join "`n")
    if ($LASTEXITCODE -ne 0) { throw 'Cannot set isolated user password offline.' }
    $records.Add([ordered]@{ tool='p4d'; arguments=@('-r','[LAB]/server-init/.p4root','--reset-password','vm-lab','-s'); stdin='[REDACTED]'; exit_code=0; output=Public-Text $resetOutput })
    $listener = New-Object Net.Sockets.TcpListener([Net.IPAddress]::Loopback,0)
    $listener.Start()
    $port = $listener.LocalEndpoint.Port
    $listener.Stop()
    $endpoint = "127.0.0.1:$port"
    $serverArguments = '-r "{0}" -p {1} -L "{2}" -J "{3}"' -f $serverRoot,$endpoint,(Join-Path $labPath 'server.log'),(Join-Path $labPath 'journal')
    $serverProcess = Start-Process -FilePath $serverBinary -ArgumentList $serverArguments -WindowStyle Hidden -WorkingDirectory $labPath -PassThru -RedirectStandardOutput (Join-Path $labPath 'server.stdout') -RedirectStandardError (Join-Path $labPath 'server.stderr')
    $ready = $false
    for ($attempt=0; $attempt -lt 40; $attempt++) {
        if ($serverProcess.HasExited) { throw 'Lab p4d exited before becoming ready. Inspect lab server logs.' }
        $tcp = New-Object Net.Sockets.TcpClient
        try { $connection=$tcp.BeginConnect('127.0.0.1',$port,$null,$null); if ($connection.AsyncWaitHandle.WaitOne(100)) { $tcp.EndConnect($connection); $ready=$true } } catch {} finally { $tcp.Close() }
        if ($ready) { break }
        Start-Sleep -Milliseconds 100
    }
    if (-not $ready) { throw 'Lab server did not start in time.' }
    $null = Lab-P4 'vm-a' @('login') $labPassword
    $info = Lab-P4 'vm-a' @('-ztag','info')
    $rootMatch = [regex]::Match($info,'(?m)^\.\.\. serverRoot (.+)$')
    Check ($rootMatch.Success -and $rootMatch.Groups[1].Value.Trim().Replace('/','\').TrimEnd('\') -eq $serverRoot.TrimEnd('\')) 'connection identifies the newly created lab server root'
    $snapshots['initialization'] = [ordered]@{ authenticated_info=Public-Text $info; security=Lab-P4 'vm-a' @('configure','show','security'); user_creation=Lab-P4 'vm-a' @('configure','show','dm.user.noautocreate'); bootstrap='p4 init -C1 -n creates a personal server; offline reset-password -s initializes its synthetic user; explicit loopback p4d follows.' }
    $null = Lab-P4 'vm-a' @('user','-i') "User: vm-lab`nEmail: lab@example.invalid`nFullName: Version Management Lab`n"
    $null = Lab-P4 'vm-a' @('depot','-i') "Depot: depot`nOwner: vm-lab`nDescription:`n`tLocal lab depot`nType: local`nMap: depot/...`n"
    $null = Lab-P4 'vm-a' @('client','-i') (Client-Form 'vm-a' $rootA)
    $null = Lab-P4 'vm-b' @('client','-i') (Client-Form 'vm-b' $rootB)
    $where = Lab-P4 'vm-a' @('where','//depot/skill.rules')
    Check ($where.Contains('//vm-a/skill.rules') -and $where.Contains($rootA)) 'client view maps depot path into A root'
    Write-Rules $rootA $baselineText
    $null = Lab-P4 'vm-a' @('add','-t','text',(Join-Path $rootA 'skill.rules'))
    $null = Lab-P4 'vm-a' @('submit','-d','Baseline')
    $null = Lab-P4 'vm-b' @('sync','//depot/...')
    Check ((File-Text $rootB) -eq $baselineText) 'B sync obtains baseline content'
    $snapshots['baseline'] = [ordered]@{ depot=Lab-P4 'vm-a' @('print','-q','//depot/skill.rules'); a=File-Text $rootA; b=File-Text $rootB; b_have=Lab-P4 'vm-b' @('have','//depot/skill.rules') }

    $created = Lab-P4 'vm-a' @('change','-i') "Change: new`nClient: vm-a`nUser: vm-lab`nDescription:`n`tDamage change for shelf exercise`n"
    $changeMatch = [regex]::Match($created,'Change (\d+) created')
    if (-not $changeMatch.Success) { throw 'Cannot identify new pending changelist.' }
    $change = $changeMatch.Groups[1].Value
    $null = Lab-P4 'vm-a' @('edit','-c',$change,(Join-Path $rootA 'skill.rules'))
    Write-Rules $rootA $shelfText
    $null = Lab-P4 'vm-a' @('shelve','-c',$change)
    Write-Rules $rootA $continuedText
    $snapshots['after_shelf_and_continue'] = [ordered]@{ a=File-Text $rootA; shelf=Lab-P4 'vm-a' @('print','-q',"//depot/skill.rules@=$change"); depot=Lab-P4 'vm-a' @('print','-q','//depot/skill.rules'); change=Lab-P4 'vm-a' @('change','-o',$change); opened=Lab-P4 'vm-a' @('opened','-c',$change) }
    $s = $snapshots['after_shelf_and_continue']
    Check ($s.a -eq $continuedText) 'A disk contains continued edit'
    Check ($s.shelf -eq $shelfText) 'shelf retains content at shelve time'
    Check ($s.depot -eq $baselineText) 'shelving does not replace submitted depot content'
    Check ($s.change -match 'Status:\s+pending') 'shelved changelist remains pending'

    $null = Lab-P4 'vm-b' @('unshelve','-s',$change)
    $snapshots['b_unshelved'] = [ordered]@{ b=File-Text $rootB; depot=Lab-P4 'vm-b' @('print','-q','//depot/skill.rules'); opened=Lab-P4 'vm-b' @('opened') }
    Check ($snapshots['b_unshelved'].b -eq $shelfText) 'B receives shelf content rather than A latest disk edit'
    Check ($snapshots['b_unshelved'].opened -match 'edit') 'unshelve opens B file for edit'
    Check ($snapshots['b_unshelved'].depot -eq $baselineText) 'unshelving does not submit content'
    $null = Lab-P4 'vm-b' @('revert',(Join-Path $rootB 'skill.rules'))
    $null = Lab-P4 'vm-a' @('shelve','-d','-c',$change)
    $null = Lab-P4 'vm-a' @('submit','-c',$change)
    $snapshots['a_submitted_b_not_synced'] = [ordered]@{ depot=Lab-P4 'vm-a' @('print','-q','//depot/skill.rules'); b=File-Text $rootB; b_have=Lab-P4 'vm-b' @('have','//depot/skill.rules'); change=Lab-P4 'vm-a' @('change','-o',$change) }
    $s = $snapshots['a_submitted_b_not_synced']
    Check ($s.depot -eq $continuedText) 'ordinary submit records current opened file content, not deleted shelf content'
    Check ($s.change -match 'Status:\s+submitted') 'changelist status becomes submitted'
    Check ($s.b -eq $baselineText) 'A submit does not rewrite B workspace'
    $null = Lab-P4 'vm-b' @('sync','//depot/skill.rules')
    $snapshots['b_synced'] = [ordered]@{ b=File-Text $rootB; have=Lab-P4 'vm-b' @('have','//depot/skill.rules') }
    Check ($snapshots['b_synced'].b -eq $continuedText) 'B sync obtains submitted current content'
    Check ($snapshots['b_synced'].have -match '#2 ') 'B have list advances to file revision 2'

    (Get-Item -LiteralPath (Join-Path $rootB 'skill.rules')).IsReadOnly = $false
    Write-Rules $rootB "damage=70`ncharge=on"
    # A same-size rewrite within the sync timestamp second was missed in run4.
    # Give this teaching edit a distinct timestamp; do not rely on a timing race.
    $offlineFile = Get-Item -LiteralPath (Join-Path $rootB 'skill.rules')
    $offlineFile.LastWriteTimeUtc = $offlineFile.LastWriteTimeUtc.AddSeconds(2)
    $records.Add([ordered]@{ action='separate_fixture_mtime_from_sync'; path=Public-Text $offlineFile.FullName; seconds_added=2 })
    $before = Lab-P4 'vm-b' @('-ztag','fstat','-T','haveRev,headRev,action','//depot/skill.rules')
    Check ($before -notmatch '(?m)^\.\.\. action ') 'disk edit alone does not register an open action'
    $preview = Lab-P4 'vm-b' @('reconcile','-n','-e',(Join-Path $rootB 'skill.rules'))
    $afterPreview = Lab-P4 'vm-b' @('-ztag','fstat','-T','haveRev,headRev,action','//depot/skill.rules')
    Check ($preview -match 'edit' -and $afterPreview -notmatch '(?m)^\.\.\. action ') 'reconcile preview reports edit without opening file'
    $null = Lab-P4 'vm-b' @('reconcile','-e',(Join-Path $rootB 'skill.rules'))
    $snapshots['b_reconciled'] = [ordered]@{ b=File-Text $rootB; opened=Lab-P4 'vm-b' @('opened'); depot=Lab-P4 'vm-b' @('print','-q','//depot/skill.rules') }
    Check ($snapshots['b_reconciled'].opened -match 'edit') 'reconcile registers existing disk edit'
    Check ($snapshots['b_reconciled'].depot -eq $continuedText) 'reconcile does not submit the offline edit'
    $passed = $true
} catch { $failure=$_.Exception.Message; throw }
finally {
    if ($serverProcess) {
        try { if (-not $serverProcess.HasExited) { $null = Lab-P4 'vm-a' @('admin','stop'); $serverProcess.WaitForExit(5000) | Out-Null } } catch {}
        if (-not $serverProcess.HasExited) { Stop-Process -Id $serverProcess.Id -Force; $serverProcess.WaitForExit(5000) | Out-Null }
        $serverStopped = $serverProcess.HasExited
    }
    $receipt = [ordered]@{ passed=($passed -and $serverStopped); failure=$failure; recorded_at_utc=[DateTime]::UtcNow.ToString('o'); client_version=$clientVersion; server_version=$serverVersion; server_binary_sha256=(Get-FileHash -LiteralPath $serverBinary -Algorithm SHA256).Hash; endpoint=$endpoint; server_stopped=$serverStopped; changelist=$change; powershell_version=$PSVersionTable.PSVersion.ToString(); scope='One loopback-only fresh server, one synthetic user, two client views, text file only. No TLS, permissions policy, Code Review, locks, Streams, engine or external network validation.'; snapshots=$snapshots; passed_checks=@($checks.ToArray()) }
    $receiptText = ($receipt | ConvertTo-Json -Depth 12).Replace("`r`n","`n")
    $recordsText = (ConvertTo-Json -InputObject $records.ToArray() -Depth 8).Replace("`r`n","`n")
    foreach ($target in @(@('receipt.json',$receiptText),@('commands.json',$recordsText))) { [IO.File]::WriteAllText((Join-Path $evidencePath $target[0]), (Public-Text $target[1]).Replace($labPath.Replace('\','\\'),'[LAB]') + "`n", $utf8) }
    foreach ($name in $savedEnvironment.Keys) { [Environment]::SetEnvironmentVariable($name,$savedEnvironment[$name],'Process') }
}
if (-not $serverStopped) { throw 'Lab server cleanup failed.' }
Write-Output "PASS: $($checks.Count) checks; server stopped. Evidence: $evidencePath"
