using FrameForge.Core;
namespace FrameForge.App;
// Used only by --ui-test; ordinary startup always uses the real Windows adapter.
internal sealed class UiTestSettings:ISystemSettings
{
    readonly Dictionary<string,SettingValue> values=[];
    public bool IsAdministrator=>false;
    public Task<SettingValue> Read(Optimization op)=>Task.FromResult(values.GetValueOrDefault(op.Id)??new(false,null));
    public Task Write(Optimization op,SettingValue value) {values[op.Id]=value;return Task.CompletedTask;}
}
