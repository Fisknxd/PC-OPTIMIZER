using System.Globalization;
using System.Runtime.InteropServices;
using FrameForge.Core;
using Microsoft.Win32;
namespace FrameForge.Windows;
internal static class MachinePreferences
{
    [DllImport("kernel32.dll")][return:MarshalAs(UnmanagedType.Bool)]static extern bool GetProductInfo(int major,int minor,int servicePackMajor,int servicePackMinor,out int product);
    static void Validate(Optimization op)
    {
        if(!Catalog.All.Contains(op)||op.Kind!=ActionKind.MachineRegistry||!FeatureToggles.Supports(op))throw new InvalidOperationException("Unknown machine policy.");
        if(!OperatingSystem.IsWindowsVersionAtLeast(10,0,op.MinBuild))throw new PlatformNotSupportedException("Requires Windows build "+op.MinBuild+" or later.");
        if(!GetProductInfo(10,0,0,0,out var product)||!new[]{4,27,48,49,121,122,125,126,161,162,164,165,188,191}.Contains(product))throw new PlatformNotSupportedException("This Windows edition is not in the supported Pro/Enterprise/Education policy list.");
    }
    public static SettingValue Read(Optimization op)
    {
        Validate(op);var parts=op.Target.Split('|');using var machine=RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,RegistryView.Registry64);using var key=machine.OpenSubKey(parts[0]);
        if(key==null||!key.GetValueNames().Contains(parts[1],StringComparer.OrdinalIgnoreCase))return new(false,null);
        var state=new SettingValue(true,Convert.ToString(key.GetValue(parts[1]),CultureInfo.InvariantCulture),key.GetValueKind(parts[1]).ToString());_=FeatureToggles.Enabled(op,state);return state;
    }
    public static void Write(Optimization op,SettingValue state)
    {
        Validate(op);if(!new WindowsSettings().IsAdministrator)throw new UnauthorizedAccessException("Administrator access is needed for machine policies.");
        if(state.Kind!="DWord"||state.Exists&&!TweakDefinitions.Get(op).Allows(state.Value))throw new InvalidDataException("Invalid machine policy backup.");
        var parts=op.Target.Split('|');using var machine=RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,RegistryView.Registry64);
        if(!state.Exists){using var existing=machine.OpenSubKey(parts[0],true);existing?.DeleteValue(parts[1],false);existing?.Flush();return;}
        using var key=machine.CreateSubKey(parts[0],true);key.SetValue(parts[1],int.Parse(state.Value!,CultureInfo.InvariantCulture),RegistryValueKind.DWord);key.Flush();
    }
}
