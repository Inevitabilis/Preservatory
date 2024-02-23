using Menu;
using System.Collections.Generic;
using MoreSlugcats;
using static SlugcatStats.Name;
using static MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName;
using static MoreSlugcats.MoreSlugcatsEnums.MenuSceneID;
using static Menu.MenuScene.SceneID;
using UnityEngine;

namespace PVStuffMod;

public static class StaticStuff
{
    public const int TicksPerSecond = 40;
    public const bool devBuild = true;
    public const string head = "Preservatory_";
    public const string tail = "EscEnd";
    internal const string stasisRoomName = "SWIM";
    internal static HashSet<SlugcatStats.Name> EscapismEnding = new();
    public static void LoadEnums()
    {
        Melody.LoadMusicEnums();
        EscapismEndingSlugcatScreen.LoadSlugcatSceneEnums();
    }
    public static class EscapismEndingSlugcatScreen
    {
        public static void LoadSlugcatSceneEnums()
        {
            Artificer = new(head + nameof(Artificer) + tail);
            Gourmand = new(head + nameof(Gourmand) + tail);
            Spearmaster = new(head + nameof(Spearmaster) + tail);
            Survivor = new(head + nameof(Survivor) + tail);
            Monk = new(head + nameof(Monk) + tail);
            Rivulet = new(head + nameof(Rivulet) + tail);
            Hunter = new(head + nameof(Hunter) + tail);
        }

        public static MenuScene.SceneID? Artificer;
        public static MenuScene.SceneID? Gourmand;
        public static MenuScene.SceneID? Spearmaster;
        public static MenuScene.SceneID? Survivor;
        public static MenuScene.SceneID? Monk;
        public static MenuScene.SceneID? Rivulet;
        public static MenuScene.SceneID? Hunter;
    }
    public static class Melody
    {
        public static void LoadMusicEnums()
        {
            approach0 = new(head + nameof(approach0), true);
            approach1 = new(head + nameof(approach1), true);
            approach2 = new(head + nameof(approach2), true);
            approach3 = new(head + nameof(approach3), true);
        }
        public static SoundID? approach0;
        public static SoundID? approach1;
        public static SoundID? approach2;
        public static SoundID? approach3;
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
        { Slugcat_Rivulet, Rivulet },
        { Slugcat_Rivulet_Cell, Rivulet },
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
#pragma warning disable CS8604 // Possible null reference argument.
        { Yellow, EscapismEndingSlugcatScreen.Monk },
        { Red, EscapismEndingSlugcatScreen.Hunter },
        { Gourmand, EscapismEndingSlugcatScreen.Gourmand },
        { MoreSlugcatsEnums.SlugcatStatsName.Spear, EscapismEndingSlugcatScreen.Spearmaster },
        { Artificer, EscapismEndingSlugcatScreen.Artificer },
        { Rivulet, EscapismEndingSlugcatScreen.Rivulet },
        { White, EscapismEndingSlugcatScreen.Survivor }
#pragma warning restore CS8604 // Possible null reference argument.
    };

    static internal SlugcatStats.Name GetCharacterFromSelectScene(this MenuScene.SceneID sceneID)
    {
        return sceneNameMap.TryGetValue(sceneID, out var name) ? name : White;
    }
    static internal MenuScene.SceneID GetSelectScreenSceneID(this SlugcatStats.Name character)
    {
#pragma warning disable CS8603 // Possible null reference return.
        return nameSceneMap.TryGetValue(character, out var sceneID) ? sceneID : EscapismEndingSlugcatScreen.Survivor;
#pragma warning restore CS8603 // Possible null reference return.
    }
}
public static class ROMUtils
{
    private enum EquationPosition
    {
        above,
        below
    }
    public static bool PositionWithinPoly(Vector2[] Polygon, Vector2 point)
    {
        for (int i = 0; i < Polygon.Length; i++)
        {
            Vector2 currentline = Polygon[(i + 1) % Polygon.Length] - Polygon[i];
            Vector2 nextdirline = Polygon[(i + 2) % Polygon.Length] - Polygon[i];
            EquationPosition whereToCheck = nextdirline.GetAngle() - currentline.GetAngle() > 0 ? EquationPosition.below : EquationPosition.above;
            if (!InAreaForTwoPoints(Polygon[i], Polygon[(i + 1) % Polygon.Length], point, whereToCheck)) return false;
        }
        return true;
    }
    private static bool InAreaForTwoPoints(Vector2 point1, Vector2 point2, Vector2 v, EquationPosition equationPosition)
    {
        if (equationPosition == EquationPosition.above) return (v.x - point1.x) / (point2.x - point1.x) <= (v.y - point1.y) / (point2.y - point1.y);
        else return (v.x - point1.x) * (point2.y - point1.y) > (v.y - point1.y) * (point2.x - point1.x);
    }
}
public interface IReceiveWorldTicks
{
    public void Update();
}
