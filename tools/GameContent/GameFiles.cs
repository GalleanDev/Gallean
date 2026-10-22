using Gallean.Configuration;

namespace GameContent;

public sealed record GameFiles(string Root)
{
    public static GameFiles Original { get; } = new(AppDirectories.OriginalAssets);

    public string ImagesRoot => Path.Combine(Root, "Imgs");
    public string InterfaceRoot => Path.Combine(Root, "Interf");
    public string SoundsRoot => Path.Combine(Root, "Sounds");
    public string MusicRoot => Path.Combine(Root, "Music");
    public string BriefingRoot => Path.Combine(Root, "Briefing");
    public string ScenarioDataRoot => Path.Combine(Root, "ScenData");
}
