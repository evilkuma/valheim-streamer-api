# build.ps1
param(
    [string]$ValheimPath   = "C:\Program Files (x86)\Steam\steamapps\common\Valheim dedicated server",
    [string]$OutputDir     = ".\Build",
    [string]$DeployServer  = "C:\Program Files (x86)\Steam\steamapps\common\Valheim dedicated server\BepInEx\plugins\valheim-streamer-api",
    [string]$DeployClient  = "C:\Program Files (x86)\Steam\steamapps\common\Valheim\BepInEx\plugins\valheim-streamer-api"
)

Write-Host "=== Building Valheim Streamer API ===" -ForegroundColor Green

$env:VALHEIM_INSTALL = $ValheimPath

if (!(Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

Push-Location .\src
dotnet clean
dotnet build -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    Pop-Location
    exit 1
}
Pop-Location

Write-Host "`n=== Build complete! ===" -ForegroundColor Green
Write-Host "Output: $OutputDir" -ForegroundColor Cyan

Write-Host "`nBuilt files:" -ForegroundColor Yellow
Get-ChildItem $OutputDir -Filter "*.dll" | ForEach-Object {
    $size = if ($_.Length -gt 1MB) { "{0:N2} MB" -f ($_.Length / 1MB) }
            elseif ($_.Length -gt 1KB) { "{0:N2} KB" -f ($_.Length / 1KB) }
            else { "{0} B" -f $_.Length }
    Write-Host "  - $($_.Name) ($size)" -ForegroundColor White
}

$dll     = "$OutputDir\ValheimStreamerApi.dll"
$jsonDll = "$OutputDir\Newtonsoft.Json.dll"

if (Test-Path $dll) {
    foreach ($dir in @($DeployServer, $DeployClient)) {
        if (!(Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
        Copy-Item $dll $dir -Force
        if (Test-Path $jsonDll) { Copy-Item $jsonDll $dir -Force }
        Write-Host "Deployed to: $dir" -ForegroundColor Green
    }
}
