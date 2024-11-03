using PVStuffMod;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
	private static string PreservatoryDirectory => ModManager.ActiveMods.Find(x => x.enabled && x.name == "Preservatory").path;
	private const string path = "saves";
	private const string filename = "endings.json";
	private static string FullPath => Path.Combine(PreservatoryDirectory,path,filename);
	internal static void ApplyHooks()
	{
		On.PlayerProgression.WipeAll += static (orig, self) =>
		{
			orig(self);
			EscapismEnding[self.rainWorld.options.saveSlot] = new();
			UpdateDiskSave();
		};
		On.PlayerProgression.WipeSaveState += static (orig, self, test) =>
		{
			orig(self, test);
			EscapismEnding[self.rainWorld.options.saveSlot]?.Remove(test);
			UpdateDiskSave();
		};
	}
	internal static void AppendSlugcat(int saveStateNumber, SlugcatStats.Name name)
	{
		var saves = EscapismEnding;
		if(saveStateNumber > saves.Length - 1)
		{
			Array.Resize(ref saves, saveStateNumber + 1);
			Array.ForEach(saves, save => save ??= new());
		}
		EscapismEnding[saveStateNumber].Add(name);
	}
	internal static bool TryGetValue(int saveStateNumber, SlugcatStats.Name name)
	{
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
