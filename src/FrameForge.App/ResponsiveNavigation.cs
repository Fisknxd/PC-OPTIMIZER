using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using FrameForge.Core;

namespace FrameForge.App;

public sealed partial class MainWindow
{
    readonly Dictionary<string,double> scrollPositions=[];
    readonly Dictionary<string,Border> tweakCards=[];
    readonly List<Task> featureReads=[];
    CancellationTokenSource pageReads=new();
    int pageGeneration;
    TextBlock? tweakResults,tweakContext;
    Button? downloadLimitsLink;
    readonly HashSet<string> tweakAttention=[];
    string tweakFilter="All states";
    readonly Dictionary<string,string> tweakStates=[];
    SettingReadCoordinator settingReads=null!;
    bool IsCurrent(int generation)=>generation==pageGeneration&&!pageReads.IsCancellationRequested;
    void BeginPage()
    {
        pageReads.Cancel();pageReads.Dispose();pageReads=new();pageGeneration++;
        featureReads.Clear();tweakCards.Clear();tweakStates.Clear();tweakAttention.Clear();tweakResults=null;tweakContext=null;downloadLimitsLink=null;
    }
    Task<SettingValue> ReadSetting(Optimization operation,bool fresh=false)=>settingReads.Read(operation,pageReads.Token,fresh);
    Task<T> SettingsWork<T>(Func<Task<T>> work)=>system is FrameForge.Windows.WindowsSettings?Task.Run(work):work();
    void RefreshSettings() {settingReads.Invalidate();Render();}
    void ApplySearch()
    {
        searchDelay.Stop();query=search.Text.Trim();
        if(page!="Optimize") {if(query.Length>0)Go("Optimize");return;}
        // Search changes visibility only: keep controls, state, focus and in-flight reads.
        FilterTweaks();
    }
    void FilterTweaks()
    {
        if(tweakResults==null)return;
        var found=Catalog.Search(query,"All",vm.Hardware).Select(x=>x.Id).ToHashSet();int visible=0;
        foreach(var pair in tweakCards)
        {
            var operation=Catalog.All.Single(x=>x.Id==pair.Key);
            bool category=page=="Quality of Life"?desktopSection=="All desktop"||TweakDefinitions.Get(operation).Group==desktopSection:
                !string.IsNullOrWhiteSpace(query)||MatchesFeatureSection(operation);
            var state=tweakStates.GetValueOrDefault(pair.Key,"Checking");
            bool matchesState=tweakFilter=="All states"||tweakFilter=="On"&&state=="On"||tweakFilter=="Off"&&state=="Off"||tweakFilter=="Needs review"&&(state is "Unavailable" or "Custom"||tweakAttention.Contains(pair.Key));
            var show=found.Contains(pair.Key)&&category&&(matchesState||state=="Checking");
            pair.Value.Visibility=show?Visibility.Visible:Visibility.Collapsed;if(show)visible++;
        }
        if(tweakContext!=null){tweakContext.Text=query.Length>0?"Searching across all tweaks. Clear search to return to your category.":featureSection switch {"Browser memory"=>"After changing a browser option, use Check in browser → Reload policies. Personal Microsoft-account Edge profiles ignore sleeping-tab policies.","For gaming"=>"Want a starting selection? Open Low-end PC, or review individual switches below.","Power checks"=>"Compare one plugged-in power change at a time. More power can mean more heat; originals stay in Recovery.",_=>""};if(page=="Quality of Life")tweakContext.Text="";tweakContext.Visibility=tweakContext.Text.Length==0?Visibility.Collapsed:Visibility.Visible;}
        if(downloadLimitsLink!=null)downloadLimitsLink.Visibility=featureSection=="Downloads"&&query.Length==0?Visibility.Visible:Visibility.Collapsed;
        tweakResults.Text=visible==0?"No matches. Clear search or choose another filter.":$"{visible} {(query.Length>0?"search results":visible==1?"choice":"choices")} · switches show the current saved setting";
    }
    bool MatchesFeatureSection(Optimization operation)=>featureSection switch
    {
        "Windows controls"=>!FeatureToggles.Supports(operation)&&operation.Kind is not (ActionKind.Service or ActionKind.PowerSetting),
        "Power checks"=>operation.Kind is ActionKind.PowerSetting or ActionKind.PowerPlan,
        "For gaming"=>PerformanceTweaks.Relevant(operation),
        _=>FeatureToggles.Supports(operation)&&(featureSection is "All tweaks" or "Feature toggles"||TweakDefinitions.Get(operation).Group==featureSection)
    };
    void SetTweakState(string id,string state,bool needsReview=false) {tweakStates[id]=state;if(needsReview)tweakAttention.Add(id);else tweakAttention.Remove(id);FilterTweaks();}
    void SavePreferences(Preferences updated){store.Save("preferences.json",updated);preferences=updated;}
    void UpdateNavigation()
    {
        foreach(var entry in navigation)entry.Value.Tag=entry.Key==page?"Selected":"";
    }
}
