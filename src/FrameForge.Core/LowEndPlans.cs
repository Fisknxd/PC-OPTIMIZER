namespace FrameForge.Core;
public sealed record LowEndChoice(string OperationId,bool Enabled,string Title,string Description,bool Selected=false);
public sealed record TuningPlanResult(string Session,int Applied,int AlreadyConfigured,string? Error);
public static class LowEndPlans
{
    public static IReadOnlyList<LowEndChoice> Choices {get;}=[
        new("edge-background",false,"Let Edge close fully","Stops startup boost and background mode. Close Edge normally to release its memory; tabs are preserved.",true),
        new("game-recording",false,"Stop unused Windows game recording","Turns off Windows game clip capture. Leave unchecked if you record clips.",true),
        new("transparency",false,"Reduce desktop transparency","Reduces decorative desktop drawing on weak graphics chips.",true),
        new("app-animation",false,"Remove Windows app animations","Makes supported desktop controls appear without animations.",true),
        new("menu-animation",false,"Remove desktop menu animations","Removes menu animation overhead; no in-game input-delay claim.",true),
        new("game-mode",true,"Use Windows Game Mode","Uses Windows' gaming mode. Restart your game after applying.",true),
        new("chrome-background",false,"Let Chrome close fully","Stops background Chrome apps after closing the browser. Notifications can stop."),
        new("chrome-memory",true,"Let Chrome release idle tabs","Frees unused tab memory while Chrome stays open. Tabs may reload later."),
        new("chrome-memory-strong",true,"Release Chrome tabs sooner","Stronger Memory Saver setting. Select the Memory Saver option above too."),
        new("edge-sleep",true,"Let Edge sleep idle tabs","Saves CPU and memory in local/work Edge profiles. Personal Microsoft-account profiles ignore this policy."),
        new("edge-sleep-sooner",true,"Sleep Edge tabs after five minutes","Needs sleeping tabs. Local/work profiles only. Check edge://policy after applying."),
        new("widgets",false,"Turn off Windows Widgets","Blocks the Windows 11 news panel. Sign out afterwards; shared Windows components stay installed."),
        new("search-backoff",true,"Keep search indexing out of the way","Restores normal backoff if another tweak forced full-speed indexing. Search stays enabled."),
        new("update-background-limit",true,"Slow background update downloads","Limits Microsoft background downloads to 10% of measured bandwidth. Updates take longer."),
        new("store-background",true,"Block Windows app background work","Stronger option. App notifications and sync can stop. Supported Windows editions only."),
        new("update-sharing",false,"Stop peer sharing of Windows updates","Avoids peer upload/download traffic. Normal Windows updates stay enabled."),
        new("high-performance",true,"Try High performance power","Desktop-only experiment, if this PC has the plan. More heat and power use; Experiments option required.")
    ];
}
public sealed class TuningPlanRunner(OperationEngine engine)
{
    public async Task<TuningPlanResult> Apply(IReadOnlyList<ActionPreview> previews,HardwareSnapshot hardware)
    {
        if(previews.Any(p=>!LowEndPlans.Choices.Any(c=>c.OperationId==p.Operation.Id))||previews.GroupBy(p=>p.Operation.Id).Any(g=>g.Count()>1))throw new InvalidOperationException("Unknown or repeated profile operation.");
        // Base browser settings precede their stronger dependent settings, regardless
        // of selection/input ordering. The engine revalidates them before each write.
        previews=previews.OrderBy(p=>LowEndPlans.Choices.ToList().FindIndex(c=>c.OperationId==p.Operation.Id)).ToArray();
        // Complete preflight before the first mutation. Engine.Apply rechecks each preview too.
        foreach(var preview in previews)
        {
            var choice=LowEndPlans.Choices.Single(c=>c.OperationId==preview.Operation.Id);
            var fresh=FeatureToggles.Supports(preview.Operation)?await engine.PreviewToggle(preview.Operation,hardware,choice.Enabled):await engine.Preview(preview.Operation,hardware);
            if(fresh!=preview)throw new InvalidOperationException("Profile settings changed since preview. Refresh before applying.");
        }
        foreach(var dependency in new[]{("chrome-memory-strong","chrome-memory","Chrome Memory Saver"),("edge-sleep-sooner","edge-sleep","Edge sleeping tabs")})
        {
            if(!previews.Any(p=>p.Operation.Id==dependency.Item1)||previews.Any(p=>p.Operation.Id==dependency.Item2))continue;
            var required=Catalog.All.Single(x=>x.Id==dependency.Item2);var current=await engine.PreviewToggle(required,hardware,true);
            if(FeatureToggles.Enabled(required,current.Current)!=true)throw new InvalidOperationException("Select "+dependency.Item3+" as well, or enable it first. The stronger setting alone may have no effect.");
        }
        string session="lowend-"+Guid.NewGuid().ToString("N");int applied=0,already=0;
        foreach(var preview in previews)
        {
            try {var result=await engine.Apply(preview,hardware,session);if(result==null)already++;else if(result.Status=="Applied")applied++;else return new(session,applied,already,preview.Operation.Name+": "+result.Error);}
            catch(Exception error){return new(session,applied,already,preview.Operation.Name+": "+error.Message);}
        }
        return new(session,applied,already,null);
    }
    public async Task<IReadOnlyList<ChangeRecord>> Undo(string session)
    {
        if(!session.StartsWith("lowend-",StringComparison.Ordinal))throw new InvalidOperationException("Unknown tuning session.");
        var results=new List<ChangeRecord>();
        foreach(var record in engine.History.Where(r=>r.Session==session&&r.Status!="Restored").Reverse())
        {
            var result=await engine.Undo(record.Id);results.Add(result);if(result.Status!="Restored")break;
        }
        return results;
    }
}
