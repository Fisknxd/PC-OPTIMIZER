using System.Text.Json;
using FrameForge.Core;
namespace FrameForge.Windows;

public static class ServiceControls
{
    public static async Task<SettingValue> Read(Optimization op)
    {
        var name=OptionalServices.Get(op).Name;
        // All interpolated identifiers come exclusively from OptionalServices, not user input.
        var json=await Commands.PowerShell($"$s=Get-CimInstance Win32_Service -Filter \"Name='{name}'\"; if(!$s){{throw 'This optional service is not installed.'}}; if($s.State -notin @('Running','Stopped')){{throw 'Service is changing state. Refresh in a moment.'}}; $mode=switch($s.StartMode){{'Auto'{{'Automatic'}} 'Manual'{{'Manual'}} 'Disabled'{{'Disabled'}} default{{throw 'Unsupported startup mode'}}}}; if($mode -eq 'Automatic' -and (Get-ItemProperty 'HKLM:\\SYSTEM\\CurrentControlSet\\Services\\{name}' -Name DelayedAutoStart -ErrorAction SilentlyContinue).DelayedAutoStart -eq 1){{$mode='AutomaticDelayed'}}; @{{StartMode=$mode; Running=($s.State -eq 'Running')}} | ConvertTo-Json -Compress");
        var state=JsonSerializer.Deserialize<ServiceState>(json)??throw new InvalidDataException("Windows returned no service state.");
        var packed=OptionalServices.Pack(state); _=OptionalServices.Unpack(packed); return packed;
    }
    public static async Task Write(Optimization op,SettingValue target,ActivityFeed? activity=null)
    {
        var name=OptionalServices.Get(op).Name; var requested=OptionalServices.Unpack(target);
        if(!new WindowsSettings().IsAdministrator) throw new UnauthorizedAccessException("Administrator access is needed to change services.");
        var original=OptionalServices.Unpack(await Read(op));
        try { await Commands.PowerShell(BuildWriteScript(op,OptionalServices.Pack(requested)),45,activity); }
        catch(Exception error)
        {
            activity?.Report("Service change failed. Attempting to put its original state back…");
            try { if(await Read(op)!=OptionalServices.Pack(original)) await Commands.PowerShell(BuildWriteScript(op,OptionalServices.Pack(original)),45,activity); if(await Read(op)!=OptionalServices.Pack(original)) throw new IOException("Rollback readback did not match."); }
            catch(Exception rollback) { throw new IOException(error.Message+" Original-state recovery also failed: "+rollback.Message+". Open Services and use the saved Recovery values.",error); }
            throw new IOException(error.Message+" Original service state was restored and verified.",error);
        }
    }
    public static string BuildWriteScript(Optimization op,SettingValue value)
    {
        var name=OptionalServices.Get(op).Name;var state=OptionalServices.Unpack(value);
        var mode=state.StartMode switch {"Automatic"=>"auto","AutomaticDelayed"=>"delayed-auto","Manual"=>"demand",_=>"disabled"};
        return $"$s=Get-Service -Name '{name}'; "+
            (!state.Running?"if(@($s.DependentServices | Where-Object Status -ne 'Stopped').Count -gt 0){throw 'Another service depends on this one. No forced stop is allowed.'}; ":"")+
            $"Write-Output 'Setting {name} startup: {state.StartMode}'; & (Join-Path $env:SystemRoot 'System32\\sc.exe') config '{name}' start= '{mode}'; if($LASTEXITCODE -ne 0){{throw 'Windows refused the startup change'}}; "+
            (state.Running?$"Write-Output 'Starting {name}'; Start-Service -Name '{name}'; ":$"Write-Output 'Stopping {name} without forcing dependents'; Stop-Service -Name '{name}'; ")+
            $"$s=Get-Service -Name '{name}'; $s.WaitForStatus('{(state.Running?"Running":"Stopped")}',[TimeSpan]::FromSeconds(15)); Write-Output 'Service command finished; FrameForge will read Windows again.'";
    }
}
