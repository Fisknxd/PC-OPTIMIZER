using FrameForge.Core;
using Xunit;
namespace FrameForge.Tests;
public sealed class RestorePointTests:IDisposable
{
    readonly string root=Path.Combine(Path.GetTempPath(),"ff-restore-"+Guid.NewGuid().ToString("N"));
    sealed class Backend:IRestorePointBackend
    {
        public bool IsAdministrator {get;set;}=true;
        public SettingValue Frequency=new(false,null);
        public readonly List<LocalRestorePoint> Points=[new(1,"Existing",DateTimeOffset.UtcNow.AddDays(-1))];
        public bool FailCreate,SkipCreate;
        public Action? AfterCreate;
        public int FrequencyWrites;
        public Task<IReadOnlyList<LocalRestorePoint>> List()=>Task.FromResult<IReadOnlyList<LocalRestorePoint>>(Points.ToList());
        public SettingValue ReadFrequency()=>Frequency;
        public void WriteFrequency(SettingValue value) {FrequencyWrites++; Frequency=value;}
        public Task Create(string description)
        {
            if(FailCreate) throw new IOException("Protection unavailable");
            if(!SkipCreate) Points.Add(new(2,description,DateTimeOffset.UtcNow)); AfterCreate?.Invoke(); return Task.CompletedTask;
        }
    }
    public void Dispose(){if(Directory.Exists(root))Directory.Delete(root,true);}
    [Fact] public async Task OnlyANewPointVerifiedInWindowsCountsAsCreated()
    {
        var backend=new Backend(); var store=new JsonStore(root); var manager=new RestorePointManager(backend,store);
        var point=await manager.Create(); Assert.Equal(2,point.Id); Assert.StartsWith("FrameForge ",point.Description);
        Assert.False(backend.Frequency.Exists); Assert.Equal(2,backend.FrequencyWrites); Assert.False(manager.HasPending);
        Assert.Contains(store.Load<List<AuditEvent>>("audit.json",()=>[]),x=>x.Result.Contains("Verified in Windows"));
    }
    [Fact] public async Task ExistingCustomFrequencyIsPreserved()
    {
        var backend=new Backend{Frequency=new(true,"1440")}; var manager=new RestorePointManager(backend,new JsonStore(root));
        await manager.Create(); Assert.Equal(new(true,"1440"),backend.Frequency);
    }
    [Fact] public async Task FailureRestoresFrequencyAndDoesNotInventSuccess()
    {
        var backend=new Backend{FailCreate=true}; var store=new JsonStore(root); var manager=new RestorePointManager(backend,store);
        await Assert.ThrowsAsync<IOException>(()=>manager.Create()); Assert.False(backend.Frequency.Exists); Assert.False(manager.HasPending);
        Assert.Empty(store.Load<List<AuditEvent>>("audit.json",()=>[]));
    }
    [Fact] public async Task SuccessWithoutANewPointIsRejected()
    {
        var backend=new Backend{SkipCreate=true}; var manager=new RestorePointManager(backend,new JsonStore(root));
        await Assert.ThrowsAsync<IOException>(()=>manager.Create()); Assert.False(backend.Frequency.Exists); Assert.Single(backend.Points);
    }
    [Fact] public async Task NoAdminMeansNoSystemWrite()
    {
        var backend=new Backend{IsAdministrator=false}; var manager=new RestorePointManager(backend,new JsonStore(root));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>manager.Create()); Assert.Equal(0,backend.FrequencyWrites);
    }
    [Fact] public void InterruptedPreparationRestoresTheDurableOriginal()
    {
        var backend=new Backend{Frequency=new(true,"0")}; var store=new JsonStore(root);
        store.Save("restore-point-pending.json",new RestorePointPending(true,new(false,null),"interrupted"));
        var manager=new RestorePointManager(backend,store); manager.RecoverFrequency(); Assert.False(backend.Frequency.Exists); Assert.False(manager.HasPending);
    }
    [Fact] public async Task ExternalFrequencyChangesAreNotOverwritten()
    {
        var backend=new Backend(); backend.AfterCreate=()=>backend.Frequency=new(true,"17"); var manager=new RestorePointManager(backend,new JsonStore(root));
        await Assert.ThrowsAsync<InvalidOperationException>(()=>manager.Create()); Assert.Equal("17",backend.Frequency.Value); Assert.True(manager.HasPending);
    }
}
