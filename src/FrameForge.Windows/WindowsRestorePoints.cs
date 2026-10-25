using System.ComponentModel;
using System.Management;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using FrameForge.Core;

namespace FrameForge.Windows;
public sealed class WindowsRestorePoints:IRestorePointBackend
{
    const string FrequencyPath=@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore";
    const string FrequencyName="SystemRestorePointCreationFrequency";
    public bool IsAdministrator=>new WindowsSettings().IsAdministrator;
    public Task<IReadOnlyList<LocalRestorePoint>> List()=>Task.Run<IReadOnlyList<LocalRestorePoint>>(()=>
    {
        using var search=new ManagementObjectSearcher(new ManagementScope(@"root\default"),new ObjectQuery("SELECT SequenceNumber,Description,CreationTime FROM SystemRestore"),new System.Management.EnumerationOptions {Timeout=TimeSpan.FromSeconds(15)});
        using var rows=search.Get(); var result=new List<LocalRestorePoint>();
        foreach(ManagementObject row in rows)
            using(row) result.Add(new(Convert.ToInt64(row["SequenceNumber"]),Convert.ToString(row["Description"])??"Windows restore point",new DateTimeOffset(ManagementDateTimeConverter.ToDateTime((string)row["CreationTime"]))));
        return result.OrderByDescending(x=>x.Id).ToList();
    });
    public SettingValue ReadFrequency()
    {
        using var hive=RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,RegistryView.Registry64); using var key=hive.OpenSubKey(FrequencyPath);
        if(key==null||!key.GetValueNames().Contains(FrequencyName)) return new(false,null);
        if(key.GetValueKind(FrequencyName)!=RegistryValueKind.DWord) throw new InvalidDataException("Windows restore-point timing has an unexpected format.");
        return new(true,Convert.ToString(key.GetValue(FrequencyName),System.Globalization.CultureInfo.InvariantCulture));
    }
    public void WriteFrequency(SettingValue value)
    {
        if(!IsAdministrator) throw new UnauthorizedAccessException("Administrator access is required.");
        using var hive=RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,RegistryView.Registry64); using var key=hive.CreateSubKey(FrequencyPath,true);
        if(!value.Exists) { key.DeleteValue(FrequencyName,false); key.Flush(); return; }
        if(value.Kind!="DWord"||!int.TryParse(value.Value,out var frequency)) throw new InvalidDataException("Invalid restore-point timing backup.");
        key.SetValue(FrequencyName,frequency,RegistryValueKind.DWord); key.Flush();
    }
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
    struct RestoreInfo
    {
        public uint EventType,PointType;
        public long Sequence;
        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=256)] public string Description;
    }
    [StructLayout(LayoutKind.Sequential)] struct RestoreStatus {public uint Status; public long Sequence;}
    [DllImport("srclient.dll",EntryPoint="SRSetRestorePointW",CharSet=CharSet.Unicode,SetLastError=true)]
    [return:MarshalAs(UnmanagedType.Bool)] static extern bool SetPoint(ref RestoreInfo info,out RestoreStatus status);
    public Task Create(string description)=>Task.Run(()=>
    {
        if(!IsAdministrator) throw new UnauthorizedAccessException("Administrator access is required.");
        var begin=new RestoreInfo {EventType=100,PointType=12,Description=description};
        var success=SetPoint(ref begin,out var status);
        if(!success||status.Status!=0) throw new Win32Exception((int)(status.Status!=0?status.Status:(uint)Marshal.GetLastWin32Error()),"Windows could not create a restore point. Turn on System Protection for the Windows drive, then try again.");
        var end=new RestoreInfo {EventType=101,PointType=12,Sequence=status.Sequence,Description=description};
        if(!SetPoint(ref end,out var completed)||completed.Status!=0) throw new IOException("Windows started the restore point but did not confirm completion. Check the restore-point list before retrying.");
    });
}
