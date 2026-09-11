using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace TavernLens {
public static class Design {
 public static Panel MountDeck(TabControl tabs){var host=new Panel{Dock=DockStyle.Fill,BackColor=Theme.Bg};tabs.Dock=DockStyle.None;tabs.Appearance=TabAppearance.Normal;tabs.SizeMode=TabSizeMode.Fixed;tabs.ItemSize=new Size(1,1);host.Controls.Add(tabs);host.Resize+=(s,e)=>tabs.SetBounds(-5,-8,host.Width+10,host.Height+13);tabs.SetBounds(-5,-8,host.Width+10,host.Height+13);return host;}
 public static readonly Color Border=Color.FromArgb(48,53,67),Accent=Color.FromArgb(222,182,116);
 public static GraphicsPath Round(Rectangle r,int radius){int d=radius*2;var p=new GraphicsPath();p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
 public static void Surface(Graphics g,Rectangle r,Color fill,Color border,int radius=9){g.SmoothingMode=SmoothingMode.AntiAlias;using(var path=Round(r,radius))using(var b=new SolidBrush(fill))using(var p=new Pen(border)){g.FillPath(b,path);g.DrawPath(p,path);}}
 public static void StyleTree(Control parent){foreach(Control c in parent.Controls){var combo=c as ComboBox;if(combo!=null){combo.FlatStyle=FlatStyle.Flat;combo.BackColor=Theme.Panel;combo.ForeColor=Theme.Ink;combo.DrawMode=DrawMode.OwnerDrawFixed;combo.ItemHeight=26;combo.DrawItem+=(s,e)=>{if(e.Index<0)return;using(var b=new SolidBrush((e.State&DrawItemState.Selected)!=0?Color.FromArgb(57,56,66):Theme.Panel))e.Graphics.FillRectangle(b,e.Bounds);TextRenderer.DrawText(e.Graphics,combo.GetItemText(combo.Items[e.Index]),combo.Font,new Rectangle(e.Bounds.X+9,e.Bounds.Y,e.Bounds.Width-12,e.Bounds.Height),Theme.Ink,TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);};}StyleTree(c);}}
 public static void Tabs(TabControl tabs){tabs.DrawMode=TabDrawMode.OwnerDrawFixed;tabs.SizeMode=TabSizeMode.Fixed;tabs.ItemSize=new Size(175,43);tabs.DrawItem+=(s,e)=>{bool active=e.Index==tabs.SelectedIndex;using(var b=new SolidBrush(Theme.Bg))e.Graphics.FillRectangle(b,e.Bounds);if(active)using(var b=new SolidBrush(Accent))e.Graphics.FillRectangle(b,e.Bounds.Left+20,e.Bounds.Bottom-3,e.Bounds.Width-40,3);using(var f=Theme.Font(10,active))TextRenderer.DrawText(e.Graphics,tabs.TabPages[e.Index].Text,f,e.Bounds,active?Theme.Ink:Theme.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);};}
}
public class DarkCombo : ComboBox {
 public DarkCombo(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);SelectedIndexChanged+=(s,e)=>Invalidate();}
 protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(Theme.Panel);Design.Surface(e.Graphics,new Rectangle(1,1,Width-3,Height-3),Theme.Panel,Design.Border,6);TextRenderer.DrawText(e.Graphics,SelectedItem==null?"Select…":GetItemText(SelectedItem),Font,new Rectangle(10,0,Width-36,Height),Theme.Ink,TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);using(var arrow=new Pen(Theme.Gold,1.5f)){e.Graphics.DrawLine(arrow,Width-18,Height/2-2,Width-14,Height/2+2);e.Graphics.DrawLine(arrow,Width-14,Height/2+2,Width-10,Height/2-2);}}
}
public class CorePreview : Control {
 public Comp Comp;readonly Catalog catalog;
 public CorePreview(Catalog c){catalog=c;Dock=DockStyle.Top;Height=182;BackColor=Theme.Panel;DoubleBuffered=true;}
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);if(Comp==null)return;using(var f=Theme.Font(8,true))TextRenderer.DrawText(e.Graphics,"THE CORE  /  HOVER THE BUILD GUIDE FOR CARD DETAILS",f,new Point(17,10),Theme.Gold);int count=Math.Min(7,Comp.core.Count);int w=Math.Min(105,(Width-28)/Math.Max(1,count));for(int i=0;i<count;i++){var art=ArtCache.Get(Comp.core[i],catalog.Cards.build);if(art!=null)e.Graphics.DrawImage(art,CardTile.Fit(art,new Rectangle(14+i*w,32,w,140)));}using(var pen=new Pen(Design.Border))e.Graphics.DrawLine(pen,16,Height-1,Width-16,Height-1);}
}
public class SoftButton : Button {
 bool hover;public bool Active;public bool Navigation;
 public SoftButton(){FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;UseVisualStyleBackColor=false;SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);Cursor=Cursors.Hand;MouseEnter+=(s,e)=>{hover=true;Invalidate();};MouseLeave+=(s,e)=>{hover=false;Invalidate();};}
 protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(Parent==null?Theme.Panel:Parent.BackColor);var r=new Rectangle(1,1,Width-3,Height-3);Color fill=Active?Color.FromArgb(63,53,40):hover?Color.FromArgb(45,49,62):Theme.Panel;Design.Surface(e.Graphics,r,fill,Active?Color.FromArgb(105,87,57):hover?Color.FromArgb(68,77,93):fill,7);if(Navigation&&Active)using(var accent=new SolidBrush(Theme.Gold))e.Graphics.FillRectangle(accent,2,12,3,Height-24);using(var f=Theme.Font(9.5f,Active))TextRenderer.DrawText(e.Graphics,Text,f,new Rectangle(Navigation?15:5,0,Width-(Navigation?20:10),Height),Enabled?(Active?Theme.Gold:Theme.Ink):Theme.Muted,TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|(Navigation?TextFormatFlags.Left:TextFormatFlags.HorizontalCenter));if(Focused&&ShowFocusCues)using(var p=new Pen(Theme.Gold))using(var shape=Design.Round(new Rectangle(3,3,Width-7,Height-7),6))e.Graphics.DrawPath(p,shape);}
}
public class NavigationRail : Panel {
 readonly TabControl pages;
 public NavigationRail(TabControl target){pages=target;Dock=DockStyle.Left;Width=194;BackColor=Theme.Bg;Padding=new Padding(14,22,14,0);var label=Theme.Label("WORKSPACE",18,19,160,22,8,Theme.Muted);Controls.Add(label);for(int i=0;i<pages.TabCount;i++){int index=i;var b=new SoftButton{Text=pages.TabPages[i].Text,Navigation=true,Bounds=new Rectangle(14,54+i*53,165,43),Active=i==0};b.Click+=(s,e)=>pages.SelectedIndex=index;Controls.Add(b);}pages.SelectedIndexChanged+=(s,e)=>{foreach(Control c in Controls){var b=c as SoftButton;if(b!=null){b.Active=b.Text==pages.SelectedTab.Text;b.Invalidate();}}};Controls.Add(Theme.Label("BATTLEGROUNDS\n\nYour boards. Your progress.\nStored on this PC.",20,410,156,92,9,Theme.Muted));}
 protected override void OnPaintBackground(PaintEventArgs e){BrandAssets.Paint(e.Graphics,ClientRectangle,175);}
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);using(var p=new Pen(Design.Border))e.Graphics.DrawLine(p,Width-1,16,Width-1,Height-16);}
}
public class BrandHeader : Panel {
 public BrandHeader(){Dock=DockStyle.Top;Height=114;BackColor=Theme.Bg;DoubleBuffered=true;}
 protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;BrandAssets.Paint(g,ClientRectangle,125);if(BrandAssets.Logo!=null)g.DrawImage(BrandAssets.Logo,CardTile.Fit(BrandAssets.Logo,new Rectangle(18,15,65,77)));using(var p=new Pen(Color.FromArgb(95,Theme.Gold)))g.DrawLine(p,24,Height-1,Width-24,Height-1);}
}
}
