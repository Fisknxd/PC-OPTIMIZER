using System.Windows;
using System.Windows.Controls;
using FrameForge.Core;
namespace FrameForge.App;
public sealed partial class MainWindow
{
    BenchmarkCapture? ChooseBenchmarkCapture(IReadOnlyList<BenchmarkCapture> captures)
    {
        if(captures.Count==1)return captures[0];
        var dialog=new Window {Owner=this,Title="Choose the game recording",Width=690,Height=440,MinWidth=570,MinHeight=380,WindowStartupLocation=WindowStartupLocation.CenterOwner};
        var choices=new ComboBox {ItemsSource=captures,DisplayMemberPath="Label",SelectedIndex=0,HorizontalAlignment=HorizontalAlignment.Stretch};
        var details=Text("",14,"#BDCCE0");var import=PrimaryButton("Import selected recording",()=>dialog.DialogResult=true);
        void Draw(){if(choices.SelectedItem is BenchmarkCapture capture){details.Text=capture.Explanation+"\n\n"+(capture.CanImport?$"{capture.Durations.Length:N0} usable frames.":"This stream is too short. At least 100 valid frames are needed.")+(capture.InitialRowsSkipped>0?" One initial zero interval was excluded.":"");import.IsEnabled=capture.CanImport;}}
        choices.SelectionChanged+=(_,_)=>Draw();Draw();var cancel=Button("Cancel",()=>dialog.DialogResult=false);cancel.IsCancel=true;
        dialog.Content=new Border {Padding=new(24),Child=Panel(Text("Choose your game",24,"#F1F5FC",true),Text("This file has more than one recording stream. They must stay separate to keep the FPS calculation meaningful.",14,"#BDCCE0"),choices,details,Row(import,cancel))};
        return dialog.ShowDialog()==true?choices.SelectedItem as BenchmarkCapture:null;
    }
}
