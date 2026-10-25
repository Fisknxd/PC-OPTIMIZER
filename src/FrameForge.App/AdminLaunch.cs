using System.ComponentModel;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
namespace FrameForge.App;

internal static class AdminLaunch
{
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)]
    static extern SafeFileHandle CreateFileW(string path,uint access,uint share,IntPtr security,uint creation,uint flags,IntPtr template);
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)]
    static extern uint GetFinalPathNameByHandleW(SafeFileHandle handle,StringBuilder path,uint size,uint flags);
    // Package virtualization and redirected folders can expose aliases that the elevated shell
    // cannot resolve. Launch the actual file, still using the ordinary Windows approval prompt.
    static string PhysicalPath(string path,bool directory)
    {
        using var handle=CreateFileW(path,0x80,7,IntPtr.Zero,3,directory?0x02000000u:0u,IntPtr.Zero);
        if(handle.IsInvalid) return path;
        var buffer=new StringBuilder(32768); var count=GetFinalPathNameByHandleW(handle,buffer,(uint)buffer.Capacity,0);
        if(count==0||count>=buffer.Capacity) return path;
        var resolved=buffer.ToString();
        return resolved.StartsWith(@"\\?\UNC\",StringComparison.Ordinal)?@"\\"+resolved[8..]:resolved.StartsWith(@"\\?\",StringComparison.Ordinal)?resolved[4..]:resolved;
    }
    public static string? LastFailure {get;private set;}
    public static bool IsAdministrator
    {
        get { using var identity=WindowsIdentity.GetCurrent(); return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator); }
    }
    public static bool TryStart(string[] arguments)
    {
        using var identity=WindowsIdentity.GetCurrent();
        var expected=arguments.FirstOrDefault(x=>x.StartsWith("--requested-by=",StringComparison.Ordinal))?[15..];
        if(expected!=null&&expected!=identity.User?.Value) throw new UnauthorizedAccessException("Administrator access used a different Windows account. FrameForge stopped to avoid changing that person's settings. Start with your own administrator account, or cancel the prompt to use standard access.");
        if(IsAdministrator||arguments.Contains("--standard-user")||arguments.Contains("--as-admin")) return false;
        var executable=PhysicalPath(Environment.ProcessPath??throw new InvalidOperationException("Application path unavailable."),false);
        var start=new ProcessStartInfo(executable) {UseShellExecute=true,Verb="runas",WorkingDirectory=PhysicalPath(Environment.CurrentDirectory,true)};
        foreach(var argument in arguments) start.ArgumentList.Add(argument);
        start.ArgumentList.Add("--as-admin"); start.ArgumentList.Add("--requested-by="+identity.User?.Value);
        start.ArgumentList.Add("--wait-for-process="+Environment.ProcessId);
        try { Process.Start(start); return true; }
        catch(Win32Exception error) when(error.NativeErrorCode==1223) { LastFailure="Permission declined. FrameForge is using standard access."; return false; }
        catch(Win32Exception error) { LastFailure="Windows could not grant administrator access: "+error.Message; return false; }
    }
}
