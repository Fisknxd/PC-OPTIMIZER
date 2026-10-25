using System.Text.Json;
namespace FrameForge.Core;

public sealed record OptionalService(string Name,string Title,string Description);
public sealed record ServiceState(string StartMode,bool Running);
public static class OptionalServices
{
    // This is a capability allowlist, never a list of everything Windows can stop.
    public static IReadOnlyList<OptionalService> All {get;}=[
        new("Fax","Fax support","Sends and receives faxes. Pause only if you do not use a fax modem or Windows Fax."),
        new("MapsBroker","Downloaded maps","Lets apps use downloaded maps. Pausing makes offline maps unavailable."),
        new("RetailDemo","Shop demonstration mode","Runs the display-PC demo used in shops. Keep it if this is a demonstration PC."),
        new("WMPNetworkSvc","Media sharing","Shares your Media Player library with other devices. Pausing stops that sharing."),
        new("Spooler","Printing","Handles physical printers and Print to PDF. Pausing disables printing until restored."),
        new("DiagTrack","Optional Windows diagnostics","Collects Windows usage and diagnostic data. Pausing reduces this work, but can limit diagnostic information."),
        new("WSearch","File and mail indexing","Builds the search index. Pause only to test measured indexing load; Windows and Outlook searches can become slower.")
    ];
    public static OptionalService Get(Optimization op)
    {
        if(!Catalog.All.Contains(op)||op.Kind!=ActionKind.Service) throw new InvalidOperationException("Unknown service control.");
        return All.Single(x=>x.Name==op.Target);
    }
    public static SettingValue Pack(ServiceState state)=>new(true,JsonSerializer.Serialize(state),"Service");
    public static SettingValue Paused=>Pack(new("Disabled",false));
    public static ServiceState Unpack(SettingValue value)
    {
        if(!value.Exists||value.Kind!="Service") throw new InvalidDataException("Invalid service backup.");
        var state=JsonSerializer.Deserialize<ServiceState>(value.Value??"")??throw new InvalidDataException("Invalid service backup.");
        if(state.StartMode is not ("Automatic" or "AutomaticDelayed" or "Manual" or "Disabled")||state.StartMode=="Disabled"&&state.Running) throw new InvalidDataException("Unsupported service state. No change made.");
        return state;
    }
}
