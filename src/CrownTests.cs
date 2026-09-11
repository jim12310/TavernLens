using System;using System.Drawing;using System.IO;
namespace TavernLens {
public static class CrownTests {
 public static int Run(){int checks=0;foreach(string mode in new[]{"solo","duos"})using(var icon=new Bitmap(Path.Combine(Store.Root,"assets","menu-"+mode+".png")))foreach(int size in new[]{120,180,240})using(var frame=new Bitmap(1280,720)){using(var g=Graphics.FromImage(frame)){g.Clear(Color.FromArgb(24,27,32));g.DrawImage(icon,new Rectangle(420,60,size,(int)((double)size*icon.Height/icon.Width)));}if(CrownDetector.Detect(frame)!=(mode=="solo"?"Solo":"Duos"))throw new Exception("Crown scale recognition failed");checks++;}using(var frame=new Bitmap(1280,720)){if(CrownDetector.Detect(frame)!=null)throw new Exception("Blank crown false positive");checks++;using(var g=Graphics.FromImage(frame))using(var solo=new Bitmap(Path.Combine(Store.Root,"assets","menu-solo.png")))using(var duos=new Bitmap(Path.Combine(Store.Root,"assets","menu-duos.png"))){g.DrawImage(solo,new Rectangle(250,50,230,184));g.DrawImage(duos,new Rectangle(600,50,220,190));}if(CrownDetector.Detect(frame)!=null)throw new Exception("Ambiguous crowns must not select mode");checks++;}return checks;}
}
}
