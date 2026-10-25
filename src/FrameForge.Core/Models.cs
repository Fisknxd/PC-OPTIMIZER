namespace FrameForge.Core;

public enum Vendor { Unknown, Amd, Nvidia, Intel }
public enum ActionKind { Registry, PowerPlan, Settings, Web, RegistryGroup, Desktop, Service, MachineRegistry, PowerSetting }
public enum Risk { Low, Review, Experimental }
public sealed record HardwareSnapshot
{
    public DateTimeOffset CapturedAt { get; init; } = DateTimeOffset.UtcNow;
    public bool WindowsSupported { get; init; }
    public int WindowsBuild { get; init; }
    public string Os { get; init; } = "Unavailable";
    public string Cpu { get; init; } = "Unavailable";
    public string CpuManufacturer { get; init; } = "Unavailable";
    public int? Cores { get; init; }
    public int? Threads { get; init; }
    public int? ReportedMhz { get; init; }
    public int? MaxMhz { get; init; }
    public string Architecture { get; init; } = "Unavailable";
    public long? L3CacheKb { get; init; }
    public List<Gpu> Gpus { get; init; } = [];
    public long? TotalMemory { get; init; }
    public long? AvailableMemory { get; init; }
    public double? CpuPercent { get; init; }
    public bool? Laptop { get; init; }
    public string Board { get; init; } = "Unavailable";
    public string Bios { get; init; } = "Unavailable";
    public string PowerPlan { get; init; } = "Unavailable";
    public List<string> Displays { get; init; } = [];
    public List<string> MemoryModules { get; init; } = [];
    public List<string> Disks { get; init; } = [];
    public List<string> Volumes { get; init; } = [];
    public List<StorageVolume> StorageVolumes { get; init; } = [];
    public List<string> Network { get; init; } = [];
    public List<string> Startup { get; init; } = [];
    public List<string> Processes { get; init; } = [];
    public List<string> Security { get; init; } = [];
    public int? ProcessCount { get; init; }
    public double? UptimeHours { get; init; }
    public List<string> Warnings { get; init; } = [];
    public bool Has(Vendor vendor) => Gpus.Any(g => g.Vendor == vendor);
}
public sealed record Gpu(string Name, Vendor Vendor, string Driver);
public sealed record SettingValue(bool Exists, string? Value, string Kind = "DWord");
public sealed record Optimization(string Id, string Name, string Category, string Description,
    string Benefit, string Evidence, Risk Risk, ActionKind Kind, string Target, string? Value = null,
    bool Admin = false, bool Restart = false, Vendor Vendor = Vendor.Unknown, bool Smart = false, int MinBuild = 19045)
{
    public bool Reversible => Kind is ActionKind.Registry or ActionKind.PowerPlan or ActionKind.RegistryGroup or ActionKind.Desktop or ActionKind.Service or ActionKind.MachineRegistry or ActionKind.PowerSetting;
    public string? Incompatibility(HardwareSnapshot h) => !h.WindowsSupported ? "Requires Windows 10 22H2 or Windows 11 (build 19045+)." :
        h.WindowsBuild>0 && h.WindowsBuild<MinBuild ? "Requires Windows build "+MinBuild+" or later." :
        Vendor != Vendor.Unknown && !h.Has(Vendor) ? $"No {Vendor} GPU detected." :
        Kind == ActionKind.PowerPlan && h.Laptop != false ? "Manual power review only on laptops or unknown chassis; OEM/thermal restrictions may apply." : null;
    public string Reason(HardwareSnapshot h) => Incompatibility(h) ?? (h.TotalMemory is > 0 and < 16L * 1024 * 1024 * 1024 && Category == "Startup Optimization" ?
        "Less than 16 GiB RAM detected; review optional startup activity." :
        h.Cpu.Contains("X3D", StringComparison.OrdinalIgnoreCase) && Category == "CPU Optimization" ?
        "X3D detected: retain supported AMD chipset scheduler/parking behavior." :
        Vendor != Vendor.Unknown ? $"{Vendor} GPU detected; check game and driver feature support." : Description);
}
public sealed record ActionPreview(Optimization Operation, SettingValue Current, SettingValue Proposed);
public sealed record ChangeRecord
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Session { get; init; } = "";
    public string OperationId { get; init; } = "";
    public string Name { get; init; } = "";
    public DateTimeOffset Time { get; init; } = DateTimeOffset.UtcNow;
    public SettingValue Before { get; init; } = new(false, null);
    public SettingValue After { get; init; } = new(false, null);
    public string Status { get; set; } = "Prepared";
    public string? Error { get; set; }
    public bool Restart { get; init; }
    public string Feedback { get; set; } = "Unrated";
    public bool? WriteCompleted {get;set;}
    public bool RestoreInProgress {get;set;}
}
public sealed record AuditEvent(DateTimeOffset Time, string Kind, string Subject, string Result);
public sealed record Preferences(string Mode = "Safe", bool ShowAdvanced = false, string GamingGpu = "Automatic",bool WatchChanges=true,string[]? LowEndSelection=null);

public static class HardwareRules
{
    public static Vendor IdentifyVendor(string name)=>name.Contains("NVIDIA",StringComparison.OrdinalIgnoreCase)?Vendor.Nvidia : name.Contains("AMD",StringComparison.OrdinalIgnoreCase)||name.Contains("Radeon",StringComparison.OrdinalIgnoreCase)||name.Contains("Advanced Micro",StringComparison.OrdinalIgnoreCase)?Vendor.Amd : name.Contains("Intel",StringComparison.OrdinalIgnoreCase)?Vendor.Intel:Vendor.Unknown;
}

public sealed record StorageVolume(string Name,long TotalBytes,long FreeBytes);
