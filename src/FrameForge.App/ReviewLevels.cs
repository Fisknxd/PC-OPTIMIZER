using System.Windows;
using System.Windows.Controls;
namespace FrameForge.App;
public sealed partial class MainWindow
{
    static string ReviewLevelLabel(string value)=>value switch {"Balanced"=>"More choices","Advanced"=>"Experiments",_=>"Basic"};
    sealed record ReviewLevel(string Value,string Label) {public override string ToString()=>Label;}
    ComboBox ReviewLevelPicker(double width,Action? changed=null)
    {
        var picker=new ComboBox {ItemsSource=new[]{new ReviewLevel("Safe","Basic"),new("Balanced","More choices"),new("Advanced","Experiments")},DisplayMemberPath="Label",SelectedValuePath="Value",SelectedValue=preferences.Mode,Width=width,HorizontalAlignment=HorizontalAlignment.Left,ToolTip="Basic: everyday preferences. More choices: settings with tradeoffs. Experiments: power changes that need a careful comparison."};
        bool restoring=false;picker.SelectionChanged+=(_,_)=>{if(restoring)return;try{SavePreferences(preferences with {Mode=picker.SelectedValue as string??"Safe"});changed?.Invoke();}catch{restoring=true;picker.SelectedValue=preferences.Mode;restoring=false;throw;}};return picker;
    }
}
