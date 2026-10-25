using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using FrameForge.Core;
namespace FrameForge.Windows;

public sealed class WindowsGameConfigs:IGameConfigFiles
{
    [StructLayout(LayoutKind.Sequential)]struct FileInfoNative
    {
        public uint Attributes,CreationLow,CreationHigh,AccessLow,AccessHigh,WriteLow,WriteHigh,Volume,SizeHigh,SizeLow,Links,IndexHigh,IndexLow;
        public readonly string Identity=>$"{Volume:X8}:{IndexHigh:X8}{IndexLow:X8}";
    }
    [DllImport("kernel32.dll",SetLastError=true)]
    [return:MarshalAs(UnmanagedType.Bool)]static extern bool GetFileInformationByHandle(SafeFileHandle file,out FileInfoNative info);
    static string CurrentConfig(string folder)=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"FortniteGame","Saved","Config",folder,"GameUserSettings.ini");
    static string Validate(GameConfigTarget target)
    {
        var full=Path.GetFullPath(target.Path);
        if(full.StartsWith(@"\\",StringComparison.Ordinal))throw new InvalidOperationException("Game settings must be on a local drive.");
        if(target.Game=="Fortnite")
        {
            if(!new[]{CurrentConfig("WindowsClient"),CurrentConfig("Windows")}.Contains(full,StringComparer.OrdinalIgnoreCase))throw new InvalidOperationException("Select this Windows user's Fortnite GameUserSettings.ini.");
        }
        else if(target.Game=="Rust")
        {
            var cfg=Path.GetDirectoryName(full);var install=Path.GetDirectoryName(cfg);
            if(!Path.GetFileName(full).Equals("client.cfg",StringComparison.OrdinalIgnoreCase)||!string.Equals(Path.GetFileName(cfg),"cfg",StringComparison.OrdinalIgnoreCase)||install==null||!File.Exists(Path.Combine(install,"RustClient.exe")))throw new InvalidOperationException("Select client.cfg inside the installed Rust game's cfg folder.");
        }
        else throw new InvalidOperationException("Unsupported game.");
        for(string? item=full;item!=null;item=Path.GetDirectoryName(item))if((File.GetAttributes(item)&FileAttributes.ReparsePoint)!=0)throw new InvalidOperationException("Linked game configurations are not edited. Select an ordinary local installation.");
        return full;
    }
    static string Inspect(FileStream stream)
    {
        if(!GetFileInformationByHandle(stream.SafeFileHandle,out var info))throw new Win32Exception(Marshal.GetLastPInvokeError());
        if(info.Links!=1||(info.Attributes&0x414)!=0)throw new InvalidOperationException("Linked, system or directory configuration targets are blocked.");
        if(stream.Length<=0||stream.Length>GameGraphics.MaxBytes)throw new InvalidDataException("Configuration is empty or too large.");return info.Identity;
    }
    public Task<GameConfigTarget?> Locate(string game,string? selectedPath=null)=>Task.Run<GameConfigTarget?>(()=>
    {
        if(selectedPath!=null){var target=new GameConfigTarget(game,Path.GetFullPath(selectedPath));_=Validate(target);return target;}
        if(game=="Fortnite")
        {
            var candidates=new[]{CurrentConfig("WindowsClient"),CurrentConfig("Windows")}.Where(File.Exists).ToArray();
            if(candidates.Length>1)throw new InvalidOperationException("Two Fortnite configurations exist. Use Locate config to select the active one.");
            return candidates.Length==0?null:new(game,Validate(new(game,candidates[0])));
        }
        if(game!="Rust")throw new InvalidOperationException("Unsupported game.");
        using var steam=Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");if(steam?.GetValue("SteamPath") is not string root)return null;
        if(!Path.IsPathFullyQualified(root)||root.StartsWith(@"\\",StringComparison.Ordinal))return null;
        var libraries=new List<string>{root};var vdf=Path.Combine(root,"steamapps","libraryfolders.vdf");
        if(File.Exists(vdf)&&new FileInfo(vdf).Length<1048576)libraries.AddRange(Regex.Matches(File.ReadAllText(vdf),"\"path\"\\s+\"([^\"]+)\"").Select(m=>m.Groups[1].Value.Replace("\\\\","\\")));
        var found=new List<GameConfigTarget>();
        foreach(var library in libraries.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if(!Path.IsPathFullyQualified(library)||library.StartsWith(@"\\",StringComparison.Ordinal))continue;
            var folder="Rust";var manifest=Path.Combine(library,"steamapps","appmanifest_252490.acf");
            if(File.Exists(manifest)&&new FileInfo(manifest).Length<1048576)
            {
                var match=Regex.Match(File.ReadAllText(manifest),"\"installdir\"\\s+\"([^\"]+)\"");if(match.Success)folder=match.Groups[1].Value;
            }
            if(Path.GetFileName(folder)!=folder)continue;
            var cfg=Path.Combine(library,"steamapps","common",folder,"cfg","client.cfg");
            if(File.Exists(cfg)&&File.Exists(Path.Combine(library,"steamapps","common",folder,"RustClient.exe")))found.Add(new(game,Validate(new(game,cfg))));
        }
        if(found.Count>1)throw new InvalidOperationException("Multiple Rust installations found. Use Locate config to choose one.");return found.SingleOrDefault();
    });
    public Task<bool> IsRunning(string game)=>Task.Run(()=>
    {
        foreach(var process in Process.GetProcesses())using(process)
        {
            try {var name=process.ProcessName;if(game=="Fortnite"&&name.StartsWith("FortniteClient",StringComparison.OrdinalIgnoreCase)||game=="Rust"&&(name.Equals("RustClient",StringComparison.OrdinalIgnoreCase)||name.Equals("Rust",StringComparison.OrdinalIgnoreCase)))return true;}
            catch(InvalidOperationException){ }
        }
        return false;
    });
    public Task<GameConfigSnapshot> Read(GameConfigTarget target)=>Task.Run(()=>
    {
        var full=Validate(target);using var stream=new FileStream(full,FileMode.Open,FileAccess.Read,FileShare.None);var identity=Inspect(stream);var bytes=new byte[(int)stream.Length];stream.ReadExactly(bytes);return new GameConfigSnapshot(bytes,identity);
    });
    public async Task Write(GameConfigTarget target,string expectedIdentity,string expectedHash,byte[] bytes)
    {
        if(bytes.Length==0||bytes.Length>GameGraphics.MaxBytes)throw new InvalidDataException("Invalid graphics configuration size.");
        if(await IsRunning(target.Game))throw new InvalidOperationException("Close the game before editing graphics.");
        var full=Validate(target);if((File.GetAttributes(full)&FileAttributes.ReadOnly)!=0)throw new UnauthorizedAccessException("This configuration is read-only. FrameForge will not remove that protection.");
        using var stream=new FileStream(full,FileMode.Open,FileAccess.ReadWrite,FileShare.None);var identity=Inspect(stream);var current=new byte[(int)stream.Length];await stream.ReadExactlyAsync(current);
        if(identity!=expectedIdentity||GameGraphics.Hash(current)!=expectedHash)throw new InvalidOperationException("Configuration file identity or contents changed. Refresh before applying.");
        if(await IsRunning(target.Game))throw new InvalidOperationException("The game started during the operation. No settings were changed.");
        stream.Position=0;await stream.WriteAsync(bytes);stream.SetLength(bytes.Length);stream.Flush(flushToDisk:true);
    }
}
