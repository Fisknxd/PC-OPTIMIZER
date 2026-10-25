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
    readonly List<Task> serviceReads=[];
    internal Func<Optimization,bool,bool>? ServiceConfirmation {get;set;}
    void ServicesView()
    {
        serviceReads.Clear();
        body.Children.Add(Text("Less running in the background",25,"#F1F5FC",true));
        body.Children.Add(Text("Pause only features you never use. Originals are saved in Recovery. Already-idle services may use almost no resources.",13,"#BDCCE0"));
        body.Children.Add(Row(Button("Refresh",RefreshSettings),Button("Windows services ↗",()=>WindowsSettings.Utility("Services (review only)")),Button("Create restore point",()=>Go("Restore"))));
        if(preferences.Mode=="Safe") body.Children.Add(Card(Panel(Text("Review mode needed",18,"#FFD08A",true),Text("Service changes need More choices or Experiments, a PC scan and administrator access.",13,"#BDCCE0"),Button("Use More choices",()=> {SavePreferences(preferences with {Mode="Balanced"});Render();}))));
        foreach(var op in Catalog.All.Where(x=>x.Kind==ActionKind.Service)) body.Children.Add(ServiceCard(op));
        body.Children.Add(Text("Windows security, updates, networking, audio, drivers and anti-cheat services are kept out of these controls. Pausing does not delete a service.",13,"#BDCCE0"));
    }
    Border ServiceCard(Optimization op)
    {
        var definition=OptionalServices.Get(op); var state=Text("Reading Windows…",13,"#BDCCE0",true);var note=Text("",13,"#BDCCE0");
        var toggle=new ToggleButton {Style=(Style)FindResource("FeatureSwitch"),IsEnabled=false};toggle.SetValue(AutomationProperties.NameProperty,"Pause "+definition.Title);
        var header=new Grid();header.ColumnDefinitions.Add(new());header.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
        header.Children.Add(Panel(Text(definition.Title,18,"#F1F5FC",true),Text(definition.Description,13,"#BDCCE0")));var controls=Panel(state,toggle);controls.Width=115;Grid.SetColumn(controls,1);header.Children.Add(controls);
        var content=Panel(header,note); bool paused=false;ChangeRecord? saved=null;
        var generation=pageGeneration;
        async Task Read(bool fresh=false)
        {
            try
            {
                var value=await ReadSetting(op,fresh);if(!IsCurrent(generation))return;var native=OptionalServices.Unpack(value);paused=value==OptionalServices.Paused;
                saved=engine.History.LastOrDefault(x=>x.OperationId==op.Id&&x.Status!="Restored");
                state.Text=paused?"● Paused":native.Running?"Running":"Idle";toggle.IsChecked=paused;
                var blocked=!scanned?"Scan PC before changing services.":op.Incompatibility(vm.Hardware)??ModeReason(op)??(!system.IsAdministrator?"Administrator access is needed to change services.":null);
                if(paused&&saved==null) blocked="Already disabled outside FrameForge. Use Windows Services to enable it; there is no saved original here.";
                if(!paused&&saved!=null) blocked="A saved change needs review in Recovery before changing this service again.";
                note.Text=blocked??(paused?"Turn off Pause to restore its saved startup mode and running state.":$"{native.StartMode} startup · Turn on Pause to stop this feature until restored.");
                toggle.IsEnabled=blocked==null;
            }
            catch(OperationCanceledException) when(!IsCurrent(generation)) { }
            catch(Exception e){if(!IsCurrent(generation))return;state.Text="Unavailable";note.Text=e.Message;toggle.IsEnabled=false;}
        }
        toggle.Click+=(_,_)=>
        {
            var requested=toggle.IsChecked==true;toggle.IsChecked=paused;
            Run(async ()=>
            {
                try
                {
                    RequireScan();if(ModeReason(op) is {} blocked)throw new InvalidOperationException(blocked);
                    if(!(ServiceConfirmation?.Invoke(op,requested)??(MessageBox.Show(this,(requested?"Pause ":"Restore ")+definition.Title+"?\n\n"+definition.Description+"\n\n"+(requested?"This stops the service and disables its startup until you restore it. Active dependent services block the change.":"The saved startup mode and running state will be restored."),"Review service change",MessageBoxButton.YesNo,MessageBoxImage.Question)==MessageBoxResult.Yes)))return;
                    if(requested)
                    {
                        var preview=await SettingsWork(()=>engine.Preview(op,vm.Hardware));Watch(op.Name);
                        var result=await SettingsWork(()=>engine.Apply(preview,vm.Hardware,Guid.NewGuid().ToString("N")));if(result?.Status=="Failed")throw new IOException(result.Error);
                    }
                    else
                    {
                        if(saved==null)throw new InvalidOperationException("No saved original is available.");Watch("Restore "+definition.Title);
                        var result=await SettingsWork(()=>engine.Undo(saved.Id));if(result.Status!="Restored")throw new IOException(result.Error);
                    }
                    vm.Status=definition.Title+(requested?" paused and verified.":" restored and verified.");
                }
                finally{await Read(true);}
            });
        };
        serviceReads.Add(Read());return Card(content);
    }
}
