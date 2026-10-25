using System.IO;
using System.Windows.Controls;
using FrameForge.Core;
namespace FrameForge.App;

public sealed partial class MainWindow
{
    internal async Task ValidateMonitorAndActivity(string output)
    {
        Directory.CreateDirectory(output);
        var previousTrace=importedTrace;var previousPresentation=importedPresentation;var previousSource=importedSource;var previousApplication=importedApplication;var previousTab=monitorTab;
        try
        {
            ShowActivity();var pump=activityPump!;var log=activityText!;var window=activityWindow!;
            var follow=Descendants<CheckBox>(window).Single(x=>x.Content as string=="Follow latest");
            var batches=pump.Batches;
            // Produce a burst in one dispatcher turn. UI work must remain bounded
            // rather than scheduling 2,500 callbacks or relaying every line layout.
            for(int i=0;i<2500;i++)activity.Report($"SYNTHETIC LOG BURST {i:D4} "+new string('x',120));
            if(pump.PendingCount!=ActivityLogPump.MaximumPending)throw new InvalidDataException("Live log queue was not bounded.");
            pump.Flush();window.UpdateLayout();
            if(pump.Batches!=batches+1||pump.PendingCount!=0||log.Text.Length>ActivityLogPump.MaximumCharacters||!log.Text.Contains("SYNTHETIC LOG BURST 2499"))throw new InvalidDataException("Live log batching lost the latest line or exceeded the display bound.");
            follow.IsChecked=false;log.ScrollToHome();window.UpdateLayout();var offset=log.VerticalOffset;
            activity.Report("SYNTHETIC LOG: keep reading earlier steps");pump.Flush();window.UpdateLayout();
            if(follow.IsChecked!=false||Math.Abs(log.VerticalOffset-offset)>1)throw new InvalidDataException("New output moved the paused log reader.");
            activity.Report(new string('z',30_000));pump.Flush();
            if(!log.Text.Contains("long entry shortened")||log.Text.Length>ActivityLogPump.MaximumCharacters)throw new InvalidDataException("A long live-log entry was not bounded clearly.");
            var oldText=log.Text;window.Close();ShowActivity();var reopened=activityPump!;
            activity.Report("SYNTHETIC LOG: reopened window only");pump.Flush();reopened.Flush();
            if(log.Text!=oldText||!activityText!.Text.Contains("SYNTHETIC LOG: reopened window only"))throw new InvalidDataException("A closed live-log pump modified the old window or missed new output.");
            var activeWindow=activityWindow!;activeWindow.UpdateLayout();Capture(activeWindow,Path.Combine(output,"Live-changes-batched-QA.png"));activeWindow.Close();

            var capture=new BenchmarkCapture("synthetic-game.exe","123","0x1","FrameTime",Enumerable.Repeat(10d,Benchmark.MaximumFrames-1).Append(100d).ToArray());
            var prepared=await Task.Run(()=>
            {
                if(Dispatcher.CheckAccess())throw new InvalidDataException("Benchmark preparation ran on the UI thread.");
                return PrepareBenchmark(capture.ToTrace());
            });
            if(prepared.Trace.Stats.Frames!=Benchmark.MaximumFrames||prepared.MedianMs!=10||prepared.Over60FpsBudget!=1||prepared.Over50Ms!=1||prepared.Chart.Length>2048||prepared.Chart.Max()!=100||Math.Abs(prepared.DurationSeconds-10000.09)>.00001)throw new InvalidDataException("Large benchmark preparation changed measurements or hid its slow frame.");
            importedTrace=prepared.Trace;importedPresentation=prepared;importedSource="SYNTHETIC-ONE-MILLION-FRAMES-QA.csv";importedApplication=capture.Application;monitorTab="Benchmarks";Go("Monitor");UpdateLayout();
            if(!ReferenceEquals(CurrentBenchmarkPresentation(prepared.Trace),prepared))throw new InvalidDataException("Benchmark display recomputed its prepared capture.");
            Go("Dashboard");Go("Monitor");UpdateLayout();
            if(!ReferenceEquals(importedPresentation,prepared)||importedTrace.Milliseconds.Length!=Benchmark.MaximumFrames)throw new InvalidDataException("Returning to Benchmarks discarded cached preparation or full measurements.");
            Capture(this,Path.Combine(output,"Benchmark-large-cached-QA.png"));
            File.WriteAllText(Path.Combine(output,"monitor-activity-ui.txt"),"Passed: bounded 2,500-entry burst, one batched UI append, latest-line retention, paused reader scroll, long-entry display limit and isolated close/reopen lifecycle. A synthetic one-million-frame capture was prepared off the UI thread; exact statistics and the slow peak were retained while chart data stayed bounded to 2,048 points. Returning to Benchmarks reused its cached preparation. No system settings were changed.");
        }
        finally
        {
            activityWindow?.Close();importedTrace=previousTrace;importedPresentation=previousPresentation;importedSource=previousSource;importedApplication=previousApplication;monitorTab=previousTab;
        }
    }
}
