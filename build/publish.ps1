# Publishes a self-contained win-x64 build a teacher can run without installing .NET (plan §2 / §Sprint 12).
# Usage:  pwsh build/publish.ps1 [-Configuration Release] [-Output publish\win-x64]
param(
    [string]$Configuration = "Release",
    [string]$Output = "publish/win-x64"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

dotnet publish "$root/src/ZeugnisApp.App/ZeugnisApp.App.csproj" `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=none `
    -o "$root/$Output"

Write-Host "Published to $root/$Output" -ForegroundColor Green
Write-Host "Next: run Inno Setup on build/ZeugnisApp.iss, then Authenticode-sign the resulting installer."
