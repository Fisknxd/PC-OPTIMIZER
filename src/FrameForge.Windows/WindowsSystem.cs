using System.Diagnostics;
using System.Globalization;
using System.Management;
using System.Net.NetworkInformation;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using FrameForge.Core;

namespace FrameForge.Windows;

public static class Commands
{
    public static async Task<string> Run(string file,IEnumerable<string> args,int seconds=30)
    {
        var info=new ProcessStartInfo(file) { UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true };
        foreach(var arg in args) info.ArgumentList.Add(arg);
        using var p=Process.Start(info)??throw new IOException("Could not start Windows utility.");
        var output=p.StandardOutput.ReadToEndAsync(); var error=p.StandardError.ReadToEndAsync();
        using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(seconds));
        try { await p.WaitForExitAsync(timeout.Token); }
        catch(OperationCanceledException) { try { p.Kill(entireProcessTree:true); } catch(InvalidOperationException) { } throw new TimeoutException("Windows utility timed out; rescan before retrying."); }
        var text=await output; var err=await error;
        if(p.ExitCode!=0) throw new IOException($"{Path.GetFileName(file)} failed ({p.ExitCode}): {err.Trim()} {text.Trim()}");
        return text.Trim();
    }
    public static string SystemFile(string file)=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),file);
    public static Task<string> PowerShell(string fixedScript,int seconds=60)=>Run(SystemFile(@"WindowsPowerShell\v1.0\powershell.exe"),["-NoLogo","-NoProfile","-NonInteractive","-EncodedCommand",Convert.ToBase64String(Encoding.Unicode.GetBytes("$ErrorActionPreference='Stop'; [Console]::OutputEncoding=[Text.Encoding]::UTF8; "+fixedScript))],seconds);
}
public sealed partial class WindowsSettings : ISystemSettings
{
    public bool IsAdministrator { get { using var id=WindowsIdentity.GetCurrent(); return new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator); } }
    [GeneratedRegex(@"\b[0-9a-fA-F]{8}-(?:[0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}\b")]
    private static partial Regex GuidPattern();
    static void Validate(Optimization op)
    {
        if(!Catalog.All.Contains(op)) throw new InvalidOperationException("Unknown operation.");
        if(!OperatingSystem.IsWindowsVersionAtLeast(10,0,19045)) throw new PlatformNotSupportedException("Windows build 19045+ required.");
    }
    public async Task<SettingValue> Read(Optimization op)
    {
        Validate(op);
        if(op.Kind==ActionKind.PowerPlan)
        {
            var text=await Commands.Run(Commands.SystemFile("powercfg.exe"),["/getactivescheme"]);
            var match=GuidPattern().Match(text);
            if(!match.Success) throw new InvalidDataException("Active power scheme GUID unavailable.");
            return new(true,match.Value.ToLowerInvariant(),"Guid");
        }
        if(op.Kind!=ActionKind.Registry) throw new InvalidOperationException("Not a supported setting.");
        var parts=op.Target.Split('|'); using var key=Registry.CurrentUser.OpenSubKey(parts[0]);
        if(key==null||!key.GetValueNames().Contains(parts[1],StringComparer.OrdinalIgnoreCase)) return new(false,null);
        var kind=key.GetValueKind(parts[1]);
        return new(true,Convert.ToString(key.GetValue(parts[1]),CultureInfo.InvariantCulture),kind.ToString());
    }
    public async Task Write(Optimization op,SettingValue state)
    {
        Validate(op);
        if(op.Kind==ActionKind.PowerPlan)
        {
            if(!state.Exists||!Guid.TryParseExact(state.Value,"D",out var guid)) throw new InvalidDataException("Invalid power plan backup.");
            var list=await Commands.Run(Commands.SystemFile("powercfg.exe"),["/list"]);
            if(!GuidPattern().Matches(list).Any(x=>x.Value.Equals(guid.ToString(),StringComparison.OrdinalIgnoreCase))) throw new InvalidOperationException("Power plan no longer exists; no change made.");
            await Commands.Run(Commands.SystemFile("powercfg.exe"),["/setactive",guid.ToString()]); return;
        }
        if(op.Kind!=ActionKind.Registry) throw new InvalidOperationException("Not a supported setting.");
        var parts=op.Target.Split('|');
        if(!state.Exists)
        {
            using var existing=Registry.CurrentUser.OpenSubKey(parts[0],writable:true); existing?.DeleteValue(parts[1],throwOnMissingValue:false); return;
        }
        if(state.Kind!="DWord"||!int.TryParse(state.Value,out var value)||value is < 0 or > 2) throw new InvalidDataException("Invalid registry backup; refusing restore.");
        using var key=Registry.CurrentUser.CreateSubKey(parts[0],writable:true); key.SetValue(parts[1],value,RegistryValueKind.DWord); key.Flush();
    }
    public static void Open(Optimization op)
    {
        if(!Catalog.All.Contains(op)||op.Kind is not (ActionKind.Settings or ActionKind.Web)) throw new InvalidOperationException("Unknown shortcut.");
        Process.Start(new ProcessStartInfo(op.Target) { UseShellExecute=true });
    }
    public static void Utility(string name)
    {
        var map=new Dictionary<string,(string,string?)> {
            ["Task Manager"]=("Taskmgr.exe",null),["Resource Monitor"]=("resmon.exe",null),["System Information"]=("msinfo32.exe",null),
            ["DirectX Diagnostic"]=("dxdiag.exe",null),["Event Viewer"]=("mmc.exe","eventvwr.msc"),["Reliability Monitor"]=("perfmon.exe","/rel"),["Services (review only)"]=("mmc.exe","services.msc") };
        if(!map.TryGetValue(name,out var entry)) throw new InvalidOperationException("Unknown utility.");
        var p=new ProcessStartInfo(Commands.SystemFile(entry.Item1)) { UseShellExecute=true };
        if(entry.Item2!=null) p.ArgumentList.Add(entry.Item2); Process.Start(p);
    }
}
public static class Scanner
{
    static List<Dictionary<string,object?>> Query(string cls,string fields,List<string> warnings,string scope=@"root\cimv2",string? where=null)
    {
        try
        {
            using var search=new ManagementObjectSearcher(new ManagementScope(scope),new ObjectQuery($"SELECT {fields} FROM {cls}"+(where==null?"":" WHERE "+where)),new System.Management.EnumerationOptions { Timeout=TimeSpan.FromSeconds(8) });
            using var rows=search.Get(); var result=new List<Dictionary<string,object?>>();
            foreach(ManagementObject row in rows)
            {
                using(row) { var d=new Dictionary<string,object?>(); foreach(PropertyData p in row.Properties) d[p.Name]=p.Value; result.Add(d); }
            }
            return result;
        }
        catch(Exception e) when(e is ManagementException or System.Runtime.InteropServices.COMException or UnauthorizedAccessException) { warnings.Add($"{cls}: {e.Message}"); return []; }
    }
    static string Text(Dictionary<string,object?>? d,string k)=>d!=null&&d.TryGetValue(k,out var v)&&v!=null ? v is Array a ? string.Join(", ",a.Cast<object>()) : Convert.ToString(v,CultureInfo.InvariantCulture)??"Unavailable" : "Unavailable";
    static long? Num(Dictionary<string,object?>? d,string k)=>long.TryParse(Text(d,k),out var v)?v:null;
    public static async Task<HardwareSnapshot> Scan()
    {
        return await Task.Run(async ()=>
        {
            var w=new List<string>();
            var os=Query("Win32_OperatingSystem","Caption,Version,BuildNumber,ProductType,TotalVisibleMemorySize,FreePhysicalMemory,LastBootUpTime",w).FirstOrDefault();
            var cpu=Query("Win32_Processor","Name,Manufacturer,NumberOfCores,NumberOfLogicalProcessors,MaxClockSpeed,CurrentClockSpeed,Architecture,L3CacheSize",w);
            var c=cpu.FirstOrDefault();
            var gpu=Query("Win32_VideoController","Name,AdapterCompatibility,DriverVersion,CurrentHorizontalResolution,CurrentVerticalResolution,CurrentRefreshRate",w);
            var mem=Query("Win32_PhysicalMemory","Capacity,ConfiguredClockSpeed,Manufacturer,DeviceLocator",w);
            var system=Query("Win32_ComputerSystem","PCSystemType,TotalPhysicalMemory",w).FirstOrDefault();
            var battery=Query("Win32_Battery","Name",w);
            var board=Query("Win32_BaseBoard","Manufacturer,Product",w).FirstOrDefault();
            var bios=Query("Win32_BIOS","SMBIOSBIOSVersion,Manufacturer",w).FirstOrDefault();
            var utilization=Query("Win32_PerfFormattedData_PerfOS_Processor","PercentProcessorTime",w,where:"Name='_Total'").FirstOrDefault();
            var disks=Query("MSFT_PhysicalDisk","FriendlyName,BusType,MediaType,HealthStatus,Size",w,@"root\Microsoft\Windows\Storage");
            var startup=Query("Win32_StartupCommand","Name,Location,User",w);
            var network=Query("Win32_NetworkAdapterConfiguration","Description,DHCPEnabled,DNSServerSearchOrder,IPAddress",w,where:"IPEnabled=True");
            var security=Query("AntiVirusProduct","displayName,productState",w,@"root\SecurityCenter2");
            string power;
            try { power=await Commands.Run(Commands.SystemFile("powercfg.exe"),["/getactivescheme"]); } catch(Exception e) { power="Unavailable"; w.Add(e.Message); }
            var procs=new List<string>(); int? count=null;
            try
            {
                var list=Process.GetProcesses(); count=list.Length;
                try
                {
                    var top=new List<(string,long)>();
                    foreach(var p in list) { try { top.Add(($"{p.ProcessName} (PID {p.Id})",p.WorkingSet64)); } catch(Exception e) when(e is System.ComponentModel.Win32Exception or InvalidOperationException) { } }
                    procs=top.OrderByDescending(x=>x.Item2).Take(15).Select(x=>$"{x.Item1} — working set {x.Item2/1048576d:F0} MiB").ToList();
                }
                finally { foreach(var p in list) p.Dispose(); }
            }
            catch(Exception e) { w.Add("Processes: "+e.Message); }
            double? uptime=null;
            try { if(Text(os,"LastBootUpTime")!="Unavailable") uptime=(DateTime.Now-ManagementDateTimeConverter.ToDateTime(Text(os,"LastBootUpTime"))).TotalHours; } catch(ArgumentOutOfRangeException) { }
            var volumes=new List<string>(); var storageVolumes=new List<StorageVolume>();
            foreach(var d in DriveInfo.GetDrives())
            {
                try { if(d.IsReady&&d.DriveType==DriveType.Fixed) { volumes.Add($"{d.Name} • {d.AvailableFreeSpace/1073741824d:F1} GiB free / {d.TotalSize/1073741824d:F1} GiB"); storageVolumes.Add(new(d.Name,d.TotalSize,d.AvailableFreeSpace)); } } catch(IOException e) { w.Add(e.Message); }
            }
            var build=(int)(Num(os,"BuildNumber")??0);
            return new HardwareSnapshot {
                WindowsSupported=OperatingSystem.IsWindowsVersionAtLeast(10,0,19045)&&Num(os,"ProductType")==1,WindowsBuild=build,Os=$"{Text(os,"Caption")} • {Text(os,"Version")} • build {build}",
                Cpu=Text(c,"Name"),CpuManufacturer=Text(c,"Manufacturer"),Cores=cpu.Count==0?null:(int)cpu.Sum(x=>Num(x,"NumberOfCores")??0),Threads=cpu.Count==0?null:(int)cpu.Sum(x=>Num(x,"NumberOfLogicalProcessors")??0),
                ReportedMhz=(int?)Num(c,"CurrentClockSpeed"),MaxMhz=(int?)Num(c,"MaxClockSpeed"),Architecture=System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString(),L3CacheKb=Num(c,"L3CacheSize"),
                Gpus=gpu.Select(g=>new Gpu(Text(g,"Name"),HardwareRules.IdentifyVendor(Text(g,"Name")+" "+Text(g,"AdapterCompatibility")),Text(g,"DriverVersion"))).ToList(),
                TotalMemory=Num(system,"TotalPhysicalMemory")??Num(os,"TotalVisibleMemorySize")*1024,AvailableMemory=Num(os,"FreePhysicalMemory")*1024,CpuPercent=Num(utilization,"PercentProcessorTime"),
                Laptop=battery.Count>0||Num(system,"PCSystemType")==2 ? true : Num(system,"PCSystemType") is >0 ? false:null,
                Board=$"{Text(board,"Manufacturer")} {Text(board,"Product")}",Bios=$"{Text(bios,"Manufacturer")} {Text(bios,"SMBIOSBIOSVersion")}",PowerPlan=power,
                MemoryModules=mem.Select(m=>$"{Text(m,"DeviceLocator")} • {(Num(m,"Capacity") is {} cap ? $"{cap/1073741824d:F1} GiB":"Unavailable")} • configured speed {Text(m,"ConfiguredClockSpeed")} MHz (firmware reported)").ToList(),
                Displays=gpu.Select(g=>$"{Text(g,"Name")} • reported {Text(g,"CurrentHorizontalResolution")} × {Text(g,"CurrentVerticalResolution")} • {Text(g,"CurrentRefreshRate")} Hz (WMI; per-monitor features unavailable)").ToList(),
                Disks=disks.Select(d=>$"{Text(d,"FriendlyName")} • bus {Bus(Num(d,"BusType"))} • media {Media(Num(d,"MediaType"))} • health {Health(Num(d,"HealthStatus"))} • {(Num(d,"Size") is {} size ? $"{size/1073741824d:F1} GiB" : "Unavailable")}").ToList(),
                Volumes=volumes,StorageVolumes=storageVolumes,Startup=startup.Select(s=>$"{Text(s,"Name")} • {Text(s,"Location")} • {Text(s,"User")}").ToList(),
                Network=network.Select(n=>$"{Text(n,"Description")} • IP {Text(n,"IPAddress")} • DNS {Text(n,"DNSServerSearchOrder")} • DHCP {Text(n,"DHCPEnabled")}").Concat(NetworkInterface.GetAllNetworkInterfaces().Where(n=>n.OperationalStatus==OperationalStatus.Up).Select(n=>$"{n.Name} • {n.NetworkInterfaceType} • reported link {n.Speed/1000000} Mbps (not throughput)")).ToList(),
                Processes=procs,ProcessCount=count,UptimeHours=uptime,Security=security.Select(s=>$"{Text(s,"displayName")} • raw provider state {Text(s,"productState")} (not a security audit)").ToList(),Warnings=w
            };
        });
    }

    static string Bus(long? n)=>n switch {17=>"NVMe",11=>"SATA",7=>"USB",_=>"Unavailable / "+n};
    static string Media(long? n)=>n switch {3=>"HDD",4=>"SSD",_=>"Unavailable"};
    static string Health(long? n)=>n switch {0=>"Healthy (provider)",1=>"Warning",2=>"Unhealthy",_=>"Unavailable"};
}
public static class Packages
{
    public static async Task<List<InstalledPackage>> Inventory()
    {
        var json=await Commands.PowerShell("@(Get-AppxPackage | Select-Object Name,PackageFullName,Publisher,IsFramework,IsResourcePackage,NonRemovable,@{n='DependencyNames';e={@($_.Dependencies | ForEach-Object { $_.Name })}}) | ConvertTo-Json -Depth 5 -Compress");
        if(string.IsNullOrWhiteSpace(json)) return [];
        using var doc=JsonDocument.Parse(json);
        var rows=doc.RootElement.ValueKind==JsonValueKind.Array?doc.RootElement.EnumerateArray().ToArray():[doc.RootElement];
        return rows.Select(x=>new InstalledPackage(S(x,"Name"),S(x,"PackageFullName"),S(x,"Publisher"),B(x,"IsFramework"),B(x,"IsResourcePackage"),B(x,"NonRemovable"),x.TryGetProperty("DependencyNames",out var deps)&&deps.ValueKind==JsonValueKind.Array?deps.EnumerateArray().Select(d=>d.GetString()??"").ToArray():[])).ToList();
    }
    static string S(JsonElement e,string n)=>e.TryGetProperty(n,out var v)?v.GetString()??"":"";
    static bool B(JsonElement e,string n)=>!e.TryGetProperty(n,out var v)||v.ValueKind!=JsonValueKind.False;
    public static async Task Remove(InstalledPackage approved)
    {
        var all=await Inventory();
        var current=all.SingleOrDefault(p=>p.FullName==approved.FullName)??throw new InvalidOperationException("Package changed since preview. Rescan.");
        if(!DebloatPolicy.CanRemove(current,all)) throw new InvalidOperationException("Protected package or dependency; removal blocked.");
        if(!Regex.IsMatch(current.FullName,@"\A[A-Za-z0-9.\-_]+\z")) throw new InvalidDataException("Unexpected package identity.");
        await Commands.PowerShell($"Remove-AppxPackage -Package '{current.FullName}' -Confirm:$false");
        if((await Inventory()).Any(p=>p.FullName==current.FullName)) throw new IOException("Uninstall could not be verified. Inspect Windows app status.");
    }
}
public static class Diagnostics
{
    public static async Task<string> Ping(string host)
    {
        if(Uri.CheckHostName(host)==UriHostNameType.Unknown||host.Length>253) throw new ArgumentException("Enter a hostname or IP, without a URL or port.");
        var samples=new List<double>(); int lost=0;
        for(int i=0;i<10;i++)
        {
            using var ping=new System.Net.NetworkInformation.Ping();
            try { var r=await ping.SendPingAsync(host,1000); if(r.Status==IPStatus.Success) samples.Add(r.RoundtripTime); else lost++; }
            catch(PingException) { lost++; }
            await Task.Delay(100);
        }
        return samples.Count==0?"No ICMP replies. This may be filtering; it does not prove the connection is down.":
            $"{host}: {samples.Count}/10 replies • loss {lost*10}% • mean {samples.Average():F1} ms • min {samples.Min():F1} / max {samples.Max():F1} ms • mean successive RTT difference {(samples.Count>1?samples.Zip(samples.Skip(1),(a,b)=>Math.Abs(a-b)).Average():0):F1} ms.\nICMP results for this host do not establish game-server latency or bandwidth. Jitter excludes missing replies.";
    }
    public static string DetectGames()
    {
        var found=new List<string>();
        var local=Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if(Directory.Exists(Path.Combine(local,"FortniteGame"))) found.Add("Fortnite user configuration directory detected (does not prove installation).");
        if(Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),".minecraft"))) found.Add("Minecraft user directory detected (does not prove installation).");
        using var steam=Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
        if(steam?.GetValue("SteamPath") is string root)
        {
            var libraries=new List<string>{root};
            var vdf=Path.Combine(root,"steamapps","libraryfolders.vdf");
            if(File.Exists(vdf)) libraries.AddRange(Regex.Matches(File.ReadAllText(vdf),"\"path\"\\s+\"([^\"]+)\"").Select(m=>m.Groups[1].Value.Replace("\\\\","\\")));
            var games=new Dictionary<string,string>{{"252490","Rust"},{"730","Counter-Strike 2"},{"1172470","Apex Legends"},{"1938090","Call of Duty"}};
            foreach(var lib in libraries.Distinct()) foreach(var g in games)
                if(File.Exists(Path.Combine(lib,"steamapps",$"appmanifest_{g.Key}.acf"))) found.Add(g.Value+" — Steam manifest detected (files not verified).");
        }
        return found.Count==0?"No supported local markers found. Custom/other launchers and Valorant detection are unavailable; select a profile manually.":string.Join("\n",found);
    }
}
