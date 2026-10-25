namespace FrameForge.Core;
public static class FeatureToggles
{
    public static bool Supports(Optimization operation)=>TweakDefinitions.All.Any(x=>x.Id==operation.Id);
    public static SettingValue Target(Optimization operation,bool enabled)
    {
        var definition=TweakDefinitions.Get(operation);
        return definition.Members is not null?TweakDefinitions.GroupTarget(definition,enabled):new(true,enabled?definition.On:definition.Off,definition.Kind);
    }
    public static bool? Enabled(Optimization operation,SettingValue state)
    {
        var definition=TweakDefinitions.Get(operation);
        if(definition.Members is not null)
        {
            var values=TweakDefinitions.ReadGroup(state,definition);
            if(values.All(x=>x.Exists&&x.Value==definition.On)) return true;
            if(values.All(x=>x.Exists&&x.Value==definition.Off)) return false;
            return null;
        }
        if(!state.Exists) return null;
        if(operation.Id=="mouse-acceleration")
        {
            var parts=(state.Value??"").Split(',');
            if(state.Kind!="Mouse"||parts.Length!=3||!parts.All(x=>int.TryParse(x,out var n)&&n>=0&&n<=20)||int.Parse(parts[2])>2) throw new InvalidDataException("Mouse settings could not be read safely.");
            return parts[2]!="0";
        }
        if(operation.Id=="quick-menus"&&state.Kind=="Milliseconds"&&uint.TryParse(state.Value,out var delay)&&delay<=10000) return delay<=100;
        if(state==Target(operation,true)) return true;
        if(state==Target(operation,false)) return false;
        if(state.Kind=="DWord"&&definition.Allows(state.Value))return null;
        throw new InvalidDataException("This setting has an unexpected value. No changes will be made.");
    }
}
