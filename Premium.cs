using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace OrbitLibrary {
 public static class Style {
  public static Color Background=Color.FromArgb(7,9,15), Mint=Color.FromArgb(115,236,206), Violet=Color.FromArgb(145,121,247), Muted=Color.FromArgb(144,158,181);
  public static GraphicsPath Round(RectangleF r,float radius) {float d=radius*2;var p=new GraphicsPath();p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
  public static Color Mix(Color a,Color b,float t){t=Math.Max(0,Math.Min(1,t));return Color.FromArgb((int)(a.R+(b.R-a.R)*t),(int)(a.G+(b.G-a.G)*t),(int)(a.B+(b.B-a.B)*t));}
  public static string IconFor(string text){text=text.ToLowerInvariant();if(text.Contains("unfavorite")||text.Contains("favorite"))return "heart";if(text.Contains("recent"))return "clock";if(text.Contains("scan"))return "scan";if(text.Contains("setup")||text.Contains("settings"))return "settings";if(text.Contains("browse")||text.Contains("import")||text.Contains("folder"))return "folder";if(text.Contains("save"))return "save";if(text.Contains("cover"))return "image";if(text.Contains("remove"))return "remove";if(text.Contains("add"))return "add";if(text.Contains("fullscreen"))return "fullscreen";if(text.Contains("help"))return "help";if(text.Contains("play "))return "play";if(text.Contains("more"))return "more";if(text.Contains("boy")||text.Contains("ds")||text.Contains("portable")||text.Contains("gear")||text.Contains("psp"))return "handheld";if(text.Contains("arcade"))return "arcade";if(text.Contains("dos"))return "computer";return "gamepad";}
  public static Bitmap IconImage(string name,Color color){var b=new Bitmap(24,24);using(var g=Graphics.FromImage(b))Icon(g,name,new RectangleF(2,2,20,20),color);return b;}
  public static void Icon(Graphics g,string name,RectangleF bounds,Color color) {
   var state=g.Save();g.TranslateTransform(bounds.X,bounds.Y);g.ScaleTransform(bounds.Width/24,bounds.Height/24);g.SmoothingMode=SmoothingMode.AntiAlias;
   using(var p=new Pen(color,1.7f){StartCap=LineCap.Round,EndCap=LineCap.Round,LineJoin=LineJoin.Round})using(var b=new SolidBrush(color)) {
    switch(name) {
     case "heart":using(var path=new GraphicsPath()){path.AddBezier(12,21,9,18,2,13,2,7);path.AddBezier(2,7,2,1,10,1,12,6);path.AddBezier(12,6,14,1,22,1,22,7);path.AddBezier(22,7,22,13,15,18,12,21);g.DrawPath(p,path);}break;
     case "clock":g.DrawEllipse(p,3,3,18,18);g.DrawLines(p,new[]{new PointF(12,6),new PointF(12,12),new PointF(16,14)});break;
     case "play":g.DrawPolygon(p,new[]{new PointF(7,3),new PointF(21,12),new PointF(7,21)});break;
     case "search":g.DrawEllipse(p,3,3,12,12);g.DrawLine(p,14,14,21,21);break;
     case "scan":g.DrawArc(p,3,3,18,18,35,275);g.DrawLines(p,new[]{new PointF(20,3),new PointF(21,9),new PointF(15,8)});break;
     case "settings":g.DrawEllipse(p,6,6,12,12);g.DrawEllipse(p,10,10,4,4);for(int i=0;i<8;i++){double a=i*Math.PI/4;g.DrawLine(p,12+(float)Math.Cos(a)*8,12+(float)Math.Sin(a)*8,12+(float)Math.Cos(a)*10,12+(float)Math.Sin(a)*10);}break;
     case "folder":g.DrawPolygon(p,new[]{new PointF(2,6),new PointF(9,6),new PointF(11,9),new PointF(22,9),new PointF(20,20),new PointF(2,20)});g.DrawLine(p,2,11,21,11);break;
     case "save":g.DrawRectangle(p,3,3,18,18);g.DrawRectangle(p,7,3,10,6);g.DrawRectangle(p,7,14,10,7);break;
     case "image":g.DrawRectangle(p,3,3,18,18);g.DrawEllipse(p,6,6,4,4);g.DrawLines(p,new[]{new PointF(4,18),new PointF(10,12),new PointF(14,16),new PointF(17,12),new PointF(20,17)});break;
     case "remove":g.DrawLine(p,5,6,19,6);g.DrawRectangle(p,9,3,6,3);g.DrawLines(p,new[]{new PointF(6,6),new PointF(7,21),new PointF(17,21),new PointF(18,6)});g.DrawLine(p,10,10,10,17);g.DrawLine(p,14,10,14,17);break;
     case "add":g.DrawLine(p,12,4,12,20);g.DrawLine(p,4,12,20,12);break;
     case "fullscreen":g.DrawLines(p,new[]{new PointF(3,9),new PointF(3,3),new PointF(9,3)});g.DrawLines(p,new[]{new PointF(15,3),new PointF(21,3),new PointF(21,9)});g.DrawLines(p,new[]{new PointF(21,15),new PointF(21,21),new PointF(15,21)});g.DrawLines(p,new[]{new PointF(9,21),new PointF(3,21),new PointF(3,15)});break;
     case "help":g.DrawEllipse(p,3,3,18,18);g.DrawArc(p,9,6,6,6,180,210);g.DrawLine(p,12,12,12,14);g.FillEllipse(b,11,17,2,2);break;
     case "more":g.FillEllipse(b,3,10,3,3);g.FillEllipse(b,10,10,3,3);g.FillEllipse(b,17,10,3,3);break;
     case "handheld":using(var r=Round(new RectangleF(6,1,12,22),3))g.DrawPath(p,r);g.DrawRectangle(p,9,5,6,7);g.DrawLine(p,9,17,12,17);g.DrawLine(p,10.5f,15.5f,10.5f,18.5f);g.FillEllipse(b,15,16,2,2);break;
     case "arcade":g.DrawPolygon(p,new[]{new PointF(6,2),new PointF(18,2),new PointF(18,12),new PointF(21,16),new PointF(19,22),new PointF(5,22),new PointF(3,16),new PointF(6,12)});g.DrawRectangle(p,8,5,8,6);g.DrawLine(p,6,16,18,16);g.DrawLine(p,9,12,9,16);break;
     case "computer":g.DrawRectangle(p,2,3,20,14);g.DrawLine(p,12,17,12,21);g.DrawLine(p,7,21,17,21);break;
     default:using(var r=Round(new RectangleF(2,6,20,13),4))g.DrawPath(p,r);g.DrawLine(p,6,12,12,12);g.DrawLine(p,9,9,9,15);g.FillEllipse(b,16,10,2,2);g.FillEllipse(b,19,13,2,2);break;
    }
   }g.Restore(state);
  }
  public static void Logo(Graphics g,PointF center,float radius,double time,float alpha) {
   g.SmoothingMode=SmoothingMode.AntiAlias;var r=new RectangleF(center.X-radius,center.Y-radius,radius*2,radius*2);
   for(int i=5;i>=1;i--)using(var p=new Pen(Color.FromArgb((int)(alpha*7),Mint),i*5))g.DrawEllipse(p,r);
   using(var p=new Pen(Color.FromArgb((int)(alpha*215),Mint),3.2f))g.DrawArc(p,r,(float)(time*35),280);
   using(var p=new Pen(Color.FromArgb((int)(alpha*150),Violet),1.4f))g.DrawEllipse(p,center.X-radius*1.45f,center.Y-radius*.65f,radius*2.9f,radius*1.3f);
   double angle=time*1.7;float x=center.X+(float)Math.Cos(angle)*radius,y=center.Y+(float)Math.Sin(angle)*radius;
   using(var b=new SolidBrush(Color.FromArgb((int)(alpha*255),Mint)))g.FillEllipse(b,x-6,y-6,12,12);
   using(var b=new SolidBrush(Color.FromArgb((int)(alpha*230),Color.White)))g.FillEllipse(b,center.X-6,center.Y-6,12,12);
  }
  [DllImport("dwmapi.dll")]static extern int DwmSetWindowAttribute(IntPtr hwnd,int attr,ref int value,int size);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern IntPtr SendMessage(IntPtr hwnd,int msg,IntPtr value,string text);
  [DllImport("uxtheme.dll",CharSet=CharSet.Unicode)]static extern int SetWindowTheme(IntPtr hwnd,string app,string id);
  public static void DarkScroll(Control control){try{SetWindowTheme(control.Handle,"DarkMode_Explorer",null);}catch{} }
  public static void SetCue(TextBox box,string text){SendMessage(box.Handle,0x1501,(IntPtr)1,text);}
  public static void DarkTitle(Form form){try{int one=1;DwmSetWindowAttribute(form.Handle,20,ref one,4);}catch{} }
 }
 public class PremiumButton : Button {
  Timer timer=new Timer{Interval=16};float hover;bool inside;
  public PremiumButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);FlatStyle=FlatStyle.Flat;Cursor=Cursors.Hand;timer.Tick+=(s,e)=>{float target=inside?1:0;hover+=(target-hover)*.22f;Invalidate();if(Math.Abs(target-hover)<.01){hover=target;timer.Stop();}};}
  protected override void OnMouseEnter(EventArgs e){inside=true;timer.Start();base.OnMouseEnter(e);}
  protected override void OnMouseLeave(EventArgs e){inside=false;timer.Start();base.OnMouseLeave(e);}
  protected override void Dispose(bool disposing){if(disposing)timer.Dispose();base.Dispose(disposing);}
  protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Parent==null?Style.Background:Parent.BackColor);bool primary=Text.StartsWith("Play")||Text.StartsWith("Save");Color fill=primary?Style.Mix(Color.FromArgb(28,44,66),Color.FromArgb(44,64,93),hover):Style.Mix(Color.FromArgb(20,24,34),Color.FromArgb(35,42,57),hover);using(var path=Style.Round(new RectangleF(1,1,Width-3,Height-3),9)){using(var b=new SolidBrush(fill))g.FillPath(b,path);using(var p=new Pen(primary?Color.FromArgb(75,103,137):Color.FromArgb(57,69,90),1))g.DrawPath(p,path);}Color color=Enabled?Color.FromArgb(228,238,244):Color.FromArgb(94,113,130);Style.Icon(g,Style.IconFor(Text),new RectangleF(12,Height/2-9,18,18),primary&&Enabled?Style.Mint:color);TextRenderer.DrawText(g,Text,Font,new Rectangle(37,0,Width-43,Height),color,TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);if(Focused)using(var p=new Pen(Style.Mint)){using(var path=Style.Round(new RectangleF(3,3,Width-7,Height-7),7))g.DrawPath(p,path);}}
 }
 public class BrandControl : SceneControl {
  public BrandControl(){SetStyle(ControlStyles.SupportsTransparentBackColor,true);DoubleBuffered=true;Size=new Size(220,90);}
  protected override void OnPaint(PaintEventArgs e){Style.Logo(e.Graphics,new PointF(26,30),15,.4,1);using(var f=new Font("Segoe UI",22,FontStyle.Bold))TextRenderer.DrawText(e.Graphics,"ORBIT",f,new Rectangle(61,8,150,44),Color.White);using(var f=new Font("Segoe UI",7.5f,FontStyle.Bold))TextRenderer.DrawText(e.Graphics,"A UNIVERSE OF PLAY",f,new Rectangle(62,51,150,20),Style.Muted);}
 }
 public class PremiumHeader : Panel {
  public PremiumHeader(){DoubleBuffered=true;BackColor=Style.Background;}
  protected override void OnPaintBackground(PaintEventArgs e){e.Graphics.Clear(BackColor);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;using(var path=Style.Round(new RectangleF(0,0,Width-3,130),18)){using(var b=new LinearGradientBrush(new Rectangle(0,0,Math.Max(1,Width),130),Color.FromArgb(26,35,50),Color.FromArgb(35,27,55),0f))g.FillPath(b,path);using(var p=new Pen(Color.FromArgb(52,60,78)))g.DrawPath(p,path);}Style.Logo(g,new PointF(Width-110,65),33,.8,.65f);using(var f=new Font("Segoe UI",8,FontStyle.Bold))TextRenderer.DrawText(g,"YOUR PERSONAL ARCADE",f,new Rectangle(22,14,340,20),Style.Mint,TextFormatFlags.Left);}
 }
 public class EmptyCollection : Control {
  public EmptyCollection(){Size=new Size(750,360);DoubleBuffered=true;}
  protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.Clear(Style.Background);g.SmoothingMode=SmoothingMode.AntiAlias;using(var path=Style.Round(new RectangleF(1,1,Width-3,Height-3),18)){using(var b=new SolidBrush(Color.FromArgb(19,25,38)))g.FillPath(b,path);using(var p=new Pen(Color.FromArgb(46,57,77)){DashStyle=DashStyle.Dash})g.DrawPath(p,path);}Style.Logo(g,new PointF(Width/2,85),32,1,1);using(var f=new Font("Segoe UI",23,FontStyle.Bold))TextRenderer.DrawText(g,"Your next adventure starts here",f,new Rectangle(20,145,Width-40,44),Color.White,TextFormatFlags.HorizontalCenter);using(var f=new Font("Segoe UI",11))TextRenderer.DrawText(g,Text,f,new Rectangle(40,204,Width-80,110),Style.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.WordBreak);}
 }
 public class Card : Control {
  public Game Game;public bool Active;Image cover;public event EventHandler Activated;float highlight,enter;bool hover;public int Stagger;Stopwatch age=Stopwatch.StartNew();
  public Card(Game game){Game=game;Size=new Size(194,246);Margin=new Padding(0,0,18,18);DoubleBuffered=true;Cursor=Cursors.Hand;string path=game.Cover;if(string.IsNullOrEmpty(path))foreach(string ext in new[]{".png",".jpg",".jpeg"}){string f=Path.ChangeExtension(game.Path,ext);if(File.Exists(f)){path=f;break;}}if(!string.IsNullOrEmpty(path)&&File.Exists(path))try{using(var img=Image.FromFile(path))cover=new Bitmap(img);}catch{} }
  public void Advance(){float next=(Active||hover)?1:0;float old=highlight;highlight+=(next-highlight)*.18f;float entrance=Math.Max(0,Math.Min(1,(age.ElapsedMilliseconds-Stagger*32)/320f));if(entrance!=enter||Math.Abs(old-highlight)>.002){enter=entrance;Invalidate();}}
  protected override void OnMouseEnter(EventArgs e){hover=true;base.OnMouseEnter(e);}
  protected override void OnMouseLeave(EventArgs e){hover=false;base.OnMouseLeave(e);}
  protected override void Dispose(bool disposing){if(disposing&&cover!=null)cover.Dispose();base.Dispose(disposing);}
  protected override void OnClick(EventArgs e){base.OnClick(e);if(Activated!=null)Activated(this,e);}
  protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Style.Background);float lift=highlight*4;var r=new RectangleF(3,7-lift,Width-7,Height-13);using(var path=Style.Round(r,13)){using(var b=new SolidBrush(Color.FromArgb(23,30,44)))g.FillPath(b,path);var saved=g.Save();g.SetClip(path);if(cover!=null){float scale=Math.Max(r.Width/cover.Width,169f/cover.Height)*(1+highlight*.025f);g.DrawImage(cover,r.X+(r.Width-cover.Width*scale)/2,r.Y+(169-cover.Height*scale)/2,cover.Width*scale,cover.Height*scale);}else{int seed=Math.Abs(Game.Title.GetHashCode()%100);Color a=Color.FromArgb(34+seed/5,48+seed/4,70+seed/3),b=Color.FromArgb(51+seed/5,31+seed/4,74+seed/3);using(var brush=new LinearGradientBrush(new RectangleF(r.X,r.Y,r.Width,169),a,b,45))g.FillRectangle(brush,r.X,r.Y,r.Width,169);for(int i=0;i<3;i++)using(var pen=new Pen(Color.FromArgb(27,Style.Mint),1))g.DrawEllipse(pen,r.X+20-i*26,r.Y+24-i*12,145+i*50,145+i*50);Style.Icon(g,Style.IconFor(Catalog.Find(Game.System).Name),new RectangleF(r.X+r.Width/2-30,r.Y+43,60,60),Color.FromArgb(190,207,222));using(var f=new Font("Segoe UI",8,FontStyle.Bold))TextRenderer.DrawText(g,Game.System.ToUpper(),f,new Rectangle((int)r.X+10,(int)r.Y+136,(int)r.Width-20,21),Color.FromArgb(195,206,222),TextFormatFlags.HorizontalCenter);}
    using(var brush=new LinearGradientBrush(new RectangleF(r.X,r.Y+135,r.Width,38),Color.Transparent,Color.FromArgb(23,30,44),90))g.FillRectangle(brush,r.X,r.Y+135,r.Width,38);g.Restore(saved);
    using(var f=new Font("Segoe UI",10,FontStyle.Bold))TextRenderer.DrawText(g,Game.Title,f,new Rectangle(15,(int)r.Y+178,Width-29,25),Color.FromArgb(236,243,250),TextFormatFlags.EndEllipsis|TextFormatFlags.SingleLine);
    Style.Icon(g,Game.Favorite?"heart":"play",new RectangleF(15,r.Y+211,12,12),Game.Favorite?Color.FromArgb(245,193,109):Style.Mint);using(var f=new Font("Segoe UI",7.5f,FontStyle.Bold))TextRenderer.DrawText(g,Game.Favorite?"FAVORITE":"PLAY YOUR WAY",f,new Rectangle(33,(int)r.Y+207,Width-42,20),Game.Favorite?Color.FromArgb(245,193,109):Style.Muted,TextFormatFlags.SingleLine);
    using(var p=new Pen(Style.Mix(Color.FromArgb(45,56,74),Style.Mint,highlight),1+highlight))g.DrawPath(p,path);
   }if(enter<1){using(var brush=new SolidBrush(Color.FromArgb((int)((1-enter)*255),Style.Background)))g.FillRectangle(brush,ClientRectangle);}
  }
 }
 public class IntroForm : Form {
  Timer timer=new Timer{Interval=16};Stopwatch watch=new Stopwatch();public double FixedTime=-1;public double Duration {get{return watch.Elapsed.TotalSeconds;}}
  public IntroForm(bool full=false){ClientSize=new Size(850,490);FormBorderStyle=FormBorderStyle.None;StartPosition=FormStartPosition.CenterScreen;BackColor=Style.Background;DoubleBuffered=true;ShowInTaskbar=false;if(full){Bounds=Screen.PrimaryScreen.Bounds;WindowState=FormWindowState.Maximized;}timer.Tick+=(s,e)=>{Invalidate();if(watch.Elapsed.TotalSeconds>=3)Close();};Shown+=(s,e)=>{watch.Start();timer.Start();if(full){Artwork.Attach(this);foreach(var system in Catalog.All)Artwork.Logo(system.Id);WindowFocus.Front(this);}};}
  protected override void Dispose(bool disposing){if(disposing)timer.Dispose();base.Dispose(disposing);}
  protected override void OnPaint(PaintEventArgs e){double t=FixedTime>=0?FixedTime:watch.Elapsed.TotalSeconds;float fade=(float)Math.Min(1,t/.5);if(t>2.65)fade=(float)Math.Max(0,(3-t)/.35);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Style.Background);
   for(int i=0;i<34;i++){float x=(i*179%Width),y=(i*97%Height);using(var b=new SolidBrush(Color.FromArgb((int)(fade*(15+12*Math.Sin(t+i))),Style.Mint)))g.FillEllipse(b,x,y,2,2);}
   float rise=(1-fade)*14;float centerY=Height/2f-61;Style.Logo(g,new PointF(Width/2,centerY+rise),57,t,fade);
   using(var f=new Font("Segoe UI",36,FontStyle.Bold))TextRenderer.DrawText(g,"O R B I T",f,new Rectangle(0,(int)(centerY+85+rise),Width,72),Style.Mix(Style.Background,Color.White,fade),TextFormatFlags.HorizontalCenter);
   using(var f=new Font("Segoe UI",9,FontStyle.Bold))TextRenderer.DrawText(g,"A  U N I V E R S E  O F  P L A Y",f,new Rectangle(0,(int)centerY+163,Width,30),Style.Mix(Style.Background,Style.Muted,fade),TextFormatFlags.HorizontalCenter);
   using(var path=Style.Round(new RectangleF(Width/2-85,centerY+227,170,3),1.4f)){using(var b=new SolidBrush(Color.FromArgb(35,44,61)))g.FillPath(b,path);}if(t>0)using(var b=new SolidBrush(Style.Mix(Style.Background,Style.Mint,fade)))g.FillRectangle(b,Width/2-85,centerY+227,(float)Math.Min(170,t/3*170),3);
  }
 }
}




