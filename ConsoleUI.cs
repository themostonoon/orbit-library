using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace OrbitLibrary {
 public static class ConsoleArt {
  public static Bitmap Get(string id){return Artwork.Logo(id);}
  sealed class Scaled {public Bitmap Image;public long Used;}
  static Dictionary<string,Scaled> scaled=new Dictionary<string,Scaled>();static long clock,scaledBytes;
  public static void Fit(Graphics g,Image image,RectangleF area,float opacity){
   if(image==null||opacity<=0)return;float ratio=Math.Min(area.Width/image.Width,area.Height/image.Height);
   var transform=g.Transform;float sx=Math.Abs(transform.Elements[0]),sy=Math.Abs(transform.Elements[3]);transform.Dispose();if(sx<.01f)sx=1;if(sy<.01f)sy=1;
   int w=Math.Max(1,(int)Math.Round(image.Width*ratio*sx)),h=Math.Max(1,(int)Math.Round(image.Height*ratio*sy));string key=System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(image)+":"+w+":"+h;
   Scaled hit;if(!scaled.TryGetValue(key,out hit)){var bitmap=new Bitmap(w,h,PixelFormat.Format32bppPArgb);using(var graphics=Graphics.FromImage(bitmap)){graphics.CompositingMode=CompositingMode.SourceCopy;graphics.InterpolationMode=InterpolationMode.HighQualityBicubic;graphics.DrawImage(image,0,0,w,h);}hit=new Scaled{Image=bitmap,Used=++clock};scaled[key]=hit;scaledBytes+=(long)w*h*4;while(scaled.Count>96||scaledBytes>24*1024*1024){string oldest=scaled.OrderBy(x=>x.Value.Used).First().Key;var victim=scaled[oldest].Image;scaledBytes-=(long)victim.Width*victim.Height*4;victim.Dispose();scaled.Remove(oldest);}}
   hit.Used=++clock;var target=new RectangleF(area.X+(area.Width-w/sx)/2,area.Y+(area.Height-h/sy)/2,w/sx,h/sy);var mode=g.InterpolationMode;g.InterpolationMode=InterpolationMode.NearestNeighbor;
   if(opacity>=.999f)g.DrawImage(hit.Image,target,new RectangleF(0,0,w,h),GraphicsUnit.Pixel);
   else using(var attributes=new ImageAttributes()){var matrix=new ColorMatrix();matrix.Matrix33=opacity;attributes.SetColorMatrix(matrix);g.DrawImage(hit.Image,Rectangle.Round(target),0,0,w,h,GraphicsUnit.Pixel,attributes);}g.InterpolationMode=mode;
  }
  public static string ShortName(string id){switch(id){case "nes":return "NES";case "n64":return "Nintendo 64";case "snes":return "Super Nintendo";case "gc":return "GameCube";case "nds":return "Nintendo DS";case "3ds":return "Nintendo 3DS";case "megadrive":return "Genesis";case "mastersystem":return "Master System";case "gamegear":return "Game Gear";case "segacd":return "Sega CD";case "sega32x":return "Sega 32X";case "xbox360":return "Xbox 360";case "atari2600":return "Atari 2600";case "psx":return "PlayStation";case "ps2":return "PlayStation 2";case "ps3":return "PlayStation 3";case "psp":return "PSP";default:return Catalog.Find(id).Name.Replace("Nintendo ","").Replace("Sega ","");}}
 }
 public class LivingBackground : Panel {
  Stopwatch watch=Stopwatch.StartNew();public double FixedTime=-1;public bool Motion=true;SceneSurface frameSurface,baseSurface;Bitmap cached,backdrop;Bitmap[] planets;Rectangle[] previous;double lastFrame=-1,nextFrame;
  public LivingBackground(){DoubleBuffered=false;BackColor=Style.Background;SetStyle(ControlStyles.AllPaintingInWmPaint|ControlStyles.UserPaint|ControlStyles.ResizeRedraw,true);SetStyle(ControlStyles.OptimizedDoubleBuffer,false);}
  void Ensure(){if(Width<1||Height<1)return;if(cached!=null&&cached.Width==Width&&cached.Height==Height)return;Release();baseSurface=new SceneSurface(Width,Height);frameSurface=new SceneSurface(Width,Height);backdrop=baseSurface.Bitmap;cached=frameSurface.Bitmap;using(var g=Graphics.FromImage(backdrop))DrawBaseScene(g,ClientRectangle);frameSurface.Copy(baseSurface,ClientRectangle);planets=new Bitmap[5];previous=new Rectangle[81];for(int i=0;i<5;i++){planets[i]=new Bitmap(220,160,PixelFormat.Format32bppPArgb);using(var g=Graphics.FromImage(planets[i]))DrawPlanet(g,i,110,80);}PaintFrame(0,false);}
  void Release(){if(frameSurface!=null)frameSurface.Dispose();if(baseSurface!=null)baseSurface.Dispose();frameSurface=baseSurface=null;if(planets!=null)foreach(var planet in planets)planet.Dispose();cached=backdrop=null;planets=null;}
  public void DrawBase(Graphics g,Rectangle area){Ensure();if(baseSurface!=null)baseSurface.DrawTo(g,new Rectangle(0,0,area.Width,area.Height),area.Location);}
  public void Advance(){if(!Motion||!Visible)return;double t=FixedTime>=0?FixedTime:watch.Elapsed.TotalSeconds;if(t<lastFrame){lastFrame=-1;nextFrame=t;}if(lastFrame>=0&&t<nextFrame)return;Ensure();PaintFrame(t,true);lastFrame=t;nextFrame=Math.Max(nextFrame+1.0/30,t-.1);}
  void PaintFrame(double time,bool invalidate){if(cached==null)return;var next=new Rectangle[81];using(var region=new Region()){region.MakeEmpty();foreach(var rect in previous)if(rect.Width>0){var clipped=Rectangle.Intersect(rect,ClientRectangle);frameSurface.Copy(baseSurface,clipped);region.Union(rect);}using(var g=Graphics.FromImage(cached)){
    for(int i=0;i<5;i++){int x=(int)(Width*(.16f+i*.19f)+Math.Sin(time*.035+i)*16),y=(int)(Height*(.2f+(i%3)*.29f)+Math.Cos(time*.045+i)*12);next[i]=new Rectangle(x-110,y-80,220,160);g.DrawImageUnscaled(planets[i],x-110,y-80);region.Union(next[i]);}
    g.SmoothingMode=SmoothingMode.AntiAlias;for(int i=0;i<76;i++){float x=(i*173%Math.Max(1,Width))+(float)Math.Sin(time*.023+i)*4,y=(i*97%Math.Max(1,Height))+(float)Math.Cos(time*.028+i)*4;int alpha=(int)(29+12*Math.Sin(time*.35+i));float size=i%9==0?2.3f:1.3f;next[i+5]=new Rectangle((int)x-5,(int)y-5,12,12);using(var brush=new SolidBrush(Color.FromArgb(alpha,170,186,218)))g.FillEllipse(brush,x,y,size,size);if(i%19==0)using(var pen=new Pen(Color.FromArgb(alpha/2,170,186,218),.7f)){g.DrawLine(pen,x-3,y+.6f,x+4,y+.6f);g.DrawLine(pen,x+.6f,y-3,x+.6f,y+4);}region.Union(next[i+5]);}
   }previous=next;if(invalidate){foreach(Control child in Controls)if(child.Visible)region.Exclude(child.Bounds);Invalidate(region,false);}}
  }
  protected override void OnPaintBackground(PaintEventArgs e){Ensure();if(frameSurface!=null)frameSurface.DrawTo(e.Graphics,e.ClipRectangle,e.ClipRectangle.Location);}
  protected override void Dispose(bool disposing){if(disposing)Release();base.Dispose(disposing);}
  static void DrawBaseScene(Graphics g,Rectangle bounds){using(var gradient=new LinearGradientBrush(bounds,Color.FromArgb(7,9,15),Color.FromArgb(13,13,23),25f))g.FillRectangle(gradient,bounds);float w=bounds.Width,h=bounds.Height;using(var glow=new GraphicsPath()){glow.AddEllipse(w*.45f,h*.03f,w*.72f,h*1.1f);using(var brush=new PathGradientBrush(glow)){brush.CenterColor=Color.FromArgb(27,42,56,96);brush.SurroundColors=new[]{Color.FromArgb(0,7,9,15)};g.FillPath(brush,glow);}}}
  static void DrawPlanet(Graphics g,int i,float x,float y){g.SmoothingMode=SmoothingMode.AntiAlias;float radius=18+(i%3)*22;using(var path=new GraphicsPath()){path.AddEllipse(x-radius,y-radius,radius*2,radius*2);using(var fill=new LinearGradientBrush(new RectangleF(x-radius,y-radius,radius*2,radius*2),Color.FromArgb(35,31,41,66),Color.FromArgb(5,7,9,15),35))g.FillPath(fill,path);using(var pen=new Pen(Color.FromArgb(27,116,135,172),1))g.DrawArc(pen,x-radius,y-radius,radius*2,radius*2,205,160);}if(i==3){var saved=g.Save();g.TranslateTransform(x,y);g.RotateTransform(-24);using(var pen=new Pen(Color.FromArgb(32,138,150,184),1))g.DrawEllipse(pen,-radius*1.7f,-radius*.37f,radius*3.4f,radius*.74f);g.Restore(saved);}}
  public static void Draw(Graphics g,Rectangle bounds,double time){g.SmoothingMode=SmoothingMode.AntiAlias;g.InterpolationMode=InterpolationMode.HighQualityBicubic;using(var gradient=new LinearGradientBrush(bounds,Color.FromArgb(7,9,15),Color.FromArgb(13,13,23),25f))g.FillRectangle(gradient,bounds);
   float w=bounds.Width,h=bounds.Height;using(var glow=new GraphicsPath()){glow.AddEllipse(w*.45f,h*.03f,w*.72f,h*1.1f);using(var brush=new PathGradientBrush(glow)){brush.CenterColor=Color.FromArgb(27,42,56,96);brush.SurroundColors=new[]{Color.FromArgb(0,7,9,15)};g.FillPath(brush,glow);}}
   for(int i=0;i<5;i++){float x=w*(.16f+i*.19f)+(float)Math.Sin(time*.035+i)*16;float y=h*(.2f+(i%3)*.29f)+(float)Math.Cos(time*.045+i)*12;float radius=18+(i%3)*22;Color edge=Color.FromArgb(27,116,135,172);using(var path=new GraphicsPath()){path.AddEllipse(x-radius,y-radius,radius*2,radius*2);using(var fill=new LinearGradientBrush(new RectangleF(x-radius,y-radius,radius*2,radius*2),Color.FromArgb(35,31,41,66),Color.FromArgb(5,7,9,15),35))g.FillPath(fill,path);using(var pen=new Pen(edge,1))g.DrawArc(pen,x-radius,y-radius,radius*2,radius*2,205,160);}if(i==3){var saved=g.Save();g.TranslateTransform(x,y);g.RotateTransform(-24);using(var pen=new Pen(Color.FromArgb(32,138,150,184),1))g.DrawEllipse(pen,-radius*1.7f,-radius*.37f,radius*3.4f,radius*.74f);g.Restore(saved);}}
   for(int i=0;i<76;i++){float x=(i*173%Math.Max(1,bounds.Width))+(float)Math.Sin(time*.023+i)*4,y=(i*97%Math.Max(1,bounds.Height))+(float)Math.Cos(time*.028+i)*4;int alpha=(int)(29+12*Math.Sin(time*.35+i));float size=i%9==0?2.3f:1.3f;using(var brush=new SolidBrush(Color.FromArgb(alpha,170,186,218)))g.FillEllipse(brush,x,y,size,size);if(i%19==0)using(var pen=new Pen(Color.FromArgb(alpha/2,170,186,218),.7f)){g.DrawLine(pen,x-3,y+.6f,x+4,y+.6f);g.DrawLine(pen,x+.6f,y-3,x+.6f,y+4);}}
  }
 }
 public class ConsoleRail : SceneControl {
  public int SelectedIndex;public Action<int> Changed;public float Zoom=1;float offset,target;Stopwatch motion=Stopwatch.StartNew();double last;bool hovered;int hoverIndex=-1;
  public ConsoleRail(){DoubleBuffered=true;Cursor=Cursors.Hand;Height=123;}
  public void Advance(){double now=motion.Elapsed.TotalSeconds;double dt=last==0?.016:Math.Min(.1,now-last);last=now;float old=offset;offset+=(target-offset)*(float)(1-Math.Exp(-18*dt));if(Math.Abs(target-offset)<.2)offset=target;if(old!=offset)Invalidate();}
  public void SetSelected(int index){SelectedIndex=index;target=Math.Max(0,index*160-Width/Zoom*.35f);target=Math.Min(target,Math.Max(0,Catalog.All.Count*160-Width/Zoom+22));Invalidate();}
  protected override void OnMouseMove(MouseEventArgs e){hovered=true;int next=(int)((e.X/Zoom+offset)/160);if(next!=hoverIndex){hoverIndex=next;Invalidate();}base.OnMouseMove(e);}
  protected override void OnMouseLeave(EventArgs e){hovered=false;Invalidate();base.OnMouseLeave(e);}
  protected override void OnMouseDown(MouseEventArgs e){int i=(int)((e.X/Zoom+offset)/160);if(i>=0&&i<Catalog.All.Count&&Changed!=null)Changed(i);base.OnMouseDown(e);}
  protected override void OnMouseWheel(MouseEventArgs e){target=Math.Max(0,Math.Min(Math.Max(0,Catalog.All.Count*160-Width/Zoom+22),target-e.Delta*.8f));base.OnMouseWheel(e);}
  protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.ScaleTransform(Zoom,Zoom);for(int i=0;i<Catalog.All.Count;i++){float x=i*160-offset+3;if(x< -160||x>Width/Zoom)continue;bool active=i==SelectedIndex;var r=new RectangleF(x,active?2:10,146,active?92:78);using(var path=Style.Round(r,12)){using(var brush=new SolidBrush(Color.FromArgb(active?130:48,20,24,36)))g.FillPath(brush,path);if(active||hovered&&i==hoverIndex)using(var pen=new Pen(active?Color.FromArgb(224,239,255):Color.FromArgb(110,155,197),active?2:1))g.DrawPath(pen,path);}var art=ConsoleArt.Get(Catalog.All[i].Id);if(art!=null)ConsoleArt.Fit(g,art,new RectangleF(r.X+9,r.Y+12,r.Width-18,r.Height-24),1);else Style.Icon(g,Style.IconFor(Catalog.All[i].Name),new RectangleF(r.X+55,r.Y+23,36,36),Style.Muted);using(var f=new Font("Segoe UI",8.5f,active?FontStyle.Bold:FontStyle.Regular))using(var brush=new SolidBrush(active?Color.White:Style.Muted))using(var format=new StringFormat{Alignment=StringAlignment.Center,Trimming=StringTrimming.EllipsisCharacter})g.DrawString(ConsoleArt.ShortName(Catalog.All[i].Id),f,brush,new RectangleF(x-3,99,154,22),format);}}
 }
 public class Spotlight : SceneControl {
  public Game Game;public string SystemId="nes";public int GameCount;public bool Configured;Image cover;float progress=1;string previousPath="";Stopwatch transition=Stopwatch.StartNew();
  public Spotlight(){DoubleBuffered=true;}
  public void SetGame(Game game,string system,int count,bool configured){string path=game==null?system:game.Path;if(path!=previousPath){previousPath=path;progress=0;transition.Restart();}Game=game;SystemId=system;GameCount=count;Configured=configured;Invalidate();}
  public void Advance(){if(progress<1){progress=Math.Min(1,transition.ElapsedMilliseconds/340f);Invalidate();}}
  protected override void Dispose(bool disposing){base.Dispose(disposing);}
  protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.InterpolationMode=InterpolationMode.HighQualityBicubic;int shift=(int)((1-progress)*18);int artWidth=Math.Min(Width/2,Width>1400?590:500);var artArea=new RectangleF(Width-artWidth-30,10,artWidth,Height-24);
   cover=Game==null?null:Artwork.GameImage(Game,1000);if(cover!=null){using(var path=Style.Round(artArea,18)){var saved=g.Save();g.SetClip(path);ConsoleArt.Fit(g,cover,artArea,progress);g.Restore(saved);}}else{var art=ConsoleArt.Get(SystemId);if(art!=null)ConsoleArt.Fit(g,art,artArea,progress);else Style.Logo(g,new PointF(Width-artWidth/2-30,Height/2),60,.8,progress);}
   string system=Catalog.Find(SystemId).Name;using(var f=new Font("Segoe UI",9,FontStyle.Bold))TextRenderer.DrawText(g,system.ToUpperInvariant(),f,new Rectangle(4+shift,26,Width/2,26),Color.FromArgb(143,198,238),TextFormatFlags.Left|TextFormatFlags.EndEllipsis);
   using(var f=new Font("Segoe UI",Width>1400?47:35,FontStyle.Regular))TextRenderer.DrawText(g,Game==null?"Your next adventure":Game.Title,f,new Rectangle(0+shift,65,Math.Max(350,Width-artWidth-55),83),Color.White,TextFormatFlags.Left|TextFormatFlags.WordBreak|TextFormatFlags.EndEllipsis);
   string description=Game==null?"Connect your emulator and add games to start playing.":(Configured?"Ready to play. Pick up where you left off.":"Set up your emulator to launch this game.");using(var f=new Font("Segoe UI",11))TextRenderer.DrawText(g,description,f,new Rectangle(4+shift,Math.Min(174,Height-72),Math.Max(350,Width-artWidth-55),54),Color.FromArgb(165,182,204),TextFormatFlags.Left|TextFormatFlags.WordBreak);
   using(var f=new Font("Segoe UI",9))TextRenderer.DrawText(g,GameCount+" games in this collection"+(Game!=null&&Game.Favorite?"   /   Favorite":""),f,new Rectangle(4+shift,Math.Min(232,Height-28),Width/2,24),Style.Muted,TextFormatFlags.Left);
  }
 }
 public static class WindowFocus {
  [DllImport("user32.dll")]static extern bool SetForegroundWindow(IntPtr hwnd);
  public static void Front(Form form){form.TopMost=true;form.Show();form.Activate();SetForegroundWindow(form.Handle);var release=new Timer{Interval=250};release.Tick+=(s,e)=>{release.Stop();release.Dispose();if(!form.IsDisposed)form.TopMost=false;};release.Start();}
 }
}







