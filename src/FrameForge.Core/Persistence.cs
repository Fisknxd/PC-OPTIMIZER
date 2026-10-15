using System.Text.Json;

namespace FrameForge.Core;

public sealed class JsonStore
{
    readonly string root;
    readonly object sync = new();
    static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    public string Root => root;
    public JsonStore(string root) { this.root = Path.GetFullPath(root); Directory.CreateDirectory(this.root); }
    string PathFor(string name)
    {
        if (Path.GetFileName(name) != name || !name.EndsWith(".json", StringComparison.Ordinal)) throw new ArgumentException("Invalid store name");
        return Path.Combine(root, name);
    }
    public T Load<T>(string name, Func<T> empty)
    {
        lock(sync)
        {
            var p = PathFor(name);
            if (!File.Exists(p)) return empty();
            // Corrupt state must never silently become an empty journal.
            return JsonSerializer.Deserialize<T>(File.ReadAllText(p), Options) ?? throw new InvalidDataException($"Invalid {name}. Preserve it and recover its .bak file manually.");
        }
    }
    public void Save<T>(string name, T value)
    {
        lock(sync)
        {
            var p = PathFor(name); var tmp = p + ".tmp";
            using (var stream = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, value, Options); stream.Flush(flushToDisk: true);
            }
            if (File.Exists(p)) File.Copy(p, p + ".bak", overwrite:true);
            File.Move(tmp,p,overwrite:true);
        }
    }
    public void Audit(string kind, string subject, string result)
    {
        lock(sync)
        {
            var entries=Load<List<AuditEvent>>("audit.json",()=>[]);
            entries.Add(new(DateTimeOffset.UtcNow,kind,subject,result));
            Save("audit.json",entries.TakeLast(1000).ToList());
        }
    }
}
public interface ISystemSettings
{
    bool IsAdministrator { get; }
    Task<SettingValue> Read(Optimization operation);
    Task Write(Optimization operation, SettingValue state);
}
public sealed class OperationEngine(ISystemSettings system, JsonStore store)
{
    readonly SemaphoreSlim gate = new(1,1);
    public List<ChangeRecord> History => store.Load<List<ChangeRecord>>("history.json",()=>[]);
    public async Task<ActionPreview> Preview(Optimization op, HardwareSnapshot hardware, bool enabled = true)
    {
        if (op.Incompatibility(hardware) is {} reason) throw new InvalidOperationException(reason);
        if (!op.Reversible) throw new InvalidOperationException("This action opens a reviewed external interface and is not an automatic operation.");
        if (op.Admin && !system.IsAdministrator) throw new UnauthorizedAccessException("Administrator permission required; no changes made.");
        if(!enabled&&!op.CanToggle) throw new InvalidOperationException("This action does not support an on/off switch.");
        var current=await system.Read(op);
        if (current.Exists && op.Kind == ActionKind.Registry && current.Kind != "DWord") throw new InvalidOperationException("Unexpected existing registry type; refusing to overwrite.");
        if(op.CanToggle&&current.Exists&&current.Value!=op.Value&&current.Value!=op.DisabledValue) throw new InvalidOperationException("This setting has an unrecognized value. Inspect it before changing it.");
        return new(op,current,new(true,op.Kind == ActionKind.PowerPlan ? op.Target : enabled ? op.Value : op.DisabledValue, op.Kind == ActionKind.PowerPlan ? "Guid" : "DWord"),enabled);
    }
    public async Task<ChangeRecord?> Apply(ActionPreview preview, HardwareSnapshot hardware, string session)
    {
        await gate.WaitAsync();
        try
        {
            var op=Catalog.All.Single(o=>o.Id==preview.Operation.Id);
            var fresh=await Preview(op,hardware,preview.Enabled);
            if (fresh != preview) throw new InvalidOperationException("Configuration changed since preview. Review a new preview.");
            if (fresh.Current == fresh.Proposed) return null;
            var changes=History;
            var record=new ChangeRecord { OperationId=op.Id,Name=op.Name,Before=fresh.Current,After=fresh.Proposed,Session=session,Restart=op.Restart };
            changes.Add(record); store.Save("history.json",changes); // write-ahead, before touching Windows
            try
            {
                await system.Write(op,record.After);
                if (await system.Read(op) != record.After) throw new IOException("Post-change verification failed.");
                record.Status="Applied";
            }
            catch(Exception e) { record.Status="Failed"; record.Error=e.Message; }
            store.Save("history.json",changes);
            return record;
        }
        finally { gate.Release(); }
    }
    public async Task<ChangeRecord> Undo(string id)
    {
        await gate.WaitAsync();
        try
        {
            var history=History; var r=history.Single(x=>x.Id==id);
            if (r.Status=="Restored") return r;
            var op=Catalog.All.Single(x=>x.Id==r.OperationId);
            if (!op.Reversible) throw new InvalidOperationException("Operation is not reversible.");
            if (op.Admin && !system.IsAdministrator) throw new UnauthorizedAccessException("Administrator permission required.");
            try
            {
                var current=await system.Read(op);
                if (current!=r.Before)
                {
                    if (current!=r.After) throw new InvalidOperationException("Restore conflict: the setting changed outside this record. No overwrite performed.");
                    r.Status="Restoring"; store.Save("history.json",history);
                    await system.Write(op,r.Before);
                    if (await system.Read(op)!=r.Before) throw new IOException("Restore verification failed.");
                }
                r.Status="Restored"; r.Error=null;
            }
            catch(Exception e) { r.Status="Restore failed"; r.Error=e.Message; }
            store.Save("history.json",history); return r;
        }
        finally { gate.Release(); }
    }
    public void Feedback(string id,string feedback)
    {
        if (feedback is not ("Beneficial" or "Neutral" or "Harmful")) throw new ArgumentException("Invalid feedback");
        var h=History; h.Single(x=>x.Id==id).Feedback=feedback; store.Save("history.json",h);
    }
}
