param (
    [Parameter(Mandatory = $true)]
    [string]$LocalSourcesDir,
    [string]$BuildType = "Release",
    [string[]]$PackageProjects = @()
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$packageDir = Join-Path $repoRoot "output/Nuget/$BuildType"

& (Join-Path $PSScriptRoot "PackToLocal.ps1") -BuildType $BuildType -PackageProjects $PackageProjects

New-Item -Path $LocalSourcesDir -ItemType Directory -Force | Out-Null

$packages = Get-ChildItem -Path $packageDir -Filter "*.nupkg" -Recurse -File
if (-not $packages) {
    throw "No NuGet packages found in $packageDir."
}

foreach ($package in $packages) {
    dotnet nuget push $package.FullName --source $LocalSourcesDir --skip-duplicate
}

