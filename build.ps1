$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) {
    throw '未找到 .NET Framework C# 编译器。'
}
Push-Location -LiteralPath $PSScriptRoot
try {
    & .\make-icon.ps1
    $compilerArgs = @(
        '/nologo', '/target:winexe', '/platform:x64', '/out:VRWristLite.exe',
        '/win32icon:WristLite.ico',
        '/reference:System.Windows.Forms.dll', '/reference:System.Drawing.dll',
        '/reference:System.Numerics.dll', '/reference:System.Numerics.Vectors.dll',
        '/reference:System.Management.dll',
        '/resource:WristLite-icon.png,WristLite.icon.png'
    )
    Get-ChildItem -LiteralPath .\assets -Filter '*.png' -File | ForEach-Object {
        $compilerArgs += "/resource:$($_.FullName),WristLite.assets.$($_.Name)"
    }
    $compilerArgs += @('Program.cs', 'Hardware.cs', 'OpenVR.cs')
    & $compiler @compilerArgs
    if ($LASTEXITCODE -ne 0) { throw '编译失败。' }
    Write-Host '已生成 VRWristLite.exe'
} finally {
    Pop-Location
}
