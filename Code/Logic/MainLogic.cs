﻿using UnityEngine;
using Menu;
using static PVStuffMod.StaticStuff;
using System;
using System.Collections.Generic;
using System.CodeDom;
using PVStuffMod.Logic.ROM_objects;
using ROM.RoomObjectService;
using System.IO;
using PVStuffMod.Logic;
using System.Linq;
using PVStuff.Logic.ROM_objects;
using PVStuff.Logic;

namespace PVStuffMod;

internal static class MainLogic
{
    public static void Log(string message) => PVStuff.s_logger?.LogDebug(message);
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
    }
    private static void MenuScene_BuildScene(On.Menu.MenuScene.orig_BuildScene orig, MenuScene self)
    {
        if(self.menu is SlugcatSelectMenu && self.sceneID != null && (devBuild || SaveManager.TryGetValue(self.menu.manager.rainWorld.options.saveSlot,self.sceneID.GetCharacterFromSelectScene())))
            self.sceneID = self.sceneID.GetCharacterFromSelectScene().GetSelectScreenSceneID();
        orig(self);
    }
}
