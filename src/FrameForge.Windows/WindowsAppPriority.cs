using System.Diagnostics;
using System.Management;
using FrameForge.Core;
namespace FrameForge.Windows;

public sealed class WindowsAppPriority:IAppPriority
{
    static HashSet<int> ServicePids()
    {
        // Fail closed if service ownership cannot be determined. Shared service hosts are excluded.
        using var search=new ManagementObjectSearcher("SELECT ProcessId FROM Win32_Service WHERE ProcessId > 0");
        search.Options.Timeout=TimeSpan.FromSeconds(8); using var results=search.Get(); var pids=new HashSet<int>();
        foreach(ManagementObject row in results) using(row) pids.Add(Convert.ToInt32(row["ProcessId"])); return pids;
    }
    static PriorityTarget Inspect(Process process,HashSet<int> services)
    {
        if(process.Id==Environment.ProcessId||process.SessionId!=Process.GetCurrentProcess().SessionId||process.MainWindowHandle==IntPtr.Zero||services.Contains(process.Id)) throw new InvalidOperationException("Select a desktop app, not a Windows or service process.");
        var path=process.MainModule?.FileName??throw new InvalidOperationException("App location unavailable.");
        if(path.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.Windows)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Windows processes are protected.");
        if(new[]{"anticheat","anti-cheat","easyanticheat","battleye","beservice","vgc","vgtray","msmpeng"}.Any(s=>path.Contains(s,StringComparison.OrdinalIgnoreCase))) throw new InvalidOperationException("Security and anti-cheat processes are protected.");
        var priority=process.PriorityClass.ToString();
        if(priority is not ("Idle" or "BelowNormal" or "Normal" or "AboveNormal" or "High")) throw new InvalidOperationException("This app uses a protected scheduling mode.");
        return new(process.Id,process.StartTime.ToUniversalTime().Ticks,path,process.ProcessName,priority);
    }
    public Task<IReadOnlyList<PriorityTarget>> List()=>Task.Run<IReadOnlyList<PriorityTarget>>(()=>
    {
        var services=ServicePids(); var result=new List<PriorityTarget>();
        foreach(var process in Process.GetProcesses()) using(process)
        {
            try {result.Add(Inspect(process,services));}
            catch(Exception e) when(e is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException) { }
        }
        return result.OrderBy(x=>x.Name).ToArray();
    });
    public Task<PriorityTarget> Read(PriorityTarget identity)=>Task.Run(()=>
    {
        using var process=Process.GetProcessById(identity.Pid); var current=Inspect(process,ServicePids());
        if(!AppPriorityManager.SameProcess(identity,current)) throw new InvalidOperationException("The original app exited or restarted. Refresh the list."); return current;
    });
    public async Task Write(PriorityTarget identity,string priority)
    {
        if(priority is not ("Idle" or "BelowNormal" or "Normal" or "AboveNormal" or "High")) throw new InvalidDataException("Unsupported priority.");
        _=await Read(identity);
        using var process=Process.GetProcessById(identity.Pid);
        // Keep this process handle open across identity check and write to prevent PID reuse.
        _=process.Handle;
        if(process.StartTime.ToUniversalTime().Ticks!=identity.Started||!string.Equals(process.MainModule?.FileName,identity.Path,StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("App restarted before the change.");
        process.PriorityClass=Enum.Parse<ProcessPriorityClass>(priority);
    }
}
