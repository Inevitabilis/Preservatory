﻿using PVStuffMod;
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
        get => save;
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
    }
    internal static void AppendSlugcat(int saveStateNumber, SlugcatStats.Name name)
    {
        if(!EscapismEnding[saveStateNumber].Contains(name)) EscapismEnding[saveStateNumber].Add(name);
    }
    internal static bool TryGetValue(int saveStateNumber, SlugcatStats.Name name)
    {
        if(EscapismEnding.Length == 0) LoadData();
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
        MainLogic.Log("trying to load: " + FullPath);
        object? data = Newtonsoft.Json.JsonConvert.DeserializeObject<string[][]>(File.ReadAllText(FullPath));
        if(data is string[][] strings)
        {
            EscapismEnding = strings.Select(x => x.Select(y => new SlugcatStats.Name(y)).ToHashSet()).ToArray();
        }
        else
        {
            MainLogic.Log("No savefile for preservatory found");
            CreateSave();
        }
    }
    internal static void UpdateDiskSave()
    {
        File.WriteAllText(FullPath, Newtonsoft.Json.JsonConvert.SerializeObject(SerializableFormOfSave));
    }
    internal static string[][] SerializableFormOfSave => EscapismEnding.Select(e => e.Select(x => x.value).ToArray()).ToArray();
}