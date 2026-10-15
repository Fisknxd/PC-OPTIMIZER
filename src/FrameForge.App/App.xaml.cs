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
        using var identity=WindowsIdentity.GetCurrent();
        instance=new Mutex(true,@"Global\FrameForge-"+identity.User?.Value,out var first);
        if(!first) { MessageBox.Show(e.Args.Contains("--update-desktop")?"Close the running FrameForge application, then run Update-FrameForge.cmd again. Your existing files have not been changed.":"FrameForge is already running in this session."); Shutdown(e.Args.Contains("--update-desktop")?1:0); return; }
        DispatcherUnhandledException+=(s,args)=> { MessageBox.Show(args.Exception.Message,"FrameForge — action failed",MessageBoxButton.OK,MessageBoxImage.Warning); args.Handled=true; };
        try
        {
            if(e.Args.Contains("--update-desktop")) { DesktopUpdateFlow.Run(); Shutdown(); return; }
            var testMode=e.Args.Contains("--smoke-test")||e.Args.Contains("--ui-test");
            var store=new JsonStore(testMode?Path.Combine(Path.GetTempPath(),"FrameForge-ui-"+Guid.NewGuid().ToString("N")):Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"FrameForge"));
            // Fail closed if the journal is damaged. Never replace recoverable state with empty data.
            _=store.Load<List<ChangeRecord>>("history.json",()=>[]);
            if(testMode)
            {
                var window=new MainWindow(store,autoScan:false); MainWindow=window; window.Show();
                window.Dispatcher.BeginInvoke(new Action(()=>
                {
                    try
                    {
                        if(e.Args.Contains("--ui-test")) window.ValidateAppearance(Path.Combine(Environment.CurrentDirectory,"artifacts","ui-checks"));
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
}
