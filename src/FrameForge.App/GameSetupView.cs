using System.Windows.Controls;
using FrameForge.Core;
using FrameForge.Windows;
namespace FrameForge.App;
public sealed partial class MainWindow
{
    void Gaming()
    {
        body.Children.Add(Text("A faster-feeling setup",25,"#F1F5FC",true));
        body.Children.Add(Text("Short guides for your game, screen and mouse. These steps are done in the game or Windows.",13,"#BDCCE0"));
        body.Children.Add(Card(Panel(Text("Potato Settings",20,"#5EE0BA",true),Text("Apply saved low-detail profiles for Fortnite or Rust in one click, with backups and restore.",13,"#BDCCE0"),Button("Open Potato Settings",()=>Go("Potato Settings")))));
        body.Children.Add(Row(Button("Browser memory tweaks",()=>{featureSection="Browser memory";Go("Optimize");}),Button("Power checks",()=>{featureSection="Power checks";Go("Optimize");}),Button("Background downloads",()=>{featureSection="Downloads";Go("Optimize");})));
        var groups=new ComboBox {ItemsSource=new[]{"All guides","Windows","Fortnite","AMD","Mouse","Less clutter","Storage","Network"},SelectedIndex=0,MinWidth=170};
        var games=new ComboBox {ItemsSource=Games.Names,SelectedIndex=0,MinWidth=170}; var target=new StackPanel();
        void Draw()
        {
            target.Children.Clear(); var selected=groups.SelectedItem as string??"All guides";
            var guides=SetupGuides.All.Where(g=>(selected=="All guides"||g.Group==selected)&&(games.SelectedItem as string=="Fortnite"||g.Group!="Fortnite")&&(!scanned||g.Vendor==Vendor.Unknown||vm.Hardware.Has(g.Vendor))).ToList();
            foreach(var guide in guides)
            {
                var content=Panel(Text(guide.Title,18,"#F1F5FC",true),Text(guide.Summary,13,"#BDCCE0"));
                content.Children.Add(new Expander {Header="How to change it · "+guide.Group,Content=Text(guide.Steps,13,"#BDCCE0")});
                if(guide.Control is {} id) content.Children.Add(Button("Open "+(guide.Vendor==Vendor.Unknown?"Windows settings":"official support"),()=>Run(()=>Handle(Catalog.All.Single(x=>x.Id==id)))));
                if(guide.Title=="Let Windows maintain your SSD") content.Children.Add(Button("Open Optimize Drives",()=>WindowsSettings.Utility("Optimize Drives")));
                target.Children.Add(Card(content));
            }
            if(games.SelectedItem is string game&&game!="Fortnite") target.Children.Insert(0,Card(Panel(Text(game+" notes",18,"#F1F5FC",true),new Expander {Header="Game-specific tips",Content=Text(Games.Guidance(game,vm.Hardware),13,"#BDCCE0")})));
        }
        groups.SelectionChanged+=(_,_)=>Draw(); games.SelectionChanged+=(_,_)=>Draw();
        body.Children.Add(Row(Text("Show",13,"#BDCCE0"),groups,Text("Game",13,"#BDCCE0"),games,Button("Mouse and desktop tweaks",()=> {featureSection="Mouse & desktop"; Go("Optimize");}))); body.Children.Add(target); Draw();
    }
}
