$ErrorActionPreference = 'Stop'
Push-Location -LiteralPath $PSScriptRoot
try {
    & .\build.ps1
    & .\test.ps1
    New-Item -ItemType Directory -Path .\dist -Force | Out-Null
    $package = Join-Path $PSScriptRoot 'dist\VRWristLite-v1.0.0-win64.zip'
    Compress-Archive -Path @(
        '.\VRWristLite.exe', '.\README.md', '.\LICENSE',
        '.\THIRD_PARTY_NOTICES.md', '.\LICENSE-VRCX.txt',
        '.\LICENSE-OpenVR.txt', '.\docs'
    ) -DestinationPath $package -Force
    Write-Host "已生成 $package"
} finally {
    Pop-Location
}
