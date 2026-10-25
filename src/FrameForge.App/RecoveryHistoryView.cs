using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using FrameForge.Core;
using Microsoft.Win32;
namespace FrameForge.App;
public sealed partial class MainWindow
{
    string recoveryFilter="Active changes",recoverySearch="";
    internal Func<IReadOnlyList<ChangeRecord>,bool>? SessionUndoConfirmation {get;set;}
    void RecoveryHistory()
    {
        var history=engine.History;var active=history.Count(x=>x.Status!="Restored");var attention=history.Count(x=>x.Status is not ("Applied" or "Restored"));
        body.Children.Add(Text("Your saved changes",22,"#F1F5FC",true));
        body.Children.Add(Text($"{active} changes with saved originals · {attention} need attention",13,"#BDCCE0"));
        var filter=new ComboBox {ItemsSource=new[]{"Active changes","Needs attention","All history"},SelectedItem=recoveryFilter,MinWidth=160};
        var find=new TextBox {Text=recoverySearch,Tag="Find a setting or session",MinWidth=220,MaxLength=120};var results=new StackPanel();int limit=20;
        body.Children.Add(Row(filter,find,Button("Export history",()=>Run(()=>
        {
            var dialog=new SaveFileDialog {FileName="FrameForge-history.json",Filter="History (JSON)|*.json"};if(dialog.ShowDialog(this)==true)File.WriteAllText(dialog.FileName,JsonSerializer.Serialize(new{Changes=history,Audit=store.Load<List<AuditEvent>>("audit.json",()=>[])},new JsonSerializerOptions{WriteIndented=true}));return Task.CompletedTask;
        }))));body.Children.Add(results);
        void Draw()
        {
            results.Children.Clear();
            bool Matches(ChangeRecord r)=>(recoveryFilter=="All history"||recoveryFilter=="Active changes"&&r.Status!="Restored"||recoveryFilter=="Needs attention"&&r.Status is not ("Applied" or "Restored"))&&(string.IsNullOrWhiteSpace(recoverySearch)||$"{r.Name} {r.Session} {r.Status} {r.Error}".Contains(recoverySearch,StringComparison.OrdinalIgnoreCase));
            var groups=history.GroupBy(r=>string.IsNullOrWhiteSpace(r.Session)?"record-"+r.Id:r.Session).Reverse().Where(g=>g.Any(Matches)).ToList();
            foreach(var group in groups.Take(limit))
            {
                var records=group.ToList();var pending=records.Where(x=>x.Status!="Restored").ToList();var session=records[0].Session;
                var content=Panel(Text(session.StartsWith("lowend-",StringComparison.Ordinal)?"Low-end PC profile":records.Count==1?records[0].Name:"Tuning session",18,"#F1F5FC",true),Text($"{records[0].Time.ToLocalTime():g} · {pending.Count} remaining / {records.Count} saved changes",13,"#BDCCE0"));
                if(pending.Count>0&&!string.IsNullOrWhiteSpace(session))content.Children.Add(Button("Undo session · "+pending.Count,()=>Run(async ()=>
                {
                    var fresh=engine.History.Where(x=>x.Session==session&&x.Status!="Restored").ToList();
                    if(!(SessionUndoConfirmation?.Invoke(fresh)??MessageBox.Show(this,"Restore these saved originals, newest first?\n\n"+string.Join("\n",fresh.Select(x=>"• "+x.Name))+"\n\nUndo stops if a value changed outside FrameForge. The other saved originals stay available.","Undo tuning session",MessageBoxButton.YesNo,MessageBoxImage.Question)==MessageBoxResult.Yes))return;
                    Watch("Undo tuning session");var result=await SettingsWork(()=>RecoverySessions.Undo(engine,session));vm.Status=result.Error==null?$"Restored {result.Restored} changes and verified the originals.":$"Restored {result.Restored}; {result.Remaining} remain. "+result.Error;Render();
                })));
                var details=new StackPanel();var expander=new Expander {Header="Individual changes & saved details",Content=details};bool loaded=false;
                expander.Expanded+=(_,_)=>
                {
                    if(loaded)return;loaded=true;
                    foreach(var record in records.AsEnumerable().Reverse())
                    {
                        var entry=Panel(Text(record.Name+" · "+record.Status,15,"#F1F5FC",true));
                        if(record.Error is {} error)entry.Children.Add(Text(error,13,"#FFD08A"));
                        entry.Children.Add(new Expander {Header="Saved values",Content=Text($"Before: {Value(record.Before)}\nAfter: {Value(record.After)}",12,"#BDCCE0")});
                        if(record.Status!="Restored")entry.Children.Add(Button("Undo this change",()=>Run(async ()=>
                        {
                            if(MessageBox.Show(this,"Return this setting to its saved original?","Undo "+record.Name,MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;
                            Watch("Undo "+record.Name);var result=await SettingsWork(()=>engine.Undo(record.Id));vm.Status=result.Status=="Restored"?"Previous setting restored and verified.":"Undo stopped: "+result.Error;Render();
                        })));
                        details.Children.Add(entry);
                    }
                };
                content.Children.Add(expander);results.Children.Add(Card(content));
            }
            if(groups.Count==0)results.Children.Add(Text(history.Count==0?"No changes yet. Originals are saved here when you apply a tweak.":"No matching sessions. Change the filter or clear search.",14,"#BDCCE0"));
            if(groups.Count>limit)results.Children.Add(Button("Show more sessions",()=>{limit+=20;Draw();}));
        }
        var debounce=new System.Windows.Threading.DispatcherTimer {Interval=TimeSpan.FromMilliseconds(180)};debounce.Tick+=(_,_)=>{debounce.Stop();Draw();};find.Unloaded+=(_,_)=>debounce.Stop();
        filter.SelectionChanged+=(_,_)=>{recoveryFilter=(string)filter.SelectedItem;limit=20;Draw();};find.TextChanged+=(_,_)=>{recoverySearch=find.Text;limit=20;debounce.Stop();debounce.Start();};Draw();
    }
}
