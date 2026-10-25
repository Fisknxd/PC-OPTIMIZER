using System.Windows.Controls;
using FrameForge.Core;
namespace FrameForge.App;
public sealed partial class MainWindow
{
    Task? lowEndRead;
    int lowEndGeneration;
    void LowEndView()
    {
        body.Children.Add(Text("Make room for the game",25,"#F1F5FC",true));
        body.Children.Add(Text("A focused profile for low-memory PCs and weak graphics. Every selected change is backed up. Check the tradeoffs before applying.",13,"#BDCCE0"));
        body.Children.Add(Row(Button("Refresh",RefreshSettings),Button("Potato Settings",()=>Go("Potato Settings")),Button("Services",()=>Go("Services")),Button("Create restore point",()=>Go("Restore"))));
        if(preferences.Mode=="Safe")body.Children.Add(Button("Unlock choices with tradeoffs",()=> {SavePreferences(preferences with {Mode="Balanced"});Render();}));
        var generation=++lowEndGeneration;var options=new StackPanel();var optional=new StackPanel();var selected=new Dictionary<CheckBox,(Optimization Op,LowEndChoice Choice)>();var status=Text("Reading current values…",13,"#5EE0BA",true);
        var availableIds=new HashSet<string>();var configuredIds=new HashSet<string>();bool loading=true,updating=false;
        var presetButtons=Row();presetButtons.IsEnabled=false;
        var apply=PrimaryButton("Apply selected low-end profile",()=>Run(async ()=>
        {
            RequireScan();var previews=new List<ActionPreview>();
            foreach(var entry in selected.Where(x=>x.Key.IsChecked==true).Select(x=>x.Value))
            {
                if(ModeReason(entry.Op) is {} blocked)throw new InvalidOperationException(blocked);
                previews.Add(FeatureToggles.Supports(entry.Op)?await SettingsWork(()=>engine.PreviewToggle(entry.Op,vm.Hardware,entry.Choice.Enabled)):await SettingsWork(()=>engine.Preview(entry.Op,vm.Hardware)));
            }
            if(previews.Count==0)throw new InvalidOperationException("Select an available change first.");Watch("Low-end PC profile");
            var result=await SettingsWork(()=>new TuningPlanRunner(engine).Apply(previews,vm.Hardware));
            vm.Status=$"{result.Applied} changes applied; {result.AlreadyConfigured} already configured."+(result.Error==null?" Originals saved in Recovery.":" Stopped: "+result.Error+" Previous changes are saved in Recovery.");Render();
        }));apply.IsEnabled=false;

        body.Children.Add(Card(Panel(Text("Your low-end profile",21,"#F1F5FC",true),Text("Pick a starting selection, adjust it, then apply. These buttons only select choices; they do not change Windows.",13,"#BDCCE0"),presetButtons,status,apply,options)));
        var optionalPanel=new Expander {Header="More choices · browsers, downloads & Windows",Content=optional,Margin=new(0,12,0,4)};
        foreach(var name in new[]{"Recommended","Browser memory","Clear selection"})presetButtons.Children.Add(Button(name,()=>SetSelection(ProfileSelection.Resolve(ProfileSelection.Preset(name),availableIds,configuredIds),true)));
        void UpdateSelection()
        {
            var count=selected.Keys.Count(x=>x.IsEnabled&&x.IsChecked==true);status.Text=loading?"Checking which choices fit this PC…":count==0?"Nothing selected. Choose a starting selection or open More choices.":count+(count==1?" selected change":" selected changes")+" · your choices are remembered";apply.IsEnabled=!loading&&count>0;
        }
        void SetSelection(ISet<string> ids,bool save)
        {
            if(ids.Any(id=>LowEndPlans.Choices.Any(x=>x.OperationId==id&&!x.Selected)))optionalPanel.IsExpanded=true;
            updating=true;try{foreach(var (check,entry) in selected)check.IsChecked=check.IsEnabled&&ids.Contains(entry.Op.Id);}finally{updating=false;}
            if(save&&!loading){SavePreferences(preferences with {LowEndSelection=ids.Order().ToArray()});}UpdateSelection();
        }
        async Task Read()
        {
            int available=0;
            foreach(var choice in LowEndPlans.Choices)
            {
                var op=Catalog.All.Single(x=>x.Id==choice.OperationId);var check=new CheckBox {Content=choice.Title,FontSize=17,IsEnabled=false,Margin=new(0,12,0,4)};var state=Text("Checking…",12,"#BDCCE0");selected[check]=(op,choice);(choice.Selected?options:optional).Children.Add(Panel(check,Text(choice.Description,13,"#BDCCE0"),state));
                void Changed()
                {
                    if(loading||updating)return;
                    var current=selected.Where(x=>x.Key.IsChecked==true).Select(x=>x.Value.Op.Id);
                    SetSelection(ProfileSelection.Toggle(current,op.Id,check.IsChecked==true,availableIds,configuredIds),true);
                }
                check.Checked+=(_,_)=>Changed();check.Unchecked+=(_,_)=>Changed();
                try
                {
                    if(!scanned)throw new InvalidOperationException("Scan PC first.");if(op.Admin&&!system.IsAdministrator)throw new InvalidOperationException("Administrator access is needed for this choice.");if((op.Incompatibility(vm.Hardware)??ModeReason(op)) is {} reason)throw new InvalidOperationException(reason);
                    var value=await ReadSetting(op);var target=FeatureToggles.Supports(op)?FeatureToggles.Enabled(op,value)==choice.Enabled?value:FeatureToggles.Target(op,choice.Enabled):op.Kind==ActionKind.PowerSetting?PowerSettings.Target(op,value):new SettingValue(true,op.Kind==ActionKind.PowerPlan?op.Target:op.Value,op.Kind==ActionKind.PowerPlan?"Guid":"DWord");var preview=new ActionPreview(op,value,target);
                    if(generation!=lowEndGeneration||page!="Low-end PC")return;
                    var configured=preview.Current==preview.Proposed;
                    state.Text=configured?"Already configured · no change needed":"Ready · original will be saved";check.IsEnabled=!configured;if(configured)configuredIds.Add(op.Id);else availableIds.Add(op.Id);available++;
                }
                catch(OperationCanceledException) when(generation!=lowEndGeneration||page!="Low-end PC"){return;}
                catch(Exception error){if(generation!=lowEndGeneration||page!="Low-end PC")return;state.Text=error.Message;check.IsChecked=false;}
            }
            if(generation!=lowEndGeneration||page!="Low-end PC")return;
            options.Children.Add(optionalPanel);optionalPanel.Header=$"More choices · {LowEndPlans.Choices.Count(x=>!x.Selected)} optional settings";loading=false;presetButtons.IsEnabled=true;SetSelection(ProfileSelection.Resolve(preferences.LowEndSelection??ProfileSelection.Preset("Recommended").ToArray(),availableIds,configuredIds),false);
            if(available==0)status.Text="Scan PC and unlock choices with tradeoffs to see available settings.";
        }
        lowEndRead=Read();
        foreach(var group in engine.History.Where(x=>x.Session.StartsWith("lowend-",StringComparison.Ordinal)&&x.Status!="Restored").GroupBy(x=>x.Session).Reverse())
        {
            var session=group.Key;body.Children.Add(Card(Panel(Text("Saved low-end profile",18,"#F1F5FC",true),Text($"{group.First().Time.ToLocalTime():g} · {group.Count()} remaining changes",13,"#BDCCE0"),Button("Restore this profile",()=>Run(async ()=>
            {
                Watch("Restore low-end PC profile");var results=await SettingsWork(()=>new TuningPlanRunner(engine).Undo(session));vm.Status=results.All(x=>x.Status=="Restored")?"Profile restored and verified.":"Restore stopped: "+results.Last().Error;Render();
            })))));
        }
        body.Children.Add(Text("Change one profile at a time and compare the same game scene. These changes do not bypass CPU, RAM or GPU limits. Fewer processes alone is not proof of faster gaming.",13,"#BDCCE0"));
    }
}
