using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using FrameForge.Core;
using FrameForge.Windows;
using Microsoft.Win32;
namespace FrameForge.App;

public sealed partial class MainWindow
{
    void Restore()
    {
        body.Children.Add(Text("A way back",25,"#F1F5FC",true));
        body.Children.Add(Text("Undo a tweak here, or save a Windows restore point before bigger changes.",13,"#BDCCE0"));
        var points=new StackPanel(); var pointStatus=Text("Saved in Windows' own restore-point list on this PC.",13,"#BDCCE0");
        async Task RefreshPoints()
        {
            points.Children.Clear();
            try
            {
                var list=await restorePoints.List();
                pointStatus.Text=list.Count==0?"No restore points yet. Turn on System Protection for your Windows drive if creation is unavailable.":$"{list.Count} local Windows restore points found.";
                foreach(var point in list.Take(5)) points.Children.Add(Text($"{point.CreatedAt.ToLocalTime():g}  ·  {point.Description}  ·  #{point.Id}",13,"#BDCCE0"));
            }
            catch(Exception) { pointStatus.Text="Windows could not read restore points. Open System Protection to check access and protection for your Windows drive."; }
        }
        var create=PrimaryButton("Create restore point",()=>Run(async ()=>
        {
            vm.Status="Creating a Windows restore point. This can take a few minutes…";
            Watch("Create local Windows restore point");
            var point=await restorePoints.Create(); await RefreshPoints();
            pointStatus.Text=$"Created and verified in Windows: {point.Description} (#{point.Id})."; vm.Status="Windows restore point created and verified on this PC.";
        }));
        create.IsEnabled=restorePoints.CanCreate;
        var restoreCard=Panel(Text("Windows restore point",20,"#F1F5FC",true),Text("Saves system settings for recovery. It is not a backup of your personal files.",13,"#BDCCE0"),
            Row(create,Button("Refresh list",()=>Run(RefreshPoints)),Button("System Protection",()=>WindowsSettings.Utility("System Protection")),Button("Open System Restore",()=>WindowsSettings.Utility("System Restore"))),pointStatus,points);
        if(!restorePoints.CanCreate) restoreCard.Children.Add(Text("Administrator access is needed to create a restore point. Use Restart as admin in Settings.",13,"#FFD08A"));
        if(restorePoints.HasPending) restoreCard.Children.Add(Button("Finish interrupted restore-point setup",()=>Run(()=> {restorePoints.RecoverFrequency(); vm.Status="Original Windows restore-point timing recovered."; Render(); return Task.CompletedTask;})));
        body.Children.Add(new Expander {Header="Windows restore points",Content=Card(restoreCard),Margin=new(0,8,0,12)});
        var graphics=new StackPanel();GraphicsHistory(target:graphics);if(graphics.Children.Count>0)body.Children.Add(new Expander {Header="Game graphics backups",Content=graphics,Margin=new(0,0,0,12)});
        RecoveryHistory();
        var audit=store.Load<List<AuditEvent>>("audit.json",()=>[]);
        if(audit.Count>0) body.Children.Add(new Expander {Header="Cleanup, app removal and backup history",Content=Text(string.Join("\n",audit.TakeLast(20).Reverse().Select(x=>$"{x.Time.ToLocalTime():g} · {x.Kind} · {x.Subject} · {x.Result}")),12,"#BDCCE0")});
    }
}
