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
    Task CheckAvailability(Optimization operation)=>Task.CompletedTask;
    Task<SettingValue> Read(Optimization operation);
    Task Write(Optimization operation, SettingValue state);
}
public sealed class OperationEngine(ISystemSettings system, JsonStore store, ActivityFeed? activity=null)
{
    readonly SemaphoreSlim gate = new(1,1);
    public List<ChangeRecord> History => store.Load<List<ChangeRecord>>("history.json",()=>[]);
    public async Task<ActionPreview> Preview(Optimization op, HardwareSnapshot hardware)
    {
        if (op.Incompatibility(hardware) is {} reason) throw new InvalidOperationException(reason);
        if (!op.Reversible) throw new InvalidOperationException("This action opens a reviewed external interface and is not an automatic operation.");
        if (op.Admin && !system.IsAdministrator) throw new UnauthorizedAccessException("Administrator permission required; no changes made.");
        await system.CheckAvailability(op);
        var current=await system.Read(op);
        if (current.Exists && op.Kind is ActionKind.Registry or ActionKind.MachineRegistry && current.Kind != "DWord") throw new InvalidOperationException("Unexpected existing registry type; refusing to overwrite.");
        return new(op,current,op.Kind==ActionKind.PowerSetting?PowerSettings.Target(op,current):op.Kind==ActionKind.Service?OptionalServices.Paused:FeatureToggles.Supports(op)?FeatureToggles.Target(op,true):new(true,op.Kind == ActionKind.PowerPlan ? op.Target : op.Value, op.Kind == ActionKind.PowerPlan ? "Guid" : "DWord"));
    }
    public async Task<ActionPreview> PreviewToggle(Optimization operation,HardwareSnapshot hardware,bool enabled)
    {
        var target=FeatureToggles.Target(operation,enabled);
        var preview=await Preview(operation,hardware);
        var current=FeatureToggles.Enabled(operation,preview.Current);
        return preview with { Proposed=current==enabled?preview.Current:target };
    }
    public async Task<ChangeRecord?> Apply(ActionPreview preview, HardwareSnapshot hardware, string session)
    {
        await gate.WaitAsync();
        try
        {
            var op=Catalog.All.Single(o=>o.Id==preview.Operation.Id);
            activity?.Report("Checking current setting: "+op.Name);
            ActionPreview fresh;
            if(FeatureToggles.Supports(op))
            {
                var enabled=preview.Proposed==preview.Current&&FeatureToggles.Enabled(op,preview.Current) is {} existing?existing:preview.Proposed==FeatureToggles.Target(op,true)?true:preview.Proposed==FeatureToggles.Target(op,false)?false:
                    throw new InvalidOperationException("Unrecognized toggle target. No change made.");
                fresh=await PreviewToggle(op,hardware,enabled);
            }
            else fresh=await Preview(op,hardware);
            if (fresh != preview) throw new InvalidOperationException("Configuration changed since preview. Review a new preview.");
            if (fresh.Current == fresh.Proposed) return null;
            var changes=History;
            var record=new ChangeRecord { OperationId=op.Id,Name=op.Name,Before=fresh.Current,After=fresh.Proposed,Session=session,Restart=op.Restart,WriteCompleted=false };
            changes.Add(record); store.Save("history.json",changes); // write-ahead, before touching Windows
            activity?.Report("Original saved to Recovery. Before: "+record.Before.Value+" → Requested: "+record.After.Value);
            try
            {
                await system.Write(op,record.After);
                activity?.Report("Reading Windows again to verify the result…");
                if (await system.Read(op) != record.After) throw new IOException("Post-change verification failed.");
                record.Status="Applied";record.WriteCompleted=true;
                activity?.Report("VERIFIED: "+op.Name+". Windows matches the requested value.");
            }
            catch(Exception e) { record.Status="Failed"; record.Error=e.Message; activity?.Report("FAILED: "+e.Message); }
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
            activity?.Report("Restoring original setting: "+op.Name);
            if (!op.Reversible) throw new InvalidOperationException("Operation is not reversible.");
            if (op.Admin && !system.IsAdministrator) throw new UnauthorizedAccessException("Administrator permission required.");
            try
            {
                if(history.Skip(history.IndexOf(r)+1).Any(x=>x.OperationId==r.OperationId&&x.Status!="Restored"))
                    throw new InvalidOperationException("Restore newer records for this setting first. No overwrite performed.");
                var current=await system.Read(op);
                if (current!=r.Before)
                {
                    if (current!=r.After&&!GroupedSettings.CanRecoverPartial(op,r,current)) throw new InvalidOperationException("Restore conflict: the setting changed outside this record. No overwrite performed.");
                    r.Status="Restoring";r.RestoreInProgress=true; store.Save("history.json",history);
                    await system.Write(op,r.Before);
                    if (await system.Read(op)!=r.Before) throw new IOException("Restore verification failed.");
                }
                r.Status="Restored"; r.Error=null;r.RestoreInProgress=false;
                activity?.Report("VERIFIED: Original setting restored.");
            }
            catch(Exception e) { r.Status="Restore failed"; r.Error=e.Message; activity?.Report("RESTORE FAILED: "+e.Message); }
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
