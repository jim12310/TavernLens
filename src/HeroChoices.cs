using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace TavernLens {
public class HeroChoices : Floating {
 readonly Main main;readonly List<HeroBadge> badges=new List<HeroBadge>();
 public HeroChoices(Main m){main=m;PassThrough(true);}
 public static string BaseHero(string id){int at=(id??"").IndexOf("_SKIN_",StringComparison.Ordinal);return at<0?id:id.Substring(0,at);}
 public static string Grade(HeroRank hero,IEnumerable<HeroRank> pool){var ranks=pool.Where(h=>h.avg>0).OrderBy(h=>h.avg).ToList();if(hero==null||hero.avg<=0||ranks.Count==0)return "—";double p=(double)ranks.Count(h=>h.avg<hero.avg)/ranks.Count;return p<.15?"S":p<.35?"A":p<.65?"B":p<.85?"C":"D";}
 public static Rectangle BadgeBounds(Rectangle game,int index,int count){double scale=game.Height/1080.0;int width=(int)(218*scale),height=(int)(54*scale);return new Rectangle(game.X+game.Width/2+(int)((index-(count-1)/2.0)*342*scale)-width/2,game.Y+(int)(game.Height*.225),Math.Max(125,width),Math.Max(44,height));}
 public static int Position(MatchState state,string id){var entity=state.Entities.Values.Where(e=>e.CardId==id&&e.S("ZONE")=="HAND"&&e.N("CONTROLLER")==state.Controller&&e.N("ZONE_POSITION")>0).OrderByDescending(e=>e.Id).FirstOrDefault();return entity!=null?entity.N("ZONE_POSITION"):state.ChoicePositions.ContainsKey(id)?state.ChoicePositions[id]:state.Choices.IndexOf(id)+1;}
 public void Follow(Rectangle game){if(main.State.Phase!="Hero selection"||main.State.Choices.Count==0||main.State.ChoiceKind!="MULLIGAN"||main.Scenes.Loading){Hide();return;}
 var ids=main.State.Choices.OrderBy(id=>Position(main.State,id)).Take(4).ToList();while(badges.Count<ids.Count)badges.Add(new HeroBadge());for(int i=0;i<badges.Count;i++){if(i>=ids.Count){badges[i].Hide();continue;}var rank=main.Catalog.Meta.heroes.FirstOrDefault(h=>h.id==BaseHero(ids[i]));bool fresh=main.Catalog.MetaFresh&&rank!=null;badges[i].Set(main.Catalog.Name(ids[i]),fresh?Grade(rank,main.Catalog.Meta.heroes):"—",fresh?(double?)rank.avg:null);badges[i].Place(BadgeBounds(game,i,ids.Count));}}
 public new void Hide(){base.Hide();foreach(var b in badges)b.Hide();}
 protected override void Dispose(bool disposing){if(disposing)foreach(var b in badges)b.Dispose();base.Dispose(disposing);}
}
public class HeroBadge : Floating {
 string name="",grade="—";double? average;
 public HeroBadge(){PassThrough(true);Size=new Size(218,54);}
 public static Color TierColor(string tier){return tier=="S"?Color.FromArgb(244,199,112):tier=="A"?Color.FromArgb(110,224,185):tier=="B"?Color.FromArgb(125,184,249):tier=="C"?Color.FromArgb(186,157,234):tier=="D"?Color.FromArgb(230,144,163):Theme.Muted;}
 public void Set(string hero,string tier,double? avg){if(name==hero&&grade==tier&&average==avg)return;name=hero;grade=tier;average=avg;Invalidate();}
 protected override void OnResize(EventArgs e){base.OnResize(e);if(Width>20&&Height>20){var old=Region;using(var shape=Design.Round(new Rectangle(0,0,Width,Height),10))Region=new Region(shape);if(old!=null)old.Dispose();}}
 protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.Clear(Theme.Panel);var color=TierColor(grade);Design.Surface(g,new Rectangle(1,1,Width-3,Height-3),Theme.Panel,Color.FromArgb(90,color),10);var state=g.Save();g.ScaleTransform(Width/218f,Height/54f);g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;using(var letter=new Font("Segoe UI",29,FontStyle.Bold,GraphicsUnit.Pixel))using(var small=new Font("Segoe UI",11,FontStyle.Bold,GraphicsUnit.Pixel))using(var value=new Font("Segoe UI",18,FontStyle.Bold,GraphicsUnit.Pixel))using(var tint=new SolidBrush(color))using(var ink=new SolidBrush(Theme.Ink))using(var muted=new SolidBrush(Theme.Muted)){g.DrawString(grade,letter,tint,12,8);g.DrawString("TIER",small,tint,48,21);g.DrawString(average.HasValue?average.Value.ToString("0.00"):"—",value,ink,103,16);g.DrawString(average.HasValue?"avg":"no data",small,muted,158,21);}g.Restore(state);}
}
}
