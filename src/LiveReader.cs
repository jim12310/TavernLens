using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace TavernLens {
// One parser owns mutable telemetry. Only detached snapshots reach the UI.
public sealed class LiveReader : IDisposable {
 readonly CancellationTokenSource stop=new CancellationTokenSource();readonly ConcurrentQueue<MatchRecord> results=new ConcurrentQueue<MatchRecord>();
 public volatile MatchState Latest=new MatchState();public volatile bool CaughtUp;public volatile string Error="";readonly Task worker;
 public LiveReader(string path){worker=Task.Run(async()=>{try{var bootstrap=Locate(path,stop.Token);using(var tail=new Tail()){var state=new MatchState{Stream=path};foreach(var line in bootstrap.Context)state.Feed(line);tail.Open(path,false,bootstrap.Complete?bootstrap.End:bootstrap.Start);bool recovering=true;state.OnComplete=r=>{if(!recovering)results.Enqueue(r);};while(!stop.IsCancellationRequested){int bytes=tail.Poll(state.Feed,262144);if(recovering&&tail.CaughtUp){recovering=false;if(state.Complete){state=new MatchState{Stream=path,Mode=state.Mode,Build=state.Build};state.OnComplete=r=>results.Enqueue(r);}Latest=Copy(state);}else if(!recovering&&bytes>0)Latest=Copy(state);CaughtUp=tail.CaughtUp;await Task.Delay(bytes>0&&!CaughtUp?1:80,stop.Token);}}}catch(OperationCanceledException){}catch(Exception e){Error=e.Message;}});}
 public class MatchStart {public long Start,End;public bool Complete=true;public string[] Context=new string[0];}
 public static MatchStart Locate(string path,CancellationToken cancel){var found=new MatchStart();var context=new Queue<string>();using(var file=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete)){long limit=file.Length,offset=0;byte[] bytes=new byte[65536];char[] chars=new char[65536];var decoder=Encoding.UTF8.GetDecoder();string pending="";while(file.Position<limit){cancel.ThrowIfCancellationRequested();int n=file.Read(bytes,0,(int)Math.Min(bytes.Length,limit-file.Position));if(n==0)break;pending+=new string(chars,0,decoder.GetChars(bytes,0,n,chars,0));int at;while((at=pending.IndexOf('\n'))>=0){string raw=pending.Substring(0,at+1);pending=pending.Substring(at+1);string line=raw.TrimEnd('\r','\n');if(line.Contains("GameState.DebugPrintGame()")){context.Enqueue(line);while(context.Count>64)context.Dequeue();}if(line.Contains("PowerTaskList.DebugPrintPower()")){if(line.Substring(line.IndexOf(" - ")+3).Trim()=="CREATE_GAME"){found.Start=offset;found.Complete=false;found.Context=context.ToArray();}else if(line.Contains("tag=STATE value=COMPLETE"))found.Complete=true;}offset+=Encoding.UTF8.GetByteCount(raw);}}found.End=offset;if(found.Complete){found.Start=offset;found.Context=context.ToArray();}}return found;}

 static MatchState Copy(MatchState s){return new MatchState{TeamBoards=new List<TeamBoard>(s.TeamBoards),Stream=s.Stream,Mode=s.Mode,Build=s.Build,Key=s.Key,Started=s.Started,Complete=s.Complete,GameId=s.GameId,LastLine=s.LastLine,EventTime=s.EventTime,CompletedCount=s.CompletedCount,Entities=s.Entities.ToDictionary(p=>p.Key,p=>new Entity{Id=p.Value.Id,CardId=p.Value.CardId,Tags=new Dictionary<string,string>(p.Value.Tags)}),Fights=new List<FightSnapshot>(s.Fights),LastFights=new Dictionary<int,FightSnapshot>(s.LastFights),TierHistory=s.TierHistory.ToDictionary(x=>x.Key,x=>new List<TierVisit>(x.Value)),ChoicePositions=new Dictionary<string,int>(s.ChoicePositions),Choices=new List<string>(s.Choices),ChoiceKind=s.ChoiceKind,CurrentFight=s.CurrentFight};}
 public MatchRecord[] Drain(){var list=new List<MatchRecord>();MatchRecord r;while(results.TryDequeue(out r))list.Add(r);return list.ToArray();}
 public void Dispose(){stop.Cancel();}
}
}
