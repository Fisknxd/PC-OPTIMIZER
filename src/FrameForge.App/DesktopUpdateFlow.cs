using System.IO;
using System.Windows;
using Microsoft.Win32;
using FrameForge.Core;

namespace FrameForge.App;
internal static class DesktopUpdateFlow
{
    public static void Run()
    {
        var source=AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
        var desktop=Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var destination=Path.Combine(desktop,"FrameForge");
        if(Directory.Exists(destination)&&!File.Exists(Path.Combine(destination,"FrameForge.exe")))
        {
            var nested=Path.Combine(destination,"FrameForge");
            if(File.Exists(Path.Combine(nested,"FrameForge.exe"))) destination=nested;
            else destination=SelectFolder(desktop);
        }
        else if(!Directory.Exists(destination))
        {
            var choice=MessageBox.Show("Install on Desktop?\n\nChoose Yes to create Desktop\\FrameForge. Choose No to select an existing application folder elsewhere. Saved settings and optimization history stay in LocalAppData.","FrameForge 0.1.2 update",MessageBoxButton.YesNoCancel,MessageBoxImage.Question);
            if(choice==MessageBoxResult.Cancel) return;
            if(choice==MessageBoxResult.No) destination=SelectFolder(desktop);
        }
        if(string.IsNullOrWhiteSpace(destination)) return;
        if(Path.GetFullPath(source).Equals(Path.GetFullPath(destination),StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("This folder already contains the update you are running. Open FrameForge.exe to use it.","Already in this folder"); return;
        }
        if(MessageBox.Show($"Update this application folder?\n\n{destination}\n\nThe entire previous folder will be kept in a dated backup beside it. Your saved history/settings in LocalAppData are not changed. Extract this update into Downloads first, and close the old app before continuing.","Install FrameForge 0.1.2",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes) return;
        var result=FolderUpdater.Install(source,destination);
        MessageBox.Show($"FrameForge 0.1.2 is installed.\n\nOpen: {Path.Combine(result.Destination,"FrameForge.exe")}\n\n"+(result.Backup==null?"No existing folder was replaced.":"Previous version preserved at: "+result.Backup)+"\n\nSettings and optimization history are unchanged.","Update complete",MessageBoxButton.OK,MessageBoxImage.Information);
    }
    static string SelectFolder(string initial)
    {
        var picker=new OpenFolderDialog { Title="Choose the existing folder containing FrameForge.exe",InitialDirectory=initial,Multiselect=false };
        return picker.ShowDialog()==true?picker.FolderName:"";
    }
}
