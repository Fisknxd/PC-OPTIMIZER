using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using FrameForge.Core;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace FrameForge.App;
public sealed partial class MainWindow
{
    internal void ValidateAppearance(string output)
    {
        Directory.CreateDirectory(output);
        CheckContrast(heading.Foreground,Background,"Main heading on the window");
        if(Background is not SolidColorBrush background||background.Color!=Color.FromRgb(11,18,32)) throw new InvalidDataException("MainWindow did not receive the dark canvas brush.");
        var report=new List<string>(); int dropdowns=0;
        foreach(var name in navigation.Keys.ToArray())
        {
            Go(name); UpdateLayout();
            var safe=name.Replace(" ","-");
            foreach(var combo in Descendants<ComboBox>(this))
            {
                combo.ApplyTemplate(); CheckContrast(combo.Foreground,combo.Background,name+" selected dropdown");
                combo.IsDropDownOpen=true; Dispatcher.Invoke(DispatcherPriority.ApplicationIdle,new Action(()=>{})); combo.UpdateLayout();
                if(combo.Template.FindName("PopupSurface",combo) is not Border popup) throw new InvalidDataException("Dropdown popup template is missing on "+name);
                CheckContrast(combo.Foreground,popup.Background,name+" expanded dropdown");
                popup.UpdateLayout(); Capture(popup,Path.Combine(output,$"{safe}-dropdown-{dropdowns}.png")); dropdowns++;
                combo.IsDropDownOpen=false;
            }
            foreach(var list in Descendants<ListBox>(this)) CheckContrast(list.Foreground,list.Background,name+" list");
            Capture(this,Path.Combine(output,safe+".png")); report.Add(name+": rendered; dark canvas and applicable controls checked.");
        }
        if(dropdowns<4) throw new InvalidDataException("Expected Optimize, Gaming and Settings dropdowns to be exercised.");
        File.WriteAllLines(Path.Combine(output,"report.txt"),report.Append($"{dropdowns} dropdowns checked; foreground/background contrast >= 4.5:1. No system changes executed."));
    }
    internal async Task ValidateFeatureInteractions(string output)
    {
        if(system is not UiTestSettings) throw new InvalidOperationException("Interaction checks require the isolated test adapter.");
        Directory.CreateDirectory(output);
        vm.Hardware=new HardwareSnapshot {WindowsSupported=true,Laptop=false};scanned=true;
        preferences=preferences with {Mode="Balanced",TipsDismissed=true};
        await RefreshFeatureStates();filter="Quick switches";Go("Optimize");UpdateLayout();
        var results=new List<string>();
        foreach(var op in Catalog.All.Where(o=>o.CanToggle))
        {
            var toggle=Descendants<ToggleButton>(this).Single(t=>Equals(t.Tag,op.Id));
            if(!toggle.IsEnabled) throw new InvalidDataException("Expected an available switch: "+op.Id);
            var peer=new ToggleButtonAutomationPeer(toggle);
            ((IToggleProvider)peer.GetPattern(PatternInterface.Toggle)).Toggle();
            await WaitForAction();
            if(featureStates[op.Id].Enabled!=true||engine.History.Last().Status!="Applied") throw new InvalidDataException("Enable click failed: "+op.Id);
            toggle=Descendants<ToggleButton>(this).Single(t=>Equals(t.Tag,op.Id));
            ((IToggleProvider)new ToggleButtonAutomationPeer(toggle).GetPattern(PatternInterface.Toggle)).Toggle();
            await WaitForAction();
            if(featureStates[op.Id].Enabled!=false) throw new InvalidDataException("Disable click failed: "+op.Id);
            ((IInvokeProvider)new ButtonAutomationPeer(undoButton).GetPattern(PatternInterface.Invoke)).Invoke();
            await WaitForAction();
            if(featureStates[op.Id].Enabled!=true) throw new InvalidDataException("Undo click failed: "+op.Id);
            results.Add(op.Id+": actual WPF enable, disable and Undo clicks passed using the fake system adapter.");
        }
        var favorite=Descendants<Button>(this).Single(b=>Equals(b.GetValue(System.Windows.Automation.AutomationProperties.AutomationIdProperty),"favorite-dark"));
        ((IInvokeProvider)new ButtonAutomationPeer(favorite).GetPattern(PatternInterface.Invoke)).Invoke();
        await Dispatcher.InvokeAsync(()=>{},DispatcherPriority.ApplicationIdle);
        if(!Favorite("dark")||store.Load("preferences.json",()=>new Preferences()).Favorites?.Contains("dark")!=true) throw new InvalidDataException("Favorite click did not persist.");
        filter="Favorites";Render();UpdateLayout();if(Descendants<ToggleButton>(this).Count(t=>t.Tag is string)==0) throw new InvalidDataException("Favorites filter lost the saved switch.");
        results.Add("Favorites: pin click, persistence and filter passed.");
        filter="Quick switches";Render();
        File.WriteAllLines(Path.Combine(output,"interactions.txt"),results.Append("All system mutations were redirected to the in-memory test adapter."));
    }
    async Task WaitForAction()
    {
        await Dispatcher.InvokeAsync(()=>{},DispatcherPriority.ApplicationIdle);
        var until=DateTime.UtcNow.AddSeconds(5);
        while(vm.Busy&&DateTime.UtcNow<until) await Task.Delay(20);
        if(vm.Busy) throw new TimeoutException("A switch action did not finish.");
        UpdateLayout();
    }
    static IEnumerable<T> Descendants<T>(DependencyObject parent) where T:DependencyObject
    {
        for(int i=0;i<VisualTreeHelper.GetChildrenCount(parent);i++)
        {
            var child=VisualTreeHelper.GetChild(parent,i);
            if(child is T match) yield return match;
            foreach(var descendant in Descendants<T>(child)) yield return descendant;
        }
    }
    static void CheckContrast(Brush foreground,Brush background,string context)
    {
        if(foreground is not SolidColorBrush fg||background is not SolidColorBrush bg) throw new InvalidDataException("Expected solid theme colors: "+context);
        static double L(Color c)
        {
            static double Channel(byte b) { double v=b/255d; return v<=.04045?v/12.92:Math.Pow((v+.055)/1.055,2.4); }
            return .2126*Channel(c.R)+.7152*Channel(c.G)+.0722*Channel(c.B);
        }
        double a=L(fg.Color),b=L(bg.Color),ratio=(Math.Max(a,b)+.05)/(Math.Min(a,b)+.05);
        if(ratio<4.5) throw new InvalidDataException($"Unreadable theme colors ({ratio:F2}:1): {context}");
    }
    static void Capture(FrameworkElement element,string path)
    {
        var width=(int)Math.Ceiling(element.ActualWidth); var height=(int)Math.Ceiling(element.ActualHeight);
        if(width<=0||height<=0) throw new InvalidDataException("A view did not render: "+path);
        var bitmap=new RenderTargetBitmap(width,height,96,96,PixelFormats.Pbgra32); bitmap.Render(element);
        var png=new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(bitmap)); using var file=File.Create(path); png.Save(file);
    }
}
