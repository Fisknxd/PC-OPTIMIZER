using FrameForge.Core;
using Xunit;
namespace FrameForge.Tests;
public sealed class ServiceAndPriorityTests:IDisposable
{
    readonly string root=Path.Combine(Path.GetTempPath(),"ff-service-priority-"+Guid.NewGuid().ToString("N"));
    JsonStore Store()=>new(root);
    public void Dispose(){if(Directory.Exists(root))Directory.Delete(root,true);}
    sealed class Settings:ISystemSettings
    {
        public bool IsAdministrator {get;set;}=true;
        public SettingValue State=OptionalServices.Pack(new("AutomaticDelayed",true));
        public bool IgnoreWrite;
        public int Writes;
        public Action? BeforeWrite;
        public Task<SettingValue> Read(Optimization op)=>Task.FromResult(State);
        public Task Write(Optimization op,SettingValue value){BeforeWrite?.Invoke();Writes++;if(!IgnoreWrite)State=value;return Task.CompletedTask;}
    }
    public static IEnumerable<object[]> Services=>OptionalServices.All.Select(x=>new object[]{x.Name});
    [Theory][MemberData(nameof(Services))]
    public async Task OptionalServicesJournalAndRestoreExactDelayedStartupAndRunningState(string name)
    {
        var op=Catalog.All.Single(x=>x.Kind==ActionKind.Service&&x.Target==name);var backend=new Settings();var original=backend.State;
        var engine=new OperationEngine(backend,Store());backend.BeforeWrite=()=>Assert.Contains(engine.History,x=>x.Status is "Prepared" or "Restoring");
        var change=await engine.Apply(await engine.Preview(op,new(){WindowsSupported=true}),new(){WindowsSupported=true},"service");
        Assert.Equal("Applied",change!.Status);Assert.Equal(OptionalServices.Paused,backend.State);
        Assert.Equal("Restored",(await engine.Undo(change.Id)).Status);Assert.Equal(original,backend.State);
    }
    [Fact]public async Task ServiceChangesNeedAdminAndRefuseStaleState()
    {
        var op=Catalog.All.First(x=>x.Kind==ActionKind.Service);var backend=new Settings{IsAdministrator=false};var engine=new OperationEngine(backend,Store());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>engine.Preview(op,new(){WindowsSupported=true}));Assert.Equal(0,backend.Writes);
        backend.IsAdministrator=true;var preview=await engine.Preview(op,new(){WindowsSupported=true});backend.State=OptionalServices.Pack(new("Manual",false));
        await Assert.ThrowsAsync<InvalidOperationException>(()=>engine.Apply(preview,new(){WindowsSupported=true},"stale"));Assert.Equal(0,backend.Writes);
    }
    [Fact]public async Task IgnoredServiceChangeCannotReportSuccess()
    {
        var backend=new Settings{IgnoreWrite=true};var engine=new OperationEngine(backend,Store());var op=Catalog.All.First(x=>x.Kind==ActionKind.Service);
        var result=await engine.Apply(await engine.Preview(op,new(){WindowsSupported=true}),new(){WindowsSupported=true},"ignored");Assert.Equal("Failed",result!.Status);Assert.Contains("verification",result.Error);
    }
    [Fact]public void ArbitraryServicesAndMalformedBackupsAreRejected()
    {
        var op=Catalog.All.First(x=>x.Kind==ActionKind.Service);
        Assert.Throws<InvalidOperationException>(()=>OptionalServices.Get(op with {Target="WinDefend"}));
        Assert.Throws<InvalidDataException>(()=>OptionalServices.Unpack(OptionalServices.Pack(new("Boot",false))));
        Assert.Throws<InvalidDataException>(()=>OptionalServices.Unpack(new(true,"{}","Service")));
        Assert.Throws<InvalidDataException>(()=>OptionalServices.Unpack(OptionalServices.Pack(new("Disabled",true))));
    }
    sealed class Priority:IAppPriority
    {
        public PriorityTarget Current=new(42,1234,"C:\\Games\\game.exe","Game","Normal");
        public int Writes;public bool IgnoreWrite;public Action? BeforeWrite;
        public Task<IReadOnlyList<PriorityTarget>> List()=>Task.FromResult<IReadOnlyList<PriorityTarget>>([Current]);
        public Task<PriorityTarget> Read(PriorityTarget identity)=>Task.FromResult(Current);
        public Task Write(PriorityTarget identity,string priority){BeforeWrite?.Invoke();Writes++;if(!IgnoreWrite)Current=Current with {Priority=priority};return Task.CompletedTask;}
    }
    [Fact]public async Task PrioritySavesBeforeWriteAndRestoresOriginal()
    {
        var backend=new Priority();var manager=new AppPriorityManager(backend,Store());backend.BeforeWrite=()=>Assert.Contains(manager.History,x=>x.Status is "Prepared" or "Restoring");
        var change=await manager.Apply(backend.Current,"AboveNormal");Assert.Equal("Applied",change.Status);Assert.Equal("AboveNormal",backend.Current.Priority);
        Assert.Equal("Restored",(await manager.Undo(change.Id)).Status);Assert.Equal("Normal",backend.Current.Priority);
    }
    [Fact]public async Task PriorityRejectsReusedPidAndExternalPriorityChange()
    {
        var backend=new Priority();var manager=new AppPriorityManager(backend,Store());var preview=backend.Current;backend.Current=preview with {Started=9999};
        await Assert.ThrowsAsync<InvalidOperationException>(()=>manager.Apply(preview,"High"));Assert.Equal(0,backend.Writes);
        backend.Current=preview;var change=await manager.Apply(preview,"AboveNormal");backend.Current=backend.Current with {Priority="BelowNormal"};
        Assert.Equal("Restore failed",(await manager.Undo(change.Id)).Status);Assert.Equal(1,backend.Writes);
        backend.Current=backend.Current with {Started=9999,Priority="AboveNormal"};Assert.Equal("Restore failed",(await manager.Undo(change.Id)).Status);Assert.Equal(1,backend.Writes);
    }
    [Fact]public async Task PriorityRefusesOlderUndoUntilNewerChangeIsRestored()
    {
        var backend=new Priority();var manager=new AppPriorityManager(backend,Store());var first=await manager.Apply(backend.Current,"AboveNormal");var second=await manager.Apply(backend.Current,"High");
        Assert.Equal("Restore failed",(await manager.Undo(first.Id)).Status);Assert.Equal("High",backend.Current.Priority);
        Assert.Equal("Restored",(await manager.Undo(second.Id)).Status);Assert.Equal("Restored",(await manager.Undo(first.Id)).Status);Assert.Equal("Normal",backend.Current.Priority);
    }
    [Fact]public async Task PriorityCannotClaimAnIgnoredWriteOrAcceptRealTime()
    {
        var backend=new Priority{IgnoreWrite=true};var manager=new AppPriorityManager(backend,Store());
        await Assert.ThrowsAsync<InvalidOperationException>(()=>manager.Apply(backend.Current,"RealTime"));Assert.Equal(0,backend.Writes);
        Assert.Equal("Failed",(await manager.Apply(backend.Current,"High")).Status);
    }
    [Fact]public async Task FailedPriorityJournalPreventsWrite()
    {
        var backend=new Priority();var store=Store();Directory.CreateDirectory(Path.Combine(root,"priority-history.json.tmp"));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>new AppPriorityManager(backend,store).Apply(backend.Current,"High"));Assert.Equal(0,backend.Writes);
    }
    [Fact]public void ActivityObserversCannotInterruptChangesAndHistoryIsBounded()
    {
        var feed=new ActivityFeed();int delivered=0;feed.Added+=_=>throw new IOException("observer failure");feed.Added+=_=>delivered++;
        for(int i=0;i<2500;i++)feed.Report(i.ToString());Assert.Equal(2500,delivered);Assert.Equal(2000,feed.Lines.Count);Assert.Equal("500",feed.Lines[0].Message);
    }
    [Fact]public void GraphicsChecklistPersistsPerGameAndRejectsUnknownSteps()
    {
        var store=Store();foreach(var profile in PotatoProfiles.All){var value=new PotatoProgress("Old resolution: 1920x1080",[0,1]);store.Save(profile.Game+".json",value);var saved=PotatoProfiles.Validate(profile,store.Load(profile.Game+".json",()=>new PotatoProgress("",[])));Assert.Equal(value.Notes,saved.Notes);Assert.True(saved.Done.SetEquals(value.Done));Assert.Throws<InvalidDataException>(()=>PotatoProfiles.Validate(profile,value with {Done=[999]}));}
    }
    [Fact]public void OlderPreferencesKeepLiveChangesEnabledByDefault()
    {
        var old=System.Text.Json.JsonSerializer.Deserialize<Preferences>("{\"Mode\":\"Balanced\",\"ShowAdvanced\":false,\"GamingGpu\":\"Automatic\"}")!;
        Assert.True(old.WatchChanges);var store=Store();store.Save("preferences.json",old with {WatchChanges=false});Assert.False(store.Load("preferences.json",()=>new Preferences()).WatchChanges);
    }
}
