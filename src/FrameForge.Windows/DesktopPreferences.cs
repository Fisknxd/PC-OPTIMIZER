using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using FrameForge.Core;

namespace FrameForge.Windows;
internal static class DesktopPreferences
{
    [DllImport("user32.dll",EntryPoint="SystemParametersInfoW",SetLastError=true)]
    [return:MarshalAs(UnmanagedType.Bool)] static extern bool Get(uint action,uint parameter,out uint value,uint flags);
    [DllImport("user32.dll",EntryPoint="SystemParametersInfoW",SetLastError=true)]
    [return:MarshalAs(UnmanagedType.Bool)] static extern bool Mouse(uint action,uint parameter,[In,Out] int[] values,uint flags);
    [DllImport("user32.dll",EntryPoint="SystemParametersInfoW",SetLastError=true)]
    [return:MarshalAs(UnmanagedType.Bool)] static extern bool Set(uint action,uint parameter,IntPtr value,uint flags);
    static void Check(bool success) { if(!success) throw new Win32Exception(Marshal.GetLastWin32Error()); }
    public static SettingValue Read(Optimization operation)
    {
        if(operation.Id=="mouse-acceleration")
        {
            var values=new int[3]; Check(Mouse(0x0003,0,values,0)); return new(true,string.Join(",",values),"Mouse");
        }
        var action=operation.Id switch {"menu-animation"=>0x1002u,"app-animation"=>0x1042u,"quick-menus"=>0x006Au,_=>throw new InvalidOperationException("Unknown desktop setting.")};
        Check(Get(action,0,out var value,0)); return new(true,value.ToString(CultureInfo.InvariantCulture),operation.Id=="quick-menus"?"Milliseconds":"Boolean");
    }
    public static void Write(Optimization operation,SettingValue state)
    {
        if(!state.Exists) throw new InvalidDataException("A desktop setting needs a saved value.");
        _=FeatureToggles.Enabled(operation,state);
        if(operation.Id=="mouse-acceleration") { Check(Mouse(0x0004,0,state.Value!.Split(',').Select(int.Parse).ToArray(),3)); return; }
        var value=uint.Parse(state.Value!,CultureInfo.InvariantCulture);
        if(operation.Id=="quick-menus") Check(Set(0x006B,value,IntPtr.Zero,3));
        else Check(Set(operation.Id=="menu-animation"?0x1003u:0x1043u,0,new IntPtr(value),3));
    }
}
