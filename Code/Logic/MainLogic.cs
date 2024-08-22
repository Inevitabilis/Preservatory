using UnityEngine;
using Menu;
using static PVStuffMod.StaticStuff;
using System.Collections.Generic;
using PVStuffMod.Logic.ROM_objects;
using ROM.RoomObjectService;
using PVStuffMod.Logic;
using PVStuff.Logic.ROM_objects;
using PVStuff.Logic;
using System.Diagnostics;
using BepInEx.Logging;
using PVStuff.Logic.ControllerParser;

namespace PVStuffMod;

internal static class MainLogic
{
    public static void Log(string message) => PVStuff.s_logger?.LogDebug(message);
    public static ManualLogSource logger => PVStuff.s_logger;
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
        SlugController.Hook();
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

        initialized = true;
    }

    private static void PathFinder_Reset(On.PathFinder.orig_Reset orig, PathFinder self, Room newRealizedRoom)
    {
        if (newRealizedRoom is null) Log((new StackTrace()).ToString());
        orig(self, newRealizedRoom);
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
        TypeOperator.RegisterType<NPC2Operator>();
        PVSlugNPC.ApplyHooks();
    }
    private static void MenuScene_BuildScene(On.Menu.MenuScene.orig_BuildScene orig, MenuScene self)
    {
        if(self.menu is SlugcatSelectMenu && self.sceneID != null && (devBuild || SaveManager.TryGetValue(self.menu.manager.rainWorld.options.saveSlot,self.sceneID.GetCharacterFromSelectScene())))
            self.sceneID = self.sceneID.GetCharacterFromSelectScene().GetSelectScreenSceneID();
        orig(self);
    }
}
