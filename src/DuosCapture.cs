using System;using System.Collections.Generic;using System.Linq;
namespace TavernLens {
public class TeamBoard {public int PlayerId,Turn;public bool First,Local,Teammate;public Entity Hero;public List<Entity> Board=new List<Entity>(),Entities=new List<Entity>();}
// Capture each warband when the client finishes setting it up. SETASIDE is not a board:
// it also contains reserve hands and combat copies. Never infer ownership from creation order.
public class DuosCapture {
 int local,mate,enemy,other;bool emitted;readonly Dictionary<int,TeamBoard> seen=new Dictionary<int,TeamBoard>();
 static Entity Copy(Entity e){return new Entity{Id=e.Id,CardId=e.CardId,Tags=new Dictionary<string,string>(e.Tags)};}
 public void Begin(MatchState s){seen.Clear();emitted=false;s.TeamBoards.Clear();var p=s.Player;if(p==null)return;local=p.N("PLAYER_ID");mate=p.N("BACON_DUO_TEAMMATE_PLAYER_ID");enemy=p.N("NEXT_OPPONENT_PLAYER_ID");other=p.N("NEXT_OPPONENT_TEAMMATE_PLAYER_ID");s.CurrentFight=new FightSnapshot{Mode=s.Mode,MatchKey=s.Key,Key=s.Key+"|duos="+((s.E(s.GameId).N("TURN")+1)/2),Turn=(s.E(s.GameId).N("TURN")+1)/2,SnapshotProblem="Waiting for all four Duos warbands to be revealed"};}
 public void Capture(MatchState s,bool unused){} // Solo timing is not a Duos board boundary.
 public void Ready(MatchState s){if(!s.Duos||s.Phase!="Combat"||new[]{local,mate,enemy,other}.Any(x=>x<=0)||new[]{local,mate,enemy,other}.Distinct().Count()!=4)return;
 foreach(var p in s.Entities.Values.Where(e=>e.S("CARDTYPE")=="PLAYER").ToList()){
 Entity hero;if(!s.Entities.TryGetValue(p.N("HERO_ENTITY"),out hero))continue;
 int pid=p.N("BACON_CURRENT_COMBAT_PLAYER_ID");if(pid<=0)pid=hero.N("PLAYER_ID");
 if(!new[]{local,mate,enemy,other}.Contains(pid)||seen.ContainsKey(pid)||String.IsNullOrEmpty(hero.CardId))continue;
 var units=s.Board(p.N("CONTROLLER")).Select(Copy).ToList();if(units.Count>7)continue;
 var b=new TeamBoard{PlayerId=pid,Turn=s.Turn,Local=pid==local,Teammate=pid==mate,First=!seen.Values.Any(x=>(x.Local||x.Teammate)==(pid==local||pid==mate)),Hero=Copy(hero),Board=units};
 var context=s.Entities.Values.Where(e=>e.N("CONTROLLER")==p.N("CONTROLLER")&&(e.S("ZONE")=="PLAY"||e.S("ZONE")=="HAND"||e.S("ZONE")=="SECRET")).Select(Copy).ToList();
 context.RemoveAll(e=>e.S("CARDTYPE")=="PLAYER"||e.S("CARDTYPE")=="HERO");context.Add(b.Hero);var player=Copy(p);player.Id=-10000-pid;player.Tags["PLAYER_ID"]=pid.ToString();context.Add(player);
 foreach(var e in context)e.Tags["CONTROLLER"]=pid.ToString();foreach(var e in units)e.Tags["CONTROLLER"]=pid.ToString();b.Entities=context;seen[pid]=b;
 // Immutable snapshots survive subsequent attacks, hero swaps, and recruit phases.
 s.LastFights[pid]=new FightSnapshot{Mode=s.Mode,MatchKey=s.Key,Key=s.Key+"|turn="+s.Turn+"|player="+pid,Turn=s.Turn,OpponentId=pid,HeroId=hero.CardId,Enemy=units,Stage="Duos combat opening"};
 }
 s.TeamBoards=seen.Values.ToList();if(seen.Count!=4||emitted)return;
 var me=seen[local];var foe=seen[enemy];var fight=new FightSnapshot{Mode=s.Mode,MatchKey=s.Key,Key=s.Key+"|duos="+s.Turn,Turn=s.Turn,Stage="Duos combat opening",OpponentId=enemy,HeroId=foe.Hero.CardId,FriendlyController=local,EnemyController=enemy,GameEntity=s.GameId,CapturedAt=DateTime.UtcNow,Friendly=me.Board,Enemy=foe.Board,FriendlyPartner=seen[mate],EnemyPartner=seen[other],FriendlyFirst=me.First,EnemyFirst=foe.First,All=seen.Values.SelectMany(b=>b.Entities).Concat(new[]{Copy(s.E(s.GameId))}).ToList()};
 s.CurrentFight=fight;s.Fights.Add(fight);emitted=true;if(s.OnCombat!=null)s.OnCombat(fight);
 }
}
}
