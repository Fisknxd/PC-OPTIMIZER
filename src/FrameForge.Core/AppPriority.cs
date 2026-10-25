namespace FrameForge.Core;

public sealed record PriorityTarget(int Pid,long Started,string Path,string Name,string Priority);
public sealed record PriorityChange
{
    public string Id {get;init;}=Guid.NewGuid().ToString("N");
    public DateTimeOffset Time {get;init;}=DateTimeOffset.UtcNow;
    public required PriorityTarget Before {get;init;}
    public required string After {get;init;}
    public string Status {get;set;}="Prepared";
    public string? Error {get;set;}
}
public interface IAppPriority
{
    Task<IReadOnlyList<PriorityTarget>> List();
    Task<PriorityTarget> Read(PriorityTarget identity);
    Task Write(PriorityTarget identity,string priority);
}
public sealed class AppPriorityManager(IAppPriority backend,JsonStore store,ActivityFeed? activity=null)
{
    readonly SemaphoreSlim gate=new(1,1);
    public List<PriorityChange> History=>store.Load<List<PriorityChange>>("priority-history.json",()=>[]);
    public static bool SameProcess(PriorityTarget a,PriorityTarget b)=>a.Pid==b.Pid&&a.Started==b.Started&&a.Path.Equals(b.Path,StringComparison.OrdinalIgnoreCase);
    public async Task<PriorityChange> Apply(PriorityTarget preview,string priority)
    {
        if(priority is not ("BelowNormal" or "Normal" or "AboveNormal" or "High")) throw new InvalidOperationException("Unsupported priority. Real-time is never offered.");
        await gate.WaitAsync();
        try
        {
            var current=await backend.Read(preview);
            if(!SameProcess(preview,current)||current.Priority!=preview.Priority) throw new InvalidOperationException("This app changed or restarted. Refresh before continuing.");
            if(current.Priority==priority) throw new InvalidOperationException("This app already has that priority.");
            var record=new PriorityChange {Before=current,After=priority}; var history=History; history.Add(record); store.Save("priority-history.json",history);
            try
            {
                activity?.Report($"Set process priority: {current.Name} · PID {current.Pid} · {current.Priority} → {priority}");
                await backend.Write(current,priority); var readback=await backend.Read(current);
                if(!SameProcess(current,readback)||readback.Priority!=priority) throw new IOException("Priority verification failed.");
                record.Status="Applied"; activity?.Report("VERIFIED: App priority changed. It lasts until the app exits.");
            }
            catch(Exception e) {record.Status="Failed";record.Error=e.Message; activity?.Report("FAILED: "+e.Message);}
            store.Save("priority-history.json",history); return record;
        }
        finally {gate.Release();}
    }
    public async Task<PriorityChange> Undo(string id)
    {
        await gate.WaitAsync();
        try
        {
            var history=History; var record=history.Single(x=>x.Id==id); if(record.Status=="Restored") return record;
            try
            {
                if(history.Skip(history.IndexOf(record)+1).Any(x=>SameProcess(x.Before,record.Before)&&x.Status!="Restored")) throw new InvalidOperationException("Restore newer priority changes for this app first.");
                var current=await backend.Read(record.Before);
                if(!SameProcess(current,record.Before)) throw new InvalidOperationException("The original app has exited. A new process will not be changed.");
                if(current.Priority!=record.Before.Priority)
                {
                    if(current.Priority!=record.After) throw new InvalidOperationException("Priority changed outside FrameForge. Refresh and review it manually.");
                    record.Status="Restoring"; store.Save("priority-history.json",history);
                    await backend.Write(current,record.Before.Priority); var result=await backend.Read(current);
                    if(!SameProcess(current,result)||result.Priority!=record.Before.Priority) throw new IOException("Priority restore verification failed.");
                }
                record.Status="Restored";record.Error=null;activity?.Report("VERIFIED: Previous app priority restored.");
            }
            catch(Exception e) {record.Status="Restore failed";record.Error=e.Message;activity?.Report("RESTORE FAILED: "+e.Message);}
            store.Save("priority-history.json",history); return record;
        }
        finally {gate.Release();}
    }
}
