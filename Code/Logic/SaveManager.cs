using PVStuffMod;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PVStuff.Logic;
/// <summary>
/// SaveManager is used to keep data about who has completed preservatory between sessions
/// </summary>
internal static class SaveManager
{
	private static HashSet<SlugcatStats.Name>[] save = [];
	internal static HashSet<SlugcatStats.Name>[] EscapismEnding
	{
		get
		{
			if (save.Length == 0) LoadData();
			return save;
		}

		private set => save = value;
	}
	private static void extendSaveIfNecessary(int accessingNumber)
	{
		var saves = EscapismEnding;
		if (accessingNumber > saves.Length - 1)
		{
			Array.Resize(ref saves, accessingNumber + 1);
			Array.ForEach(saves, save => save ??= new());
		}
	}
	private static readonly ConditionalWeakTable<RegionState.ConsumedItem, object> ConsumedItemTracker = new();
	private static List<string> SavedCreatureCache = [];
	private static List<string> SavedObjectCache = [];
	private static List<string> SwallowedItemsCache = [];
	private static List<string> UnrecognizedSwallowedItemsCache = [];

	private static string PreservatoryDirectory => ModManager.ActiveMods.Find(x => x.enabled && x.name == "Preservatory").path;
	private const string path = "saves";
	private const string filename = "endings.json";
	private static string FullPath => Path.Combine(PreservatoryDirectory,path,filename);
	internal static void ApplyHooks()
	{
		On.PlayerProgression.WipeAll += static (orig, self) =>
		{
			orig(self);
			extendSaveIfNecessary(self.rainWorld.options.saveSlot);
			EscapismEnding[self.rainWorld.options.saveSlot] = new();
			UpdateDiskSave();
		};
		On.PlayerProgression.WipeSaveState += static (orig, self, test) =>
		{
			orig(self, test);
			if(self.rainWorld.options.saveSlot >= 0)
			{
				extendSaveIfNecessary(self.rainWorld.options.saveSlot);
				EscapismEnding[self.rainWorld.options.saveSlot]?.Remove(test);
				UpdateDiskSave();
			}
		};
		On.RegionState.ReportConsumedItem += static (orig, self, originRoom, placedObjectIndex, waitCycles) =>
		{
			orig(self, originRoom, placedObjectIndex, waitCycles);
			ConsumedItemTracker.Add(self.consumedItems.Last(), new());
		};
		On.SaveState.LoadGame += static (orig, self, str, game) =>
		{
			orig(self, str, game);
			SavedCreatureCache = self.pendingFriendCreatures is not null ? [.. self.pendingFriendCreatures] : null!;
			SavedObjectCache = self.pendingObjects is not null ? [.. self.pendingObjects] : null!;
			SwallowedItemsCache = self.swallowedItems is not null ? [.. self.swallowedItems] : null!;
			UnrecognizedSwallowedItemsCache = self.unrecognizedSwallowedItems is not null ? [.. self.unrecognizedSwallowedItems] : null!;
            PVStuffMod.PVStuff.s_logger!.LogDebug($"To save: {SavedCreatureCache is not null} {SavedObjectCache is not null} {SwallowedItemsCache is not null} {UnrecognizedSwallowedItemsCache is not null}");
        };
		On.SaveState.SaveToString += static (orig, self) =>
		{
			if (CreditHooks.DoPVCredits)
			{
				if (SavedCreatureCache is not null) self.pendingFriendCreatures = SavedCreatureCache;
				if (SavedObjectCache is not null) self.pendingObjects = SavedObjectCache;
				if (SwallowedItemsCache is not null) self.swallowedItems = [.. SwallowedItemsCache];
				if (UnrecognizedSwallowedItemsCache is not null) self.unrecognizedSwallowedItems = UnrecognizedSwallowedItemsCache;
				PVStuffMod.PVStuff.s_logger!.LogDebug($"Saved: {SavedCreatureCache is not null} {SavedObjectCache is not null} {SwallowedItemsCache is not null} {UnrecognizedSwallowedItemsCache is not null}");
			}
			else
			{
				PVStuffMod.PVStuff.s_logger!.LogDebug("Not PV ending, will not save");
			}
			return orig(self);
		};
	}
	internal static void RevertConsumedItems(RainWorldGame game)
	{
		var saveState = game.GetStorySession.saveState;
		foreach (var regionState in saveState.regionStates)
		{
			if (regionState != null)
			{
				for (int i = regionState.consumedItems.Count - 1; i >= 0; i--)
				{
					if (ConsumedItemTracker.TryGetValue(regionState.consumedItems[i], out _))
					{
						regionState.consumedItems.RemoveAt(i);
					}
				}
			}
		}
	}
	internal static void AppendSlugcat(int saveStateNumber, SlugcatStats.Name name)
	{
		if (saveStateNumber < 0) return;
		extendSaveIfNecessary(saveStateNumber);
		EscapismEnding[saveStateNumber].Add(name);
	}
	internal static bool TryGetValue(int saveStateNumber, SlugcatStats.Name name)
	{
        if (saveStateNumber < 0) return false;
        extendSaveIfNecessary(saveStateNumber);
		return EscapismEnding[saveStateNumber].Contains(name);
	}
	internal static void LoadData()
	{
		string dir = PreservatoryDirectory;
		if (!Directory.Exists(Path.Combine(dir, path))) Directory.CreateDirectory(Path.Combine(dir, path));
		if (!File.Exists(FullPath))
		{
			CreateSave();
		}
		else
		{
			TryLoadSaveFromDisk();
		}
	}
	private static void CreateSave()
	{
		EscapismEnding = [new(), new(), new()];
		UpdateDiskSave();
	}
	private static void TryLoadSaveFromDisk()
	{
		object? data = Newtonsoft.Json.JsonConvert.DeserializeObject<string[][]>(File.ReadAllText(FullPath));
		if(data is string[][] strings)
		{
			EscapismEnding = strings.Select(x => x.Select(y => new SlugcatStats.Name(y)).ToHashSet()).ToArray();
		}
		else
		{
			CreateSave();
		}
	}
	internal static void UpdateDiskSave()
	{
		File.WriteAllText(FullPath, Newtonsoft.Json.JsonConvert.SerializeObject(SerializableFormOfSave));
	}
	internal static string[][] SerializableFormOfSave => EscapismEnding.Select(e => e.Select(x => x.value).ToArray()).ToArray();
}
