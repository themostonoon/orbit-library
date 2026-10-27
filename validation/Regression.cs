using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using OrbitLibrary;

public static class PerformanceRegression {
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 static void PumpUntil(Func<bool> condition,int timeout){var clock=Stopwatch.StartNew();while(!condition()){if(clock.ElapsedMilliseconds>timeout)throw new Exception("Timed out waiting for background work");Application.DoEvents();System.Threading.Thread.Sleep(3);}}
 static T Field<T>(object obj,string name){return (T)obj.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(obj);}
 static object Call(object obj,string method,params object[] args){return obj.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.DeclaredOnly).Invoke(obj,args);}
 [STAThread]public static int Main(){string root=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"regression-data");Directory.CreateDirectory(root);string result=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"performance-regression.txt");try{
  Application.EnableVisualStyles();var store=new Store(root);store.Data.Games.Clear();for(int i=0;i<10000;i++)store.Data.Games.Add(new Game{Title="Game "+i.ToString("D5"),System=Catalog.All[i%29].Id,Path=Path.Combine(root,i+".rom"),Favorite=i%5==0});
  using(var form=new MainForm(store,false)){
   form.ClientSize=new Size(2560,1440);form.Show();WindowFocus.Front(form);Application.DoEvents();var grid=Field<VirtualGameStrip>(form,"grid");Check(grid.Games.Count==10000,"10,000-game library missing entries");Check(grid.Controls.Count==0,"Gallery creates child controls per game");Check(form.Controls[0].Controls.Count<30,"Unbounded UI control count");
   Call(form,"Select",9999);for(int i=0;i<24;i++){grid.Advance();Application.DoEvents();System.Threading.Thread.Sleep(15);}Check(grid.Selected==9999,"Navigation to last game failed");using(var shot=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(shot,new Rectangle(0,0,form.Width,form.Height));shot.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"large-library-preview.png"));}Check(grid.CachedTiles<=grid.VisibleCount+4,"Tile cache grows with library size");
   var search=Field<TextBox>(form,"search");search.Text="Game 009";search.Text="Game 00042";PumpUntil(()=>grid.Games.Count==1,3000);Check(grid.Games[0].Title=="Game 00042","Debounced search shows stale results");search.Clear();PumpUntil(()=>grid.Games.Count==10000,3000);
   var favorite=store.Data.Games[0];favorite.Favorite=true;
   string romRoot=Path.Combine(root,"import"),nes=Path.Combine(romRoot,"nes"),snes=Path.Combine(romRoot,"snes");Directory.CreateDirectory(nes);Directory.CreateDirectory(snes);File.WriteAllText(Path.Combine(nes,"New game.nes"),"");File.WriteAllText(Path.Combine(snes,"Another game.sfc"),"");
   var watch=Stopwatch.StartNew();Call(form,"BeginScan",romRoot);Check(watch.ElapsedMilliseconds<100,"Scan blocks the UI thread");PumpUntil(()=>!Field<bool>(form,"scanning"),5000);Check(store.Data.Games.Count==10002,"Background scan did not merge correctly");Check(favorite.Favorite,"Scan lost existing favorites");Check(store.Data.Platforms["nes"].Folder==nes,"Import did not assign folders");Call(form,"BeginScan",romRoot);PumpUntil(()=>!Field<bool>(form,"scanning"),5000);Check(store.Data.Games.Count==10002,"Repeated async scan duplicates entries");
   var coverGame=new Game{Path=Path.Combine(root,"art-test.nes"),System="nes",Cover=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","consoles","nes.png")};watch.Restart();Artwork.GameImage(coverGame,256);Check(watch.ElapsedMilliseconds<30,"Artwork decode blocks the UI");PumpUntil(()=>Artwork.GameImage(coverGame,256)!=null,5000);Check(Artwork.GameImage(coverGame,256).Width<=256,"Thumbnail was not downsampled");Check(Artwork.CachedCount<=160,"Artwork cache is unbounded");
   var nav=Field<ListBox>(form,"nav");nav.SelectedIndex=1;Check(grid.Games.All(g=>g.Favorite),"Favorites regression");nav.SelectedIndex=3;Check(grid.Games.All(g=>g.System=="nes"),"System filter regression");
   var process=Process.GetCurrentProcess();process.Refresh();long before=GetGuiResources(process.Handle,0);for(int i=0;i<25;i++){form.ClientSize=new Size(1200+i*8,800+i*4);Application.DoEvents();}process.Refresh();long after=GetGuiResources(process.Handle,0);Check(after-before<50,"Scene resize leaks GDI handles");
   form.Close();
  }
  File.WriteAllText(result,"PASS: 10,000-game virtual gallery; bounded UI controls and visible-tile cache; last-game navigation; latest-query search; responsive background scan and import; duplicate prevention; favorites preserved; asynchronous downsampled artwork; bounded artwork cache; console/favorite filters; repeated resize GDI handle stability.");return 0;
 }catch(Exception e){File.WriteAllText(result,e.ToString());return 1;}}
 [System.Runtime.InteropServices.DllImport("user32.dll")]static extern int GetGuiResources(IntPtr process,int flags);
}

