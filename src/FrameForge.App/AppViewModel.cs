using System.ComponentModel;
using System.Runtime.CompilerServices;
using FrameForge.Core;
namespace FrameForge.App;
public sealed class AppViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    string status="Scan your PC to begin. No settings change during scanning.";
    bool busy;
    public HardwareSnapshot Hardware { get; set; }=new();
    public string Status { get=>status; set { status=value; Changed(); } }
    public bool Busy { get=>busy; set { busy=value; Changed(); } }
    void Changed([CallerMemberName]string? property=null)=>PropertyChanged?.Invoke(this,new(property));
}
