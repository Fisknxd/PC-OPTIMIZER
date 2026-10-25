namespace FrameForge.Core;

public sealed record ActivityLine(DateTimeOffset Time,string Message);
public sealed class ActivityFeed
{
    readonly object sync=new();
    readonly List<ActivityLine> lines=[];
    public event Action<ActivityLine>? Added;
    public IReadOnlyList<ActivityLine> Lines { get { lock(sync) return lines.ToArray(); } }
    public void Report(string message)
    {
        var line=new ActivityLine(DateTimeOffset.Now,message);
        lock(sync) { lines.Add(line); if(lines.Count>2000) lines.RemoveRange(0,lines.Count-2000); }
        // Observers cannot interrupt a journaled Windows change.
        if(Added is {} handlers) foreach(Action<ActivityLine> handler in handlers.GetInvocationList()) { try { handler(line); } catch { } }
    }
}
