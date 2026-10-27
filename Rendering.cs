using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace OrbitLibrary {
 // Persistent DIB surfaces permit region copies without re-uploading a fullscreen bitmap.
 public sealed class SceneSurface : IDisposable {
  [StructLayout(LayoutKind.Sequential)] struct Header {public uint Size;public int Width,Height;public ushort Planes,BitCount;public uint Compression,ImageSize;public int X,Y;public uint Used,Important;}
  [DllImport("gdi32.dll")]static extern IntPtr CreateCompatibleDC(IntPtr dc);
  [DllImport("gdi32.dll")]static extern IntPtr CreateDIBSection(IntPtr dc,ref Header info,uint usage,out IntPtr bits,IntPtr section,uint offset);
  [DllImport("gdi32.dll")]static extern IntPtr SelectObject(IntPtr dc,IntPtr obj);
  [DllImport("gdi32.dll")]static extern bool DeleteObject(IntPtr obj);
  [DllImport("gdi32.dll")]static extern bool DeleteDC(IntPtr dc);
  [DllImport("gdi32.dll")]static extern bool BitBlt(IntPtr dest,int x,int y,int width,int height,IntPtr source,int sx,int sy,uint op);
  IntPtr dc,handle,old;public Bitmap Bitmap;public int Width,Height;
  public SceneSurface(int width,int height){Width=width;Height=height;dc=CreateCompatibleDC(IntPtr.Zero);var header=new Header{Size=(uint)Marshal.SizeOf(typeof(Header)),Width=width,Height=-height,Planes=1,BitCount=32};IntPtr bits;handle=CreateDIBSection(dc,ref header,0,out bits,IntPtr.Zero,0);if(handle==IntPtr.Zero||bits==IntPtr.Zero){DeleteDC(dc);dc=IntPtr.Zero;throw new InvalidOperationException("Could not allocate the scene buffer.");}old=SelectObject(dc,handle);Bitmap=new Bitmap(width,height,width*4,PixelFormat.Format32bppPArgb,bits);}
  public void Copy(SceneSurface source,Rectangle area){BitBlt(dc,area.X,area.Y,area.Width,area.Height,source.dc,area.X,area.Y,0x00CC0020);}
  public void DrawTo(Graphics graphics,Rectangle dest,Point source){IntPtr target=graphics.GetHdc();try{BitBlt(target,dest.X,dest.Y,dest.Width,dest.Height,dc,source.X,source.Y,0x00CC0020);}finally{graphics.ReleaseHdc(target);}}
  public void Dispose(){if(Bitmap!=null){Bitmap.Dispose();Bitmap=null;}if(dc!=IntPtr.Zero){SelectObject(dc,old);DeleteObject(handle);DeleteDC(dc);dc=IntPtr.Zero;}}
 }
 // A single bounded cache and worker keep file access and image decoding off the UI thread.
 public static class Artwork {
  sealed class Entry { public Bitmap Image;public long Used; }
  sealed class Request { public string Key,Path;public int Size;public bool Sidecar; }
  static Dictionary<string,Entry> entries=new Dictionary<string,Entry>();
  static Queue<Request> queue=new Queue<Request>();static HashSet<string> pending=new HashSet<string>();
  static long clock,bytes;static bool working;static Control dispatcher;static int revision;
  public static event Action Changed;
  public static int Revision {get{return revision;}}
  public static int CachedCount {get{return entries.Count;}}
  public static void RefreshGames(){foreach(string key in entries.Keys.Where(k=>k.StartsWith("game:")).ToArray()){var entry=entries[key];if(entry.Image!=null){bytes-=(long)entry.Image.Width*entry.Image.Height*4;entry.Image.Dispose();}entries.Remove(key);}revision++;if(Changed!=null)Changed();}
  public static void Attach(Control ui){dispatcher=ui;}
  public static string Key(Game game,int size){return "game:"+game.Path+"|"+game.Cover+"|"+size;}
  public static Bitmap GameImage(Game game,int size){return Get(Key(game,size),string.IsNullOrEmpty(game.Cover)?game.Path:game.Cover,size,string.IsNullOrEmpty(game.Cover));}
  public static Bitmap Logo(string id){return Get("logo:"+id,Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","consoles",id+".png"),800,false);}
  static Bitmap Get(string key,string path,int size,bool sidecar){Entry hit;if(entries.TryGetValue(key,out hit)){hit.Used=++clock;return hit.Image;}lock(queue){if(pending.Add(key))queue.Enqueue(new Request{Key=key,Path=path,Size=size,Sidecar=sidecar});if(!working&&queue.Count>0){working=true;ThreadPool.QueueUserWorkItem(Work);}}return null;}
  static void Work(object unused){
   while(true){Request request;lock(queue){if(queue.Count==0){working=false;return;}request=queue.Dequeue();}
    Bitmap bitmap=null;try{string file=request.Path;if(request.Sidecar){file=null;foreach(string suffix in new[]{".png",".jpg",".jpeg"}){string candidate=Path.ChangeExtension(request.Path,suffix);if(File.Exists(candidate)){file=candidate;break;}}}if(file!=null&&File.Exists(file))using(var source=Image.FromFile(file)){float scale=Math.Min(1,request.Size/(float)Math.Max(source.Width,source.Height));bitmap=new Bitmap(Math.Max(1,(int)(source.Width*scale)),Math.Max(1,(int)(source.Height*scale)),PixelFormat.Format32bppPArgb);using(var g=Graphics.FromImage(bitmap)){g.CompositingMode=CompositingMode.SourceCopy;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(source,0,0,bitmap.Width,bitmap.Height);}}}catch(ArgumentException){}catch(IOException){}catch(UnauthorizedAccessException){}catch(OutOfMemoryException){}catch(ExternalException){}
    var image=bitmap;var item=request;Control ui=dispatcher;
    if(ui==null||ui.IsDisposed||!ui.IsHandleCreated){if(image!=null)image.Dispose();lock(queue)pending.Remove(item.Key);continue;}
    try{ui.BeginInvoke((Action)(()=>{lock(queue)pending.Remove(item.Key);entries[item.Key]=new Entry{Image=image,Used=++clock};if(image!=null)bytes+=(long)image.Width*image.Height*4;while(entries.Count>160||bytes>32*1024*1024){string oldest=entries.OrderBy(x=>x.Value.Used).First().Key;var evicted=entries[oldest];entries.Remove(oldest);if(evicted.Image!=null){bytes-=(long)evicted.Image.Width*evicted.Image.Height*4;evicted.Image.Dispose();}}revision++;if(Changed!=null)Changed();}));}catch(InvalidOperationException){if(image!=null)image.Dispose();lock(queue)pending.Remove(item.Key);}
   }
  }
  public static Bitmap LoadForTest(string id){string file=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","consoles",id+".png");if(!File.Exists(file))return null;using(var image=Image.FromFile(file))return new Bitmap(image);}
 }
 public class SceneControl : Control {
  public SceneControl(){SetStyle(ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.UserPaint|ControlStyles.ResizeRedraw,true);BackColor=Style.Background;}
  protected override void OnPaintBackground(PaintEventArgs e){var scene=Parent as LivingBackground;if(scene!=null)scene.DrawBase(e.Graphics,Bounds);else e.Graphics.Clear(Style.Background);}
 }
 public class SceneLabel : Label {
  public SceneLabel(){BackColor=Style.Background;SetStyle(ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);}
  protected override void OnPaintBackground(PaintEventArgs e){var scene=Parent as LivingBackground;if(scene!=null)scene.DrawBase(e.Graphics,Bounds);else base.OnPaintBackground(e);}
 }
 public class VirtualGameStrip : SceneControl {
  public List<Game> Games=new List<Game>();public int Selected=-1;public Action<int> SelectedChanged;public Action Play;
  public string EmptyText="";float offset,target,entrance=1;int hovered=-1;Stopwatch watch=Stopwatch.StartNew(),entry=Stopwatch.StartNew();double last;Dictionary<int,float> highlights=new Dictionary<int,float>();
  Font regular=new Font("Segoe UI",9),bold=new Font("Segoe UI",9,FontStyle.Bold);Dictionary<int,Bitmap> tiles=new Dictionary<int,Bitmap>();int artRevision=-1;
  public int CachedTiles {get{return tiles.Count;}}public int VisibleCount {get{return Math.Min(Games.Count,(Width+170)/171+2);}}
  public VirtualGameStrip(){Height=192;Cursor=Cursors.Hand;TabStop=true;}
  public void SetGames(List<Game> games){ClearTiles();highlights.Clear();Games=games;Selected=-1;offset=target=0;entrance=0;entry.Restart();Invalidate();}
  void ClearTiles(){foreach(var tile in tiles.Values)tile.Dispose();tiles.Clear();}
  public void SetSelected(int index){if(index<0||index>=Games.Count)return;int before=Selected;Selected=index;float x=index*171;if(x<target)target=x;else if(x+154>target+Width)target=x+154-Width;Clamp();if(before>=0)Invalidate(TileBounds(before));Invalidate(TileBounds(index));}
  void Clamp(){target=Math.Max(0,Math.Min(target,Math.Max(0,Games.Count*171-17-Width)));}
  Rectangle TileBounds(int index){return new Rectangle((int)(index*171-offset),0,156,166);}
  public void Advance(){double now=watch.Elapsed.TotalSeconds;double dt=last==0?.016:Math.Min(.1,now-last);last=now;if(Math.Abs(target-offset)>.3){offset+=(target-offset)*(float)(1-Math.Exp(-18*dt));Invalidate();}else if(offset!=target){offset=target;Invalidate();}int start=Math.Max(0,(int)(offset/171)),end=Math.Min(Games.Count-1,(int)((offset+Width)/171)+1);foreach(int key in highlights.Keys.Where(k=>k<start||k>end).ToArray())highlights.Remove(key);for(int i=start;i<=end;i++){float old;highlights.TryGetValue(i,out old);float goal=i==Selected||i==hovered?1:0;float next=old+(goal-old)*(float)(1-Math.Exp(-22*dt));if(Math.Abs(goal-next)<.006)next=goal;highlights[i]=next;if(old!=next)Invalidate(TileBounds(i));}if(entrance<1){entrance=Math.Min(1,entry.ElapsedMilliseconds/220f);Invalidate();}}
  protected override void OnResize(EventArgs e){base.OnResize(e);Clamp();}
  protected override void OnMouseMove(MouseEventArgs e){int next=e.Y<164?(int)((e.X+offset)/171):-1;if(next>=Games.Count)next=-1;if(next!=hovered){if(hovered>=0)Invalidate(TileBounds(hovered));hovered=next;if(hovered>=0)Invalidate(TileBounds(hovered));}base.OnMouseMove(e);}
  protected override void OnMouseLeave(EventArgs e){if(hovered>=0)Invalidate(TileBounds(hovered));hovered=-1;base.OnMouseLeave(e);}
  protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);Focus();int i=(int)((e.X+offset)/171);if(e.Y<164&&i>=0&&i<Games.Count&&SelectedChanged!=null)SelectedChanged(i);else if(e.Y>=Height-18&&Games.Count*171>Width){target=(float)e.X/Math.Max(1,Width)*(Games.Count*171-Width);Clamp();}}
  protected override void OnDoubleClick(EventArgs e){base.OnDoubleClick(e);if(Selected>=0&&Play!=null)Play();}
  protected override void OnMouseWheel(MouseEventArgs e){target-=e.Delta*2;Clamp();base.OnMouseWheel(e);}
  Bitmap Tile(int index){Bitmap tile;if(tiles.TryGetValue(index,out tile))return tile;var game=Games[index];var art=Artwork.GameImage(game,256)??ConsoleArt.Get(game.System);tile=new Bitmap(150,122,PixelFormat.Format32bppPArgb);using(var g=Graphics.FromImage(tile)){g.SmoothingMode=SmoothingMode.AntiAlias;using(var path=Style.Round(new RectangleF(2,2,146,116),13)){using(var brush=new SolidBrush(Color.FromArgb(19,22,32)))g.FillPath(brush,path);var state=g.Save();g.SetClip(path);if(art!=null)ConsoleArt.Fit(g,art,new RectangleF(12,14,126,92),1);else Style.Icon(g,Style.IconFor(Catalog.Find(game.System).Name),new RectangleF(51,42,48,48),Style.Muted);g.Restore(state);using(var p=new Pen(Color.FromArgb(48,67,92)))g.DrawPath(p,path);}if(game.Favorite)Style.Icon(g,"heart",new RectangleF(124,12,14,14),Color.FromArgb(249,203,128));}tiles[index]=tile;return tile;}
  protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;if(artRevision!=Artwork.Revision){ClearTiles();artRevision=Artwork.Revision;}if(Games.Count==0){TextRenderer.DrawText(g,EmptyText,Font,new Rectangle(12,28,Math.Max(1,Width-24),130),Style.Muted,TextFormatFlags.WordBreak);return;}
   int start=Math.Max(0,(int)(offset/171)),end=Math.Min(Games.Count-1,(int)((offset+Width)/171)+1);foreach(int key in tiles.Keys.Where(k=>k<start-2||k>end+2).ToArray()){tiles[key].Dispose();tiles.Remove(key);}
   for(int i=start;i<=end;i++){int x=(int)Math.Round(i*171-offset);if(!e.ClipRectangle.IntersectsWith(new Rectangle(x,0,154,164)))continue;bool active=i==Selected;float highlight;highlights.TryGetValue(i,out highlight);int y=(int)Math.Round(6-highlight*4+(1-entrance)*(1-entrance)*10);g.DrawImageUnscaled(Tile(i),x+1,y);if(highlight>0)using(var p=new Pen(Style.Mix(Color.FromArgb(48,67,92),Color.White,highlight),1+highlight))using(var path=Style.Round(new RectangleF(x+3,y+2,146,116),13))g.DrawPath(p,path);TextRenderer.DrawText(g,Games[i].Title,active?bold:regular,new Rectangle(x+5,130,145,27),active?Color.White:Style.Muted,TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis);}
   if(Games.Count*171>Width){float length=Math.Max(36,Width*(float)Width/(Games.Count*171));using(var b=new SolidBrush(Color.FromArgb(32,39,52)))g.FillRectangle(b,0,Height-10,Width,3);using(var b=new SolidBrush(Color.FromArgb(108,126,149)))g.FillRectangle(b,offset/Math.Max(1,Games.Count*171-Width)*(Width-length),Height-10,length,3);}
  }
  protected override void Dispose(bool disposing){if(disposing){ClearTiles();regular.Dispose();bold.Dispose();}base.Dispose(disposing);}
 }
}

