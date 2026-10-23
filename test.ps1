$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) {
    throw '未找到 .NET Framework C# 编译器。'
}
Push-Location -LiteralPath $PSScriptRoot
try {
    & $compiler /nologo /target:exe /platform:x64 /main:WristLite.TriggerTests `
        /out:TriggerTests.exe /reference:System.Windows.Forms.dll `
        /reference:System.Drawing.dll /reference:System.Numerics.dll `
        /reference:System.Numerics.Vectors.dll /reference:System.Management.dll `
        Program.cs Hardware.cs OpenVR.cs TriggerTests.cs
    if ($LASTEXITCODE -ne 0) { throw '测试编译失败。' }
    & .\TriggerTests.exe
    if ($LASTEXITCODE -ne 0) { throw '测试未通过。' }
} finally {
    Pop-Location
}
