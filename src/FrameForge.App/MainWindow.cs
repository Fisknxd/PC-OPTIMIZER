using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;
using FrameForge.Core;
using FrameForge.Windows;

namespace FrameForge.App;

public sealed partial class MainWindow : Window
{
    readonly JsonStore store;
    readonly WindowsSettings system=new();
    readonly OperationEngine engine;
    readonly AppViewModel vm=new();
    readonly StackPanel body=new();
    readonly Grid workspace=new();
    readonly TextBlock heading=new() { FontSize=28,FontWeight=FontWeights.SemiBold };
    readonly TextBox search=new() { Width=330,Tag="Search features, categories, or GPU…",ToolTip="Search features, descriptions, categories and GPU vendors",VerticalContentAlignment=VerticalAlignment.Center };
    readonly Dictionary<string,Button> navigation=[];
    string page="Dashboard";
    string query="",filter="All";
    Preferences preferences;
    bool scanned;
    public MainWindow(JsonStore store,bool autoScan=true)
    {
        this.store=store; engine=new(system,store); preferences=store.Load("preferences.json",()=>new Preferences());
        SetResourceReference(BackgroundProperty,"CanvasBrush"); SetResourceReference(ForegroundProperty,"TextBrush");
        FontFamily=new FontFamily("Segoe UI"); FontSize=15;
        UseLayoutRounding=true; SnapsToDevicePixels=true;
        Title="FrameForge 0.1.1 • Windows gaming toolkit"; Width=1280; Height=850; MinWidth=940; MinHeight=650; WindowStartupLocation=WindowStartupLocation.CenterScreen; DataContext=vm;
        var shell=new Grid(); shell.ColumnDefinitions.Add(new(){Width=new(225)}); shell.ColumnDefinitions.Add(new()); Content=shell;
        var side=new DockPanel { Background=Brush("#101A2B"),Margin=new(0) }; shell.Children.Add(side);
        var brand=new StackPanel { Margin=new(22,28,16,30) }; brand.Children.Add(Text("◈ FRAMEFORGE",21,"#5EE0BA",true)); brand.Children.Add(Text("A clearer path to play",12,"#8FA2BC")); DockPanel.SetDock(brand,Dock.Top); side.Children.Add(brand);
        var footer=Text("LOCAL BY DESIGN\nNo telemetry. No magic FPS claims.\nv0.1.1 · Engineering preview",11,"#8FA2BC"); footer.Margin=new(22,12,16,22); DockPanel.SetDock(footer,Dock.Bottom); side.Children.Add(footer);
        var nav=new StackPanel { Margin=new(12,0,10,0) }; side.Children.Add(nav);
        foreach(var item in new[]{("Dashboard","◫"),("Optimize","⚡"),("Debloat","▦"),("Gaming","◇"),("Monitor","▥"),("Cleanup","⌁"),("Tools","⊞"),("Quality of Life","◐"),("Restore","↶"),("Settings","⚙")})
        {
            var label=item.Item1; var button=Button(item.Item2+"   "+label,()=>Go(label)); button.Margin=new(0,2,0,2); button.Background=Brush("#101A2B"); navigation[label]=button; nav.Children.Add(button);
        }
        Grid.SetColumn(workspace,1); workspace.Margin=new(30,24,28,16); shell.Children.Add(workspace);
        workspace.RowDefinitions.Add(new(){Height=GridLength.Auto}); workspace.RowDefinitions.Add(new(){Height=GridLength.Auto}); workspace.RowDefinitions.Add(new()); workspace.RowDefinitions.Add(new(){Height=GridLength.Auto});
        var top=new DockPanel(); workspace.Children.Add(top);
        var controls=new StackPanel { Orientation=Orientation.Horizontal }; DockPanel.SetDock(controls,Dock.Right); controls.Children.Add(Button("Scan PC",()=>Run(Scan))); controls.Children.Add(Button("Smart Optimize",()=>Run(Smart))); top.Children.Add(controls); top.Children.Add(heading);
        search.TextChanged+=(_,_)=> { query=search.Text; if(page!="Optimize") Go("Optimize"); else Render(); }; search.SetValue(System.Windows.Automation.AutomationProperties.NameProperty,"Global feature search");
        var searchbar=new DockPanel { Margin=new(0,14,0,12) }; DockPanel.SetDock(search,Dock.Left); searchbar.Children.Add(search); searchbar.Children.Add(Text("Find a setting or hardware recommendation",13,"#BDCCE0")); Grid.SetRow(searchbar,1); workspace.Children.Add(searchbar);
        var scroll=new ScrollViewer { VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Content=body }; Grid.SetRow(scroll,2); workspace.Children.Add(scroll);
        var statusArea=new StackPanel { Margin=new(0,12,0,0) };
        var progress=new ProgressBar { IsIndeterminate=true,Margin=new(0,0,0,8) }; progress.SetBinding(VisibilityProperty,new Binding(nameof(AppViewModel.Busy)) { Converter=new BooleanToVisibilityConverter() }); statusArea.Children.Add(progress);
        var status=new TextBlock { FontSize=13,Foreground=Brush("#BDCCE0"),TextWrapping=TextWrapping.Wrap }; status.SetBinding(TextBlock.TextProperty,new Binding(nameof(AppViewModel.Status))); statusArea.Children.Add(status); Grid.SetRow(statusArea,3); workspace.Children.Add(statusArea);
        Closing+=(_,e)=> { if(vm.Busy) { e.Cancel=true; vm.Status="Wait for the current action to finish before closing; changes may be in progress."; } };
        Go("Dashboard");
        if(autoScan) Loaded+=(_,_)=> { if(!scanned&&!vm.Busy) Run(Scan); };
    }
    static SolidColorBrush Brush(string color)=>new((Color)ColorConverter.ConvertFromString(color));
    static TextBlock Text(string text,double size=14,string color="#E9EFF8",bool bold=false)=>new() { Text=text,FontSize=Math.Max(13,size<16?size+1:size),LineHeight=Math.Max(13,size<16?size+1:size)*1.45,Foreground=Brush(color=="#8FA2BC"?"#BDCCE0":color=="#A7B7CD"?"#C3D0E2":color),FontWeight=bold?FontWeights.SemiBold:FontWeights.Normal,TextWrapping=TextWrapping.Wrap,Margin=new(0,4,0,6),VerticalAlignment=VerticalAlignment.Center };
    static Button Button(string text,Action click) { var b=new Button { Content=text }; b.Click+=(_,_)=>click(); return b; }
    static WrapPanel Row(params UIElement[] items) { var p=new WrapPanel { Orientation=Orientation.Horizontal }; foreach(var i in items) p.Children.Add(i); return p; }
    static Border Card(UIElement content)=>new() { Background=Brush("#141E30"),CornerRadius=new(12),Padding=new(20),Margin=new(0,0,12,14),Child=content };
    static StackPanel Panel(params UIElement[] elements) { var p=new StackPanel(); foreach(var e in elements) p.Children.Add(e); return p; }
    void Add(string title,string detail)=>body.Children.Add(Card(Panel(Text(title,18,"#E9EFF8",true),Text(detail,13,"#A7B7CD"))));
    static string Number(object? value,string suffix="")=>value==null?"Unavailable":value+suffix;
    static string GiB(long? value)=>value is {} v?$"{v/1073741824d:F1} GiB":"Unavailable";
    void Go(string name)
    {
        if(vm.Busy) return; page=name; heading.Text=name;
        foreach(var n in navigation) n.Value.Background=Brush(n.Key==name?"#25413F":"#101A2B"); Render();
    }
    void Render()
    {
        body.Children.Clear();
        switch(page)
        {
            case "Dashboard": Dashboard(); break;
            case "Optimize": Optimize(false); break;
            case "Quality of Life": Optimize(true); break;
            case "Debloat": Debloat(); break;
            case "Cleanup": Cleanup(); break;
            case "Gaming": Gaming(); break;
            case "Monitor": Monitor(); break;
            case "Restore": Restore(); break;
            case "Tools": Tools(); break;
            case "Settings": Settings(); break;
        }
    }
    async void Run(Func<Task> work)
    {
        if(vm.Busy) return; vm.Busy=true;
        // Disable navigation and action surfaces for the duration; status stays visible.
        workspace.IsEnabled=false; foreach(var b in navigation.Values) b.IsEnabled=false;
        try { await work(); }
        catch(Exception e)
        {
            vm.Status="Action failed: "+e.Message;
            MessageBox.Show(this,e.Message,"FrameForge — action failed",MessageBoxButton.OK,MessageBoxImage.Warning);
        }
        finally { vm.Busy=false; workspace.IsEnabled=true; foreach(var b in navigation.Values) b.IsEnabled=true; }
    }
    async Task Scan()
    {
        vm.Status="Reading Windows hardware and configuration… No settings are changed.";
        vm.Hardware=await Scanner.Scan(); scanned=true; Render();
        vm.Status=$"Scan completed {DateTime.Now:t} • {vm.Hardware.Warnings.Count} unavailable sources. See Dashboard for details. No settings changed.";
    }
    void RequireScan() { if(!scanned) throw new InvalidOperationException("Scan this PC first so compatibility can be checked."); }
    void Dashboard()
    {
        var h=vm.Hardware;
        var hero=Panel(Text("Know your system. Change with confidence.",26,"#E9EFF8",true),Text("Review real configuration, understand each tradeoff, and keep a path back. No optimization runs until you approve it.",14,"#A7B7CD"));
        body.Children.Add(Card(hero));
        var stats=new Grid(); for(int i=0;i<3;i++) stats.ColumnDefinitions.Add(new());
        UIElement Metric(string label,string value,string note)=>Card(Panel(Text(label.ToUpperInvariant(),11,"#8FA2BC"),Text(value,27,"#5EE0BA",true),Text(note,12,"#A7B7CD")));
        var cards=new[]{Metric("CPU sample",h.CpuPercent is {} c?$"{c:F0}%":"Unavailable",h.Cpu),Metric("Memory available",GiB(h.AvailableMemory),$"Installed: {GiB(h.TotalMemory)}"),Metric("Verified changes",engine.History.Count(x=>x.Status=="Applied").ToString(),"Journal records; not a performance score")};
        for(int i=0;i<cards.Length;i++) { Grid.SetColumn(cards[i],i); stats.Children.Add(cards[i]); } body.Children.Add(stats);
        Add("Your hardware",scanned?$"{h.Os}\n{h.Cpu} • {Number(h.Cores)} cores / {Number(h.Threads)} threads\n"+string.Join("\n",h.Gpus.Select(g=>$"{g.Name} • driver {g.Driver}"))+"\n"+string.Join("\n",h.Volumes):"Run Scan PC to read this computer. Unavailable values are never filled with demo data.");
        var findings=HealthAdvisor.Analyze(h);
        if(scanned) Add("System review",findings.Any(f=>f.Attention)?$"{findings.Count(f=>f.Attention)} readings deserve a closer look. These are diagnostics, not a gaming performance score.":"No attention flags were generated from the available measurements. Unsupported readings remain Unavailable. This is not a gaming benchmark.");
        foreach(var finding in findings)
            body.Children.Add(Card(Panel(Text(finding.Title,18,finding.Attention?"#FFD08A":"#5EE0BA",true),Text(finding.Detail,14,"#C3D0E2"),Button("Review supported controls",()=>Run(()=>Handle(Catalog.All.Single(o=>o.Id==finding.OperationId)))))));
        var opportunities=Catalog.Search("","Recommended",h).Take(4).ToList();
        foreach(var o in opportunities) body.Children.Add(Card(Panel(Text(o.Name,17,"#E9EFF8",true),Text(o.Reason(h),13,"#A7B7CD"),Button(o.Reversible?"Review change":"Open supported controls",()=>Run(()=>Handle(o))))));
        if(scanned&&h.Cpu.Contains("X3D",StringComparison.OrdinalIgnoreCase)) Add("X3D-aware guidance","Use supported AMD chipset drivers and OEM power/scheduler behavior. FrameForge will not modify core parking, affinity or scheduler registry values.");
        if(scanned&&h.TotalMemory is >0 and <16L*1024*1024*1024) Add("Memory pressure review","Less than 16 GiB RAM detected. Inspect optional startup apps and measured working sets. Process count alone does not prove a gaming performance problem.");
        if(h.Warnings.Count>0) Add("Unavailable scan sources",string.Join("\n",h.Warnings));
        var recent=engine.History.TakeLast(3).Reverse(); Add("Recent changes",recent.Any()?string.Join("\n",recent.Select(r=>$"{r.Time.ToLocalTime():g} • {r.Name} • {r.Status}")):"No changes recorded. Backups are created before reversible actions.");
    }
    void Optimize(bool qol)
    {
        Add(qol?"Convenience, clearly labeled":"A focused, evidence-aware catalog",qol?"Explorer and theme changes are convenience features. They carry no FPS claim.":"Direct changes provide previews and undo. Review actions open supported Windows/vendor controls; FrameForge cannot verify changes you make there. Unimplemented experimental tweaks are unavailable.");
        var filters=new ComboBox { ItemsSource=new[]{"All","Recommended","Safe","Advanced","Requires restart","AMD","NVIDIA","Applied","Not applicable"},SelectedItem=filter };
        var target=new StackPanel();
        void Draw()
        {
            target.Children.Clear(); var applied=new HashSet<string>(engine.History.Where(r=>r.Status=="Applied").Select(r=>r.OperationId));
            var ops=Catalog.Search(query,filter,vm.Hardware,applied).Where(o=>!qol||o.Category is "Windows Quality of Life" or "Registry Configuration").ToList();
            if(ops.Count==0) target.Children.Add(Text("No matching features. Try another search or filter.",14,"#A7B7CD"));
            foreach(var o in ops)
            {
                var incompatibility=o.Incompatibility(vm.Hardware);
                var modeReason=ModeReason(o);
                var reason=incompatibility??modeReason;
                var state=incompatibility!=null?"Not applicable":modeReason!=null?(o.Risk==Risk.Experimental?"Requires Advanced mode":"Requires Balanced mode"):applied.Contains(o.Id)?"Applied (journal)":"Available";
                var b=Button(o.Reversible?"Preview change":"Open controls",()=>Run(()=>Handle(o))); b.IsEnabled=reason==null;
                var details=new Expander { Header="Evidence, compatibility and restore details",Content=Text($"Benefit: {o.Benefit}\nEvidence: {o.Evidence}\nAdministrator: {(o.Admin?"required":"not required")} • Restart/refresh: {(o.Restart?"may be needed":"not required")}\nReversible: {(o.Reversible?"yes, with conflict checks":"external controls; manual reversal")}",13,"#BDCCE0") };
                target.Children.Add(Card(Panel(Text(o.Name,19,"#F1F5FC",true),Text($"{o.Category}  ·  {o.Risk} risk  ·  {state}",13,reason!=null?"#FFD08A":"#5EE0BA"),Text(o.Description,14,"#C3D0E2"),details,reason==null?b:Text(reason,14,"#FFD08A"))));
            }
        }
        filters.SelectionChanged+=(_,_)=> { filter=filters.SelectedItem as string??"All"; Draw(); }; body.Children.Add(filters); body.Children.Add(target); Draw();
    }
    string? ModeReason(Optimization o)=>o.Risk switch
    {
        Risk.Experimental when preferences.Mode!="Advanced"=>"Switch to Advanced mode to review this experiment.",
        Risk.Review when preferences.Mode=="Safe"=>"Switch to Balanced or Advanced mode to review this option.",
        _=>null
    };
    async Task Handle(Optimization o)
    {
        if(!scanned) await Scan();
        if((o.Incompatibility(vm.Hardware)??ModeReason(o)) is {} reason) throw new InvalidOperationException(reason);
        if(!o.Reversible) { WindowsSettings.Open(o); vm.Status="Opened "+o.Name+". Any external changes need manual review and reversal."; return; }
        var preview=await engine.Preview(o,vm.Hardware);
        if(preview.Current==preview.Proposed) { vm.Status="Already configured: "+o.Name; return; }
        var approved=Review([preview]);
        if(approved.Count==0) return;
        var r=await engine.Apply(preview,vm.Hardware,Guid.NewGuid().ToString("N"));
        vm.Status=r==null?"Already configured.":$"{r.Name}: {r.Status}. {r.Error}"; Render();
        if(r?.Status=="Failed") throw new IOException(r.Error+" The journal contains the original state; use Restore.");
    }
    async Task Smart()
    {
        if(!scanned) await Scan();
        var candidates=Catalog.All.Where(o=>o.Smart&&o.Risk==Risk.Low&&o.Incompatibility(vm.Hardware)==null);
        var previews=new List<ActionPreview>();
        foreach(var o in candidates) { var p=await engine.Preview(o,vm.Hardware); if(p.Current!=p.Proposed) previews.Add(p); }
        var approved=Review(previews,smart:true); var session=Guid.NewGuid().ToString("N"); var report=new List<string>();
        foreach(var p in approved)
        {
            try { var r=await engine.Apply(p,vm.Hardware,session); report.Add($"{p.Operation.Name}: {r?.Status??"Already configured"} {r?.Error}"); if(r?.Status=="Failed") break; }
            catch(Exception e) { report.Add(p.Operation.Name+": "+e.Message); break; }
        }
        if(approved.Count>0) MessageBox.Show(this,string.Join("\n",report)+"\n\nOriginal states are stored in Restore. No gaming performance gain was measured.","Smart Optimize report");
        vm.Status="Smart Optimize review complete. Each approved change was separately verified."; Render();
    }
    List<ActionPreview> Review(IReadOnlyList<ActionPreview> previews,bool smart=false)
    {
        var selected=new Dictionary<CheckBox,ActionPreview>();
        var win=new Window { Title="Review proposed changes",Owner=this,Width=760,Height=650,WindowStartupLocation=WindowStartupLocation.CenterOwner };
        var grid=new DockPanel { Margin=new(25) }; win.Content=grid;
        var buttons=Row(Button("Apply selected changes",()=>win.DialogResult=true),Button("Cancel",()=>win.DialogResult=false)); DockPanel.SetDock(buttons,Dock.Bottom); grid.Children.Add(buttons);
        var content=new StackPanel(); grid.Children.Add(new ScrollViewer { Content=content,VerticalScrollBarVisibility=ScrollBarVisibility.Auto });
        content.Children.Add(Text(smart?"Your system action plan":"Choose what changes",25,"#5EE0BA",true)); content.Children.Add(Text($"Mode: {preferences.Mode}. Every action still needs approval. Only implemented reversible low-risk conveniences are included in Smart Optimize. Advanced mode never bypasses protections.",13,"#A7B7CD"));
        if(smart)
        {
            content.Children.Add(Text("Review gaming and resource settings",19,"#5EE0BA",true));
            var ids=new HashSet<string>{"gamemode","capture","startup","graphics","display","power","amd","nvidia","intel"};
            if(vm.Hardware.TotalMemory is >0 and <16L*1073741824) ids.Add("memory");
            foreach(var finding in HealthAdvisor.Analyze(vm.Hardware)) ids.Add(finding.OperationId);
            foreach(var operation in Catalog.All.Where(o=>ids.Contains(o.Id)&&o.Incompatibility(vm.Hardware)==null))
            {
                content.Children.Add(Card(Panel(Text(operation.Name,17),Text(operation.Reason(vm.Hardware),13,"#A7B7CD"),Text($"Current: inspect in Windows/vendor controls\nProposed: manual review only\nEvidence: {operation.Evidence} • Risk: {operation.Risk}\nThese external changes are not backed up or verified by FrameForge.",12,"#8FA2BC"),Button("Open for manual review",()=>
                {
                    try { WindowsSettings.Open(operation); }
                    catch(Exception e) { MessageBox.Show(win,e.Message,"Could not open controls"); }
                }))));
            }
            content.Children.Add(Text("Optional reversible conveniences",19,"#5EE0BA",true));
            if(previews.Count==0) content.Children.Add(Text("These conveniences are already configured; no automatic changes are pending.",13,"#A7B7CD"));
        }
        foreach(var p in previews)
        {
            var c=new CheckBox { Content=p.Operation.Name,IsChecked=false }; selected[c]=p;
            content.Children.Add(Card(Panel(c,Text(p.Operation.Description),Text($"Current: {Value(p.Current)}\nProposed: {Value(p.Proposed)}\nTarget: {p.Operation.Target}\nEvidence: {p.Operation.Evidence}\nRisk: {p.Operation.Risk} • Administrator: {p.Operation.Admin} • Restart/refresh: {p.Operation.Restart}\nBenefit: {p.Operation.Benefit}\nReason: {p.Operation.Reason(vm.Hardware)}\nRollback: exact original value or absence; external changes are protected.",12,"#A7B7CD"))));
        }
        return win.ShowDialog()==true?selected.Where(x=>x.Key.IsChecked==true).Select(x=>x.Value).ToList():[];
    }
    static string Value(SettingValue s)=>s.Exists?$"{s.Value} ({s.Kind})":"Not configured / value absent";
    void Debloat()
    {
        Add("Remove deliberately","Current-user Store packages only. No apps are preselected. Frameworks, dependencies, core components and unknown packages are protected. Traditional programs can be managed in Windows Installed apps. Size, background activity and package startup status are Unavailable.");
        var list=new ListBox { Height=340 }; var details=Text("Select a package after scanning.",13,"#A7B7CD"); List<InstalledPackage> all=[];
        list.SelectionChanged+=(_,_)=> { if(list.SelectedItem is PackageRow row) { var p=row.Package; details.Text=$"{p.Name}\nPublisher: {p.Publisher}\nPackage: {p.FullName}\nClassification: {DebloatPolicy.Classify(p,all)}\nDependencies: {string.Join(", ",p.Dependencies)}\nFramework: {p.Framework} • Non-removable: {p.NonRemovable}\nRemoval affects this Windows user only. Reinstallation is not guaranteed. Review whether games or workflows use this app."; } };
        body.Children.Add(Row(Button("Scan installed packages",()=>Run(async ()=> { all=await Packages.Inventory(); list.ItemsSource=all.OrderBy(p=>p.Name).Select(p=>new PackageRow(p,DebloatPolicy.Classify(p,all))).ToList(); vm.Status=$"Read {all.Count} packages. Nothing was removed."; })),Button("Open Installed apps",()=>Run(()=>Handle(Catalog.All.Single(o=>o.Id=="memory"))))));
        body.Children.Add(list); body.Children.Add(Card(details));
        body.Children.Add(Button("Review removal of selected package",()=>Run(async ()=>
        {
            if(list.SelectedItem is not PackageRow row) throw new InvalidOperationException("Select one package.");
            if(!DebloatPolicy.CanRemove(row.Package,all)) throw new InvalidOperationException("This package is protected or unclassified. Removal blocked.");
            if(MessageBox.Show(this,$"Remove {row.Package.Name} for this user?\n\nPublisher: {row.Package.Publisher}\nClassification: {DebloatPolicy.Classify(row.Package,all)}\nDependencies: {string.Join(", ",row.Package.Dependencies)}\n\nApp data/features may be lost. There is no automatic undo. Reinstall through the official Store/publisher if available. This is not a measured FPS improvement.","Confirm package removal",MessageBoxButton.YesNo,MessageBoxImage.Warning)!=MessageBoxResult.Yes) return;
            store.Save("package-inventory.json",all); store.Audit("Removal prepared",row.Package.FullName,"Approved; no guaranteed rollback");
            try { await Packages.Remove(row.Package); store.Audit("Removal",row.Package.FullName,"Verified absent"); }
            catch(Exception e) { store.Audit("Removal",row.Package.FullName,"Failed: "+e.Message); throw; }
            all=await Packages.Inventory(); list.ItemsSource=all.OrderBy(p=>p.Name).Select(p=>new PackageRow(p,DebloatPolicy.Classify(p,all))).ToList(); details.Text="Removal verified. Use Restore to view the audit record; uninstall is not reversible by FrameForge."; vm.Status="Package removal verified.";
        })));
    }
    sealed record PackageRow(InstalledPackage Package,string Classification) { public override string ToString()=>$"[{Classification}]  {Package.Name}"; }
    void Cleanup()
    {
        Add("Conservative temporary-file cleanup","Preview regular .tmp/.temp/.log files older than 7 days in this user's TEMP only. Links, protected files, recent files, known shader/browser/cache/save paths are excluded. Files that changed or are locked are skipped. Irreversible deletion; no FPS claim. Other storage categories use Windows Storage Sense.");
        var temp=System.IO.Path.GetFullPath(System.IO.Path.GetTempPath()).TrimEnd(System.IO.Path.DirectorySeparatorChar);
        var known=System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Temp");
        if(!temp.Equals(known,StringComparison.OrdinalIgnoreCase))
        {
            Add("Custom TEMP location","Deletion is unavailable for this custom TEMP location. Use Windows Storage Sense instead.");
            body.Children.Add(Button("Open Storage Sense",()=>Run(()=>Handle(Catalog.All.Single(o=>o.Id=="storage"))))); return;
        }
        IReadOnlyList<CleanupFile> files=[]; var service=new CleanupService(temp);
        var summary=Text("No scan performed.",14,"#5EE0BA"); var list=new ListBox { Height=340,SelectionMode=SelectionMode.Extended };
        body.Children.Add(Row(Button("Preview old temporary files",()=>Run(async ()=> { files=await Task.Run(()=>service.Preview(DateTime.UtcNow)); list.ItemsSource=files.Select(f=>new FileRow(f)).ToList(); summary.Text=$"{files.Count} candidate files • {files.Sum(f=>f.Bytes)/1048576d:F1} MiB. Select individual rows (Ctrl/Shift for multiple). Scan is capped at 50,000 files; unavailable/locked directories are skipped."; vm.Status="Cleanup preview complete; no files deleted."; })),Button("Open Storage Sense",()=>Run(()=>Handle(Catalog.All.Single(o=>o.Id=="storage"))))));
        body.Children.Add(summary); body.Children.Add(list);
        body.Children.Add(Button("Delete selected previewed files…",()=>Run(async ()=>
        {
            var chosen=list.SelectedItems.Cast<FileRow>().Select(x=>x.File).ToList();
            if(chosen.Count==0) throw new InvalidOperationException("Select files from the preview first.");
            if(MessageBox.Show(this,$"Permanently delete {chosen.Count} selected temporary files ({chosen.Sum(f=>f.Bytes)/1048576d:F1} MiB)?\n\nThis bypasses the Recycle Bin and cannot be undone. The file list is shown in the preview. Changed and locked files will be skipped.","Confirm irreversible cleanup",MessageBoxButton.YesNo,MessageBoxImage.Warning)!=MessageBoxResult.Yes) return;
            store.Save("cleanup-preview.json",chosen); store.Audit("Cleanup prepared",chosen.Count+" files","Approved; irreversible");
            var result=await Task.Run(()=>service.Delete(chosen,DateTime.UtcNow)); store.Save("cleanup-result.json",result); store.Audit("Cleanup",result.Deleted+" files",$"{result.Bytes} bytes recovered; {result.Skipped} skipped");
            summary.Text=$"Deleted {result.Deleted} files • {result.Bytes/1048576d:F1} MiB recovered • {result.Skipped} skipped.\n"+string.Join("\n",result.Errors.Take(10)); list.ItemsSource=null; vm.Status="Cleanup finished. Results saved locally.";
        })));
    }
    sealed record FileRow(CleanupFile File) { public override string ToString()=>$"{File.Bytes/1024d:F1} KiB  ·  {File.LastWriteUtc.ToLocalTime():g}  ·  {File.Path}"; }
    void Gaming()
    {
        Add("Game guidance, matched to hardware","These are reviewable recommendations, not automatic presets. No game file, anti-cheat component or process is modified. Renderer, upscaling, frame caps and visual-quality tradeoffs must be tested in-game.");
        var games=new ComboBox { ItemsSource=Games.Names,SelectedIndex=0 };
        var choices=new List<string>{"Automatic"}; choices.AddRange(vm.Hardware.Gpus.Select(g=>g.Name));
        var gpu=new ComboBox { ItemsSource=choices,SelectedItem=choices.Contains(preferences.GamingGpu)?preferences.GamingGpu:"Automatic",MinWidth=350 };
        var description=Text("",14,"#A7B7CD");
        void Update()
        {
            var name=gpu.SelectedItem as string??"Automatic"; var vendor=vm.Hardware.Gpus.FirstOrDefault(g=>g.Name==name)?.Vendor??Vendor.Unknown;
            description.Text=Games.Guidance(games.SelectedItem as string??Games.Names[0],vm.Hardware,vendor);
        }
        games.SelectionChanged+=(_,_)=>Update(); gpu.SelectionChanged+=(_,_)=> { preferences=preferences with {GamingGpu=gpu.SelectedItem as string??"Automatic"}; store.Save("preferences.json",preferences); Update(); };
        body.Children.Add(Row(games,gpu)); body.Children.Add(Card(description)); Update();
        var detected=Text("Detection has not run.",13,"#A7B7CD"); body.Children.Add(Button("Look for local game markers",()=>Run(()=> { detected.Text=Diagnostics.DetectGames(); return Task.CompletedTask; }))); body.Children.Add(detected);
        Add("Benchmark protocol","Record hardware, OS/build, GPU driver, game version, renderer, resolution, quality, cap and repeatable scene. Warm caches; use several runs under similar conditions. Export FrameTimeMs CSV to Monitor. No FPS or latency improvements have been established by FrameForge.");
    }
    void Monitor()
    {
        var h=vm.Hardware;
        Add("On-demand measurements","No background polling or service. Scan PC refreshes these samples. CPU utilization is a provider sample; process working set is not private memory. GPU utilization, dedicated VRAM, temperatures, throttling, live FPS, per-process CPU/disk/network rate and TRIM are Unavailable.");
        Add("Resource snapshot",$"Taken: {(scanned?h.CapturedAt.ToLocalTime().ToString("g"):"Unavailable")}\nCPU {Number(h.CpuPercent,"% (provider sample)")} • Available RAM {GiB(h.AvailableMemory)} / installed {GiB(h.TotalMemory)}\nProcesses: {Number(h.ProcessCount)} • Startup entries: {(scanned?h.Startup.Count.ToString():"Unavailable")} • Uptime: {Number(h.UptimeHours?.ToString("F1")," hours")}\nPower: {h.PowerPlan}");
        var compare=Text("Capture two comparable snapshots to compare system resources. This does not measure gaming FPS.",13,"#A7B7CD");
        body.Children.Add(Row(Button("Save baseline",()=>Run(()=> { RequireScan(); store.Save("baseline.json",h); compare.Text="Baseline saved. Repeat the same idle/workload conditions before comparing."; return Task.CompletedTask; })),Button("Compare current to baseline",()=>Run(()=> { RequireScan(); var baseline=store.Load<HardwareSnapshot?>("baseline.json",()=>null)??throw new InvalidOperationException("Save a baseline first."); compare.Text=$"Baseline {baseline.CapturedAt.ToLocalTime():g} → sample {h.CapturedAt.ToLocalTime():g}\nCPU: {Number(baseline.CpuPercent)} → {Number(h.CpuPercent)} %\nAvailable RAM: {GiB(baseline.AvailableMemory)} → {GiB(h.AvailableMemory)}\nProcesses: {Number(baseline.ProcessCount)} → {Number(h.ProcessCount)}\nStartup entries: {baseline.Startup.Count} → {h.Startup.Count}\nResource changes do not by themselves prove FPS gains."; return Task.CompletedTask; })))); body.Children.Add(Card(compare));
        var statsText=Text("No frame-time data imported.",13,"#A7B7CD"); var chart=new Canvas { Height=100,Background=Brush("#0B1220") };
        body.Children.Add(Button("Import measured FrameTimeMs CSV",()=>Run(async ()=>
        {
            var d=new OpenFileDialog { Filter="Frame duration CSV|*.csv",CheckFileExists=true };
            if(d.ShowDialog(this)!=true) return;
            var file=new FileInfo(d.FileName); if(file.Length>20*1024*1024) throw new InvalidDataException("CSV exceeds 20 MiB limit.");
            var csv=await File.ReadAllTextAsync(d.FileName); var stats=Benchmark.Parse(csv);
            var history=store.Load<List<BenchmarkEntry>>("benchmarks.json",()=>[]); history.Add(new(DateTimeOffset.UtcNow,file.Name,stats,h)); store.Save("benchmarks.json",history);
            statsText.Text=$"Imported {file.Name}: {stats.Frames} frames • average {stats.AverageFps:F1} FPS • 1% low {stats.LowOnePercentFps:F1} FPS • p99 {stats.P99FrameMs:F2} ms.\n1% low = 1000 / mean slowest 1% frame durations. Source supplied by user, not live FrameForge capture. Record game/settings externally.\n"+string.Join("\n",history.TakeLast(4).Select(x=>$"{x.Time.ToLocalTime():g} {x.Source}: {x.Stats.AverageFps:F1} average / {x.Stats.LowOnePercentFps:F1} 1% low FPS"));
            var times=csv.Split('\n',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries).Skip(1).Select(x=>double.Parse(x,System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            chart.Children.Clear(); var line=new Polyline { Stroke=Brush("#5EE0BA"),StrokeThickness=1.3 }; var max=times.Max(); var step=Math.Max(1,times.Length/1000);
            for(int i=0;i<times.Length;i+=step) line.Points.Add(new Point(i*760d/(times.Length-1),95-times[i]/max*90)); chart.Children.Add(line);
        })));
        body.Children.Add(statsText); body.Children.Add(Text("Imported frame duration trace • left to right: frames • vertical scale auto-normalized, milliseconds",11,"#8FA2BC")); body.Children.Add(chart);
        Add("CPU and platform",$"{h.CpuManufacturer} • {h.Architecture}\nReported {Number(h.ReportedMhz," MHz")} • maximum reported {Number(h.MaxMhz," MHz")} • L3 {Number(h.L3CacheKb," KiB")}\n{h.Board}\nBIOS {h.Bios}\nLaptop: {Number(h.Laptop)}");
        Add("Top process working sets",Join(h.Processes)); Add("Memory modules",Join(h.MemoryModules)); Add("Storage",Join(h.Disks.Concat(h.Volumes))); Add("Display report",Join(h.Displays)); Add("Startup inventory (read-only)",Join(h.Startup)); Add("Security providers",Join(h.Security));
    }
    sealed record BenchmarkEntry(DateTimeOffset Time,string Source,FrameStats Stats,HardwareSnapshot Hardware);
    static string Join(IEnumerable<string> lines)=>lines.Any()?string.Join("\n",lines):"Unavailable / no entries returned";
    void Restore()
    {
        Add("A path back for supported settings","Prepared and failed records retain their original states. Undo checks the current value before overwriting it. External changes create a restore conflict; inspect it rather than force-overwriting. Cleanup and uninstalls are irreversible and appear in the audit log.");
        body.Children.Add(Button("Export history and audit JSON",()=>Run(()=> { var d=new SaveFileDialog { FileName="FrameForge-history.json",Filter="JSON|*.json" }; if(d.ShowDialog(this)==true) { File.WriteAllText(d.FileName,JsonSerializer.Serialize(new { Changes=engine.History,Audit=store.Load<List<AuditEvent>>("audit.json",()=>[]) },new JsonSerializerOptions {WriteIndented=true})); vm.Status="Exported history. It may contain local paths and package names; review before sharing."; } return Task.CompletedTask; })));
        var history=engine.History;
        foreach(var r in history.AsEnumerable().Reverse())
        {
            var panel=Panel(Text(r.Name,18,"#E9EFF8",true),Text($"{r.Time.ToLocalTime():g} • {r.Status} • session {r.Session}\nBefore: {Value(r.Before)}\nAfter: {Value(r.After)}\nRestart/refresh: {r.Restart}\nFeedback: {r.Feedback}\n{r.Error}",12,"#A7B7CD"));
            var buttons=Row();
            if(r.Status!="Restored")
            {
                buttons.Children.Add(Button("Undo this change",()=>Run(async ()=> { if(MessageBox.Show(this,$"Restore {r.Name} to {Value(r.Before)}? Current values must match this record; unrelated changes are protected.","Confirm restore",MessageBoxButton.YesNo)!=MessageBoxResult.Yes) return; var result=await engine.Undo(r.Id); vm.Status=result.Status+": "+result.Error; Render(); })));
                buttons.Children.Add(Button("Undo session",()=>Run(async ()=> { var session=engine.History.Where(x=>x.Session==r.Session&&x.Status!="Restored").Reverse().ToList(); if(MessageBox.Show(this,$"Undo {session.Count} changes in this session, newest first? Restoration stops at a conflict or failure.","Confirm session restore",MessageBoxButton.YesNo)!=MessageBoxResult.Yes) return; foreach(var x in session) { var result=await engine.Undo(x.Id); if(result.Status!="Restored") { vm.Status="Session restore stopped: "+result.Error; Render(); return; } } vm.Status="Session restored and verified."; Render(); })));
            }
            foreach(var rating in new[]{"Beneficial","Neutral","Harmful"}) buttons.Children.Add(Button(rating,()=>Run(()=> { engine.Feedback(r.Id,rating); Render(); return Task.CompletedTask; })));
            panel.Children.Add(buttons); body.Children.Add(Card(panel));
        }
        if(history.Count==0) Add("No setting changes","Original values will appear here after you approve a reversible change.");
        var audit=store.Load<List<AuditEvent>>("audit.json",()=>[]); Add("Irreversible action audit",audit.Count==0?"No cleanup or uninstall events.":string.Join("\n",audit.TakeLast(20).Reverse().Select(e=>$"{e.Time.ToLocalTime():g} • {e.Kind} • {e.Subject} • {e.Result}")));
    }
    void Tools()
    {
        Add("Built-in tools and official sources","Utilities launch only when clicked. FrameForge does not bundle vendor installers, download executables or silently install drivers. Driver update availability is Unavailable; compare your reported version with official support resources.");
        var utilities=new WrapPanel(); foreach(var name in new[]{"Task Manager","Resource Monitor","System Information","DirectX Diagnostic","Event Viewer","Reliability Monitor","Services (review only)"}) utilities.Children.Add(Button(name,()=>Run(()=> { WindowsSettings.Utility(name); return Task.CompletedTask; }))); body.Children.Add(utilities);
        foreach(var o in Catalog.All.Where(o=>o.Kind==ActionKind.Web&&o.Incompatibility(vm.Hardware)==null)) body.Children.Add(Card(Panel(Text(o.Name,18),Text(o.Description,13,"#A7B7CD"),Button("Open official support",()=>Run(()=>Handle(o))))));
        Add("Network adapters and DNS",Join(vm.Hardware.Network));
        var host=new TextBox { Text="",Tag="Hostname or IP address",Width=270,ToolTip="Hostname or IP you want to test" }; host.SetValue(System.Windows.Automation.AutomationProperties.NameProperty,"Diagnostic ping host"); var result=Text("Enter a destination you want to diagnose. Ten bounded ICMP requests; no configuration change.",13,"#A7B7CD");
        body.Children.Add(Row(host,Button("Test latency",()=>Run(async ()=> { var destination=host.Text.Trim(); result.Text=await Diagnostics.Ping(destination); store.Audit("Network diagnostic",destination,result.Text); vm.Status="Network diagnostic completed; settings unchanged."; })))); body.Children.Add(Card(result));
    }
    void Settings()
    {
        Add("Preferences and local data","FrameForge stores JSON in your LocalAppData folder. No account, analytics, background service or automatic updater. Logs/history may contain hardware names, paths and package names. No network request occurs until an official link or diagnostic is selected.");
        var mode=new ComboBox { ItemsSource=new[]{"Safe","Balanced","Advanced"},SelectedItem=preferences.Mode };
        mode.SelectionChanged+=(_,_)=> { preferences=preferences with {Mode=mode.SelectedItem as string??"Safe"}; store.Save("preferences.json",preferences); vm.Status="Mode saved. Explicit review and all compatibility rules still apply."; };
        body.Children.Add(Text("Optimization review mode",16)); body.Children.Add(mode);
        Add("Modes","Safe: low-risk changes. Balanced: review optional convenience changes individually. Advanced: power-plan experiments exposed individually with thermal/compatibility checks. Smart Optimize only proposes the implemented safe, reversible conveniences in every mode; no experimental action is applied automatically.");
        Add("Data directory",store.Root+"\nKeep backups before moving/deleting this folder. A corrupt history file blocks startup rather than discarding originals. Close FrameForge, preserve all files and recover the .bak manually if necessary. No history import or script execution from configuration is supported.");
        Add("Release status and unavailable capabilities","0.1.1 engineering preview. Windows runtime acceptance is required before production use. Live FPS, temperatures, reliable dedicated VRAM, GPU utilization, automatic driver updates, scheduler, Gaming Session Mode, automatic game-config edits and restore-point creation are unavailable. Cleanup and package removal cannot be undone by FrameForge.");
    }
}
