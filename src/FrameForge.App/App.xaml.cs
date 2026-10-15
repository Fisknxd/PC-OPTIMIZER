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
        if(!first) { MessageBox.Show("FrameForge is already running in this session."); Shutdown(); return; }
        DispatcherUnhandledException+=(s,args)=> { MessageBox.Show(args.Exception.Message,"FrameForge — action failed",MessageBoxButton.OK,MessageBoxImage.Warning); args.Handled=true; };
        try
        {
            var store=new JsonStore(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"FrameForge"));
            // Fail closed if the journal is damaged. Never replace recoverable state with empty data.
            _=store.Load<List<ChangeRecord>>("history.json",()=>[]);
            if(e.Args.Contains("--smoke-test"))
            {
                var window=new MainWindow(store); MainWindow=window; window.Show();
                window.Dispatcher.BeginInvoke(new Action(()=> { window.Close(); Shutdown(0); })); return;
            }
            MainWindow=new MainWindow(store); MainWindow.Show();
        }
        catch(Exception ex) { MessageBox.Show(ex.Message,"FrameForge — startup failed",MessageBoxButton.OK,MessageBoxImage.Error); Shutdown(1); }
    }
    protected override void OnExit(ExitEventArgs e) { instance?.Dispose(); base.OnExit(e); }
}
