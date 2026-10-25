using FrameForge.Core;
using Xunit;
namespace FrameForge.Tests;

public sealed class FeatureToggleTests:IDisposable
{
    readonly string root=Path.Combine(Path.GetTempPath(),"ff-toggle-"+Guid.NewGuid().ToString("N"));
    static readonly HardwareSnapshot Desktop=new(){WindowsSupported=true,Laptop=false};
    sealed class Settings:ISystemSettings
    {
        public bool IsAdministrator=>false;
        public SettingValue State=new(false,null);
        public int Writes;
        public bool IgnoreWrites;
        public Task<SettingValue> Read(Optimization op)=>Task.FromResult(State);
        public Task Write(Optimization op,SettingValue state) { Writes++; if(!IgnoreWrites) State=state; return Task.CompletedTask; }
    }
    public void Dispose() { if(Directory.Exists(root)) Directory.Delete(root,true); }
    [Theory] [InlineData("extensions","1")] [InlineData("hidden","2")] [InlineData("dark","1")]
    public async Task EnableDisableAndUndoPreserveExactOriginals(string id,string off)
    {
        var op=Catalog.All.Single(x=>x.Id==id); var system=new Settings(); var store=new JsonStore(root); var engine=new OperationEngine(system,store);
        Assert.Null(FeatureToggles.Enabled(op,system.State));
        var on=await engine.Apply(await engine.PreviewToggle(op,Desktop,true),Desktop,"on");
        Assert.True(FeatureToggles.Enabled(op,system.State)); Assert.Equal("Applied",on!.Status);
        var disabled=await engine.Apply(await engine.PreviewToggle(op,Desktop,false),Desktop,"off");
        Assert.Equal(off,system.State.Value); Assert.False(FeatureToggles.Enabled(op,system.State));
        Assert.Equal("Restored",(await engine.Undo(disabled!.Id)).Status); Assert.True(FeatureToggles.Enabled(op,system.State));
        Assert.Equal("Restored",(await engine.Undo(on.Id)).Status); Assert.False(system.State.Exists);
    }
    [Fact] public async Task ExternalChangesAfterPreviewAreNotOverwritten()
    {
        var op=Catalog.All.Single(x=>x.Id=="extensions"); var system=new Settings{State=new(true,"0")}; var engine=new OperationEngine(system,new JsonStore(root));
        var off=await engine.PreviewToggle(op,Desktop,false); system.State=new(false,null);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>engine.Apply(off,Desktop,"off")); Assert.Equal(0,system.Writes); Assert.Empty(engine.History);
    }
    [Fact] public async Task DisableAlreadyOffDoesNotWriteOrCreateHistory()
    {
        var op=Catalog.All.Single(x=>x.Id=="dark"); var system=new Settings{State=new(true,"1")}; var engine=new OperationEngine(system,new JsonStore(root));
        Assert.Null(await engine.Apply(await engine.PreviewToggle(op,Desktop,false),Desktop,"off")); Assert.Equal(0,system.Writes); Assert.Empty(engine.History);
    }
    [Fact] public async Task FailedDisableVerificationKeepsOriginalValueInJournal()
    {
        var op=Catalog.All.Single(x=>x.Id=="dark"); var system=new Settings{State=new(true,"0"),IgnoreWrites=true}; var engine=new OperationEngine(system,new JsonStore(root));
        var result=await engine.Apply(await engine.PreviewToggle(op,Desktop,false),Desktop,"off");
        Assert.Equal("Failed",result!.Status); Assert.Equal("0",result.Before.Value); Assert.Equal("1",result.After.Value);
    }
    [Theory] [InlineData("gamemode")] [InlineData("balanced")] [InlineData("nvidia")]
    public void ExternalAndNonBinaryControlsCannotBeToggled(string id)=>Assert.Throws<InvalidOperationException>(()=>FeatureToggles.Target(Catalog.All.Single(x=>x.Id==id),false));
    [Fact] public async Task UnrecognizedTargetsAndExistingValuesAreRejected()
    {
        var op=Catalog.All.Single(x=>x.Id=="extensions"); var system=new Settings(); var engine=new OperationEngine(system,new JsonStore(root));
        var preview=await engine.PreviewToggle(op,Desktop,true);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>engine.Apply(preview with {Proposed=new(true,"42")},Desktop,"bad"));
        system.State=new(true,"2"); await Assert.ThrowsAsync<InvalidDataException>(()=>engine.PreviewToggle(op,Desktop,true)); Assert.Equal(0,system.Writes);
    }
}
