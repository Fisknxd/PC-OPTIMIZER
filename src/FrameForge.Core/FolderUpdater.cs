using System.Security.Cryptography;

namespace FrameForge.Core;

public sealed record FolderUpdateResult(string Destination,string? Backup,bool AlreadyCurrent);
public static class FolderUpdater
{
    static string Normalize(string path)=>Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
    static readonly StringComparison Comparison=OperatingSystem.IsWindows()?StringComparison.OrdinalIgnoreCase:StringComparison.Ordinal;
    static void RejectLinks(string root)
    {
        for(string? path=Path.GetFullPath(root);path!=null;path=Path.GetDirectoryName(path))
            if(Directory.Exists(path)&&(File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0) throw new IOException("Update paths must not contain filesystem links.");
    }
    public static FolderUpdateResult Install(string source,string destination)
    {
        source=Path.GetFullPath(source); destination=Path.GetFullPath(destination);
        if(!File.Exists(Path.Combine(source,"FrameForge.exe"))||!File.Exists(Path.Combine(source,"FrameForge.runtimeconfig.json"))) throw new InvalidDataException("Select the complete extracted FrameForge application folder.");
        RejectLinks(source); RejectLinks(destination);
        if(Normalize(source).Equals(Normalize(destination),Comparison)) return new(destination,null,true);
        if(Normalize(source).StartsWith(Normalize(destination),Comparison)||Normalize(destination).StartsWith(Normalize(source),Comparison)) throw new IOException("Extract the update outside the existing application folder first, for example into Downloads.");
        var parent=Directory.GetParent(destination)?.FullName??throw new IOException("Cannot update a filesystem root.");
        if(!Directory.Exists(parent)) throw new DirectoryNotFoundException("Destination parent folder does not exist.");
        if(Directory.Exists(destination)&&(!File.Exists(Path.Combine(destination,"FrameForge.exe"))||!File.Exists(Path.Combine(destination,"FrameForge.runtimeconfig.json")))) throw new IOException("The existing folder is not a recognized FrameForge application. Choose the folder containing FrameForge.exe; no files were changed.");
        var staging=Path.Combine(parent,".FrameForge-update-"+Guid.NewGuid().ToString("N"));
        string? backup=null;
        try
        {
            CopyVerified(source,staging);
            if(Directory.Exists(destination))
            {
                backup=destination+"-backup-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N")[..6];
                Directory.Move(destination,backup);
            }
            try { Directory.Move(staging,destination); }
            catch
            {
                if(backup!=null&&!Directory.Exists(destination)) Directory.Move(backup,destination);
                throw;
            }
            return new(destination,backup,false);
        }
        finally
        {
            // This path was created by this invocation; never delete a destination or backup.
            if(Directory.Exists(staging)) Directory.Delete(staging,true);
        }
    }
    static void CopyVerified(string source,string destination)
    {
        Directory.CreateDirectory(destination);
        foreach(var path in Directory.EnumerateFileSystemEntries(source))
        {
            var attributes=File.GetAttributes(path);
            if((attributes&FileAttributes.ReparsePoint)!=0) throw new IOException("Links are not supported inside an application update.");
            var target=Path.Combine(destination,Path.GetFileName(path));
            if((attributes&FileAttributes.Directory)!=0) CopyVerified(path,target);
            else
            {
                File.Copy(path,target,overwrite:false);
                using var original=File.OpenRead(path); using var copy=File.OpenRead(target);
                if(!SHA256.HashData(original).SequenceEqual(SHA256.HashData(copy))) throw new IOException("Update copy failed integrity verification: "+Path.GetFileName(path));
            }
        }
    }
}
