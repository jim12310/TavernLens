param([string]$ImagePath,[switch]$Worker)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Runtime.WindowsRuntime
$null=[Windows.Storage.StorageFile,Windows.Storage,ContentType=WindowsRuntime]
$null=[Windows.Graphics.Imaging.BitmapDecoder,Windows.Graphics.Imaging,ContentType=WindowsRuntime]
$null=[Windows.Globalization.Language,Windows.Globalization,ContentType=WindowsRuntime]
$null=[Windows.Media.Ocr.OcrEngine,Windows.Foundation,ContentType=WindowsRuntime]
$awaitMethod=[System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object { $_.Name -eq 'AsTask' -and $_.IsGenericMethod -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1' } | Select-Object -First 1
function Await-Result($operation,[Type]$type) { $task=$awaitMethod.MakeGenericMethod($type).Invoke($null,@($operation)); $task.Wait(); return $task.Result }
function Read-RatingImage([string]$ImagePath) {
try {
 $ImagePath=(Resolve-Path -LiteralPath $ImagePath).Path
 $file=Await-Result ([Windows.Storage.StorageFile]::GetFileFromPathAsync($ImagePath)) ([Windows.Storage.StorageFile])
 $stream=Await-Result ($file.OpenAsync([Windows.Storage.FileAccessMode]::Read)) ([Windows.Storage.Streams.IRandomAccessStream])
 $decoder=Await-Result ([Windows.Graphics.Imaging.BitmapDecoder]::CreateAsync($stream)) ([Windows.Graphics.Imaging.BitmapDecoder])
 $bitmap=Await-Result ($decoder.GetSoftwareBitmapAsync()) ([Windows.Graphics.Imaging.SoftwareBitmap])

 $result=Await-Result ($engine.RecognizeAsync($bitmap)) ([Windows.Media.Ocr.OcrResult])
 $blocks=@($result.Lines | ForEach-Object { $line=$_; $rects=@($line.Words | ForEach-Object BoundingRect); $left=($rects.X | Measure-Object -Minimum).Minimum; $top=($rects.Y | Measure-Object -Minimum).Minimum; $right=($rects | ForEach-Object {$_.X+$_.Width} | Measure-Object -Maximum).Maximum; $bottom=($rects | ForEach-Object {$_.Y+$_.Height} | Measure-Object -Maximum).Maximum; @{text=$line.Text;x=$left;y=$top;w=$right-$left;h=$bottom-$top} })
 @{lines=@($result.Lines | ForEach-Object Text);blocks=$blocks} | ConvertTo-Json -Depth 5 -Compress
 $bitmap.Dispose();$stream.Dispose()
} catch { @{error=$_.Exception.Message} | ConvertTo-Json -Compress }

}
$engine=[Windows.Media.Ocr.OcrEngine]::TryCreateFromLanguage([Windows.Globalization.Language]::new('en-US'))
if($null -eq $engine){throw 'English OCR is not installed'}
if($Worker){while($null -ne ($request=[Console]::ReadLine())){Read-RatingImage $request}}else{Read-RatingImage $ImagePath}
