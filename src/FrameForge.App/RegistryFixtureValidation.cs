using System.Globalization;
using System.IO;
using FrameForge.Core;
using Microsoft.Win32;
namespace FrameForge.App;
public sealed partial class MainWindow
{
    static async Task ValidateDisposableRegistryGroup(string output)
    {
        var path=@"Software\FrameForge-Validation-"+Guid.NewGuid().ToString("N");
        try
        {
            using var key=Registry.CurrentUser.CreateSubKey(path,true);key.SetValue("B",0,RegistryValueKind.DWord);key.Flush();
            var definition=new TweakDefinition("fixture","Fixture","1","0","RegistryGroup",Members:[new(path,"A"),new(path,"B")]);
            Task<SettingValue> Read(int index)
            {
                var name=index==0?"A":"B";return Task.FromResult(key.GetValueNames().Contains(name)?new SettingValue(true,Convert.ToString(key.GetValue(name),CultureInfo.InvariantCulture),key.GetValueKind(name).ToString()):new(false,null));
            }
            Task Write(int index,SettingValue value)
            {
                var name=index==0?"A":"B";if(value.Exists)key.SetValue(name,int.Parse(value.Value!,CultureInfo.InvariantCulture),RegistryValueKind.DWord);else key.DeleteValue(name,false);key.Flush();return Task.CompletedTask;
            }
            var original=GroupedSettings.Pack([await Read(0),await Read(1)]);bool failed=false;
            try{await GroupedSettings.Write(definition,TweakDefinitions.GroupTarget(definition,true),Read,(index,value)=>index==1&&value.Value=="1"?Task.FromException(new IOException("Injected second-value refusal")):Write(index,value));}
            catch(IOException){failed=true;}
            if(!failed||GroupedSettings.Pack([await Read(0),await Read(1)])!=original)throw new InvalidDataException("Native disposable grouped-registry rollback failed.");
            await GroupedSettings.Write(definition,TweakDefinitions.GroupTarget(definition,true),Read,Write);
            if((await Read(0)).Value!="1"||(await Read(1)).Value!="1")throw new InvalidDataException("Native disposable grouped-registry write failed.");
            await GroupedSettings.Write(definition,original,Read,Write);
            if(GroupedSettings.Pack([await Read(0),await Read(1)])!=original)throw new InvalidDataException("Native disposable grouped-registry restore did not preserve absence.");
        }
        finally{Registry.CurrentUser.DeleteSubKey(path,false);}
        File.WriteAllText(Path.Combine(output,"registry-fixture.txt"),"Passed: native current-user disposable registry group apply/readback, injected second-write failure, compensation and exact restore including value absence. The unique fixture key was removed. No actual browser, Windows policy, power-plan or service setting was changed.");
    }
}
