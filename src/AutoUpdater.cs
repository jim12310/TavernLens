using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TavernLens {
public class UpdateManifest { public string version,url,sha256,notes; }
public static class AutoUpdater {
 public const string CurrentVersion="1.0.9";
 const string ManifestUrl="https://tavernlens.pages.dev/update.json";
 static bool checking;
 public static async void Check(Main owner){
  if(checking||Program.DiagnosticMode)return;checking=true;
  try{
   ServicePointManager.SecurityProtocol=(SecurityProtocolType)3072;
   var manifest=await Task.Run(()=>{using(var client=new WebClient()){client.Headers[HttpRequestHeader.UserAgent]="TavernLens/"+CurrentVersion;return Store.Json().Deserialize<UpdateManifest>(client.DownloadString(ManifestUrl));}});
   if(manifest==null||!IsNewer(manifest.version,CurrentVersion)||!Uri.IsWellFormedUriString(manifest.url,UriKind.Absolute)||!manifest.url.StartsWith("https://",StringComparison.OrdinalIgnoreCase))return;
   string detail=String.IsNullOrWhiteSpace(manifest.notes)?"A newer TavernLens version is ready.":manifest.notes;
   if(MessageBox.Show(detail+"\n\nUpdate from "+CurrentVersion+" to "+manifest.version+" now?","TavernLens update",MessageBoxButtons.YesNo,MessageBoxIcon.Information)!=DialogResult.Yes)return;
   owner.SetUpdateStatus("DOWNLOADING UPDATE…");
   string installer=Path.Combine(Path.GetTempPath(),"TavernLens-Setup-"+manifest.version+".exe");
   await Task.Run(()=>{using(var client=new WebClient()){client.Headers[HttpRequestHeader.UserAgent]="TavernLens/"+CurrentVersion;client.DownloadFile(manifest.url,installer);}Verify(installer,manifest.sha256);});
   Process.Start(new ProcessStartInfo(installer,"/FROMAPP"){UseShellExecute=true});owner.ExitForUpdate();
  }catch(Exception e){owner.SetUpdateStatus("UPDATE CHECK FAILED · "+e.Message);}finally{checking=false;}
 }
 public static bool IsNewer(string available,string installed){Version remote,current;return Version.TryParse(available,out remote)&&Version.TryParse(installed,out current)&&remote>current;}
 public static void Verify(string file,string expected){
  if(String.IsNullOrWhiteSpace(expected)||expected.Length!=64)throw new InvalidDataException("Update checksum is missing.");
  string actual;using(var stream=File.OpenRead(file))using(var hash=SHA256.Create())actual=BitConverter.ToString(hash.ComputeHash(stream)).Replace("-","").ToLowerInvariant();
  if(!String.Equals(actual,expected.Trim(),StringComparison.OrdinalIgnoreCase)){try{File.Delete(file);}catch{}throw new InvalidDataException("The downloaded update did not pass verification.");}
 }
}
}
