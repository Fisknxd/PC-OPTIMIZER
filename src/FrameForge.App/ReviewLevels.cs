using System.Windows;
using System.Windows.Controls;
namespace FrameForge.App;
public sealed partial class MainWindow
{
    sealed record ReviewLevel(string Value,string Label);
    ComboBox ReviewLevelPicker(double width,Action? changed=null)
    {
        var picker=new ComboBox {ItemsSource=new[]{new ReviewLevel("Safe","Basic"),new("Balanced","More choices"),new("Advanced","Experiments")},DisplayMemberPath="Label",SelectedValuePath="Value",SelectedValue=preferences.Mode,Width=width,HorizontalAlignment=HorizontalAlignment.Left,ToolTip="Basic: everyday preferences. More choices: settings with tradeoffs. Experiments: power changes that need a careful comparison."};
        picker.SelectionChanged+=(_,_)=>{preferences=preferences with {Mode=picker.SelectedValue as string??"Safe"};store.Save("preferences.json",preferences);changed?.Invoke();};return picker;
    }
}
