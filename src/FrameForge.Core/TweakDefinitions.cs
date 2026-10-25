using System.Text.Json;
namespace FrameForge.Core;
public sealed record RegistryMember(string Path,string Name);
public sealed record TweakDefinition(string Id,string Group,string On,string Off,string Kind="DWord",string Note="",RegistryMember[]? Members=null,string[]? AllowedValues=null,int? Minimum=null,int? Maximum=null)
{
    public bool Allows(string? value)=>AllowedValues?.Contains(value)==true || Minimum is {} min && Maximum is {} max && int.TryParse(value,out var n)&&n>=min&&n<=max;
}
public static class TweakDefinitions
{
    public static IReadOnlyList<TweakDefinition> All {get;}=[
        ..PerformanceTweaks.Definitions,
        new("extensions","Appearance","0","1",Note:"Refresh File Explorer to see the change."),
        new("hidden","Appearance","1","2",Note:"Protected Windows files stay hidden."),
        new("dark","Appearance","0","1",Note:"Changes the theme in apps that follow Windows."),
        new("game-mode","Gaming","1","0",Note:"Restart your game after changing this."),
        new("game-recording","Gaming","1","0","RegistryGroup","Off stops Windows game recording, including saved clips.",[
            new(@"System\GameConfigStore","GameDVR_Enabled"),new(@"Software\Microsoft\Windows\CurrentVersion\GameDVR","AppCaptureEnabled")]),
        new("controller-bar","Gaming","1","0",Note:"Does not uninstall Game Bar or disable its keyboard shortcut."),
        new("game-tips","Less clutter","1","0",Note:"Hides or shows older Game Bar startup tips; cosmetic only."),
        new("notifications-toggle","Less clutter","1","0",Note:"Affects app pop-ups, not Windows security services."),
        new("mouse-acceleration","Mouse & desktop","6,10,1","0,0,0","Mouse","Off makes desktop mouse movement consistent. Games with raw input usually ignore this. On uses Windows' standard acceleration curve."),
        new("menu-animation","Mouse & desktop","1","0","Boolean","Off removes menu animations. It does not speed up games."),
        new("app-animation","Mouse & desktop","1","0","Boolean","Off removes supported Windows app animations; desktop feel only."),
        new("quick-menus","Mouse & desktop","100","400","Milliseconds","On opens desktop menus after 0.1 seconds instead of the usual 0.4. No in-game effect."),
        new("advertising-id","Less clutter","1","0",Note:"Apps can still show ads. This only changes Windows ad personalization."),
        new("windows-tips","Less clutter","1","0",Note:"Sign out and back in to refresh Windows content preferences. No game performance claim."),
        new("start-suggestions","Less clutter","1","0",Note:"Sign out and back in. Availability depends on your Windows version."),
        new("settings-suggestions","Less clutter","1","0",Note:"Close Settings and sign out and back in. Availability depends on your Windows version."),
        new("lock-screen-tips","Less clutter","1","0",Note:"Applies to supported picture lock screens. Spotlight can manage content separately."),
        new("edge-background","Low-end PC","1","0","RegistryGroup","Off prevents Edge startup boost and background mode. Close Edge normally after changing it; saved tabs stay intact. First launch may be slower.",[
            new(@"Software\Policies\Microsoft\Edge","BackgroundModeEnabled"),new(@"Software\Policies\Microsoft\Edge","StartupBoostEnabled")]),
        new("store-background","Low-end PC","2","0",Note:"On denies background work for supported Windows apps. Notifications and app sync can stop. It does not close desktop apps. Requires a supported Pro/Enterprise/Education edition and an app restart.",AllowedValues:["0","1","2"]),
        new("update-sharing","Low-end PC","1","0",Note:"Off uses HTTP downloads without peer sharing. Windows updates remain enabled. It cannot fix distance to a game server. Requires a supported policy edition.",AllowedValues:["0","1","2","3","99","100"]),
        new("transparency","Low-end PC","1","0",Note:"Off reduces Windows transparency effects. Close affected windows or sign in again; it is a desktop rendering choice, not an in-game latency fix.")
    ];
    public static TweakDefinition Get(Optimization operation)
    {
        if(Catalog.All.SingleOrDefault(x=>x.Id==operation.Id)!=operation) throw new InvalidOperationException("Unknown feature.");
        return All.SingleOrDefault(x=>x.Id==operation.Id)??throw new InvalidOperationException("This option has no On/Off switch.");
    }
    public static SettingValue GroupTarget(TweakDefinition definition,bool enabled)=>new(true,
        JsonSerializer.Serialize(definition.Members!.Select(_=>new SettingValue(true,enabled?definition.On:definition.Off)).ToArray()),"RegistryGroup");
    public static SettingValue[] ReadGroup(SettingValue state,TweakDefinition definition)
    {
        if(state.Kind!="RegistryGroup"||!state.Exists) throw new InvalidDataException("Invalid grouped setting backup.");
        var values=JsonSerializer.Deserialize<SettingValue[]>(state.Value??"")??throw new InvalidDataException("Invalid grouped setting backup.");
        if(values.Length!=definition.Members!.Length||values.Any(x=>x is null||x.Kind!="DWord"||x.Exists&&x.Value is not ("0" or "1"))) throw new InvalidDataException("Unexpected grouped setting value. No change made.");
        return values;
    }
}
