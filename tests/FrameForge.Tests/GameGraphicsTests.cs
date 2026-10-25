using System.Text;
using FrameForge.Core;
using Xunit;
namespace FrameForge.Tests;
public sealed class GameGraphicsTests:IDisposable
{
    readonly string root=Path.Combine(Path.GetTempPath(),"ff-game-config-"+Guid.NewGuid().ToString("N"));
    JsonStore Store()=>new(root);
    public void Dispose(){if(Directory.Exists(root))Directory.Delete(root,true);}
    static string Fixture(string game)=>game=="Fortnite"?"; untouched comment\r\n[/Script/FortniteGame.FortGameUserSettings]\r\nbUseVSync=True\r\nbMotionBlur=True\r\nbUseNanite=True\r\nbRayTracing=True\r\n[ScalabilityGroups]\r\nsg.ResolutionQuality=100\r\nsg.TextureQuality = 3 ; texture comment\r\nsg.ShadowQuality=3\r\nsg.LandscapeQuality=2\r\nsg.ShadingQuality=3\r\n[D3DRHIPreference]\r\nPreferredFeatureLevel=sm6\r\n[Unrelated]\r\nCustomValue=keep\r\n":"// untouched comment\ngraphics.renderscale \"1\"\nmesh.quality \"100\"\nterrain.quality \"100\"\ntree.quality \"100\"\ngraphicssettings.shadowqualitypreset \"3\"\ngraphics.shadowlights \"3\"\neffects.antialiasing \"2\"\neffects.ao \"True\"\ninput.sensitivity \"0.5\"\n";
    [Theory][InlineData("Fortnite","Potato")][InlineData("Fortnite","Sharper potato")][InlineData("Rust","Potato")][InlineData("Rust","Sharper potato")]
    public void RealConfigPatchChangesOwnedKeysAndRoundTripsExactly(string game,string mode)
    {
        var original=Encoding.UTF8.GetBytes(Fixture(game));var patch=GameGraphics.Build(game,mode,original);Assert.NotEmpty(patch.Changes);
        var changed=Encoding.UTF8.GetString(patch.Bytes);Assert.Contains(game=="Fortnite"?"CustomValue=keep":"input.sensitivity \"0.5\"",changed);Assert.Contains("untouched comment",changed);
        Assert.Empty(GameGraphics.Build(game,mode,patch.Bytes).Changes);Assert.Equal(original,GameGraphics.Restore(game,patch.Bytes,patch.Changes).Bytes);
        Assert.DoesNotContain(patch.Changes,x=>x.Key.Contains("input.")||x.Key.Contains("bind")||x.Key.Contains("anti-cheat"));
    }
    [Theory][InlineData(false)][InlineData(true)]
    public void ConfigEncodingAndPreambleSurvive(bool bigEndian)
    {
        var encoding=new UnicodeEncoding(bigEndian,true,true);var original=encoding.GetPreamble().Concat(encoding.GetBytes(Fixture("Fortnite"))).ToArray();
        var patch=GameGraphics.Build("Fortnite","Potato",original);Assert.Equal(encoding.GetPreamble(),patch.Bytes.Take(2));Assert.Equal(original,GameGraphics.Restore("Fortnite",patch.Bytes,patch.Changes).Bytes);
    }
    [Fact]public void Utf8BomSurvives()
    {
        var original=new byte[]{239,187,191}.Concat(Encoding.UTF8.GetBytes(Fixture("Rust"))).ToArray();var patch=GameGraphics.Build("Rust","Potato",original);Assert.Equal(original,GameGraphics.Restore("Rust",patch.Bytes,patch.Changes).Bytes);
    }
    [Fact]public void RemovedRustCommandsAreNeverInventedButExistingLegacyPotatoIsSupported()
    {
        var current=GameGraphics.Build("Rust","Potato",Encoding.UTF8.GetBytes(Fixture("Rust")));Assert.Contains("graphics.shaderlod",current.Skipped);Assert.DoesNotContain("graphics.shaderlod",Encoding.UTF8.GetString(current.Bytes));
        var old=GameGraphics.Build("Rust","Potato",Encoding.UTF8.GetBytes(Fixture("Rust")+"graphics.shaderlod \"600\"\n"));Assert.Contains("graphics.shaderlod \"100\"",Encoding.UTF8.GetString(old.Bytes));
    }
    [Fact]public void MissingRequiredDuplicateAndMalformedGraphicsValuesFailClosed()
    {
        Assert.Throws<InvalidDataException>(()=>GameGraphics.Build("Fortnite","Potato",Encoding.UTF8.GetBytes("[ScalabilityGroups]\nsg.TextureQuality=3\n")));
        Assert.Throws<InvalidDataException>(()=>GameGraphics.Build("Rust","Potato",Encoding.UTF8.GetBytes(Fixture("Rust")+"mesh.quality \"200\"\n")));
        Assert.Throws<InvalidDataException>(()=>GameGraphics.Build("Rust","Potato",Encoding.UTF8.GetBytes(Fixture("Rust").Replace("renderscale \"1\"","renderscale \"NaN\""))));
        Assert.Throws<InvalidOperationException>(()=>GameGraphics.Build("Rust","Unknown",Encoding.UTF8.GetBytes(Fixture("Rust"))));
    }
    sealed class Files(string game):IGameConfigFiles
    {
        public byte[] Content=Encoding.UTF8.GetBytes(Fixture(game));public bool Running,IgnoreWrites;public int Writes;public string Identity="file-1";public Action? BeforeWrite;
        public Task<GameConfigTarget?> Locate(string g,string? selectedPath=null)=>Task.FromResult<GameConfigTarget?>(new(g,"fixture-"+g));
        public Task<bool> IsRunning(string g)=>Task.FromResult(Running);
        public Task<GameConfigSnapshot> Read(GameConfigTarget target)=>Task.FromResult(new GameConfigSnapshot(Content.ToArray(),Identity));
        public Task Write(GameConfigTarget target,string identity,string hash,byte[] bytes){BeforeWrite?.Invoke();if(identity!=Identity||hash!=GameGraphics.Hash(Content))throw new IOException("Stale fixture");Writes++;if(!IgnoreWrites)Content=bytes.ToArray();return Task.CompletedTask;}
    }
    [Theory][InlineData("Fortnite")][InlineData("Rust")]
    public async Task ApplyJournalsBeforeWriteAndUndoRestoresOriginalBytes(string game)
    {
        var files=new Files(game);var original=files.Content.ToArray();var manager=new GameGraphicsManager(files,Store());files.BeforeWrite=()=>Assert.Contains(manager.History,x=>x.Status is "Prepared" or "Restoring");
        var result=await manager.Apply(await manager.Preview(game,"Potato"));Assert.Equal("Applied",result!.Status);Assert.NotEqual(original,files.Content);
        Assert.Equal("Restored",(await manager.Undo(result.Id)).Status);Assert.Equal(original,files.Content);
    }
    [Fact]public async Task RunningGameChangedContentsAndReplacedFilesAreNotOverwritten()
    {
        var files=new Files("Rust");var manager=new GameGraphicsManager(files,Store());var preview=await manager.Preview("Rust","Potato");files.Running=true;
        await Assert.ThrowsAsync<InvalidOperationException>(()=>manager.Apply(preview));files.Running=false;files.Identity="new-file";
        await Assert.ThrowsAsync<InvalidOperationException>(()=>manager.Apply(preview));files.Identity="file-1";files.Content=files.Content.Concat(Encoding.UTF8.GetBytes("// external edit\n")).ToArray();
        await Assert.ThrowsAsync<InvalidOperationException>(()=>manager.Apply(preview));Assert.Equal(0,files.Writes);Assert.Empty(manager.History);
    }
    [Fact]public async Task IgnoredWritesCannotClaimSuccessAndJournalFailurePreventsWrite()
    {
        var files=new Files("Rust"){IgnoreWrites=true};var store=Store();var manager=new GameGraphicsManager(files,store);
        var result=await manager.Apply(await manager.Preview("Rust","Potato"));Assert.Equal("Failed",result!.Status);Assert.Equal("Restored",(await manager.Undo(result.Id)).Status);
        Directory.CreateDirectory(Path.Combine(root,"game-graphics-history.json.tmp"));var writes=files.Writes;
        var preview=await manager.Preview("Rust","Potato");await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>manager.Apply(preview));Assert.Equal(writes,files.Writes);
    }
    [Fact]public async Task RestorePreservesUnrelatedGameEditsAndRefusesGraphicsConflicts()
    {
        var files=new Files("Rust");var manager=new GameGraphicsManager(files,Store());var result=await manager.Apply(await manager.Preview("Rust","Potato"));
        files.Content=Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(files.Content).Replace("input.sensitivity \"0.5\"","input.sensitivity \"0.7\""));
        Assert.Equal("Restored",(await manager.Undo(result!.Id)).Status);Assert.Contains("input.sensitivity \"0.7\"",Encoding.UTF8.GetString(files.Content));
        result=await manager.Apply(await manager.Preview("Rust","Potato"));files.Content=Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(files.Content).Replace("graphics.renderscale \"0.5\"","graphics.renderscale \"0.8\""));var writes=files.Writes;
        Assert.Equal("Restore failed",(await manager.Undo(result!.Id)).Status);Assert.Equal(writes,files.Writes);
    }
    [Fact]public async Task OlderGraphicsUndoCannotOverwriteNewerProfile()
    {
        var files=new Files("Fortnite");var manager=new GameGraphicsManager(files,Store());var first=await manager.Apply(await manager.Preview("Fortnite","Potato"));var second=await manager.Apply(await manager.Preview("Fortnite","Sharper potato"));
        Assert.Equal("Restore failed",(await manager.Undo(first!.Id)).Status);Assert.Equal("Restored",(await manager.Undo(second!.Id)).Status);Assert.Equal("Restored",(await manager.Undo(first.Id)).Status);
    }
    [Fact]public async Task AlteredBackupCannotWriteArbitrarySettings()
    {
        var files=new Files("Rust");var store=Store();var manager=new GameGraphicsManager(files,store);var record=await manager.Apply(await manager.Preview("Rust","Potato"));
        var history=manager.History;history[0]=history[0] with {BeforeBase64=Convert.ToBase64String(Encoding.UTF8.GetBytes(Fixture("Rust")+"input.bind evil\n"))};store.Save("game-graphics-history.json",history);var writes=files.Writes;
        Assert.Equal("Restore failed",(await manager.Undo(record!.Id)).Status);Assert.Equal(writes,files.Writes);
    }
}
