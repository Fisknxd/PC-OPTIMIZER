using System.Windows.Controls;
using FrameForge.Core;
using FrameForge.Windows;
namespace FrameForge.App;
public sealed partial class MainWindow
{
    void Tools()
    {
        body.Children.Add(Text("Check your connection",25,"#F1F5FC",true));
        body.Children.Add(Text("Look for missed replies or uneven response times. This checks only the address you enter.",13,"#BDCCE0"));
        var host=new TextBox {Tag="Router address, IP address or server name",MinWidth=300};
        var result=Text("Enter an address and run the test. No network settings are changed.",13,"#BDCCE0");
        body.Children.Add(Card(Panel(Text("Response-time test",19,"#F1F5FC",true),Row(host,PrimaryButton("Test connection",()=>Run(async ()=>
        {
            var destination=host.Text.Trim(); result.Text=await Diagnostics.Ping(destination); store.Audit("Connection test",destination,result.Text); vm.Status="Connection test complete.";
        }))),result)));
        foreach(var guide in SetupGuides.All.Where(x=>x.Group=="Network"))
            body.Children.Add(Card(Panel(Text(guide.Title,18,"#F1F5FC",true),Text(guide.Summary,13,"#BDCCE0"),new Expander {Header="What to do",Content=Text(guide.Steps,13,"#BDCCE0")})));
        body.Children.Add(Button("Open Windows network settings",()=>Run(()=>Handle(Catalog.All.Single(x=>x.Id=="network")))));
        body.Children.Add(new Expander {Header="Connection details",Content=Text(Join(vm.Hardware.Network),12,"#BDCCE0")});
        var utilities=Row(); foreach(var name in new[]{"Task Manager","Resource Monitor","System Information","DirectX Diagnostic","Reliability Monitor"}) utilities.Children.Add(Button(name,()=>WindowsSettings.Utility(name)));
        body.Children.Add(new Expander {Header="Windows troubleshooting tools",Content=utilities});
    }
}
