[CmdletBinding()]
param(
  [string]$SourceRoot,
  [string]$DestinationRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$receipt = [ordered]@{
  schema_version = '36-x01-recovery-cycle-2-collector-v1'
  source_root = $SourceRoot
  destination_root = $DestinationRoot
  source_count = 0
  destination_count = 0
  files = @()
  validations = [ordered]@{
    inputs_present = $false
    source_root_exists = $false
    destination_root_exists = $false
    destination_initially_empty = $false
    source_nonempty = $false
    every_copy_present = $false
    paths_match = $false
    bytes_match = $false
    sha256_match = $false
    final_count_match = $false
  }
  status = 'FAIL'
  failure_code = $null
  failure_message = $null
}

function Complete-Collector([int]$ExitCode, [string]$FailureCode, [string]$FailureMessage) {
  $receipt.failure_code = $FailureCode
  $receipt.failure_message = $FailureMessage
  $receipt.status = if ($ExitCode -eq 0) { 'PASS' } else { 'FAIL' }
  [pscustomobject]$receipt | ConvertTo-Json -Depth 8 -Compress | Write-Output
  exit $ExitCode
}

function Get-RelativeFilePath([string]$Root, [string]$Path) {
  $rootWithSeparator = [System.IO.Path]::GetFullPath($Root).TrimEnd([char[]]@('\', '/')) + [System.IO.Path]::DirectorySeparatorChar
  $fullPath = [System.IO.Path]::GetFullPath($Path)
  if (-not $fullPath.StartsWith($rootWithSeparator, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Path is outside the specified root: $fullPath"
  }
  return $fullPath.Substring($rootWithSeparator.Length).Replace('\', '/')
}

try {
  if ([string]::IsNullOrWhiteSpace($SourceRoot) -or [string]::IsNullOrWhiteSpace($DestinationRoot)) {
    Complete-Collector 2 'INPUT_MISSING' 'SourceRoot and DestinationRoot must both be supplied.'
  }
  $receipt.validations.inputs_present = $true

  $sourceFull = [System.IO.Path]::GetFullPath($SourceRoot)
  $destinationFull = [System.IO.Path]::GetFullPath($DestinationRoot)
  $receipt.source_root = $sourceFull
  $receipt.destination_root = $destinationFull

  if (-not (Test-Path -LiteralPath $sourceFull -PathType Container)) {
    Complete-Collector 3 'SOURCE_ROOT_MISSING' 'The specified source root does not exist.'
  }
  $receipt.validations.source_root_exists = $true

  if (-not (Test-Path -LiteralPath $destinationFull -PathType Container)) {
    Complete-Collector 4 'DESTINATION_ROOT_MISSING' 'The specified destination root does not exist.'
  }
  $receipt.validations.destination_root_exists = $true

  $destinationInitialEntries = @(Get-ChildItem -LiteralPath $destinationFull -Force)
  if ($destinationInitialEntries.Count -ne 0) {
    Complete-Collector 5 'DESTINATION_NOT_EMPTY' 'The specified destination root is not empty.'
  }
  $receipt.validations.destination_initially_empty = $true

  $sourceFiles = @(Get-ChildItem -LiteralPath $sourceFull -File -Recurse -Force | Sort-Object FullName)
  $receipt.source_count = $sourceFiles.Count
  if ($sourceFiles.Count -eq 0) {
    Complete-Collector 6 'SOURCE_FILESET_EMPTY' 'The specified source root contains no files.'
  }
  $receipt.validations.source_nonempty = $true

  $records = [System.Collections.Generic.List[object]]::new()
  foreach ($sourceFile in $sourceFiles) {
    $relativePath = Get-RelativeFilePath $sourceFull $sourceFile.FullName
    if ($relativePath.StartsWith('../') -or $relativePath -eq '..') {
      Complete-Collector 7 'RELATIVE_PATH_OUTSIDE_SOURCE' "A source file resolved outside the source root: $($sourceFile.FullName)"
    }
    $destinationFile = Join-Path $destinationFull ($relativePath.Replace('/', [System.IO.Path]::DirectorySeparatorChar))
    $destinationParent = [System.IO.Path]::GetDirectoryName($destinationFile)
    if ([string]::IsNullOrWhiteSpace($destinationParent)) {
      Complete-Collector 7 'DESTINATION_PARENT_MISSING' "The destination parent could not be resolved: $destinationFile"
    }
    [System.IO.Directory]::CreateDirectory($destinationParent) | Out-Null
    Copy-Item -LiteralPath $sourceFile.FullName -Destination $destinationFile -Force
    $destinationExists = Test-Path -LiteralPath $destinationFile -PathType Leaf
    $sourceBytes = [System.IO.FileInfo]$sourceFile
    $destinationBytes = if ($destinationExists) { ([System.IO.FileInfo](Get-Item -LiteralPath $destinationFile)).Length } else { $null }
    $sourceHash = (Get-FileHash -LiteralPath $sourceFile.FullName -Algorithm SHA256).Hash
    $destinationHash = if ($destinationExists) { (Get-FileHash -LiteralPath $destinationFile -Algorithm SHA256).Hash } else { $null }
    $records.Add([ordered]@{
      relative_path = $relativePath
      source_byte_count = $sourceBytes.Length
      destination_byte_count = $destinationBytes
      source_sha256 = $sourceHash
      destination_sha256 = $destinationHash
      destination_present = $destinationExists
      path_match = $destinationExists
      bytes_match = ($destinationExists -and $sourceBytes.Length -eq $destinationBytes)
      sha256_match = ($destinationExists -and $sourceHash -eq $destinationHash)
    })
  }

  $destinationFiles = @(Get-ChildItem -LiteralPath $destinationFull -File -Recurse -Force | Sort-Object FullName)
  $receipt.destination_count = $destinationFiles.Count
  $receipt.files = @($records)
  $sourcePaths = @($records | ForEach-Object { $_.relative_path } | Sort-Object)
  $destinationPaths = @($destinationFiles | ForEach-Object { Get-RelativeFilePath $destinationFull $_.FullName } | Sort-Object)
  $receipt.validations.every_copy_present = @($records | Where-Object { -not $_.destination_present }).Count -eq 0
  $receipt.validations.paths_match = (($sourcePaths.Count -eq $destinationPaths.Count) -and -not (Compare-Object -ReferenceObject $sourcePaths -DifferenceObject $destinationPaths))
  $receipt.validations.bytes_match = @($records | Where-Object { -not $_.bytes_match }).Count -eq 0
  $receipt.validations.sha256_match = @($records | Where-Object { -not $_.sha256_match }).Count -eq 0
  $receipt.validations.final_count_match = $receipt.source_count -eq $receipt.destination_count

  $allPassed = @($receipt.validations.Values | Where-Object { -not $_ }).Count -eq 0
  if (-not $allPassed) {
    Complete-Collector 8 'POST_TRANSFER_MISMATCH' 'At least one required post-transfer validation failed.'
  }
  Complete-Collector 0 $null $null
} catch {
  Complete-Collector 9 'UNHANDLED_COLLECTOR_ERROR' $_.Exception.Message
}
