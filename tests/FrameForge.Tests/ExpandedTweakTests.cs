using FrameForge.Core;
using Xunit;
namespace FrameForge.Tests;
public sealed class ExpandedTweakTests:IDisposable
{
    readonly string root=Path.Combine(Path.GetTempPath(),"ff-expanded-"+Guid.NewGuid().ToString("N"));
    sealed class Settings:ISystemSettings
    {
        public bool IsAdministrator=>true;
        public SettingValue State=new(false,null);
        public Task<SettingValue> Read(Optimization operation)=>Task.FromResult(State);
        public Task Write(Optimization operation,SettingValue state){State=state;return Task.CompletedTask;}
    }
    public void Dispose(){if(Directory.Exists(root))Directory.Delete(root,true);}
    public static IEnumerable<object[]> Tweaks=>TweakDefinitions.All.Select(x=>new object[]{x.Id});
    [Theory] [MemberData(nameof(Tweaks))] public async Task EverySwitchWorksInBothDirectionsAndRestoresItsOriginal(string id)
    {
        var operation=Catalog.All.Single(x=>x.Id==id); var system=new Settings{State=FeatureToggles.Target(operation,false)}; var original=system.State;
        var engine=new OperationEngine(system,new JsonStore(root)); var hardware=new HardwareSnapshot{WindowsSupported=true,Laptop=false};
        var on=await engine.Apply(await engine.PreviewToggle(operation,hardware,true),hardware,"on"); Assert.Equal("Applied",on!.Status); Assert.True(FeatureToggles.Enabled(operation,system.State));
        var off=await engine.Apply(await engine.PreviewToggle(operation,hardware,false),hardware,"off"); Assert.Equal("Applied",off!.Status); Assert.False(FeatureToggles.Enabled(operation,system.State));
        Assert.Equal("Restored",(await engine.Undo(off.Id)).Status); Assert.Equal("Restored",(await engine.Undo(on.Id)).Status); Assert.Equal(original,system.State);
    }
    [Fact] public void RecordingMixedStateIsNotClaimedAsOnOrOff()
    {
        var operation=Catalog.All.Single(x=>x.Id=="game-recording"); var state=new SettingValue(true,System.Text.Json.JsonSerializer.Serialize(new[]{new SettingValue(true,"0"),new SettingValue(false,null)}),"RegistryGroup");
        Assert.Null(FeatureToggles.Enabled(operation,state));
    }
    [Fact] public void MalformedRecordingBackupsAreRejectedBeforeAnyWrite()
    {
        var operation=Catalog.All.Single(x=>x.Id=="game-recording");
        Assert.Throws<InvalidDataException>(()=>FeatureToggles.Enabled(operation,new(true,"[]","RegistryGroup")));
    }
    [Fact] public async Task CustomMouseThresholdsArePreservedByUndo()
    {
        var operation=Catalog.All.Single(x=>x.Id=="mouse-acceleration"); var system=new Settings{State=new(true,"3,8,2","Mouse")}; var original=system.State;
        var engine=new OperationEngine(system,new JsonStore(root)); var hardware=new HardwareSnapshot{WindowsSupported=true};
        var off=await engine.Apply(await engine.PreviewToggle(operation,hardware,false),hardware,"off"); await engine.Undo(off!.Id); Assert.Equal(original,system.State);
    }
    [Fact] public void DescriptionsAreShortAndGuidesAreActionsNotAutomaticTweaks()
    {
        Assert.All(Catalog.All.Where(FeatureToggles.Supports),x=>Assert.InRange(x.Description.Length,20,180));
        Assert.All(SetupGuides.All,x=>{Assert.False(string.IsNullOrWhiteSpace(x.Summary));Assert.False(string.IsNullOrWhiteSpace(x.Steps));if(x.Control is {} id)Assert.Contains(Catalog.All,o=>o.Id==id&&o.Kind is ActionKind.Settings or ActionKind.Web);});
    }
}
