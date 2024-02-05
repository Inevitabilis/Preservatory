using Menu;
using System.Collections.Generic;
using MoreSlugcats;
using static SlugcatStats.Name;
using static MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName;
using static MoreSlugcats.MoreSlugcatsEnums.MenuSceneID;
using static Menu.MenuScene.SceneID;

namespace PVStuffMod;

public static class StaticStuff
{
    public const bool devBuild = true;
    public const string head = "Preservatory_";
    public const string tail = "EscEnd";
    internal const string stasisRoomName = "SWIM";
    internal static HashSet<SlugcatStats.Name> EscapismEnding = new();
    public class EscapismEndingSlugcatScreen : ExtEnum<EscapismEndingSlugcatScreen>
    {
        public EscapismEndingSlugcatScreen(string value, bool register = false) : base(value, register) { }
        public static MenuScene.SceneID Artificer = new(head + nameof(Artificer) + tail, false);
        public static MenuScene.SceneID Gourmand = new(head +nameof(Gourmand) + tail, false);
        public static MenuScene.SceneID Spearmaster = new(head + nameof(Spearmaster) + tail, false);
        public static MenuScene.SceneID Survivor = new(head + nameof(Survivor) + tail, false);
        public static MenuScene.SceneID Monk = new(head + nameof(Monk) + tail, false);
        public static MenuScene.SceneID Rivulet = new(head + nameof(Rivulet) + tail, false);
        public static MenuScene.SceneID Hunter = new(head + nameof(Hunter) + tail, false);

    }
    internal static Dictionary<MenuScene.SceneID, SlugcatStats.Name> sceneNameMap = new()
    {
        { Ghost_Yellow, Yellow },
        { Slugcat_Yellow, Yellow },
        { AltEnd_Monk, Yellow },
        { Ghost_Red, Red },
        { Slugcat_Red, Red },
        { Slugcat_Gourmand, Gourmand },
        { End_Gourmand, Gourmand },
        { AltEnd_Gourmand, Gourmand },
        { AltEnd_Gourmand_Full, Gourmand },
        { End_Spear, MoreSlugcatsEnums.SlugcatStatsName.Spear },
        { AltEnd_Spearmaster, MoreSlugcatsEnums.SlugcatStatsName.Spear },
        { Slugcat_Spear, MoreSlugcatsEnums.SlugcatStatsName.Spear },
        { Slugcat_Artificer, Artificer },
        { Slugcat_Artificer_Robo, Artificer },
        { Slugcat_Artificer_Robo2, Artificer },
        { AltEnd_Artificer_Portrait, Artificer },
        { End_Artificer, Artificer },
        { End_Rivulet, Rivulet },
        { AltEnd_Rivulet, Rivulet },
        { AltEnd_Rivulet_Robe, Rivulet },
        { Slugcat_Saint, Saint },
        { SaintMaxKarma, Saint },
        { End_Saint, Saint },
        { Slugcat_White, SlugcatStats.Name.White }
    };
    internal static Dictionary<SlugcatStats.Name, MenuScene.SceneID> nameSceneMap = new()
    {
        { Yellow, EscapismEndingSlugcatScreen.Monk },
        { Red, EscapismEndingSlugcatScreen.Hunter },
        { Gourmand, EscapismEndingSlugcatScreen.Gourmand },
        { MoreSlugcatsEnums.SlugcatStatsName.Spear, EscapismEndingSlugcatScreen.Spearmaster },
        { Artificer, EscapismEndingSlugcatScreen.Artificer },
        { Rivulet, EscapismEndingSlugcatScreen.Rivulet }
    };

    static internal SlugcatStats.Name GetCharacterFromSelectScene(this MenuScene.SceneID sceneID)
    {
        return sceneNameMap.TryGetValue(sceneID, out var name) ? name : White;
    }
    static internal MenuScene.SceneID GetSelectScreenSceneID(this SlugcatStats.Name character)
    {
        return nameSceneMap.TryGetValue(character, out var sceneID) ? sceneID : EscapismEndingSlugcatScreen.Survivor;
    }
}
