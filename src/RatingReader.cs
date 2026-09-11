using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace TavernLens {
public class RatingChange {public int Before,After;public int Delta {get{return After-Before;}}}
public static class TierOrder {public static int Rank(string value){int i=Array.IndexOf(new[]{"S","A","B","C","D","F"},value);return i<0?99:i;}}
public class OcrBlock {public string text;public double x,y,w,h;}
public class RatingOcr {public List<OcrBlock> blocks=new List<OcrBlock>();public List<string> lines=new List<string>();public string error;}
public class RatingReader : IDisposable {
 bool disposed;string modeCandidate;public void Dispose(){disposed=true;}
 readonly Main main;DateTime next=DateTime.MinValue;bool busy;int? candidate;int confirmations;public string Status="Waiting for Hearthstone rating";public bool Selecting;
 public RatingReader(Main m){main=m;}
 public void ResetConfirmation(){candidate=null;confirmations=0;next=DateTime.MinValue;}
 public static int? Parse(IEnumerable<string> lines){var numbers=lines.Select(x=>x.Trim()).Where(x=>Regex.IsMatch(x,@"^\d{1,3}(?:,\d{3})$|^\d{1,5}$")).Select(x=>Int32.Parse(x.Replace(",",""))).Distinct().ToArray();return numbers.Length==1?(int?)numbers[0]:null;}
 public static string DetectMode(IEnumerable<string> lines){var modes=lines.Select(x=>(x??"").Trim()).Where(x=>Regex.IsMatch(x,@"^(?:(?:Hearthstone\s+)?Battlegrounds\s*[-:·]?\s*)?(Solo|Duos)(?:\s+Battlegrounds)?$",RegexOptions.IgnoreCase)).Select(x=>Regex.IsMatch(x,@"\bDuos\b",RegexOptions.IgnoreCase)?"Duos":"Solo").Distinct().ToArray();return modes.Length==1?modes[0]:null;}
 public static string Signed(int value){return value>0?"+"+value:value.ToString();}
 public static int? Locate(IEnumerable<OcrBlock> blocks){var list=blocks.ToList();var values=new List<int>();foreach(var label in list.Where(b=>Regex.IsMatch(b.text??"",@"^\s*Ratin[gqe]\s*$",RegexOptions.IgnoreCase))){var candidates=list.Where(b=>b.y>=label.y+label.h*.6&&b.y<=label.y+Math.Max(180,label.h*6)&&Math.Abs(b.x+b.w/2-label.x-label.w/2)<=Math.Max(label.w*1.2,80)).Select(b=>new {Block=b,Value=Parse(new[]{b.text??""})}).Where(b=>b.Value.HasValue).OrderBy(b=>b.Block.y).ToList();if(candidates.Count>0)values.Add(candidates[0].Value.Value);}var unique=values.Distinct().ToArray();return unique.Length==1?(int?)unique[0]:null;}
 public async void Tick(IntPtr target,bool lobby){if(disposed||busy||target==IntPtr.Zero||DateTime.UtcNow<next)return;busy=true;next=DateTime.UtcNow.AddMilliseconds(400);try{var sample=await Task.Run(()=>LiveMenu.Read());if(disposed)return;
 if(sample.Loaded&&!sample.Transitioning){main.Scenes.Scene=sample.Scene==15?"BACON":sample.Scene==4?"GAMEPLAY":"OTHER";main.Scenes.Loading=false;}else if(sample.Transitioning)main.Scenes.Loading=true;
 if(sample.Mode==null){candidate=null;confirmations=0;modeCandidate=null;Status="Waiting for the Battlegrounds menu";return;}
 if(modeCandidate!=sample.Mode){modeCandidate=sample.Mode;candidate=null;confirmations=0;}main.SwitchMode(sample.Mode=="Duos");
 if(!sample.Rating.HasValue){Status="Waiting for Hearthstone rating data";return;}if(candidate==sample.Rating)confirmations++;else{candidate=sample.Rating;confirmations=1;}if(confirmations<2){Status="Confirming game rating…";return;}
 if(main.Session.CurrentRating!=sample.Rating.Value||main.Session.RatingGameCount!=main.Session.Games.Count){main.Session.ApplyRating(sample.Rating.Value);main.SaveSession();}Status="Live rating · "+sample.Mode;
 }catch(Exception e){if(!disposed)Status="Game-state reader unavailable: "+e.Message;next=DateTime.UtcNow.AddSeconds(2);}finally{busy=false;}}
}
}
