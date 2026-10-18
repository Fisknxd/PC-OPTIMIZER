using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using FrameForge.Core;
using FrameForge.Windows;

namespace FrameForge.App;

public sealed partial class MainWindow
{
    string featureSection="For gaming";
    string desktopSection="All desktop";

    void OpenFeatures()
    {
        featureSection="For gaming"; query=""; search.Text=""; searchDelay.Stop(); Go("Optimize");
    }

    void Optimize(bool qol)
    {
        powerReads.Clear();
        body.Children.Add(Text(qol?"Your desktop, your way":"Tune your PC",25,"#F1F5FC",true));
        body.Children.Add(Text("Pick what your PC needs. Every change keeps its previous setting in Recovery.",13,"#BDCCE0"));
        var sections=Row();
        foreach(var section in qol?new[]{"All desktop","Appearance","Less clutter"}:new[]{"For gaming","All tweaks","Browser memory","Power checks"})
        {
            var button=Button(section,()=> { if(qol)desktopSection=section;else featureSection=section; Render(); });
            button.Background=Brush((qol?desktopSection:featureSection)==section?"#27534F":"#23334C"); sections.Children.Add(button);
        }
        body.Children.Add(sections);
        if(!qol)
        {
        var other=new ComboBox {ItemsSource=new[]{"More categories","Background work","Downloads","Mouse & desktop","Less clutter","Appearance","Windows controls"},MinWidth=180,SelectedItem=new[]{"Background work","Downloads","Mouse & desktop","Less clutter","Appearance","Windows controls"}.Contains(featureSection)?featureSection:"More categories"};
        other.SetValue(AutomationProperties.NameProperty,"Other tweak categories");other.SelectionChanged+=(_,_)=> {if(other.SelectedItem is string section&&section!="More categories"){featureSection=section;Render();}};sections.Children.Add(other);
        }
        var mode=ReviewLevelPicker(165,Render);
        mode.SetValue(AutomationProperties.NameProperty,"Feature review mode");
        body.Children.Add(Row(Text("Options",13,"#BDCCE0"),mode,Button("Refresh",()=>Render()),Button("Create a restore point",()=>Go("Restore"))));
        if(preferences.Mode=="Safe")body.Children.Add(Button("Unlock choices with tradeoffs",()=> {preferences=preferences with {Mode="Balanced"};store.Save("preferences.json",preferences);Render();}));
        if(!qol&&featureSection=="Browser memory")body.Children.Add(Text("After changing a browser option, use Check in browser → Reload policies and look for OK. Edge sleeping-tab policies support local/work profiles; personal Microsoft-account profiles ignore them.",13,"#BDCCE0"));
        if(!qol&&featureSection=="Downloads")body.Children.Add(Row(Button("Open Windows download limits ↗",()=>Run(()=>Handle(Catalog.All.Single(x=>x.Id=="update-downloads"))))));
        if(!scanned) body.Children.Add(Card(Panel(Text("Scan to unlock changes",17,"#FFD08A",true),Button("Scan PC",()=>Run(Scan)))));
        var shown=Catalog.Search(query,"All",vm.Hardware).Where(o=>qol?FeatureToggles.Supports(o)&&TweakDefinitions.Get(o).Group is "Appearance" or "Less clutter"&&(desktopSection=="All desktop"||TweakDefinitions.Get(o).Group==desktopSection):!string.IsNullOrWhiteSpace(query)||(featureSection switch {
            "Windows controls"=>!FeatureToggles.Supports(o)&&o.Kind is not (ActionKind.Service or ActionKind.PowerSetting),
            "Power checks"=>o.Kind is ActionKind.PowerSetting or ActionKind.PowerPlan,
            "For gaming"=>PerformanceTweaks.Relevant(o),
            _=>FeatureToggles.Supports(o)&&(featureSection is "All tweaks" or "Feature toggles"||TweakDefinitions.Get(o).Group==featureSection)
        })).ToList();
        if(!qol&&featureSection=="For gaming"&&string.IsNullOrWhiteSpace(query))body.Children.Add(Text("Start with Low-end PC for a ready-made selection, or choose individual switches here. Browser and download settings help only when that work competes with your game.",13,"#BDCCE0"));
        if(shown.Count==0) body.Children.Add(Text("No matches. Try another category or clear search.",14,"#BDCCE0"));
        var cards=new List<UIElement>();
        foreach(var operation in shown.OrderBy(o=>FeatureToggles.Supports(o)?TweakDefinitions.Get(o).Group switch {"Gaming"=>0,"Low-end PC"=>1,"Browser memory"=>2,"Background work"=>3,"Downloads"=>4,_=>5}:6)) cards.Add(FeatureToggles.Supports(operation)?FeatureCard(operation):operation.Kind==ActionKind.PowerSetting?PowerSettingCard(operation):ExternalFeatureCard(operation));
        var grid=new TweakCardGrid();foreach(var card in cards)grid.Children.Add(card);body.Children.Add(grid);
    }

    Border FeatureCard(Optimization operation)
    {
        var state=Text("Reading Windows…",13,"#BDCCE0",true);
        var toggle=new ToggleButton { Style=(Style)FindResource("FeatureSwitch"),IsEnabled=false,ToolTip="Review enabling or disabling "+operation.Name };
        toggle.SetValue(AutomationProperties.NameProperty,operation.Name);
        toggle.SetValue(AutomationProperties.HelpTextProperty,"A review dialog opens before the change is applied. Space toggles when focused.");
        var action=new StackPanel { Width=88,HorizontalAlignment=HorizontalAlignment.Right };
        action.Children.Add(state); action.Children.Add(toggle);
        var layout=new Grid(); layout.ColumnDefinitions.Add(new()); layout.ColumnDefinitions.Add(new(){Width=GridLength.Auto}); Grid.SetColumn(action,1); layout.Children.Add(action);
        var detail=Panel(Text(operation.Name,18,"#F1F5FC",true),Text(operation.Description,13,"#BDCCE0")); detail.Margin=new(0,0,22,0); layout.Children.Add(detail);
        var reason=Text("",12,"#FFD08A");
        var extra=Panel(layout,Text(operation.Benefit,11,"#5EE0BA"),reason);
        var definition=TweakDefinitions.Get(operation);
        extra.Children.Add(new Expander {Header="Details & tradeoffs",Content=Text(definition.Note,12,"#BDCCE0")});
        if(BrowserPolicies.Supports(operation))extra.Children.Add(Row(Button("Check in browser ↗",()=>Run(()=> {BrowserPolicies.OpenPolicies(operation);return Task.CompletedTask;}))));
        var card=Card(extra); card.Padding=new(16);
        SettingValue? current=null;
        bool? enabled=null;
        async Task ReadState()
        {
            try
            {
                await system.CheckAvailability(operation);current=await system.Read(operation); enabled=FeatureToggles.Enabled(operation,current);
                toggle.IsChecked=enabled==true;
                state.Text=enabled==true?"● On":enabled==false?"○ Off":current.Exists?"Custom":"Default";
                toggle.SetValue(AutomationProperties.ItemStatusProperty,enabled==true?"On":enabled==false?"Off":"Windows default, no explicit value configured");
                state.Foreground=Brush(enabled==true?"#5EE0BA":"#BDCCE0");
                var blocked=!scanned?"Scan PC before changing this feature.":operation.Incompatibility(vm.Hardware)??ModeReason(operation)??(operation.Admin&&!system.IsAdministrator?"Administrator access is needed for this setting.":null);
                reason.Text=blocked??"";
                reason.Visibility=reason.Text.Length==0?Visibility.Collapsed:Visibility.Visible;
                if(blocked==null) reason.Foreground=Brush("#BDCCE0");
                toggle.IsEnabled=blocked==null;
            }
            catch(Exception e) { state.Text="Unavailable";state.Foreground=Brush("#FFD08A");reason.Text=e.Message;reason.Visibility=Visibility.Visible;toggle.IsEnabled=false;toggle.SetValue(AutomationProperties.ItemStatusProperty,"Unavailable: "+e.Message); }
        }
        toggle.Click+=(_,_)=>
        {
            var requested=toggle.IsChecked==true;
            toggle.IsChecked=enabled==true; // The displayed state changes only after verification.
            Run(async ()=>
            {
                try { await SetFeature(operation,requested); }
                finally { await ReadState(); }
            });
        };
        _=ReadState(); return card;
    }

    Border ExternalFeatureCard(Optimization operation)
    {
        if(operation.Kind==ActionKind.Service) return ServiceCard(operation);
        var reason=!scanned?"Scan PC to check compatibility.":operation.Incompatibility(vm.Hardware)??ModeReason(operation);
        var button=Button(operation.Reversible?"Review power-plan change":"Open Windows settings ↗",()=>Run(()=>Handle(operation)));
        if(operation.Kind==ActionKind.Web) button.Content="Open official guidance ↗";
        button.IsEnabled=reason==null;
        return Card(Panel(Text(operation.Name,18,"#F1F5FC",true),Text(operation.Description,13,"#BDCCE0"),
            Text(operation.Reversible?"Power experiment · original saved and checked":"Manual control · change and reverse this in the window that opens",12,"#BDCCE0"),
            reason==null?button:Panel(Text(reason,13,"#FFD08A"),button)));
    }

    // Injectable confirmation lets UI integration exercise the exact click path with in-memory settings.
    internal Func<ActionPreview,bool>? FeatureConfirmation { get; set; }
    async Task SetFeature(Optimization operation,bool enabled)
    {
        RequireScan();
        if(ModeReason(operation) is {} blocked) throw new InvalidOperationException(blocked);
        if(enabled&&operation.Id is "chrome-memory-strong" or "edge-sleep-sooner")
        {
            var required=Catalog.All.Single(x=>x.Id==(operation.Id=="chrome-memory-strong"?"chrome-memory":"edge-sleep"));
            if(FeatureToggles.Enabled(required,await system.Read(required))!=true)throw new InvalidOperationException("Turn on "+required.Name+" first so this setting can take effect.");
        }
        var preview=await engine.PreviewToggle(operation,vm.Hardware,enabled);
        if(preview.Current==preview.Proposed) { vm.Status=operation.Name+" is already "+(enabled?"on.":"off."); return; }
        if(!(FeatureConfirmation?.Invoke(preview)??ReviewFeature(preview,enabled))) { vm.Status="Change cancelled. "+operation.Name+" was left unchanged."; return; }
        Watch(operation.Name);
        var result=await engine.Apply(preview,vm.Hardware,Guid.NewGuid().ToString("N"));
        if(result?.Status=="Failed") throw new IOException(result.Error+" Original values remain available in Restore.");
        vm.Status=$"{operation.Name}: {(enabled?"On":"Off")} · saved value verified. Original saved in Recovery."+(BrowserPolicies.Supports(operation)?" Check acceptance in the browser policy page.":"");
    }

    bool ReviewFeature(ActionPreview preview,bool enabled)
    {
        var window=new Window { Owner=this,Title=(enabled?"Enable ":"Disable ")+preview.Operation.Name,Width=590,SizeToContent=SizeToContent.Height,ResizeMode=ResizeMode.NoResize,WindowStartupLocation=WindowStartupLocation.CenterOwner };
        var current=FeatureToggles.Enabled(preview.Operation,preview.Current);
        var currentLabel=current==true?"On":current==false?"Off":preview.Current.Exists?"Custom":"Windows default";
        var panel=Panel(Text(preview.Operation.Name,23,"#F1F5FC",true),Text(enabled?"Turn this feature on?":"Turn this feature off?",16,"#BDCCE0"),
            Text(preview.Operation.Description,14,"#BDCCE0"),
            Card(Panel(Text("CURRENT → REQUESTED",11,"#BDCCE0",true),Text($"{currentLabel}  →  {(enabled?"On":"Off")}",19,"#5EE0BA",true))),
            Text("Your previous setting will be saved in Recovery.",13,"#BDCCE0"));
        panel.Children.Add(new Expander {Header="Technical details",Content=Text($"Current: {Value(preview.Current)}\nRequested: {Value(preview.Proposed)}\nTarget: {preview.Operation.Target}",12,"#BDCCE0")});
        panel.Children.Add(Text(TweakDefinitions.Get(preview.Operation).Note,13,"#BDCCE0"));
        var cancel=Button("Cancel",()=>window.DialogResult=false); cancel.IsCancel=true;
        panel.Children.Add(Row(PrimaryButton(enabled?"Turn on":"Turn off",()=>window.DialogResult=true),cancel)); panel.Margin=new(24); window.Content=panel;
        return window.ShowDialog()==true;
    }
}
