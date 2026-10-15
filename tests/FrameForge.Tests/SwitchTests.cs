using FrameForge.Core;
using Xunit;
namespace FrameForge.Tests;
public sealed class SwitchTests:IDisposable
{
    readonly string root=Path.Combine(Path.GetTempPath(),"ff-switch-tests-"+Guid.NewGuid().ToString("N"));
    static readonly HardwareSnapshot Windows=new(){WindowsSupported=true,Laptop=false};
    static Optimization Op(string id)=>Catalog.All.Single(o=>o.Id==id);
    public SwitchTests()=>Directory.CreateDirectory(root);
    public void Dispose()=>Directory.Delete(root,true);
    sealed class Adapter:ISystemSettings
    {
        public bool IsAdministrator=>false;
        public SettingValue Current=new(false,null);
        public bool Fail,Ignore;
        public int Writes;
        public Task<SettingValue> Read(Optimization op)=>Task.FromResult(Current);
        public Task Write(Optimization op,SettingValue value)
        {
            Writes++;
            if(Fail) throw new IOException("Write refused");
            if(!Ignore) Current=value;
            return Task.CompletedTask;
        }
    }
    OperationEngine Engine(Adapter adapter)=>new(adapter,new JsonStore(root));
    [Theory] [InlineData("extensions","0","1")] [InlineData("hidden","1","2")] [InlineData("dark","0","1")]
    public async Task SwitchOnOffUndoRestoresExactPreviousValues(string id,string on,string off)
    {
        var a=new Adapter();var engine=Engine(a);var op=Op(id);
        var enabled=await engine.Apply(await engine.Preview(op,Windows,true),Windows,"toggle");Assert.Equal(on,a.Current.Value);Assert.Equal("Applied",enabled!.Status);
        var disabled=await engine.Apply(await engine.Preview(op,Windows,false),Windows,"toggle");Assert.Equal(off,a.Current.Value);Assert.Equal("Applied",disabled!.Status);
        Assert.Equal("Restored",(await engine.Undo(disabled.Id)).Status);Assert.Equal(on,a.Current.Value);
        Assert.Equal("Restored",(await engine.Undo(enabled.Id)).Status);Assert.False(a.Current.Exists);
    }
    [Fact] public async Task AlreadyOnExternalSettingCanBeTurnedOffWithBackup()
    {
        var a=new Adapter{Current=new(true,"0")};var engine=Engine(a);
        var r=await engine.Apply(await engine.Preview(Op("dark"),Windows,false),Windows,"s");
        Assert.Equal("1",a.Current.Value);Assert.Equal("0",r!.Before.Value);
        await engine.Undo(r.Id);Assert.Equal("0",a.Current.Value);
    }
    [Fact] public async Task StaleDisablePreviewCannotOverwriteNewExternalValue()
    {
        var a=new Adapter{Current=new(true,"0")};var engine=Engine(a);var p=await engine.Preview(Op("dark"),Windows,false);a.Current=new(true,"1");
        await Assert.ThrowsAsync<InvalidOperationException>(()=>engine.Apply(p,Windows,"s"));Assert.Equal(0,a.Writes);Assert.Empty(engine.History);
    }
    [Fact] public async Task ForgedDisablePayloadIsRefused()
    {
        var a=new Adapter();var engine=Engine(a);var p=await engine.Preview(Op("dark"),Windows,false);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>engine.Apply(p with {Proposed=new(true,"2")},Windows,"s"));Assert.Equal(0,a.Writes);
    }
    [Fact] public async Task FailedDisableDoesNotClaimOff()
    {
        var a=new Adapter{Current=new(true,"0"),Fail=true};var engine=Engine(a);
        var r=await engine.Apply(await engine.Preview(Op("dark"),Windows,false),Windows,"s");
        Assert.Equal("Failed",r!.Status);Assert.True(FeatureState.From(Op("dark"),a.Current).Enabled);
    }
    [Fact] public async Task UnverifiedDisableIsReportedAsFailure()
    {
        var a=new Adapter{Current=new(true,"0"),Ignore=true};var engine=Engine(a);
        var r=await engine.Apply(await engine.Preview(Op("dark"),Windows,false),Windows,"s");Assert.Equal("Failed",r!.Status);Assert.Contains("verification",r.Error);
    }
    [Fact] public async Task ManualFeaturesCannotBeTurnedOffByFakeSwitch()
    {
        var engine=Engine(new());
        await Assert.ThrowsAsync<InvalidOperationException>(()=>engine.Preview(Op("balanced"),Windows,false));
        await Assert.ThrowsAsync<InvalidOperationException>(()=>engine.Preview(Op("gamemode"),Windows,false));
        Assert.All(Catalog.All.Where(o=>o.CanToggle),o=>Assert.Equal(ActionKind.Registry,o.Kind));
    }
    [Fact] public async Task UnknownStateIsNotBlindlyChanged()
    {
        var a=new Adapter{Current=new(true,"17")};var engine=Engine(a);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>engine.Preview(Op("dark"),Windows,false));Assert.Equal(0,a.Writes);
    }
    [Fact] public void DefaultAndUnavailableAreNotPresentedAsMeasuredOff()
    {
        var op=Op("dark");Assert.Null(FeatureState.From(op,new(false,null)).Enabled);Assert.Equal("Windows default",FeatureState.From(op,new(false,null)).Label);
        Assert.Null(FeatureState.From(op,new(true,"9")).Enabled);Assert.NotNull(FeatureState.From(op,new(true,"9")).Error);
        Assert.NotNull(FeatureState.From(op,new(true,"0","String")).Error);
        Assert.True(FeatureState.From(op,new(true,"0")).Enabled);Assert.False(FeatureState.From(op,new(true,"1")).Enabled);
    }
    [Fact] public void OldPreferencesLoadAndFavoritesPersist()
    {
        File.WriteAllText(Path.Combine(root,"preferences.json"),"{\"Mode\":\"Balanced\",\"GamingGpu\":\"Automatic\"}");var store=new JsonStore(root);
        var old=store.Load("preferences.json",()=>new Preferences());Assert.Equal("Balanced",old.Mode);Assert.Null(old.Favorites);Assert.False(old.TipsDismissed);
        store.Save("preferences.json",old with {Favorites=["dark","extensions"],TipsDismissed=true});var loaded=store.Load("preferences.json",()=>new Preferences());Assert.Equal(new[]{"dark","extensions"},loaded.Favorites);Assert.True(loaded.TipsDismissed);
    }
}
