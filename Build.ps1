$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$sources = Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter '*.cs' | ForEach-Object FullName
& $compiler /nologo /target:winexe /optimize+ /platform:x64 "/win32icon:$PSScriptRoot\assets\TavernLens.ico" /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll /r:System.Core.dll "/r:$PSScriptRoot\HearthMirror.dll" "/out:$PSScriptRoot\TavernLens.exe" $sources
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
Write-Output 'Built TavernLens.exe'
