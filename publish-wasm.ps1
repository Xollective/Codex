[CmdletBinding(PositionalBinding = $false)]
param(
    [Parameter(ValueFromRemainingArguments=$true)]
    [string[]]$RemainingArgs
)

$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $true

# Prebuild Codex.Web.Wasm using current SDK (9+) since we rely on later compiler features
& dotnet build "$PSScriptRoot/src/Codex.Web.Wasm/Codex.Web.Wasm.csproj" -c "Release" /bl 

# Switch to Codex.Web.Wasm directory in order to build Codex.Web.Wasm project using .net 8 since this is required for wasm multithreading
pushd "$PSScriptRoot/src/Codex.Web.Wasm" 

# NOTE: We disable building project references here to since we need to build dependencies with (9+ SDK) 
& dotnet publish /p:BuildProjectReferences=false @RemainingArgs