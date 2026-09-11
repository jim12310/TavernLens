# Run this only if Hearthstone does not produce Power.log. Restart the game afterwards.
$ErrorActionPreference = 'Stop'
$configDir = Join-Path $env:LOCALAPPDATA 'Blizzard\Hearthstone'
$configFile = Join-Path $configDir 'log.config'
New-Item -ItemType Directory -Force -Path $configDir | Out-Null
$existing = if (Test-Path -LiteralPath $configFile) { Get-Content -LiteralPath $configFile -Raw } else { '' }
if (Test-Path -LiteralPath $configFile) { Copy-Item -LiteralPath $configFile -Destination ($configFile + '.tavernlens-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.bak') }
$power = "[Power]`r`nLogLevel=1`r`nFilePrinting=true`r`nConsolePrinting=false`r`nScreenPrinting=false`r`nVerbose=true`r`n"
$pattern = '(?ms)^\[Power\]\s*\r?\n.*?(?=^\[|\z)'
if ([regex]::IsMatch($existing, $pattern)) { $updated = [regex]::Replace($existing, $pattern, $power) } else { $updated = $existing.TrimEnd() + "`r`n`r`n" + $power }
$loading = "[LoadingScreen]
LogLevel=1
FilePrinting=true
ConsolePrinting=false
ScreenPrinting=false
Verbose=true
"
$loadingPattern = '(?ms)^\[LoadingScreen\]\s*\r?\n.*?(?=^\[|\z)'
if ([regex]::IsMatch($updated, $loadingPattern)) { $updated = [regex]::Replace($updated, $loadingPattern, $loading) } else { $updated = $updated.TrimEnd() + "

" + $loading }
[IO.File]::WriteAllText($configFile, $updated)
Write-Output 'Power and scene logging enabled. Existing settings were backed up. Restart Hearthstone.'
