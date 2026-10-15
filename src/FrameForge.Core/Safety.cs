namespace FrameForge.Core;

public sealed record CleanupFile(string Path, long Bytes, DateTime LastWriteUtc, string? Identity = null);
public sealed record CleanupResult(int Deleted, long Bytes, int Skipped, IReadOnlyList<string> Errors);
public sealed class CleanupService
{
    readonly string root;
    public CleanupService(string userTemp)
    {
        root=Path.GetFullPath(userTemp).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;

    }
    static readonly string[] Excluded = ["shader", "d3d", "dxcache", "glcache", "nvcache", "save", "document", "download", "frameforge", "browser", "chrome", "edge", "firefox"];
    public bool IsSafe(string file, DateTime cutoff)
    {
        try
        {
            var full=Path.GetFullPath(file);
            if (!full.StartsWith(root,OperatingSystem.IsWindows()?StringComparison.OrdinalIgnoreCase:StringComparison.Ordinal)) return false;
            if (!new[]{".tmp",".temp",".log"}.Contains(Path.GetExtension(full),StringComparer.OrdinalIgnoreCase)) return false;
            if (Excluded.Any(s=>Path.GetRelativePath(root,full).Contains(s,StringComparison.OrdinalIgnoreCase))) return false;
            for(var p=full; p!=null; p=Path.GetDirectoryName(p))
            {
                if ((File.GetAttributes(p)&FileAttributes.ReparsePoint)!=0) return false;
            }
            var info=new FileInfo(full);
            return info.Exists && (info.Attributes&(FileAttributes.Directory|FileAttributes.ReadOnly|FileAttributes.System))==0 && info.LastWriteTimeUtc < cutoff;
        }
        catch(IOException) { return false; }
        catch(UnauthorizedAccessException) { return false; }
    }
    public IReadOnlyList<CleanupFile> Preview(DateTime utcNow)
    {
        var results=new List<CleanupFile>();
        if (!Directory.Exists(root)) return results;
        var pending=new Stack<string>(); pending.Push(root);
        while(pending.TryPop(out var dir))
        {
            try
            {
                if ((File.GetAttributes(dir)&FileAttributes.ReparsePoint)!=0) continue;
                foreach(var file in Directory.EnumerateFiles(dir))
                {
                    if (results.Count>=50000) return results;
                    try
                    {
                        if(IsSafe(file,utcNow.AddDays(-7)))
                        {
                            var f=NativeCleanup.Describe(file);
                            if(f.LastWriteUtc<utcNow.AddDays(-7)) results.Add(f);
                        }
                    }
                    catch(IOException) { }
                    catch(UnauthorizedAccessException) { }
                }
                foreach(var child in Directory.EnumerateDirectories(dir))
                    if ((File.GetAttributes(child)&FileAttributes.ReparsePoint)==0 && !Excluded.Any(s=>Path.GetFileName(child).Contains(s,StringComparison.OrdinalIgnoreCase))) pending.Push(child);
            }
            catch(IOException) { }
            catch(UnauthorizedAccessException) { }
        }
        return results;
    }
    public CleanupResult Delete(IReadOnlyList<CleanupFile> approved, DateTime utcNow)
    {
        int deleted=0,skipped=0; long bytes=0; var errors=new List<string>();
        foreach(var f in approved.DistinctBy(f=>f.Path))
        {
            try
            {
                if (!IsSafe(f.Path,utcNow.AddDays(-7))) { skipped++; continue; }
                var info=new FileInfo(f.Path);
                if(info.Length!=f.Bytes||info.LastWriteTimeUtc!=f.LastWriteUtc) { skipped++; continue; }
                NativeCleanup.DeleteExact(f);
                deleted++; bytes+=f.Bytes;
            }
            catch(Exception e) when(e is IOException or UnauthorizedAccessException) { skipped++; errors.Add($"{f.Path}: {e.Message}"); }
        }
        return new(deleted,bytes,skipped,errors);
    }
}
public sealed record InstalledPackage(string Name,string FullName,string Publisher,bool Framework,bool Resource,bool NonRemovable,string[] Dependencies);
public static class DebloatPolicy
{
    // Only narrow consumer packages may be offered. Unknown packages are protected.
    public static readonly string[] Optional = ["Microsoft.BingNews","Microsoft.BingWeather","Microsoft.MicrosoftSolitaireCollection","Clipchamp.Clipchamp"];
    public static readonly string[] Decision = ["Microsoft.YourPhone","MicrosoftTeams","MSTeams","Microsoft.XboxGamingOverlay","Microsoft.GamingApp"];
    public static readonly string[] ProtectedKeywords = ["Store","VCLibs","NET.Native","UI.Xaml","DesktopAppInstaller","SecHealth","WindowsDefender","AAD","AccountsControl","ShellExperience","StartMenu","GamingServices","WindowsAppRuntime","Edge","WebExperience"];
    public static string Classify(InstalledPackage p,IReadOnlyList<InstalledPackage> all)
    {
        if(p.Framework||p.Resource||p.NonRemovable||ProtectedKeywords.Any(k=>p.Name.Contains(k,StringComparison.OrdinalIgnoreCase))||all.Any(x=>x.FullName!=p.FullName&&x.Dependencies.Any(d=>d==p.Name||d==p.FullName))) return "Protected";
        if(Optional.Contains(p.Name,StringComparer.Ordinal)) return "Optional — review";
        if(Decision.Contains(p.Name,StringComparer.Ordinal)) return "User decision";
        return "Protected / unclassified";
    }
    public static bool CanRemove(InstalledPackage p,IReadOnlyList<InstalledPackage> all) => Classify(p,all) is "Optional — review" or "User decision";
}
public sealed record FrameStats(int Frames,double AverageFps,double LowOnePercentFps,double P99FrameMs);
public static class Benchmark
{
    // Explicit exported frame durations only: header FrameTimeMs and one positive value/row.
    public static FrameStats Parse(string csv)
    {
        var lines=csv.Split('\n',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries);
        if(lines.Length<101 || lines[0]!="FrameTimeMs") throw new InvalidDataException("Expected FrameTimeMs CSV header and at least 100 frame durations (milliseconds).");
        var times=lines.Skip(1).Select(x=>double.Parse(x,System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        if(times.Any(x=>!double.IsFinite(x)||x<=0||x>60000)) throw new InvalidDataException("Frame durations must be finite, positive, and at most 60000 ms.");
        var sorted=times.OrderDescending().ToArray();
        return new(times.Length,1000/times.Average(),1000/sorted.Take(Math.Max(1,(int)Math.Ceiling(times.Length*.01))).Average(),sorted[Math.Max(0,(int)Math.Ceiling(times.Length*.01)-1)]);
    }
}
public static class Games
{
    public static readonly string[] Names=["Fortnite","Rust","Counter-Strike 2","Valorant","Apex Legends","Minecraft","Call of Duty"];
    public static string Guidance(string game,HardwareSnapshot h,Vendor selected=Vendor.Unknown)
    {
        var gpu=selected==Vendor.Unknown ? string.Join(", ",h.Gpus.Select(g=>g.Vendor).Distinct()) : selected.ToString();
        var common=$"{game} • GPU: {gpu}\nUse an identical replay/scene, resolution, settings, driver and game version for A/B measurements. Record VSync, frame cap and refresh rate. Warm shader caches before measuring. Change one setting at a time. A system CPU sample cannot prove a game bottleneck.\n\n";
        var specific=game switch
        {
            "Fortnite"=>"Compare Performance Mode and supported DirectX renderers in-game. Reduced geometry/shadows trade visual quality for workload reduction. Allow shaders to compile; do not clear caches routinely.",
            "Rust"=>"Inspect RAM pressure and background memory before lowering texture/detail settings. Use an SSD for load times where available; low free space and paging may affect stutter. Do not disable the pagefile.",
            "Counter-Strike 2"=>"Compare frame caps and supported Reflex/Anti-Lag controls in the game. Keep competitive settings consistent; avoid launch-option packs and affinity changes.",
            "Valorant"=>"Review supported in-game latency controls and multithreaded rendering. Do not modify Vanguard or protected processes.",
            "Apex Legends"=>"Review in-game frame cap, texture memory budget and supported Reflex. Compare frame-time stability, not only average FPS.",
            "Minecraft"=>"Compare render/simulation distances. Allocate heap according to the edition/modpack's documented needs, leaving memory for Windows. No universal JVM flag pack.",
            _=>"Review in-game upscaling, texture memory budget and supported latency controls. Let shaders finish compiling. Do not edit protected files or anti-cheat services."
        };
        return common+specific+"\n\n"+(gpu.Contains("Nvidia",StringComparison.OrdinalIgnoreCase)?"NVIDIA: Reflex must be supported by the game; a vendor match alone does not establish support.\n":"")+(gpu.Contains("Amd",StringComparison.OrdinalIgnoreCase)?"AMD: Anti-Lag 2 needs a supported game, GPU and driver. Radeon Chill/frame limiting trade power, responsiveness and smoothness; compare them deliberately.\n":"")+"\nAutomatic game configuration editing and live FPS capture: Unavailable in this release. Import explicitly exported FrameTimeMs data in Monitor. No injection, anti-cheat manipulation, or invented measurements.";
    }
}
