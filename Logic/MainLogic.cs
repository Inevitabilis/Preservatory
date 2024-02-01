using UnityEngine;

namespace PVStuffMod;

internal static class MainLogic
{

    static internal void Startup()
    {
        PlayerLogic.ApplyHooks();

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
                SaveState orInitiateSaveState = self.manager.rainWorld.progression.GetOrInitiateSaveState(currentlySelectedSinglePlayerSlugcat, null, self.manager.menuSetup, false);
                SlugBase.Assets.CustomScene.SetSelectMenuScene(orInitiateSaveState, currentlySelectedSinglePlayerSlugcat.GetSelectScreenSceneID());
            }
        };
    }
}
