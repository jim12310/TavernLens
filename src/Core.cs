using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace TavernLens {
public static class Store {
 public static string Root=AppDomain.CurrentDomain.BaseDirectory;
 public static string Data {get{return Path.Combine(Root,"data");}}
 public static JavaScriptSerializer Json(){return new JavaScriptSerializer{MaxJsonLength=40000000,RecursionLimit=100};}
 public static T Read<T>(string file,T fallback){try{return Json().Deserialize<T>(File.ReadAllText(Path.Combine(Data,file)));}catch{return fallback;}}
 public static void Write(string file,object value){Directory.CreateDirectory(Data);string p=Path.Combine(Data,file);File.WriteAllText(p+".tmp",Json().Serialize(value),Encoding.UTF8);if(File.Exists(p))File.Replace(p+".tmp",p,null);else File.Move(p+".tmp",p);}
}
public class Card {public string id,name,text,type,tribe;public int dbfId,tier,attack,health,normal,premium;public bool pool,hero,spell,duosOnly,solosOnly;}
public class CardData {public string build,updatedAt;public List<Card> cards=new List<Card>();}
public class HeroRank {public string id,name,games,url;public int rank;public double avg,pick;}
public class Comp {public bool unranked;public string notes;public string name,tier,url,games,build,detailError;public double avg,first,top4;public List<string> core=new List<string>(),addons=new List<string>();}
public class Meta {public string build,updatedAt,sourceAge,source,url,scope;public List<HeroRank> heroes=new List<HeroRank>();public List<Comp> comps=new List<Comp>();}
public class Patch {public string title,date,url,version,checkedAt;}
public class UpdateStatus {public string checkedAt;public List<string> errors=new List<string>();}
public class Settings {public List<string> PinnedCards=new List<string>(),PinnedTribes=new List<string>();public string DataMode="Solo";public string HearthstonePath=@"C:\Program Files (x86)\Hearthstone";public int OverlayWidth=340;public double Opacity=0.92;public double RatingX,RatingY,RatingW,RatingH;public bool OverlayEnabled=true;public bool GuideCollapsed=false,SessionCollapsed=false;}
public class Catalog {
 public bool Duos;public Meta BuildMeta {get{return SoloMeta;}}public CardData Cards;public Meta SoloMeta,DuosMeta;public Meta Meta {get{return Duos?DuosMeta:SoloMeta;}}public Patch Patch;public UpdateStatus Status;Dictionary<string,Card> byId;
 public Catalog(){Reload();}
 public void Reload(){Cards=Store.Read("cards.json",new CardData());SoloMeta=Store.Read("meta.json",new Meta());DuosMeta=Store.Read("meta-duos.json",new Meta());Patch=Store.Read("patch.json",new Patch());Status=Store.Read("update-status.json",new UpdateStatus());byId=Cards.cards.GroupBy(x=>x.id).ToDictionary(x=>x.Key,x=>x.First());}
 public void Adopt(Catalog other){Cards=other.Cards;SoloMeta=other.SoloMeta;DuosMeta=other.DuosMeta;Patch=other.Patch;Status=other.Status;byId=other.byId;}
 public bool Available(Card c){var b=Get(BaseId(c.id))??c;return Duos?!b.solosOnly:!b.duosOnly;}
 public Card Get(string id){Card c;return id!=null&&byId.TryGetValue(id,out c)?c:null;}
 public string Name(string id){var c=Get(id);return c==null?(String.IsNullOrEmpty(id)?"Unknown card":id):c.name;}
 public string BaseId(string id){var c=Get(id);if(c!=null&&c.normal>0){var n=Cards.cards.FirstOrDefault(x=>x.dbfId==c.normal);if(n!=null)return n.id;}return id;}
 public Card Golden(string id){var normal=Get(BaseId(id));if(normal==null||normal.premium<=0)return null;return Cards.cards.FirstOrDefault(c=>c.dbfId==normal.premium&&c.normal==normal.dbfId&&c.type==normal.type);}
 public bool MetaFresh {get {DateTime d;if(String.IsNullOrEmpty(Meta.build)||Meta.build!=Cards.build||!DateTime.TryParse(Meta.updatedAt,out d))return false;double age=(DateTime.UtcNow-d.ToUniversalTime()).TotalHours;if(age<0||age>=24||String.IsNullOrEmpty(Meta.sourceAge))return false;var m=Regex.Match(Meta.sourceAge,@"(\d+)\s+(minute|hour|second)");if(m.Success){double n=Double.Parse(m.Groups[1].Value);age+=m.Groups[2].Value=="hour"?n:m.Groups[2].Value=="minute"?n/60:n/3600;return age<24;}return Regex.IsMatch(Meta.sourceAge,@"(a minute|an hour|less than|just now)");}}
 public string Freshness {get{return MetaFresh?"Source build "+Meta.build+" · "+Meta.sourceAge:"CHECK DATA · source/card mismatch, old cache, or unknown source age";}}
}
public class Entity {
 public int Id;public string CardId="";public Dictionary<string,string> Tags=new Dictionary<string,string>();
 public int N(string key){int n;return Tags.ContainsKey(key)&&Int32.TryParse(Tags[key],out n)?n:0;}
 public string S(string key){return Tags.ContainsKey(key)?Tags[key]:"";}
}
public class MatchRecord {public string key,hero,endedAt,build,mode;public int placement,turns;}
public class MatchState {public DuosCapture DuosCapture=new DuosCapture();public List<TeamBoard> TeamBoards=new List<TeamBoard>();public Dictionary<string,int> ChoicePositions=new Dictionary<string,int>();public List<string> Choices=new List<string>();public string ChoiceKind="";public Dictionary<int,List<TierVisit>> TierHistory=new Dictionary<int,List<TierVisit>>();
 public Dictionary<int,FightSnapshot> LastFights=new Dictionary<int,FightSnapshot>();
 public List<string> SnapshotNotes=new List<string>();public List<FightSnapshot> Fights=new List<FightSnapshot>();public FightSnapshot CurrentFight;public Action<FightSnapshot> OnCombat;int capturedTurn,simulatedTurn;
 public Dictionary<int,Entity> Entities=new Dictionary<int,Entity>();
 public Dictionary<string,int> Names=new Dictionary<string,int>();
 public string Mode="",Build="",Key="",Stream="";public int GameId=1,CurrentId,CompletedCount;public bool Started,Complete;
 public DateTime LastLine=DateTime.MinValue;public string EventTime="";
 public Action<MatchRecord> OnComplete;
 readonly Dictionary<int,string> playerNames=new Dictionary<int,string>();
 static readonly Regex tagChange=new Regex(@"TAG_CHANGE Entity=(.+?) tag=(\w+) value=(\S+)",RegexOptions.Compiled);
 static readonly Regex tag=new Regex(@"^tag=(\w+) value=(\S+)",RegexOptions.Compiled);
 static readonly Regex full=new Regex(@"FULL_ENTITY - (?:Creating|Updating) (?:ID|Entity)=(\d+) CardID=(\S*)",RegexOptions.Compiled);
 public Entity E(int id){Entity e;if(!Entities.TryGetValue(id,out e)){e=new Entity{Id=id};Entities[id]=e;}return e;}
 int Resolve(string raw){int n;if(Int32.TryParse(raw.Trim(),out n))return n;var m=Regex.Match(raw,@"\bid=(\d+)");if(m.Success)return Int32.Parse(m.Groups[1].Value);return Names.TryGetValue(raw.Trim(),out n)?n:0;}
 public Entity Player {get{return Entities.Values.FirstOrDefault(x=>x.S("CARDTYPE")=="PLAYER"&&x.N("BACON_DUMMY_PLAYER")==0&&x.N("PLAYER_ID")>0&&x.N("PLAYER_ID")!=9);}}
 public int Controller {get {var p=Player;return p==null?0:p.N("CONTROLLER");}}
 public Entity Hero {get {var p=Player;return p!=null&&p.N("HERO_ENTITY")>0?E(p.N("HERO_ENTITY")):null;}}
 public int Turn {get {return (E(GameId).N("TURN")+1)/2;}}
 public string Phase {get {return Complete?"Finished":!Started?"Waiting":E(GameId).N("TURN")==0?"Hero selection":E(GameId).N("TURN")%2==1?"Recruit":"Combat";}}
 public bool Duos {get{return Mode=="GT_BATTLEGROUNDS_DUO"||Mode=="GT_BATTLEGROUNDS_DUO_FRIENDLY";}}
 public bool Battlegrounds {get{return Solo||Duos;}}
 public bool Solo {get{return Mode=="GT_BATTLEGROUNDS";}}
 public List<Entity> Board(int controller){return Entities.Values.Where(x=>controller>0&&x.N("CONTROLLER")==controller&&x.S("CARDTYPE")=="MINION"&&x.S("ZONE")=="PLAY").OrderBy(x=>x.N("ZONE_POSITION")).ToList();}
 public List<Entity> Lobby {get {return Entities.Values.Where(x=>x.S("CARDTYPE")=="HERO"&&x.N("PLAYER_ID")>0&&!x.CardId.Contains("KelThuzad")&&!x.CardId.Contains("HERO_PH")).GroupBy(x=>x.N("PLAYER_ID")).Select(g=>g.OrderByDescending(x=>x.N("PLAYER_LEADERBOARD_PLACE")>0&&x.N("PLAYER_LEADERBOARD_PLACE")<=8).ThenByDescending(x=>x.S("ZONE")=="SETASIDE"||x.S("ZONE")=="PLAY").ThenBy(x=>x.Id).First()).OrderBy(x=>x.N("PLAYER_LEADERBOARD_PLACE")==0?9:x.N("PLAYER_LEADERBOARD_PLACE")).ToList();}}
 public Entity Portrait(int slot){var candidates=Lobby.Where(e=>e.N("PLAYER_LEADERBOARD_PLACE")==slot+1).ToList();return candidates.Count==1?candidates[0]:null;}
 public void Feed(string line){if(Started&&line.Contains("GameState.DebugPrintEntityChoices()")){var kind=Regex.Match(line,@"ChoiceType=(\w+)");if(kind.Success){ChoiceKind=kind.Groups[1].Value;Choices.Clear();ChoicePositions.Clear();}var choice=Regex.Match(line,@"Entities\[\d+\]=.*cardId=(\S+) player=(\d+)");if(choice.Success&&Int32.Parse(choice.Groups[2].Value)==Controller&&!Choices.Contains(choice.Groups[1].Value)){Choices.Add(choice.Groups[1].Value);var position=Regex.Match(line,@"zonePos=(\d+)");if(position.Success)ChoicePositions[choice.Groups[1].Value]=Int32.Parse(position.Groups[1].Value);}return;}
  if(line.Contains("GameState.DebugPrintGame()")){
   var gm=Regex.Match(line,@"GameType=(\w+)");if(gm.Success)Mode=gm.Groups[1].Value;
   var bm=Regex.Match(line,@"BuildNumber=(\d+)");if(bm.Success)Build=bm.Groups[1].Value;
   var pm=Regex.Match(line,@"PlayerID=(\d+), PlayerName=(.*)");if(pm.Success)playerNames[Int32.Parse(pm.Groups[1].Value)]=pm.Groups[2].Value.Trim();
   return;
  }
  // PowerTaskList reflects displayed events; GameState can expose events before animation.
  if(!line.Contains("PowerTaskList.DebugPrintPower()"))return;
  string s=line.Substring(line.IndexOf(" - ")+3).Trim();if(!Started&&s!="CREATE_GAME")return;LastLine=DateTime.UtcNow;
  var day=Regex.Match(Stream,@"Hearthstone_(\d{4})_(\d{2})_(\d{2})");var clock=Regex.Match(line,@"^\w\s+(\d{2}:\d{2}:\d{2})");
  if(day.Success&&clock.Success)EventTime=day.Groups[1].Value+"-"+day.Groups[2].Value+"-"+day.Groups[3].Value+"T"+clock.Groups[1].Value;
  if(s=="CREATE_GAME") {DuosCapture=new DuosCapture();TeamBoards.Clear();Entities.Clear();Names.Clear();TierHistory.Clear();Choices.Clear();ChoicePositions.Clear();ChoiceKind="";LastFights.Clear();Fights.Clear();CurrentFight=null;capturedTurn=0;simulatedTurn=0;CurrentId=0;GameId=1;Started=true;Complete=false;Key=Path.GetDirectoryName(Stream)+"|"+line.Substring(0,line.IndexOf("PowerTaskList"));return;}
  
  if(s.StartsWith("BLOCK_START BlockType=ATTACK")&&Phase=="Combat"&&simulatedTurn!=Turn)CaptureFight(true);
  var m=Regex.Match(s,@"^GameEntity EntityID=(\d+)");if(m.Success){GameId=CurrentId=Int32.Parse(m.Groups[1].Value);E(CurrentId).Tags["CARDTYPE"]="GAME";Names["GameEntity"]=GameId;return;}
  m=Regex.Match(s,@"^Player EntityID=(\d+) PlayerID=(\d+)");if(m.Success){CurrentId=Int32.Parse(m.Groups[1].Value);var e=E(CurrentId);e.Tags["CARDTYPE"]="PLAYER";e.Tags["PLAYER_ID"]=m.Groups[2].Value;int pid=Int32.Parse(m.Groups[2].Value);if(playerNames.ContainsKey(pid))Names[playerNames[pid]]=CurrentId;return;}
  m=full.Match(s);if(m.Success){CurrentId=Int32.Parse(m.Groups[1].Value);E(CurrentId).CardId=m.Groups[2].Value;return;}
  m=Regex.Match(s,@"^(?:FULL_ENTITY|SHOW_ENTITY|CHANGE_ENTITY) - Updating (?:Entity=)?(.+?) CardID=(\S*)");if(m.Success){CurrentId=Resolve(m.Groups[1].Value);if(CurrentId>0)E(CurrentId).CardId=m.Groups[2].Value;return;}
  m=Regex.Match(s,@"^HIDE_ENTITY - Entity=(.+?) tag=ZONE value=(\w+)");if(m.Success){int id=Resolve(m.Groups[1].Value);if(id>0)E(id).Tags["ZONE"]=m.Groups[2].Value;CurrentId=0;return;}
  m=tagChange.Match(s);if(m.Success){int id=Resolve(m.Groups[1].Value);if(id==0&&Duos&&m.Groups[2].Value=="HERO_ENTITY"){int hid;if(Int32.TryParse(m.Groups[3].Value,out hid)&&Entities.ContainsKey(hid)){var owners=Entities.Values.Where(x=>x.S("CARDTYPE")=="PLAYER"&&x.N("CONTROLLER")==E(hid).N("CONTROLLER")).ToList();if(owners.Count==1){id=owners[0].Id;Names[m.Groups[1].Value.Trim()]=id;}}}if(id>0)Set(E(id),m.Groups[2].Value,m.Groups[3].Value);CurrentId=0;return;}
  m=tag.Match(s);if(m.Success&&CurrentId>0){Set(E(CurrentId),m.Groups[1].Value,m.Groups[2].Value);return;}
  CurrentId=0;
 }
 void Set(Entity e,string key,string value){
  if(Duos&&e.Id==GameId&&key=="TURN"&&Int32.Parse(value)>0&&Int32.Parse(value)%2==0)DuosCapture.Begin(this);
  int previous=e.N(key);e.Tags[key]=value;if(Duos&&e.Id==GameId&&key=="3533"&&previous==1&&value=="0")DuosCapture.Ready(this);if(e.S("CARDTYPE")=="HERO"&&e.N("PLAYER_ID")>0&&e.N("PLAYER_TECH_LEVEL")>0){int pid=e.N("PLAYER_ID");List<TierVisit> list;if(!TierHistory.TryGetValue(pid,out list)){list=new List<TierVisit>();TierHistory[pid]=list;}int tier=e.N("PLAYER_TECH_LEVEL");if(list.Count==0||list.Last().Tier!=tier)list.Add(new TierVisit{Turn=Turn,Tier=tier});}
  if(e.Id==GameId&&key=="STEP"&&value=="MAIN_ACTION"&&Phase=="Combat")CaptureFight();
  if(key=="BACON_BARTENDER_CARD_ID"&&String.IsNullOrEmpty(Mode))Mode="GT_BATTLEGROUNDS";
  if(e.Id==GameId&&key=="STATE"&&value=="COMPLETE"&&!Complete){Complete=true;CompletedCount++;var h=Hero;var p=Player;if(Duos&&p!=null)h=Lobby.FirstOrDefault(x=>x.N("PLAYER_ID")==p.N("PLAYER_ID"))??h;int place=h==null?0:h.N("PLAYER_LEADERBOARD_PLACE");if(place==0&&p!=null)place=p.N("PLAYER_LEADERBOARD_PLACE");if(place==0&&p!=null&&p.S("PLAYSTATE")=="WON")place=1;if(Battlegrounds&&place>=1&&place<=(Duos?4:8)&&OnComplete!=null)OnComplete(new MatchRecord{key=Key,hero=h==null?"":h.CardId,placement=place,turns=Turn,endedAt=EventTime==""?"Unknown log date":EventTime,build=Build,mode=Mode});}
 }
 static Entity Clone(Entity e){return new Entity{Id=e.Id,CardId=e.CardId,Tags=new Dictionary<string,string>(e.Tags)};}
 public void CaptureFight(bool forSimulation=false){if(Duos){DuosCapture.Capture(this,forSimulation);return;}if(!Solo||Turn<=0||(forSimulation?simulatedTurn==Turn:capturedTurn==Turn)||Controller<=0)return;var enemyPlayer=Entities.Values.FirstOrDefault(e=>e.S("CARDTYPE")=="PLAYER"&&e.N("CONTROLLER")!=Controller);if(enemyPlayer==null)return;int enemyController=enemyPlayer.N("CONTROLLER");var enemyHero=E(enemyPlayer.N("HERO_ENTITY"));int opponent=enemyPlayer.N("BACON_CURRENT_COMBAT_PLAYER_ID");if(opponent<=0)opponent=enemyHero.N("PLAYER_ID");if(opponent<=0&&Player!=null)opponent=Player.N("NEXT_OPPONENT_PLAYER_ID");if(opponent<=0)return;
  var snapshot=new FightSnapshot{MatchKey=Key,Key=Key+"|turn="+Turn,Turn=Turn,OpponentId=opponent,HeroId=enemyHero.CardId,FriendlyController=Controller,EnemyController=enemyController,GameEntity=GameId,CapturedAt=DateTime.UtcNow,Friendly=Board(Controller).Select(Clone).ToList(),Enemy=Board(enemyController).Select(Clone).ToList(),All=Entities.Values.Where(x=>x.S("ZONE")=="PLAY"||x.S("ZONE")=="HAND"||x.S("ZONE")=="SECRET"||x.S("CARDTYPE")=="PLAYER"||x.S("CARDTYPE")=="HERO"||x.Id==GameId).Select(Clone).ToList()};
  if(!forSimulation)SnapshotNotes.Add(Turn+":"+snapshot.Friendly.Count+"/"+snapshot.Enemy.Count);if(snapshot.Friendly.Count>7||snapshot.Enemy.Count>7)return;CurrentFight=snapshot;if(forSimulation){simulatedTurn=Turn;snapshot.Stage="After start-of-combat";if(capturedTurn!=Turn){capturedTurn=Turn;LastFights[opponent]=snapshot;Fights.Add(snapshot);}if(OnCombat!=null)OnCombat(snapshot);}else{capturedTurn=Turn;snapshot.Stage="Combat opening";LastFights[opponent]=snapshot;Fights.Add(snapshot);}
 }
}
// Keeps partial lines between polls and supports truncation/rotation without reading ahead.
public class Tail : IDisposable {
 FileStream file;long position;string pending="";Decoder decoder=Encoding.UTF8.GetDecoder();public string PathName="";public bool CaughtUp;
 public void Open(string path,bool fromEnd=false,long startPosition=0){Dispose();PathName=path;file=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete);position=fromEnd?file.Length:Math.Min(startPosition,file.Length);file.Position=position;pending="";decoder.Reset();CaughtUp=fromEnd;}
 public int Poll(Action<string> consume,int maxBytes=1048576){if(file==null)return 0;if(file.Length<position){file.Position=position=0;pending="";decoder.Reset();}int read=0;byte[] buffer=new byte[65536];char[] chars=new char[65536];while(read<maxBytes){int n=file.Read(buffer,0,Math.Min(buffer.Length,maxBytes-read));if(n==0)break;position+=n;read+=n;pending+=new string(chars,0,decoder.GetChars(buffer,0,n,chars,0));int at;while((at=pending.IndexOf('\n'))>=0){string line=pending.Substring(0,at).TrimEnd('\r');pending=pending.Substring(at+1);consume(line);}if(pending.Length>1048576)pending="";}CaughtUp=position>=file.Length;return read;}
 public void Dispose(){if(file!=null){file.Dispose();file=null;}}
}
}
