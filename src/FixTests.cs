using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
namespace TavernLens {
public static class FixTests {
 public static int Run(){int n=0;Action<bool,string> check=(ok,title)=>{if(!ok)throw new Exception("Fix test: "+title);n++;};string path=Path.Combine(Path.GetTempPath(),"tavernlens-"+Guid.NewGuid()+".log");File.WriteAllText(path,"old game details\n");using(var tail=new Tail()){tail.Open(path,true);int lines=0;tail.Poll(x=>lines++);check(lines==0&&tail.CaughtUp,"startup skips existing lines");File.AppendAllText(path,"new line\n");tail.Poll(x=>lines++);check(lines==1,"new lines still consumed");}File.WriteAllText(path,"D 12:00:00 PowerTaskList.DebugPrintPower() - CREATE_GAME\nD 12:00:00 PowerTaskList.DebugPrintPower() - TAG_CHANGE Entity=GameEntity tag=STATE value=COMPLETE\n");using(var reader=new LiveReader(path)){Thread.Sleep(120);check(!reader.Latest.Started&&reader.Latest.Fights.Count==0,"completed match stays unloaded");File.AppendAllText(path,"D 12:00:00 GameState.DebugPrintGame() - GameType=GT_BATTLEGROUNDS\nD 12:00:01 PowerTaskList.DebugPrintPower() - CREATE_GAME\nD 12:00:01 PowerTaskList.DebugPrintPower() - GameEntity EntityID=1\n");for(int i=0;i<100&&!reader.Latest.Started;i++)Thread.Sleep(10);check(reader.Latest.Started&&reader.Latest.Solo,"new match starts background reader");check(reader.Drain().Length==0,"no fabricated historical result");}File.Delete(path);
 using(var main=new Main())using(var fixture=new Form{Bounds=new Rectangle(30,30,1180,800)}){fixture.Show();main.Settings.OverlayEnabled=true;main.Overlay.Guide.Collapsed=false;main.Overlay.Guide.SelectMode(0);main.Overlay.Follow(fixture.Handle,true);Application.DoEvents();var list=main.Overlay.Guide.Controls.OfType<RibbonList>().Single();var move=typeof(RibbonList).GetMethod("OnMouseMove",BindingFlags.Instance|BindingFlags.NonPublic);move.Invoke(list,new object[]{new MouseEventArgs(MouseButtons.None,0,25,48,0)});check(main.Overlay.Guide.Preview.Visible,"row hover opens preview");check(main.Overlay.Guide.Preview.Owner==main.Overlay,"preview is owned above sidebar");main.Overlay.Guide.Preview.Hide();move.Invoke(list,new object[]{new MouseEventArgs(MouseButtons.None,0,25,48,0)});check(main.Overlay.Guide.Preview.Visible,"same row reopens hidden preview");main.Overlay.Pinned=main.Catalog.BuildMeta.comps.First().name;main.Overlay.Guide.Collapsed=true;main.Overlay.Guide.RefreshRows();main.Overlay.Guide.Sync();main.Overlay.Follow(fixture.Handle,true);Application.DoEvents();check(main.Overlay.Height==204,"collapsed height reserves pinned cards");check(main.Overlay.Guide.Controls.OfType<PinnedShelf>().Single().Visible,"pinned cards remain visible");main.Overlay.Follow(fixture.Handle,false);check(!main.Overlay.Guide.Preview.Visible,"background hides preview");fixture.Close();}
 return n;
 }
}
}
