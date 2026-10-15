namespace FrameForge.Core;

public static class Catalog
{
    public static IReadOnlyList<Optimization> All { get; } = Create();
    static Optimization[] Create()
    {
        const string explorer = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
        var ops = new List<Optimization>
        {
            new("extensions", "Show file extensions", "Windows Quality of Life", "Show known file extensions in Explorer. Refresh Explorer if necessary.", "Convenience; no FPS claim", "Observed Windows shell preference", Risk.Low, ActionKind.Registry, explorer + "|HideFileExt", "0", Restart:true, Smart:true),
            new("hidden", "Show hidden files", "Registry Configuration", "Show hidden files, without exposing protected operating-system files.", "Convenience only", "Observed Windows shell preference", Risk.Review, ActionKind.Registry, explorer + "|Hidden", "1", Restart:true),
            new("dark", "Dark application theme", "Windows Quality of Life", "Use dark colors in applications that follow the Windows theme.", "Convenience only", "Observed Windows shell preference", Risk.Low, ActionKind.Registry, @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize|AppsUseLightTheme", "0", Smart:true),
            new("balanced", "Balanced power plan experiment", "Power Management", "Switch to the built-in Balanced plan only if it exists. Compare workloads; do not assume any FPS gain.", "Power/thermal tradeoff; measure", "Documented powercfg mechanism", Risk.Experimental, ActionKind.PowerPlan, "381b4222-f694-41f0-9685-ff5bb260df2e")
        };
        void Settings(string id, string name, string cat, string uri, string description) => ops.Add(new(id,name,cat,description,"Review in Windows Settings; no automatic change", "Documented Windows Settings interface",Risk.Low,ActionKind.Settings,uri));
        Settings("gamemode", "Review Game Mode", "Gaming Performance", "ms-settings:gaming-gamemode", "Review Game Mode for your workload. Test before/after rather than assuming disabling it is beneficial.");
        Settings("capture", "Review background recording", "Background Processes", "ms-settings:gaming-gamedvr", "Review capture preferences. Background recording uses resources when active; preserve recording if needed.");
        Settings("startup", "Review startup applications", "Startup Optimization", "ms-settings:startupapps", "Disable only applications you recognize and do not need at login. FrameForge does not disable entries automatically.");
        Settings("memory", "Review background memory pressure", "RAM and Memory", "ms-settings:appsfeatures", "Inspect measured RAM and top processes in Monitor; close optional applications yourself. No memory purger or pagefile tweaks.");
        Settings("storage", "Storage Sense and maintenance", "Storage and Cleanup", "ms-settings:storagesense", "Use Windows storage maintenance. Keep shader caches unless troubleshooting a specific defect. Disk cleanup is not an FPS boost.");
        Settings("network", "Network troubleshooting", "Network and Connectivity", "ms-settings:network-status", "Inspect adapter/DNS and bounded latency diagnostics. DNS resolution speed is not in-game ping.");
        Settings("mouse", "Mouse preferences", "Input and Responsiveness", "ms-settings:mousetouchpad", "Review pointer preferences. Games using raw input may ignore desktop pointer settings.");
        Settings("graphics", "Per-application GPU preference", "GPU Optimization", "ms-settings:display-advancedgraphics", "Choose the intended GPU for a game in Windows. Availability depends on Windows, driver and game.");
        Settings("display", "Refresh rate and displays", "Gaming Performance", "ms-settings:display-advanced", "Check that your monitor uses its intended refresh rate and resolution.");
        Settings("power", "CPU power and thermal review", "CPU Optimization", "ms-settings:powersleep", "Review OEM-supported power settings and chipset drivers. Do not force affinity, parking, scheduler or voltage changes, particularly on X3D.");
        Settings("privacy", "Background permissions", "Privacy and Background Activity", "ms-settings:privacy", "Review app-specific privacy/background permissions; preserve security services.");
        Settings("notifications", "Notifications", "Windows Quality of Life", "ms-settings:notifications", "Review distraction and notification preferences.");
        Settings("clipboard", "Clipboard history", "Windows Quality of Life", "ms-settings:clipboard", "Review clipboard history and its privacy implications.");
        Settings("defaults", "Default applications", "Windows Quality of Life", "ms-settings:defaultapps", "Choose default applications using Windows.");
        Settings("taskbar", "Taskbar preferences", "Windows Quality of Life", "ms-settings:taskbar", "Review supported taskbar preferences.");
        Settings("updates", "Windows Update status", "Windows Services", "ms-settings:windowsupdate", "Review updates and restart scheduling. FrameForge never disables servicing or security services.");
        ops.Add(new("amd", "AMD driver and Anti-Lag guidance", "GPU Optimization", "Anti-Lag 2 requires an integrated supported game, compatible GPU and driver. Use game/Adrenalin controls; never force it externally.","Latency guidance; measure in-game", "Official AMD Anti-Lag 2 SDK",Risk.Low,ActionKind.Web,"https://www.amd.com/en/support",Vendor:Vendor.Amd));
        ops.Add(new("nvidia", "NVIDIA driver and Reflex guidance", "GPU Optimization", "Use Reflex only in supported games. Do not combine latency controls blindly; verify game/driver support and measure.","Latency guidance; measure in-game", "Official NVIDIA Streamline Reflex documentation",Risk.Low,ActionKind.Web,"https://www.nvidia.com/en-us/drivers/",Vendor:Vendor.Nvidia));
        ops.Add(new("intel", "Intel graphics support", "GPU Optimization", "Review official Intel graphics drivers and game compatibility.","Compatibility guidance", "Official Intel support",Risk.Low,ActionKind.Web,"https://www.intel.com/content/www/us/en/download-center/home.html",Vendor:Vendor.Intel));
        return ops.ToArray();
    }
    public static IEnumerable<Optimization> Search(string query, string filter, HardwareSnapshot h, ISet<string>? applied = null) => All.Where(o =>
        (string.IsNullOrWhiteSpace(query) || $"{o.Name} {o.Category} {o.Description} {o.Vendor} {o.Evidence}".Contains(query, StringComparison.OrdinalIgnoreCase)) &&
        (filter switch
        {
            "Recommended" => o.Incompatibility(h) == null && o.Risk == Risk.Low && !(applied?.Contains(o.Id) ?? false),
            "Safe" => o.Risk == Risk.Low,
            "Advanced" => o.Risk != Risk.Low,
            "Requires restart" => o.Restart,
            "AMD" => o.Vendor == Vendor.Amd,
            "NVIDIA" => o.Vendor == Vendor.Nvidia,
            "Applied" => applied?.Contains(o.Id) == true,
            "Not applicable" => o.Incompatibility(h) != null,
            _ => true
        }));
}
