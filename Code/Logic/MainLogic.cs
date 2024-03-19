using UnityEngine;
using Menu;
using static PVStuffMod.StaticStuff;
using System;
using System.Collections.Generic;
using System.CodeDom;
using PVStuffMod.Logic.ROM_objects;
using ROM.RoomObjectService;
using System.IO;
using PVStuffMod.Logic;

namespace PVStuffMod;

internal static class MainLogic
{
    public static List<IReceiveWorldTicks> globalUpdateReceivers;
    public static InternalSoundController internalSoundController;
    static MainLogic()
    {
        internalSoundController = new();
        globalUpdateReceivers = [internalSoundController];
    }
    static internal void Startup()
    {
        //Scene related changes
        On.Menu.MenuScene.BuildScene += MenuScene_BuildScene;
        PVEnums.Melody.Register();
        //for things that do not receive local updates
        On.RainWorldGame.Update += static (orig, self) =>
        {
            orig(self);
            GarbageCollector(globalUpdateReceivers, self.cameras);
            globalUpdateReceivers.ForEach(x =>
            {
                if(x is IDrawable drawable && !self.cameras[0].spriteLeasers.Exists(x => x.drawableObject == drawable)) self.cameras[0].NewObjectInRoom(drawable);
            });
            globalUpdateReceivers.ForEach(x => x.Update());
        };
        RegisterROMObjects();
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
    }
    private static void MenuScene_BuildScene(On.Menu.MenuScene.orig_BuildScene orig, MenuScene self)
    {
        if(self.menu is SlugcatSelectMenu && self.sceneID != null && (devBuild || EscapismEnding.Contains(self.sceneID.GetCharacterFromSelectScene())))
            self.sceneID = self.sceneID.GetCharacterFromSelectScene().GetSelectScreenSceneID();
        orig(self);
    }
}
