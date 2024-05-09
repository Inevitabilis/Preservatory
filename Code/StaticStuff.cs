using Menu;
using System.Collections.Generic;
using MoreSlugcats;
using static SlugcatStats.Name;
using static MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName;
using static MoreSlugcats.MoreSlugcatsEnums.MenuSceneID;
using static Menu.MenuScene.SceneID;
using UnityEngine;
using System.Runtime.InteropServices;
using PVStuffMod.Logic;
using IL.RWCustom;
using Newtonsoft.Json.Serialization;
using System;

namespace PVStuffMod;

public static class PVEnums
{
    const string head = "Preservatory_";
    const string tail = "EscEnd";
    static PVEnums()
    {
        Artificer = new(head + nameof(Artificer) + tail);
        Gourmand = new(head + nameof(Gourmand) + tail);
        Spearmaster = new(head + nameof(Spearmaster) + tail);
        Survivor = new(head + nameof(Survivor) + tail);
        Monk = new(head + nameof(Monk) + tail);
        Rivulet = new(head + nameof(Rivulet) + tail);
        Hunter = new(head + nameof(Hunter) + tail);
    }
    public static MenuScene.SceneID Artificer;
    public static MenuScene.SceneID Gourmand;
    public static MenuScene.SceneID Spearmaster;
    public static MenuScene.SceneID Survivor;
    public static MenuScene.SceneID Monk;
    public static MenuScene.SceneID Rivulet;
    public static MenuScene.SceneID Hunter;


    public static class Melody
    {
        public static void Register()
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
}
public static class StaticStuff
{
    public static bool logging = false;
    public const int TicksPerSecond = 40;
    public const bool devBuild = true;
    public static short[] playerColorableSpritesIndices = [0,1,2,3,4,5,6,7,8,9];

    public static Vector2 centerOfOneScreenRoom = new(482, 349);
    public static void TeleportCreaturesIntoRoom(this List<AbstractCreature> creatures, World world, RainWorldGame game, Destination d)
    {
        AbstractRoom room = world.GetAbstractRoom(d.roomName);
        room.RealizeRoom(world, game);
        while (world.loadingRooms.Count > 0)
        {

                for (int j = world.loadingRooms.Count - 1; j >= 0; j--)
                {
                    if (world.loadingRooms[j].done)
                    {
                        world.loadingRooms.RemoveAt(j);
                    }
                    else
                    {
                        world.loadingRooms[j].Update();
                    }
                }
            
        }
        RWCustom.IntVector2 middleOfRoom = new(room.realizedRoom.TileWidth / 2 + 10, room.realizedRoom.TileHeight / 2);
        WorldCoordinate destination = RWCustom.Custom.MakeWorldCoordinate(room.realizedRoom.GetTilePosition(d.position), room.index);
        creatures.ForEach(creature => creature.pos = destination);
        creatures.ForEach(player =>
        {
            player.RealizeInRoom();
            player.realizedCreature.mainBodyChunk.pos = d.position;
        });
        room.world.game.roomRealizer.followCreature = creatures[0];
        game.cameras[0].MoveCamera(room.realizedRoom, 0);
        game.cameras[0].virtualMicrophone.AllQuiet();
        game.cameras[0].virtualMicrophone.NewRoom(game.cameras[0].room);


    }
    public static ScreenFlasher RegisterScreenFlasher(RoomCamera rCam)
    {
        ScreenFlasher screenFlasher = new();
        rCam.NewObjectInRoom(screenFlasher);
        MainLogic.globalUpdateReceivers.Add(screenFlasher);
        return screenFlasher;
    }
    public struct Destination
    {
        public string roomName;
        public Vector2 position;
    }
}
public static class PVMaps
{
    static PVMaps()
    {
        nameSceneMap = new()
        {
            { Yellow, PVEnums.Monk },
            { Red, PVEnums.Hunter },
            { MoreSlugcatsEnums.SlugcatStatsName.Gourmand, PVEnums.Gourmand },
            { MoreSlugcatsEnums.SlugcatStatsName.Spear, PVEnums.Spearmaster },
            { MoreSlugcatsEnums.SlugcatStatsName.Artificer, PVEnums.Artificer },
            { MoreSlugcatsEnums.SlugcatStatsName.Rivulet, PVEnums.Rivulet },
            { White, PVEnums.Survivor }
        };
        sceneNameMap = new()
        {
            { Ghost_Yellow, Yellow },
            { Slugcat_Yellow, Yellow },
            { AltEnd_Monk, Yellow },
            { Ghost_Red, Red },
            { Slugcat_Red, Red },
            { Slugcat_Gourmand, MoreSlugcatsEnums.SlugcatStatsName.Gourmand },
            { End_Gourmand, MoreSlugcatsEnums.SlugcatStatsName.Gourmand },
            { AltEnd_Gourmand, MoreSlugcatsEnums.SlugcatStatsName.Gourmand },
            { AltEnd_Gourmand_Full, MoreSlugcatsEnums.SlugcatStatsName.Gourmand },
            { End_Spear, MoreSlugcatsEnums.SlugcatStatsName.Spear },
            { AltEnd_Spearmaster, MoreSlugcatsEnums.SlugcatStatsName.Spear },
            { Slugcat_Spear, MoreSlugcatsEnums.SlugcatStatsName.Spear },
            { Slugcat_Artificer, MoreSlugcatsEnums.SlugcatStatsName.Artificer },
            { Slugcat_Artificer_Robo, MoreSlugcatsEnums.SlugcatStatsName.Artificer },
            { Slugcat_Artificer_Robo2, MoreSlugcatsEnums.SlugcatStatsName.Artificer },
            { AltEnd_Artificer_Portrait, MoreSlugcatsEnums.SlugcatStatsName.Artificer },
            { End_Artificer, MoreSlugcatsEnums.SlugcatStatsName.Artificer },
            { Slugcat_Rivulet, MoreSlugcatsEnums.SlugcatStatsName.Rivulet },
            { Slugcat_Rivulet_Cell, MoreSlugcatsEnums.SlugcatStatsName.Rivulet },
            { End_Rivulet, MoreSlugcatsEnums.SlugcatStatsName.Rivulet },
            { AltEnd_Rivulet, MoreSlugcatsEnums.SlugcatStatsName.Rivulet },
            { AltEnd_Rivulet_Robe, MoreSlugcatsEnums.SlugcatStatsName.Rivulet },
            { Slugcat_Saint, Saint },
            { SaintMaxKarma, Saint },
            { End_Saint, Saint },
            { Slugcat_White, SlugcatStats.Name.White }
        };
        dreamRoom = new()
        {
            { Yellow, new() {roomName = "PV_DREAM_TREE03", position = new(306f, 269f) } },
            { White, new() {roomName = "PV_DREAM_TREE03", position = new(298.7f, 269.0f) } },
            { Gourmand, new() {roomName = "PV_DREAM_TREE03", position = new(298.7f, 269.0f) } },
            { Artificer, new() {roomName = "PV_DREAM_ARTI", position = new(534f, 84f)} },
            { Red, new() { roomName = "PV_DREAM_RED", position = new(4962, 1024) } }
        };
        endRoom = new()
        {
            { Red, new() { roomName = "PV_END_RED", position = new(482, 349) } }
        };


    }

    #region Maps
    internal static Dictionary<MenuScene.SceneID, SlugcatStats.Name> sceneNameMap;

    internal static Dictionary<SlugcatStats.Name, MenuScene.SceneID> nameSceneMap;

    internal static Dictionary<SlugcatStats.Name, StaticStuff.Destination> dreamRoom;

    internal static Dictionary<SlugcatStats.Name, StaticStuff.Destination> endRoom;
    #endregion

    #region Methods
    static internal SlugcatStats.Name GetCharacterFromSelectScene(this MenuScene.SceneID sceneID)
    {
        return sceneNameMap.TryGetValue(sceneID, out var name) ? name : White;
    }
    static internal MenuScene.SceneID GetSelectScreenSceneID(this SlugcatStats.Name character)
    {
        return nameSceneMap.TryGetValue(character, out var sceneID) ? sceneID : PVEnums.Survivor;
    }
    static internal StaticStuff.Destination GetDreamDestination(this SlugcatStats.Name character)
    {
        return dreamRoom.TryGetValue(character, out var roomName) ? roomName : new() { roomName = "PV_END", position = new(0,0) }; //yes the player actually needs to not be noticed, with hunter long legs replacing it
    }
    static internal StaticStuff.Destination GetEndDestination(this SlugcatStats.Name character)
    {
        return endRoom.TryGetValue(character, out var roomName) ? roomName : new() { roomName = "PV_END", position = StaticStuff.centerOfOneScreenRoom };
    }
    #endregion
}
public static class ROMUtils
{
    public static bool PositionWithinPoly(Vector2[] Polygon, Vector2 point)
    {
            bool result = true;
        for (int i = 0; i < Polygon.Length; i++)
        {
            if (IsAboveEquationByTwoPoints(Polygon[i], Polygon[(i + 1) % Polygon.Length], point) 
                != IsAboveEquationByTwoPoints(Polygon[i], Polygon[(i + 1) % Polygon.Length], Polygon[(i+2)%Polygon.Length])) result = false;
        }
        return result;
    }
    private static bool IsAboveEquationByTwoPoints(Vector2 point1, Vector2 point2, Vector2 v)
    {
        bool isAboveLine = (point1.x - v.x) * (point2.y - point1.y) <= (point1.y - v.y) * (point2.x - point1.x);
        return isAboveLine;
    }
}
public interface IReceiveWorldTicks
{
    public void Update();
    public bool SlatedForDeletion { get; }
}
