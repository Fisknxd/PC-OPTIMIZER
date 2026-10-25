using System.IO;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Automation;
using FrameForge.Core;

namespace FrameForge.App;

internal sealed class FeatureTestSettings:ISystemSettings
{
    public bool Administrator {get;set;}
    public bool IsAdministrator=>Administrator;
    public readonly Dictionary<string,SettingValue> States=Catalog.All.Where(FeatureToggles.Supports).ToDictionary(x=>x.Id,x=>FeatureToggles.Target(x,false));
    public int Writes {get;private set;}
    public int Reads {get;private set;}
    public Task<SettingValue> Read(Optimization operation){ Reads++;return Task.FromResult(States.TryGetValue(operation.Id,out var value)?value:operation.Kind==ActionKind.PowerSetting?PowerSettings.Pack(new(new Guid("381b4222-f694-41f0-9685-ff5bb260df2e"),operation.Id=="cpu-ac-limit"?70u:2u,operation.Id=="cpu-ac-limit"?60u:3u)):operation.Kind==ActionKind.Service?OptionalServices.Pack(new("Manual",false)):new(false,null)); }
    public Task Write(Optimization operation,SettingValue value) { States[operation.Id]=value; Writes++; return Task.CompletedTask; }
}

public sealed partial class MainWindow
{
    internal async Task ValidateFeatureToggles(string output,FeatureTestSettings fake)
    {
        Directory.CreateDirectory(output); Width=1360; Height=900; vm.Hardware=new(){WindowsSupported=true,Laptop=false}; scanned=true;
        fake.Administrator=true;
        preferences=preferences with {Mode="Balanced"}; featureSection="All tweaks"; Go("Optimize"); UpdateLayout();await WaitForUiWork();
        var switches=Descendants<ToggleButton>(body).Where(x=>x.IsVisible&&x.Style==FindResource("FeatureSwitch")).ToList();
        if(switches.Count!=TweakDefinitions.All.Count||switches.Any(x=>!x.IsEnabled||x.IsChecked!=false)) throw new InvalidDataException("Three enabled, initially-off feature switches must be visible.");
        foreach(var toggle in switches)
        {
            var name=(string)toggle.GetValue(AutomationProperties.NameProperty);
            var op=Catalog.All.Single(x=>x.Name==name); var original=fake.States[op.Id];
            var dependency=op.Id switch {"chrome-memory-strong"=>"chrome-memory","edge-sleep-sooner"=>"edge-sleep",_=>null};
            if(dependency!=null)fake.States[dependency]=FeatureToggles.Target(Catalog.All.Single(x=>x.Id==dependency),true);
            FeatureConfirmation=_=>false;
            toggle.IsChecked=true; toggle.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            await WaitForUiWork();
            if(fake.States[op.Id]!=original||toggle.IsChecked!=false) throw new InvalidDataException("Cancelled toggle changed a feature.");
            FeatureConfirmation=_=>true;
            toggle.IsChecked=true; toggle.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            await WaitForUiWork();
            if(toggle.IsChecked!=true||fake.States[op.Id]!=FeatureToggles.Target(op,true)) throw new InvalidDataException("Enable click was not verified.");
            toggle.IsChecked=false; toggle.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            await WaitForUiWork();
            if(toggle.IsChecked!=false||fake.States[op.Id]!=FeatureToggles.Target(op,false)) throw new InvalidDataException("Disable click was not verified.");
            if(dependency!=null)fake.States[dependency]=FeatureToggles.Target(Catalog.All.Single(x=>x.Id==dependency),false);
        }
        if(fake.Writes!=TweakDefinitions.All.Count*2||engine.History.Count!=TweakDefinitions.All.Count*2) throw new InvalidDataException("Each confirmed toggle must be journaled once.");
        preferences=preferences with {Mode="Safe"}; Render(); UpdateLayout();
        var safeHidden=Descendants<ToggleButton>(body).Single(x=>(string)x.GetValue(AutomationProperties.NameProperty)=="Show hidden files");
        if(safeHidden.IsEnabled) throw new InvalidDataException("Safe mode must not bypass the hidden-files review requirement.");
        preferences=preferences with {Mode="Balanced"}; Render(); UpdateLayout();
        switches=Descendants<ToggleButton>(body).Where(x=>x.IsVisible&&x.Style==FindResource("FeatureSwitch")).ToList();
        // Capture an On state through the same click path.
        switches[0].IsChecked=true; switches[0].RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); await WaitForUiWork();
        UpdateLayout(); Capture(this,Path.Combine(output,"Features-toggles.png"));
        var cards=Descendants<TweakCardGrid>(body).Single();
        var first=(System.Windows.FrameworkElement)cards.Children[0];var second=(System.Windows.FrameworkElement)cards.Children[1];
        if(second.TranslatePoint(new(0,0),cards).X<=first.TranslatePoint(new(0,0),cards).X)throw new InvalidDataException("Wide tweak cards did not use two columns.");
        Width=MinWidth; Height=MinHeight; UpdateLayout(); Capture(this,Path.Combine(output,"Features-toggles-compact.png"));
        if(second.TranslatePoint(new(0,0),cards).Y<=first.TranslatePoint(new(0,0),cards).Y)throw new InvalidDataException("Tweak cards did not reflow after narrowing the window.");
        Width=1360; Height=900;
        featureSection="Windows controls"; Render(); UpdateLayout();
        if(Descendants<ToggleButton>(body).Any(x=>x.IsVisible&&x.Style==FindResource("FeatureSwitch"))) throw new InvalidDataException("Manual Windows controls must not masquerade as switches.");
        Capture(this,Path.Combine(output,"Features-Windows-controls.png"));
        OpenFeatures(); UpdateLayout();
        if(featureSection!="For gaming"||Descendants<ToggleButton>(body).Count(x=>x.IsVisible&&x.Style==FindResource("FeatureSwitch"))!=Catalog.All.Count(PerformanceTweaks.Relevant))
            throw new InvalidDataException("Feature toggles shortcut must always reveal the switches.");
        foreach(var record in engine.History.AsEnumerable().Reverse())
            if((await engine.Undo(record.Id)).Status!="Restored") throw new InvalidDataException("Feature undo failed in the integration test.");
        if(fake.States.Any(x=>x.Value!=FeatureToggles.Target(Catalog.All.Single(o=>o.Id==x.Key),false))) throw new InvalidDataException("Undo did not preserve originals.");
        FeatureConfirmation=null;
        Render(); UpdateLayout();
        Exception? reviewError=null;
        _=Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle,new Action(()=>
        {
            Window? dialog=null;
            try
            {
                dialog=OwnedWindows.Cast<Window>().Single(); dialog.UpdateLayout();
                Capture(dialog,Path.Combine(output,"Features-confirmation.png"));
            }
            catch(Exception error) { reviewError=error; }
            finally { if(dialog!=null) dialog.DialogResult=false; }
        }));
        var cancelledSwitch=Descendants<ToggleButton>(body).First(x=>x.IsVisible&&x.Style==FindResource("FeatureSwitch"));
        var beforeCancel=fake.Writes; cancelledSwitch.IsChecked=true; cancelledSwitch.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        await WaitForUiWork();
        if(reviewError!=null) throw new InvalidDataException("Feature confirmation failed to render.",reviewError);
        if(fake.Writes!=beforeCancel||cancelledSwitch.IsChecked!=false) throw new InvalidDataException("Cancelling the real review dialog changed a setting.");
        File.WriteAllText(Path.Combine(output,"feature-toggles.txt"),$"Passed: {TweakDefinitions.All.Count} discoverable switches; cancel leaves state unchanged; each enable/disable click is verified and journaled; exact undo in reverse order; compact layout; Windows controls have no fake switches. All writes used an in-memory settings adapter and isolated journal. No real Windows settings changed.");
    }
}
