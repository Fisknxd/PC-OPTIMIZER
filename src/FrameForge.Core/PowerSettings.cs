using System.Text.Json;
namespace FrameForge.Core;

public sealed record PowerSettingDefinition(string Id,Guid Subgroup,Guid Setting,uint Target,uint Maximum,string Unit);
public sealed record PowerSettingState(Guid Scheme,uint PluggedIn,uint Battery);
public static class PowerSettings
{
    public static IReadOnlyList<PowerSettingDefinition> All {get;}=[
        new("cpu-ac-limit",new("54533251-82be-4824-96c1-47b60b740d00"),new("bc5038f7-23e0-4960-96da-33abaf5935ec"),100,100,"% CPU limit"),
        new("wifi-ac-power",new("19cbb8fa-5279-450e-9fac-8a3d5fedd0c1"),new("12bbebe6-58d6-4636-95bb-3217ef867c1a"),0,3,"Wi-Fi power mode")
    ];
    public static PowerSettingDefinition Get(Optimization op)
    {
        if(!Catalog.All.Contains(op)||op.Kind!=ActionKind.PowerSetting)throw new InvalidOperationException("Unknown power setting.");
        return All.Single(x=>x.Id==op.Id);
    }
    public static SettingValue Pack(PowerSettingState state)=>new(true,JsonSerializer.Serialize(state),"PowerSetting");
    public static PowerSettingState Unpack(Optimization op,SettingValue value)
    {
        var definition=Get(op);
        if(!value.Exists||value.Kind!="PowerSetting")throw new InvalidDataException("Invalid power setting backup.");
        var state=JsonSerializer.Deserialize<PowerSettingState>(value.Value??"")??throw new InvalidDataException("Invalid power setting backup.");
        if(state.Scheme==Guid.Empty||state.PluggedIn>definition.Maximum||state.Battery>definition.Maximum)throw new InvalidDataException("Power setting is outside its supported range.");
        return state;
    }
    public static SettingValue Target(Optimization op,SettingValue current)=>Pack(Unpack(op,current) with {PluggedIn=Get(op).Target});
    public static string Describe(Optimization op,SettingValue value)
    {
        var state=Unpack(op,value);return op.Id=="cpu-ac-limit"?state.PluggedIn+"% CPU limit when plugged in":state.PluggedIn==0?"Full Wi-Fi performance when plugged in":"Wi-Fi power-saving level "+state.PluggedIn;
    }
}
