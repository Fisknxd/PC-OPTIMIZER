using System.IO;
using System.Windows;
using System.Windows.Controls;
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
