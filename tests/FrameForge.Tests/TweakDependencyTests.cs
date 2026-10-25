using FrameForge.Core;
using Xunit;
namespace FrameForge.Tests;

public sealed class TweakDependencyTests:IDisposable
{
    readonly string root=Path.Combine(Path.GetTempPath(),"ff-dependencies-"+Guid.NewGuid().ToString("N"));
    static readonly HardwareSnapshot Hardware=new(){WindowsSupported=true,WindowsBuild=26100,Laptop=false};
    static Optimization Op(string id)=>Catalog.All.Single(x=>x.Id==id);
    sealed class Settings:ISystemSettings
    {
        public bool IsAdministrator=>true;
        public readonly Dictionary<string,SettingValue> States=[];
        public readonly List<string> Writes=[];
        public string? Unavailable;
        public Task CheckAvailability(Optimization op)=>op.Id==Unavailable?Task.FromException(new PlatformNotSupportedException("Setting unavailable")):Task.CompletedTask;
        public Task<SettingValue> Read(Optimization op)=>Task.FromResult(States.GetValueOrDefault(op.Id,new(false,null)));
        public Task Write(Optimization op,SettingValue state){Writes.Add(op.Id);States[op.Id]=state;return Task.CompletedTask;}
    }
    public void Dispose(){if(Directory.Exists(root))Directory.Delete(root,true);}
    [Theory][InlineData("chrome-memory-strong","chrome-memory")][InlineData("edge-sleep-sooner","edge-sleep")]
    public async Task DependencyChangedAfterPreviewPreventsAnyWriteOrJournal(string dependent,string required)
    {
        var settings=new Settings();settings.States[required]=FeatureToggles.Target(Op(required),true);
        var engine=new OperationEngine(settings,new(root));var preview=await engine.PreviewToggle(Op(dependent),Hardware,true);
        settings.States[required]=FeatureToggles.Target(Op(required),false);
        var error=await Assert.ThrowsAsync<InvalidOperationException>(()=>engine.Apply(preview,Hardware,"stale"));
        Assert.Contains(Op(required).Name,error.Message);Assert.Empty(settings.Writes);Assert.Empty(engine.History);
    }
    [Theory][InlineData("chrome-memory-strong","chrome-memory")][InlineData("edge-sleep-sooner","edge-sleep")]
    public async Task UnavailableDependencyIsNotTreatedAsEnabled(string dependent,string required)
    {
        var settings=new Settings{Unavailable=required};settings.States[required]=FeatureToggles.Target(Op(required),true);
        var engine=new OperationEngine(settings,new(root));var preview=await engine.PreviewToggle(Op(dependent),Hardware,true);
        await Assert.ThrowsAsync<PlatformNotSupportedException>(()=>engine.Apply(preview,Hardware,"unavailable"));
        Assert.Empty(settings.Writes);Assert.Empty(engine.History);
    }
    [Theory][InlineData("chrome-memory-strong","chrome-memory")][InlineData("edge-sleep-sooner","edge-sleep")]
    public async Task ReversedProfileInputStillAppliesPrerequisiteBeforeDependent(string dependent,string required)
    {
        var settings=new Settings();var engine=new OperationEngine(settings,new(root));
        var previews=new[]{await engine.PreviewToggle(Op(dependent),Hardware,true),await engine.PreviewToggle(Op(required),Hardware,true)};
        var result=await new TuningPlanRunner(engine).Apply(previews,Hardware);
        Assert.Null(result.Error);Assert.Equal(2,result.Applied);Assert.Equal(new[]{required,dependent},settings.Writes);
        Assert.All(new[]{required,dependent},id=>Assert.True(FeatureToggles.Enabled(Op(id),settings.States[id])));
    }
    [Theory][InlineData("chrome-memory-strong")][InlineData("edge-sleep-sooner")]
    public async Task DisablingDependentDoesNotRequireEnablingItsBase(string id)
    {
        var settings=new Settings();settings.States[id]=FeatureToggles.Target(Op(id),true);var engine=new OperationEngine(settings,new(root));
        var result=await engine.Apply(await engine.PreviewToggle(Op(id),Hardware,false),Hardware,"off");
        Assert.Equal("Applied",result!.Status);Assert.False(FeatureToggles.Enabled(Op(id),settings.States[id]));
    }
}
