namespace FrameForge.Core;

public sealed record HealthFinding(string Title,string Detail,string OperationId,bool Attention=false);
public static class HealthAdvisor
{
    public static IReadOnlyList<HealthFinding> Analyze(HardwareSnapshot h)
    {
        var findings=new List<HealthFinding>();
        if(!h.WindowsSupported) return findings;
        if(h.TotalMemory is >0 && h.AvailableMemory is >=0 && h.AvailableMemory<h.TotalMemory*.15)
            findings.Add(new("Memory pressure worth reviewing",$"Only {h.AvailableMemory/1073741824d:F1} GiB is available out of {h.TotalMemory/1073741824d:F1} GiB installed in this sample. Review optional background apps; do not disable the pagefile. This sample does not prove a gaming bottleneck.","memory",true));
        foreach(var volume in h.StorageVolumes.Where(v=>v.TotalBytes>0&&v.FreeBytes>=0&&v.FreeBytes<v.TotalBytes*.10))
            findings.Add(new($"Low free space on {volume.Name}",$"{volume.FreeBytes/1073741824d:F1} GiB free ({100d*volume.FreeBytes/volume.TotalBytes:F0}%). Review files or Storage Sense. Freeing storage is not a measured FPS improvement.","storage",true));
        if(h.CpuPercent is >=80 and <=100)
            findings.Add(new("High CPU activity in the latest sample",$"Windows reported {h.CpuPercent:F0}% CPU utilization. Check Task Manager/Monitor and repeat under the same workload before drawing conclusions. FrameForge will not terminate processes.","capture",true));
        if(h.Startup.Count>8)
            findings.Add(new("Review what starts with Windows",$"The scan found {h.Startup.Count} startup entries. Keep security, accessibility, drivers and applications you rely on. Entry count alone does not establish performance impact.","startup"));
        if(h.Laptop==true) findings.Add(new("Keep laptop thermal and battery limits in mind","Use OEM-supported power controls. Automatic power-plan experiments stay blocked on laptops.","power"));
        if(h.Cpu.Contains("X3D",StringComparison.OrdinalIgnoreCase)) findings.Add(new("Preserve the supported X3D scheduler setup","Use official AMD chipset drivers. Avoid core-parking, affinity and scheduler registry packs.","power"));
        return findings.OrderByDescending(f=>f.Attention).ToList();
    }
}
