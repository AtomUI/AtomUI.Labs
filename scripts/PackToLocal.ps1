param (
    [string]$BuildType = "Release",
    [string[]]$PackageProjects = @()
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$solutionPath = Join-Path $repoRoot "AtomUI.Labs.slnx"
$srcDir = Join-Path $repoRoot "src"
$testsDir = Join-Path $repoRoot "tests"

function Resolve-ProjectPath {
    param (
        [Parameter(Mandatory = $true)]
        [string]$ProjectPath
    )

    if ([System.IO.Path]::IsPathRooted($ProjectPath)) {
        return (Get-Item -Path $ProjectPath).FullName
    }

    return (Get-Item -Path (Join-Path $repoRoot $ProjectPath)).FullName
}

dotnet restore $solutionPath
dotnet build $solutionPath --configuration $BuildType --no-restore

if (Test-Path $testsDir) {
    $testProjects = Get-ChildItem -Path $testsDir -Filter "*.csproj" -Recurse -File
    foreach ($testProject in $testProjects) {
        dotnet test $testProject.FullName --framework net10.0 --configuration $BuildType --no-build
    }
}

if ($PackageProjects.Count -gt 0) {
    $packableProjects = foreach ($projectPath in $PackageProjects) {
        Resolve-ProjectPath -ProjectPath $projectPath
    }
} elseif (Test-Path $srcDir) {
    $packableProjects = Get-ChildItem -Path $srcDir -Filter "*.csproj" -Recurse -File |
        Where-Object {
            -not (Select-String -Path $_.FullName -Pattern "<IsPackable>\s*false\s*</IsPackable>" -Quiet)
        } |
        ForEach-Object { $_.FullName }
} else {
    $packableProjects = @()
}

if (-not $packableProjects -or $packableProjects.Count -eq 0) {
    Write-Host "No packable projects found under src/."
    return
}

foreach ($project in $packableProjects) {
    dotnet pack $project --configuration $BuildType --no-build
}

