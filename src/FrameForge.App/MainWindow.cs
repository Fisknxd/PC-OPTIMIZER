using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;
using FrameForge.Core;
using FrameForge.Windows;

namespace FrameForge.App;

public sealed partial class MainWindow : Window
{
    readonly JsonStore store;
    readonly ISystemSettings system;
    readonly OperationEngine engine;
    readonly ActivityFeed activity=new();
    readonly IAppPriority priorityBackend=new WindowsAppPriority();
    readonly AppPriorityManager priorities;
    readonly IGameConfigFiles gameFiles;
    readonly GameGraphicsManager graphicsProfiles;
    readonly RestorePointManager restorePoints;
    readonly AppViewModel vm=new();
    readonly StackPanel body=new();
    readonly Grid workspace=new();
    readonly TextBlock heading=new() { FontSize=28,FontWeight=FontWeights.SemiBold };
    readonly TextBox search=new() { Width=330,Tag="Find a tweak, game or setting…",ToolTip="Search by name, description or graphics brand",VerticalContentAlignment=VerticalAlignment.Center };
    readonly Dictionary<string,Button> navigation=[];
    readonly StackPanel topActions=new() { Orientation=Orientation.Horizontal };
    readonly ScrollViewer pageScroll=new() { VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled };
    readonly DispatcherTimer searchDelay=new() { Interval=TimeSpan.FromMilliseconds(200) };
    CancellationTokenSource? readCancellation;
    static readonly HashSet<string> ExtraPages=["Services","Debloat","Gaming","App priority","Cleanup","Tools","Quality of Life"];
    Expander? extraNavigation;
    string page="Dashboard";
    string query="";
    Preferences preferences;
    bool scanned;
    internal bool AutomatedValidation {get;set;}
    public MainWindow(JsonStore store,bool autoScan=true,ISystemSettings? settings=null,IRestorePointBackend? restoreBackend=null,IGameConfigFiles? gameFiles=null)
    {
        this.store=store; system=settings??new WindowsSettings(activity); engine=new(system,store,activity); priorities=new(priorityBackend,store,activity); restorePoints=new(restoreBackend??new WindowsRestorePoints(),store,activity); preferences=store.Load("preferences.json",()=>new Preferences());if(preferences.Mode is not ("Safe" or "Balanced" or "Advanced"))preferences=preferences with {Mode="Safe"};watchAutomatically=settings==null&&preferences.WatchChanges;
        this.gameFiles=gameFiles??new WindowsGameConfigs();graphicsProfiles=new(this.gameFiles,store,activity);
        SetResourceReference(BackgroundProperty,"CanvasBrush"); SetResourceReference(ForegroundProperty,"TextBrush");
        FontFamily=new FontFamily("Segoe UI"); FontSize=15;
        UseLayoutRounding=true; SnapsToDevicePixels=true;
        Icon=System.Windows.Media.Imaging.BitmapFrame.Create(new Uri("pack://application:,,,/Assets/FrameForge.ico"));
        Title="FrameForge 0.7.0 • Windows gaming toolkit"; Width=1360; Height=900; MinWidth=980; MinHeight=700; WindowStartupLocation=WindowStartupLocation.CenterScreen; DataContext=vm;
        var shell=new Grid(); shell.ColumnDefinitions.Add(new(){Width=new(225)}); shell.ColumnDefinitions.Add(new()); Content=shell;
        var side=new DockPanel { Background=Brush("#101A2B"),Margin=new(0) }; shell.Children.Add(side);
        var brand=new StackPanel { Margin=new(22,28,16,24) }; var brandRow=new StackPanel {Orientation=Orientation.Horizontal}; brandRow.Children.Add(new Image {Source=new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Assets/FrameForge.png")),Width=30,Height=30,Margin=new(0,0,8,0)}); brandRow.Children.Add(Text("FRAMEFORGE",18,"#5EE0BA",true)); brand.Children.Add(brandRow); brand.Children.Add(Text("READY FOR PLAY.",10,"#8FA2BC")); DockPanel.SetDock(brand,Dock.Top); side.Children.Add(brand);
        var footer=Text((system.IsAdministrator?"● Administrator access":"○ Standard access")+"\nLocal · No account\nv0.7.0 · Preview",11,"#8FA2BC"); footer.Margin=new(22,12,16,22); DockPanel.SetDock(footer,Dock.Bottom); side.Children.Add(footer);
        var nav=new StackPanel { Margin=new(12,0,10,0) }; side.Children.Add(new ScrollViewer {Content=nav,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled});
        var moreTools=new StackPanel();extraNavigation=new Expander {Header="More tools",Content=moreTools,Margin=new(6,10,0,8)};
        foreach(var item in new[]{("Dashboard","◫"),("Low-end PC","↓"),("Potato Settings","◈"),("Optimize","⚡"),("Monitor","▥"),("Restore","↶"),("Services","≋"),("Debloat","▦"),("Gaming","◇"),("App priority","↑"),("Cleanup","⌁"),("Tools","⊞"),("Quality of Life","◐"),("Settings","⚙")})
        {
            if(item.Item1=="Services")nav.Children.Add(extraNavigation);
            var label=item.Item1;var button=Button(item.Item2+"   "+PageName(label),()=>Go(label));button.Margin=new(0,2,0,2);button.Background=Brush("#101A2B");navigation[label]=button;
            (ExtraPages.Contains(label)?moreTools:nav).Children.Add(button);
        }
        Grid.SetColumn(workspace,1); workspace.Margin=new(30,24,28,16); shell.Children.Add(workspace);
        workspace.RowDefinitions.Add(new(){Height=GridLength.Auto}); workspace.RowDefinitions.Add(new(){Height=GridLength.Auto}); workspace.RowDefinitions.Add(new()); workspace.RowDefinitions.Add(new(){Height=GridLength.Auto});
        var top=new DockPanel(); workspace.Children.Add(top);
        DockPanel.SetDock(topActions,Dock.Right); topActions.Children.Add(Button("Watch changes",ShowActivity)); topActions.Children.Add(Button("↻  Scan PC",()=>Run(Scan))); topActions.Children.Add(PrimaryButton("Tweaks",OpenFeatures)); top.Children.Add(topActions); top.Children.Add(heading);
        searchDelay.Tick+=(_,_)=> { searchDelay.Stop(); query=search.Text; if(page!="Optimize") Go("Optimize"); else Render(); };
        search.TextChanged+=(_,_)=> { searchDelay.Stop(); searchDelay.Start(); }; search.SetValue(System.Windows.Automation.AutomationProperties.NameProperty,"Global feature search");
        var searchbar=new DockPanel { Margin=new(0,12,0,14) }; var shortcut=Text("Ctrl+K  Search",12,"#BDCCE0"); DockPanel.SetDock(shortcut,Dock.Right); searchbar.Children.Add(shortcut); search.Width=double.NaN; searchbar.Children.Add(search); Grid.SetRow(searchbar,1); workspace.Children.Add(searchbar);
        pageScroll.Content=body; Grid.SetRow(pageScroll,2); workspace.Children.Add(pageScroll);
        var statusArea=new StackPanel { Margin=new(0,12,0,0) };
        var progress=new ProgressBar { IsIndeterminate=true,Margin=new(0,0,0,8) }; progress.SetBinding(VisibilityProperty,new Binding(nameof(AppViewModel.Busy)) { Converter=new BooleanToVisibilityConverter() }); statusArea.Children.Add(progress);
        var statusLine=new DockPanel(); var cancel=Button("Cancel scan",()=>readCancellation?.Cancel()); cancel.Visibility=Visibility.Collapsed; cancel.SetBinding(VisibilityProperty,new Binding(nameof(AppViewModel.CanCancel)) { Converter=new BooleanToVisibilityConverter() }); DockPanel.SetDock(cancel,Dock.Right); statusLine.Children.Add(cancel);
        var status=new TextBlock { FontSize=13,Foreground=Brush("#BDCCE0"),TextWrapping=TextWrapping.Wrap,VerticalAlignment=VerticalAlignment.Center }; status.SetBinding(TextBlock.TextProperty,new Binding(nameof(AppViewModel.Status))); statusLine.Children.Add(status); statusArea.Children.Add(statusLine); Grid.SetRow(statusArea,3); workspace.Children.Add(statusArea);
        PreviewKeyDown+=(_,e)=> { if(e.Key==System.Windows.Input.Key.K&&System.Windows.Input.Keyboard.Modifiers==System.Windows.Input.ModifierKeys.Control) { search.Focus(); search.SelectAll(); e.Handled=true; } };
        Closed+=(_,_)=> { StopLiveMonitor(); searchDelay.Stop(); activityWindow?.Close(); };
        Closing+=(_,e)=> { if(vm.Busy) { e.Cancel=true; vm.Status="Wait for the current action to finish before closing; changes may be in progress."; } };
        Go("Dashboard");
        if(autoScan) Loaded+=(_,_)=> { if(!scanned&&!vm.Busy) Run(Scan); };
    }
    static SolidColorBrush Brush(string color)=>new((Color)ColorConverter.ConvertFromString(color));
    static TextBlock Text(string text,double size=14,string color="#E9EFF8",bool bold=false)=>new() { Text=text,FontSize=Math.Max(13,size<16?size+1:size),LineHeight=Math.Max(13,size<16?size+1:size)*1.45,Foreground=Brush(color=="#8FA2BC"?"#BDCCE0":color=="#A7B7CD"?"#C3D0E2":color),FontWeight=bold?FontWeights.SemiBold:FontWeights.Normal,TextWrapping=TextWrapping.Wrap,Margin=new(0,4,0,6),VerticalAlignment=VerticalAlignment.Center };
    static Button Button(string text,Action click) { var b=new Button { Content=text }; b.Click+=(_,_)=>click(); return b; }
    static Button PrimaryButton(string text,Action click)
    {
        var b=Button(text,click); var style=new Style(typeof(Button),(Style)Application.Current.FindResource(typeof(Button)));
        style.Setters.Add(new Setter(Control.BackgroundProperty,Brush("#5EE0BA"))); style.Setters.Add(new Setter(Control.ForegroundProperty,Brush("#09231E")));
        style.Setters.Add(new Setter(Control.FontWeightProperty,FontWeights.SemiBold)); b.Style=style; return b;
    }
    static WrapPanel Row(params UIElement[] items) { var p=new WrapPanel { Orientation=Orientation.Horizontal }; foreach(var i in items) p.Children.Add(i); return p; }
    static Border Card(UIElement content)=>new() { Background=Brush("#141E30"),BorderBrush=Brush("#26344B"),BorderThickness=new(1),CornerRadius=new(12),Padding=new(20),Margin=new(0,0,12,14),Child=content };
    static StackPanel Panel(params UIElement[] elements) { var p=new StackPanel(); foreach(var e in elements) p.Children.Add(e); return p; }
    void Add(string title,string detail)=>body.Children.Add(Card(Panel(Text(title,18,"#E9EFF8",true),Text(detail,13,"#A7B7CD"))));
    static string Number(object? value,string suffix="")=>value==null?"Unavailable":value+suffix;
    static string GiB(long? value)=>value is {} v?$"{v/1073741824d:F1} GiB":"Unavailable";
    void Go(string name)
    {
        if(vm.Busy) return; StopLiveMonitor();if(extraNavigation!=null)extraNavigation.IsExpanded=ExtraPages.Contains(name);page=name; heading.Text=PageName(name); pageScroll.ScrollToTop();
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
            case "Services": ServicesView(); break;
            case "App priority": PriorityView(); break;
            case "Potato Settings": PotatoView(); break;
            case "Low-end PC": LowEndView(); break;
        }
    }
    static string PageName(string name)=>name switch {"Dashboard"=>"Home","Optimize"=>"Tweaks","Debloat"=>"Apps","Gaming"=>"Game setup","Monitor"=>"Live stats","Cleanup"=>"Clean up","Quality of Life"=>"Desktop","Restore"=>"Recovery","Tools"=>"Connection",_=>name};
    async void Run(Func<Task> work)
    {
        if(vm.Busy) return; vm.Busy=true;
        // Disable navigation and action surfaces for the duration; status stays visible.
        StopLiveMonitor(); body.IsEnabled=false; search.IsEnabled=false; topActions.IsEnabled=false; foreach(var b in navigation.Values) b.IsEnabled=false;
        try { await work(); }
        catch(OperationCanceledException) { vm.Status="Scan cancelled. The previous snapshot is retained; no settings changed."; }
        catch(Exception e)
        {
            activity.Report("ACTION FAILED: "+e.Message);
            vm.Status="Action failed: "+e.Message;
            if(AutomatedValidation)throw;
            MessageBox.Show(this,e.Message,"FrameForge — action failed",MessageBoxButton.OK,MessageBoxImage.Warning);
        }
        finally { vm.Busy=false; body.IsEnabled=true; search.IsEnabled=true; topActions.IsEnabled=true; foreach(var b in navigation.Values) b.IsEnabled=true; }
    }
    async Task Scan()
    {
        vm.Status="Reading Windows hardware and configuration… No settings are changed.";
        using var cancellation=new CancellationTokenSource(); readCancellation=cancellation; vm.CanCancel=true;
        try
        {
            var progress=new Progress<string>(message=>vm.Status=message+" • Read-only scan");
            var snapshot=await Scanner.Scan(cancellation.Token,progress); cancellation.Token.ThrowIfCancellationRequested();
            vm.Hardware=snapshot; scanned=true; Render();
            vm.Status=$"Scan completed {DateTime.Now:t} • {vm.Hardware.Warnings.Count} unavailable sources. No settings changed.";
        }
        finally { vm.CanCancel=false; readCancellation=null; }
    }
    void RequireScan() { if(!scanned) throw new InvalidOperationException("Scan this PC first so compatibility can be checked."); }
    string? ModeReason(Optimization o)=>o.Risk switch
    {
        Risk.Experimental when preferences.Mode!="Advanced"=>"Choose Experiments to review this power test.",
        Risk.Review when preferences.Mode=="Safe"=>"Choose More choices to review this setting and its tradeoffs.",
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
        Watch(o.Name);
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
        if(approved.Count>0) Watch("Apply reviewed changes");
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
        Add("Remove apps you do not use","Choose an app to review. Shared Windows components are protected. Removal can delete app data and has no Undo here.");
        var list=new ListBox { Height=340 }; var details=Text("Select a package after scanning.",13,"#A7B7CD"); List<InstalledPackage> all=[];
        var packageSearch=new TextBox { Tag="Filter packages by name or publisher" };
        var classification=new ComboBox { ItemsSource=new[]{"All packages","Removable after review","Protected"},SelectedIndex=0 };
        var packageCount=Text("Scan to read installed Store packages.",13,"#BDCCE0");
        void DrawPackages()
        {
            var needle=packageSearch.Text.Trim(); var mode=classification.SelectedItem as string;
            var rows=all.Where(p=>(p.Name.Contains(needle,StringComparison.OrdinalIgnoreCase)||p.Publisher.Contains(needle,StringComparison.OrdinalIgnoreCase))&&(mode=="All packages"||DebloatPolicy.CanRemove(p,all)==(mode=="Removable after review"))).OrderBy(p=>p.Name).Select(p=>new PackageRow(p,DebloatPolicy.Classify(p,all))).ToList();
            list.ItemsSource=rows; details.Text="Select a package to review publisher, dependencies and removal eligibility.";
            packageCount.Text=$"{rows.Count} shown / {all.Count} packages · {all.Count(p=>DebloatPolicy.CanRemove(p,all))} eligible for individual review. Nothing is selected automatically.";
        }
        packageSearch.TextChanged+=(_,_)=>DrawPackages(); classification.SelectionChanged+=(_,_)=>DrawPackages();
        list.SelectionChanged+=(_,_)=> { if(list.SelectedItem is PackageRow row) { var p=row.Package; var label=AppLabel(p.Name); details.Text=$"{label.Name}\n{label.Description}\n\n"+(DebloatPolicy.CanRemove(p,all)?"You can review removal for this Windows account. Saved app data may be lost; reinstalling is not guaranteed.":"Protected: Windows or another app may need it. Removal is blocked."); } };
        body.Children.Add(Row(Button("Scan installed packages",()=>Run(async ()=> { all=await Packages.Inventory(); DrawPackages(); vm.Status=$"Read {all.Count} packages. Nothing was removed."; })),Button("Open Installed apps",()=>Run(()=>Handle(Catalog.All.Single(o=>o.Id=="memory"))))));
        body.Children.Add(Columns(packageSearch,classification)); body.Children.Add(packageCount);
        body.Children.Add(list); body.Children.Add(Card(details));
        body.Children.Add(Button("Review removal of selected package",()=>Run(async ()=>
        {
            if(list.SelectedItem is not PackageRow row) throw new InvalidOperationException("Select one package.");
            if(!DebloatPolicy.CanRemove(row.Package,all)) throw new InvalidOperationException("This package is protected or unclassified. Removal blocked.");
            if(MessageBox.Show(this,$"Remove {row.Package.Name} for this user?\n\nPublisher: {row.Package.Publisher}\nClassification: {DebloatPolicy.Classify(row.Package,all)}\nDependencies: {string.Join(", ",row.Package.Dependencies)}\n\nApp data/features may be lost. There is no automatic undo. Reinstall through the official Store/publisher if available. This is not a measured FPS improvement.","Confirm package removal",MessageBoxButton.YesNo,MessageBoxImage.Warning)!=MessageBoxResult.Yes) return;
            store.Save("package-inventory.json",all); store.Audit("Removal prepared",row.Package.FullName,"Approved; no guaranteed rollback");
            Watch("Remove selected app: "+row.Package.Name);
            try { await Packages.Remove(row.Package,activity); store.Audit("Removal",row.Package.FullName,"Verified absent");activity.Report("VERIFIED: App package is absent for this Windows user."); }
            catch(Exception e) { store.Audit("Removal",row.Package.FullName,"Failed: "+e.Message); throw; }
            all=await Packages.Inventory(); list.ItemsSource=all.OrderBy(p=>p.Name).Select(p=>new PackageRow(p,DebloatPolicy.Classify(p,all))).ToList(); details.Text="Removal verified. Use Restore to view the audit record; uninstall is not reversible by FrameForge."; vm.Status="Package removal verified.";
        })));
    }
    static (string Name,string Description) AppLabel(string name)=>name switch
    {
        "Microsoft.BingNews"=>("News","Microsoft's news-reading app."),
        "Microsoft.BingWeather"=>("Weather","Microsoft's weather forecast app."),
        "Microsoft.MicrosoftSolitaireCollection"=>("Solitaire","Microsoft's collection of card games."),
        "Clipchamp.Clipchamp"=>("Clipchamp","A video editor. Keep it if you use it to edit recordings."),
        "Microsoft.ZuneMusic"=>("Media Player","Windows' music and media player. Removal can change how media files open."),
        "Microsoft.ZuneVideo"=>("Movies & TV","Windows' movie player and video store app."),
        "Microsoft.WindowsFeedbackHub"=>("Feedback Hub","Sends problem reports and feedback to Microsoft."),
        "Microsoft.Getstarted"=>("Tips","Help and tips about using Windows."),
        "Microsoft.YourPhone"=>("Phone Link","Connects your phone to Windows for messages, calls and photos."),
        "MSTeams" or "MicrosoftTeams"=>("Microsoft Teams","Chat and meeting software. Keep it if work or school uses it."),
        "Microsoft.XboxGamingOverlay"=>("Xbox Game Bar","The Windows game overlay, recording controls and Xbox shortcuts."),
        "Microsoft.GamingApp"=>("Xbox app","Installs and manages Xbox and Game Pass PC games."),
        _=>(name,"An installed Windows or Store app. Its dependencies are checked before removal.")
    };
    sealed record PackageRow(InstalledPackage Package,string Classification) { public override string ToString()=>$"{AppLabel(Package.Name).Name}  ·  {Classification}"; }
    void Cleanup()
    {
        Add("Free up a little space","Preview old temporary files, then choose what to delete. Recent files, game saves and known browser or shader caches are left alone. Deletion cannot be undone.");
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
            Watch("Delete selected temporary files");activity.Report($"Native file deletion: checking {chosen.Count} approved files again before deletion.");
            var result=await Task.Run(()=>service.Delete(chosen,DateTime.UtcNow)); store.Save("cleanup-result.json",result); store.Audit("Cleanup",result.Deleted+" files",$"{result.Bytes} bytes recovered; {result.Skipped} skipped");
            activity.Report($"Cleanup completed: {result.Deleted} files deleted; {result.Bytes} bytes recovered; {result.Skipped} skipped.");foreach(var error in result.Errors.Take(20))activity.Report(error);
            summary.Text=$"Deleted {result.Deleted} files • {result.Bytes/1048576d:F1} MiB recovered • {result.Skipped} skipped.\n"+string.Join("\n",result.Errors.Take(10)); list.ItemsSource=null; vm.Status="Cleanup finished. Results saved locally.";
        })));
    }
    sealed record FileRow(CleanupFile File) { public override string ToString()=>$"{File.Bytes/1024d:F1} KiB  ·  {File.LastWriteUtc.ToLocalTime():g}  ·  {File.Path}"; }
    static string Join(IEnumerable<string> lines)=>lines.Any()?string.Join("\n",lines):"Unavailable / no entries returned";
}
