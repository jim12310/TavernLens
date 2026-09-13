using System;using System.Drawing;
namespace TavernLens {
public static class GoldTests {
 static int count;static void Check(bool value,string name){if(!value)throw new Exception("Gold regression: "+name);count++;}
 static void Feed(MatchState s,string line){s.Feed("D 12:00:00 PowerTaskList.DebugPrintPower() - "+line);}
 public static int Run(){count=0;var s=new MatchState{Mode="GT_BATTLEGROUNDS",Stream=@"C:\Logs\Power.log"};foreach(var line in new[]{"CREATE_GAME","GameEntity EntityID=1","tag=TURN value=19","Player EntityID=2 PlayerID=1","tag=CONTROLLER value=11","tag=3148 value=11","tag=4286 value=2"})Feed(s,line);
  Check(GoldValues.Read(s).Maximum==11,"Strike Oil maximum");Check(GoldValues.Read(s).Guaranteed==2,"numeric next turn gold");
  foreach(int id in new[]{100,101,102}){Feed(s,"FULL_ENTITY - Creating ID="+id+" CardID=BG28_884e");Feed(s,"tag=CONTROLLER value="+(id==101?12:11));Feed(s,"tag=ZONE value=PLAY");}
  var v=GoldValues.Read(s);Check(v.Gambles==2&&v.Range=="2/8"&&v.Tie==4,"stacked own gambles only");Check(GoldValues.Read(s).Win==8,"reading twice does not add casts");Feed(s,"TAG_CHANGE Entity=100 tag=ZONE value=REMOVEDFROMGAME");v=GoldValues.Read(s);Check(v.Range=="2/5"&&v.Tie==3&&v.Win==5,"loss tie and win possibilities");
  Feed(s,"TAG_CHANGE Entity=2 tag=BACON_PLAYER_EXTRA_GOLD_NEXT_TURN value=4");Feed(s,"TAG_CHANGE Entity=2 tag=4286 value=2");Check(GoldValues.Read(s).Guaranteed==2,"numeric and symbolic alias updates");
  Feed(s,"TAG_CHANGE Entity=1 tag=TURN value=20");Check(GoldValues.Read(s).Range=="2/5","bonuses persist during combat");Feed(s,"TAG_CHANGE Entity=2 tag=4286 value=0");Feed(s,"TAG_CHANGE Entity=102 tag=ZONE value=REMOVEDFROMGAME");Feed(s,"TAG_CHANGE Entity=1 tag=TURN value=21");Check(!GoldValues.Read(s).Pending,"paid bonuses disappear");Check(GoldValues.Read(s).Maximum==11,"permanent cap persists");
  s.Mode="GT_BATTLEGROUNDS_DUO";Feed(s,"TAG_CHANGE Entity=2 tag=4286 value=6");Feed(s,"TAG_CHANGE Entity=2 tag=4287 value=2");Check(GoldValues.Read(s).Guaranteed==4,"Duos and next-turn debt");s.Complete=true;Check(!GoldValues.Read(s).Pending&&!GoldValues.Read(s).Maximum.HasValue,"completed game cleared");Feed(s,"CREATE_GAME");Check(!GoldValues.Read(s).Pending&&!GoldValues.Read(s).Maximum.HasValue,"new game resets");
  var one=HistoryLayout.Measure(1,260,new Rectangle(0,0,1920,1080));var seven=HistoryLayout.Measure(7,260,new Rectangle(0,0,1920,1080));Check(one.Width<400&&one.Height<180,"single minion compact box");Check(seven.Width>one.Width&&seven.Height==one.Height,"board width follows count");Check(HistoryLayout.Measure(0,260,new Rectangle(0,0,1920,1080)).Height==64,"no empty card area");return count;
 }
}
}
