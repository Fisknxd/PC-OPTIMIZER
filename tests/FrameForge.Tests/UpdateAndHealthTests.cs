using FrameForge.Core;
using Xunit;
namespace FrameForge.Tests;
public sealed class UpdateAndHealthTests:IDisposable
{
    readonly string root=Path.Combine(Path.GetTempPath(),"ff-update-tests-"+Guid.NewGuid().ToString("N"));
    public UpdateAndHealthTests()=>Directory.CreateDirectory(root);
    public void Dispose()=>Directory.Delete(root,true);
    string App(string name,string contents)
    {
        var p=Path.Combine(root,name);Directory.CreateDirectory(p);
        File.WriteAllText(Path.Combine(p,"FrameForge.exe"),contents);
        File.WriteAllText(Path.Combine(p,"FrameForge.runtimeconfig.json"),"{}");
        Directory.CreateDirectory(Path.Combine(p,"docs"));File.WriteAllText(Path.Combine(p,"docs","guide.txt"),contents);
        return p;
    }
    [Fact] public void FreshInstallCopiesTheWholeFolderAndKeepsSource()
    {
        var source=App("source","new");var target=Path.Combine(root,"target");var r=FolderUpdater.Install(source,target);
        Assert.Null(r.Backup);Assert.Equal("new",File.ReadAllText(Path.Combine(target,"docs","guide.txt")));Assert.True(File.Exists(Path.Combine(source,"FrameForge.exe")));
    }
    [Fact] public void UpdatePreservesAllOriginalFilesAndExternalHistory()
    {
        var source=App("source","new");var target=App("target","old");File.WriteAllText(Path.Combine(target,"user-notes.txt"),"preserve me");
        var history=Path.Combine(root,"history.json");File.WriteAllText(history,"keep history");
        var r=FolderUpdater.Install(source,target);Assert.NotNull(r.Backup);
        Assert.Equal("old",File.ReadAllText(Path.Combine(r.Backup!,"FrameForge.exe")));Assert.Equal("preserve me",File.ReadAllText(Path.Combine(r.Backup!,"user-notes.txt")));
        Assert.Equal("new",File.ReadAllText(Path.Combine(target,"FrameForge.exe")));Assert.Equal("keep history",File.ReadAllText(history));
    }
    [Fact] public void UpdatingSameFolderIsANoOp()
    {
        var source=App("app","same");var r=FolderUpdater.Install(source,source);Assert.True(r.AlreadyCurrent);Assert.Null(r.Backup);Assert.Single(Directory.GetDirectories(root));
    }
    [Fact] public void UnknownDestinationIsNeverOverwritten()
    {
        var source=App("source","new");var target=Path.Combine(root,"personal");Directory.CreateDirectory(target);File.WriteAllText(Path.Combine(target,"document.txt"),"keep");
        Assert.Throws<IOException>(()=>FolderUpdater.Install(source,target));Assert.Equal("keep",File.ReadAllText(Path.Combine(target,"document.txt")));Assert.False(File.Exists(Path.Combine(target,"FrameForge.exe")));
    }
    [Fact] public void SourceInsideDestinationIsRefused()
    {
        var target=App("target","old");var source=App("target/new","new");Assert.Throws<IOException>(()=>FolderUpdater.Install(source,target));Assert.Equal("old",File.ReadAllText(Path.Combine(target,"FrameForge.exe")));
    }
    [Fact] public void DestinationInsideSourceIsRefused()
    {
        var source=App("source","new");Assert.Throws<IOException>(()=>FolderUpdater.Install(source,Path.Combine(source,"nested")));
    }
    [Fact] public void IncompleteSourceCannotChangeExistingInstallation()
    {
        var source=Path.Combine(root,"empty");Directory.CreateDirectory(source);var target=App("target","old");
        Assert.Throws<InvalidDataException>(()=>FolderUpdater.Install(source,target));Assert.Equal("old",File.ReadAllText(Path.Combine(target,"FrameForge.exe")));
    }
    [Fact] public void SourceLinkFailureLeavesExistingInstallationIntact()
    {
        var source=App("source","new");var target=App("target","old");File.CreateSymbolicLink(Path.Combine(source,"linked.txt"),Path.Combine(target,"FrameForge.exe"));
        Assert.Throws<IOException>(()=>FolderUpdater.Install(source,target));Assert.Equal("old",File.ReadAllText(Path.Combine(target,"FrameForge.exe")));Assert.Empty(Directory.GetDirectories(root,".FrameForge-update-*"));
    }
    [Fact] public void SuccessiveUpdatesKeepDistinctBackups()
    {
        var source=App("source","new");var target=App("target","old");var a=FolderUpdater.Install(source,target);var b=FolderUpdater.Install(source,target);
        Assert.NotEqual(a.Backup,b.Backup);Assert.True(Directory.Exists(a.Backup));Assert.True(Directory.Exists(b.Backup));
    }
    [Fact] public void HealthFindingsRequireMeasuredEvidence()
    {
        Assert.Empty(HealthAdvisor.Analyze(new()));
        var findings=HealthAdvisor.Analyze(new(){WindowsSupported=true,TotalMemory=32L*1073741824,AvailableMemory=2L*1073741824,CpuPercent=92,StorageVolumes=[new("C:",1000,50)]});
        Assert.Equal(3,findings.Count);Assert.All(findings,f=>Assert.True(f.Attention));Assert.Contains(findings,f=>f.OperationId=="memory");Assert.Contains(findings,f=>f.OperationId=="storage");
    }
    [Fact] public void HealthyAndUnavailableSamplesDoNotGeneratePressureClaims()
    {
        Assert.Empty(HealthAdvisor.Analyze(new(){WindowsSupported=true,TotalMemory=32L*1073741824,AvailableMemory=20L*1073741824,CpuPercent=12,StorageVolumes=[new("C:",1000,500)]}));
        Assert.Empty(HealthAdvisor.Analyze(new(){WindowsSupported=true}));
    }
    [Fact] public void X3dAndLaptopGuidanceNeverRequestsSchedulerChanges()
    {
        var findings=HealthAdvisor.Analyze(new(){WindowsSupported=true,Cpu="Ryzen X3D",Laptop=true});Assert.Equal(2,findings.Count);Assert.All(findings,f=>Assert.Equal("power",f.OperationId));Assert.All(findings,f=>Assert.False(f.Attention));
    }
}
