using System.ComponentModel;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using FrameForge.Core;
namespace FrameForge.Windows;

internal static class WindowsPowerSettings
{
    [DllImport("powrprof.dll")]static extern uint PowerGetActiveScheme(nint root,out nint scheme);
    [DllImport("powrprof.dll")]static extern uint PowerReadACValueIndex(nint root,ref Guid scheme,ref Guid subgroup,ref Guid setting,out uint value);
    [DllImport("powrprof.dll")]static extern uint PowerReadDCValueIndex(nint root,ref Guid scheme,ref Guid subgroup,ref Guid setting,out uint value);
    [DllImport("powrprof.dll")]static extern uint PowerWriteACValueIndex(nint root,ref Guid scheme,ref Guid subgroup,ref Guid setting,uint value);
    [DllImport("powrprof.dll")]static extern uint PowerSetActiveScheme(nint root,ref Guid scheme);
    [DllImport("kernel32.dll")]static extern nint LocalFree(nint memory);
    static void Check(uint error)
    {
        if(error!=0)throw new Win32Exception((int)error,"Windows could not read or update this power setting. It may be unavailable or managed by your PC maker. ("+error+")");
    }
    static Guid Active()
    {
        Check(PowerGetActiveScheme(0,out var pointer));
        try{return Marshal.PtrToStructure<Guid>(pointer);}finally{LocalFree(pointer);}
    }
    public static SettingValue Read(Optimization op)
    {
        var definition=PowerSettings.Get(op);
        if(op.Id=="wifi-ac-power"&&!NetworkInterface.GetAllNetworkInterfaces().Any(x=>x.NetworkInterfaceType==NetworkInterfaceType.Wireless80211))throw new PlatformNotSupportedException("No Wi-Fi adapter found. Wired connections do not use this setting.");
        var scheme=Active();var subgroup=definition.Subgroup;var setting=definition.Setting;
        Check(PowerReadACValueIndex(0,ref scheme,ref subgroup,ref setting,out var ac));Check(PowerReadDCValueIndex(0,ref scheme,ref subgroup,ref setting,out var dc));
        if(Active()!=scheme)throw new InvalidOperationException("Power plan changed while reading. Refresh and try again.");
        var packed=PowerSettings.Pack(new(scheme,ac,dc));_=PowerSettings.Unpack(op,packed);return packed;
    }
    public static void Write(Optimization op,SettingValue target,ActivityFeed? activity)
    {
        var definition=PowerSettings.Get(op);var state=PowerSettings.Unpack(op,target);var current=PowerSettings.Unpack(op,Read(op));
        if(state.Scheme!=current.Scheme||state.Battery!=current.Battery)throw new InvalidOperationException("The active plan or battery setting changed. No power setting was overwritten.");
        var scheme=state.Scheme;var subgroup=definition.Subgroup;var setting=definition.Setting;
        if(Active()!=scheme)throw new InvalidOperationException("The power plan changed. Refresh first.");
        activity?.Report("PowerWriteACValueIndex: plan "+scheme+" · "+op.Name+" → "+state.PluggedIn+". Battery value retained: "+state.Battery);
        Check(PowerWriteACValueIndex(0,ref scheme,ref subgroup,ref setting,state.PluggedIn));
        if(Active()!=scheme)throw new InvalidOperationException("The active plan changed during the write. The saved plan value remains in Recovery; return to that plan before undoing.");
        Check(PowerSetActiveScheme(0,ref scheme));
        if(Read(op)!=target)throw new IOException("Power setting readback did not match. Original saved in Recovery.");
    }
}
