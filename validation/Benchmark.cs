using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using OrbitLibrary;

public class OrbitBench {
 [STAThread] public static int Main(string[] args){
  string report=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"benchmark.json");
  try{
   Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
   int count=args.Length>0?int.Parse(args[0]):1000;
   string folder=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"benchmark-data");
   var store=new Store(folder);store.Data.Games.Clear();
   for(int i=0;i<count;i++)store.Data.Games.Add(new Game{Title="Benchmark Game "+i.ToString("D5"),System=Catalog.All[i%29].Id,Path=Path.Combine(folder,i+".rom"),Favorite=i%3==0});
   var result=new Dictionary<string,object>();result["games"]=count;result["resolution"]="2560x1440";
   var sw=Stopwatch.StartNew();
   using(var form=new MainForm(store,false)){
    form.PauseAnimation();result["construct_ms"]=sw.Elapsed.TotalMilliseconds;
    form.ClientSize=new Size(2560,1440);sw.Restart();form.Show();Application.DoEvents();result["show_ms"]=sw.Elapsed.TotalMilliseconds;
    using(var shot=new Bitmap(form.Width,form.Height)){
     form.DrawToBitmap(shot,new Rectangle(0,0,form.Width,form.Height));
     sw.Restart();for(int i=0;i<5;i++)form.DrawToBitmap(shot,new Rectangle(0,0,form.Width,form.Height));
     result["full_snapshot_ms"]=sw.Elapsed.TotalMilliseconds/5;
     shot.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"benchmark-preview.png"));
    }
    var intervals=new List<double>();var frameTimes=new List<double>();
    var animation=(Timer)typeof(MainForm).GetField("animation",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(form);
    var canvas=form.Controls[0];int paints=0;canvas.Paint+=(s,e)=>paints++;
    var bg=(LivingBackground)canvas;bg.Motion=true;sw.Restart();for(int i=0;i<20;i++){bg.FixedTime=10+i*.05;bg.Advance();}result["background_update_ms"]=sw.Elapsed.TotalMilliseconds/20;bg.FixedTime=-1;
    WindowFocus.Front(form);form.Controls[0].Focus();int activeSamples=0,totalSamples=0;
    var clock=Stopwatch.StartNew();double last=0;var process=Process.GetCurrentProcess();var cpu=process.TotalProcessorTime;
    var monitor=new Timer{Interval=16};
    monitor.Tick+=(s,e)=>{double now=clock.Elapsed.TotalMilliseconds;totalSamples++;if(form.ContainsFocus)activeSamples++;if(last>0)intervals.Add(now-last);last=now;if(now>=4000){monitor.Stop();animation.Stop();process.Refresh();result["active_samples"]=activeSamples;result["total_samples"]=totalSamples;result["animation_wall_ms"]=now;result["animation_cpu_ms"]= (process.TotalProcessorTime-cpu).TotalMilliseconds;result["background_paints"]=paints;intervals.Sort();result["message_loop_p95_ms"]=intervals.Count>0?intervals[(int)((intervals.Count-1)*.95)]:0;result["private_memory_mb"]=process.PrivateMemorySize64/1048576.0;form.Close();}};
    typeof(LivingBackground).GetField("Motion").SetValue(canvas,true);animation.Start();monitor.Start();Application.Run(form);monitor.Dispose();
   }
   File.WriteAllText(report,new JavaScriptSerializer().Serialize(result));return 0;
  }catch(Exception e){File.WriteAllText(report,e.ToString());return 1;}
 }
}
