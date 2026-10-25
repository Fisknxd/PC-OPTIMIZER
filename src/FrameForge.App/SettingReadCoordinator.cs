using FrameForge.Core;

namespace FrameForge.App;

// Read-only probes share a small worker budget. Confirmation/apply always reread via the engine.
internal sealed class SettingReadCoordinator(Func<Optimization,Task<SettingValue>> probe,bool background=true)
{
    sealed record Entry(Task<SettingValue> Work,CancellationToken Owner,DateTimeOffset Started);
    readonly object sync=new();
    readonly SemaphoreSlim workers=new(3);
    readonly Dictionary<string,Entry> cache=[];
    public void Invalidate() { lock(sync)cache.Clear(); }
    public Task<SettingValue> Read(Optimization operation,CancellationToken cancellation=default,bool fresh=false)
    {
        if(!background)return probe(operation); // Deterministic in-memory acceptance adapter.
        Task<SettingValue> work;
        lock(sync)
        {
            if(!fresh&&cache.TryGetValue(operation.Id,out var entry)&&
                (entry.Work.IsCompletedSuccessfully&&DateTimeOffset.UtcNow-entry.Started<TimeSpan.FromSeconds(8)||!entry.Work.IsCompleted&&entry.Owner==cancellation))work=entry.Work;
            else
            {
                work=Task.Run(async ()=>
                {
                    await workers.WaitAsync(cancellation).ConfigureAwait(false);
                    try { cancellation.ThrowIfCancellationRequested();return await probe(operation).ConfigureAwait(false); }
                    finally {workers.Release();}
                });
                cache[operation.Id]=new(work,cancellation,DateTimeOffset.UtcNow);
                // Observe failures even when navigation stopped waiting for this read.
                _=work.ContinueWith(t=>{_ = t.Exception;},CancellationToken.None,TaskContinuationOptions.OnlyOnFaulted,TaskScheduler.Default);
            }
        }
        return work.WaitAsync(cancellation);
    }
}
