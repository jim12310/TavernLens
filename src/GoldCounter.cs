using System;using System.Drawing;using System.Drawing.Drawing2D;using System.Linq;using System.Windows.Forms;
namespace TavernLens {
public class GoldValues {
 public int? Maximum;public int Guaranteed,Gambles;
 public int Tie {get{return Guaranteed+Gambles;}}
 public int Win {get{return Guaranteed+3*Gambles;}}
 public bool Pending {get{return Guaranteed!=0||Gambles>0;}}
 public string Range {get{return Gambles>0?Guaranteed+"/"+Win:Guaranteed.ToString();}}
 static int? Tag(Entity e,string name,string numeric){int n;string value;if(e.Tags.TryGetValue(name,out value)||e.Tags.TryGetValue(numeric,out value))if(Int32.TryParse(value,out n))return n;return null;}
 public static GoldValues Read(MatchState s){var v=new GoldValues();var p=s.Player;if(!s.Started||s.Complete||!s.Battlegrounds||p==null)return v;
  v.Maximum=Tag(p,"BACON_MAX_RESOURCES","3148");
  v.Guaranteed=(Tag(p,"BACON_PLAYER_EXTRA_GOLD_NEXT_TURN","4286")??0)-(Tag(p,"BACON_PLAYER_OVERDRAWN_GOLD_NEXT_TURN","4287")??0);
  // Read live enchantments rather than counting casts: stacking, replay and removal stay idempotent.
  v.Gambles=s.Entities.Values.Count(e=>e.CardId=="BG28_884e"&&(e.S("ZONE")=="PLAY"||e.S("ZONE")=="3")&&(e.N("CONTROLLER")==s.Controller||e.N("ATTACHED")==p.Id||(s.Hero!=null&&e.N("ATTACHED")==s.Hero.Id)));
  return v;
 }
}
public class GoldCounter:Floating {
 readonly Main main;GoldValues values=new GoldValues();string signature="";
 public GoldCounter(Main m){main=m;Text="Gold income";PassThrough(true);}
 public void Follow(Rectangle game){var s=main.State;if(!s.Battlegrounds||!s.Started||s.Complete||s.Phase=="Hero selection"||main.Scenes.InLobby||main.Scenes.Loading){Hide();return;}values=GoldValues.Read(s);string next=values.Maximum+"|"+values.Guaranteed+"|"+values.Gambles;Place(new Rectangle(game.Left+(int)(game.Width*.16),game.Top+(int)(game.Height*.79)+54,values.Pending?250:116,values.Pending?62:42));if(next!=signature){signature=next;Invalidate();}}
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);Draw(e.Graphics,ClientRectangle,values);}
 public static void Draw(Graphics g,Rectangle r,GoldValues v){g.SmoothingMode=SmoothingMode.AntiAlias;using(var b=new LinearGradientBrush(new Rectangle(9,10,25,25),Color.FromArgb(255,222,111),Color.FromArgb(176,111,30),45))g.FillEllipse(b,9,10,25,25);using(var p=new Pen(Color.FromArgb(94,57,18),2))g.DrawEllipse(p,12,13,19,19);
  using(var small=Theme.Font(7.5f,true))using(var large=Theme.Font(14,true)){TextRenderer.DrawText(g,"MAX GOLD",small,new Rectangle(41,4,72,16),Theme.Gold,TextFormatFlags.NoPadding);TextRenderer.DrawText(g,v.Maximum.HasValue?v.Maximum.ToString():"—",large,new Rectangle(41,18,72,26),Theme.Ink,TextFormatFlags.NoPadding);
   if(v.Pending){using(var pen=new Pen(Color.FromArgb(68,64,58)))g.DrawLine(pen,116,9,116,r.Height-9);TextRenderer.DrawText(g,"NEXT TURN +",small,new Rectangle(129,4,116,16),Theme.Gold,TextFormatFlags.NoPadding);TextRenderer.DrawText(g,v.Range,large,new Rectangle(129,18,116,25),Theme.Ink,TextFormatFlags.NoPadding);if(v.Gambles>0)TextRenderer.DrawText(g,"TIE "+v.Tie+"  ·  WIN "+v.Win,small,new Rectangle(129,43,116,15),Theme.Muted,TextFormatFlags.NoPadding);else TextRenderer.DrawText(g,"GUARANTEED",small,new Rectangle(129,43,116,15),Theme.Muted,TextFormatFlags.NoPadding);}}
 }
}
}
