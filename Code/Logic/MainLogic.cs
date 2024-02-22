using UnityEngine;
using Menu;
using static PVStuffMod.StaticStuff;
using System;
using System.Collections.Generic;
using System.CodeDom;
using PVStuff.Logic.ROM_objects;
using ROM.RoomObjectService;
using System.IO;

namespace PVStuffMod;

internal static class MainLogic
{
    public static List<IReceiveWorldTicks> globalUpdateReceivers;

    static MainLogic()
    {
        globalUpdateReceivers = [new InternalSoundController(), new ScreenFlasher()];
    }
    static internal void Startup()
    {
        //PlayerLogic.ApplyHooks();
        //Scene related changes
        On.Menu.MenuScene.BuildScene += MenuScene_BuildScene;
        //for things that do not receive local updates
        On.RainWorldGame.Update += static (orig, self) =>
        {
            orig(self);
            globalUpdateReceivers.ForEach(x => x.Update());
        };
        StaticStuff.LoadEnums();
        RegisterROMObjects();
    }

    private static bool SoundLoader_CheckIfFileExistsAsExternal(On.SoundLoader.orig_CheckIfFileExistsAsExternal orig, SoundLoader self, string name)
    {
        throw new NotImplementedException();
    }

    static internal void RegisterROMObjects()
    {
        TypeOperator.RegisterType<ExposedSoundControllerOperator>();
    }
    private static void MenuScene_BuildScene(On.Menu.MenuScene.orig_BuildScene orig, MenuScene self)
    {
        if(self.menu is SlugcatSelectMenu && (devBuild || EscapismEnding.Contains(self.sceneID.GetCharacterFromSelectScene())))
            self.sceneID = self.sceneID.GetCharacterFromSelectScene().GetSelectScreenSceneID();
        orig(self);
    }
}
