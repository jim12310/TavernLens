using System;using System.Diagnostics;using System.IO;
namespace TavernLens {
// One local OCR process per app, reused across menu scans. The caller serializes requests.
public sealed class RatingWorker : IDisposable {
 Process process;bool disposed;
 public RatingOcr Read(string image){if(disposed)throw new ObjectDisposedException("RatingWorker");try{if(process==null||process.HasExited){Stop();process=new Process{StartInfo=new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"WindowsPowerShell","v1.0","powershell.exe"),"-NoLogo -NoProfile -NonInteractive -File \""+Path.Combine(Store.Root,"src","read-rating.ps1")+"\" -Worker"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true}};process.Start();process.StandardError.ReadToEndAsync();}process.StandardInput.WriteLine(image);process.StandardInput.Flush();var line=process.StandardOutput.ReadLineAsync();if(!line.Wait(8000)||line.Result==null)throw new Exception("Rating reader did not respond");return Store.Json().Deserialize<RatingOcr>(line.Result);}catch{Stop();throw;}}
 void Stop(){var p=process;process=null;if(p!=null){try{if(!p.HasExited)p.Kill();}catch{}p.Dispose();}}
 public void Dispose(){disposed=true;Stop();}
}
}
