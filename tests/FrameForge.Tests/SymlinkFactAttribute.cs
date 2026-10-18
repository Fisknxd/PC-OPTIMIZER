using Xunit;
namespace FrameForge.Tests;
public sealed class SymlinkFactAttribute : FactAttribute
{
    public SymlinkFactAttribute()
    {
        if(!OperatingSystem.IsWindows()) return;
        var path=Path.Combine(Path.GetTempPath(),"ff-link-probe-"+Guid.NewGuid().ToString("N"));
        try { File.CreateSymbolicLink(path,path+"-target"); }
        catch(IOException e) when((e.HResult&0xFFFF)==1314) { Skip="Windows symlink privilege unavailable. Junction protection is tested separately."; }
        finally { if(File.Exists(path)||new FileInfo(path).LinkTarget!=null) File.Delete(path); }
    }
}
