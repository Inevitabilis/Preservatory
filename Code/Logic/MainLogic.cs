using UnityEngine;
using Menu;
using static PVStuffMod.StaticStuff;
using System;
using System.Collections.Generic;
using System.CodeDom;
//using PVStuffMod.Logic.ROM_objects;
using System.IO;
using PVStuffMod.Logic;
using System.Linq;
//using PVStuff.Logic.ROM_objects;
using PVStuff.Logic;
using PVStuff.Logic.POM_objects;
using BepInEx.Logging;
using PVStuff.Logic.ControllerParser;





using PVStuffMod.Logic.POM_objects;
using System.Runtime.CompilerServices;


namespace PVStuffMod;

internal static class MainLogic
{
    public static ManualLogSource logger => PVStuff.s_logger;
    public static ConditionalWeakTable<RainWorldGame, InternalSoundController> internalSoundControllerRef = new();
    public static ConditionalWeakTable<RainWorldGame, ScreenFlasher> screenFlasherRef = new();

    static bool initialized = false;
    
    static internal void Startup()
    {
        if (initialized) return;
        //Scene related changes
        On.Menu.MenuScene.BuildScene += MenuScene_BuildScene;
        PVEnums.Melody.Register();
        PVEnums.NPCBehaviour.Register();
        _ControllerMeta.Startup();
        //for things that do not receive local updates
        On.RainWorldGame.Update += static (orig, self) =>
        {
            orig(self);

            if(internalSoundControllerRef.TryGetValue(self, out var controller))
            {
                controller.Update();
            }

            if(screenFlasherRef.TryGetValue(self, out var flasher))
            {
                if(!self.cameras[0].spriteLeasers.Exists(x => x.drawableObject == flasher)) self.cameras[0].NewObjectInRoom(flasher);

                if(flasher.SlatedForDeletion)
                {
                    self.cameras[0].spriteLeasers.ForEach(sleaser =>
                    {
                        if (sleaser.drawableObject == flasher) sleaser.CleanSpritesAndRemove();
                    });
                }
            }
        };

        On.RainWorldGame.ctor += RainWorldGame_ctor;

        RegisterPOMObjects();
        SaveManager.ApplyHooks();
        initialized = true;
    }

    private static void RainWorldGame_ctor(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
    {
        orig(self, manager);
        internalSoundControllerRef.Add(self, new(self));
    }
    static internal void RegisterPOMObjects()
    {
        HLL.RegisterObject();
        RedInducedIllness.RegisterObject();
        Teleporter.RegisterObject();
        VatScene.RegisterEffect();
        ExposedSoundController.RegisterObject();
        ControlledSlugcat.Register();
    }
    private static void MenuScene_BuildScene(On.Menu.MenuScene.orig_BuildScene orig, MenuScene self)
    {
        if(self.menu is SlugcatSelectMenu && self.sceneID != null && (devBuild || SaveManager.TryGetValue(self.menu.manager.rainWorld.options.saveSlot,self.sceneID.GetCharacterFromSelectScene())))
            self.sceneID = self.sceneID.GetCharacterFromSelectScene().GetSelectScreenSceneID();
        orig(self);
    }
}
