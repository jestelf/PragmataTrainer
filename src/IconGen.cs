using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

static class IconGen {
 static readonly Color Back=Color.FromArgb(10,13,18),Ring=Color.FromArgb(98,204,242),Moon=Color.FromArgb(236,242,248),Grid=Color.FromArgb(98,204,242);
 static Bitmap Draw(int s){
  var bmp=new Bitmap(s,s,PixelFormat.Format32bppArgb);
  using(var g=Graphics.FromImage(bmp)){
   g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Color.Transparent);
   float r=s*0.18f;var box=new RectangleF(s*0.02f,s*0.02f,s*0.96f,s*0.96f);
   using(var path=Rounded(box,r))using(var bg=new LinearGradientBrush(box,Color.FromArgb(26,36,48),Back,90f))g.FillPath(bg,path);
   float w=Math.Max(1.5f,s*0.055f);var ring=new RectangleF(s*0.13f,s*0.13f,s*0.74f,s*0.74f);
   using(var pen=new Pen(Ring,w))g.DrawEllipse(pen,ring);

   var moon=new RectangleF(s*0.27f,s*0.27f,s*0.46f,s*0.46f);var cut=new RectangleF(s*0.38f,s*0.21f,s*0.42f,s*0.42f);
   using(var mp=new GraphicsPath()){mp.AddEllipse(moon);using(var region=new Region(mp)){using(var cp=new GraphicsPath()){cp.AddEllipse(cut);region.Exclude(cp);}using(var br=new SolidBrush(Moon))g.FillRegion(br,region);}}

   if(s>=32){float c=Math.Max(1f,s*0.045f);using(var br=new SolidBrush(Grid))for(int i=0;i<3;i++)for(int j=0;j<3;j++){if((i+j)%2==1)continue;g.FillRectangle(br,s*0.54f+i*s*0.075f,s*0.36f+j*s*0.075f,c,c);}}
  }
  return bmp;
 }
 static GraphicsPath Rounded(RectangleF b,float r){var p=new GraphicsPath();float d=r*2;p.AddArc(b.X,b.Y,d,d,180,90);p.AddArc(b.Right-d,b.Y,d,d,270,90);p.AddArc(b.Right-d,b.Bottom-d,d,d,0,90);p.AddArc(b.X,b.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
 static void Main(string[] args){
  string dir=args[0];int[] sizes={16,20,24,32,40,48,64,128,256};var images=new byte[sizes.Length][];
  for(int i=0;i<sizes.Length;i++)using(var b=Draw(sizes[i]))using(var ms=new MemoryStream()){b.Save(ms,ImageFormat.Png);images[i]=ms.ToArray();if(sizes[i]==256)File.WriteAllBytes(Path.Combine(dir,"pragmata.png"),images[i]);}
  using(var f=new BinaryWriter(File.Create(Path.Combine(dir,"pragmata.ico")))){
   f.Write((short)0);f.Write((short)1);f.Write((short)sizes.Length);int offset=6+16*sizes.Length;
   for(int i=0;i<sizes.Length;i++){f.Write((byte)(sizes[i]>=256?0:sizes[i]));f.Write((byte)(sizes[i]>=256?0:sizes[i]));f.Write((byte)0);f.Write((byte)0);f.Write((short)1);f.Write((short)32);f.Write(images[i].Length);f.Write(offset);offset+=images[i].Length;}
   foreach(var img in images)f.Write(img);
  }
 }
}
