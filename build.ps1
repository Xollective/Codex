[CmdletBinding(PositionalBinding = $false)]
param(
    [Parameter(ValueFromRemainingArguments=$true)]
    [string[]]$RemainingArgs
)

& dotnet build "$PSScriptRoot/Codex.xln" /bl @RemainingArgs