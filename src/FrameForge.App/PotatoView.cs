using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using FrameForge.Core;
using Microsoft.Win32;
namespace FrameForge.App;
public sealed partial class MainWindow
{
    string potatoGame="Fortnite";
    string potatoMode="Potato";
    readonly Dictionary<string,string> selectedGameConfigs=[];
    Task? potatoRead;
    void PotatoView()
    {
        body.Children.Add(Text("One click. A much lighter game.",25,"#F1F5FC",true));
        var tabs=Row();foreach(var name in new[]{"Fortnite","Rust"}){var tab=Button(name,()=> {potatoGame=name;Render();});tab.Background=Brush(potatoGame==name?"#27534F":"#23334C");tabs.Children.Add(tab);}body.Children.Add(tabs);
        var game=potatoGame;var mode=potatoMode;
        var selector=new ComboBox {ItemsSource=new[]{"Potato","Sharper potato"},SelectedItem=mode,MinWidth=180};selector.SelectionChanged+=(_,_)=> {potatoMode=(string)selector.SelectedItem;Render();};
        var state=Text("Finding saved game settings…",14,"#5EE0BA",true);var location=Text("",12,"#BDCCE0");var changes=new StackPanel();GameGraphicsPreview? preview=null;
        var apply=PrimaryButton("Apply Potato Settings",()=>Run(async ()=>
        {
            if(preview==null)throw new InvalidOperationException("Wait for a valid graphics preview first.");
            Watch(game+" · "+mode);var result=await graphicsProfiles.Apply(preview);
            if(result?.Status=="Failed")throw new IOException(result.Error+" The original configuration is saved in Recovery.");
            vm.Status=result==null?"This profile is already saved.":game+" graphics saved and verified. Start the game to load them.";Render();
        }));apply.IsEnabled=false;
        body.Children.Add(Card(Panel(Text(game+" · "+mode,22,"#F1F5FC",true),
            Text(game=="Fortnite"?"Performance mode, minimum saved terrain and object detail, low textures and no costly lighting effects.":"Minimum saved terrain, object, tree and effect detail, low shadows and a fixed low render scale.",14,"#BDCCE0"),
            Text(mode=="Potato"?"50% width × 50% height renders one quarter as many pixels. The image will be very rough; HUD resolution stays unchanged.":"75% width × 75% height keeps more clarity, with minimum detail settings.",13,"#BDCCE0"),
            Row(selector,Button("Refresh",Render),Button("Locate config",()=>
            {
                var dialog=new OpenFileDialog {Title="Select "+game+" configuration",Filter=game=="Rust"?"Rust client.cfg|client.cfg":"Fortnite GameUserSettings.ini|GameUserSettings.ini"};
                if(dialog.ShowDialog(this)==true){selectedGameConfigs[game]=dialog.FileName;Render();}
            })),state,apply,Text("Close the game first. This button backs up the file, changes the listed settings and verifies the saved result. Restore is below and in Recovery.",13,"#BDCCE0"))));
        body.Children.Add(new Expander {Header="Changes included · preview before applying",Content=changes});
        body.Children.Add(new Expander {Header="Configuration location",Content=location});
        body.Children.Add(new Expander {Header="About flat terrain and game rules",Content=Text("This edits normal saved graphics values. It never installs texture packs, replaces shaders, changes anti-cheat or sets values outside this profile's limits. Old Rust shader level 100 is used only if this installation already saves that legacy control; removed commands are not added. Current games can clamp or ignore saved options. File verification is not a measured FPS gain or a guarantee against bans. The exact look of a video cannot be promised.",13,"#BDCCE0")});
        async Task Read()
        {
            try
            {
                selectedGameConfigs.TryGetValue(game,out var chosen);preview=await graphicsProfiles.Preview(game,mode,chosen);
                state.Text=preview.Patch.Changes.Count==0?"Already matches this profile":$"Ready · {preview.Patch.Changes.Count} settings will change";apply.IsEnabled=preview.Patch.Changes.Count>0;
                location.Text=preview.Target.Path;
                foreach(var change in preview.Patch.Changes)changes.Children.Add(Text($"{change.Description}\n{change.Key}: {change.Before} → {change.After}",13,"#BDCCE0"));
                if(preview.Patch.Skipped.Count>0)changes.Children.Add(Text("Not exposed by this configuration; left alone: "+string.Join(", ",preview.Patch.Skipped),12,"#BDCCE0"));
                if(game=="Rust"&&preview.Patch.Skipped.Contains("graphics.shaderlod"))state.Text+=" · current Rust controls; legacy flat shader unavailable";
            }
            catch(Exception e){state.Text=e.Message;state.Foreground=Brush("#FFD08A");apply.IsEnabled=false;}
        }
        potatoRead=Read();GraphicsHistory(game);
        var official=PotatoProfiles.All.Single(x=>x.Game==game);body.Children.Add(Row(Button("Open launcher ↗",()=>Run(()=> {Process.Start(new ProcessStartInfo(official.Launcher){UseShellExecute=true});return Task.CompletedTask;})),Button("Official guidance ↗",()=>Run(()=> {Process.Start(new ProcessStartInfo(official.Source){UseShellExecute=true});return Task.CompletedTask;}))));
    }
    void GraphicsHistory(string? game=null,StackPanel? target=null)
    {
        var output=target??body;var history=graphicsProfiles.History.Where(x=>game==null||x.Target.Game==game).Reverse().ToList();if(history.Count==0)return;
        output.Children.Add(Text("Saved graphics originals",19,"#F1F5FC",true));
        foreach(var record in history)
        {
            var content=Panel(Text(record.Target.Game+" · "+record.Mode+" · "+record.Status,17,"#F1F5FC",true),Text(record.Time.ToLocalTime().ToString("g"),12,"#BDCCE0"));
            if(record.Error!=null)content.Children.Add(Text(record.Error,13,"#FFD08A"));
            if(record.Status!="Restored")content.Children.Add(Button("Restore original graphics",()=>Run(async ()=>
            {
                Watch("Restore "+record.Target.Game+" graphics");var result=await graphicsProfiles.Undo(record.Id);if(result.Status!="Restored")throw new IOException(result.Error);vm.Status="Previous graphics restored and verified.";Render();
            })));
            content.Children.Add(Button("Save original file copy",()=>Run(()=>
            {
                var save=new SaveFileDialog {FileName=record.Target.Game+"-original-"+Path.GetFileName(record.Target.Path),Filter="Backup file|*.*"};
                if(save.ShowDialog(this)==true) {var path=Path.GetFullPath(save.FileName);if(path.Equals(Path.GetFullPath(record.Target.Path),StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Save the backup to a different location; use Restore to change the active file.");File.WriteAllBytes(path,Convert.FromBase64String(record.BeforeBase64));}return Task.CompletedTask;
            })));
            output.Children.Add(Card(content));
        }
    }
}
