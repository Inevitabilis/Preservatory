using UnityEngine;
using Menu;
using static PVStuffMod.StaticStuff;

namespace PVStuffMod;

internal static class MainLogic
{

    static internal void Startup()
    {
        PlayerLogic.ApplyHooks();
        On.Menu.MenuScene.BuildScene += MenuScene_BuildScene;

    }
    static internal void RegisterROMObjects()
    {

    }

    static internal void HackySolutions()
    {
        On.Menu.SlugcatSelectMenu.Update += static (On.Menu.SlugcatSelectMenu.orig_Update orig, Menu.SlugcatSelectMenu self) =>
        {
            orig(self);
            if (Input.GetKeyUp("g"))
            {
                SlugcatStats.Name currentlySelectedSinglePlayerSlugcat = self.manager.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat;
                SaveState saveState = self.manager.rainWorld.progression.GetOrInitiateSaveState(currentlySelectedSinglePlayerSlugcat, null, self.manager.menuSetup, false);

            }
        };
    }
    private static void MenuScene_BuildScene(On.Menu.MenuScene.orig_BuildScene orig, MenuScene self)
    {
        if(self.menu is SlugcatSelectMenu && (devBuild || EscapismEnding.Contains(self.sceneID.GetCharacterFromSelectScene())))
            self.sceneID = self.sceneID.GetCharacterFromSelectScene().GetSelectScreenSceneID();
        orig(self);
    }
}
