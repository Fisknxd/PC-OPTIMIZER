namespace FrameForge.Core;
public static class ProfileSelection
{
    public static IReadOnlyDictionary<string,string> Dependencies {get;}=new Dictionary<string,string>{["chrome-memory-strong"]="chrome-memory",["edge-sleep-sooner"]="edge-sleep"};
    public static HashSet<string> Resolve(IEnumerable<string> requested,ISet<string> available,ISet<string> configured)
    {
        var result=requested.Where(available.Contains).ToHashSet(StringComparer.Ordinal);
        foreach(var (dependent,required) in Dependencies)
        {
            if(!result.Contains(dependent)||configured.Contains(required))continue;
            if(available.Contains(required))result.Add(required);else result.Remove(dependent);
        }
        return result;
    }
    public static HashSet<string> Toggle(IEnumerable<string> selected,string changed,bool enabled,ISet<string> available,ISet<string> configured)
    {
        var result=selected.ToHashSet(StringComparer.Ordinal);
        if(enabled)result.Add(changed);else{result.Remove(changed);foreach(var (dependent,required) in Dependencies)if(required==changed)result.Remove(dependent);}
        return Resolve(result,available,configured);
    }
    public static IEnumerable<string> Preset(string preset)=>preset switch
    {
        "Recommended"=>LowEndPlans.Choices.Where(x=>x.Selected).Select(x=>x.OperationId),
        "Browser memory"=>["edge-background","chrome-background","chrome-memory","chrome-memory-strong","edge-sleep","edge-sleep-sooner"],
        "Clear selection"=>[],
        _=>throw new InvalidOperationException("Unknown profile selection.")
    };
}
