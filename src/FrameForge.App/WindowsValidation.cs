using System.IO;
using System.Windows;
using FrameForge.Windows;

namespace FrameForge.App;

public sealed partial class MainWindow
{
    internal async Task ValidateWindowsReadings(string output)
    {
        Directory.CreateDirectory(output);
        if(Icon==null) throw new InvalidDataException("The application icon is missing.");
        var readings=new List<string>(); var settings=new WindowsSettings();
        foreach(var operation in FrameForge.Core.Catalog.All.Where(FrameForge.Core.FeatureToggles.Supports))
        {
            try {await settings.CheckAvailability(operation);var value=await settings.Read(operation); var enabled=FrameForge.Core.FeatureToggles.Enabled(operation,value);readings.Add(operation.Name+": "+(enabled==true?"On":enabled==false?"Off":"Default/custom"));}
            catch(Exception error) {if(operation.Kind==FrameForge.Core.ActionKind.Desktop)throw new InvalidDataException("Native desktop setting could not be read: "+operation.Id,error);readings.Add(operation.Name+": unavailable: "+error.Message);}
        }
        File.WriteAllLines(Path.Combine(output,"tweak-readings.txt"),readings.Append("All native desktop settings read from Windows without writing changes. Window icon loaded."));
        foreach(var operation in FrameForge.Core.Catalog.All.Where(x=>x.Kind==FrameForge.Core.ActionKind.PowerSetting))
        {
            try{var value=await settings.Read(operation);readings.Add(operation.Name+": "+FrameForge.Core.PowerSettings.Describe(operation,value)+" · "+value.Value);}
            catch(Exception error){readings.Add(operation.Name+": unavailable: "+error.Message);}
        }
        File.WriteAllLines(Path.Combine(output,"performance-readings.txt"),readings.Append("New policy availability and native AC/DC power readings checked. No policy or power value was changed."));
        var serviceReport=new List<string>();int installed=0;var scripts=new List<string>();
        foreach(var operation in FrameForge.Core.Catalog.All.Where(x=>x.Kind==FrameForge.Core.ActionKind.Service))
        {
            try {var state=FrameForge.Core.OptionalServices.Unpack(await settings.Read(operation));serviceReport.Add(operation.Target+": "+state.StartMode+" · "+(state.Running?"Running":"Stopped"));installed++;}
            catch(IOException error) when(error.Message.Contains("not installed",StringComparison.Ordinal)){serviceReport.Add(operation.Target+": not installed.");}
            foreach(var state in new[]{new FrameForge.Core.ServiceState("Disabled",false),new("Automatic",true),new("AutomaticDelayed",false),new("Manual",false)})
            {
                var script=ServiceControls.BuildWriteScript(operation,FrameForge.Core.OptionalServices.Pack(state));var encoded=Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(script));
                scripts.Add("$tokens=$null;$errors=$null;[void][System.Management.Automation.Language.Parser]::ParseInput([Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('"+encoded+"')),[ref]$tokens,[ref]$errors);if($errors.Count -gt 0){throw ($errors | Out-String)}");
            }
        }
        if(installed==0)throw new InvalidDataException("No service state was read from Windows.");
        foreach(var batch in scripts.Chunk(4)) await Commands.PowerShell(string.Join(";",batch)+";Write-Output 'Service scripts parsed. No service writes executed.'");
        var appList=await priorityBackend.List();serviceReport.Add("Eligible desktop app priorities read: "+appList.Count);
        ShowActivity();activity.Report("READ-ONLY WINDOWS CHECK · No settings will be changed.");
        await Commands.PowerShell("Write-Output 'Reading printing service status';Get-Service -Name 'Spooler' | Select-Object Name,Status | Format-Table | Out-String",activity:activity);
        await System.Windows.Threading.Dispatcher.Yield();activityWindow!.UpdateLayout();Capture(activityWindow,Path.Combine(output,"Live-PowerShell-readonly.png"));activityWindow.Close();
        serviceReport.Add($"All {scripts.Count} service write/restore scripts passed the Windows PowerShell parser. They were not executed; service mutation requires an administrator acceptance test.");
        File.WriteAllLines(Path.Combine(output,"service-and-priority-readings.txt"),serviceReport);
        var sampler=new ResourceSampler(); _=sampler.Read(); await Task.Delay(1100); var sample=sampler.Read();
        if(sample.CpuPercent is not >=0 or not <=100 || sample.TotalMemory<=0 || sample.AvailableMemory<0 || sample.AvailableMemory>sample.TotalMemory)
            throw new InvalidDataException("Native CPU/memory counters returned invalid measurements.");
        using(var cancelled=new CancellationTokenSource())
        {
            cancelled.Cancel(); bool stopped=false;
            try { await Scanner.Scan(cancelled.Token); } catch(OperationCanceledException) { stopped=true; }
            if(!stopped) throw new InvalidDataException("Cancelled scans must not complete.");
        }
        vm.Hardware=await Scanner.Scan(); scanned=true;
        Width=1360; Height=900; vm.Status="Read-only Windows integration validation.";
        preferences=preferences with {Mode="Balanced"};Go("Services");await Task.WhenAll(serviceReads);UpdateLayout();Capture(this,Path.Combine(output,"Services-real-state.png"));
        foreach(var game in FrameForge.Core.PotatoProfiles.All.Select(x=>x.Game)){potatoGame=game;Go("Potato Settings");if(potatoRead!=null)await potatoRead;UpdateLayout();Capture(this,Path.Combine(output,"Potato-"+game+".png"));}
        Go("Low-end PC");if(lowEndRead!=null)await lowEndRead;UpdateLayout();Capture(this,Path.Combine(output,"Low-end-PC-real-state.png"));
        preferences=preferences with {Mode="Advanced"};featureSection="Power checks";Go("Optimize");await Task.WhenAll(powerReads);UpdateLayout();Capture(this,Path.Combine(output,"Power-checks-real-state.png"));preferences=preferences with {Mode="Balanced"};
        featureSection="Browser memory";Render();UpdateLayout();Capture(this,Path.Combine(output,"Browser-memory-real-state.png"));
        await ValidateNativeGraphicsFiles(output);
        await ValidateDisposableRegistryGroup(output);
        Go("Dashboard"); UpdateLayout(); Capture(this,Path.Combine(output,"Dashboard-scanned.png"));
        featureSection="Gaming";Go("Optimize");UpdateLayout();Capture(this,Path.Combine(output,"Tweaks-real-state.png"));
        featureSection="Mouse & desktop";Render();UpdateLayout();Capture(this,Path.Combine(output,"Mouse-tweaks-real-state.png"));
        monitorTab="Live resources"; Go("Monitor");
        var start=Descendants<System.Windows.Controls.Button>(this).Single(b=>b.Content as string=="▶  Start monitoring");
        start.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
        await Task.Delay(2250);
        if(liveSamples.Count<1||liveTimer==null) throw new InvalidDataException("Live monitoring did not collect native samples.");
        UpdateLayout(); Capture(this,Path.Combine(output,"Monitor-live.png"));
        Go("Dashboard");
        if(liveTimer!=null) throw new InvalidDataException("Monitoring must stop when leaving Monitor.");
        File.WriteAllText(Path.Combine(output,"windows-readings.txt"),$"Read-only integration passed. CPU {sample.CpuPercent:F2}%; physical memory {sample.TotalMemory} bytes. Full scan completed with {vm.Hardware.Warnings.Count} unavailable sources. Cancelled scan rejected. Live timer collected {liveSamples.Count} samples and stopped on navigation. No setting changes, cleanup, or package removal executed.");
    }
}
