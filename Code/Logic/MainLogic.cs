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
using System.Runtime.ExceptionServices;


namespace PVStuffMod;

internal static class MainLogic
{
	public static ManualLogSource logger => PVStuff.s_logger;
	public static ConditionalWeakTable<RainWorldGame, InternalSoundController> internalSoundControllerRef = new();
	public static ConditionalWeakTable<RainWorldGame, ScreenFlasher> screenFlasherRef = new();

	static bool initialized = false;
	static bool keypressed = false;
	static internal void Startup()
	{
		if (initialized) return;

		string PVpath = ModManager.ActiveMods.FirstOrDefault(x => x.id == "preservatory").path;
		StaticStuff.devBuild = File.Exists(Path.Combine(PVpath, "devmode.txt"));
		//Scene related changes
		On.Menu.MenuScene.BuildScene += MenuScene_BuildScene;
		//Registering enums
		PVEnums.Melody.Register();
		PVEnums.NPCBehaviour.Register();
		//starting up controller logic
		_ControllerMeta.Startup();

		RegisterPOMObjects();
		//starting up save system logic
		SaveManager.ApplyHooks();
		initialized = true;
		//for things that do not receive local updates
		On.RainWorldGame.Update += static (orig, self) =>
		{
			orig(self);
			#region internal sound controller
			if (internalSoundControllerRef.TryGetValue(self, out var controller))
			{
				if(controller.ShouldWork)	controller.Update();

				//slated for deletion never works actually i believe
				if(controller.SlatedForDeletion)
				{
					internalSoundControllerRef.Remove(self);
				}
			}
			#endregion
			#region screen flasher
			if (screenFlasherRef.TryGetValue(self, out var flasher))
			{
				flasher.Update();

				if(!self.cameras[0].spriteLeasers.Exists(x => x.drawableObject == flasher)) self.cameras[0].NewObjectInRoom(flasher);

				if(flasher.SlatedForDeletion)
				{
					self.cameras[0].spriteLeasers.ForEach(sleaser =>
					{
						if (sleaser.drawableObject == flasher) sleaser.CleanSpritesAndRemove();
					});
					screenFlasherRef.Remove(self);
				}
			}
			#endregion
			#region debug
			if(Input.GetKeyDown(KeyCode.K) && !keypressed)
			{
				Dump(self);
			}
			keypressed = Input.GetKeyDown(KeyCode.K);
			#endregion

		};

		On.RainWorldGame.ctor += (orig, self, manager) =>
		{
			orig(self, manager);
			internalSoundControllerRef.Add(self, new(self) { ShouldWork = self.world.name == "PV"});
			NPCHooks.lobotomizedAbstractCreatures.Add(self, new HashSet<int>());
		};

		On.OverWorld.LoadWorld += (orig, self, worldName, playerCharacterNumber, singleRoomWorld) =>
		{
			orig(self, worldName, playerCharacterNumber, singleRoomWorld);
			if(internalSoundControllerRef.TryGetValue(self.game, out var internalSoundController))
			{
				internalSoundController.ShouldWork = self.activeWorld.name == "PV";
			}
		};


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
		if(self.menu is SlugcatSelectMenu
			&& self.sceneID != null
			&& self.owner is SlugcatSelectMenu.SlugcatPage page)
		{
			var owner = page.slugcatNumber;
			if(devBuild || SaveManager.TryGetValue(self.menu.manager.rainWorld.options.saveSlot, owner ))
			{
                self.sceneID = owner.GetSelectScreenSceneID();
            }
		}			
		orig(self);
	}

	private static void Dump(RainWorldGame game)
	{
		loginf("dump started");
		if (internalSoundControllerRef.TryGetValue(game, out var internalSoundController))
		{
			loginf("internal sound controller associated with game found");
			var disemb = internalSoundController.disembodiedLoopEmitters;
			if (disemb != null)
			{
				for (short i = 0; i < disemb.Length; i++)
				{
					if (disemb[i].TryGetTarget(out _)) loginf($"soundloop {i} has reference");
					else loginf($"soundloop {i} doesn't contain any reference");
				}
			}
			else loginf("disembodied sound loops were null");
		}
		else loginf("associated sound controller not found");
	}
}
