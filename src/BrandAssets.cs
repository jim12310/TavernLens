using System;using System.Drawing;using System.Drawing.Imaging;using System.IO;using System.Windows.Forms;
namespace TavernLens {
public static class BrandAssets {
 static Image Load(string name){string file=Path.Combine(Store.Root,"assets",name);if(!File.Exists(file))return null;using(var image=Image.FromFile(file))return new Bitmap(image);}
 public static readonly Image Logo=Load("ember-logo.png"),Background=Load("ember-background.png");
 public static readonly Icon AppIcon=File.Exists(Path.Combine(Store.Root,"assets","TavernLens.ico"))?new Icon(Path.Combine(Store.Root,"assets","TavernLens.ico"),32,32):SystemIcons.Application;
 public static void Paint(Graphics g,Rectangle area,int shade){if(Background!=null){double scale=Math.Max((double)area.Width/Background.Width,(double)area.Height/Background.Height);float w=(float)(area.Width/scale),h=(float)(area.Height/scale);g.DrawImage(Background,area,new RectangleF((Background.Width-w)/2,(Background.Height-h)/2,w,h),GraphicsUnit.Pixel);using(var tint=new SolidBrush(Color.FromArgb(shade,12,17,23)))g.FillRectangle(tint,area);}else g.Clear(Theme.Bg);}
}
public class TavernPage:TabPage {
 public TavernPage(string name):base(name){DoubleBuffered=true;}
 protected override void OnPaintBackground(PaintEventArgs e){BrandAssets.Paint(e.Graphics,ClientRectangle,120);}
}
}
