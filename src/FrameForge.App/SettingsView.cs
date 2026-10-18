using System.Windows;
using System.Windows.Controls;
namespace FrameForge.App;
public sealed partial class MainWindow
{
    void Settings()
    {
        body.Children.Add(Text("Make it yours",25,"#F1F5FC",true));
        var mode=ReviewLevelPicker(200,()=>vm.Status="Options saved.");
        body.Children.Add(Card(Panel(Text("Available options",19,"#F1F5FC",true),Text("Basic offers everyday preferences. More choices unlocks settings with tradeoffs. Experiments adds power tests. Each change still explains what it does.",13,"#BDCCE0"),mode)));
        var access=Panel(Text(system.IsAdministrator?"Administrator access is on":"Standard access",19,"#F1F5FC",true),Text("Windows asks for administrator permission at launch. If you cancel, the app still opens; restore-point creation stays locked.",13,"#BDCCE0"));
        if(AdminLaunch.LastFailure is {} failure) access.Children.Add(Text(failure,13,"#FFD08A"));
        if(!system.IsAdministrator) access.Children.Add(Button("Restart as admin",()=>
        {
            if(AdminLaunch.TryStart([])) Application.Current.Shutdown();
        }));
        body.Children.Add(Card(access));
        body.Children.Add(Card(Panel(Text("Private, local data",19,"#F1F5FC",true),Text("No account or analytics. Your settings and change history stay on this PC.",13,"#BDCCE0"),new Expander {Header="Where the data is saved",Content=Text(store.Root,12,"#BDCCE0")},Text("Keep this folder if you rely on Undo. Cleanup and app removal do not have an Undo button.",12,"#BDCCE0"))));
    }
}
