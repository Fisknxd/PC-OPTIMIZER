using System.IO;
using System.Threading;
using System.Security.Principal;
using System.Windows;
using FrameForge.Core;

namespace FrameForge.App;
public partial class App : Application
{
    Mutex? instance;
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var automatedTest=e.Args.Any(x=>x is "--failure-exit-test" or "--smoke-test" or "--ui-test" or "--feature-test" or "--integration-test" or "--recovery-test");
        if(automatedTest)ShutdownMode=ShutdownMode.OnExplicitShutdown;
        try { if(!automatedTest&&AdminLaunch.TryStart(e.Args)) { Shutdown(); return; } }
        catch(Exception error) { MessageBox.Show(error.Message,"FrameForge — account check"); Shutdown(1); return; }
        var parent=e.Args.FirstOrDefault(x=>x.StartsWith("--wait-for-process=",StringComparison.Ordinal));
        if(parent!=null&&int.TryParse(parent[19..],out var parentId)&&parentId!=Environment.ProcessId)
        {
            try { using var process=System.Diagnostics.Process.GetProcessById(parentId); process.WaitForExit(10000); } catch(ArgumentException) { }
        }
        using var identity=WindowsIdentity.GetCurrent();
        instance=new Mutex(true,@"Global\FrameForge-"+identity.User?.Value+(automatedTest?"-test-"+Guid.NewGuid().ToString("N"):""),out var first);
        if(!first) { MessageBox.Show(e.Args.Contains("--update-desktop")?"Close the running FrameForge application, then run Update-FrameForge.cmd again. Your existing files have not been changed.":"FrameForge is already running in this session."); Shutdown(e.Args.Contains("--update-desktop")?1:0); return; }
        DispatcherUnhandledException+=(s,args)=>
        {
            args.Handled=true;
            if(automatedTest){Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory,"artifacts"));File.WriteAllText(Path.Combine(Environment.CurrentDirectory,"artifacts","ui-test-error.txt"),args.Exception.ToString());Shutdown(1);return;}
            MessageBox.Show(args.Exception.Message,"FrameForge — action failed",MessageBoxButton.OK,MessageBoxImage.Warning);
        };
        try
        {
            if(e.Args.Contains("--update-desktop")) { DesktopUpdateFlow.Run(); Shutdown(); return; }
            var testMode=automatedTest;
            var store=new JsonStore(testMode?Path.Combine(Path.GetTempPath(),"FrameForge-ui-"+Guid.NewGuid().ToString("N")):Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"FrameForge"));
            // Fail closed if the journal is damaged. Never replace recoverable state with empty data.
            _=store.Load<List<ChangeRecord>>("history.json",()=>[]);
            if(e.Args.Contains("--restore-test"))
            {
                _=VerifyLocalRestorePoint(store); return;
            }
            if(testMode)
            {
                var recovery=e.Args.Contains("--recovery-test")?new RecoveryTestBackend():null;
                var fake=e.Args.Contains("--feature-test")?new FeatureTestSettings {Administrator=recovery!=null}:null;
                var window=new MainWindow(store,autoScan:false,settings:fake,restoreBackend:recovery,gameFiles:fake!=null?new GraphicsTestFiles():null){AutomatedValidation=true}; MainWindow=window; window.Show();
                window.Dispatcher.BeginInvoke(new Action(async ()=>
                {
                    try
                    {
                        if(e.Args.Contains("--failure-exit-test"))throw new InvalidDataException("Synthetic automation failure for exit-code validation.");
                        if(e.Args.Contains("--ui-test")) window.ValidateAppearance(Path.Combine(Environment.CurrentDirectory,"artifacts","ui-checks"));
                        if(e.Args.Contains("--integration-test")) await window.ValidateWindowsReadings(Path.Combine(Environment.CurrentDirectory,"artifacts","ui-checks"));
                        if(fake!=null) await window.ValidateFeatureToggles(Path.Combine(Environment.CurrentDirectory,"artifacts","ui-checks"),fake);
                        if(fake!=null) await window.ValidateExpandedControls(Path.Combine(Environment.CurrentDirectory,"artifacts","ui-checks"),fake);
                        if(fake!=null) await window.ValidatePerformanceControls(Path.Combine(Environment.CurrentDirectory,"artifacts","ui-checks"),fake);
                        if(fake!=null) await window.ValidateQualityControls(Path.Combine(Environment.CurrentDirectory,"artifacts","ui-checks"),fake);
                        if(fake!=null) await window.ValidateResponsiveControls(Path.Combine(Environment.CurrentDirectory,"artifacts","ui-checks"),fake);
                        if(fake!=null) await window.ValidateMonitorAndActivity(Path.Combine(Environment.CurrentDirectory,"artifacts","ui-checks"));
                        if(recovery!=null) await window.ValidateRecovery(Path.Combine(Environment.CurrentDirectory,"artifacts","ui-checks"),recovery);
                        window.Close(); Shutdown(0);
                    }
                    catch(Exception ex)
                    {
                        Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory,"artifacts"));
                        File.WriteAllText(Path.Combine(Environment.CurrentDirectory,"artifacts","ui-test-error.txt"),ex.ToString());
                        window.Close(); Shutdown(1);
                    }
                })); return;
            }
            MainWindow=new MainWindow(store); MainWindow.Show();
        }
        catch(Exception ex) { MessageBox.Show(ex.Message,e.Args.Contains("--update-desktop")?"FrameForge — update not completed":"FrameForge — startup failed",MessageBoxButton.OK,MessageBoxImage.Error); Shutdown(1); }
    }
    protected override void OnExit(ExitEventArgs e) { instance?.Dispose(); base.OnExit(e); }
    async Task VerifyLocalRestorePoint(JsonStore store)
    {
        var output=Path.Combine(Environment.CurrentDirectory,"artifacts","ui-checks"); Directory.CreateDirectory(output);
        try
        {
            var manager=new RestorePointManager(new FrameForge.Windows.WindowsRestorePoints(),store);
            var point=await manager.Create();
            File.WriteAllText(Path.Combine(output,"real-restore-point.txt"),$"VERIFIED: Windows local restore point #{point.Id}, {point.Description}, {point.CreatedAt:O}. Native creation completed; the new point was found in Windows SystemRestore. Original creation frequency restored."); Shutdown(0);
        }
        catch(Exception error) { File.WriteAllText(Path.Combine(output,"real-restore-point.txt"),"NOT VERIFIED: "+AdminLaunch.LastFailure+"\n"+error); Shutdown(1); }
    }
}
