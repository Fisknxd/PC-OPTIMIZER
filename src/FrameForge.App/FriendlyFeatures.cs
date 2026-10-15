using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using FrameForge.Core;

namespace FrameForge.App;
public sealed partial class MainWindow
{
    readonly Dictionary<string,FeatureState> featureStates=[];
    readonly Button undoButton=new() {Content="↶ Undo last change",HorizontalAlignment=HorizontalAlignment.Left,ToolTip="Restore the previous saved value (Ctrl+Z outside text fields)"};
    string? undoId;
    async Task RefreshFeatureStates()
    {
        foreach(var op in Catalog.All.Where(o=>o.CanToggle))
        {
            try { featureStates[op.Id]=FeatureState.From(op,(await engine.Preview(op,vm.Hardware)).Current); }
            catch(Exception e) { featureStates[op.Id]=new(null,scanned?"Unavailable":"Scan PC first",e.Message); }
        }
    }
    bool Favorite(string id)=>preferences.Favorites?.Contains(id,StringComparer.Ordinal)==true;
    void SetFavorite(string id,bool favorite)
    {
        var ids=new HashSet<string>(preferences.Favorites??[],StringComparer.Ordinal);
        if(favorite) ids.Add(id); else ids.Remove(id);
        preferences=preferences with {Favorites=ids.Where(x=>Catalog.All.Any(o=>o.Id==x)).Order().ToArray()};
        store.Save("preferences.json",preferences); Render();
    }
    void FeatureLibrary(bool qol)
    {
        body.Children.Add(Panel(Text(qol?"Small changes. A nicer Windows.":"Your settings, one click away.",24,"#F1F5FC",true),Text("Switches save your previous setting automatically. Use Undo to get it back.",14,"#C3D0E2")));
        if(!preferences.TipsDismissed)
        {
            var tip=new DockPanel(); var dismiss=Button("Got it",()=> { preferences=preferences with {TipsDismissed=true}; store.Save("preferences.json",preferences); Render(); }); DockPanel.SetDock(dismiss,Dock.Right);tip.Children.Add(dismiss);
            tip.Children.Add(Text("A switch changes that setting immediately. “Windows default” means no explicit value is set. Windows and driver settings have a separate Open button. Ctrl+K jumps to search.",13,"#C3D0E2"));body.Children.Add(Card(tip));
        }
        var filters=new ComboBox { ItemsSource=new[]{"Quick switches","Recommended","Favorites","All features","On","Off","AMD","NVIDIA","Advanced","Not applicable"},SelectedItem=filter,MinWidth=200,ToolTip="Choose which features to show" };
        var count=Text("",13,"#BDCCE0");var target=new StackPanel();
        void Draw()
        {
            target.Children.Clear();
            var on=new HashSet<string>(featureStates.Where(x=>x.Value.Enabled==true).Select(x=>x.Key));
            string catalogFilter=filter is "Quick switches" or "Favorites" or "All features" or "On" or "Off"?"All":filter;
            var ops=Catalog.Search(query,catalogFilter,vm.Hardware,on).Where(o=>!qol||o.Category is "Windows Quality of Life" or "Registry Configuration").Where(o=>filter switch
            {
                "Quick switches"=>o.CanToggle,
                "Favorites"=>Favorite(o.Id),
                "On"=>o.CanToggle&&featureStates.GetValueOrDefault(o.Id)?.Enabled==true,
                "Off"=>o.CanToggle&&featureStates.GetValueOrDefault(o.Id)?.Enabled==false,
                _=>true
            }).OrderByDescending(o=>o.CanToggle).ThenByDescending(o=>Favorite(o.Id)).ToList();
            count.Text=$"{ops.Count} {(ops.Count==1?"feature":"features")}";
            if(ops.Count==0)
            {
                target.Children.Add(Card(Panel(Text(filter=="Favorites"?"Your favorites live here":"No matching features",19),Text(filter=="Favorites"?"Tap ☆ on a feature to keep it close.":"Try a different search or choose All features.",14,"#C3D0E2"),Button("Show all features",()=> { filter="All features"; search.Clear(); Render(); }))));
                return;
            }
            foreach(var op in ops) target.Children.Add(FeatureCard(op));
            if(filter=="Quick switches") target.Children.Add(Button("Explore gaming & Windows settings →",()=> { filter="All features"; Render(); }));
        }
        filters.SelectionChanged+=(_,_)=> { filter=filters.SelectedItem as string??"Quick switches"; Draw(); };
        body.Children.Add(Row(filters,Button("Refresh switches",()=>Run(async()=> { await RefreshFeatureStates(); Render(); vm.Status="Switches refreshed from Windows. No changes made."; })),count));
        body.Children.Add(target); Draw();
    }
    Border FeatureCard(Optimization op)
    {
        var reason=(!scanned?"Scan PC to read the current setting.":op.Incompatibility(vm.Hardware))??ModeReason(op);
        var state=featureStates.GetValueOrDefault(op.Id)??new(null,"Scan PC first");
        var header=new DockPanel(); var actions=new WrapPanel {VerticalAlignment=VerticalAlignment.Top};DockPanel.SetDock(actions,Dock.Right);header.Children.Add(actions);
        var star=Button(Favorite(op.Id)?"★":"☆",()=>SetFavorite(op.Id,!Favorite(op.Id)));star.ToolTip=Favorite(op.Id)?"Remove from favorites":"Add to favorites";star.Padding=new Thickness(9);star.MinWidth=40;star.SetValue(System.Windows.Automation.AutomationProperties.AutomationIdProperty,"favorite-"+op.Id);star.SetValue(System.Windows.Automation.AutomationProperties.NameProperty,"Favorite "+op.Name);actions.Children.Add(star);
        if(op.CanToggle)
        {
            actions.Children.Add(Text(state.Label,13,state.Enabled==true?"#5EE0BA":"#BDCCE0"));
            var toggle=new ToggleButton { Style=(Style)FindResource("FeatureSwitch"),Tag=op.Id,IsChecked=state.Enabled==true,IsThreeState=false,IsEnabled=reason==null&&state.Error==null,ToolTip=reason??state.Error??$"Turn {op.Name} on or off. Your previous setting is backed up automatically." };
            toggle.SetValue(System.Windows.Automation.AutomationProperties.AutomationIdProperty,"feature-"+op.Id);toggle.SetValue(System.Windows.Automation.AutomationProperties.NameProperty,op.Name);
            RoutedEventHandler changed=(_,_)=>Run(async()=>
            {
                await ToggleFeature(op,toggle.IsChecked==true);
                _ = Dispatcher.BeginInvoke(DispatcherPriority.Input,new Action(()=>Descendants<ToggleButton>(this).FirstOrDefault(t=>Equals(t.Tag,op.Id))?.Focus()));
            });
            toggle.Checked+=changed;toggle.Unchecked+=changed;actions.Children.Add(toggle);
        }
        else
        {
            var open=Button(op.Reversible?"Review experiment":op.Kind==ActionKind.Web?"Official support ↗":"Open settings ↗",()=>Run(()=>Handle(op)));open.IsEnabled=reason==null;actions.Children.Add(open);
        }
        header.Children.Add(Text(op.Name.Replace("Review ","",StringComparison.Ordinal),18,"#F1F5FC",true));
        var panel=Panel(header,Text(op.Description,14,"#C3D0E2"));
        panel.Children.Add(Text(op.CanToggle?"Automatic backup  ·  "+(op.Restart?"Explorer refresh may be needed":"Takes effect without a restart"):op.Reversible?"Advanced experiment  ·  Review before applying":"Opens supported controls  ·  Changes there are managed manually",12,"#BDCCE0"));
        if(reason!=null||op.CanToggle&&state.Error!=null)
        {
            panel.Children.Add(Text(reason??state.Error!,13,"#FFD08A"));
            if(ModeReason(op)!=null) panel.Children.Add(Button("Change review mode",()=>Go("Settings")));
        }
        panel.Children.Add(new Expander {Header="Details & how to undo",Content=Text($"{op.Category} • {op.Risk} risk\nBenefit: {op.Benefit}\nEvidence: {op.Evidence}\nAdministrator: {(op.Admin?"required":"not required")}\n"+(op.CanToggle?$"On value: {op.Value} • Off value: {op.DisabledValue}\nTarget: HKCU\\{op.Target}\nThe switch sets On or Off. Undo restores the exact previous value or Windows default. A value changed by another app is protected from overwriting.":op.Reversible?"The original plan is saved before the change. Restore it using Undo or Restore.":"FrameForge does not change, back up or verify settings in this external interface."),13,"#BDCCE0")});
        return Card(panel);
    }
    async Task ToggleFeature(Optimization op,bool enabled)
    {
        if(!scanned) throw new InvalidOperationException("Scan PC first.");
        if(!op.CanToggle) throw new InvalidOperationException("This action has no switch.");
        if((op.Incompatibility(vm.Hardware)??ModeReason(op)) is {} reason) throw new InvalidOperationException(reason);
        try
        {
            var preview=await engine.Preview(op,vm.Hardware,enabled);
            var result=await engine.Apply(preview,vm.Hardware,Guid.NewGuid().ToString("N"));
            if(result!=null) undoId=result.Id;
            if(result?.Status=="Failed") throw new IOException(result.Error+" Your original setting is saved; use Undo or Restore.");
            vm.Status=$"{op.Name}: {(enabled?"On":"Off")}. "+(result==null?"Already configured.":"Saved and verified. Undo is available below.");
        }
        finally { await RefreshFeatureStates(); Render(); }
    }
    async Task UndoLastChange()
    {
        var record=undoId!=null?engine.History.FirstOrDefault(r=>r.Id==undoId):engine.History.LastOrDefault(r=>r.Status=="Applied");
        if(record==null) { vm.Status="Nothing to undo yet.";return; }
        var result=await engine.Undo(record.Id);
        if(result.Status!="Restored") throw new IOException(result.Error??"Restore could not be verified. Open Restore to inspect the record.");
        undoId=null;await RefreshFeatureStates();Render();vm.Status=record.Name+": previous setting restored and verified.";
    }
}
