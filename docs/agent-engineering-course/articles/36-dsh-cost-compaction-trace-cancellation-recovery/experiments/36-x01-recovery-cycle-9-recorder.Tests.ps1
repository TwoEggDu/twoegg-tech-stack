param(
  [Parameter(Mandatory)][ValidateSet('Bootstrap','Subject')][string]$Entry,
  [Parameter(Mandatory)][ValidateSet('Probe','Red','Green')][string]$Case,
  [Parameter(Mandatory)][string]$MarkerPath,
  [Parameter(Mandatory)][string]$ReceiptRoot,
  [Parameter(Mandatory)][string]$PwshPath,
  [Parameter(Mandatory)][ValidatePattern('^[0-9a-f]{32}$')][string]$AttemptNonce,
  [Parameter(Mandatory)][string]$OwnerExecutionId,
  [Parameter(Mandatory)][ValidatePattern('^[0-9A-F]{64}$')][string]$ExpectedTestsSha256
)
$ErrorActionPreference='Stop'
if($Entry -eq 'Bootstrap'){
  $tests=$PSCommandPath
  if(![IO.Path]::IsPathFullyQualified($MarkerPath)-or![IO.Path]::IsPathFullyQualified($ReceiptRoot)-or![IO.Path]::IsPathFullyQualified($PwshPath)-or[string]::IsNullOrWhiteSpace($OwnerExecutionId)){exit 90}
  $timeout=if($Case -eq 'Probe'){10}elseif($Case -eq 'Red'){30}else{80}
  [string[]]$argv=@('-NoLogo','-NoProfile','-File',$tests,'-Entry','Subject','-Case',$Case,'-MarkerPath',$MarkerPath,'-ReceiptRoot',$ReceiptRoot,'-PwshPath',$PwshPath,'-AttemptNonce',$AttemptNonce,'-OwnerExecutionId',$OwnerExecutionId,'-ExpectedTestsSha256',$ExpectedTestsSha256)
  $pairs=@(
    @('creator','Tests/Bootstrap'),@('owner_role','REVISION_WORKER'),@('owner_execution_id',$OwnerExecutionId),@('attempt_nonce',$AttemptNonce),
    @('marker_path',$MarkerPath),@('receipt_root',$ReceiptRoot),@('tests_path',$tests),@('expected_tests_sha256',$ExpectedTestsSha256),
    @('subject_executable',$PwshPath),@('working_directory',$PWD.Path),@('timeout_seconds',[string]$timeout),@('case',$Case),@('argv_count',[string]$argv.Count)
  )
  $marker="H`tC9-BOOTSTRAP-PREPARING-V1`n";$i=0
  foreach($pair in $pairs){$i++;[byte[]]$vb=[Text.Encoding]::UTF8.GetBytes([string]$pair[1]);$marker+="K`t$($i.ToString('0000'))`t$($pair[0])`t$([Convert]::ToBase64String($vb))`n"}
  $i=0;foreach($token in $argv){$i++;[byte[]]$vb=[Text.Encoding]::UTF8.GetBytes($token);$marker+="A`t$($i.ToString('0000'))`t$([Convert]::ToBase64String($vb))`n"}
  [byte[]]$mb=[Text.Encoding]::UTF8.GetBytes($marker)
  $mf=[IO.File]::Open($MarkerPath,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::Read)
  try{$mf.Write($mb,0,$mb.Length);$mf.Flush($true)}finally{$mf.Dispose()}
  function Hash([string]$p){(Get-FileHash -Algorithm SHA256 -LiteralPath $p).Hash}
  function B64([string]$s){[Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($s))}
  function NewF([string]$p,[string]$s){[byte[]]$b=[Text.Encoding]::UTF8.GetBytes($s);$f=[IO.File]::Open($p,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::Read);try{$f.Write($b,0,$b.Length);$f.Flush($true)}finally{$f.Dispose()}}
  function KV([string]$type,$d){$s="H`t$type-V1`n";$n=0;foreach($k in $d.Keys){$n++;$s+="K`t$($n.ToString('0000'))`t$k`t$(B64 ([string]$d[$k]))`n"};$s}
  $proc=$null;$of=$null;$ef=$null
  try{
    [IO.Directory]::CreateDirectory($ReceiptRoot)|Out-Null
    $ti=Get-Item -LiteralPath $tests;$pi=Get-Item -LiteralPath $PwshPath
    if((Hash $tests)-cne$ExpectedTestsSha256){throw 'TESTS_IDENTITY_MISMATCH'}
    NewF (Join-Path $ReceiptRoot 'identity.kv') (KV 'C9-BOOTSTRAP-IDENTITY' ([ordered]@{owner_role='REVISION_WORKER';owner_execution_id=$OwnerExecutionId;attempt_nonce=$AttemptNonce;tests_path=$tests;tests_bytes=[string]$ti.Length;tests_sha256=Hash $tests;pwsh_path=$PwshPath;pwsh_bytes=[string]$pi.Length;pwsh_sha256=Hash $PwshPath;host="$([Environment]::MachineName)|$($PSVersionTable.PSVersion)|$([Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture)"}))
    $op=Join-Path $ReceiptRoot 'stdout.bin';$ep=Join-Path $ReceiptRoot 'stderr.bin'
    $of=[IO.File]::Open($op,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::Read);$ef=[IO.File]::Open($ep,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::Read)
    $si=[Diagnostics.ProcessStartInfo]::new($PwshPath);$si.UseShellExecute=$false;$si.RedirectStandardOutput=$true;$si.RedirectStandardError=$true;$si.WorkingDirectory=$PWD.Path
    foreach($token in $argv){[void]$si.ArgumentList.Add([string]$token)}
    NewF (Join-Path $ReceiptRoot 'ready.kv') (KV 'C9-BOOTSTRAP-READY' ([ordered]@{attempt_nonce=$AttemptNonce;identity_checked='Y';start_imminent='Y'}))
    $proc=[Diagnostics.Process]::new();$proc.StartInfo=$si;$attempted=$true;$started=$proc.Start()
    if(!$started){throw 'PROCESS_START_FALSE'}
    $ot=$proc.StandardOutput.BaseStream.CopyToAsync($of);$et=$proc.StandardError.BaseStream.CopyToAsync($ef)
    $natural=$proc.WaitForExit($timeout*1000);$kill='N';$killok='N'
    if(!$natural){$kill='Y';$proc.Kill($true);$killok=[string]$(if($proc.WaitForExit(5000)){'Y'}else{'N'});if($killok-ne'Y'){throw 'PROCESS_STILL_RUNNING'}}
    $terminal=[Diagnostics.Stopwatch]::GetTimestamp();[System.Threading.Tasks.Task[]]$tasks=@($ot,$et);if(![System.Threading.Tasks.Task]::WaitAll($tasks,5000)){throw 'PUMP_TIMEOUT'}
    $of.Flush($true);$ef.Flush($true);$of.Dispose();$ef.Dispose();$of=$null;$ef=$null
    $exit=$proc.ExitCode;$proc.Dispose();$proc=$null
    NewF (Join-Path $ReceiptRoot 'exception.kv') (KV 'C9-BOOTSTRAP-EXCEPTION' ([ordered]@{type='NONE';message='NONE'}))
    NewF (Join-Path $ReceiptRoot 'result.kv') (KV 'C9-BOOTSTRAP-RESULT' ([ordered]@{owner_role='REVISION_WORKER';owner_execution_id=$OwnerExecutionId;attempt_nonce=$AttemptNonce;case=$Case;marker_sha256=Hash $MarkerPath;expected_tests_sha256=$ExpectedTestsSha256;actual_tests_sha256=Hash $tests;actual_pwsh_path=$PwshPath;actual_pwsh_bytes=[string]$pi.Length;actual_pwsh_sha256=Hash $PwshPath;start_attempted=[string]$attempted;start_actual=[string]$started;child_exit=[string]$exit;timeout_seconds=[string]$timeout;kill_attempted=$kill;kill_succeeded=$killok;still_running='N';stdout_pump_complete='Y';stderr_pump_complete='Y';stdout_flush='Y';stderr_flush='Y';stdout_closed='Y';stderr_closed='Y';process_closed='Y';terminal_tick=[string]$terminal;pump_grace_seconds='5';exception='NONE';bootstrap_exit='0'}))
    $inv="H`tC9-BOOTSTRAP-INVENTORY-V1`n";$n=0;foreach($x in @(Get-ChildItem -LiteralPath $ReceiptRoot -File -Recurse|Where-Object Name -ne 'inventory.tsv'|Sort-Object FullName)){$n++;$rel=$x.FullName.Substring($ReceiptRoot.Length+1).Replace('\','/');$inv+="F`t$($n.ToString('0000'))`t$(B64 $rel)`tFILE`t$($x.Length)`t$(Hash $x.FullName)`n"};NewF (Join-Path $ReceiptRoot 'inventory.tsv') $inv
    exit 0
  }catch{
    if($of){try{$of.Dispose()}catch{}};if($ef){try{$ef.Dispose()}catch{}};if($proc){try{if(!$proc.HasExited){$proc.Kill($true);[void]$proc.WaitForExit(5000)}}catch{};try{$proc.Dispose()}catch{}}
    try{if(Test-Path -LiteralPath $ReceiptRoot){NewF (Join-Path $ReceiptRoot 'emergency.kv') (KV 'C9-BOOTSTRAP-EMERGENCY' ([ordered]@{owner_execution_id=$OwnerExecutionId;attempt_nonce=$AttemptNonce;exception_type=$_.Exception.GetType().FullName;exception_message=$_.Exception.Message;bootstrap_exit='90'}))}}catch{}
    exit 90
  }
}
function Hash([string]$p){(Get-FileHash -Algorithm SHA256 -LiteralPath $p).Hash}
function B64([string]$s){[Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($s))}
function NewF([string]$p,[string]$s){[byte[]]$b=[Text.Encoding]::UTF8.GetBytes($s);$f=[IO.File]::Open($p,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::Read);try{$f.Write($b,0,$b.Length);$f.Flush($true)}finally{$f.Dispose()}}
function KV([string]$type,$d,[string[]]$a=@()){$s="H`t$type-V1`n";$n=0;foreach($k in $d.Keys){$n++;$s+="K`t$($n.ToString('0000'))`t$k`t$(B64 ([string]$d[$k]))`n"};$n=0;foreach($v in $a){$n++;$s+="A`t$($n.ToString('0000'))`t$(B64 $v)`n"};$s}
if($Case -eq 'Probe'){[Console]::Out.WriteLine('C9_BOOTSTRAP_PROBE_STDOUT');[Console]::Error.WriteLine('C9_BOOTSTRAP_PROBE_STDERR');exit 23}
$rec=Join-Path $PSScriptRoot '36-x01-recovery-cycle-9-recorder.ps1';$run=Join-Path $PSScriptRoot '36-x01-recovery-cycle-9-runner.ps1';$ver=Join-Path $PSScriptRoot '36-x01-recovery-cycle-9-verify.ps1'
if($Case -eq 'Red'){if((Test-Path -LiteralPath $rec)-or(Test-Path -LiteralPath $run)-or(Test-Path -LiteralPath $ver)){[Console]::Error.WriteLine('RED_PRODUCTION_PRESENT');exit 2};[Console]::Error.WriteLine('RECORDER_CONTRACT_UNIMPLEMENTED');exit 1}
if((Hash $PSCommandPath)-cne$ExpectedTestsSha256){throw 'TESTS_DRIFT'}
foreach($p in @($rec,$run,$ver)){if(!(Test-Path -LiteralPath $p -PathType Leaf)){throw 'PRODUCTION_MISSING'}}
if((Get-Item $PSCommandPath).Length+(Get-Item $rec).Length+(Get-Item $run).Length+(Get-Item $ver).Length-gt24576){throw 'SOURCE_CAP'}
$exec=Join-Path $ReceiptRoot 'execution';$owners=Join-Path $exec 'owners';[IO.Directory]::CreateDirectory($owners)|Out-Null
function InvokeRec([string]$name,[string]$exe,[string[]]$av,[int]$to,[int]$gr,[string]$start,[string]$ce,[string]$identity,[string]$sx,[string]$script='NONE'){
  $nonce=[Guid]::NewGuid().ToString('N');$owner=Join-Path $owners "$name.owner";$mark=Join-Path $exec "$name.bootstrap.kv";$root=Join-Path $exec "$name-process"
  $bytes=if($identity-eq'EXPECTED_ABSENT'){'NA'}else{[string](Get-Item $exe).Length};$sha=if($identity-eq'EXPECTED_ABSENT'){'EXPECTED_ABSENT'}else{$identity}
  NewF $owner (KV 'C9-RECORDER-OWNER' ([ordered]@{role='REVISION_WORKER';owner_execution_id=$OwnerExecutionId;nonce=$nonce;executable=$exe;script=$script;cwd=$PSScriptRoot;timeout_seconds=[string]$to;expected_start=$start;expected_child_exit=$ce;expected_executable_bytes=$bytes;expected_executable_sha256=$sha;expected_start_exception=$sx;host_marker="$([Environment]::MachineName)|$([Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture)";argv_count=[string]$av.Count}) $av)
  & $PwshPath -NoLogo -NoProfile -File $rec -OwnerPath $owner -OwnerSha256 (Hash $owner) -OwnerNonce $nonce -MarkerPath $mark -ReceiptRoot $root -PumpGraceSeconds $gr
  if($LASTEXITCODE-ne0){throw "$name RECORDER_FAIL"}
}
$missing=Join-Path ([IO.Path]::GetTempPath()) ('a36-c9-absent-'+[Guid]::NewGuid().ToString('N')+'.exe')
InvokeRec absent $missing @() 10 5 N NA EXPECTED_ABSENT System.ComponentModel.Win32Exception
InvokeRec exit23 $PwshPath @('-NoLogo','-NoProfile','-Command','[Console]::Out.WriteLine(''C9_RECORDER_STDOUT'');[Console]::Error.WriteLine(''C9_RECORDER_STDERR'');exit 23') 15 5 Y 23 (Hash $PwshPath) NONE
$target=$exec;$vowners=Join-Path $ReceiptRoot 'owners';[IO.Directory]::CreateDirectory($vowners)|Out-Null;$script:owners=$vowners;$script:exec=$ReceiptRoot
$assess=Join-Path $ReceiptRoot 'assessment';$vargs=@('-NoLogo','-NoProfile','-File',$ver,'-Mode','Green','-OwnerPath',(Join-Path $vowners 'verifier.owner'),'-TargetRoot',$target,'-AssessmentRoot',$assess)
InvokeRec verifier $PwshPath $vargs 30 5 Y 0 (Hash $PwshPath) NONE $ver
exit 0
