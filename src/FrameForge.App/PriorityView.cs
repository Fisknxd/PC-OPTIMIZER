using System.IO;
using System.Windows;
using System.Windows.Controls;
using FrameForge.Core;
namespace FrameForge.App;
public sealed partial class MainWindow
{
    void PriorityView()
    {
        body.Children.Add(Text("Choose which app gets CPU time first",25,"#F1F5FC",true));
        body.Children.Add(Text("Only helps when apps compete for CPU time. Changes last until that app closes. Above normal is the gentler choice.",13,"#BDCCE0"));
        var apps=new ComboBox {MinWidth=300,DisplayMemberPath="Name"};var levels=new ComboBox {ItemsSource=new[]{new PriorityChoice("BelowNormal","Lower than normal"),new("Normal","Normal"),new("AboveNormal","Above normal"),new("High","High · experiment")},DisplayMemberPath="Label",SelectedValuePath="Value",SelectedValue="AboveNormal",MinWidth=190};var detail=Text("Refresh to find open desktop apps.",13,"#BDCCE0");
        var apply=PrimaryButton("Review priority change",()=>Run(async ()=>
        {
            RequireScan();if(apps.SelectedItem is not PriorityAppChoice choice)throw new InvalidOperationException("Refresh and select an app first.");
            if(preferences.Mode=="Safe")throw new InvalidOperationException("Choose More choices or Experiments in Tweaks before changing app priority.");
            var selected=choice.Target;var requested=(string)levels.SelectedValue;
            if(MessageBox.Show(this,$"{selected.Name} (PID {selected.Pid})\n{selected.Priority} → {requested}\n\n"+(requested=="High"?"High priority can take CPU time from sound, input and other apps and make stutter worse. Try Above normal first.\n\n":"")+"This is a test, not a guaranteed FPS or latency improvement. Your original priority is saved below.","Review priority",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;
            Watch("App priority: "+selected.Name);var result=await priorities.Apply(selected,requested);if(result.Status!="Applied")throw new IOException(result.Error);vm.Status="App priority changed and verified.";Render();
        }));apply.IsEnabled=false;
        apps.SelectionChanged+=(_,_)=> {if(apps.SelectedItem is PriorityAppChoice choice){var p=choice.Target;detail.Text=$"{p.Name} · PID {p.Pid} · Current: {p.Priority}\n{p.Path}";apply.IsEnabled=scanned&&preferences.Mode!="Safe";}};
        body.Children.Add(Card(Panel(Text("Open desktop apps",18,"#F1F5FC",true),Text("Windows, service hosts and known anti-cheat processes are excluded. If a game refuses access, FrameForge leaves it alone.",13,"#BDCCE0"),Row(Button("Refresh apps",()=>Run(async ()=> {apps.ItemsSource=(await priorityBackend.List()).Select(x=>new PriorityAppChoice(x)).ToList();apps.SelectedIndex=0;detail.Text=apps.Items.Count==0?"No eligible desktop apps found.":detail.Text;})),apps,levels),detail,apply)));
        body.Children.Add(Text("Service priority belongs to its host process. Raising a shared host can affect several Windows features, so service hosts are excluded from this experiment.",13,"#BDCCE0"));
        if(preferences.Mode=="Safe")body.Children.Add(Button("Unlock choices with tradeoffs",()=> {SavePreferences(preferences with {Mode="Balanced"});Render();}));
        body.Children.Add(Text("Saved priority changes",19,"#F1F5FC",true));
        foreach(var change in priorities.History.AsEnumerable().Reverse())
        {
            var content=Panel(Text(change.Before.Name+" · "+change.Status,17,"#F1F5FC",true),Text($"{change.Before.Priority} → {change.After} · {change.Time.ToLocalTime():g}",13,"#BDCCE0"));
            if(change.Error!=null)content.Children.Add(Text(change.Error,13,"#FFD08A"));
            if(change.Status!="Restored")content.Children.Add(Button("Restore previous priority",()=>Run(async ()=>
            {
                Watch("Restore priority: "+change.Before.Name);var result=await priorities.Undo(change.Id);vm.Status=result.Status=="Restored"?"Original app priority restored.":"Could not restore priority: "+result.Error;Render();
            })));
            body.Children.Add(Card(content));
        }
    }
    sealed record PriorityAppChoice(PriorityTarget Target){public string Name=>Target.Name+" · PID "+Target.Pid;public override string ToString()=>Name;}
    sealed record PriorityChoice(string Value,string Label) {public override string ToString()=>Label;}
}
