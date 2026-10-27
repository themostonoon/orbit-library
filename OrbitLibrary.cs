using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace OrbitLibrary {
 public class SystemDef {
  public string Id, Name, Emulator, Core, Extensions, Aliases, Arguments;
  public SystemDef(string id,string name,string em,string core,string ext,string aliases,string args) { Id=id;Name=name;Emulator=em;Core=core;Extensions=ext;Aliases=aliases;Arguments=args; }
 }
 public class PlatformSettings { public string Folder="", Exe="", Args="\"{rom}\"", Core=""; public bool RetroArch=false; }
 public class Game { public string Path="", System="", Title="", Cover=""; public bool Favorite=false; public string LastPlayed=""; public int Plays=0; }
 public class LibraryData { public int Version=1; public Dictionary<string,PlatformSettings> Platforms=new Dictionary<string,PlatformSettings>(); public List<Game> Games=new List<Game>(); }
 public static class Catalog {
  public static readonly List<SystemDef> All=new List<SystemDef> {
   new SystemDef("nes","Nintendo Entertainment System","Mesen","mesen_libretro.dll",".nes|.fds|.zip","nes|famicom","\"{rom}\""),
   new SystemDef("snes","Super Nintendo","Snes9x / Mesen","snes9x_libretro.dll",".sfc|.smc|.zip","snes|sfc|supernintendo","\"{rom}\""),
   new SystemDef("n64","Nintendo 64","ares / RetroArch","mupen64plus_next_libretro.dll",".n64|.z64|.v64|.zip","n64|nintendo64","\"{rom}\""),
   new SystemDef("gb","Game Boy","mGBA","gambatte_libretro.dll",".gb|.zip","gb|gameboy","\"{rom}\""),
   new SystemDef("gbc","Game Boy Color","mGBA","gambatte_libretro.dll",".gbc|.zip","gbc|gameboycolor","\"{rom}\""),
   new SystemDef("gba","Game Boy Advance","mGBA","mgba_libretro.dll",".gba|.zip","gba|gameboyadvance","\"{rom}\""),
   new SystemDef("nds","Nintendo DS","melonDS","melondsds_libretro.dll",".nds|.zip","nds|ds|nintendods","\"{rom}\""),
   new SystemDef("3ds","Nintendo 3DS","Azahar","azahar_libretro.dll",".3ds|.cci|.cxi|.app","3ds|n3ds|nintendo3ds","\"{rom}\""),
   new SystemDef("gc","Nintendo GameCube","Dolphin","dolphin_libretro.dll",".iso|.gcm|.rvz|.ciso|.wia","gc|gamecube","-b -e \"{rom}\""),
   new SystemDef("wii","Nintendo Wii","Dolphin","dolphin_libretro.dll",".iso|.wbfs|.rvz|.wad|.ciso","wii","-b -e \"{rom}\""),
   new SystemDef("wiiu","Nintendo Wii U","Cemu","",".wud|.wux|.rpx|.wua","wiiu","-g \"{rom}\""),
   new SystemDef("switch","Nintendo Switch","Configure your emulator","",".nsp|.xci|.nro","switch|nintendoswitch","\"{rom}\""),
   new SystemDef("psx","PlayStation","DuckStation","swanstation_libretro.dll",".cue|.chd|.pbp|.iso|.m3u","psx|ps1|playstation","-batch \"{rom}\""),
   new SystemDef("ps2","PlayStation 2","PCSX2","lrps2_libretro.dll",".iso|.chd|.cso|.bin|.m3u","ps2|playstation2","-batch \"{rom}\""),
   new SystemDef("ps3","PlayStation 3","RPCS3","",".elf|.self","ps3|playstation3","\"{rom}\""),
   new SystemDef("psp","PlayStation Portable","PPSSPP","ppsspp_libretro.dll",".iso|.cso|.pbp|.chd","psp","\"{rom}\""),
   new SystemDef("megadrive","Sega Mega Drive / Genesis","RetroArch / ares","genesis_plus_gx_libretro.dll",".md|.gen|.smd|.bin|.zip","megadrive|genesis|md","\"{rom}\""),
   new SystemDef("mastersystem","Sega Master System","RetroArch","genesis_plus_gx_libretro.dll",".sms|.zip","mastersystem|sms","\"{rom}\""),
   new SystemDef("gamegear","Sega Game Gear","RetroArch","genesis_plus_gx_libretro.dll",".gg|.zip","gamegear|gg","\"{rom}\""),
   new SystemDef("segacd","Sega CD","RetroArch","genesis_plus_gx_libretro.dll",".cue|.chd|.iso|.m3u","segacd|megacd","\"{rom}\""),
   new SystemDef("sega32x","Sega 32X","RetroArch / ares","picodrive_libretro.dll",".32x|.bin|.zip","sega32x|32x","\"{rom}\""),
   new SystemDef("saturn","Sega Saturn","Mednafen / RetroArch","mednafen_saturn_libretro.dll",".cue|.chd|.iso|.m3u","saturn|segasaturn","\"{rom}\""),
   new SystemDef("dreamcast","Sega Dreamcast","Flycast","flycast_libretro.dll",".gdi|.chd|.cdi|.cue","dreamcast|dc","\"{rom}\""),
   new SystemDef("xbox","Original Xbox","xemu","",".iso","xbox","-dvd_path \"{rom}\""),
   new SystemDef("xbox360","Xbox 360","Xenia","",".iso|.xex","xbox360|x360","\"{rom}\""),
   new SystemDef("arcade","Arcade / Neo Geo","MAME / RetroArch","fbneo_libretro.dll",".zip|.7z","arcade|mame|fbneo|neogeo","{name}"),
   new SystemDef("pce","PC Engine / TurboGrafx-16","RetroArch","mednafen_pce_fast_libretro.dll",".pce|.cue|.chd|.zip","pce|pcengine|tg16","\"{rom}\""),
   new SystemDef("atari2600","Atari 2600","Stella","stella_libretro.dll",".a26|.bin|.zip","atari2600|2600","\"{rom}\""),
   new SystemDef("dos","DOS PC","DOSBox / RetroArch","dosbox_pure_libretro.dll",".zip|.conf|.exe|.com","dos|pcdos","\"{rom}\"")
  };
  public static SystemDef Find(string id) { return All.First(x=>x.Id==id); }
 }
 public class Store {
  public string FilePath; public LibraryData Data; public string Warning="";
  public Store(LibraryData data){Data=data;}
  public Store(string folder) {
   Directory.CreateDirectory(folder); FilePath=Path.Combine(folder,"library.json"); Data=new LibraryData();
   if(File.Exists(FilePath)) try { Data=new JavaScriptSerializer{MaxJsonLength=int.MaxValue}.Deserialize<LibraryData>(File.ReadAllText(FilePath)); if(Data==null || Data.Platforms==null || Data.Games==null) throw new Exception("Invalid library data"); } catch(Exception e) { string backup=FilePath+".unreadable-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".bak";File.Copy(FilePath,backup,true);Data=new LibraryData();Warning="The saved library could not be read. A backup was preserved at "+backup+"\n"+e.Message; }
   foreach(var s in Catalog.All) if(!Data.Platforms.ContainsKey(s.Id)) Data.Platforms[s.Id]=new PlatformSettings{Args=s.Arguments};
  }
  public void Save() { string temp=FilePath+".tmp";File.WriteAllText(temp,new JavaScriptSerializer{MaxJsonLength=int.MaxValue}.Serialize(Data));if(File.Exists(FilePath)) File.Replace(temp,FilePath,FilePath+".bak");else File.Move(temp,FilePath); }
  public int Scan(string id,string folder) {
   var sys=Catalog.Find(id);var extensions=new HashSet<string>(sys.Extensions.Split('|'),StringComparer.OrdinalIgnoreCase);
   var files=new List<string>(); Collect(folder,files);
   var existing=new HashSet<string>(Data.Games.Where(g=>g.System==id).Select(g=>g.Path),StringComparer.OrdinalIgnoreCase);
   var suppressed=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
   foreach(string path in files.Where(p=>Path.GetExtension(p).Equals(".cue",StringComparison.OrdinalIgnoreCase))) {
    try { foreach(string line in File.ReadLines(path)) {var m=System.Text.RegularExpressions.Regex.Match(line,"^\\s*FILE\\s+\"([^\"]+)\"",System.Text.RegularExpressions.RegexOptions.IgnoreCase);if(m.Success) suppressed.Add(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path),m.Groups[1].Value)));} } catch(IOException) {} catch(UnauthorizedAccessException) {}
   }
   int count=0;foreach(string path in files) if(extensions.Contains(Path.GetExtension(path)) && !suppressed.Contains(path) && existing.Add(path)) { Data.Games.Add(new Game{Path=path,System=id,Title=Path.GetFileNameWithoutExtension(path)});count++; }
   return count;
  }
  static void Collect(string folder,List<string> files) { try { files.AddRange(Directory.GetFiles(folder)); foreach(string sub in Directory.GetDirectories(folder)) if((File.GetAttributes(sub)&FileAttributes.ReparsePoint)==0) Collect(sub,files); } catch(UnauthorizedAccessException) {} catch(IOException) {} }
 }
 public static class Launch {
  public static ProcessStartInfo Build(Game game,PlatformSettings settings) {
   if(!File.Exists(game.Path)) throw new Exception("The game file is missing. Reconnect its drive or import its new location.");
   if(!File.Exists(settings.Exe)) throw new Exception("Choose an emulator executable in System setup first.");
   if(settings.RetroArch && !File.Exists(settings.Core)) throw new Exception("Choose the installed RetroArch core DLL in System setup first.");
   string args=(settings.Args??"\"{rom}\"").Replace("{rom}",game.Path).Replace("{name}",Path.GetFileNameWithoutExtension(game.Path)).Replace("{romdir}",Path.GetDirectoryName(game.Path));
   if(settings.RetroArch) args="-L \""+settings.Core+"\" "+args;
   return new ProcessStartInfo(settings.Exe,args){UseShellExecute=false,WorkingDirectory=Path.GetDirectoryName(settings.Exe)};
  }
 }
 public class SetupDialog : Form {
  ComboBox system;TextBox folder,exe,args,core;CheckBox retro;Label suggested;Store store;bool loading;string loadedId=null;Dictionary<string,PlatformSettings> drafts;
  public SetupDialog(Store st,string initial) {store=st;Shown+=(s,e)=>Style.DarkTitle(this);drafts=new JavaScriptSerializer{MaxJsonLength=int.MaxValue}.Deserialize<Dictionary<string,PlatformSettings>>(new JavaScriptSerializer{MaxJsonLength=int.MaxValue}.Serialize(st.Data.Platforms));Text="System setup - Orbit Library";ClientSize=new Size(690,480);BackColor=Color.FromArgb(24,29,43);ForeColor=Color.White;Font=new Font("Segoe UI",10);FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;StartPosition=FormStartPosition.CenterParent;
   system=new ComboBox{Left=24,Top=44,Width=642,DropDownStyle=ComboBoxStyle.DropDownList};system.Items.AddRange(Catalog.All.Select(s=>s.Name).ToArray());Controls.Add(system);LabelAt("SYSTEM",24,20);suggested=LabelAt("",24,82);suggested.Width=640;
   folder=Field("ROM FOLDER",115,true,false);exe=Field("EMULATOR EXECUTABLE",181,false,false);args=Field("LAUNCH ARGUMENTS",247,false,true);
   retro=new CheckBox{Text="Launch through RetroArch",Left=24,Top=310,Width=320};Controls.Add(retro);core=Field("RETROARCH CORE DLL",342,false,false);
   var save=ButtonAt("Save setup",510,430,156);save.Click+=(s,e)=>{Persist();store.Data.Platforms=drafts;store.Save();DialogResult=DialogResult.OK;Close();};
   LabelAt("Tokens: {rom} = full path   {name} = filename   {romdir} = folder",24,408).Width=640;
   system.SelectedIndexChanged+=(s,e)=>{if(!loading){if(loadedId!=null)Persist();LoadFields();}};retro.CheckedChanged+=(s,e)=>core.Enabled=retro.Checked;
   system.SelectedIndex=Math.Max(0,Catalog.All.FindIndex(s=>s.Id==initial));
  }
  Label LabelAt(string text,int x,int y){var l=new Label{Text=text,Left=x,Top=y,Width=350,Height=22,ForeColor=Color.FromArgb(155,169,194),Font=new Font("Segoe UI",9)};Controls.Add(l);return l;}
  Button ButtonAt(string text,int x,int y,int w){var b=new PremiumButton{Text=text,Left=x,Top=y,Width=w,Height=34,FlatStyle=FlatStyle.Flat,FlatAppearance={BorderColor=Color.FromArgb(62,78,98)},BackColor=Color.FromArgb(40,54,72),ForeColor=Color.White};Controls.Add(b);return b;}
  TextBox Field(string label,int y,bool dir,bool raw){LabelAt(label,24,y);var box=new TextBox{Left=24,Top=y+23,Width=raw?642:544,BackColor=Color.FromArgb(35,42,59),ForeColor=Color.White,BorderStyle=BorderStyle.FixedSingle};Controls.Add(box);if(!raw){var b=ButtonAt("Browse",580,y+20,86);b.Click+=(s,e)=>{if(dir){using(var d=new FolderBrowserDialog{Description="Select the ROM folder for this system"})if(d.ShowDialog(this)==DialogResult.OK)box.Text=d.SelectedPath;}else using(var d=new OpenFileDialog{Filter=label.Contains("DLL")?"Core DLL|*.dll":"Executable|*.exe",CheckFileExists=true})if(d.ShowDialog(this)==DialogResult.OK)box.Text=d.FileName;};}return box;}
  void LoadFields(){loading=true;var def=Catalog.All[system.SelectedIndex];loadedId=def.Id;var p=drafts[def.Id];folder.Text=p.Folder;exe.Text=p.Exe;args.Text=p.Args;core.Text=p.Core;retro.Checked=p.RetroArch;core.Enabled=retro.Checked;suggested.Text="Suggested: "+def.Emulator+(def.Core.Length>0?"  |  Core: "+def.Core:"");loading=false;}
  void Persist(){var p=drafts[loadedId];p.Folder=folder.Text.Trim();p.Exe=exe.Text.Trim();p.Args=args.Text;p.Core=core.Text.Trim();p.RetroArch=retro.Checked;}
 }
 public class MainForm : Form {
  Store store;ListBox nav;VirtualGameStrip grid;TextBox search;Label heading,summary,status,detail;Control empty;Timer animation;LivingBackground canvas;ConsoleRail rail;Spotlight spotlight;bool ConsoleFocus=false;bool startupFullscreen;Button play,favorite;List<Game> cards=new List<Game>();Timer searchDelay=new Timer{Interval=100};bool scanning;bool pausedAnimation;string filter="all";int selected=-1;bool fullscreen;FormBorderStyle oldBorder;Rectangle oldBounds;Timer padTimer;ushort previous;DateTime lastMove=DateTime.MinValue;Process running;bool launching;
  static Color bg=Style.Background,muted=Color.FromArgb(150,164,189),accent=Color.FromArgb(118,227,206);
  [StructLayout(LayoutKind.Sequential)]struct Pad {public ushort Buttons;public byte LT,RT;public short LX,LY,RX,RY;}
  [StructLayout(LayoutKind.Sequential)]struct PadState {public uint Packet;public Pad Gamepad;}
  [DllImport("xinput1_4.dll")]static extern uint XInputGetState(uint index,out PadState state);
  public MainForm(Store st,bool full=true){store=st;startupFullscreen=full;Text="Orbit Library";ClientSize=new Size(1280,860);MinimumSize=new Size(1080,740);StartPosition=FormStartPosition.CenterScreen;BackColor=Style.Background;ForeColor=Color.White;Font=new Font("Segoe UI",10);KeyPreview=true;DoubleBuffered=true;Artwork.Attach(this);Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);
   canvas=new LivingBackground{Dock=DockStyle.Fill};Controls.Add(canvas);
   var brand=new BrandControl{Left=45,Top=12,BackColor=Style.Background};brand.Size=new Size(218,80);canvas.Controls.Add(brand);
   var all=MakeButton("All games",292,32,125);var favorites=MakeButton("Favorites",430,32,130);var recent=MakeButton("Recently played",573,32,167);canvas.Controls.Add(all);canvas.Controls.Add(favorites);canvas.Controls.Add(recent);all.Click+=(s,e)=>nav.SelectedIndex=0;favorites.Click+=(s,e)=>nav.SelectedIndex=1;recent.Click+=(s,e)=>nav.SelectedIndex=2;
   var setup=MakeButton("System setup",0,32,143);canvas.Controls.Add(setup);setup.Click+=(s,e)=>Setup();var more=MakeButton("More",0,32,92);canvas.Controls.Add(more);
   var menu=new ContextMenuStrip{BackColor=Color.FromArgb(24,31,46),ForeColor=Color.White};menu.Items.Add("Import ROM folders",null,(s,e)=>ImportRoot());menu.Items.Add("Scan library",null,(s,e)=>Scan());menu.Items.Add("Add a game file",null,(s,e)=>AddGame());menu.Items.Add("Set selected game cover",null,(s,e)=>SetCover());menu.Items.Add("Remove selected game from library",null,(s,e)=>RemoveGame());menu.Items.Add("Open library data folder",null,(s,e)=>Process.Start("explorer.exe",Path.GetDirectoryName(store.FilePath)));menu.Items.Add("Fullscreen (F11)",null,(s,e)=>ToggleFullscreen());var motion=new ToolStripMenuItem("Animated background"){Checked=true,CheckOnClick=true};motion.CheckedChanged+=(s,e)=>canvas.Motion=motion.Checked;menu.Items.Add(motion);menu.Items.Add("Help",null,(s,e)=>Help());menu.Items.Add("Exit Orbit",null,(s,e)=>Close());foreach(ToolStripItem item in menu.Items)item.Image=Style.IconImage(Style.IconFor(item.Text),Style.Mint);more.Click+=(s,e)=>menu.Show(more,new Point(0,more.Height));
   search=new TextBox{Left=58,Top=94,Width=270,BackColor=Color.FromArgb(19,24,35),ForeColor=Color.White,BorderStyle=BorderStyle.FixedSingle};canvas.Controls.Add(search);searchDelay.Tick+=(s,e)=>{searchDelay.Stop();Render();};search.TextChanged+=(s,e)=>{searchDelay.Stop();searchDelay.Start();};search.HandleCreated+=(s,e)=>Style.SetCue(search,"Search games  /  Ctrl+K");
   heading=new SceneLabel{Left=350,Top=94,Width=340,Height=24,BackColor=Style.Background,ForeColor=Color.FromArgb(215,230,245),Font=new Font("Segoe UI",11)};canvas.Controls.Add(heading);
   summary=new SceneLabel{Left=700,Top=96,Width=360,Height=22,BackColor=Style.Background,ForeColor=Style.Muted,Font=new Font("Segoe UI",9)};canvas.Controls.Add(summary);
   nav=new ListBox();nav.SelectedIndexChanged+=(s,e)=>{if(nav.SelectedIndex<0)return;filter=nav.SelectedIndex==0?"all":nav.SelectedIndex==1?"favorites":nav.SelectedIndex==2?"recent":Catalog.All[nav.SelectedIndex-3].Id;if(nav.SelectedIndex>=3)rail.SetSelected(nav.SelectedIndex-3);Render();};
   rail=new ConsoleRail{Left=56,Top=134,Width=1168};canvas.Controls.Add(rail);rail.Changed=i=>{ConsoleFocus=true;nav.SelectedIndex=i+3;};
   spotlight=new Spotlight{Left=56,Top=262,Width=1168,Height=280};canvas.Controls.Add(spotlight);
   play=MakeButton("Play game  [Enter]",60,560,213);play.Height=44;canvas.Controls.Add(play);play.Click+=(s,e)=>Play();favorite=MakeButton("Favorite  [F]",287,560,175);favorite.Height=44;canvas.Controls.Add(favorite);favorite.Click+=(s,e)=>Favorite();
   var scan=MakeButton("Scan library",476,560,144);scan.Height=44;canvas.Controls.Add(scan);scan.Click+=(s,e)=>Scan();var import=MakeButton("Import ROMs",634,560,150);import.Height=44;canvas.Controls.Add(import);import.Click+=(s,e)=>ImportRoot();
   var libraryLabel=new SceneLabel{Left=60,Top=626,Width=450,Height=25,Text="YOUR COLLECTION",BackColor=Style.Background,ForeColor=Color.FromArgb(207,223,240),Font=new Font("Segoe UI",9,FontStyle.Bold)};canvas.Controls.Add(libraryLabel);
   grid=new VirtualGameStrip{Left=56,Top=657,Width=1168,Height=192};grid.SelectedChanged=i=>{ConsoleFocus=false;Select(i);};grid.Play=()=>Play();canvas.Controls.Add(grid);grid.MouseDown+=(s,e)=>ConsoleFocus=false;
   empty=new SceneLabel{Size=new Size(880,150),BackColor=Style.Background,ForeColor=Style.Muted,Font=new Font("Segoe UI",14),Padding=new Padding(12,28,0,0)};
   detail=new SceneLabel{Left=60,Width=610,Height=25,BackColor=Style.Background,ForeColor=Style.Muted,Font=new Font("Segoe UI",8.5f)};canvas.Controls.Add(detail);status=new SceneLabel{Left=700,Width=500,Height=25,BackColor=Style.Background,ForeColor=Style.Muted,Font=new Font("Segoe UI",8.5f)};canvas.Controls.Add(status);
   Action layout=()=>{int w=ClientSize.Width,h=ClientSize.Height;int left=Math.Max(56,(w-1760)/2),wide=Math.Min(w-112,1760);brand.Left=left-11;all.Left=left+236;favorites.Left=left+374;recent.Left=left+517;setup.Left=left+wide-246;more.Left=left+wide-89;search.Left=left+2;heading.Left=left+294;summary.Left=left+644;rail.Left=left;rail.Width=wide;rail.Zoom=h>=1100?1.35f:1;rail.Height=(int)(123*rail.Zoom);rail.Top=h>=1100?155:134;spotlight.Left=left;spotlight.Top=rail.Bottom+12;spotlight.Width=wide;spotlight.Height=Math.Max(230,Math.Min(h>=1100?460:345,h-610));play.Left=left+4;favorite.Left=left+231;scan.Left=left+420;import.Left=left+578;libraryLabel.Left=left+4;grid.Left=left;int actions=spotlight.Bottom+3;play.Top=actions;favorite.Top=actions;scan.Top=actions;import.Top=actions;libraryLabel.Top=actions+63;grid.Top=libraryLabel.Bottom+4;grid.Width=wide;grid.Height=Math.Max(120,h-grid.Top-49);detail.Left=left+4;detail.Top=h-32;status.Top=h-32;status.Left=left+Math.Max(644,wide-510);};Resize+=(s,e)=>layout();layout();
   RefreshNav();nav.SelectedIndex=0;status.Text="Up: consoles  /  Down: games  /  F11: window  /  F1: help";
   padTimer=new Timer{Interval=70};padTimer.Tick+=(s,e)=>PollPad();padTimer.Start();animation=new Timer{Interval=15};animation.Tick+=(s,e)=>{if(WindowState!=FormWindowState.Minimized&&ContainsFocus){canvas.Advance();rail.Advance();spotlight.Advance();grid.Advance();}};Artwork.Changed+=ArtworkChanged;animation.Start();Activated+=(s,e)=>{if(!pausedAnimation)animation.Start();};Deactivate+=(s,e)=>animation.Stop();Resize+=(s,e)=>{if(animation==null)return;if(WindowState==FormWindowState.Minimized)animation.Stop();else if(ContainsFocus&&!pausedAnimation)animation.Start();};FormClosing+=(s,e)=>{padTimer.Stop();animation.Stop();animation.Dispose();padTimer.Dispose();searchDelay.Dispose();Artwork.Changed-=ArtworkChanged;nav.Dispose();store.Save();};
   Shown+=(s,e)=>{Style.DarkTitle(this);Style.DarkScroll(grid);if(startupFullscreen){ToggleFullscreen();WindowFocus.Front(this);}if(store.Warning.Length>0)MessageBox.Show(this,store.Warning,"Library recovery");};
  }
  void ArtworkChanged(){rail.Invalidate();spotlight.Invalidate();grid.Invalidate();}
  public void PauseAnimation(){pausedAnimation=true;animation.Stop();canvas.Motion=false;}
  Button MakeButton(string text,int x,int y,int w){return new PremiumButton{Text=text,Left=x,Top=y,Width=w,Height=36,FlatStyle=FlatStyle.Flat,FlatAppearance={BorderColor=Color.FromArgb(62,78,98)},BackColor=Color.FromArgb(33,43,60),ForeColor=Color.White,Cursor=Cursors.Hand};}
  void RefreshNav(){int i=nav.SelectedIndex;var counts=store.Data.Games.GroupBy(g=>g.System).ToDictionary(g=>g.Key,g=>g.Count());nav.BeginUpdate();nav.Items.Clear();nav.Items.Add("All games");nav.Items.Add("Favorites");nav.Items.Add("Recently played");foreach(var def in Catalog.All){int n;counts.TryGetValue(def.Id,out n);nav.Items.Add(def.Name+(n>0?"  ("+n+")":""));}if(i>=0)nav.SelectedIndex=i;nav.EndUpdate();}
  void Render(){searchDelay.Stop();string old=selected>=0&&selected<cards.Count?cards[selected].Path:null;
   var known=new HashSet<string>(Catalog.All.Select(s=>s.Id));IEnumerable<Game> games=store.Data.Games.Where(g=>known.Contains(g.System));if(filter=="favorites")games=games.Where(g=>g.Favorite);else if(filter=="recent")games=games.Where(g=>!string.IsNullOrEmpty(g.LastPlayed));else if(filter!="all")games=games.Where(g=>g.System==filter);
   if(search.Text.Length>0)games=games.Where(g=>g.Title.IndexOf(search.Text,StringComparison.OrdinalIgnoreCase)>=0);games=filter=="recent"?games.OrderByDescending(g=>g.LastPlayed):games.OrderBy(g=>g.Title,StringComparer.OrdinalIgnoreCase);
   cards=games.ToList();grid.SetGames(cards);heading.Text=filter=="all"?"All games":filter=="favorites"?"Favorites":filter=="recent"?"Recently played":Catalog.Find(filter).Name;summary.Text=cards.Count+" games  /  "+store.Data.Games.Count+" in your collection";selected=-1;
   if(cards.Count==0){grid.EmptyText=store.Data.Games.Count==0?"Import your ROM folders, connect an emulator, and bring your collection together.\n\nStart with Import ROMs or System setup above.":"No matching games yet. Try another system, clear your search, or scan a ROM folder.";detail.Text="Choose a game to see its launch details.";play.Enabled=false;favorite.Enabled=false;spotlight.SetGame(null,CurrentSystem(),0,false);}else Select(Math.Max(0,cards.FindIndex(c=>c.Path==old)));
  }
  public void VerifyNavigation(){
   nav.SelectedIndex=3;AssertUi(filter=="nes","console filter");
   MoveSelection(0,-1);MoveSelection(1,0);AssertUi(filter=="snes","console arrow navigation");
   nav.SelectedIndex=0;AssertUi(cards.Count==store.Data.Games.Count,"all games");
   nav.SelectedIndex=1;AssertUi(cards.All(c=>c.Favorite),"favorites filter");
   nav.SelectedIndex=0;search.Text="Example";Render();AssertUi(cards.Count==1,"search filter");search.Clear();
   var before=Bounds;ToggleFullscreen();AssertUi(FormBorderStyle==FormBorderStyle.None&&Bounds==Screen.FromControl(this).Bounds,"fullscreen bounds");ToggleFullscreen();AssertUi(Bounds==before,"restore window bounds");
   foreach(var def in Catalog.All)using(var logo=Artwork.LoadForTest(def.Id))AssertUi(logo!=null,"logo asset "+def.Id);
   using(var a=new Bitmap(320,200))using(var b=new Bitmap(320,200)){using(var g=Graphics.FromImage(a))LivingBackground.Draw(g,new Rectangle(0,0,320,200),0);using(var g=Graphics.FromImage(b))LivingBackground.Draw(g,new Rectangle(0,0,320,200),10);bool changed=false;for(int y=0;y<200&&!changed;y+=3)for(int x=0;x<320;x+=3)if(a.GetPixel(x,y)!=b.GetPixel(x,y)){changed=true;break;}AssertUi(changed,"animated space background");}
  }
  static void AssertUi(bool condition,string message){if(!condition)throw new Exception("UI TEST FAILED: "+message);}
  void Select(int index){if(index<0||index>=cards.Count)return;selected=index;var game=cards[index];grid.SetSelected(index);var settings=store.Data.Platforms[game.System];bool configured=File.Exists(settings.Exe);detail.Text=Catalog.Find(game.System).Name+"  /  "+(configured?Path.GetFileNameWithoutExtension(settings.Exe):"Emulator not configured");play.Enabled=!launching;favorite.Enabled=true;favorite.Text=game.Favorite?"Unfavorite  [F]":"Favorite  [F]";spotlight.SetGame(game,game.System,cards.Count,configured);}
  Game Current(){return selected>=0&&selected<cards.Count?cards[selected]:null;}
  string CurrentSystem(){var game=Current();return filter!="all"&&filter!="favorites"&&filter!="recent"?filter:game!=null?game.System:"nes";}
  void Setup(){using(var d=new SetupDialog(store,CurrentSystem()))if(d.ShowDialog(this)==DialogResult.OK){status.Text="Setup saved. Scan library to add games.";Render();}}
  sealed class ScanResult {public List<Game> Added;public Dictionary<string,string> Folders;}
  void Scan(){BeginScan(null);}
  void ImportRoot(){if(scanning){status.Text="A library scan is already running.";return;}using(var dialog=new FolderBrowserDialog{Description="Select the root containing system folders (nes, ps2, gc...)"})if(dialog.ShowDialog(this)==DialogResult.OK)BeginScan(dialog.SelectedPath);}
  void BeginScan(string root){if(scanning){status.Text="A library scan is already running.";return;}scanning=true;status.Text="Scanning folders in the background. You can keep browsing.";
   var snapshot=store.Data.Games.ToArray();var folders=store.Data.Platforms.ToDictionary(x=>x.Key,x=>x.Value.Folder);
   System.Threading.Tasks.Task.Factory.StartNew(()=>{
    var matched=new Dictionary<string,string>();
    if(root!=null){foreach(string dir in Directory.GetDirectories(root)){string name=new string(Path.GetFileName(dir).ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());var def=Catalog.All.FirstOrDefault(x=>x.Aliases.Split('|').Contains(name));if(def!=null)matched[def.Id]=dir;}}else foreach(var folder in folders)if(Directory.Exists(folder.Value))matched[folder.Key]=folder.Value;
    var worker=new Store(new LibraryData{Games=snapshot.ToList()});foreach(var folder in matched)worker.Scan(folder.Key,folder.Value);
    return new ScanResult{Added=worker.Data.Games.Skip(snapshot.Length).ToList(),Folders=matched};
   }).ContinueWith(task=>{if(IsDisposed||Disposing||!IsHandleCreated)return;try{BeginInvoke((Action)(()=>{scanning=false;if(task.IsFaulted){Error(task.Exception.GetBaseException());return;}var result=task.Result;var existing=new HashSet<string>(store.Data.Games.Select(g=>g.System+"|"+g.Path),StringComparer.OrdinalIgnoreCase);int added=0;foreach(var game in result.Added)if(existing.Add(game.System+"|"+game.Path)){store.Data.Games.Add(game);added++;}if(root!=null)foreach(var folder in result.Folders)store.Data.Platforms[folder.Key].Folder=folder.Value;Artwork.RefreshGames();store.Save();RefreshNav();Render();status.Text="Scan complete: "+added+" new games in "+result.Folders.Count+" system folders.";if(root!=null&&result.Folders.Count==0)status.Text="No recognized system folders. Assign your folders in System setup.";}));}catch(InvalidOperationException){}});
  }
  void AddGame(){string id=CurrentSystem();using(var d=new OpenFileDialog{Title="Add game to "+Catalog.Find(id).Name,Filter="Game files|"+string.Join(";",Catalog.Find(id).Extensions.Split('|').Select(x=>"*"+x))+"|All files|*.*",Multiselect=true})if(d.ShowDialog(this)==DialogResult.OK){foreach(string file in d.FileNames)if(!store.Data.Games.Any(g=>g.System==id&&g.Path.Equals(file,StringComparison.OrdinalIgnoreCase)))store.Data.Games.Add(new Game{System=id,Path=file,Title=Path.GetFileNameWithoutExtension(file)});store.Save();RefreshNav();Render();}}
  void SetCover(){var game=Current();if(game==null)return;using(var d=new OpenFileDialog{Filter="Cover images|*.png;*.jpg;*.jpeg;*.bmp"})if(d.ShowDialog(this)==DialogResult.OK){game.Cover=d.FileName;Artwork.RefreshGames();store.Save();Render();}}
  void RemoveGame(){var game=Current();if(game==null)return;store.Data.Games.Remove(game);store.Save();RefreshNav();Render();status.Text="Removed from library. The game file is untouched.";}
  void Favorite(){var game=Current();if(game==null)return;game.Favorite=!game.Favorite;store.Save();Render();}
  void Play(){var game=Current();if(game==null||launching)return;try{var p=store.Data.Platforms[game.System];var info=Launch.Build(game,p);running=Process.Start(info);launching=true;play.Enabled=false;game.LastPlayed=DateTime.UtcNow.ToString("o");game.Plays++;store.Save();status.Text="Playing "+game.Title+". Return here after closing the emulator.";WindowState=FormWindowState.Minimized;var timer=new Timer{Interval=700};timer.Tick+=(s,e)=>{if(running==null||running.HasExited){timer.Stop();timer.Dispose();if(running!=null)running.Dispose();running=null;launching=false;WindowState=FormWindowState.Normal;Activate();Render();status.Text="Welcome back. Choose your next game.";}};timer.Start();}catch(Exception e){Error(e);}}
  void Error(Exception e){MessageBox.Show(this,e.Message,"Orbit Library",MessageBoxButtons.OK,MessageBoxIcon.Information);status.Text="Action could not be completed.";}
  void ToggleFullscreen(){if(!fullscreen){oldBounds=Bounds;oldBorder=FormBorderStyle;FormBorderStyle=FormBorderStyle.None;WindowState=FormWindowState.Normal;Bounds=Screen.FromControl(this).Bounds;fullscreen=true;}else{FormBorderStyle=oldBorder;Bounds=oldBounds;fullscreen=false;}}
  void Help(){MessageBox.Show(this,"1. System setup: choose ROM folder, emulator .exe, and launch arguments.\n2. For RetroArch, enable its checkbox and select an installed core DLL.\n3. Scan library, select a game, and press Enter.\n\nArtwork: place GameTitle.png or .jpg beside a ROM, or use More > Set cover.\n\nKeyboard: arrows move, Enter launches, F favorites, F11 fullscreen, Ctrl+K searches.\nController: D-pad / left stick moves, A launches, X favorites, LB/RB switches systems, Start toggles fullscreen.\n\nOrbit does not download games, emulators, BIOS, or artwork. Arcade standalone MAME needs its ROM path configured in MAME. PS3 games use their EBOOT.BIN: add it manually with All files.\n\nLibrary data: "+Path.GetDirectoryName(store.FilePath),"Orbit Library help");}
  void MoveSelection(int dx,int dy){if(dy<0){ConsoleFocus=true;rail.SetSelected(Math.Max(0,nav.SelectedIndex-3));return;}if(dy>0){ConsoleFocus=false;return;}if(ConsoleFocus){nav.SelectedIndex=Math.Max(3,Math.Min(nav.Items.Count-1,Math.Max(3,nav.SelectedIndex)+dx));return;}if(cards.Count>0)Select(Math.Max(0,Math.Min(cards.Count-1,selected+dx)));}
  protected override bool ProcessCmdKey(ref Message msg,Keys keyData){if(keyData==Keys.F11){ToggleFullscreen();return true;}if(keyData==Keys.F1){Help();return true;}if(keyData==(Keys.Control|Keys.K)){search.Focus();return true;}if(search.Focused){if(keyData==Keys.Escape){search.Clear();grid.Focus();return true;}return base.ProcessCmdKey(ref msg,keyData);}if(keyData==Keys.Enter){if(ConsoleFocus){ConsoleFocus=false;if(cards.Count>0)Select(0);}else Play();return true;}if(keyData==Keys.F){Favorite();return true;}if(keyData==Keys.Left){MoveSelection(-1,0);return true;}if(keyData==Keys.Right){MoveSelection(1,0);return true;}if(keyData==Keys.Up){MoveSelection(0,-1);return true;}if(keyData==Keys.Down){MoveSelection(0,1);return true;}return base.ProcessCmdKey(ref msg,keyData);}
  void PollPad(){if(!ContainsFocus||launching||Form.ActiveForm!=this)return;try{PadState state=new PadState();bool found=false;for(uint i=0;i<4;i++)if(XInputGetState(i,out state)==0){found=true;break;}if(!found){previous=0;return;}ushort b=state.Gamepad.Buttons,edge=(ushort)(b&~previous);if((edge&0x1000)!=0){if(ConsoleFocus){ConsoleFocus=false;if(cards.Count>0)Select(0);}else Play();}if((edge&0x4000)!=0)Favorite();if((edge&0x10)!=0)ToggleFullscreen();if((edge&0x100)!=0)nav.SelectedIndex=Math.Max(0,nav.SelectedIndex-1);if((edge&0x200)!=0)nav.SelectedIndex=Math.Min(nav.Items.Count-1,nav.SelectedIndex+1);if((DateTime.UtcNow-lastMove).TotalMilliseconds>180){int dx=(b&8)!=0||state.Gamepad.LX>17000?1:(b&4)!=0||state.Gamepad.LX< -17000?-1:0;int dy=(b&2)!=0||state.Gamepad.LY< -17000?1:(b&1)!=0||state.Gamepad.LY>17000?-1:0;if(dx!=0||dy!=0){MoveSelection(dx,dy);lastMove=DateTime.UtcNow;}}previous=b;}catch(DllNotFoundException){padTimer.Stop();}}
 }
 public static class Program {
  [STAThread] public static int Main(string[] args){try{if(args.Length>0&&args[0]=="--self-test")return SelfTest();Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);string dir=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"data");if(args.Length>1&&(args[0]=="--preview"||args[0]=="--preview-fullscreen"))dir=Path.Combine(Path.GetDirectoryName(args[1]),"preview-data");if(args.Length==0){using(var intro=new IntroForm(true))Application.Run(intro);}if(args.Length>1&&args[0]=="--preview-intro"){using(var intro=new IntroForm()){intro.FixedTime=1.65;intro.Show();Application.DoEvents();using(var picture=new Bitmap(intro.Width,intro.Height)){intro.DrawToBitmap(picture,intro.ClientRectangle);picture.Save(args[1]);}intro.Close();}return 0;}var store=new Store(dir);using(var form=new MainForm(store,args.Length==0||(args.Length>0&&args[0]=="--preview-fullscreen"))){if(args.Length>1&&(args[0]=="--preview"||args[0]=="--preview-fullscreen")){var timer=new Timer{Interval=2600};timer.Tick+=(s,e)=>{timer.Stop();using(var image=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(image,new Rectangle(0,0,form.Width,form.Height));image.Save(args[1]);if(args[0]=="--preview-fullscreen")File.WriteAllText(args[1]+".check.txt", "Fullscreen="+(form.FormBorderStyle==FormBorderStyle.None&&form.Bounds==Screen.FromControl(form).Bounds)+"; TopMost released="+!form.TopMost+"; Bounds="+form.Bounds);}form.Close();};timer.Start();}Application.Run(form);}return 0;}catch(Exception e){if(args.Length>0)Console.Error.WriteLine(e);else MessageBox.Show(e.Message,"Orbit Library could not start");return 1;}}
  static void Assert(bool condition,string message){if(!condition)throw new Exception("TEST FAILED: "+message);}
  static int SelfTest(){string root=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"OrbitTest-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);try{var store=new Store(root);string romdir=Path.Combine(root,"ROMs with spaces");Directory.CreateDirectory(romdir);File.WriteAllText(Path.Combine(romdir,"Example.nes"),"");File.WriteAllText(Path.Combine(romdir,"cover.png"),"");Assert(store.Scan("nes",romdir)==1,"ROM filtering");Assert(store.Scan("nes",romdir)==0,"duplicate scan");store.Data.Games[0].Favorite=true;store.Save();store.Save();var reload=new Store(root);Assert(reload.Data.Games.Count==1&&reload.Data.Games[0].Favorite,"persist favorites");File.WriteAllText(Path.Combine(romdir,"Disc.cue"),"FILE \"Disc.bin\" BINARY");File.WriteAllText(Path.Combine(romdir,"Disc.bin"),"");Assert(reload.Scan("ps2",romdir)==0,"cue track suppression");Assert(reload.Scan("psx",romdir)==1,"cue import");var game=reload.Data.Games[0];var p=new PlatformSettings{Exe=Application.ExecutablePath,Args="\"{rom}\" --name \"{name}\" --dir \"{romdir}\""};var info=Launch.Build(game,p);Assert(info.Arguments.Contains("\""+game.Path+"\""),"quoted ROM argument");Assert(info.Arguments.Contains("\"Example\""),"name substitution");Assert(info.WorkingDirectory==Path.GetDirectoryName(Application.ExecutablePath),"working directory");p.Exe=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"System32","cmd.exe");p.Args="/c exit 0";using(var process=Process.Start(Launch.Build(game,p))){Assert(process.WaitForExit(5000)&&process.ExitCode==0,"actual process launch");}p.Exe=Application.ExecutablePath;p.RetroArch=true;p.Core=Path.Combine(root,"test core.dll");File.WriteAllText(p.Core,"");Assert(Launch.Build(game,p).Arguments.StartsWith("-L \""+p.Core+"\""),"RetroArch core argument");p.Core="missing";bool failed=false;try{Launch.Build(game,p);}catch(Exception){failed=true;}Assert(failed,"missing core rejection");Assert(Catalog.All.Select(s=>s.Id).Distinct().Count()==Catalog.All.Count,"unique platform ids");Application.EnableVisualStyles();using(var form=new MainForm(reload,false)){form.PauseAnimation();form.Show();Application.DoEvents();Assert(form.Controls.Count==1,"UI initialization");form.VerifyNavigation();using(var screenshot=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(screenshot,new Rectangle(0,0,form.Width,form.Height));}form.Close();}using(var setup=new SetupDialog(reload,"psx")){setup.Show();Application.DoEvents();setup.Close();}using(var intro=new IntroForm()){Application.Run(intro);Assert(intro.Duration>=3&&intro.Duration<4.5,"three-second intro duration");}File.WriteAllText(reload.FilePath,"broken JSON");var recovered=new Store(root);Assert(recovered.Warning.Length>0&&recovered.Data.Games.Count==0,"corrupt data recovery");File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"self-test-result.txt"),"PASS: filtering, duplicate scans, persistence, CUE track suppression, launch paths, actual subprocess launch, RetroArch arguments, missing core validation, platform IDs, library UI, console navigation, favorites and search filters, fullscreen bounds and restore, all 29 logo assets, animated space background, setup UI, three-second intro duration, corrupt data backup.\r\n");return 0;}finally{Directory.Delete(root,true);}}
 }
}










