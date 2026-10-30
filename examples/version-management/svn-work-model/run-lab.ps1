[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$LabRoot,
    [Parameter(Mandatory = $true)][string]$EvidenceDirectory
)
$ErrorActionPreference = 'Stop'
$labPath = [IO.Path]::GetFullPath($LabRoot)
$evidencePath = [IO.Path]::GetFullPath($EvidenceDirectory)
if ((Test-Path -LiteralPath $labPath) -or (Test-Path -LiteralPath $evidencePath)) { throw 'Output directories must be new.' }
$null = New-Item -ItemType Directory -Path $labPath
$null = New-Item -ItemType Directory -Path $evidencePath
$configPath = Join-Path $labPath 'client-config'
$seedPath = Join-Path $labPath 'seed'
$null = New-Item -ItemType Directory -Path $configPath
$null = New-Item -ItemType Directory -Path $seedPath
$utf8 = New-Object System.Text.UTF8Encoding($false)
$records = New-Object 'System.Collections.Generic.List[object]'
$checks = New-Object 'System.Collections.Generic.List[string]'
$snapshots = [ordered]@{}
$passed = $false
$failure = $null

function Public-Text([string]$Value) { return $Value.Replace($labPath, '[LAB]').Replace($labPath.Replace('\','/'), '[LAB]') }
function Tool([string]$Name, [string[]]$Arguments) {
    $actual = @($Arguments)
    if ($Name -eq 'svn') { $actual += @('--config-dir', $configPath, '--non-interactive', '--no-auth-cache', '--username', 'vm-lab') }
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $lines = @(& $Name @actual 2>&1 | ForEach-Object { $_.ToString() })
        $toolExit = $LASTEXITCODE
    } finally { $ErrorActionPreference = $previous }
    $output = $lines -join "`n"
    $records.Add([ordered]@{ tool=$Name; arguments=@($actual | ForEach-Object { Public-Text $_ }); exit_code=$toolExit; output=Public-Text $output })
    if ($toolExit -ne 0) { throw "$Name failed ($toolExit): $($Arguments -join ' ')" }
    return $output
}
function Check([bool]$Condition, [string]$Label) { if (-not $Condition) { throw "Assertion failed: $Label" }; $checks.Add($Label) }
function Write-Rule([string]$Path, [string]$Content) {
    [IO.File]::WriteAllText($Path, $Content + "`n", $utf8)
    $records.Add([ordered]@{ action='write_utf8_no_bom_lf'; path=Public-Text $Path; content=$Content + "`n" })
}
function Snapshot([string]$Directory) {
    [xml]$info = Tool 'svn' @('info', '--xml', '-R', $Directory)
    $entries = @($info.info.entry)
    $root = @($entries | Where-Object { $_.kind -eq 'dir' })[0]
    $damage = @($entries | Where-Object { $_.path.Replace('\','/').EndsWith('/damage.rules') })[0]
    $charge = @($entries | Where-Object { $_.path.Replace('\','/').EndsWith('/charge.rules') })[0]
    return [ordered]@{
        repository_youngest = [int](Tool 'svnlook' @('youngest', $repository))
        root_revision = [int]$root.revision
        damage_revision = [int]$damage.revision
        damage_last_changed = [int]$damage.commit.revision
        charge_revision = [int]$charge.revision
        charge_last_changed = [int]$charge.commit.revision
        svnversion = Tool 'svnversion' @('-n', $Directory)
        status = Public-Text (Tool 'svn' @('status', $Directory))
        status_verbose = Public-Text (Tool 'svn' @('status', '-v', $Directory))
        damage = [IO.File]::ReadAllText((Join-Path $Directory 'damage.rules')).TrimEnd("`r","`n")
        charge = [IO.File]::ReadAllText((Join-Path $Directory 'charge.rules')).TrimEnd("`r","`n")
    }
}

$repository = Join-Path $labPath 'repository'
$url = ([Uri]($repository.Replace('\','/') + '/')).AbsoluteUri.TrimEnd('/')
$wcA = Join-Path $labPath 'wc-a'
$wcB = Join-Path $labPath 'wc-b'
$wcC = Join-Path $labPath 'wc-r3'
try {
    $version = Tool 'svn' @('--version', '--quiet')
    $adminVersion = Tool 'svnadmin' @('--version', '--quiet')
    $null = Tool 'svnadmin' @('create', $repository)
    Write-Rule (Join-Path $seedPath 'damage.rules') 'damage=100'
    Write-Rule (Join-Path $seedPath 'charge.rules') 'charge=off'
    $null = Tool 'svn' @('import', $seedPath, $url, '-m', 'Baseline')
    $null = Tool 'svn' @('checkout', '-r', '1', $url, $wcA)
    $null = Tool 'svn' @('checkout', '-r', '1', $url, $wcB)
    $snapshots['baseline_a'] = Snapshot $wcA
    Check ($snapshots['baseline_a'].svnversion -eq '1') 'A starts at one clean revision'
    Check ($snapshots['baseline_a'].status -eq '') 'A starts without local modifications'

    Write-Rule (Join-Path $wcA 'damage.rules') 'damage=80'
    $null = Tool 'svn' @('commit', (Join-Path $wcA 'damage.rules'), '-m', 'Reduce damage')
    $snapshots['a_after_commit_r2'] = Snapshot $wcA
    $s = $snapshots['a_after_commit_r2']
    Check ($s.repository_youngest -eq 2) 'A commit creates repository revision 2'
    Check ($s.root_revision -eq 1 -and $s.damage_revision -eq 2 -and $s.charge_revision -eq 1) 'A commit leaves mixed path revisions'
    Check ($s.svnversion -eq '1:2') 'svnversion reports mixed revision range after commit'
    Check ($s.status -eq '') 'mixed revisions do not require local modifications'

    Write-Rule (Join-Path $wcB 'charge.rules') 'charge=on'
    $null = Tool 'svn' @('commit', (Join-Path $wcB 'charge.rules'), '-m', 'Enable charge')
    $snapshots['a_after_b_commit_r3'] = Snapshot $wcA
    $s = $snapshots['a_after_b_commit_r3']
    Check ($s.repository_youngest -eq 3) 'B independent file commit creates revision 3'
    Check ($s.charge -eq 'charge=off' -and $s.charge_revision -eq 1) 'B commit does not update A files or working revisions'

    $null = Tool 'svn' @('update', (Join-Path $wcA 'charge.rules'))
    $snapshots['a_after_file_update'] = Snapshot $wcA
    $s = $snapshots['a_after_file_update']
    Check ($s.root_revision -eq 1 -and $s.damage_revision -eq 2 -and $s.charge_revision -eq 3) 'single file update changes only selected path revision'
    Check ($s.svnversion -eq '1:3') 'root and files produce mixed revision range after partial update'
    Check ($s.charge -eq 'charge=on') 'selected file update acquires B content'

    $null = Tool 'svn' @('update', $wcA)
    $snapshots['a_after_full_update'] = Snapshot $wcA
    $s = $snapshots['a_after_full_update']
    Check ($s.root_revision -eq 3 -and $s.damage_revision -eq 3 -and $s.charge_revision -eq 3) 'full update aligns working revisions at 3'
    Check ($s.damage_last_changed -eq 2 -and $s.charge_last_changed -eq 3) 'last changed revisions remain distinct from working revisions'
    Check ($s.damage -eq 'damage=80') 'working revision can advance without content changes'
    Check ($s.svnversion -eq '3' -and $s.status -eq '') 'full update produces one clean revision'

    $null = Tool 'svn' @('checkout', '-r', '3', $url, $wcC)
    $snapshots['clean_checkout_r3'] = Snapshot $wcC
    Check ($snapshots['clean_checkout_r3'].damage -eq $s.damage -and $snapshots['clean_checkout_r3'].charge -eq $s.charge) 'explicit clean checkout has matching file contents'
    Write-Rule (Join-Path $wcA 'damage.rules') 'damage=70'
    $snapshots['a_with_local_edit'] = Snapshot $wcA
    $remoteDamage = Tool 'svn' @('cat', '-r', '3', ($url + '/damage.rules'))
    Check ($snapshots['a_with_local_edit'].svnversion -eq '3M') 'local edit is represented separately from base revision'
    Check ($snapshots['a_with_local_edit'].damage -eq 'damage=70' -and $remoteDamage -eq 'damage=80') 'local edit does not change repository content'
    Check ($snapshots['a_with_local_edit'].repository_youngest -eq 3) 'local edit does not create a new repository revision'
    $passed = $true
} catch { $failure = $_.Exception.Message; throw }
finally {
    $receipt = [ordered]@{ passed=$passed; failure=$failure; recorded_at_utc=[DateTime]::UtcNow.ToString('o'); svn_version=$version; svnadmin_version=$adminVersion; powershell_version=$PSVersionTable.PSVersion.ToString(); scope='Local file:// repository, two author work copies and one clean checkout, text only. No server, network, conflicts or engine validation.'; snapshots=$snapshots; passed_checks=@($checks.ToArray()) }
    [IO.File]::WriteAllText((Join-Path $evidencePath 'receipt.json'), ($receipt | ConvertTo-Json -Depth 12).Replace("`r`n","`n") + "`n", $utf8)
    [IO.File]::WriteAllText((Join-Path $evidencePath 'commands.json'), (ConvertTo-Json -InputObject $records.ToArray() -Depth 8).Replace("`r`n","`n") + "`n", $utf8)
}
Write-Output "PASS: $($checks.Count) checks. Evidence: $evidencePath"
