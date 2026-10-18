using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using FrameForge.Core;
using Microsoft.Win32;

namespace FrameForge.App;

public sealed partial class MainWindow
{
    static Grid Columns(params UIElement[] items)
    {
        var grid=new Grid();
        for(int i=0;i<items.Length;i++) { grid.ColumnDefinitions.Add(new()); Grid.SetColumn(items[i],i); grid.Children.Add(items[i]); }
        return grid;
    }

    static Border Metric(string label,string value,string note,double? percent=null,string color="#5EE0BA")
    {
        var panel=Panel(Text(label.ToUpperInvariant(),11,"#BDCCE0",true),Text(value,28,color,true));
        if(percent is {} p) panel.Children.Add(new ProgressBar { Value=Math.Clamp(p,0,100),Maximum=100,Foreground=Brush(color),Height=5,Margin=new(0,8,0,10) });
        panel.Children.Add(Text(note,12,"#BDCCE0")); return Card(panel);
    }

    void Dashboard()
    {
        var h=vm.Hardware; var history=engine.History;
        var outstanding=history.Count(x=>x.Status is "Prepared" or "Failed" or "Restoring" or "Restore failed");
        var hero=Panel(Text(scanned?"Make room for better play.":"Let's get your PC ready.",30,"#F1F5FC",true),
            Text(scanned?"Choose a profile below, or browse individual tweaks. Your original settings stay in Recovery.":"A quick hardware scan finds the choices that fit your PC. It changes no settings.",14,"#BDCCE0"),
            Row(PrimaryButton("Browse gaming tweaks",OpenFeatures),Button("Check game setup",()=>Go("Gaming")),Button("See live stats",()=>Go("Monitor"))));
        body.Children.Add(Card(hero));
        body.Children.Add(Columns(
            Card(Panel(Text("LESS BACKGROUND WORK",11,"#5EE0BA",true),Text("Low-end PC",23,"#F1F5FC",true),Text("Choose a few useful changes, then apply them together. Undo the whole profile whenever you want.",13,"#BDCCE0"),PrimaryButton("Build my profile",()=>Go("Low-end PC")))),
            Card(Panel(Text("LESS WORK FOR YOUR GRAPHICS",11,"#5EE0BA",true),Text("Potato Settings",23,"#F1F5FC",true),Text("One button for a lower-detail Fortnite or Rust profile. Your original graphics file is backed up first.",13,"#BDCCE0"),PrimaryButton("Choose my game",()=>Go("Potato Settings"))))));
        double? memoryUsed=h.TotalMemory is >0 && h.AvailableMemory is {} available?100d*(h.TotalMemory.Value-available)/h.TotalMemory.Value:null;
        body.Children.Add(Columns(
            Metric("CPU utilization",h.CpuPercent is {} c?$"{c:F0}%":"—","During the last full scan",h.CpuPercent),
            Metric("Memory in use",memoryUsed is {} m?$"{m:F0}%":"—",scanned?$"{GiB(h.AvailableMemory)} available":"Run a scan to read memory",memoryUsed),
            Metric("Restore journal",history.Count(x=>x.Status=="Applied").ToString(),outstanding>0?$"{outstanding} records need review":"Verified changes with undo",null,outstanding>0?"#FFD08A":"#5EE0BA")));
        if(outstanding>0) body.Children.Add(Card(Panel(Text("Review interrupted or failed changes",18,"#FFD08A",true),Text("The journal retained the original values. Review the records before applying more changes."),Button("Open Restore",()=>Go("Restore")))));

        var graphics=h.Gpus.Count>0?string.Join("\n",h.Gpus.Select(g=>g.Name)):"Scan to identify your graphics";
        body.Children.Add(Columns(
            Card(Panel(Text("PROCESSOR & MEMORY",11,"#BDCCE0",true),Text(scanned?h.Cpu:"Waiting for your first scan",19,"#F1F5FC",true),Text(scanned?$"{Number(h.Cores)} cores · {Number(h.Threads)} threads\n{GiB(h.TotalMemory)} installed RAM":"Hardware information stays on this PC.",13,"#BDCCE0"))),
            Card(Panel(Text("GRAPHICS",11,"#BDCCE0",true),Text(graphics,19,"#F1F5FC",true),Text(scanned?string.Join("\n",h.Gpus.Select(g=>"Driver "+g.Driver)):"Vendor-specific guidance appears after scanning.",13,"#BDCCE0")))));

        if(!scanned)
        {
            Add("A deliberate workflow","1. Scan your hardware  →  2. Review a setting and its tradeoffs  →  3. Measure the result\nSupported setting changes keep original values in Restore. File cleanup and app removal require their own review.");
            return;
        }
        var findings=HealthAdvisor.Analyze(h);
        body.Children.Add(Text("WHAT NEEDS ATTENTION",12,"#BDCCE0",true));
        if(findings.Count==0) Add("No pressure flags in this snapshot","Available measurements did not cross the review thresholds. Open Monitor to watch CPU and RAM during your workload; a snapshot does not establish gaming performance.");
        foreach(var finding in findings.Take(4))
            body.Children.Add(Card(Panel(Text(finding.Title,18,finding.Attention?"#FFD08A":"#5EE0BA",true),Text(finding.Detail,14,"#BDCCE0"),Button("Review controls",()=>Run(()=>Handle(Catalog.All.Single(o=>o.Id==finding.OperationId)))))));
        if(h.StorageVolumes.Count>0)
        {
            body.Children.Add(Text("STORAGE",12,"#BDCCE0",true));
            body.Children.Add(Columns(h.StorageVolumes.Take(3).Select(v=>Metric(v.Name,$"{v.FreeBytes/1073741824d:F0} GiB free",$"{v.TotalBytes/1073741824d:F0} GiB total",v.TotalBytes>0?100d*(v.TotalBytes-v.FreeBytes)/v.TotalBytes:null,v.TotalBytes>0&&v.FreeBytes/(double)v.TotalBytes<.1?"#FFD08A":"#5EE0BA")).ToArray()));
        }
        var recent=history.TakeLast(3).Reverse().ToList();
        var activity=Panel(Text("Recent activity",19,"#F1F5FC",true));
        if(recent.Count==0) activity.Children.Add(Text("No setting changes yet. Every supported change starts with a preview.",13,"#BDCCE0"));
        foreach(var record in recent) activity.Children.Add(Text($"{record.Time.ToLocalTime():g}   {record.Name}  ·  {record.Status}",13,"#BDCCE0"));
        activity.Children.Add(Row(Button("View restore history",()=>Go("Restore")),Button("Export PC details",()=>Run(ExportSystemReport)))); body.Children.Add(Card(activity));
        if(h.Warnings.Count>0) body.Children.Add(new Expander { Header=$"{h.Warnings.Count} scan sources unavailable — view details",Content=Text(string.Join("\n",h.Warnings),13,"#FFD08A"),Margin=new(0,4,12,16) });
    }

    Task ExportSystemReport()
    {
        RequireScan();
        var dialog=new SaveFileDialog { FileName=$"FrameForge-system-{DateTime.Now:yyyyMMdd-HHmm}.json",Filter="System report (JSON)|*.json" };
        if(dialog.ShowDialog(this)==true)
        {
            File.WriteAllText(dialog.FileName,JsonSerializer.Serialize(new { Version="0.7.0",Hardware=vm.Hardware,Findings=HealthAdvisor.Analyze(vm.Hardware) },new JsonSerializerOptions {WriteIndented=true}));
            vm.Status="System report exported. It contains hardware names and local network details; review before sharing.";
        }
        return Task.CompletedTask;
    }
}
