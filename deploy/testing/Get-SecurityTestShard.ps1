param(
    [Parameter(Mandatory)][string]$DiscoveryPath,
    [Parameter(Mandatory)][ValidateRange(0,63)][int]$ShardIndex,
    [ValidateRange(1,64)][int]$ShardCount = 4
)
$ErrorActionPreference = 'Stop'
if ($ShardIndex -ge $ShardCount) { throw 'Shard index must be smaller than shard count.' }
$lines = Get-Content -LiteralPath $DiscoveryPath
if (-not ($lines -contains 'The following Tests are available:')) { throw 'Test discovery header missing.' }
$header = [Array]::IndexOf($lines, 'The following Tests are available:')
$cases = @($lines | Select-Object -Skip ($header + 1) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
if ($cases.Count -eq 0) { throw 'No security test cases discovered.' }
$selected = [Collections.Generic.SortedSet[string]]::new([StringComparer]::Ordinal)
$selectedCases = 0
foreach ($case in $cases) {
    if ($case -notmatch '^\s+(EnvanterTR\.SecurityAcceptance\.[A-Za-z0-9_.+]+)(?:\(.*\))?\s*$') {
        throw "Unrecognized discovered test: $case"
    }
    $method = $Matches[1]
    $hash = [Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($method))
    $bucket = [Convert]::ToUInt32([Convert]::ToHexString($hash).Substring(0,8),16) % $ShardCount
    if ($bucket -eq $ShardIndex) { [void]$selected.Add($method); $selectedCases++ }
}
if ($selected.Count -eq 0) { throw 'Empty shard; refusing an empty test run.' }
[pscustomobject]@{
    Index = $ShardIndex; Count = $ShardCount; TotalCases = $cases.Count
    SelectedCases = $selectedCases; Methods = @($selected)
    Filter = (@($selected | ForEach-Object { 'FullyQualifiedName=' + $_ }) -join '|')
}
