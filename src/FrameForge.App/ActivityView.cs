using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using FrameForge.Core;
using Microsoft.Win32;
namespace FrameForge.App;

public sealed partial class MainWindow
{
    Window? activityWindow;
    TextBox? activityText;
    ActivityLogPump? activityPump;
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
        var title=Panel(Text("Watch your changes",23,"#5EE0BA",true),Text("Windows operations and checks appear here as they run. Scroll up to read earlier steps; turn on Follow latest to jump back to new entries.",13,"#BDCCE0")); DockPanel.SetDock(title,Dock.Top);root.Children.Add(title);
        var text=new TextBox {IsReadOnly=true,IsUndoEnabled=false,AcceptsReturn=true,TextWrapping=TextWrapping.Wrap,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,FontFamily=new FontFamily("Consolas"),FontSize=13,Background=Brush("#101A2B"),Foreground=Brush("#CDE9DF"),Padding=new(14)};
        activityText=text;
        var auto=new CheckBox {Content="Open when applying changes",IsChecked=watchAutomatically,Margin=new(5,12,12,5)};
        var follow=new CheckBox {Content="Follow latest",IsChecked=true,Margin=new(5,12,12,5)};
        follow.SetValue(System.Windows.Automation.AutomationProperties.NameProperty,"Follow latest log entries");
        auto.Click+=(_,_)=>
        {
            try {var updated=preferences with {WatchChanges=auto.IsChecked==true};store.Save("preferences.json",updated);preferences=updated;watchAutomatically=updated.WatchChanges;}
            catch(Exception error){auto.IsChecked=watchAutomatically;activity.Report("Could not save live-window preference: "+error.Message);}
        };
        var bottom=Row(auto,follow,Button("Save log",()=>
        {
            try {var dialog=new SaveFileDialog {FileName="FrameForge-live-changes.txt",Filter="Text log|*.txt"};if(dialog.ShowDialog(window)==true)File.WriteAllLines(dialog.FileName,activity.Lines.Select(x=>$"{x.Time:HH:mm:ss.fff}  {x.Message}"));}
            catch(Exception e){MessageBox.Show(window,e.Message,"Could not save log");}
        }),Button("Close",()=>window.Close()));DockPanel.SetDock(bottom,Dock.Bottom);root.Children.Add(bottom);root.Children.Add(text);
        var pump=new ActivityLogPump(activity,text,follow,Dispatcher);activityPump=pump;
        window.Closed+=(_,_)=>
        {
            pump.Dispose();
            if(ReferenceEquals(activityWindow,window)){activityWindow=null;activityText=null;activityPump=null;}
        };
        window.Content=root;window.Show();
    }

    // Output may arrive much faster than WPF can lay out a TextBox. One bounded batch
    // per timer tick keeps the dispatcher free; every window owns its own pump.
    sealed class ActivityLogPump:IDisposable
    {
        internal const int MaximumPending=2000,MaximumCharacters=200_000;
        readonly ActivityFeed feed;
        readonly TextBox text;
        readonly CheckBox follow;
        readonly DispatcherTimer timer;
        readonly object sync=new();
        readonly Queue<ActivityLine> pending=[];
        readonly HashSet<ActivityLine> seen=new(ReferenceEqualityComparer.Instance);
        readonly Queue<ActivityLine> seenOrder=[];
        bool disposed,updating;
        int omitted;
        internal int Batches {get;private set;}
        internal int PendingCount {get{lock(sync)return pending.Count;}}
        internal ActivityLogPump(ActivityFeed feed,TextBox text,CheckBox follow,Dispatcher dispatcher)
        {
            this.feed=feed;this.text=text;this.follow=follow;
            feed.Added+=Added;
            // Subscribe first; reference deduplication covers entries present in both
            // the snapshot and the event stream without losing an intervening line.
            foreach(var line in feed.Lines)Added(line);
            timer=new DispatcherTimer(TimeSpan.FromMilliseconds(100),DispatcherPriority.Background,(_,_)=>Flush(),dispatcher);
            text.AddHandler(ScrollViewer.ScrollChangedEvent,new ScrollChangedEventHandler(Scrolled));
            follow.Checked+=FollowChecked;
            Flush();timer.Start();
        }
        void Added(ActivityLine line)
        {
            lock(sync)
            {
                if(disposed||!seen.Add(line))return;
                seenOrder.Enqueue(line);if(seenOrder.Count>MaximumPending+1)seen.Remove(seenOrder.Dequeue());
                if(pending.Count==MaximumPending){pending.Dequeue();omitted++;}
                pending.Enqueue(line);
            }
        }
        void Scrolled(object sender,ScrollChangedEventArgs e)
        {
            if(!updating&&e.ExtentHeightChange==0&&e.VerticalChange<0&&text.VerticalOffset+text.ViewportHeight<text.ExtentHeight-1)follow.IsChecked=false;
        }
        void FollowChecked(object sender,RoutedEventArgs e)
        {
            updating=true;try{text.ScrollToEnd();}finally{updating=false;}
        }
        internal void Flush()
        {
            List<ActivityLine> batch;int skipped;
            lock(sync)
            {
                if(disposed||pending.Count==0)return;
                batch=pending.OrderBy(x=>x.Time).ToList();pending.Clear();skipped=omitted;omitted=0;
            }
            // Walk from the newest entry and stop at the display budget. A burst
            // of long output lines must not allocate/layout an unbounded batch.
            var formatted=new List<string>();int characters=0;
            for(int i=batch.Count-1;i>=0&&characters<MaximumCharacters;i--)
            {
                var line=batch[i];
                var message=line.Message.Length>16_000?line.Message[..16_000]+"\n… long entry shortened in the display; use Save log for retained entries.":line.Message;
                var entry=line.Time.ToString("HH:mm:ss.fff")+"  "+message+Environment.NewLine;formatted.Add(entry);characters+=entry.Length;
            }
            formatted.Reverse();var buffer=new StringBuilder(Math.Min(characters+100,MaximumCharacters+16_100));
            if(skipped>0)buffer.AppendLine($"… {skipped:N0} earlier entries left out of this display batch.");
            foreach(var entry in formatted)buffer.Append(entry);
            var addition=buffer.ToString();
            if(addition.Length>MaximumCharacters)addition=Tail(addition);
            var offset=text.VerticalOffset;var selection=text.SelectionStart;var selectionLength=text.SelectionLength;
            updating=true;
            try
            {
                int removed=0;
                if(text.Text.Length+addition.Length>MaximumCharacters)
                {
                    var combined=text.Text+addition;var retained=Tail(combined);removed=combined.Length-retained.Length;text.Text=retained;
                }
                else text.AppendText(addition);
                if(follow.IsChecked==true)text.ScrollToEnd();
                else
                {
                    var start=Math.Clamp(selection-removed,0,text.Text.Length);text.Select(start,Math.Min(selectionLength,text.Text.Length-start));text.ScrollToVerticalOffset(offset);
                }
                Batches++;
            }
            finally{updating=false;}
        }
        static string Tail(string value)
        {
            var start=Math.Max(0,value.Length-MaximumCharacters);var newline=value.IndexOf('\n',start);
            return value[(newline>=0&&newline<value.Length-1?newline+1:start)..];
        }
        public void Dispose()
        {
            lock(sync){if(disposed)return;disposed=true;pending.Clear();seen.Clear();seenOrder.Clear();}
            timer.Stop();feed.Added-=Added;follow.Checked-=FollowChecked;text.RemoveHandler(ScrollViewer.ScrollChangedEvent,new ScrollChangedEventHandler(Scrolled));
        }
    }
}
