using FrameForge.Core;
using Xunit;
namespace FrameForge.Tests;
public sealed class PerformanceTweakTests:IDisposable
{
    readonly string root=Path.Combine(Path.GetTempPath(),"ff-performance-"+Guid.NewGuid().ToString("N"));
    static readonly HardwareSnapshot Hardware=new(){WindowsSupported=true,WindowsBuild=26100,Laptop=false};
    sealed class Settings:ISystemSettings
    {
        public bool IsAdministrator=>true;public bool Available=true,IgnoreWrites;public int Writes;
        public SettingValue State=PowerSettings.Pack(new(Guid.NewGuid(),70,45));
        public Dictionary<string,SettingValue> OtherStates=[];
        public Task CheckAvailability(Optimization operation)=>Available?Task.CompletedTask:Task.FromException(new PlatformNotSupportedException("Browser missing"));
        public Task<SettingValue> Read(Optimization operation)=>Task.FromResult(OtherStates.GetValueOrDefault(operation.Id,State));
        public Task Write(Optimization operation,SettingValue state){Writes++;if(!IgnoreWrites)State=state;return Task.CompletedTask;}
    }
    public void Dispose(){if(Directory.Exists(root))Directory.Delete(root,true);}
    static Optimization Op(string id)=>Catalog.All.Single(x=>x.Id==id);
    [Theory][InlineData("cpu-ac-limit",70,45)][InlineData("wifi-ac-power",2,3)]
    public async Task PowerChangesKeepSchemeAndBatteryAndUndoExactly(string id,int ac,int dc)
    {
        var op=Op(id);var original=PowerSettings.Pack(new(Guid.NewGuid(),(uint)ac,(uint)dc));var settings=new Settings{State=original};var engine=new OperationEngine(settings,new(root));
        var preview=await engine.Preview(op,Hardware);var before=PowerSettings.Unpack(op,original);var proposed=PowerSettings.Unpack(op,preview.Proposed);
        Assert.Equal(before.Scheme,proposed.Scheme);Assert.Equal(before.Battery,proposed.Battery);Assert.Equal(PowerSettings.Get(op).Target,proposed.PluggedIn);
        var record=await engine.Apply(preview,Hardware,"power");Assert.Equal("Applied",record!.Status);Assert.Null(await engine.Apply(await engine.Preview(op,Hardware),Hardware,"noop"));Assert.Equal(1,settings.Writes);
        Assert.Equal("Restored",(await engine.Undo(record.Id)).Status);Assert.Equal(original,settings.State);
    }
    [Fact]public async Task ChangingPowerPlanAfterPreviewOrBeforeUndoRefusesTheWrite()
    {
        var op=Op("cpu-ac-limit");var settings=new Settings();var engine=new OperationEngine(settings,new(root));var preview=await engine.Preview(op,Hardware);
        settings.State=PowerSettings.Pack(PowerSettings.Unpack(op,settings.State) with {Scheme=Guid.NewGuid()});
        await Assert.ThrowsAsync<InvalidOperationException>(()=>engine.Apply(preview,Hardware,"stale"));Assert.Equal(0,settings.Writes);
        var record=await engine.Apply(await engine.Preview(op,Hardware),Hardware,"power");settings.State=PowerSettings.Pack(PowerSettings.Unpack(op,settings.State) with {Scheme=Guid.NewGuid()});
        var writes=settings.Writes;Assert.Equal("Restore failed",(await engine.Undo(record!.Id)).Status);Assert.Equal(writes,settings.Writes);
    }
    [Fact]public async Task BatteryEditOutsideTheAppPreventsUndoInsteadOfOverwritingIt()
    {
        var op=Op("cpu-ac-limit");var settings=new Settings();var engine=new OperationEngine(settings,new(root));var record=await engine.Apply(await engine.Preview(op,Hardware),Hardware,"power");
        settings.State=PowerSettings.Pack(PowerSettings.Unpack(op,settings.State) with {Battery=50});var writes=settings.Writes;
        Assert.Equal("Restore failed",(await engine.Undo(record!.Id)).Status);Assert.Equal(writes,settings.Writes);
    }
    [Fact]public async Task IgnoredPowerWriteIsFailedAndRetainsOriginal()
    {
        var settings=new Settings{IgnoreWrites=true};var original=settings.State;var engine=new OperationEngine(settings,new(root));var op=Op("cpu-ac-limit");
        var record=await engine.Apply(await engine.Preview(op,Hardware),Hardware,"ignored");Assert.Equal("Failed",record!.Status);Assert.Equal(original,record.Before);
    }
    [Theory][InlineData("cpu-ac-limit",101)][InlineData("wifi-ac-power",4)]
    public void InvalidPowerValuesAreRejected(string id,int value)=>Assert.Throws<InvalidDataException>(()=>PowerSettings.Unpack(Op(id),PowerSettings.Pack(new(Guid.NewGuid(),(uint)value,0))));
    [Theory][InlineData("update-background-limit",37)][InlineData("update-foreground-limit",88)][InlineData("edge-sleep-sooner",21600)]
    public async Task CustomSupportedPolicyValuesSurviveUndo(string id,int value)
    {
        var op=Op(id);var original=new SettingValue(true,value.ToString());var settings=new Settings{State=original};var engine=new OperationEngine(settings,new(root));
        if(id=="edge-sleep-sooner")settings.OtherStates["edge-sleep"]=FeatureToggles.Target(Op("edge-sleep"),true);
        Assert.Null(FeatureToggles.Enabled(op,original));var record=await engine.Apply(await engine.PreviewToggle(op,Hardware,true),Hardware,"policy");
        Assert.Equal("Applied",record!.Status);Assert.Equal("Restored",(await engine.Undo(record.Id)).Status);Assert.Equal(original,settings.State);
    }
    [Theory][InlineData("update-background-limit","101")][InlineData("update-foreground-limit","-1")][InlineData("edge-sleep-sooner","13")]
    public void InvalidPolicyValuesFailClosed(string id,string value)=>Assert.Throws<InvalidDataException>(()=>FeatureToggles.Enabled(Op(id),new(true,value)));
    [Fact]public async Task MissingBrowserBlocksApplyButDoesNotBlockUndoOfItsSavedPolicy()
    {
        var settings=new Settings{State=new(false,null)};var engine=new OperationEngine(settings,new(root));var op=Op("chrome-memory");var record=await engine.Apply(await engine.PreviewToggle(op,Hardware,true),Hardware,"browser");
        settings.Available=false;await Assert.ThrowsAsync<PlatformNotSupportedException>(()=>engine.PreviewToggle(op,Hardware,false));
        Assert.Equal("Restored",(await engine.Undo(record!.Id)).Status);Assert.False(settings.State.Exists);
    }
    [Theory][InlineData("chrome-memory-strong")][InlineData("edge-sleep-sooner")]
    public async Task StrongBrowserChoiceRequiresItsBaseSettingBeforeAnyProfileWrite(string id)
    {
        var settings=new Settings{State=new(false,null)};var engine=new OperationEngine(settings,new(root));var op=Op(id);
        var preview=await engine.PreviewToggle(op,Hardware,true);await Assert.ThrowsAsync<InvalidOperationException>(()=>new TuningPlanRunner(engine).Apply([preview],Hardware));Assert.Equal(0,settings.Writes);
    }
    [Fact]public void WidgetsCannotBeAppliedOnWindows10()=>Assert.NotNull(Op("widgets").Incompatibility(Hardware with {WindowsBuild=19045}));
    [Fact]public void GamingViewExcludesCosmeticAdAndThemeChoices()
    {
        Assert.False(PerformanceTweaks.Relevant(Op("advertising-id")));Assert.False(PerformanceTweaks.Relevant(Op("dark")));Assert.False(PerformanceTweaks.Relevant(Op("game-tips")));
        Assert.True(PerformanceTweaks.Relevant(Op("game-recording")));Assert.True(PerformanceTweaks.Relevant(Op("chrome-memory")));
    }
}
