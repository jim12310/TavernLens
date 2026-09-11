using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
namespace TavernLens {
public class FightSnapshot {
 public string Mode,SnapshotProblem;public bool FriendlyFirst=true,EnemyFirst=true;public TeamBoard FriendlyPartner,EnemyPartner;public string MatchKey,Key,HeroId,Stage="Before combat triggers";public int Turn,OpponentId;
 public List<Entity> Friendly=new List<Entity>(),Enemy=new List<Entity>(),All=new List<Entity>();
 public int FriendlyController,EnemyController,GameEntity;public DateTime CapturedAt;
}
public class Odds {public string key,status,reason,engine,scope;public double win,loss,draw,margin;public int simulations;public List<string> warnings=new List<string>();}
public class CombatRunner {
 public Odds Result=new Odds{status="waiting",reason="Waiting for both combat boards"};public bool Busy;
 public async Task Run(FightSnapshot fight){if(Busy)return;Busy=true;Result=new Odds{key=fight.Key,status="running",reason="Simulating combat…"};try{
  var output=await Task.Run(()=>{var p=new Process{StartInfo=new ProcessStartInfo(Path.Combine(Store.Root,"runtime","node.exe"),"\""+Path.Combine(Store.Root,"src","simulate.cjs")+"\""){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,WorkingDirectory=Store.Root}};p.Start();var stderr=p.StandardError.ReadToEndAsync();var stdout=p.StandardOutput.ReadToEndAsync();p.StandardInput.Write(Store.Json().Serialize(fight));p.StandardInput.Close();if(!p.WaitForExit(20000)){p.Kill();throw new Exception("Simulation timed out");}string text=stdout.Result;if(p.ExitCode!=0)throw new Exception("Simulation failed; no odds available");p.Dispose();return text;});Result=Store.Json().Deserialize<Odds>(output);
 }catch(Exception e){Result=new Odds{key=fight.Key,status="unavailable",reason=e.Message};}finally{Busy=false;}}
}
}
