namespace FrameForge.Core;

public sealed record GraphicsStep(string Title,string Description);
public sealed record PotatoProfile(string Game,string Summary,string Launcher,string Source,GraphicsStep[] Steps);
public sealed record PotatoProgress(string Notes,HashSet<int> Done);
public static class PotatoProfiles
{
    public static IReadOnlyList<PotatoProfile> All {get;}=[
        new("Fortnite","Use the game's own Performance mode for simpler graphics and less rendering work.","com.epicgames.launcher://","https://www.epicgames.com/help/c-1/a20197720?lang=en-US",[
            new("Save your current settings","Take screenshots of your Video settings before starting. Keep them to restore your exact choices later."),
            new("Switch to Performance mode","In Fortnite: Settings → Video → Rendering Mode → Performance / Lower Graphical Fidelity. Apply, then restart the game."),
            new("Choose Low quality","Use the Low quality preset and the lowest texture and mesh options offered in the menu. Names depend on the current rendering mode."),
            new("Lower the rendered resolution","Start at 100% 3D Resolution, then try 80% and 70% if the game is still GPU-limited. Lower values make distant objects harder to see."),
            new("Remove optional high-resolution textures","In Epic Games Launcher: Library → Fortnite menu → Options → uncheck High Resolution Textures → Apply. The launcher manages this optional download."),
            new("Check input delay and smoothness","Try V-Sync Off if tearing is acceptable. Use the game's NVIDIA Reflex option if supported. Compare the same scene and choose an FPS cap your PC can keep steadily.")
        ]),
        new("Rust","Use the lowest graphics preset and the menu's shadow and resolution controls. Terrain remains part of the game.","steam://nav/games/details/252490","https://rust.facepunch.com/news/shipshape",[
            new("Save your current settings","Take screenshots of Graphics and Screen settings before starting. Keep them to restore your exact choices later."),
            new("Choose the lowest quality preset","In Rust's Graphics menu, choose the lowest Graphics Quality preset offered. It may be called Potato or Super Potato; names change between versions."),
            new("Lower shadows through the menu","Set Shadow Quality to the lowest preset offered. Current Rust uses a Shadow Quality preset; use the menu's allowed minimum for extra shadow lights."),
            new("Reduce extra effects","Turn off optional motion blur and depth-of-field effects if shown. Keep visibility and readability in mind when choosing other quality options."),
            new("Try a smaller resolution","In Screen settings, try 1600×900 or 1280×720 if your display offers it. This cuts rendering work but makes the image less sharp. Keep the usual aspect ratio."),
            new("Compare in the same place","Check frame rate in the same scene after the game has warmed up. Use the game's supported latency option if available. Crowded servers and CPU limits may still restrict FPS.")
        ])
    ];
    public static PotatoProgress Validate(PotatoProfile profile,PotatoProgress progress)
    {
        if(!All.Contains(profile)||progress.Notes.Length>12000||progress.Done.Any(i=>i<0||i>=profile.Steps.Length)) throw new InvalidDataException("Invalid graphics checklist.");return progress;
    }
}
