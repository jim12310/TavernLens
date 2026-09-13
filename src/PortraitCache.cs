using System;using System.Collections.Concurrent;using System.Drawing;using System.IO;using System.Net;using System.Threading;using System.Threading.Tasks;using System.Text.RegularExpressions;
namespace TavernLens {
public static class PortraitCache {
 static readonly ConcurrentDictionary<string,Image> images=new ConcurrentDictionary<string,Image>();
 static readonly ConcurrentDictionary<string,DateTime> requested=new ConcurrentDictionary<string,DateTime>();
 static readonly SemaphoreSlim slots=new SemaphoreSlim(4);class PortraitClient:WebClient {protected override WebRequest GetWebRequest(Uri address){var request=base.GetWebRequest(address);request.Timeout=10000;return request;}}
 public static Image Get(string id,string build){if(String.IsNullOrEmpty(id)||!Regex.IsMatch(id,@"^[A-Za-z0-9_-]+$")||!Regex.IsMatch(build??"",@"^\d+$"))return null;string key=build+"/"+id;Image image;if(images.TryGetValue(key,out image))return image;DateTime previous;if(requested.TryGetValue(key,out previous)&&(DateTime.UtcNow-previous).TotalMinutes<1)return null;requested[key]=DateTime.UtcNow;
  Task.Run(()=>{slots.Wait();try{string dir=Path.Combine(Store.Data,"art","portraits",build),file=Path.Combine(dir,id+".jpg");byte[] bytes;if(File.Exists(file))bytes=File.ReadAllBytes(file);else{using(var client=new PortraitClient()){bytes=client.DownloadData("https://art.hearthstonejson.com/v1/256x/"+id+".jpg");if(client.ResponseHeaders["X-Image-Fallback"]!=null)return;}if(bytes.Length>2000000)return;}
   using(var stream=new MemoryStream(bytes))using(var source=Image.FromStream(stream)){if(source.Width!=source.Height||source.Width<128)return;images[key]=new Bitmap(source);}if(!File.Exists(file)){Directory.CreateDirectory(dir);File.WriteAllBytes(file,bytes);}ArtCache.Notify();
  }catch{}finally{slots.Release();}});return null;
 }
}
public static class HistoryLayout {
 public static Size Measure(int count,int headingWidth,Rectangle game){int available=Math.Max(100,game.Width-40);int width=Math.Min(available,Math.Max(Math.Min(headingWidth+24,420),Math.Max(220,count*112+20)));int tile=Math.Min(112,(width-20)/Math.Max(1,count));return new Size(width,count==0?64:40+(int)Math.Ceiling(tile*310f/325f)+8);}
}
}
