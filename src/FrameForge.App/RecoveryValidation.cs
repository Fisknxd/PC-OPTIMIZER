using System.IO;
using System.Windows;
using System.Windows.Controls;
using FrameForge.Core;
namespace FrameForge.App;

internal sealed class RecoveryTestBackend:IRestorePointBackend
{
    public bool IsAdministrator=>true;
    public SettingValue Frequency=new(false,null);
    public readonly List<LocalRestorePoint> Points=[];
    public int FrequencyWrites;
    public Task<IReadOnlyList<LocalRestorePoint>> List()=>Task.FromResult<IReadOnlyList<LocalRestorePoint>>(Points.ToList());
    public SettingValue ReadFrequency()=>Frequency;
    public void WriteFrequency(SettingValue value) {Frequency=value;FrequencyWrites++;}
    public Task Create(string description) {Points.Add(new(1,description,DateTimeOffset.UtcNow));return Task.CompletedTask;}
}
public sealed partial class MainWindow
{
    internal async Task ValidateRecovery(string output,RecoveryTestBackend backend)
    {
        Width=1360;Height=900;Go("Restore");UpdateLayout();Descendants<Expander>(body).Single(x=>x.Header as string=="Windows restore points").IsExpanded=true;UpdateLayout();
        var button=Descendants<Button>(body).Single(x=>x.Content as string=="Create restore point");
        if(!button.IsEnabled) throw new InvalidDataException("Restore creation should be available with approved administrator access.");
        button.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent)); await System.Windows.Threading.Dispatcher.Yield();
        if(backend.Points.Count!=1||backend.Frequency.Exists||backend.FrequencyWrites!=2||restorePoints.HasPending) throw new InvalidDataException("Recovery UI did not verify creation and restore original timing.");
        if(!Descendants<TextBlock>(body).Any(x=>x.Text.Contains("Created and verified in Windows"))) throw new InvalidDataException("Recovery UI did not show the verified result.");
        UpdateLayout();Capture(this,Path.Combine(output,"Recovery-simulated.png"));
        File.WriteAllText(Path.Combine(output,"recovery-ui.txt"),"Passed: Recovery create button invokes the coordinator, shows the verified local point and restores original creation timing. This UI test uses an in-memory restore backend; it does not claim real Windows point creation. See real-restore-point.txt for the separate interactive result.");
    }
}
