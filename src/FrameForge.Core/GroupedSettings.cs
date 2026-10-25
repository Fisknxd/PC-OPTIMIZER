using System.Text.Json;
namespace FrameForge.Core;

public static class GroupedSettings
{
    public static SettingValue Pack(IEnumerable<SettingValue> values)=>new(true,JsonSerializer.Serialize(values),"RegistryGroup");
    public static bool CanRecoverPartial(Optimization operation,ChangeRecord record,SettingValue current)
    {
        var interrupted=record.WriteCompleted==false&&record.Status is "Prepared" or "Failed" or "Restore failed"||record.WriteCompleted==null&&record.Status is "Prepared" or "Failed" or "Restoring";
        if(operation.Kind!=ActionKind.RegistryGroup||!(record.RestoreInProgress||interrupted))return false;
        var definition=TweakDefinitions.Get(operation);
        var before=TweakDefinitions.ReadGroup(record.Before,definition);var after=TweakDefinitions.ReadGroup(record.After,definition);var now=TweakDefinitions.ReadGroup(current,definition);
        return now.Select((value,index)=>value==before[index]||value==after[index]).All(x=>x);
    }
    // Compensating writes are verified and never replace a value that changed externally.
    // A process crash is handled separately by the engine's durable write-ahead journal.
    public static async Task Write(TweakDefinition definition,SettingValue target,Func<int,Task<SettingValue>> read,Func<int,SettingValue,Task> write,ActivityFeed? activity=null)
    {
        var requested=TweakDefinitions.ReadGroup(target,definition);var original=new SettingValue[requested.Length];
        for(int i=0;i<original.Length;i++)original[i]=await read(i);
        _=TweakDefinitions.ReadGroup(Pack(original),definition);
        var attempted=new List<int>();
        try
        {
            for(int i=0;i<requested.Length;i++)
            {
                if(await read(i)!=original[i])throw new InvalidOperationException("A grouped setting changed while applying. That external change was left alone.");
                if(original[i]==requested[i])continue;
                attempted.Add(i);activity?.Report("Write "+definition.Members![i].Name+" → "+(requested[i].Exists?requested[i].Value:"remove saved value"));
                await write(i,requested[i]);if(await read(i)!=requested[i])throw new IOException("A grouped value did not match after writing.");
            }
        }
        catch(Exception error)
        {
            var failures=new List<string>();
            foreach(var i in attempted.AsEnumerable().Reverse())
            {
                try
                {
                    var current=await read(i);if(current==original[i])continue;
                    if(current!=requested[i])throw new IOException("Value changed outside this operation; left alone.");
                    await write(i,original[i]);if(await read(i)!=original[i])throw new IOException("Original-value readback failed.");
                    activity?.Report("Recovered original: "+definition.Members![i].Name);
                }
                catch(Exception recovery){failures.Add(definition.Members![i].Name+": "+recovery.Message);}
            }
            throw new IOException(error.Message+(failures.Count==0?" Changes made by this attempt were rolled back and checked.":" Some values need Recovery: "+string.Join("; ",failures)),error);
        }
    }
}

public sealed record SessionUndoResult(int Restored,int Remaining,string? Error);
public static class RecoverySessions
{
    public static async Task<SessionUndoResult> Undo(OperationEngine engine,string session)
    {
        if(string.IsNullOrWhiteSpace(session))throw new InvalidOperationException("This older record has no session. Undo it individually.");
        var pending=engine.History.Where(x=>x.Session==session&&x.Status!="Restored").Reverse().ToList();int restored=0;
        foreach(var record in pending)
        {
            try
            {
                var result=await engine.Undo(record.Id);
                if(result.Status!="Restored")return new(restored,pending.Count-restored,result.Name+": "+result.Error);
                restored++;
            }
            catch(Exception error){return new(restored,pending.Count-restored,record.Name+": "+error.Message);}
        }
        return new(restored,0,null);
    }
}
