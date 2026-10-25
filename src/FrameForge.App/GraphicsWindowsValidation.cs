using System.IO;
using System.Text;
using FrameForge.Core;
using FrameForge.Windows;
namespace FrameForge.App;
public sealed partial class MainWindow
{
    internal async Task ValidateNativeGraphicsFiles(string output)
    {
        var native=new WindowsGameConfigs();var report=new List<string>();
        foreach(var game in new[]{"Fortnite","Rust"})
        {
            var target=await native.Locate(game);if(target==null){report.Add(game+": no installed config found.");continue;}
            var before=await native.Read(target);var patch=GameGraphics.Build(game,"Potato",before.Bytes);var after=await native.Read(target);
            if(GameGraphics.Hash(before.Bytes)!=GameGraphics.Hash(after.Bytes))throw new InvalidDataException("Read-only game config changed during validation.");
            report.Add($"{game}: existing config parsed; {patch.Changes.Count} changes previewed, {patch.Skipped.Count} unsupported/missing fields skipped. Installed file unchanged; SHA256 {GameGraphics.Hash(before.Bytes)}.");
        }
        // Exercise the real Windows file backend against disposable config data, never the installed game.
        var folder=Path.Combine(store.Root,"graphics-file-fixture","Rust");var cfgFolder=Path.Combine(folder,"cfg");Directory.CreateDirectory(cfgFolder);File.WriteAllBytes(Path.Combine(folder,"RustClient.exe"),[0]);
        var path=Path.Combine(cfgFolder,"client.cfg");var original=Encoding.UTF8.GetBytes("graphics.renderscale \"1\"\r\nmesh.quality \"100\"\r\nterrain.quality \"100\"\r\ngraphicssettings.shadowqualitypreset \"3\"\r\ninput.sensitivity \"0.5\"\r\n");File.WriteAllBytes(path,original);
        var fixtureStore=new JsonStore(Path.Combine(folder,"journal"));var manager=new GameGraphicsManager(native,fixtureStore);var preview=await manager.Preview("Rust","Potato",path);
        var result=await manager.Apply(preview);if(result?.Status!="Applied")throw new IOException("Native fixture write failed: "+result?.Error);
        if(GameGraphics.Hash(File.ReadAllBytes(path))!=result.AfterHash)throw new IOException("Native fixture readback mismatch.");
        File.AppendAllText(path,"// unrelated game save\r\n");if((await manager.Undo(result.Id)).Status!="Restored")throw new IOException("Native fixture merge restore failed.");
        if(!File.ReadAllText(path).Contains("// unrelated game save",StringComparison.Ordinal)||!File.ReadAllText(path).Contains("graphics.renderscale \"1\"",StringComparison.Ordinal))throw new IOException("Native merge restore lost unrelated values.");
        preview=await manager.Preview("Rust","Potato",path);File.AppendAllText(path,"// stale-preview test\r\n");bool blocked=false;
        try{await manager.Apply(preview);}catch(InvalidOperationException){blocked=true;}if(!blocked)throw new IOException("Native stale config was overwritten.");
        File.SetAttributes(path,File.GetAttributes(path)|FileAttributes.ReadOnly);try
        {
            var snapshot=await native.Read(new("Rust",path));blocked=false;
            try{await native.Write(new("Rust",path),snapshot.Identity,GameGraphics.Hash(snapshot.Bytes),GameGraphics.Build("Rust","Potato",snapshot.Bytes).Bytes);}catch(UnauthorizedAccessException){blocked=true;}
            if(!blocked)throw new IOException("Read-only game protection was not honored.");
        }
        finally{File.SetAttributes(path,File.GetAttributes(path)&~FileAttributes.ReadOnly);}
        var oldIdentity=await native.Read(new("Rust",path));File.Move(path,path+".previous");File.WriteAllBytes(path,oldIdentity.Bytes);blocked=false;
        try{await native.Write(new("Rust",path),oldIdentity.Identity,GameGraphics.Hash(oldIdentity.Bytes),GameGraphics.Build("Rust","Potato",oldIdentity.Bytes).Bytes);}catch(InvalidOperationException){blocked=true;}
        if(!blocked)throw new IOException("A replaced file identity was not rejected.");
        report.Add("Real Windows file backend passed fixture apply/readback, journaled merge restore, stale-content rejection, replaced-file identity rejection and read-only protection. Installed game files were read only. No game was launched; runtime graphics/FPS and ban behavior were not tested.");
        File.WriteAllLines(Path.Combine(output,"graphics-native-validation.txt"),report);
    }
}
