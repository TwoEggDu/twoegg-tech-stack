param(
  [ValidateSet('Red','Green')][string]$Phase,
  [Parameter(Mandatory)][string]$OwnerPath,
  [string]$ReceiptRoot
)
$ErrorActionPreference='Stop'
$E=$PSScriptRoot
$Rec=Join-Path $E '36-x01-recovery-cycle-7-recorder.ps1'
$Run=Join-Path $E '36-x01-recovery-cycle-7-runner.ps1'
$Ver=Join-Path $E '36-x01-recovery-cycle-7-verify.ps1'
if($Phase -eq 'Red'){
  if((Test-Path -LiteralPath $Rec) -or (Test-Path -LiteralPath $Run) -or (Test-Path -LiteralPath $Ver)){[Console]::Error.WriteLine('RED_PRODUCTION_PRESENT');exit 2}
  [Console]::Error.WriteLine('RECORDER_CONTRACT_UNIMPLEMENTED');exit 1
}
function Sha([string]$p){$s=[IO.File]::OpenRead($p);try{$h=[Security.Cryptography.SHA256]::Create();try{([BitConverter]::ToString($h.ComputeHash($s))).Replace('-','')}finally{$h.Dispose()}}finally{$s.Dispose()}}
function B64([string]$s){[Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($s))}
function KvBytes([string]$type,[Collections.Specialized.OrderedDictionary]$d){$a=[Collections.Generic.List[string]]::new();$a.Add("H`t$type-V1");$i=0;foreach($k in $d.Keys){$i++;$a.Add("K`t$($i.ToString('0000'))`t$k`t$(B64 ([string]$d[$k]))")};[Text.Encoding]::UTF8.GetBytes(($a-join "`n")+"`n")}
function NewFile([string]$p,[byte[]]$b){$f=[IO.File]::Open($p,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::Read);try{$f.Write($b,0,$b.Length);$f.Flush($true)}finally{$f.Dispose()}}
function InvokeRecorded([string]$name,[string]$exe,[string[]]$argv,[int]$timeout,[string]$start,[string]$child,[string]$identity,[string]$startEx){
  $stage=Split-Path -Parent (Split-Path -Parent $ReceiptRoot);$owners=Join-Path $stage 'owners';$owner=Join-Path $owners "$name.owner";$nonce=[Guid]::NewGuid().ToString('N');$host=B64 "$([Environment]::MachineName)|$([Environment]::OSVersion.VersionString)|$([Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture)"
  $icd=[ordered]@{executable=$exe;script='NONE';cwd=$E;argv_json=([Text.Json.JsonSerializer]::Serialize([string[]]$argv));timeout=[string]$timeout;expected_start=$start;expected_child_exit=$child;expected_identity=$identity;expected_start_exception=$startEx};$ic=B64 ([Text.Encoding]::UTF8.GetString((KvBytes 'INVOCATION-CONTRACT' $icd)))
  $od=[ordered]@{role='REVISION_WORKER';nonce=$nonce;executable=$exe;script='NONE';cwd=$E;argv_json=$icd.argv_json;timeout=[string]$timeout;expected_start=$start;expected_child_exit=$child;expected_identity=$identity;expected_start_exception=$startEx;host_marker_b64=$host;invocation_contract_b64=$ic};NewFile $owner (KvBytes 'OWNER' $od);$os=Sha $owner
  & (Get-Command pwsh).Source -NoLogo -NoProfile -File $Rec -OwnerPath $owner -OwnerSha256 $os -OwnerNonce $nonce -ReceiptRoot (Join-Path $ReceiptRoot $name) -SubjectExecutable $exe -SubjectScript NONE -WorkingDirectory $E -TimeoutSeconds $timeout -ExpectedStart $start -ExpectedChildExitCode $child -ExpectedExecutableIdentity $identity -ExpectedStartException $startEx -HostMarkerBase64 $host -InvocationContractBase64 $ic
  if($LASTEXITCODE -ne 0){throw "$name recorder exit $LASTEXITCODE"}
}
if(!(Test-Path -LiteralPath $OwnerPath -PathType Leaf)){throw 'OWNER_MISSING'}
if(Test-Path -LiteralPath $ReceiptRoot){throw 'SUBJECT_ROOT_PREEXISTS'}
[IO.Directory]::CreateDirectory($ReceiptRoot)|Out-Null
$missing=Join-Path ([IO.Path]::GetTempPath()) ("a36-c7-"+[Guid]::NewGuid().ToString('N')+'\intentionally-absent-child.exe')
$ps=(Get-Command pwsh).Source
InvokeRecorded outer $missing @() 10 N NA EXPECTED_ABSENT System.ComponentModel.Win32Exception
InvokeRecorded inner $ps @('-NoLogo','-NoProfile','-Command','exit 23') 15 Y 23 (Sha $ps) NONE
exit 0
