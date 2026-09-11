using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace TavernLens {
public class PinnedShelf : Control {
 string[] Cards {get{var c=main.Catalog.BuildMeta.comps.FirstOrDefault(x=>x.name==overlay.Pinned);return (c==null?new string[0]:c.core.ToArray()).Concat(main.Settings.PinnedCards).Distinct().Take(7).ToArray();}}
 readonly Main main;readonly Overlay overlay;readonly TwinPreview preview;int hovered=-1;
 public PinnedShelf(Main m,Overlay o,TwinPreview p){main=m;overlay=o;preview=p;Height=150;DoubleBuffered=true;Cursor=Cursors.Hand;BackColor=Theme.Panel;MouseLeave+=(s,e)=>{hovered=-1;preview.Hide();};}
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);var comp=main.Catalog.BuildMeta.comps.FirstOrDefault(c=>c.name==overlay.Pinned);var ids=Cards;if(ids.Length==0)return;using(var f=Theme.Font(9,true))TextRenderer.DrawText(e.Graphics,"PINNED · "+(comp==null?"Watchlist":comp.name),f,new Rectangle(10,6,Width-43,25),Theme.Gold,TextFormatFlags.EndEllipsis);using(var f=Theme.Font(12))TextRenderer.DrawText(e.Graphics,"×",f,new Rectangle(Width-29,5,24,25),Theme.Muted);int w=(Width-16)/Math.Max(1,ids.Length);for(int i=0;i<ids.Length;i++){var art=ArtCache.Get(ids[i],main.Catalog.Cards.build);if(art!=null)e.Graphics.DrawImage(art,CardTile.Fit(art,new Rectangle(8+i*w,32,w-3,112)));}}
 protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);var ids=Cards;if(ids.Length==0)return;int i=e.Y<32?-1:(e.X-8)/Math.Max(1,(Width-16)/ids.Length);if(i==hovered)return;hovered=i;if(i>=0&&i<ids.Length)preview.Display(ids[i],null,this,0);else preview.Hide();}
 protected override void OnMouseClick(MouseEventArgs e){base.OnMouseClick(e);if(e.Y<32&&e.X>Width-34){overlay.Pinned="";main.Settings.PinnedCards.Clear();main.SavePreferences();overlay.Guide.Sync();overlay.RefreshLayout();}else{if(overlay.Guide.Collapsed)overlay.Guide.Toggle();if(String.IsNullOrEmpty(overlay.Pinned))overlay.Guide.SelectMode(3);else overlay.Guide.OpenBuild(overlay.Pinned);}}
}
}

