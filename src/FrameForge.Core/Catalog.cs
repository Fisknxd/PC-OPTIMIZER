namespace FrameForge.Core;

public static class Catalog
{
    public static IReadOnlyList<Optimization> All { get; } = Create();
    static Optimization[] Create()
    {
        const string explorer = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
        var ops = new List<Optimization>
        {
            new("extensions", "Show file extensions", "Windows Quality of Life", "Shows file endings like .exe and .zip, so it is easier to tell what a file is.", "Convenience; no FPS claim", "Observed Windows shell preference", Risk.Low, ActionKind.Registry, explorer + "|HideFileExt", "0", Restart:true, Smart:true),
            new("hidden", "Show hidden files", "Registry Configuration", "Shows files Windows normally hides. Protected Windows system files stay hidden.", "Convenience only", "Observed Windows shell preference", Risk.Review, ActionKind.Registry, explorer + "|Hidden", "1", Restart:true),
            new("dark", "Dark application theme", "Windows Quality of Life", "Uses darker colors in apps that follow your Windows color choice.", "Convenience only", "Observed Windows shell preference", Risk.Low, ActionKind.Registry, @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize|AppsUseLightTheme", "0", Smart:true),
            new("balanced", "Balanced power plan experiment", "Power Management", "Switch to the built-in Balanced plan only if it exists. Compare workloads; do not assume any FPS gain.", "Power/thermal tradeoff; measure", "Documented powercfg mechanism", Risk.Experimental, ActionKind.PowerPlan, "381b4222-f694-41f0-9685-ff5bb260df2e")
        };
        ops.AddRange([
            new("game-mode","Game Mode","Gaming","Gives Windows a gaming-focused mode. Results depend on your game and PC.","Gaming behavior; compare your game","Microsoft Windows settings reference",Risk.Low,ActionKind.Registry,@"Software\Microsoft\GameBar|AutoGameModeEnabled","1"),
            new("game-recording","Windows game recording","Gaming","Lets Windows record game clips. Turn off if you never use recording.","Avoid unused capture work","Windows Game DVR settings",Risk.Review,ActionKind.RegistryGroup,"GameRecording","1"),
            new("controller-bar","Controller opens Game Bar","Gaming","Lets the controller's Xbox button open Game Bar. Turn off to avoid accidental pop-ups.","Fewer accidental interruptions","Microsoft Windows settings reference",Risk.Low,ActionKind.Registry,@"Software\Microsoft\GameBar|UseNexusForGameBarEnabled","1"),
            new("game-tips","Game Bar startup tips","Less clutter","Shows older Game Bar welcome tips. Turn off to hide those prompts.","Fewer prompts; no FPS effect","Observed Game Bar preference",Risk.Low,ActionKind.Registry,@"Software\Microsoft\GameBar|ShowStartupPanel","1"),
            new("notifications-toggle","App notification pop-ups","Less clutter","Lets apps show notification banners. Turn off for fewer interruptions while playing.","Fewer pop-ups","Windows notification preference",Risk.Review,ActionKind.Registry,@"Software\Microsoft\Windows\CurrentVersion\PushNotifications|ToastEnabled","1",Restart:true),
            new("mouse-acceleration","Mouse acceleration","Mouse & desktop","Changes cursor distance based on how fast you move the mouse. Try Off for steadier desktop movement.","Aim consistency outside raw-input games","SystemParametersInfo SPI_SETMOUSE",Risk.Review,ActionKind.Desktop,"MouseAcceleration"),
            new("menu-animation","Menu animations","Mouse & desktop","Makes desktop menus slide or fade in. Off makes them appear without the animation.","Desktop feel only","SystemParametersInfo SPI_SETMENUANIMATION",Risk.Low,ActionKind.Desktop,"MenuAnimation"),
            new("app-animation","Windows app animations","Mouse & desktop","Animates supported Windows controls. Off reduces decorative movement on the desktop.","Desktop feel only","SystemParametersInfo SPI_SETCLIENTAREAANIMATION",Risk.Low,ActionKind.Desktop,"ClientAnimation"),
            new("quick-menus","Faster desktop menus","Mouse & desktop","Shortens the wait before desktop submenus open. It does not reduce delay inside games.","Desktop menu response only","SystemParametersInfo SPI_SETMENUSHOWDELAY",Risk.Low,ActionKind.Desktop,"MenuDelay")
        ]);
        ops.AddRange([
            new("edge-background","Keep Edge ready in the background","Low-end PC","Keeps Edge loaded after closing it. Off can free memory on small PCs; the next browser launch may be slower.","Less idle browser memory","Microsoft Edge BackgroundModeEnabled and StartupBoostEnabled policies",Risk.Review,ActionKind.RegistryGroup,"EdgeBackground"),
            new("store-background","Pause Windows app background work","Low-end PC","Blocks supported Windows apps from working while closed. Can reduce background work, but notifications and sync may stop.","Less optional background work","Microsoft Privacy policy LetAppsRunInBackground",Risk.Review,ActionKind.MachineRegistry,@"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy|LetAppsRunInBackground","2",Admin:true),
            new("update-sharing","Share updates with other PCs","Low-end PC","Lets Windows share update files with nearby PCs. Off stops peer sharing while keeping normal updates working.","Avoid peer upload traffic","Microsoft Delivery Optimization DODownloadMode",Risk.Review,ActionKind.MachineRegistry,@"SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization|DODownloadMode","1",Admin:true),
            new("transparency","Windows transparency effects","Low-end PC","Adds transparent desktop effects. Off reduces extra desktop drawing work, especially on weaker graphics chips.","Less desktop rendering work","Windows transparency preference",Risk.Low,ActionKind.Registry,@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize|EnableTransparency","1",Restart:true),
            new("high-performance","High performance power plan","Power Management","Tries the installed High performance plan on desktop PCs. May reduce power-saving waits but increases heat and power use.","Power-response experiment; measure","Microsoft powercfg supported power plans",Risk.Experimental,ActionKind.PowerPlan,"8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c")
        ]);
        foreach(var service in OptionalServices.All) ops.Add(new("service-"+service.Name,"Pause "+service.Title,"Optional services",service.Description,"Less unused background work; no guaranteed FPS gain","Windows Service Control Manager; optional capability review",Risk.Review,ActionKind.Service,service.Name,Admin:true));
        const string content=@"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager";
        ops.AddRange([
            new("advertising-id","Personalized app ads","Less clutter","Lets apps use your Windows advertising ID. Off reduces ad personalization; it does not raise FPS.","Privacy preference","Windows advertising ID preference",Risk.Review,ActionKind.Registry,@"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo|Enabled","1"),
            new("windows-tips","Windows tips","Less clutter","Shows Windows tips while using your PC. Off hides these suggestions after signing in again.","Fewer prompts","Windows content delivery preference",Risk.Low,ActionKind.Registry,content+"|SoftLandingEnabled","1",Restart:true),
            new("start-suggestions","Start app suggestions","Less clutter","Suggests apps in Start on supported Windows versions. Off hides suggestions; installed apps stay.","Less Start clutter","Windows content delivery preference",Risk.Low,ActionKind.Registry,content+"|SubscribedContent-338388Enabled","1",Restart:true),
            new("settings-suggestions","Settings suggestions","Less clutter","Shows suggested content in Windows Settings. Off hides that content on supported Windows versions.","Less suggested content","Windows content delivery preference",Risk.Low,ActionKind.Registry,content+"|SubscribedContent-338393Enabled","1",Restart:true),
            new("lock-screen-tips","Lock screen tips","Less clutter","Shows facts and tips on supported picture lock screens. Off hides tips; Spotlight may manage its own content.","Less lock screen clutter","Windows content delivery preference",Risk.Low,ActionKind.Registry,content+"|SubscribedContent-338389Enabled","1",Restart:true)
        ]);
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
        ops.AddRange(PerformanceTweaks.Operations);
        Settings("update-downloads","Update download limits","Network and Connectivity","ms-settings:delivery-optimization-advanced","Use Windows' own download limits, including on Home editions. Lower limits leave more room for play but make updates slower.");
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
