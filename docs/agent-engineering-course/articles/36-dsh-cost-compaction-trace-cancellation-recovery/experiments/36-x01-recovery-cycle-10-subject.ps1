# Deliberate parser failure as data, never executed as a scriptblock.
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::WriteLine('SYNTHETIC_SUBJECT_START')
[Console]::WriteLine("child_pid=$PID")
[Console]::WriteLine("child_psversion=$($PSVersionTable.PSVersion)")
[Console]::WriteLine("child_utc=$([DateTime]::UtcNow.ToString('o'))")
[Console]::WriteLine("child_os=$([Environment]::OSVersion.VersionString)")
[Console]::WriteLine("child_cwd=$((Get-Location).Path)")
try {
    $unused = [scriptblock]::Create('Test-Path -LiteralPath (')
    [Console]::WriteLine('UNEXPECTED_PARSE_SUCCESS')
    exit 24
} catch {
    $detail = 'exception_type=' + $_.Exception.GetType().FullName + "`nerror_id=" + $_.FullyQualifiedErrorId + "`nexception_message=" + $_.Exception.Message
    if ($null -ne $_.Exception.InnerException) { $detail += "`ninner_exception_type=" + $_.Exception.InnerException.GetType().FullName }
    $truncated = $detail.Length -gt 800
    if ($truncated) { $detail = $detail.Substring(0, 800) }
    # At most 800 UTF-16 code units plus fixed flag: safely below 4 KiB UTF-8.
    [Console]::Error.WriteLine("diagnostic_truncated=$truncated")
    [Console]::Error.WriteLine($detail)
    [Console]::WriteLine('SYNTHETIC_PARSE_CAUGHT')
    exit 23
}
