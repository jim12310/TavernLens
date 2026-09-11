using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
namespace TavernLens {
public class BuffValues {
 public int? GemAttack,GemHealth,SpellAttack,SpellHealth;
 public string Gems {get{return Pair(GemAttack,GemHealth);}}
 public string Spells {get{return Pair(SpellAttack,SpellHealth);}}
 static string Pair(int? a,int? h){return (a.HasValue?"+"+a.Value:"—")+" / "+(h.HasValue?"+"+h.Value:"—");}
 public static BuffValues Read(MatchState state){
  var result=new BuffValues();var player=state.Player;if(!state.Started||state.Complete||!state.Battlegrounds||player==null)return result;
  // These are cumulative values, not deltas. Replaying a tag must never add it twice.
  var gems=state.Entities.Values.Where(e=>e.CardId=="BG26_159pe"&&e.N("ATTACHED")==player.Id&&e.S("ZONE")!="GRAVEYARD"&&e.S("ZONE")!="REMOVEDFROMGAME").ToArray();
  result.GemAttack=Gem(player,"BACON_BLOODGEMBUFFATKVALUE",gems,"TAG_SCRIPT_DATA_NUM_1");
  result.GemHealth=Gem(player,"BACON_BLOODGEMBUFFHEALTHVALUE",gems,"TAG_SCRIPT_DATA_NUM_2");
  result.SpellAttack=Value(player,"TAVERN_SPELL_ATTACK_INCREASE");result.SpellHealth=Value(player,"TAVERN_SPELL_HEALTH_INCREASE");return result;
 }
 static int? Value(Entity e,string tag){int n;return e.Tags.ContainsKey(tag)&&Int32.TryParse(e.Tags[tag],out n)&&n>=0?(int?)n:null;}
 static int? Gem(Entity player,string tag,Entity[] enchants,string script){int? bonus=Value(player,tag);foreach(var e in enchants){var v=Value(e,script);if(v.HasValue)bonus=bonus.HasValue?Math.Max(bonus.Value,v.Value):v;}return bonus.HasValue?(int?)(1+bonus.Value):null;}
}
public class BuffCounters : Floating {
 readonly Main main;BuffValues values=new BuffValues();string signature="";
 public BuffCounters(Main m){main=m;Text="Blood Gem and Tavern spell bonuses";Size=new Size(410,70);BackColor=Color.FromArgb(1,2,3);TransparencyKey=BackColor;PassThrough(true);}
 public void Follow(Rectangle game){var s=main.State;if(!s.Battlegrounds||!s.Started||s.Complete||s.Phase=="Hero selection"||main.Scenes.InLobby||main.Scenes.Loading){Hide();return;}values=BuffValues.Read(s);string next=s.Key+"|"+values.Gems+"|"+values.Spells;int width=Math.Min(410,game.Width/2);Place(new Rectangle(game.Left+(int)(game.Width*.16),game.Top+(int)(game.Height*.79),width,70));if(next!=signature){signature=next;Invalidate();}}
 protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(BackColor);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;DrawChip(e.Graphics,new Rectangle(2,2,Width/2-8,64),false,values.Gems);DrawChip(e.Graphics,new Rectangle(Width/2+2,2,Width/2-8,64),true,values.Spells);}
 public static void DrawChip(Graphics g,Rectangle r,bool spell,string value){
  using(var path=new GraphicsPath()){int rad=52;path.AddArc(r.X,r.Y,rad,rad,90,180);path.AddArc(r.Right-rad,r.Y,rad,rad,270,180);path.CloseFigure();using(var b=new SolidBrush(Color.FromArgb(38,34,35)))g.FillPath(b,path);using(var p=new Pen(Color.FromArgb(14,12,13),2))g.DrawPath(p,path);}
  var circle=new Rectangle(r.X+3,r.Y+3,46,46);var accent=spell?Color.FromArgb(198,112,247):Color.FromArgb(245,174,85);
  using(var b=new LinearGradientBrush(circle,accent,Color.FromArgb(55,20,43),60))g.FillEllipse(b,circle);using(var p=new Pen(accent,2))g.DrawEllipse(p,circle);
  if(!spell){var pts=new[]{new Point(r.X+23,r.Y+8),new Point(r.X+39,r.Y+25),new Point(r.X+29,r.Y+43),new Point(r.X+12,r.Y+35),new Point(r.X+13,r.Y+17)};using(var b=new SolidBrush(Color.FromArgb(181,23,47)))g.FillPolygon(b,pts);using(var p=new Pen(Color.FromArgb(255,142,153),2)){g.DrawPolygon(p,pts);g.DrawLine(p,pts[0],pts[3]);g.DrawLine(p,pts[0],pts[2]);}}
  else {using(var p=new Pen(Color.FromArgb(255,226,255),3)){g.DrawLine(p,r.X+17,r.Y+35,r.X+31,r.Y+17);g.DrawLine(p,r.X+31,r.Y+10,r.X+31,r.Y+23);g.DrawLine(p,r.X+25,r.Y+16,r.X+38,r.Y+16);g.DrawLine(p,r.X+36,r.Y+31,r.X+36,r.Y+40);g.DrawLine(p,r.X+32,r.Y+35,r.X+40,r.Y+35);}}
  using(var f=Theme.Font(13,true))TextRenderer.DrawText(g,value,f,new Rectangle(r.X+54,r.Y+16,r.Width-57,28),Color.White,TextFormatFlags.VerticalCenter|TextFormatFlags.HorizontalCenter|TextFormatFlags.NoPadding);
  using(var f=Theme.Font(7.5f,true))TextRenderer.DrawText(g,spell?"TAVERN SPELL +":"BLOOD GEM",f,new Rectangle(r.X,r.Y+51,r.Width,13),accent,TextFormatFlags.HorizontalCenter|TextFormatFlags.NoPadding);
 }
}
}
