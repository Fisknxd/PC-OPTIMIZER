using FrameForge.Core;
using Xunit;

namespace FrameForge.Tests;
public sealed class CoreTests : IDisposable
{
    readonly string root=Path.Combine(Path.GetTempPath(),"ff-tests-"+Guid.NewGuid().ToString("N"));
    static readonly HardwareSnapshot Desktop=new() {WindowsSupported=true,WindowsBuild=26100,Laptop=false,Gpus=[new("RTX",Vendor.Nvidia,"test")],TotalMemory=8L*1073741824};
    public CoreTests()=>Directory.CreateDirectory(root);
    public void Dispose()=>Directory.Delete(root,true);
    JsonStore Store()=>new(Path.Combine(root,"data"));
    static Optimization Op(string id="extensions")=>Catalog.All.Single(o=>o.Id==id);
    sealed class Fake : ISystemSettings
    {
        public bool IsAdministrator {get;set;}
        public SettingValue State=new(false,null);
        public bool FailWrite,IgnoreWrite,FailRestore;
        public Action? BeforeWrite;
        public int Writes;
        public Task<SettingValue> Read(Optimization o)=>Task.FromResult(State);
        public Task Write(Optimization o,SettingValue state)
        {
            BeforeWrite?.Invoke(); Writes++;
            if(FailWrite||FailRestore&&!state.Exists) throw new IOException("injected failure");
            if(!IgnoreWrite) State=state;
            return Task.CompletedTask;
        }
    }
    [Theory]
    [InlineData("NVIDIA GeForce RTX",Vendor.Nvidia)]
    [InlineData("AMD Radeon RX",Vendor.Amd)]
    [InlineData("Advanced Micro Devices, Inc.",Vendor.Amd)]
    [InlineData("Intel Arc",Vendor.Intel)]
    [InlineData("Microsoft Basic Display Adapter",Vendor.Unknown)]
    public void VendorDetectionDoesNotGuessUnknownHardware(string name,Vendor expected)=>Assert.Equal(expected,HardwareRules.IdentifyVendor(name));
    [Fact] public async Task FailedJournalSavePreventsAnySystemWrite()
    {
        var store=Store(); var fake=new Fake(); var engine=new OperationEngine(fake,store);
        var p=await engine.Preview(Op(),Desktop);
        Directory.CreateDirectory(Path.Combine(store.Root,"history.json.tmp"));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>engine.Apply(p,Desktop,"s")); Assert.Equal(0,fake.Writes);
    }
    [Fact] public async Task ApplyJournalsBeforeWriteAndRestoresAbsence()
    {
        var store=Store(); var fake=new Fake(); var engine=new OperationEngine(fake,store);
        fake.BeforeWrite=()=>Assert.Contains(engine.History,r=>r.Status=="Prepared"||r.Status=="Restoring");
        var record=await engine.Apply(await engine.Preview(Op(),Desktop),Desktop,"s");
        Assert.Equal("Applied",record!.Status); Assert.Equal("0",fake.State.Value);
        Assert.Equal("Restored",(await engine.Undo(record.Id)).Status); Assert.False(fake.State.Exists);
        Assert.Equal("Restored",new OperationEngine(fake,Store()).History.Single().Status);
    }
    [Fact] public async Task StalePreviewMakesNoWrite()
    {
        var fake=new Fake(); var engine=new OperationEngine(fake,Store()); var p=await engine.Preview(Op(),Desktop);
        fake.State=new(true,"1");
        await Assert.ThrowsAsync<InvalidOperationException>(()=>engine.Apply(p,Desktop,"s")); Assert.Equal(0,fake.Writes); Assert.Empty(engine.History);
    }
    [Fact] public async Task UnverifiedWriteFailsAndKeepsBackup()
    {
        var fake=new Fake {IgnoreWrite=true}; var engine=new OperationEngine(fake,Store());
        var record=await engine.Apply(await engine.Preview(Op(),Desktop),Desktop,"s");
        Assert.Equal("Failed",record!.Status); Assert.Contains("verification",record.Error); Assert.False(engine.History.Single().Before.Exists);
    }
    [Fact] public async Task FailedWriteIsRecoverableAndRetryRestoreWorks()
    {
        var fake=new Fake {FailWrite=true}; var engine=new OperationEngine(fake,Store());
        var record=await engine.Apply(await engine.Preview(Op(),Desktop),Desktop,"s"); Assert.Equal("Failed",record!.Status);
        fake.FailWrite=false; Assert.Equal("Restored",(await engine.Undo(record.Id)).Status);
    }
    [Fact] public async Task RestoreRefusesExternalChanges()
    {
        var fake=new Fake(); var engine=new OperationEngine(fake,Store());
        var r=await engine.Apply(await engine.Preview(Op(),Desktop),Desktop,"s"); fake.State=new(true,"2");
        Assert.Equal("Restore failed",(await engine.Undo(r!.Id)).Status); Assert.Equal("2",fake.State.Value);
    }
    [Fact] public async Task RestoreFailureCanBeRetried()
    {
        var fake=new Fake(); var engine=new OperationEngine(fake,Store()); var r=await engine.Apply(await engine.Preview(Op(),Desktop),Desktop,"s");
        fake.FailRestore=true; Assert.Equal("Restore failed",(await engine.Undo(r!.Id)).Status);
        fake.FailRestore=false; Assert.Equal("Restored",(await engine.Undo(r.Id)).Status);
    }
    [Fact] public async Task PreparedInterruptedRecordCanBeRestored()
    {
        var store=Store(); var fake=new Fake {State=new(true,"0")};
        store.Save("history.json",new[]{new ChangeRecord {Id="interrupted",OperationId="extensions",Before=new(false,null),After=fake.State}});
        var engine=new OperationEngine(fake,store); Assert.Equal("Restored",(await engine.Undo("interrupted")).Status);
    }
    [Fact] public async Task AlreadyAppliedProducesNoHistory()
    {
        var fake=new Fake {State=new(true,"0")}; var engine=new OperationEngine(fake,Store());
        Assert.Null(await engine.Apply(await engine.Preview(Op(),Desktop),Desktop,"s")); Assert.Empty(engine.History); Assert.Equal(0,fake.Writes);
    }
    [Fact] public async Task WrongRegistryTypeIsNotOverwritten()
    {
        var engine=new OperationEngine(new Fake {State=new(true,"0","String")},Store());
        await Assert.ThrowsAsync<InvalidOperationException>(()=>engine.Preview(Op(),Desktop));
    }
    [Fact] public async Task UnsupportedHostAndAdminRequirementsFailBeforeWrite()
    {
        var fake=new Fake(); var engine=new OperationEngine(fake,Store());
        await Assert.ThrowsAsync<InvalidOperationException>(()=>engine.Preview(Op(),new()));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>engine.Preview(Op() with {Admin=true},Desktop)); Assert.Equal(0,fake.Writes);
    }
    [Fact] public void VendorCompatibilityHandlesMultiGpuAndUnknown()
    {
        Assert.Null(Op("nvidia").Incompatibility(Desktop)); Assert.NotNull(Op("amd").Incompatibility(Desktop));
        Assert.Null(Op("amd").Incompatibility(Desktop with {Gpus=[new("AMD",Vendor.Amd,"x"),new("NV",Vendor.Nvidia,"x")]}));
        Assert.NotNull(Op("nvidia").Incompatibility(Desktop with {Gpus=[]}));
    }
    [Fact] public void LaptopsNeverGetAutomaticPowerExperiment()
    {
        Assert.NotNull(Op("balanced").Incompatibility(Desktop with {Laptop=true}));
        Assert.NotNull(Op("balanced").Incompatibility(Desktop with {Laptop=null})); Assert.Null(Op("balanced").Incompatibility(Desktop));
        Assert.All(Catalog.All.Where(o=>o.Smart),o=>Assert.True(o.Reversible&&o.Risk==Risk.Low));
    }
    [Theory] [InlineData("reflex","All","nvidia")] [InlineData("","NVIDIA","nvidia")] [InlineData("","Not applicable","amd")] [InlineData("registry","All","hidden")]
    public void SearchMatchesMetadataAndFilters(string query,string filter,string expected)=>Assert.Contains(Catalog.Search(query,filter,Desktop),o=>o.Id==expected);
    [Fact] public void RecommendationsAreHardwareAware()
    {
        Assert.Contains("16 GiB",Op("startup").Reason(Desktop)); Assert.Contains("X3D",Op("power").Reason(Desktop with {Cpu="Ryzen 9800X3D"}));
        Assert.Empty(Catalog.Search("","Applied",Desktop,new HashSet<string>())); Assert.Single(Catalog.Search("","Applied",Desktop,new HashSet<string>{"extensions"}));
    }
    [Fact] public void SettingsAndHistoryPersistAndCorruptionFailsClosed()
    {
        var s=Store(); s.Save("preferences.json",new Preferences("Advanced",true,"GPU")); Assert.Equal("Advanced",Store().Load("preferences.json",()=>new Preferences()).Mode);
        File.WriteAllText(Path.Combine(s.Root,"history.json"),"broken"); Assert.Throws<System.Text.Json.JsonException>(()=>new OperationEngine(new Fake(),s).History);
    }
    [Fact] public void BackupRetainsPriorValidDocument()
    {
        var s=Store(); s.Save("preferences.json",new Preferences("Safe")); s.Save("preferences.json",new Preferences("Balanced")); Assert.Contains("Safe",File.ReadAllText(Path.Combine(s.Root,"preferences.json.bak")));
    }
    static InstalledPackage P(string name,bool framework=false,string[]? dependencies=null)=>new(name,name+"_1.0.0_x64__publisher","test",framework,false,false,dependencies??[]);
    [Fact] public void ProtectedAndUnknownPackagesNeverRemovable()
    {
        foreach(var n in DebloatPolicy.ProtectedKeywords.Append("Unknown.Vendor")) { var p=P(n); Assert.False(DebloatPolicy.CanRemove(p,[p])); }
        var optional=P("Microsoft.BingNews"); Assert.True(DebloatPolicy.CanRemove(optional,[optional])); Assert.False(DebloatPolicy.CanRemove(optional with {NonRemovable=true},[optional]));
    }
    [Fact] public void FrameworkAndDependentPackagesProtected()
    {
        var p=P("Microsoft.BingNews"); Assert.False(DebloatPolicy.CanRemove(p with {Framework=true},[p])); Assert.False(DebloatPolicy.CanRemove(p,[p,P("Consumer",dependencies:[p.Name])]));
        Assert.Equal("User decision",DebloatPolicy.Classify(P("Microsoft.YourPhone"),[]));
    }
    CleanupService Cleaner()=>new(root);
    string Old(string name)
    {
        var p=Path.Combine(root,name); Directory.CreateDirectory(Path.GetDirectoryName(p)!); File.WriteAllText(p,"test data"); File.SetLastWriteTimeUtc(p,DateTime.UtcNow.AddDays(-10)); return p;
    }
    [Fact] public void CleanupExcludesRecentSensitiveAndOutsideFiles()
    {
        var cleaner=Cleaner(); var good=Old("old.tmp"); Old("shader-cache/file.bin"); Old("Documents/report.docx"); Old("chrome-cache/file"); Old("personal-photo.jpg"); Old("report.docx"); Old("archive.zip");
        File.WriteAllText(Path.Combine(root,"recent.tmp"),"recent");
        var list=cleaner.Preview(DateTime.UtcNow); Assert.Single(list); Assert.Equal(good,list[0].Path);
        Assert.False(cleaner.IsSafe(Path.Combine(root,"..","outside"),DateTime.UtcNow));
    }
    [Fact] public void CleanupRejectsLinksAndDoesNotTraverseSymlinkDirectories()
    {
        var file=Old("data.tmp"); var outside=Path.Combine(root,"..","external-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(outside);
        try
        {
            File.CreateSymbolicLink(Path.Combine(root,"link.tmp"),file); Directory.CreateSymbolicLink(Path.Combine(root,"linked-dir"),outside);
            var list=Cleaner().Preview(DateTime.UtcNow); Assert.Single(list); Assert.DoesNotContain(list,f=>f.Path.Contains("link"));
        }
        finally { Directory.Delete(outside,true); }
    }
    [Fact] public void CleanupDeletesOnlyApprovedUnchangedFiles()
    {
        var a=Old("a.tmp"); var b=Old("b.tmp"); var c=Cleaner(); var preview=c.Preview(DateTime.UtcNow);
        File.AppendAllText(b,"changed"); var result=c.Delete(preview,DateTime.UtcNow); Assert.Equal(1,result.Deleted); Assert.Equal(1,result.Skipped); Assert.False(File.Exists(a)); Assert.True(File.Exists(b));
    }
    [Fact] public void ForgedCleanupPreviewOutsideRootIsRefused()
    {
        var file=Path.GetTempFileName(); try { var r=Cleaner().Delete([new(file,0,DateTime.UtcNow.AddDays(-20))],DateTime.UtcNow); Assert.Equal(0,r.Deleted); Assert.True(File.Exists(file)); } finally { File.Delete(file); }
    }
    [Fact] public void BenchmarkUsesActualDurationsAndSlowestOnePercent()
    {
        var stats=Benchmark.Parse("FrameTimeMs\n"+string.Join("\n",Enumerable.Repeat("10",99).Append("100")));
        Assert.Equal(100,stats.Frames); Assert.Equal(1000/10.9,stats.AverageFps,5); Assert.Equal(10,stats.LowOnePercentFps); Assert.Equal(100,stats.P99FrameMs);
    }
    [Theory] [InlineData("NaN")] [InlineData("0")] [InlineData("-1")] [InlineData("Infinity")]
    public void BenchmarkRejectsInvalidMeasurements(string value)=>Assert.Throws<InvalidDataException>(()=>Benchmark.Parse("FrameTimeMs\n"+string.Join("\n",Enumerable.Repeat(value,100))));
    [Fact] public void BenchmarkRejectsInsufficientSamples()=>Assert.Throws<InvalidDataException>(()=>Benchmark.Parse("FrameTimeMs\n10"));
    [Fact] public void GameGuidanceNeverPretendsToEditFiles()
    {
        foreach(var game in Games.Names) { var text=Games.Guidance(game,Desktop); Assert.Contains("Unavailable",text); Assert.Contains("NVIDIA",text); }
    }
}
