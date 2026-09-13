using System;using System.Collections.Generic;using System.Drawing;using System.Drawing.Drawing2D;using System.IO;using System.Linq;
namespace TavernLens {
public static class RecordedCard {
 static readonly Dictionary<string,Bitmap> icons=new Dictionary<string,Bitmap>();
 public static string ArtId(Entity unit,Catalog catalog){var c=catalog.Get(unit.CardId);if(c!=null&&c.normal>0)return unit.CardId;if(unit.N("PREMIUM")>0){var gold=catalog.Golden(catalog.BaseId(unit.CardId));if(gold!=null)return gold.id;}return unit.CardId;}
 public static bool Golden(Entity unit,Catalog catalog){var c=catalog.Get(ArtId(unit,catalog));return unit.N("PREMIUM")>0||(c!=null&&c.normal>0);}
 public static int Health(Entity unit){return Math.Max(0,unit.N("HEALTH")-unit.N("DAMAGE"));}
 public static string[] Effects(Entity unit){return new[]{"TAUNT","DIVINE_SHIELD","REBORN","VENOMOUS","POISONOUS","DEATHRATTLE"}.Where(t=>unit.N(t)>0).ToArray();}
 static Bitmap Icon(string name){Bitmap image;if(icons.TryGetValue(name,out image))return image;string file=Path.Combine(Store.Root,"assets","status",name+".png");if(!File.Exists(file))return null;image=new Bitmap(file);icons[name]=image;return image;}
 static void Layer(Graphics g,string name){var image=Icon(name);if(image!=null)g.DrawImage(image,new RectangleF(0,0,300,350));}
 // Keep the original transparent canvases: each effect aligns with the entire board portrait.
 public static void Draw(Graphics g,Image art,Rectangle box,Entity unit,bool golden=false){var saved=g.Save();try{float scale=Math.Min(box.Width/325f,box.Height/350f);g.TranslateTransform(box.X+(box.Width-300*scale)/2,box.Y);g.ScaleTransform(scale,scale);g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.SmoothingMode=SmoothingMode.AntiAlias;
  string premium=golden?"_premium":"";
  if(unit.N("TAUNT")>0)Layer(g,"taunt"+premium);
  var portrait=g.Save();using(var ellipse=new GraphicsPath()){ellipse.AddEllipse(65,44,174,240);g.SetClip(ellipse,CombineMode.Intersect);g.DrawImage(art,new RectangleF(24,36,256,256),new RectangleF(art.Width*.25f,art.Height*.075f,art.Width*.50f,art.Height*.44f),GraphicsUnit.Pixel);}g.Restore(portrait);
  Layer(g,"border"+premium);
  if(unit.N("REBORN")>0)Layer(g,"reborn");
  if(unit.N("DIVINE_SHIELD")>0){var shield=Icon("divine-shield");if(shield!=null)g.DrawImage(shield,new RectangleF(-12,12,325,311));}
  bool dualEmblem=unit.N("DEATHRATTLE")>0&&(unit.N("VENOMOUS")>0||unit.N("POISONOUS")>0);var emblems=g.Save();if(dualEmblem)g.TranslateTransform(-18,0);if(unit.N("DEATHRATTLE")>0)Layer(g,"deathrattle");g.Restore(emblems);emblems=g.Save();if(dualEmblem)g.TranslateTransform(18,0);
  if(unit.N("VENOMOUS")>0)Layer(g,"venomous");else if(unit.N("POISONOUS")>0)Layer(g,"poisonous");
  g.Restore(emblems);Layer(g,"stats"+premium);
  Stat(g,new RectangleF(53,216,75,65),unit.N("ATK"),false);
  Stat(g,new RectangleF(175,216,75,65),Health(unit),unit.N("DAMAGE")>0);
 }finally{g.Restore(saved);}}
 static void Stat(Graphics g,RectangleF box,int value,bool damaged){using(var family=new FontFamily("Georgia"))using(var outline=new GraphicsPath()){outline.AddString(Math.Max(0,value).ToString(),family,(int)FontStyle.Bold,53,new PointF(0,0),StringFormat.GenericTypographic);var bounds=outline.GetBounds();float scale=Math.Min(1,Math.Min((box.Width-4)/Math.Max(1,bounds.Width),box.Height/Math.Max(1,bounds.Height)));using(var matrix=new Matrix(scale,0,0,scale,box.X+(box.Width-bounds.Width*scale)/2-bounds.X*scale,box.Y+(box.Height-bounds.Height*scale)/2-bounds.Y*scale))outline.Transform(matrix);using(var pen=new Pen(Color.FromArgb(32,24,15),4){LineJoin=LineJoin.Round})g.DrawPath(pen,outline);using(var brush=new SolidBrush(damaged?Color.FromArgb(255,112,100):Color.FromArgb(255,252,226)))g.FillPath(brush,outline);}}
}
}
