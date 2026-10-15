using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace FrameForge.Core;
internal static class NativeCleanup
{
    [StructLayout(LayoutKind.Sequential)]
    struct Info
    {
        public uint Attributes,CreationLow,CreationHigh,AccessLow,AccessHigh,WriteLow,WriteHigh;
        public uint Volume,SizeHigh,SizeLow,Links,IndexHigh,IndexLow;
        public readonly string Identity=>$"{Volume:X8}:{IndexHigh:X8}{IndexLow:X8}";
        public readonly long Length=>((long)SizeHigh<<32)|SizeLow;
        public readonly DateTime Written=>DateTime.FromFileTimeUtc(((long)WriteHigh<<32)|WriteLow);
    }
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)]
    static extern SafeFileHandle CreateFileW(string name,uint access,uint share,IntPtr security,uint creation,uint flags,IntPtr template);
    [DllImport("kernel32.dll",SetLastError=true)]
    [return:MarshalAs(UnmanagedType.Bool)]
    static extern bool GetFileInformationByHandle(SafeFileHandle file,out Info info);
    [DllImport("kernel32.dll",SetLastError=true)]
    [return:MarshalAs(UnmanagedType.Bool)]
    static extern bool SetFileInformationByHandle(SafeFileHandle file,int infoClass,ref byte info,uint size);
    static IOException Error()=>new("Cannot safely open or delete this file: "+new Win32Exception(Marshal.GetLastPInvokeError()).Message);
    static SafeFileHandle Open(string path,bool delete)
    {
        var handle=CreateFileW(path,delete?0x10080u:0x80u,delete?0u:7u,IntPtr.Zero,3,0x00200000,IntPtr.Zero);
        if(handle.IsInvalid) { handle.Dispose(); throw Error(); } return handle;
    }
    public static CleanupFile Describe(string path)
    {
        if(!OperatingSystem.IsWindows())
        {
            var file=new FileInfo(path); return new(path,file.Length,file.LastWriteTimeUtc);
        }
        using var h=Open(path,false);
        if(!GetFileInformationByHandle(h,out var info)||(info.Attributes&0x411)!=0||info.Links!=1) throw Error();
        return new(path,info.Length,info.Written,info.Identity);
    }
    public static void DeleteExact(CleanupFile file)
    {
        if(!OperatingSystem.IsWindows())
        {
            // Portable policy tests only; product deletion uses the Windows handle path.
            using var h=new FileStream(file.Path,FileMode.Open,FileAccess.ReadWrite,FileShare.None);
            if(h.Length!=file.Bytes||File.GetLastWriteTimeUtc(file.Path)!=file.LastWriteUtc) throw new IOException("File changed since preview.");
            File.Delete(file.Path); return;
        }
        using var handle=Open(file.Path,true);
        if(!GetFileInformationByHandle(handle,out var info)) throw Error();
        if(file.Identity==null||info.Identity!=file.Identity||info.Length!=file.Bytes||info.Written!=file.LastWriteUtc||(info.Attributes&0x411)!=0||info.Links!=1)
            throw new IOException("File identity, metadata, attributes or hard-link count changed; skipped.");
        byte delete=1;
        if(!SetFileInformationByHandle(handle,4,ref delete,1)) throw Error();
    }
}
