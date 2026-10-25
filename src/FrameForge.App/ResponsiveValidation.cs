using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using FrameForge.Core;

namespace FrameForge.App;
public sealed partial class MainWindow
{
    async Task WaitForUiWork()
    {
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        var deadline=DateTime.UtcNow.AddSeconds(10);
        while(vm.Busy){if(DateTime.UtcNow>deadline)throw new TimeoutException("Fixture UI action did not finish.");await Task.Delay(5);}
        await Task.WhenAll(featureReads.Concat(serviceReads).Concat(powerReads).ToArray());
        UpdateLayout();
    }
    internal async Task ValidateResponsiveControls(string output,FeatureTestSettings fake)
    {
        preferences=preferences with {Mode="Balanced"};query="";search.Text="";searchDelay.Stop();featureSection="All tweaks";tweakFilter="All states";Go("Optimize");UpdateLayout();await WaitForUiWork();
        var categories=Descendants<ComboBox>(body).Single(x=>(string?)x.GetValue(AutomationProperties.NameProperty)=="Other tweak categories");
        categories.SelectedItem="Downloads";UpdateLayout();if(downloadLimitsLink?.Visibility!=Visibility.Visible)throw new InvalidDataException("Downloads guidance did not follow category selection.");
        Descendants<Button>(body).Single(x=>x.Content as string=="All tweaks").RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        if(categories.SelectedItem as string!="More categories")throw new InvalidDataException("Secondary category retained a stale selection.");
        Descendants<Button>(body).Single(x=>x.Content as string=="Browser memory").RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        if(tweakContext?.Text.Contains("Reload policies",StringComparison.Ordinal)!=true||downloadLimitsLink?.Visibility!=Visibility.Collapsed)throw new InvalidDataException("Browser guidance did not follow category selection.");
        Descendants<Button>(body).Single(x=>x.Content as string=="All tweaks").RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));UpdateLayout();await WaitForUiWork();
        var original=tweakCards["mouse-acceleration"];
        var reads=fake.Reads;search.Focus();search.Text="mouse";ApplySearch();UpdateLayout();
        if(!ReferenceEquals(original,tweakCards["mouse-acceleration"])||fake.Reads!=reads||!search.IsKeyboardFocusWithin)throw new InvalidDataException("Search recreated controls, reread settings or lost keyboard focus.");
        if(tweakCards.Where(x=>x.Value.Visibility==Visibility.Visible).Any(x=>!Catalog.Search("mouse","All",vm.Hardware).Any(o=>o.Id==x.Key)))throw new InvalidDataException("Search left unrelated cards visible.");
        search.Text="zz-no-such-setting-qa";ApplySearch();UpdateLayout();
        if(tweakCards.Values.Any(x=>x.Visibility==Visibility.Visible)||tweakResults?.Text.StartsWith("No matches",StringComparison.Ordinal)!=true)throw new InvalidDataException("Empty search did not explain the result.");
        search.Text="";ApplySearch();var stateFilter=Descendants<ComboBox>(body).Single(x=>(string?)x.GetValue(AutomationProperties.NameProperty)=="Filter tweaks by current state");stateFilter.SelectedItem="Off";UpdateLayout();await WaitForUiWork();
        if(tweakCards.Any(x=>x.Value.Visibility==Visibility.Visible&&tweakStates.GetValueOrDefault(x.Key)!="Off"))throw new InvalidDataException("Off-state filter showed an unrelated state.");
        categories.SelectedItem="Windows controls";UpdateLayout();if(tweakCards.Values.Any(x=>x.Visibility==Visibility.Visible))throw new InvalidDataException("Manual controls bypassed the Off-state filter.");
        stateFilter.SelectedItem="All states";Descendants<Button>(body).Single(x=>x.Content as string=="All tweaks").RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));UpdateLayout();
        var levels=Descendants<ComboBox>(body).Single(x=>(string?)x.GetValue(AutomationProperties.NameProperty)=="Feature review mode");
        if(levels.SelectionBoxItem?.ToString()!="More choices")throw new InvalidDataException("Review level exposes an internal object label.");
        pageScroll.ScrollToVerticalOffset(450);UpdateLayout();var offset=pageScroll.VerticalOffset;Go("Dashboard");Go("Optimize");await WaitForUiWork();
        if(offset<100||Math.Abs(pageScroll.VerticalOffset-offset)>2)throw new InvalidDataException("Navigation lost the saved page scroll position.");
        search.Text="mouse";if(!searchDelay.IsEnabled)throw new InvalidDataException("Search debounce did not start.");Go("Dashboard");if(searchDelay.IsEnabled)throw new InvalidDataException("Pending search can navigate after leaving the page.");search.Text="";searchDelay.Stop();query="";
        Go("Optimize");await WaitForUiWork();pageScroll.ScrollToTop();Capture(this,Path.Combine(output,"Responsive-tweaks-simulated.png"));

        void ExpandChoices(){foreach(var e in Descendants<Expander>(body).Where(x=>(x.Header as string)?.StartsWith("More choices",StringComparison.Ordinal)==true))e.IsExpanded=true;UpdateLayout();}
        fake.Administrator=false;preferences=preferences with {Mode="Advanced"};Go("Low-end PC");if(lowEndRead!=null)await lowEndRead;ExpandChoices();
        foreach(var op in Catalog.All.Where(x=>x.Admin&&LowEndPlans.Choices.Any(c=>c.OperationId==x.Id)))
        {
            var title=LowEndPlans.Choices.Single(c=>c.OperationId==op.Id).Title;
            if(Descendants<CheckBox>(body).Single(x=>x.Content as string==title).IsEnabled)throw new InvalidDataException("Standard access offered an administrator-only profile choice.");
        }
        fake.Administrator=true;var high=Catalog.All.Single(x=>x.Id=="high-performance");fake.States[high.Id]=new(true,high.Target,"Guid");Go("Low-end PC");if(lowEndRead!=null)await lowEndRead;ExpandChoices();
        if(Descendants<CheckBox>(body).Single(x=>x.Content as string==LowEndPlans.Choices.Single(c=>c.OperationId==high.Id).Title).IsEnabled)throw new InvalidDataException("An already-active power plan was offered as a new change.");
        fake.States.Remove(high.Id);preferences=preferences with {Mode="Balanced"};
        guideGroup="Storage";guideGame=Games.Names.Last();Go("Gaming");Go("Dashboard");Go("Gaming");
        if(!Descendants<ComboBox>(body).Any(x=>x.SelectedItem as string=="Storage")||!Descendants<ComboBox>(body).Any(x=>x.SelectedItem as string==guideGame))throw new InvalidDataException("Guide selections were lost on navigation.");
        connectionHost="fixture.invalid";Go("Tools");Go("Dashboard");Go("Tools");if(!Descendants<TextBox>(body).Any(x=>x.Text==connectionHost))throw new InvalidDataException("Connection input was lost on navigation.");connectionHost="";Go("Optimize");await WaitForUiWork();

        int started=0,active=0,peak=0;var release=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var reader=new SettingReadCoordinator(async _=>{Interlocked.Increment(ref started);var now=Interlocked.Increment(ref active);peak=Math.Max(peak,now);try{await release.Task;return new SettingValue(false,null);}finally{Interlocked.Decrement(ref active);}});
        var operations=Catalog.All.Where(FeatureToggles.Supports).Take(10).ToArray();var running=operations.Take(3).Select(x=>reader.Read(x)).ToArray();
        var deadline=DateTime.UtcNow.AddSeconds(5);while(Volatile.Read(ref started)<3){if(DateTime.UtcNow>deadline)throw new TimeoutException("Background read fixture did not start.");await Task.Delay(5);}
        bool heartbeat=false;await Dispatcher.InvokeAsync(()=>heartbeat=true,DispatcherPriority.Input);if(!heartbeat||running.Any(x=>x.IsCompleted))throw new InvalidDataException("Slow probes blocked the dispatcher.");
        using var cancelled=new CancellationTokenSource();var queued=operations.Skip(3).Select(x=>reader.Read(x,cancelled.Token)).ToArray();cancelled.Cancel();
        foreach(var task in queued){try{await task;throw new InvalidDataException("Cancelled page read completed.");}catch(OperationCanceledException){}}
        release.SetResult();await Task.WhenAll(running);if(started!=3||peak>3)throw new InvalidDataException("Read worker limit or navigation cancellation failed.");
        _=await reader.Read(operations[0]);if(started!=3)throw new InvalidDataException("A repeated recent read missed its cache.");reader.Invalidate();_=await reader.Read(operations[0]);if(started!=4)throw new InvalidDataException("Refresh did not reread Windows.");
        int failures=0;var retry=new SettingReadCoordinator(_=>++failures==1?Task.FromException<SettingValue>(new IOException("Synthetic refusal")):Task.FromResult(new SettingValue(false,null)));
        try{await retry.Read(operations[0]);throw new InvalidDataException("A refused read was accepted.");}catch(IOException){}
        _=await retry.Read(operations[0]);if(failures!=2)throw new InvalidDataException("A failed read was cached as a lasting result.");
        File.WriteAllText(Path.Combine(output,"responsive-ui.txt"),"Passed: search/filter preserves card identity and focus without setting rereads; no-match state; readable review labels; per-page scroll and guide/input retention; administrator-only choices blocked; already-active power plan recognized; pending search stops on navigation; blocking probes leave the input dispatcher responsive; at most three read workers; queued page reads cancel; explicit refresh bypasses cache. UI setting writes used isolated fixtures only.");
    }
}
