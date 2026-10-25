using System.Diagnostics;
using FrameForge.Core;
using Xunit;
namespace FrameForge.Tests;

public sealed class ProcessRunnerTests
{
    static (string File,string[] Arguments) Shell(string windows,string unix)=>OperatingSystem.IsWindows()?
        (Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"cmd.exe"),["/d","/c",windows]):("/bin/sh",["-c",unix]);
    static Task<string> Run(string windows,string unix,TimeSpan? timeout=null,ActivityFeed? activity=null,int limit=4*1024*1024)
    {
        var command=Shell(windows,unix);return ProcessRunner.Run(command.File,command.Arguments,timeout??TimeSpan.FromSeconds(10),activity,limit);
    }
    [Fact]public async Task CapturesCompleteOutputAndReportsStderrSeparately()
    {
        var activity=new ActivityFeed();var result=await Run("echo first& echo second& echo warning 1>&2","printf 'first\nsecond\n'; printf 'warning\n' >&2",activity:activity);
        Assert.Equal(new[]{"first","second"},result.Split('\n').Select(x=>x.Trim()).ToArray());
        Assert.Contains(activity.Lines,x=>x.Message.TrimEnd()=="stderr: warning");
    }
    [Fact]public async Task NonzeroExitIncludesTheCommandFailure()
    {
        var error=await Assert.ThrowsAsync<IOException>(()=>Run("echo refused 1>&2& exit /b 7","printf 'refused\n' >&2; exit 7"));
        Assert.Contains("(7)",error.Message);Assert.Contains("refused",error.Message);
    }
    [Fact]public async Task TimeoutIncludesInheritedPipesAfterParentExits()
    {
        // The short-lived fixture child only waits; it changes no user data/settings.
        var elapsed=Stopwatch.StartNew();
        await Assert.ThrowsAsync<TimeoutException>(()=>Run("start \"\" /b ping.exe -n 6 127.0.0.1 >nul & echo parent-finished","(sleep 5) & printf 'parent-finished\n'",TimeSpan.FromMilliseconds(400)));
        Assert.True(elapsed.Elapsed<TimeSpan.FromSeconds(4),"Pipe draining escaped the command timeout.");
    }
    [Fact]public async Task ExcessiveOutputFailsInsteadOfReturningTruncatedData()
    {
        var error=await Assert.ThrowsAsync<IOException>(()=>Run("for /l %i in (1,1,100) do @echo 123456789012345678901234567890","i=0; while [ $i -lt 100 ]; do printf '123456789012345678901234567890\n'; i=$((i+1)); done",limit:1000));
        Assert.Contains("output exceeded",error.Message);
    }
    [Fact]public async Task InvalidBoundsDoNotStartAProcess()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(()=>ProcessRunner.Run("does-not-exist",[],TimeSpan.Zero));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(()=>ProcessRunner.Run("does-not-exist",[],TimeSpan.FromSeconds(1),maximumOutputCharacters:0));
    }
}
