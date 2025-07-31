[CmdletBinding(PositionalBinding = $false)]
param(
    [string]$CodexExeDir = "$PSScriptRoot/bin/exe",
    [Parameter(ValueFromRemainingArguments=$true)]
    [string[]]$RemainingArgs
)

& "$CodexExeDir/Codex.exe" @RemainingArgs