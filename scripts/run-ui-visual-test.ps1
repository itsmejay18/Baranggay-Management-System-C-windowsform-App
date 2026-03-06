param(
    [switch]$NoBuild,
    [switch]$Fullscreen
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
Push-Location $projectRoot
try {
    if (-not $NoBuild) {
        dotnet build
    }

    $args = @("--ui-test")
    if ($Fullscreen) {
        $args += "--ui-test-fullscreen"
    }

    dotnet run --no-build -- $args
}
finally {
    Pop-Location
}
