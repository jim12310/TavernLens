using System;using System.Collections.Generic;using System.Drawing;using System.Windows.Forms;using System.Runtime.InteropServices;
namespace TavernLens {
public class PanelOffset {public int X,Y;}
public sealed class OverlayDrag : IMessageFilter {
 [DllImport("user32.dll")] static extern short GetAsyncKeyState(int key);
 static readonly OverlayDrag instance=new OverlayDrag();readonly Timer timer=new Timer();readonly List<Floating> windows=new List<Floating>();readonly Dictionary<string,int> counts=new Dictionary<string,int>();readonly Dictionary<string,PanelOffset> positions=Store.Read("overlay-layout.json",new Dictionary<string,PanelOffset>());Floating moving;Point start,origin;
 public static bool Alt {get{return (GetAsyncKeyState(0x12)&0x8000)!=0;}}
 OverlayDrag(){Application.AddMessageFilter(this);timer.Interval=25;timer.Tick+=(s,e)=>Tick();timer.Start();}
 public static void Register(Floating f){instance.windows.Add(f);string type=f.GetType().Name;int count;instance.counts.TryGetValue(type,out count);instance.counts[type]=count+1;f.LayoutKey=type+"-"+count;PanelOffset p;if(instance.positions.TryGetValue(f.LayoutKey,out p))f.LayoutOffset=new Point(p.X,p.Y);f.Disposed+=(s,e)=>instance.windows.Remove(f);}
 void Tick(){bool alt=Alt;foreach(var f in windows.ToArray())if(!f.IsDisposed&&f.IsHandleCreated&&f.Visible)f.ApplyPassThrough(alt?false:f.WantsPassThrough);if(moving==null)return;if(moving.IsDisposed||!moving.Visible){moving=null;return;}if((GetAsyncKeyState(1)&0x8000)==0||!alt){Finish();return;}var mouse=Cursor.Position;var target=new Point(origin.X+mouse.X-start.X,origin.Y+mouse.Y-start.Y);var area=Screen.FromPoint(mouse).WorkingArea;target.X=Math.Max(area.Left,Math.Min(target.X,area.Right-moving.Width));target.Y=Math.Max(area.Top,Math.Min(target.Y,area.Bottom-moving.Height));var basis=moving.LayoutBase;moving.LayoutOffset=new Point(target.X-basis.X,target.Y-basis.Y);moving.Location=target;}
 void Finish(){if(moving==null)return;moving.Capture=false;var offset=moving.LayoutOffset;positions[moving.LayoutKey]=new PanelOffset{X=offset.X,Y=offset.Y};try{Store.Write("overlay-layout.json",positions);}catch{}moving=null;}
 public bool PreFilterMessage(ref Message m){if(m.Msg==0x201&&Alt){var control=Control.FromChildHandle(m.HWnd);var f=control==null?null:control.FindForm() as Floating;if(f==null)return false;moving=f;start=Cursor.Position;origin=f.Location;f.Capture=true;return true;}if(moving!=null&&(m.Msg==0x200||m.Msg==0x202)){if(m.Msg==0x202)Finish();return true;}return false;}
}
}

