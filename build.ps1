param([switch]$Test, [string]$Output = 'dist')
$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path -LiteralPath $compiler)) { throw '需要 Windows 自带的 .NET Framework 4.x 编译器。' }
$outputDir = Join-Path $projectRoot $Output
New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
$outputExe = Join-Path $outputDir '桌面便签.exe'
$sourceFiles = @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'src') -Filter '*.cs' | ForEach-Object { $_.FullName })
$compilerArgs = @('/nologo', '/target:winexe', '/platform:x64', '/optimize+', '/codepage:65001', "/out:$outputExe", "/win32manifest:$(Join-Path $projectRoot 'src\app.manifest')", '/reference:System.dll', '/reference:System.Core.dll', '/reference:System.Drawing.dll', '/reference:System.Windows.Forms.dll', '/reference:System.Runtime.Serialization.dll')
& $compiler @compilerArgs @sourceFiles
if ($LASTEXITCODE -ne 0) { throw '编译失败。若软件正在运行，请从托盘退出后重试。' }
$iconProcess = Start-Process -FilePath $outputExe -ArgumentList '--make-icon' -WindowStyle Hidden -PassThru -Wait
if ($iconProcess.ExitCode -ne 0) { throw '生成图标失败。' }
& $compiler @compilerArgs "/win32icon:$(Join-Path $outputDir 'app.ico')" @sourceFiles
if ($LASTEXITCODE -ne 0) { throw '编译失败。' }
Copy-Item -LiteralPath (Join-Path $projectRoot '使用说明.md') -Destination (Join-Path $outputDir '使用说明.md')
Write-Output "构建完成：$outputExe"
if ($Test) {
    $testProcess = Start-Process -FilePath $outputExe -ArgumentList '--self-test' -WindowStyle Hidden -PassThru -Wait
    Get-Content -LiteralPath (Join-Path $outputDir 'test-results\latest.txt')
    if ($testProcess.ExitCode -ne 0) { throw '验证失败，查看 test-results。' }
}
