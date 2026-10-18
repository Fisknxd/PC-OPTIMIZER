using System.IO;
using System.Windows;
using System.Windows.Controls;
using FrameForge.Core;
namespace FrameForge.App;
public sealed partial class MainWindow
{
    internal async Task ValidateQualityControls(string output,FeatureTestSettings fake)
    {
        preferences=preferences with {Mode="Balanced",LowEndSelection=null};Go("Low-end PC");if(lowEndRead!=null)await lowEndRead;UpdateLayout();var writes=fake.Writes;
        Button FindButton(string text)=>Descendants<Button>(body).Single(x=>x.Content as string==text);
        CheckBox Choice(string id)=>Descendants<CheckBox>(body).Single(x=>x.Content as string==LowEndPlans.Choices.Single(c=>c.OperationId==id).Title);
        FindButton("Browser memory").RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));UpdateLayout();
        if(fake.Writes!=writes||Choice("chrome-memory").IsChecked!=true||Choice("chrome-memory-strong").IsChecked!=true)throw new InvalidDataException("Preset selection wrote Windows or omitted its prerequisite.");
        Choice("chrome-memory").IsChecked=false;if(Choice("chrome-memory-strong").IsChecked==true)throw new InvalidDataException("Unchecking a prerequisite left a dependent selected.");
        Choice("chrome-memory-strong").IsChecked=true;if(Choice("chrome-memory").IsChecked!=true)throw new InvalidDataException("Checking the stronger choice did not include its prerequisite.");
        Go("Dashboard");Go("Low-end PC");if(lowEndRead!=null)await lowEndRead;UpdateLayout();if(Choice("chrome-memory-strong").IsChecked!=true)throw new InvalidDataException("Profile selection was not retained.");
        Capture(this,Path.Combine(output,"Low-end-PC-preset-simulated.png"));FindButton("Clear selection").RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));Go("Dashboard");Go("Low-end PC");if(lowEndRead!=null)await lowEndRead;
        if(FindButton("Apply selected low-end profile").IsEnabled||preferences.LowEndSelection?.Length!=0)throw new InvalidDataException("Cleared selection was silently replaced by defaults.");
        FindButton("Recommended").RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));FindButton("Apply selected low-end profile").RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));await System.Windows.Threading.Dispatcher.Yield();if(lowEndRead!=null)await lowEndRead;
        var session=engine.History.Last().Session;var count=engine.History.Count(x=>x.Session==session&&x.Status!="Restored");if(count==0)throw new InvalidDataException("Recommended selection did not apply fixture changes.");
        recoveryFilter="Active changes";Go("Restore");UpdateLayout();var undo=FindButton("Undo session · "+count);writes=fake.Writes;
        SessionUndoConfirmation=_=>false;undo.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));await System.Windows.Threading.Dispatcher.Yield();if(fake.Writes!=writes)throw new InvalidDataException("Cancelled session undo wrote settings.");
        SessionUndoConfirmation=_=>true;undo.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));await System.Windows.Threading.Dispatcher.Yield();SessionUndoConfirmation=null;
        if(engine.History.Any(x=>x.Session==session&&x.Status!="Restored"))throw new InvalidDataException("Session undo click did not restore every fixture original.");
        recoveryFilter="All history";Render();UpdateLayout();Capture(this,Path.Combine(output,"Recovery-sessions-simulated.png"));
        var csv="Application,ProcessID,SwapChainAddress,FrameTime\n"+string.Join("\n",Enumerable.Repeat("first.exe,1,0x1,10",100).Concat(Enumerable.Repeat("second.exe,2,0x2,20",100)));
        var captures=BenchmarkImport.Read(csv);Exception? selectionError=null;
        _=Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle,new Action(()=>
        {
            Window? dialog=null;
            try{dialog=OwnedWindows.Cast<Window>().Single();Descendants<ComboBox>(dialog).Single().SelectedIndex=1;dialog.UpdateLayout();Capture(dialog,Path.Combine(output,"Benchmark-stream-picker-QA.png"));Descendants<Button>(dialog).Single(x=>x.Content as string=="Import selected recording").RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));}
            catch(Exception error){selectionError=error;if(dialog!=null)dialog.DialogResult=false;}
        }));
        var selected=ChooseBenchmarkCapture(captures);if(selectionError!=null||selected?.Application!="second.exe"||selected.ToTrace().Stats.AverageFps!=50)throw new InvalidDataException("CSV stream picker did not use the explicitly selected stream.",selectionError);
        importedTrace=selected.ToTrace();importedApplication=selected.Application;importedSource="SYNTHETIC-IMPORT-QA.csv · second.exe";monitorTab="Benchmarks";Go("Monitor");UpdateLayout();Capture(this,Path.Combine(output,"Benchmark-PresentMon-import-QA.png"));importedTrace=null;importedSource="";importedApplication="";monitorTab="Live resources";
        File.WriteAllText(Path.Combine(output,"quality-ui.txt"),"Profile preset selection/persistence/clear and automatic dependencies passed without system writes. Recommended fixture apply, cancelled session undo and confirmed session undo passed. PresentMon picker imported only the selected synthetic stream. No actual games, browser policies or power settings were changed.");
    }
}
