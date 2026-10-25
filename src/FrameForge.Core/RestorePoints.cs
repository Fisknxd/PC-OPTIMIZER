namespace FrameForge.Core;

public sealed record LocalRestorePoint(long Id,string Description,DateTimeOffset CreatedAt);
public sealed record RestorePointPending(bool Active,SettingValue Frequency,string Description);
public interface IRestorePointBackend
{
    bool IsAdministrator {get;}
    Task<IReadOnlyList<LocalRestorePoint>> List();
    SettingValue ReadFrequency();
    void WriteFrequency(SettingValue value);
    Task Create(string description);
}
public sealed class RestorePointManager(IRestorePointBackend backend,JsonStore store,ActivityFeed? activity=null)
{
    readonly SemaphoreSlim gate=new(1,1);
    static readonly SettingValue NoThrottle=new(true,"0");
    public Task<IReadOnlyList<LocalRestorePoint>> List()=>backend.List();
    public bool CanCreate=>backend.IsAdministrator;
    public bool HasPending=>store.Load<RestorePointPending?>("restore-point-pending.json",()=>null)?.Active==true;
    void RequireAdmin() { if(!backend.IsAdministrator) throw new UnauthorizedAccessException("Administrator access is needed. Restart FrameForge as administrator and approve the Windows prompt."); }
    public void RecoverFrequency()
    {
        if(!HasPending) return;
        RequireAdmin(); var pending=store.Load<RestorePointPending>("restore-point-pending.json",()=>throw new InvalidDataException("Missing restore-point backup."));
        var current=backend.ReadFrequency();
        if(current!=pending.Frequency)
        {
            if(current!=NoThrottle) throw new InvalidOperationException("Windows restore-point timing was changed elsewhere. Its saved original is preserved; review before retrying.");
            backend.WriteFrequency(pending.Frequency);
            if(backend.ReadFrequency()!=pending.Frequency) throw new IOException("Windows restore-point timing could not be restored.");
        }
        store.Save("restore-point-pending.json",pending with {Active=false});
        activity?.Report("Windows restore-point creation timing restored to its saved value.");
    }
    public async Task<LocalRestorePoint> Create()
    {
        RequireAdmin(); await gate.WaitAsync();
        try
        {
            RecoverFrequency();
            activity?.Report("Reading existing local Windows restore points…");
            var before=await backend.List();
            var description="FrameForge "+DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")+" "+Guid.NewGuid().ToString("N")[..6];
            var frequency=backend.ReadFrequency();
            store.Save("restore-point-pending.json",new RestorePointPending(true,frequency,description));
            activity?.Report("Saved original restore-point timing. Requesting Windows restore point: "+description);
            LocalRestorePoint? created=null;
            try
            {
                backend.WriteFrequency(NoThrottle);
                if(backend.ReadFrequency()!=NoThrottle) throw new IOException("Windows restore-point timing could not be prepared.");
                await backend.Create(description);
                activity?.Report("Windows creation call returned. Checking its local restore-point list…");
                for(int attempt=0;attempt<10;attempt++)
                {
                    created=(await backend.List()).FirstOrDefault(x=>x.Description==description&&!before.Any(old=>old.Id==x.Id));
                    if(created!=null) break;
                    await Task.Delay(500);
                }
                if(created==null) throw new IOException("Windows did not publish a new restore point. Nothing has been marked as created. Check System Protection and free disk space.");
            }
            finally { RecoverFrequency(); }
            store.Audit("Windows restore point",created.Id.ToString(),"Verified in Windows: "+description);
            activity?.Report("VERIFIED: Local Windows restore point #"+created.Id+" · "+description);
            return created;
        }
        finally { gate.Release(); }
    }
}
