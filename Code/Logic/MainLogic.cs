using UnityEngine;
using Menu;
using static PVStuffMod.StaticStuff;
using System.Collections.Generic;
using PVStuffMod.Logic.ROM_objects;
using ROM.RoomObjectService;
using PVStuffMod.Logic;
using PVStuff.Logic.ROM_objects;
using PVStuff.Logic;

namespace PVStuffMod;

internal static class MainLogic
{
    public static void Log(string message) => PVStuff.s_logger?.LogDebug(message);
    public static void DebugLog(string message)
    {
        if(StaticStuff.devBuild) Log(message);
    }
    public static List<IReceiveWorldTicks> globalUpdateReceivers;
    public static InternalSoundController internalSoundController;
    static bool initialized = false;
    static MainLogic()
    {
        internalSoundController = new();
        globalUpdateReceivers = [internalSoundController];
    }
    static internal void Startup()
    {
        
        if (initialized) return;
        //Scene related changes
        On.Menu.MenuScene.BuildScene += MenuScene_BuildScene;
        PVEnums.Melody.Register();
        PVEnums.NPCBehaviour.Register();
        //for things that do not receive local updates
        On.RainWorldGame.Update += static (orig, self) =>
        {
            orig(self);
            GarbageCollector(globalUpdateReceivers, self.cameras); //ScreenFlasher
            globalUpdateReceivers.ForEach(x =>
            {
                if (x is ScreenFlasher flasher && !self.cameras[0].spriteLeasers.Exists(x => x.drawableObject == flasher)) self.cameras[0].NewObjectInRoom(flasher);
            });
            globalUpdateReceivers.ForEach(x => x.Update());
            if (Input.GetKey(KeyCode.Backspace)) StaticStuff.logging = true;
        };
        RegisterROMObjects();
        SaveManager.ApplyHooks();
        On.PathFinder.CheckConnectionCost += PathFinder_CheckConnectionCost;
        //On.Player.Update += static (orig, self, eu) =>
        //{
        //    orig(self, eu);
        //    Log($"Slugcat update, class: {self.SlugCatClass.ToString()}, chunk rads: {self.bodyChunks[0].rad} and {self.bodyChunks[1].rad}, the distance between them: {(self.bodyChunks[0].pos - self.bodyChunks[1].pos).magnitude} (positions are {self.bodyChunks[0].pos} and {self.bodyChunks[1].pos}");
        //};

        initialized = true;
    }
#warning remove in the future
    private static PathCost PathFinder_CheckConnectionCost(On.PathFinder.orig_CheckConnectionCost orig, PathFinder self, PathFinder.PathingCell start, PathFinder.PathingCell goal, MovementConnection connection, bool followingPath)
    {
        PathCost pathCost = new PathCost(100f * (float)connection.distance, PathCost.Legality.IllegalConnection);
        Log($"self is {self}");
        Log($"self.realizedRoom is {(self.realizedRoom == null ? "NULL" : self.realizedRoom)}");
        Log($"aimap is {self.realizedRoom.aimap}");
        if (!self.realizedRoom.aimap.IsConnectionAllowedForCreature(connection, self.creature.creatureTemplate))
        {
            pathCost += new PathCost(0f, PathCost.Legality.IllegalConnection);
        }
        else
        {
            PathCost pathCost2 = self.creatureType.ConnectionResistance(connection.type);
            if (pathCost2.Considerable)
            {
                PathCost pathCost3 = self.CoordinateCost(goal.worldCoordinate);
                if (pathCost3.Considerable && self.CoordinateCost(start.worldCoordinate).Considerable)
                {
                    pathCost2.resistance *= (float)connection.distance;
                    pathCost = pathCost2 + pathCost3 + new PathCost(0f, self.CoordinateCost(start.worldCoordinate).legality);
                }
            }
        }
        if (start.worldCoordinate.room != goal.worldCoordinate.room)
        {
            pathCost += self.creatureType.ConnectionResistance(MovementConnection.MovementType.BetweenRooms);
        }
        else if (connection.type == MovementConnection.MovementType.NPCTransportation)
        {
            pathCost += self.creatureType.NPCTravelAversion;
        }
        else if (connection.type == MovementConnection.MovementType.ShortCut)
        {
            pathCost += self.creatureType.shortcutAversion;
        }
        if (pathCost.Considerable && self.InThisRealizedRoom(connection.destinationCoord) && self.InThisRealizedRoom(connection.startCoord))
        {
            pathCost = self.AI.TravelPreference(connection, pathCost);
        }
        return pathCost;
    }

    private static void GarbageCollector(List<IReceiveWorldTicks> list, RoomCamera[] cameras)
    {
        for(int i = 0; i < list.Count; i++) 
        {
            if (list[i].SlatedForDeletion)
            {
                if (list[i] is IDrawable)
                {
                    cameras[0].spriteLeasers.ForEach(sleaser =>
                    {
                        if (sleaser.drawableObject == list[i]) sleaser.CleanSpritesAndRemove();
                    });
                }
                list.RemoveAt(i);
            }
        }
    }
    static internal void RegisterROMObjects()
    {
        TypeOperator.RegisterType<ExposedSoundControllerOperator>();
        TypeOperator.RegisterType<DreamEnderOperator>();
        TypeOperator.RegisterType<VatSceneOperator>();
        TypeOperator.RegisterType<RedIllnessOperator>();
        TypeOperator.RegisterType<HLLOperator>();
        TypeOperator.RegisterType<PVSlugNPCOperator>();
        PVSlugNPC.ApplyHooks();
    }
    private static void MenuScene_BuildScene(On.Menu.MenuScene.orig_BuildScene orig, MenuScene self)
    {
        if(self.menu is SlugcatSelectMenu && self.sceneID != null && (devBuild || SaveManager.TryGetValue(self.menu.manager.rainWorld.options.saveSlot,self.sceneID.GetCharacterFromSelectScene())))
            self.sceneID = self.sceneID.GetCharacterFromSelectScene().GetSelectScreenSceneID();
        orig(self);
    }
}
