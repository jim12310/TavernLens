using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
namespace TavernLens {
public static class TrackingSetup {
 public static string FindLog(string install) {
  var root=Path.Combine(install??"","Logs"); var files=new List<string>();
  if(!Directory.Exists(root))return "";
  try { files.AddRange(Directory.GetFiles(root,"Power.log")); foreach(var dir in Directory.GetDirectories(root))try{files.AddRange(Directory.GetFiles(dir,"Power.log"));}catch(UnauthorizedAccessException){} }
  catch(IOException){} catch(UnauthorizedAccessException){}
  return files.Where(File.Exists).OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault()??"";
 }
 public static string ConfigPath {get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Blizzard","Hearthstone","log.config");}}
 public static bool Enabled(){try {var text=File.ReadAllText(ConfigPath);return new[]{"Power","LoadingScreen"}.All(s=>{var m=Regex.Match(text,@"(?ims)^\["+s+@"\][^\r\n]*\r?\n(.*?)(?=^\[|\z)");return m.Success&&Regex.IsMatch(m.Groups[1].Value,@"(?im)^\s*FilePrinting\s*=\s*true\s*$")&&Regex.IsMatch(m.Groups[1].Value,@"(?im)^\s*LogLevel\s*=\s*1\s*$");});}catch{return false;}}
 public static string Configure(string text){foreach(var section in new[]{"Power","LoadingScreen"}){string block="["+section+"]\r\nLogLevel=1\r\nFilePrinting=true\r\nConsolePrinting=false\r\nScreenPrinting=false\r\nVerbose=true\r\n";string pattern=@"(?ims)^\["+section+@"\][^\r\n]*\r?\n.*?(?=^\[|\z)";text=Regex.IsMatch(text,pattern)?Regex.Replace(text,pattern,block):(text.TrimEnd().Length==0?"":text.TrimEnd()+"\r\n")+block;}return text;}
 public static void Enable(){string p=ConfigPath;Directory.CreateDirectory(Path.GetDirectoryName(p));string old=File.Exists(p)?File.ReadAllText(p):"";if(File.Exists(p))File.Copy(p,p+".tavernlens-"+DateTime.UtcNow.ToString("yyyyMMddHHmmssfff")+".bak");File.WriteAllText(p,Configure(old));}
}
}

