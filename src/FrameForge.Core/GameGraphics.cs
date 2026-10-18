using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
namespace FrameForge.Core;

public sealed record GraphicsEdit(string Section,string Key,string Value,string Description,bool Required=false);
public sealed record GraphicsDifference(string Section,string Key,string Before,string After,string Description);
public sealed record ConfigPatch(byte[] Bytes,IReadOnlyList<GraphicsDifference> Changes,IReadOnlyList<string> Skipped);
public sealed record GameConfigTarget(string Game,string Path);
public sealed record GameConfigSnapshot(byte[] Bytes,string Identity);
public sealed record GameGraphicsPreview(GameConfigTarget Target,GameConfigSnapshot Snapshot,string Mode,ConfigPatch Patch);
public interface IGameConfigFiles
{
    Task<GameConfigTarget?> Locate(string game,string? selectedPath=null);
    Task<bool> IsRunning(string game);
    Task<GameConfigSnapshot> Read(GameConfigTarget target);
    Task Write(GameConfigTarget target,string expectedIdentity,string expectedHash,byte[] bytes);
}
public static class GameGraphics
{
    public const int MaxBytes=2*1024*1024;
    public static string Hash(byte[] bytes)=>Convert.ToHexString(SHA256.HashData(bytes));
    public static IReadOnlyList<GraphicsEdit> Edits(string game,string mode)
    {
        if(mode is not ("Potato" or "Sharper potato"))throw new InvalidOperationException("Unknown graphics profile.");
        bool potato=mode=="Potato";
        if(game=="Fortnite")
        {
            const string section="/Script/FortniteGame.FortGameUserSettings";
            var edits=new List<GraphicsEdit> {
                new("D3DRHIPreference","PreferredFeatureLevel","es31","Performance renderer",true),
                new("D3DRHIPreference","PreferredRHI","dx12","DirectX 12 performance renderer"),
                new("D3DRHIPreference","bPreferD3D12InGame","True","DirectX 12 preference"),
                new("ScalabilityGroups","sg.ResolutionQuality",potato?"50":"75","Render at "+(potato?"50%":"75%")+" of each dimension",true),
                new(section,"bMotionBlur","False","Remove motion blur"),new(section,"bUseNanite","False","Disable high-detail virtual geometry"),
                new(section,"bRayTracing","False","Disable ray-traced lighting"),new(section,"bUseVSync","False","Avoid waiting for vertical sync; may tear"),
                new(section,"bUseDynamicResolution","False","Use the selected fixed render scale"),new(section,"MeshQuality","0","Lowest normal mesh detail")
            };
            foreach(var key in new[]{"ViewDistance","AntiAliasing","Shadow","GlobalIllumination","Reflection","PostProcess","Texture","Effects","Foliage","Shading","Landscape"})
                edits.Add(new("ScalabilityGroups","sg."+key+"Quality","0","Lowest "+key+" quality",key is "Texture" or "Shadow"));
            return edits;
        }
        if(game=="Rust")return [
            new("","graphics.renderscale",potato?"0.5":"0.75","Render at "+(potato?"50%":"75%")+" of each dimension",true),
            new("","graphicssettings.shadowqualitypreset","0","Lowest current shadow preset"),
            // Only update legacy controls if this installed game already saved them.
            new("","graphics.shaderlod","100","Legacy built-in potato shader level, if present"),
            new("","graphics.quality","0","Legacy lowest graphics preset, if present"),
            new("","graphics.shadowlights","1","Keep the game's supported minimum extra shadow light"),
            new("","mesh.quality","0","Lowest normal object detail",true),new("","terrain.quality","0","Lowest normal terrain detail",true),
            new("","tree.quality","0","Lowest normal tree detail"),new("","decor.quality","0","Lowest decoration detail"),
            new("","particle.quality","0","Lowest particle detail"),new("","grass.quality","0","Lowest menu grass quality; no distance override"),
            new("","water.quality","0","Lowest water detail"),new("","water.reflections","0","Lowest water reflections"),
            new("","effects.antialiasing","0","No extra edge smoothing"),new("","effects.ao","False","Disable ambient occlusion"),
            new("","effects.motionblur","False","Remove motion blur"),new("","effects.bloom","False","Remove extra bloom"),
            new("","effects.lensdirt","False","Remove lens dirt"),new("","effects.shafts","False","Remove decorative light shafts"),
            new("","graphics.dof","False","Remove depth-of-field blur"),new("","graphics.parallax","0","Lowest surface depth effect"),
            new("","graphics.vsync","0","Avoid waiting for vertical sync; may tear")
        ];
        throw new InvalidOperationException("Unsupported game.");
    }
    public static ConfigPatch Build(string game,string mode,byte[] original)=>Patch(game,original,Edits(game,mode),restore:false);
    public static ConfigPatch Restore(string game,byte[] current,IReadOnlyList<GraphicsDifference> differences)
    {
        var allowed=Edits(game,"Potato");
        if(differences.Count==0||differences.GroupBy(x=>(x.Section,x.Key)).Any(g=>g.Count()>1)||differences.Any(x=>!allowed.Any(a=>a.Section==x.Section&&a.Key==x.Key)))throw new InvalidDataException("Invalid graphics backup.");
        // Validate all managed fields before editing any: unrelated game/bind/account changes survive.
        var inspected=Patch(game,current,differences.Select(x=>new GraphicsEdit(x.Section,x.Key,x.After,x.Description,true)).ToArray(),restore:true);
        if(inspected.Changes.Count!=0)throw new InvalidOperationException("Graphics changed outside FrameForge. Saved originals are kept; no overwrite performed.");
        return Patch(game,current,differences.Select(x=>new GraphicsEdit(x.Section,x.Key,x.Before,x.Description,true)).ToArray(),restore:true);
    }
    static ConfigPatch Patch(string game,byte[] bytes,IReadOnlyList<GraphicsEdit> edits,bool restore)
    {
        if(bytes.Length==0||bytes.Length>MaxBytes)throw new InvalidDataException("Game configuration is empty or too large.");
        Encoding encoding;int preamble;
        if(bytes.AsSpan().StartsWith(new byte[]{0xFF,0xFE})){encoding=new UnicodeEncoding(false,true,true);preamble=2;}
        else if(bytes.AsSpan().StartsWith(new byte[]{0xFE,0xFF})){encoding=new UnicodeEncoding(true,true,true);preamble=2;}
        else {encoding=new UTF8Encoding(false,true);preamble=bytes.AsSpan().StartsWith(new byte[]{0xEF,0xBB,0xBF})?3:0;}
        var text=encoding.GetString(bytes,preamble,bytes.Length-preamble);if(text.Contains('\0'))throw new InvalidDataException("Unsupported game configuration encoding.");
        if(text.Count(c=>c=='\n')>50000)throw new InvalidDataException("Game configuration has too many lines.");
        var lines=Regex.Split(text,"(?<=\n)");var matches=new Dictionary<string,(int Line,Match Match,GraphicsEdit Edit)>(StringComparer.OrdinalIgnoreCase);string section="";
        var wanted=edits.ToDictionary(e=>e.Section+"|"+e.Key,StringComparer.OrdinalIgnoreCase);
        var entryPattern=new Regex(game=="Fortnite"?@"^(?<prefix>\s*(?<key>[A-Za-z0-9_.]+)\s*=\s*)(?<value>[^;\r\n]*?)(?<tail>\s*(?:;.*)?)$":"^(?<prefix>\\s*(?<key>[A-Za-z0-9_.]+)\\s+\")(?<value>[^\"\\r\\n]*)(?<tail>\"\\s*(?://.*)?)$",RegexOptions.CultureInvariant,TimeSpan.FromSeconds(1));
        for(int i=0;i<lines.Length;i++)
        {
            var raw=lines[i].TrimEnd('\r','\n');var trimmed=raw.Trim();if(trimmed.StartsWith('[')&&trimmed.EndsWith(']')){section=trimmed[1..^1];continue;}
            var match=entryPattern.Match(raw);if(!match.Success)continue;
            var id=section+"|"+match.Groups["key"].Value;if(!wanted.TryGetValue(id,out var edit))continue;
            if(!matches.TryAdd(id,(i,match,edit)))throw new InvalidDataException("Duplicate game setting: "+edit.Key+". No changes made.");
        }
        var missing=edits.Where(e=>!matches.ContainsKey(e.Section+"|"+e.Key)).ToList();
        if(missing.Any(e=>e.Required))throw new InvalidDataException("This game configuration is missing "+string.Join(", ",missing.Where(e=>e.Required).Select(e=>e.Key))+". Start the current game once, save its settings and close it.");
        var changes=new List<GraphicsDifference>();
        foreach(var entry in matches.Values)
        {
            var old=entry.Match.Groups["value"].Value;
            bool normal=entry.Edit.Key is "PreferredFeatureLevel"?new[]{"es31","sm5","sm6"}.Contains(old,StringComparer.OrdinalIgnoreCase):entry.Edit.Key=="PreferredRHI"?new[]{"dx11","dx12"}.Contains(old,StringComparer.OrdinalIgnoreCase):bool.TryParse(old,out _)||double.TryParse(old,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out var number)&&double.IsFinite(number)&&number>=0&&number<=10000;
            if(!normal)throw new InvalidDataException("Unexpected graphics value for "+entry.Edit.Key+". No changes made.");
            if(restore&&(entry.Edit.Value.IndexOfAny(['\r','\n','\"',';'])>=0||entry.Edit.Value.Length>80))throw new InvalidDataException("Invalid saved graphics value.");
            if(EqualValue(old,entry.Edit.Value))continue;
            changes.Add(new(entry.Edit.Section,entry.Edit.Key,old,entry.Edit.Value,entry.Edit.Description));
            var value=entry.Match.Groups["value"];lines[entry.Line]=lines[entry.Line][..value.Index]+entry.Edit.Value+lines[entry.Line][(value.Index+value.Length)..];
        }
        var content=encoding.GetBytes(string.Concat(lines));var output=new byte[preamble+content.Length];bytes.AsSpan(0,preamble).CopyTo(output);content.CopyTo(output,preamble);
        return new(output,changes,missing.Select(x=>x.Key).ToArray());
    }
    static bool EqualValue(string a,string b)=>bool.TryParse(a,out var x)&&bool.TryParse(b,out var y)?x==y:double.TryParse(a,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out var n)&&double.TryParse(b,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out var m)?n==m:string.Equals(a,b,StringComparison.OrdinalIgnoreCase);
}

public sealed record GameGraphicsChange
{
    public string Id {get;init;}=Guid.NewGuid().ToString("N");
    public DateTimeOffset Time {get;init;}=DateTimeOffset.UtcNow;
    public required GameConfigTarget Target {get;init;}
    public required string Mode {get;init;}
    public required string BeforeBase64 {get;init;}
    public required string AfterHash {get;init;}
    public required string Identity {get;init;}
    public required List<GraphicsDifference> Changes {get;init;}
    public string Status {get;set;}="Prepared";
    public string? Error {get;set;}
}
public sealed class GameGraphicsManager(IGameConfigFiles files,JsonStore store,ActivityFeed? activity=null)
{
    readonly SemaphoreSlim gate=new(1,1);
    public List<GameGraphicsChange> History=>store.Load<List<GameGraphicsChange>>("game-graphics-history.json",()=>[]);
    public async Task<GameGraphicsPreview> Preview(string game,string mode,string? selectedPath=null)
    {
        _=GameGraphics.Edits(game,mode);var target=await files.Locate(game,selectedPath)??throw new FileNotFoundException("No saved "+game+" settings found. Start the game once and close it, or locate its configuration.");
        if(await files.IsRunning(game))throw new InvalidOperationException("Close "+game+" before applying or restoring graphics.");
        var snapshot=await files.Read(target);return new(target,snapshot,mode,GameGraphics.Build(game,mode,snapshot.Bytes));
    }
    public async Task<GameGraphicsChange?> Apply(GameGraphicsPreview preview)
    {
        await gate.WaitAsync();
        try
        {
            var fresh=await Preview(preview.Target.Game,preview.Mode,preview.Target.Path);
            if(fresh.Target!=preview.Target||fresh.Snapshot.Identity!=preview.Snapshot.Identity||GameGraphics.Hash(fresh.Snapshot.Bytes)!=GameGraphics.Hash(preview.Snapshot.Bytes))throw new InvalidOperationException("Game settings changed since the preview. Refresh before applying.");
            if(fresh.Patch.Changes.Count==0)return null;
            var record=new GameGraphicsChange {Target=fresh.Target,Mode=fresh.Mode,BeforeBase64=Convert.ToBase64String(fresh.Snapshot.Bytes),AfterHash=GameGraphics.Hash(fresh.Patch.Bytes),Identity=fresh.Snapshot.Identity,Changes=fresh.Patch.Changes.ToList()};
            var history=History;history.Add(record);store.Save("game-graphics-history.json",history);
            try
            {
                activity?.Report("Original game configuration backed up: "+record.Target.Path);
                foreach(var change in record.Changes)activity?.Report(change.Key+": "+change.Before+" → "+change.After);
                await files.Write(record.Target,record.Identity,GameGraphics.Hash(fresh.Snapshot.Bytes),fresh.Patch.Bytes);
                if(GameGraphics.Hash((await files.Read(record.Target)).Bytes)!=record.AfterHash)throw new IOException("Game config read-back verification failed.");
                record.Status="Applied";activity?.Report("VERIFIED: Graphics configuration saved. Start the game to load it; in-game behavior has not been measured.");
            }
            catch(Exception e){record.Status="Failed";record.Error=e.Message;activity?.Report("FAILED: "+e.Message);}
            store.Save("game-graphics-history.json",history);return record;
        }
        finally{gate.Release();}
    }
    public async Task<GameGraphicsChange> Undo(string id)
    {
        await gate.WaitAsync();
        try
        {
            var history=History;var record=history.Single(x=>x.Id==id);if(record.Status=="Restored")return record;
            try
            {
                if(history.Skip(history.IndexOf(record)+1).Any(x=>x.Target==record.Target&&x.Status!="Restored"))throw new InvalidOperationException("Restore newer graphics changes for this game first.");
                if(await files.IsRunning(record.Target.Game))throw new InvalidOperationException("Close the game before restoring graphics.");
                var original=Convert.FromBase64String(record.BeforeBase64);var generated=GameGraphics.Build(record.Target.Game,record.Mode,original);
                if(GameGraphics.Hash(generated.Bytes)!=record.AfterHash||!generated.Changes.SequenceEqual(record.Changes))throw new InvalidDataException("Graphics backup validation failed.");
                var target=await files.Locate(record.Target.Game,record.Target.Path);if(target!=record.Target)throw new InvalidOperationException("The original game configuration moved.");
                var current=await files.Read(record.Target);byte[] restored;
                if(GameGraphics.Hash(current.Bytes)==GameGraphics.Hash(original))restored=current.Bytes;
                else if(GameGraphics.Hash(current.Bytes)==record.AfterHash)restored=original;
                else restored=GameGraphics.Restore(record.Target.Game,current.Bytes,record.Changes).Bytes;
                if(!restored.AsSpan().SequenceEqual(current.Bytes))
                {
                    record.Status="Restoring";store.Save("game-graphics-history.json",history);await files.Write(record.Target,current.Identity,GameGraphics.Hash(current.Bytes),restored);
                    if(GameGraphics.Hash((await files.Read(record.Target)).Bytes)!=GameGraphics.Hash(restored))throw new IOException("Graphics restore verification failed.");
                }
                record.Status="Restored";record.Error=null;activity?.Report("VERIFIED: Previous graphics restored; unrelated config entries preserved.");
            }
            catch(Exception e){record.Status="Restore failed";record.Error=e.Message;activity?.Report("RESTORE FAILED: "+e.Message);}
            store.Save("game-graphics-history.json",history);return record;
        }
        finally{gate.Release();}
    }
}
