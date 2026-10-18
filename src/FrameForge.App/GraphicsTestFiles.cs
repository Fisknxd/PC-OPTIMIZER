using System.Text;
using System.IO;
using FrameForge.Core;
namespace FrameForge.App;
internal sealed class GraphicsTestFiles:IGameConfigFiles
{
    public readonly Dictionary<string,byte[]> Files=new(){["Fortnite"]=Encoding.UTF8.GetBytes("[/Script/FortniteGame.FortGameUserSettings]\r\nbMotionBlur=True\r\nbUseNanite=True\r\nbRayTracing=True\r\nbUseVSync=True\r\n[ScalabilityGroups]\r\nsg.ResolutionQuality=100\r\nsg.TextureQuality=3\r\nsg.ShadowQuality=3\r\nsg.LandscapeQuality=3\r\n[D3DRHIPreference]\r\nPreferredFeatureLevel=sm6\r\n"),["Rust"]=Encoding.UTF8.GetBytes("graphics.renderscale \"1\"\nmesh.quality \"100\"\nterrain.quality \"100\"\ngraphicssettings.shadowqualitypreset \"3\"\neffects.antialiasing \"2\"\ninput.sensitivity \"0.5\"\n")};
    public int Writes;
    public Task<GameConfigTarget?> Locate(string game,string? selectedPath=null)=>Task.FromResult<GameConfigTarget?>(new(game,"QA-ONLY-"+game+".cfg"));
    public Task<bool> IsRunning(string game)=>Task.FromResult(false);
    public Task<GameConfigSnapshot> Read(GameConfigTarget target)=>Task.FromResult(new GameConfigSnapshot(Files[target.Game].ToArray(),"qa-"+target.Game));
    public Task Write(GameConfigTarget target,string expectedIdentity,string expectedHash,byte[] bytes){if(GameGraphics.Hash(Files[target.Game])!=expectedHash)throw new IOException("Stale fixture");Files[target.Game]=bytes.ToArray();Writes++;return Task.CompletedTask;}
}
