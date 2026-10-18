using System.Windows;
using System.Windows.Controls;
using FrameForge.Core;
namespace FrameForge.App;
public sealed partial class MainWindow
{
    readonly List<Task> powerReads=[];
    internal Func<ActionPreview,bool>? PowerConfirmation {get;set;}
    Border PowerSettingCard(Optimization op)
    {
        var state=Text("Reading current plan…",13,"#BDCCE0");var reason=Text("",12,"#FFD08A");
        var apply=Button("Review change",()=>Run(async ()=>
        {
            RequireScan();if(ModeReason(op) is {} blocked)throw new InvalidOperationException(blocked);
            var preview=await engine.Preview(op,vm.Hardware);
            if(preview.Current==preview.Proposed){vm.Status="Already configured. No change needed.";return;}
            var accepted=PowerConfirmation?.Invoke(preview)??MessageBox.Show(this,op.Description+"\n\n"+PowerSettings.Describe(op,preview.Current)+"\n→ "+PowerSettings.Describe(op,preview.Proposed)+"\n\nOnly the plugged-in value changes. Battery settings and all other plan values are kept. Undo requires the same plan to be active.","Review power setting",MessageBoxButton.YesNo,MessageBoxImage.Question)==MessageBoxResult.Yes;
            if(!accepted)return;Watch(op.Name);var result=await engine.Apply(preview,vm.Hardware,Guid.NewGuid().ToString("N"));
            vm.Status=result?.Status=="Applied"?"Power setting saved and verified. Compare the game and undo in Recovery if it feels worse.":result?.Error??"Already configured.";Render();
        }));apply.IsEnabled=false;
        async Task Read()
        {
            try
            {
                var current=await system.Read(op);state.Text=PowerSettings.Describe(op,current);
                var blocked=!scanned?"Scan PC first.":op.Incompatibility(vm.Hardware)??ModeReason(op);
                var configured=PowerSettings.Target(op,current)==current;
                reason.Text=configured?"Already at the requested value. No change needed.":blocked??"Plugged-in value only · current plan · original saved";
                apply.IsEnabled=blocked==null&&!configured;
                if(configured)apply.Content="Already configured";
            }
            catch(Exception error){state.Text="Unavailable";reason.Text=error.Message;}
        }
        powerReads.Add(Read());
        var content=Panel(Text(op.Name,18,"#F1F5FC",true),Text(op.Description,13,"#BDCCE0"),state,reason,apply);
        var saved=engine.History.LastOrDefault(x=>x.OperationId==op.Id&&x.Status!="Restored");
        if(saved!=null)content.Children.Add(Button("Undo this power change",()=>Run(async ()=>
        {
            Watch("Restore "+op.Name);var result=await engine.Undo(saved.Id);vm.Status=result.Status=="Restored"?"Original power setting restored and verified.":"Restore stopped: "+result.Error;Render();
        })));
        return Card(content);
    }
}
