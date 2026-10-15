namespace FrameForge.Core;
public sealed record FeatureState(bool? Enabled,string Label,string? Error=null)
{
    public static FeatureState From(Optimization op,SettingValue state)
    {
        if(!op.CanToggle) return new(null,"Manual control","This feature has no automatic switch.");
        if(!state.Exists) return new(null,"Windows default");
        if(state.Kind!="DWord") return new(null,"Needs review","Unexpected registry type; no changes made.");
        if(state.Value==op.Value) return new(true,"On");
        if(state.Value==op.DisabledValue) return new(false,"Off");
        return new(null,"Needs review","Unrecognized setting value; inspect it before changing it.");
    }
}
