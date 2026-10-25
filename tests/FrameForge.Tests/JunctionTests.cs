using System.Diagnostics;
using FrameForge.Core;
using Xunit;
namespace FrameForge.Tests;
public sealed class JunctionTests
{
    [Fact] public void WindowsJunctionsCannotExposeOutsideCleanupFilesOrEnterAnUpdate()
    {
        if(!OperatingSystem.IsWindows()) return;
        var root=Path.Combine(Path.GetTempPath(),"ff-junction-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var outside=Path.Combine(root,"outside"); Directory.CreateDirectory(outside);
            var source=Path.Combine(root,"app"); Directory.CreateDirectory(source);
            File.WriteAllText(Path.Combine(source,"FrameForge.exe"),"test launcher"); File.WriteAllText(Path.Combine(source,"FrameForge.runtimeconfig.json"),"{}");
            var data=Path.Combine(outside,"old.tmp"); File.WriteAllText(data,"preserve"); File.SetLastWriteTimeUtc(data,DateTime.UtcNow.AddDays(-14));
            var info=new ProcessStartInfo(Path.Combine(Environment.SystemDirectory,"cmd.exe")) { UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true };
            foreach(var argument in new[]{"/c","mklink","/J",Path.Combine(source,"linked"),outside}) info.ArgumentList.Add(argument);
            using var process=Process.Start(info)!; process.WaitForExit();
            Assert.True(process.ExitCode==0,process.StandardError.ReadToEnd());
            Assert.Empty(new CleanupService(source).Preview(DateTime.UtcNow));
            Assert.Throws<IOException>(()=>FolderUpdater.Install(source,Path.Combine(root,"installed")));
            Assert.Equal("preserve",File.ReadAllText(data)); Assert.False(Directory.Exists(Path.Combine(root,"installed")));
        }
        finally
        {
            // Remove the junction itself before the target directory; never traverse it.
            var junction=Path.Combine(root,"app","linked");
            if(Directory.Exists(junction)) Directory.Delete(junction);
            Directory.Delete(root,true);
        }
    }
}
