using System;
using System.IO;
using System.Security.Cryptography;
namespace TavernLens {
public static class UpdaterTests {
 public static int Run(){int count=0;Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception("Updater test: "+name);count++;};
  check(AutoUpdater.IsNewer("1.0.7","1.0.6"),"new release detected");check(!AutoUpdater.IsNewer("1.0.6","1.0.6"),"same release ignored");check(!AutoUpdater.IsNewer("bad","1.0.6"),"invalid version ignored");
  string file=Path.Combine(Path.GetTempPath(),"TavernLens-updater-test.bin");File.WriteAllText(file,"verified update");string digest;using(var stream=File.OpenRead(file))using(var sha=SHA256.Create())digest=BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();AutoUpdater.Verify(file,digest);check(File.Exists(file),"matching checksum accepted");bool rejected=false;try{AutoUpdater.Verify(file,new string('0',64));}catch(InvalidDataException){rejected=true;}check(rejected&&!File.Exists(file),"bad checksum rejected and removed");return count;
}
}
}
