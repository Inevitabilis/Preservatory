using PVStuff.Logic;
using SlugBase;
using System.Collections.Generic;
using System.Linq;
using static MoreSlugcats.MoreSlugcatsEnums;

namespace PVStuffMod;

internal class NonPlayerCenteredLogic
{
	internal static void BeatGameModeStasis(RainWorldGame game)
	{
		AppendStatistics(game);
		UpdateSaveState(game);
		SetSelectScreen(game);
		game.ExitGame(false, false);
		CreditHooks.DoPVCredits = true;
		game.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Credits, 0);
	}

	private static void UpdateSaveState(RainWorldGame game)
	{
		var saveStateNumber = game.GetStorySession.saveStateNumber;
		var miscProgressionData = game.rainWorld.progression.miscProgressionData;
		if (saveStateNumber == SlugcatStats.Name.White
			|| saveStateNumber == SlugcatStats.Name.Yellow) miscProgressionData.redUnlocked = true;
		if (saveStateNumber == SlugcatStats.Name.Red)
		{
			miscProgressionData.beaten_Hunter = true;
			game.GetStorySession.saveState.deathPersistentSaveData.redsDeath = true;
		}
		if (saveStateNumber == SlugcatStats.Name.White)
		{
			miscProgressionData.survivorEndingID = 1;
		}
		if (saveStateNumber == SlugcatStats.Name.Yellow)
		{
			miscProgressionData.monkEndingID = 1;
		}
		if (ModManager.MSC || saveStateNumber == SlugcatStats.Name.Red)
		{
			game.GetStorySession.saveState.progression.SaveWorldStateAndProgression(false);
		}
		if (ModManager.MSC)
		{
			//i really wanted here to be switch case, but C# didn't like switching through non-constant variables
			if (saveStateNumber == SlugcatStatsName.Artificer) miscProgressionData.beaten_Artificer = true;
			//game.rainWorld.progression.miscProgressionData.artificerEndingID = 1;
			if (saveStateNumber == SlugcatStatsName.Rivulet) miscProgressionData.beaten_Rivulet = true;
			if (saveStateNumber == SlugcatStatsName.Gourmand) miscProgressionData.beaten_Gourmand = true;
			if (saveStateNumber == SlugcatStatsName.Spear) miscProgressionData.beaten_SpearMaster = true;
		}
	}
	private static void AppendStatistics(RainWorldGame game)
	{
		int playerIndex = 0;
		using (IEnumerator<Player> enumerator = (from x in game.GetStorySession.game.Players
												 select x.realizedCreature as Player).GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				Player player = enumerator.Current;
				game.GetStorySession.saveState.AppendCycleToStatistics(player, game.GetStorySession, true, playerIndex);
				playerIndex++;
			}
		}
	}
	private static void SetSelectScreen(RainWorldGame game) 
	{
		SaveManager.AppendSlugcat(game.rainWorld.options.saveSlot, game.GetStorySession.saveStateNumber);
		SaveManager.UpdateDiskSave();
	}
	
}
