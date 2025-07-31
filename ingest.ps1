[CmdletBinding(PositionalBinding = $false)]
param(
    [string]$CodexExeDir = "$PSScriptRoot\bin\exe",
    [string]$AnalysisDir = "$PSScriptRoot\bin\cdx\analyze",
    [string]$OutputDir = "$PSScriptRoot\bin\cdx\index",
    [Parameter(ValueFromRemainingArguments=$true)]
    [string[]]$RemainingArgs
)

& "$CodexExeDir\Codex.exe" ingest --in "$AnalysisDir" --out "$OutputDir" @RemainingArgs