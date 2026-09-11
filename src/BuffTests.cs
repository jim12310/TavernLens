using System;
using System.Drawing;
namespace TavernLens {
public static class BuffTests {
 static int count;static void Check(bool ok){if(!ok)throw new Exception("Buff counter regression");count++;}
 public static int Run(){count=0;var s=new MatchState{Stream=@"C:\Logs\Power.log",Started=true,Mode="GT_BATTLEGROUNDS"};var p=s.E(2);p.Tags["CARDTYPE"]="PLAYER";p.Tags["PLAYER_ID"]="1";p.Tags["CONTROLLER"]="1";
  Check(BuffValues.Read(s).Gems=="— / —");p.Tags["BACON_BLOODGEMBUFFATKVALUE"]="2";p.Tags["BACON_BLOODGEMBUFFHEALTHVALUE"]="5";
  Check(BuffValues.Read(s).Gems=="+3 / +6");Check(BuffValues.Read(s).Gems=="+3 / +6");
  var e=s.E(30);e.CardId="BG26_159pe";e.Tags["ATTACHED"]="2";e.Tags["TAG_SCRIPT_DATA_NUM_1"]="4";e.Tags["TAG_SCRIPT_DATA_NUM_2"]="1";
  Check(BuffValues.Read(s).Gems=="+5 / +6");e.Tags["ATTACHED"]="3";Check(BuffValues.Read(s).Gems=="+3 / +6");
  p.Tags["TAVERN_SPELL_ATTACK_INCREASE"]="3";p.Tags["TAVERN_SPELL_HEALTH_INCREASE"]="6";Check(BuffValues.Read(s).Spells=="+3 / +6");
  s.Mode="GT_BATTLEGROUNDS_DUO";Check(BuffValues.Read(s).Spells=="+3 / +6");s.Complete=true;Check(BuffValues.Read(s).Gems=="— / —");s.Complete=false;
  s.Feed("D 12:00:00 PowerTaskList.DebugPrintPower() - CREATE_GAME");Check(BuffValues.Read(s).Spells=="— / —");
  using(var image=new Bitmap(430,90))using(var g=Graphics.FromImage(image)){g.Clear(Color.FromArgb(24,23,29));g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;BuffCounters.DrawChip(g,new Rectangle(10,10,198,64),false,"+3 / +6");BuffCounters.DrawChip(g,new Rectangle(220,10,198,64),true,"+3 / +6");image.Save(System.IO.Path.Combine(Store.Root,"Buff-Counters.png"));}
  return count;
 }
}
}
