using FrameForge.Core;
using Xunit;
namespace FrameForge.Tests;
public sealed class LowEndPlanTests:IDisposable
{
    readonly string root=Path.Combine(Path.GetTempPath(),"ff-lowend-"+Guid.NewGuid().ToString("N"));
    public void Dispose(){if(Directory.Exists(root))Directory.Delete(root,true);}
    sealed class Settings:ISystemSettings
    {
        public bool IsAdministrator=>true;public int Writes,FailAt;
        public readonly Dictionary<string,SettingValue> Values=Catalog.All.Where(FeatureToggles.Supports).ToDictionary(x=>x.Id,x=>FeatureToggles.Target(x,true));
        public Task<SettingValue> Read(Optimization op)=>Task.FromResult(Values[op.Id]);
        public Task Write(Optimization op,SettingValue value){Writes++;if(Writes==FailAt)throw new IOException("Test failure");Values[op.Id]=value;return Task.CompletedTask;}
    }
    [Fact]public async Task ProfilePreflightRejectsStaleSettingBeforeAnyWrite()
    {
        var settings=new Settings();var engine=new OperationEngine(settings,new JsonStore(root));var hardware=new HardwareSnapshot{WindowsSupported=true};var choices=LowEndPlans.Choices.Take(3);var previews=new List<ActionPreview>();
        foreach(var choice in choices)previews.Add(await engine.PreviewToggle(Catalog.All.Single(x=>x.Id==choice.OperationId),hardware,choice.Enabled));
        settings.Values["transparency"]=new(true,"0");await Assert.ThrowsAsync<InvalidOperationException>(()=>new TuningPlanRunner(engine).Apply(previews,hardware));Assert.Equal(0,settings.Writes);
    }
    [Fact]public async Task ProfileStopsOnFailureAndGroupedUndoRestoresSavedOriginals()
    {
        var settings=new Settings{FailAt=2};var engine=new OperationEngine(settings,new JsonStore(root));var hardware=new HardwareSnapshot{WindowsSupported=true};var previews=new List<ActionPreview>();
        foreach(var choice in LowEndPlans.Choices.Take(3))previews.Add(await engine.PreviewToggle(Catalog.All.Single(x=>x.Id==choice.OperationId),hardware,choice.Enabled));
        var runner=new TuningPlanRunner(engine);var result=await runner.Apply(previews,hardware);Assert.NotNull(result.Error);Assert.Equal(1,result.Applied);Assert.Equal(2,settings.Writes);
        Assert.All(await runner.Undo(result.Session),r=>Assert.Equal("Restored",r.Status));Assert.All(settings.Values,kv=>Assert.Equal(FeatureToggles.Target(Catalog.All.Single(x=>x.Id==kv.Key),true),kv.Value));
    }
    [Fact]public async Task PeerSharingCustomModeIsPreservedByExactUndo()
    {
        var settings=new Settings();settings.Values["update-sharing"]=new(true,"99");var engine=new OperationEngine(settings,new JsonStore(root));var op=Catalog.All.Single(x=>x.Id=="update-sharing");Assert.Null(FeatureToggles.Enabled(op,settings.Values[op.Id]));
        var record=await engine.Apply(await engine.PreviewToggle(op,new(){WindowsSupported=true},false),new(){WindowsSupported=true},"policy");Assert.Equal("0",settings.Values[op.Id].Value);Assert.Equal("Restored",(await engine.Undo(record!.Id)).Status);Assert.Equal("99",settings.Values[op.Id].Value);
    }
}
