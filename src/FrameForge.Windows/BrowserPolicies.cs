using System.Diagnostics;
using FrameForge.Core;
using Microsoft.Win32;
namespace FrameForge.Windows;

public static class BrowserPolicies
{
    public static bool Supports(Optimization op)=>Catalog.All.Contains(op)&&(op.Id.StartsWith("chrome-",StringComparison.Ordinal)||op.Id is "edge-background" or "edge-sleep" or "edge-sleep-sooner");
    static bool IsChrome(Optimization op)=>op.Id.StartsWith("chrome-",StringComparison.Ordinal);
    static string FindExecutable(Optimization op)
    {
        if(!Supports(op))throw new InvalidOperationException("Unknown browser setting.");
        var chrome=IsChrome(op);var file=chrome?"chrome.exe":"msedge.exe";var folder=chrome?@"Google\Chrome\Application\chrome.exe":@"Microsoft\Edge\Application\msedge.exe";
        var candidates=new List<string>();
        foreach(var hive in new[]{RegistryHive.CurrentUser,RegistryHive.LocalMachine})foreach(var view in new[]{RegistryView.Registry64,RegistryView.Registry32})
        {
            using var root=RegistryKey.OpenBaseKey(hive,view);using var key=root.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\"+file);
            if(key?.GetValue("") is string value)candidates.Add(value.Trim('"'));
        }
        foreach(var root in new[]{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)})candidates.Add(Path.Combine(root,folder));
        return candidates.FirstOrDefault(File.Exists)??throw new PlatformNotSupportedException((chrome?"Chrome":"Edge")+" was not found. No setting was created for a missing browser.");
    }
    public static void Validate(Optimization op)
    {
        if(!Supports(op))return;
        var minimum=op.Id switch {"chrome-memory-strong"=>126,"chrome-memory"=>108,"chrome-background"=>19,"edge-background"=>88,_=>88};
        var path=FindExecutable(op);var version=FileVersionInfo.GetVersionInfo(path);
        if(version.FileMajorPart<minimum)throw new PlatformNotSupportedException("This option needs "+(IsChrome(op)?"Chrome ":"Edge ")+minimum+" or later.");
        var parts=op.Target.Split('|');var members=op.Kind==ActionKind.RegistryGroup?TweakDefinitions.Get(op).Members!:[new RegistryMember(parts[0],parts[1])];
        using var machine=RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,RegistryView.Registry64);
        foreach(var member in members)
        {
            using var key=machine.OpenSubKey(member.Path);
            if(key?.GetValueNames().Contains(member.Name,StringComparer.OrdinalIgnoreCase)==true)throw new InvalidOperationException("A machine policy already controls this browser option. Review it in the browser; FrameForge will not override it.");
        }
    }
    public static void OpenPolicies(Optimization op)
    {
        var path=FindExecutable(op);var start=new ProcessStartInfo(path){UseShellExecute=true};start.ArgumentList.Add(IsChrome(op)?"chrome://policy":"edge://policy");Process.Start(start);
    }
}
