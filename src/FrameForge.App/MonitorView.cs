using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using FrameForge.Core;
using FrameForge.Windows;
using Microsoft.Win32;

namespace FrameForge.App;

public sealed partial class MainWindow
{
    DispatcherTimer? liveTimer;
    Action? liveStopped;
    string monitorTab="Live resources";
    readonly List<ResourceSample> liveSamples=[];
    FrameTrace? importedTrace;
    string importedSource="";
    string benchmarkLabel="",benchmarkNotes="";
    string importedApplication="";

    void StopLiveMonitor()
    {
        liveTimer?.Stop(); liveTimer=null;
        liveStopped?.Invoke(); liveStopped=null;
    }

    void Monitor()
    {
        var tabs=Row(); var content=new StackPanel();
        foreach(var label in new[]{"Live resources","Benchmarks","Hardware inventory"})
        {
            var button=Button(label,()=> { StopLiveMonitor(); monitorTab=label; Render(); });
            button.Background=Brush(monitorTab==label?"#27534F":"#23334C"); tabs.Children.Add(button);
        }
        body.Children.Add(tabs); body.Children.Add(content);
        if(monitorTab=="Live resources") LiveResources(content);
        else if(monitorTab=="Benchmarks") BenchmarkLibrary(content);
        else HardwareInventory(content);
    }

    void LiveResources(StackPanel content)
    {
        content.Children.Add(Card(Panel(Text("Watch your workload",23,"#F1F5FC",true),Text("CPU and physical memory, sampled once a second while this view is open. Start monitoring, then run your workload. The last 120 samples stay in memory until you close the app.",14,"#BDCCE0"))));
        var cpu=Text("—",32,"#5EE0BA",true); var ram=Text("—",32,"#99B7FF",true); var available=Text("Waiting for a sample",13,"#BDCCE0");
        content.Children.Add(Columns(Card(Panel(Text("CPU UTILIZATION",11,"#BDCCE0",true),cpu,Text("Across all logical processors",12,"#BDCCE0"))),Card(Panel(Text("PHYSICAL MEMORY IN USE",11,"#BDCCE0",true),ram,available))));
        var chart=new TraceChart(); chart.SetData([],maximum:100,units:"%"); var state=Text("Paused. Start monitoring to collect measurements.",13,"#BDCCE0");
        content.Children.Add(Card(Panel(Text("● CPU     ● Memory (blue)",13,"#BDCCE0"),chart,state)));
        Button? start=null;
        void Draw()
        {
            if(liveSamples.Count==0) return;
            var latest=liveSamples[^1]; cpu.Text=latest.CpuPercent is {} c?$"{c:F1}%":"Sampling…"; ram.Text=$"{latest.UsedMemoryPercent:F1}%";
            available.Text=$"{GiB(latest.AvailableMemory)} available / {GiB(latest.TotalMemory)} total";
            var measured=liveSamples.Where(x=>x.CpuPercent.HasValue).ToList();
            chart.SetData(measured.Select(x=>x.CpuPercent!.Value),measured.Select(x=>x.UsedMemoryPercent),100,"%",$"{liveSamples.Count} samples · 1 second intervals →");
        }
        start=PrimaryButton("▶  Start monitoring",()=>
        {
            if(liveTimer!=null) { StopLiveMonitor(); return; }
            var sampler=new ResourceSampler();
            start!.Content="Ⅱ  Pause monitoring";
            liveStopped=()=> { start.Content="▶  Start monitoring"; state.Text=$"Paused · {liveSamples.Count} retained samples. Monitoring stops when you leave this page or run another action."; vm.Status="Live monitoring paused. Collected samples are retained for this session."; };
            vm.Status="Live CPU/RAM monitoring active. Samples remain local; no settings are changed.";
            void Sample()
            {
                try
                {
                    var sample=sampler.Read();
                    // CPU needs two consecutive counters. Do not bridge across a paused interval.
                    if(sample.CpuPercent.HasValue) { liveSamples.Add(sample); if(liveSamples.Count>120) liveSamples.RemoveAt(0); Draw(); }
                    else { ram.Text=$"{sample.UsedMemoryPercent:F1}%"; available.Text=$"{GiB(sample.AvailableMemory)} available"; cpu.Text="Sampling…"; }
                    state.Text=$"Live · {sample.Time.ToLocalTime():T} · {liveSamples.Count}/120 samples";
                }
                catch(Exception e) { StopLiveMonitor(); vm.Status="Monitoring stopped: "+e.Message; }
            }
            liveTimer=new DispatcherTimer { Interval=TimeSpan.FromSeconds(1) }; liveTimer.Tick+=(_,_)=>Sample(); Sample(); liveTimer?.Start();
        });
        content.Children.Insert(1,Row(start,Button("Export resource samples",()=>Run(()=>
        {
            if(liveSamples.Count==0) throw new InvalidOperationException("Collect some resource samples first.");
            var dialog=new SaveFileDialog { FileName="FrameForge-resources.csv",Filter="Resource samples (CSV)|*.csv" };
            if(dialog.ShowDialog(this)==true)
            {
                var csv=new StringBuilder("TimestampUtc,CpuPercent,MemoryUsedPercent,AvailableBytes\n");
                foreach(var s in liveSamples) csv.AppendLine(FormattableString.Invariant($"{s.Time:O},{s.CpuPercent:F3},{s.UsedMemoryPercent:F3},{s.AvailableMemory}"));
                File.WriteAllText(dialog.FileName,csv.ToString()); vm.Status=$"Exported {liveSamples.Count} measured resource samples.";
            }
            return Task.CompletedTask;
        })),Button("Clear samples",()=> { StopLiveMonitor(); liveSamples.Clear(); cpu.Text="—"; ram.Text="—"; available.Text="Waiting for a sample"; chart.SetData([],maximum:100,units:"%"); state.Text="Samples cleared. Start monitoring for a new capture."; })));
        Draw();
        content.Children.Add(Text("These are system resource measurements. They do not measure FPS, GPU load, temperature, or the cause of a game bottleneck. On systems with more than 64 logical processors, Windows may report CPU time for the calling processor group.",12,"#BDCCE0"));
    }

    sealed record RunChoice(BenchmarkRun Run)
    {
        public override string ToString()=>$"{(string.IsNullOrWhiteSpace(Run.Label)?Run.Source:Run.Label)} · {Run.Time.ToLocalTime():g} · {Run.Stats.AverageFps:F1} FPS";
    }

    void BenchmarkLibrary(StackPanel content)
    {
        content.Children.Add(Card(Panel(Text("Did the change help?",23,"#F1F5FC",true),Text("Import a PresentMon recording or a FrameTimeMs file. Save a before run, change one setting, then import an after run from the same game scene.",14,"#BDCCE0"))));
        var label=new TextBox { Text=benchmarkLabel,Tag="Run label — game, scene, settings",MaxLength=160 };
        var notes=new TextBox { Text=benchmarkNotes,Tag="Test conditions: resolution, driver, cap, renderer…",MaxLength=2000 };
        label.TextChanged+=(_,_)=>benchmarkLabel=label.Text; notes.TextChanged+=(_,_)=>benchmarkNotes=notes.Text;
        var metrics=new StackPanel(); var traceChart=new TraceChart(); var traceInfo=Text("Import a capture to see its frame-time trace.",13,"#BDCCE0");
        var history=store.Load<List<BenchmarkRun>>("benchmarks.json",()=>[]);
        var saved=new ComboBox { MinWidth=280,MaxWidth=750,HorizontalAlignment=HorizontalAlignment.Left };
        var comparison=Text("Choose a saved baseline, then compare it with your imported capture.",13,"#BDCCE0");
        void RefreshRuns() { saved.ItemsSource=history.AsEnumerable().Reverse().Select(x=>new RunChoice(x)).ToList(); if(history.Count>0) saved.SelectedIndex=0; }
        void DrawTrace()
        {
            metrics.Children.Clear(); if(importedTrace is not {} trace) return;
            metrics.Children.Add(Columns(Metric("Average FPS",$"{trace.Stats.AverageFps:F1}","Overall pace in this recording"),Metric("Slowest 1% FPS",$"{trace.Stats.LowOnePercentFps:F1}","Helps reveal uneven frame delivery"),Metric("Slow-frame time",$"{trace.Stats.P99FrameMs:F2} ms","99th percentile · lower is better")));
            traceChart.SetData(trace.Milliseconds,xCaption:$"{trace.Stats.Frames:N0} frames · {trace.DurationSeconds:F1} seconds →");
            traceInfo.Text=$"{importedSource} · {trace.Metric}\nMedian {trace.MedianMs:F2} ms · {trace.FramesOverBudget(1000d/60):N0} frames slower than 60 FPS · {trace.FramesOverBudget(50):N0} over 50 ms\nPeaks stay visible in the chart. Imported app/CPU intervals are not displayed FPS or input-latency measurements.";
        }
        content.Children.Add(Row(PrimaryButton("Import frame-time CSV",()=>Run(async ()=>
        {
            var dialog=new OpenFileDialog { Filter="Frame durations (CSV)|*.csv",CheckFileExists=true };
            if(dialog.ShowDialog(this)!=true) return;
            var file=new FileInfo(dialog.FileName);
            if(file.Length>20*1024*1024) throw new InvalidDataException("CSV exceeds the 20 MiB import limit.");
            var csv=await File.ReadAllTextAsync(file.FullName);
            var captures=await Task.Run(()=>BenchmarkImport.Read(csv));var selected=ChooseBenchmarkCapture(captures);if(selected==null)return;
            var trace=selected.ToTrace();importedTrace=trace;importedApplication=selected.Application;importedSource=file.Name+(selected.Application.Length==0?"":" · "+selected.Application+" · PID "+selected.ProcessId+" · stream "+selected.SwapChain);comparison.Text="New capture loaded. Compare against a saved baseline below.";
            DrawTrace(); vm.Status=$"Imported {trace.Stats.Frames:N0} measured frames. Add a label and save this run to your library."+(selected.InitialRowsSkipped>0?" One initial zero interval was excluded.":"");
        })),Button("Import help",()=>MessageBox.Show(this,"PresentMon: import the original CSV. Choose one game/process/presentation stream if the file contains several. Supported timing columns: MsBetweenPresents (including the older ms spelling), or FrameTime from PresentMon 2.x. These measure different intervals and cannot be mixed in a comparison.\n\nSimple format:\nFrameTimeMs\n16.67\n17.20\n…\n\nAt least 100 valid frames per stream, at most 1,000,000 total rows and 20 MiB. A first zero interval in each PresentMon stream is counted and excluded; other invalid rows stop the import. No FPS-sample conversion or displayed/input-latency claims.","Import a measured recording"))));
        content.Children.Add(Columns(label,notes)); content.Children.Add(metrics); content.Children.Add(Card(Panel(traceInfo,traceChart)));
        content.Children.Add(Row(Button("Save imported run",()=>Run(()=>
        {
            if(importedTrace is not {} trace) throw new InvalidOperationException("Import a measured capture first.");
            RequireScan();
            history.Add(new(DateTimeOffset.UtcNow,importedSource,trace.Stats,vm.Hardware,label.Text.Trim(),notes.Text.Trim(),trace.DurationSeconds,trace.Metric,importedApplication));
            history=history.TakeLast(100).ToList(); store.Save("benchmarks.json",history); RefreshRuns();
            vm.Status="Saved benchmark statistics, test notes and the latest hardware snapshot. The library keeps the latest 100 runs; raw frames remain in your source CSV.";
            return Task.CompletedTask;
        })),Button("Export benchmark library",()=>Run(()=>
        {
            if(history.Count==0) throw new InvalidOperationException("Save a benchmark run first.");
            var dialog=new SaveFileDialog { FileName="FrameForge-benchmarks.csv",Filter="Benchmark statistics (CSV)|*.csv" };
            if(dialog.ShowDialog(this)==true)
            {
                static string Cell(string s)
                {
                    // Quoting alone does not stop spreadsheet formula evaluation.
                    if(s.TrimStart().StartsWith('=')||s.TrimStart().StartsWith('+')||s.TrimStart().StartsWith('-')||s.TrimStart().StartsWith('@')) s="'"+s;
                    return "\""+s.Replace("\"","\"\"")+"\"";
                }
                var csv=new StringBuilder("TimestampUtc,Label,Source,Frames,AverageFps,LowOnePercentFps,P99FrameMs,DurationSeconds,Metric,Application,PercentileMethod,Notes\n");
                foreach(var r in history) csv.AppendLine(FormattableString.Invariant($"{r.Time:O},{Cell(r.Label)},{Cell(r.Source)},{r.Stats.Frames},{r.Stats.AverageFps:F4},{r.Stats.LowOnePercentFps:F4},{r.Stats.P99FrameMs:F4},{r.DurationSeconds:F3},{Cell(r.Metric)},{Cell(r.Application)},{Cell(r.Stats.PercentileMethod)},{Cell(r.Notes)}"));
                File.WriteAllText(dialog.FileName,csv.ToString()); vm.Status=$"Exported {history.Count} saved benchmark runs.";
            }
            return Task.CompletedTask;
        }))));
        content.Children.Add(Text("SAVED BASELINES",12,"#BDCCE0",true)); content.Children.Add(saved);
        content.Children.Add(Button("Compare imported capture to selected baseline",()=>Run(()=>
        {
            if(importedTrace is not {} trace||saved.SelectedItem is not RunChoice choice) throw new InvalidOperationException("Import a capture and select a saved baseline first.");
            var before=choice.Run.Stats; var after=trace.Stats;
            if(choice.Run.Metric!=trace.Metric)throw new InvalidOperationException("These captures measure different frame intervals. Import matching formats for a fair comparison.");
            if(choice.Run.Application.Length>0&&importedApplication.Length>0&&!choice.Run.Application.Equals(importedApplication,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("These captures are from different applications. Choose a baseline from the same game.");
            static string Delta(double before,double after)=>before>0&&double.IsFinite(before)?$"{(after/before-1)*100:+0.0;-0.0;0.0}%":"Unavailable";
            comparison.Text=$"{choice.Run.Label} ({choice.Run.Source}) → {importedSource}\nAverage FPS: {before.AverageFps:F1} → {after.AverageFps:F1} ({Delta(before.AverageFps,after.AverageFps)})\n1% low FPS: {before.LowOnePercentFps:F1} → {after.LowOnePercentFps:F1} ({Delta(before.LowOnePercentFps,after.LowOnePercentFps)})\nSlow-frame time: {(before.PercentileMethod==after.PercentileMethod?$"{before.P99FrameMs:F2} → {after.P99FrameMs:F2} ms ({Delta(before.P99FrameMs,after.P99FrameMs)}; lower is better)":"Not compared: this older baseline used a different percentile method.")}\nBaseline notes: {choice.Run.Notes}\nCurrent notes: {notes.Text}\nThese are descriptive differences. Repeat runs under matching conditions before attributing a change to an optimization.";
            return Task.CompletedTask;
        })));
        saved.SelectionChanged+=(_,_)=> { if(saved.SelectedItem is RunChoice choice) comparison.Text=$"Saved {choice.Run.Time.ToLocalTime():g} · {choice.Run.Stats.Frames:N0} frames\n{choice.Run.Notes}\n{choice.Run.Hardware.Cpu}\n"+string.Join(" / ",choice.Run.Hardware.Gpus.Select(g=>$"{g.Name}, driver {g.Driver}")); };
        content.Children.Add(Card(comparison)); RefreshRuns(); DrawTrace();
        if(history.Count==0) content.Children.Add(Text("Your benchmark library is empty. Import a capture, label the conditions, and save it as your first baseline.",13,"#BDCCE0"));
    }

    void HardwareInventory(StackPanel content)
    {
        var h=vm.Hardware;
        void Section(string title,string detail)=>content.Children.Add(Card(Panel(Text(title,18,"#F1F5FC",true),Text(detail,13,"#BDCCE0"))));
        Section("Latest full scan",scanned?$"{h.CapturedAt.ToLocalTime():g}\n{h.Os}\n{h.Cpu}\n{Number(h.Cores)} cores / {Number(h.Threads)} threads · {h.Architecture}\n{h.Board}\nBIOS {h.Bios}\nPower: {h.PowerPlan}":"Run Scan PC to read your hardware inventory.");
        content.Children.Add(Button("Export system report",()=>Run(ExportSystemReport)));
        var comparison=Text("Save a full-scan baseline, then scan again under comparable conditions to compare resource snapshots.",13,"#BDCCE0");
        content.Children.Add(Row(Button("Save system baseline",()=>Run(()=>
        {
            RequireScan(); store.Save("baseline.json",h); comparison.Text="System baseline saved. Repeat the same idle/workload conditions before the next full scan."; return Task.CompletedTask;
        })),Button("Compare system snapshots",()=>Run(()=>
        {
            RequireScan(); var baseline=store.Load<HardwareSnapshot?>("baseline.json",()=>null)??throw new InvalidOperationException("Save a system baseline first.");
            comparison.Text=$"{baseline.CapturedAt.ToLocalTime():g} → {h.CapturedAt.ToLocalTime():g}\nCPU: {Number(baseline.CpuPercent)} → {Number(h.CpuPercent)} %\nAvailable RAM: {GiB(baseline.AvailableMemory)} → {GiB(h.AvailableMemory)}\nProcesses: {Number(baseline.ProcessCount)} → {Number(h.ProcessCount)}\nStartup entries: {baseline.Startup.Count} → {h.Startup.Count}\nResource changes alone do not establish FPS improvements.";
            return Task.CompletedTask;
        })))); content.Children.Add(Card(comparison));
        Section("Memory modules",Join(h.MemoryModules)); Section("Graphics & displays",Join(h.Gpus.Select(g=>$"{g.Name} · driver {g.Driver}").Concat(h.Displays)));
        Section("Storage",Join(h.Disks.Concat(h.Volumes))); Section("Top process working sets",Join(h.Processes)); Section("Startup inventory",Join(h.Startup)); Section("Network",Join(h.Network)); Section("Security providers",Join(h.Security));
        if(h.Warnings.Count>0) Section("Unavailable sources",Join(h.Warnings));
        Section("Measurement limits","Process working sets are not private memory. Live GPU utilization, VRAM, temperatures, throttling, in-game FPS and per-process rates are unavailable. Driver versions are reported values; update availability is not checked.");
    }
}
