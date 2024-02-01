using Menu;
using System.Collections.Generic;
using static PVStuffMod.NonPlayerCenteredLogic;
using static MoreSlugcats.MoreSlugcatsEnums;

namespace PVStuffMod;

public static class StaticStuff
{
    public const string tail = "EscEnd";
    internal const string stasisRoomName = "SWIM";
    internal static HashSet<RainWorldGame> EscapismEnding = new();
    public class EscapismEndingSlugcatScreen : ExtEnum<EscapismEndingSlugcatScreen>
    {
        public EscapismEndingSlugcatScreen(string value, bool register = false) : base(value, register) { }
        public static MenuScene.SceneID Artificer = new(nameof(Artificer) + tail, false);
        public static MenuScene.SceneID Gourmand = new(nameof(Gourmand) + tail, false);
        public static MenuScene.SceneID Spearmaster = new(nameof(Spearmaster) + tail, false);
        public static MenuScene.SceneID Survivor = new(nameof(Survivor) + tail, false);
        public static MenuScene.SceneID Monk = new(nameof(Monk) + tail, false);
        public static MenuScene.SceneID Rivulet = new(nameof(Rivulet) + tail, false);
        public static MenuScene.SceneID Hunter = new(nameof(Hunter) + tail, false);
        
    }

    internal static MenuScene.SceneID GetSelectScreenSceneID(this SlugcatStats.Name name)
    {
        if (name == SlugcatStats.Name.Yellow) return EscapismEndingSlugcatScreen.Monk;
        if (name == SlugcatStats.Name.Red) return EscapismEndingSlugcatScreen.Hunter;
        if (name == SlugcatStatsName.Spear) return EscapismEndingSlugcatScreen.Spearmaster;
        if (name == SlugcatStatsName.Artificer) return EscapismEndingSlugcatScreen.Artificer;
        if (name == SlugcatStatsName.Gourmand) return EscapismEndingSlugcatScreen.Gourmand;
        if (name == SlugcatStatsName.Rivulet) return EscapismEndingSlugcatScreen.Rivulet;
        return EscapismEndingSlugcatScreen.Survivor;
    }
    

}
