using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using FrameForge.Core;
namespace FrameForge.App;
public sealed partial class MainWindow
{
    internal async Task ValidateExpandedControls(string output,FeatureTestSettings fake)
    {
        fake.Administrator=true;preferences=preferences with {Mode="Balanced"};Width=1360;Height=900;Go("Services");await Task.WhenAll(serviceReads);UpdateLayout();
        var toggles=Descendants<ToggleButton>(body).Where(x=>x.Style==FindResource("FeatureSwitch")).ToList();
        if(toggles.Count!=OptionalServices.All.Count||toggles.Any(x=>!x.IsEnabled))throw new InvalidDataException("All simulated optional-service controls must be enabled.");
        for(int i=0;i<toggles.Count;i++)
        {
            var toggle=toggles[i];var op=Catalog.All.Single(x=>x.Kind==ActionKind.Service&&x.Target==OptionalServices.All[i].Name);var original=await fake.Read(op);var writes=fake.Writes;
            ServiceConfirmation=(_,_)=>false;toggle.IsChecked=true;toggle.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));await System.Windows.Threading.Dispatcher.Yield();
            if(fake.Writes!=writes||toggle.IsChecked!=false)throw new InvalidDataException("Cancelled service click changed state.");
            ServiceConfirmation=(_,_)=>true;toggle.IsChecked=true;toggle.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));await System.Windows.Threading.Dispatcher.Yield();
            if(await fake.Read(op)!=OptionalServices.Paused||toggle.IsChecked!=true)throw new InvalidDataException("Service pause click failed.");
            toggle.IsChecked=false;toggle.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));await System.Windows.Threading.Dispatcher.Yield();
            if(await fake.Read(op)!=original||toggle.IsChecked!=false)throw new InvalidDataException("Service restore did not return its original state.");
        }
        ServiceConfirmation=null;Capture(this,Path.Combine(output,"Services-simulated-roundtrip.png"));
        if(gameFiles is not GraphicsTestFiles fixture)throw new InvalidDataException("Graphics integration requires isolated fixtures.");
        foreach(var game in new[]{"Fortnite","Rust"})
        {
            potatoGame=game;Go("Potato Settings");if(potatoRead!=null)await potatoRead;UpdateLayout();var original=fixture.Files[game].ToArray();
            var button=Descendants<Button>(body).Single(x=>x.Content as string=="Apply Potato Settings");if(!button.IsEnabled)throw new InvalidDataException("One-click graphics apply is unavailable for a valid fixture.");
            button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));await System.Windows.Threading.Dispatcher.Yield();if(potatoRead!=null)await potatoRead;
            if(original.AsSpan().SequenceEqual(fixture.Files[game])||graphicsProfiles.History.Last().Status!="Applied")throw new InvalidDataException("One-click graphics apply did not write and verify.");
            Capture(this,Path.Combine(output,"Potato-"+game+"-applied-simulated.png"));
            var restore=Descendants<Button>(body).Single(x=>x.Content as string=="Restore original graphics");restore.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));await System.Windows.Threading.Dispatcher.Yield();if(potatoRead!=null)await potatoRead;
            if(!original.AsSpan().SequenceEqual(fixture.Files[game]))throw new InvalidDataException("Graphics original was not restored byte for byte.");
        }
        fake.States["edge-background"]=FeatureToggles.Target(Catalog.All.Single(x=>x.Id=="edge-background"),true);
        Go("Low-end PC");if(lowEndRead!=null)await lowEndRead;UpdateLayout();var prior=engine.History.Count;
        var tune=Descendants<Button>(body).Single(x=>x.Content as string=="Apply selected low-end profile");tune.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));await System.Windows.Threading.Dispatcher.Yield();if(lowEndRead!=null)await lowEndRead;
        if(engine.History.Count<=prior)throw new InvalidDataException("Low-end profile click did not apply its selected fixture changes.");
        Capture(this,Path.Combine(output,"Low-end-PC-simulated.png"));
        var reverse=Descendants<Button>(body).Single(x=>x.Content as string=="Restore this profile");reverse.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));await System.Windows.Threading.Dispatcher.Yield();
        if(engine.History.Skip(prior).Any(x=>x.Status!="Restored"))throw new InvalidDataException("Low-end profile restore failed.");
        ShowActivity();await System.Windows.Threading.Dispatcher.Yield();activityWindow!.UpdateLayout();Capture(activityWindow,Path.Combine(output,"Live-changes-simulated.png"));
        if(!activityText!.Text.Contains("VERIFIED",StringComparison.Ordinal))throw new InvalidDataException("Live activity did not show verified operation results.");activityWindow.Close();
        File.WriteAllText(Path.Combine(output,"expanded-ui.txt"),"Simulated service cancel/pause/restore passed for every optional service. One-click Fortnite and Rust graphics apply/verify/restore passed against isolated fixtures. Low-end profile apply and grouped restore passed using in-memory Windows settings. No installed game configuration or Windows setting was changed.");
    }
}
