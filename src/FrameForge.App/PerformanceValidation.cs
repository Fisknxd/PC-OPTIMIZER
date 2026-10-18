using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using FrameForge.Core;
namespace FrameForge.App;
public sealed partial class MainWindow
{
    internal async Task ValidatePerformanceControls(string output,FeatureTestSettings fake)
    {
        preferences=preferences with {Mode="Advanced"};featureSection="Power checks";Go("Optimize");await Task.WhenAll(powerReads);UpdateLayout();
        foreach(var op in Catalog.All.Where(x=>x.Kind==ActionKind.PowerSetting))
        {
            var before=await fake.Read(op);var writes=fake.Writes;
            Border FindCard()=>Descendants<Border>(body).Single(b=>b.Child is StackPanel p&&p.Children.OfType<TextBlock>().Any(t=>t.Text==op.Name));
            var button=Descendants<Button>(FindCard()).Single(b=>b.Content as string=="Review change");
            PowerConfirmation=_=>false;button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));await System.Windows.Threading.Dispatcher.Yield();
            if(fake.Writes!=writes||await fake.Read(op)!=before)throw new InvalidDataException("Power cancellation changed a setting.");
            PowerConfirmation=_=>true;button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));await System.Windows.Threading.Dispatcher.Yield();await Task.WhenAll(powerReads);
            if(await fake.Read(op)!=PowerSettings.Target(op,before))throw new InvalidDataException("Power button did not save the requested plugged-in value.");
            if(Descendants<Button>(FindCard()).Any(x=>x.Content as string=="Review change"&&x.IsEnabled))throw new InvalidDataException("An already-configured power control remained enabled.");
            var undo=Descendants<Button>(FindCard()).Single(x=>x.Content as string=="Undo this power change");undo.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));await System.Windows.Threading.Dispatcher.Yield();await Task.WhenAll(powerReads);
            if(engine.History.Last(x=>x.OperationId==op.Id).Status!="Restored"||await fake.Read(op)!=before)throw new InvalidDataException("Power undo button did not preserve the exact plan and AC/DC values.");
            Render();await Task.WhenAll(powerReads);UpdateLayout();
        }
        Capture(this,Path.Combine(output,"Power-checks-simulated.png"));PowerConfirmation=null;
        // Browser dependencies run through the real switch handler, with isolated values.
        featureSection="Browser memory";Go("Optimize");FeatureConfirmation=_=>true;
        foreach(var id in new[]{"chrome-memory","chrome-memory-strong","edge-sleep","edge-sleep-sooner"})
        {
            var op=Catalog.All.Single(x=>x.Id==id);var toggle=Descendants<ToggleButton>(body).Single(x=>(string)x.GetValue(System.Windows.Automation.AutomationProperties.NameProperty)==op.Name);
            toggle.IsChecked=true;toggle.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));await System.Windows.Threading.Dispatcher.Yield();
            if(FeatureToggles.Enabled(op,await fake.Read(op))!=true)throw new InvalidDataException("Browser dependency enable path failed.");
        }
        Capture(this,Path.Combine(output,"Browser-memory-simulated.png"));FeatureConfirmation=null;
        foreach(var record in engine.History.Where(x=>x.Status!="Restored").AsEnumerable().Reverse())if((await engine.Undo(record.Id)).Status!="Restored")throw new InvalidDataException("Performance validation undo failed.");
        featureSection="For gaming";desktopSection="All desktop";Go("Quality of Life");UpdateLayout();
        var expected=Catalog.All.Count(x=>FeatureToggles.Supports(x)&&TweakDefinitions.Get(x).Group is "Appearance" or "Less clutter");
        if(Descendants<ToggleButton>(body).Count(x=>x.Style==FindResource("FeatureSwitch"))!=expected)throw new InvalidDataException("Desktop preferences were hidden by the gaming filter.");
        File.WriteAllText(Path.Combine(output,"performance-ui.txt"),"Power control cancellation, apply, already-configured state and exact undo passed for both controls. Browser dependency switches passed through actual UI handlers. In-memory settings only; no real policies, power plans, browser sessions or game files were changed.");
    }
}
