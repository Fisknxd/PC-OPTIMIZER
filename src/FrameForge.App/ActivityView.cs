using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FrameForge.Core;
using Microsoft.Win32;
namespace FrameForge.App;

public sealed partial class MainWindow
{
    Window? activityWindow;
    TextBox? activityText;
    bool watchAutomatically=true;
    void Watch(string title)
    {
        if(watchAutomatically) ShowActivity();
        activity.Report("── "+title+" ──");
    }
    void ShowActivity()
    {
        if(activityWindow!=null) {activityWindow.Show();activityWindow.Activate();return;}
        var window=new Window {Owner=this,Title="FrameForge · Live changes",Width=850,Height=610,MinWidth=600,MinHeight=380,Background=Brush("#0B1220"),Foreground=Brush("#F1F5FC"),WindowStartupLocation=WindowStartupLocation.CenterOwner};
        activityWindow=window;
        var root=new DockPanel {Margin=new(22)};
        var title=Panel(Text("Watch your changes",23,"#5EE0BA",true),Text("Actual Windows operations, PowerShell output and read-back checks. Native changes appear here as they run.",13,"#BDCCE0")); DockPanel.SetDock(title,Dock.Top);root.Children.Add(title);
        activityText=new TextBox {IsReadOnly=true,AcceptsReturn=true,TextWrapping=TextWrapping.Wrap,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,FontFamily=new FontFamily("Consolas"),FontSize=13,Background=Brush("#101A2B"),Foreground=Brush("#CDE9DF"),Padding=new(14)};
        var auto=new CheckBox {Content="Open when applying changes",IsChecked=watchAutomatically,Margin=new(5,12,12,5)};
        auto.Click+=(_,_)=>
        {
            try {var updated=preferences with {WatchChanges=auto.IsChecked==true};store.Save("preferences.json",updated);preferences=updated;watchAutomatically=updated.WatchChanges;}
            catch(Exception error){auto.IsChecked=watchAutomatically;activity.Report("Could not save live-window preference: "+error.Message);}
        };
        var bottom=Row(auto,Button("Save log",()=>
        {
            try {var dialog=new SaveFileDialog {FileName="FrameForge-live-changes.txt",Filter="Text log|*.txt"};if(dialog.ShowDialog(window)==true)File.WriteAllLines(dialog.FileName,activity.Lines.Select(x=>$"{x.Time:HH:mm:ss.fff}  {x.Message}"));}
            catch(Exception e){MessageBox.Show(window,e.Message,"Could not save log");}
        }),Button("Close",()=>window.Close()));DockPanel.SetDock(bottom,Dock.Bottom);root.Children.Add(bottom);root.Children.Add(activityText);
        foreach(var line in activity.Lines) Append(line);
        activity.Added+=Added;
        void Added(ActivityLine line) {if(!Dispatcher.HasShutdownStarted)Dispatcher.BeginInvoke(new Action(()=>Append(line)));}
        void Append(ActivityLine line) {activityText?.AppendText($"{line.Time:HH:mm:ss.fff}  {line.Message}\n");if(activityText?.LineCount>6000)activityText.Text=string.Join("\n",activity.Lines.Select(x=>$"{x.Time:HH:mm:ss.fff}  {x.Message}"));activityText?.ScrollToEnd();}
        window.Closed+=(_,_)=> {activity.Added-=Added;activityWindow=null;activityText=null;};window.Content=root;window.Show();
    }
}
