using System;using System.Drawing;using System.Drawing.Imaging;using System.IO;using System.Runtime.InteropServices;
namespace TavernLens {
// Match only the supplied crown/shield artwork. Ambiguous or absent artwork stays unknown.
public static class CrownDetector {
 public static double[] Scores;static readonly int[][] patterns={Pattern("menu-solo.png",new Rectangle(37,0,164,151)),Pattern("menu-duos.png",new Rectangle(32,0,162,153))};
 static int[] Pattern(string name,Rectangle rect){try{using(var source=new Bitmap(Path.Combine(Store.Root,"assets",name)))using(var crop=source.Clone(rect,PixelFormat.Format24bppRgb))using(var sample=new Bitmap(crop,new Size(10,10))){var p=new int[300];for(int y=0;y<10;y++)for(int x=0;x<10;x++){var c=sample.GetPixel(x,y);int at=(y*10+x)*3;p[at]=c.R;p[at+1]=c.G;p[at+2]=c.B;}return p;}}catch{return null;}}
 static byte[] Pixels(Bitmap image,out int stride){var bits=image.LockBits(new Rectangle(0,0,image.Width,image.Height),ImageLockMode.ReadOnly,PixelFormat.Format24bppRgb);try{stride=bits.Stride;var data=new byte[stride*image.Height];Marshal.Copy(bits.Scan0,data,0,data.Length);return data;}finally{image.UnlockBits(bits);}}
 static double Score(byte[] data,int stride,int x,int y,int w,int h,int[] p){double score=0;for(int j=0;j<10;j++)for(int i=0;i<10;i++){int at=(y+(int)((j+.5)*h/10))*stride+(x+(int)((i+.5)*w/10))*3;int k=(j*10+i)*3;score+=Math.Abs(data[at+2]-p[k])+Math.Abs(data[at+1]-p[k+1])+Math.Abs(data[at]-p[k+2]);}return score/300;}
 public static string Detect(Bitmap original){if(patterns[0]==null||patterns[1]==null)return null;int width=Math.Min(800,original.Width),height=Math.Max(1,(int)((double)original.Height*width/original.Width));using(var frame=new Bitmap(width,height,PixelFormat.Format24bppRgb)){using(var g=Graphics.FromImage(frame))g.DrawImage(original,0,0,width,height);int stride;var data=Pixels(frame,out stride);double[] best={255,255};for(int kind=0;kind<2;kind++){int bx=0,by=0,bw=0;int max=Math.Min(220,Math.Min(width,height));for(int w=24;w<=max;w+=Math.Max(2,w/12)){int h=(int)(w*.927);for(int y=0;y+h<=height;y+=4)for(int x=0;x+w<=width;x+=4){double score=Score(data,stride,x,y,w,h,patterns[kind]);if(score<best[kind]){best[kind]=score;bx=x;by=y;bw=w;}}}if(bw==0)continue;int ox=bx,oy=by,ow=bw;for(int w=Math.Max(16,ow-4);w<=ow+4;w++){int h=(int)(w*.927);for(int y=Math.Max(0,oy-4);y<=Math.Min(height-h,oy+4);y++)for(int x=Math.Max(0,ox-4);x<=Math.Min(width-w,ox+4);x++)best[kind]=Math.Min(best[kind],Score(data,stride,x,y,w,h,patterns[kind]));}}
 Scores=best;int winner=best[0]<best[1]?0:1;return best[winner]<29&&best[1-winner]-best[winner]>2.5?(winner==0?"Solo":"Duos"):null;}}
}
}


