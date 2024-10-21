using Menu;
using System.Collections.Generic;
using MoreSlugcats;
using static SlugcatStats.Name;
using static MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName;
using static MoreSlugcats.MoreSlugcatsEnums.MenuSceneID;
using static Menu.MenuScene.SceneID;
using UnityEngine;
using System.Runtime.InteropServices;
using PVStuffMod.Logic;
using IL.RWCustom;
using Newtonsoft.Json.Serialization;
using System;

namespace PVStuffMod;

public static class PVEnums
{
	const string head = "Preservatory_";
	const string tail = "EscEnd";
	static PVEnums()
	{
		Artificer = new(head + nameof(Artificer) + tail);
		Gourmand = new(head + nameof(Gourmand) + tail);
		Spearmaster = new(head + nameof(Spearmaster) + tail);
		Survivor = new(head + nameof(Survivor) + tail);
		Monk = new(head + nameof(Monk) + tail);
		Rivulet = new(head + nameof(Rivulet) + tail);
		Hunter = new(head + nameof(Hunter) + tail);
	}
	public static MenuScene.SceneID Artificer;
	public static MenuScene.SceneID Gourmand;
	public static MenuScene.SceneID Spearmaster;
	public static MenuScene.SceneID Survivor;
	public static MenuScene.SceneID Monk;
	public static MenuScene.SceneID Rivulet;
	public static MenuScene.SceneID Hunter;


	public static class Melody
	{
		public static void Register()
		{
			approach0 = new(head + nameof(approach0), true);
			approach1 = new(head + nameof(approach1), true);
			approach2 = new(head + nameof(approach2), true);
			approach3 = new(head + nameof(approach3), true);
		}
		public static SoundID? approach0;
		public static SoundID? approach1;
		public static SoundID? approach2;
		public static SoundID? approach3;

	}
	public static class NPCBehaviour
	{
		public static void Register()
		{
			completelyStill = new(head + nameof(completelyStill), true);
		}
		public static SlugNPCAI.BehaviorType? completelyStill;
	}
}
public static class StaticStuff
{
	public static bool logging = false;
	public const int TicksPerSecond = 40;
	public static bool devBuild = true;
	public const string PreservatoryPOMCategory = "Preservatory";

	public static void loginf(object e) => MainLogic.logger.LogInfo(e);
	public static void logerr(object e) => MainLogic.logger.LogError(e);

	public static Vector2 centerOfOneScreenRoom = new(482, 349);
	public static ScreenFlasher RegisterScreenFlasher(RoomCamera rCam)
	{
		ScreenFlasher screenFlasher = new();
		rCam.NewObjectInRoom(screenFlasher);
		MainLogic.screenFlasherRef.Add(rCam.game, screenFlasher);
		return screenFlasher;
	}
	public struct Destination
	{
		public string roomName;
		public Vector2 position;
	}
}
public static class PVMaps
{
	static PVMaps()
	{
		nameSceneMap = new()
		{
			{ Yellow, PVEnums.Monk },
			{ Red, PVEnums.Hunter },
			{ MoreSlugcatsEnums.SlugcatStatsName.Gourmand, PVEnums.Gourmand },
			{ MoreSlugcatsEnums.SlugcatStatsName.Spear, PVEnums.Spearmaster },
			{ MoreSlugcatsEnums.SlugcatStatsName.Artificer, PVEnums.Artificer },
			{ MoreSlugcatsEnums.SlugcatStatsName.Rivulet, PVEnums.Rivulet },
			{ White, PVEnums.Survivor }
		};
		dreamRoom = new()
		{
			{ Yellow, new() {roomName = "PV_DREAM_TREE03", position = new(306f, 269f) } },
			{ White, new() {roomName = "PV_DREAM_TREE03", position = new(298.7f, 269.0f) } },
			{ Gourmand, new() {roomName = "PV_DREAM_TREE03", position = new(298.7f, 269.0f) } },
			{ Artificer, new() {roomName = "PV_DREAM_ARTI", position = new(534f, 84f)} },
			{ Red, new() { roomName = "PV_DREAM_RED", position = new(4962, 1024) } }
		};
		endRoom = new()
		{
			{ Red, new() { roomName = "PV_END_RED", position = new(0,0) } }
		};


	}

	#region Maps
	public static Dictionary<SlugcatStats.Name, MenuScene.SceneID> nameSceneMap;

	public static Dictionary<SlugcatStats.Name, StaticStuff.Destination> dreamRoom;

	public static Dictionary<SlugcatStats.Name, StaticStuff.Destination> endRoom;
	#endregion

	#region Methods
	static internal MenuScene.SceneID GetSelectScreenSceneID(this SlugcatStats.Name character)
	{
		return nameSceneMap.TryGetValue(character, out var sceneID) ? sceneID : PVEnums.Survivor;
	}
	static internal StaticStuff.Destination GetDreamDestination(this SlugcatStats.Name character)
	{
		return dreamRoom.TryGetValue(character, out var roomName) ? roomName : new() { roomName = "PV_END", position = StaticStuff.centerOfOneScreenRoom }; //yes the player actually needs to not be noticed, with hunter long legs replacing it
	}
	static internal StaticStuff.Destination GetEndDestination(this SlugcatStats.Name character)
	{
		return endRoom.TryGetValue(character, out var roomName) ? roomName : new() { roomName = "PV_END", position = StaticStuff.centerOfOneScreenRoom };
	}
	#endregion
}
public static class ROMUtils
{
	public static bool PositionWithinPoly(Vector2[] Polygon, Vector2 point)
	{
			bool result = true;
		for (int i = 0; i < Polygon.Length; i++)
		{
			if (IsAboveEquationByTwoPoints(Polygon[i], Polygon[(i + 1) % Polygon.Length], point) 
				!= IsAboveEquationByTwoPoints(Polygon[i], Polygon[(i + 1) % Polygon.Length], Polygon[(i+2)%Polygon.Length])) result = false;
		}
		return result;
	}
	private static bool IsAboveEquationByTwoPoints(Vector2 point1, Vector2 point2, Vector2 v)
	{
		bool isAboveLine = (point1.x - v.x) * (point2.y - point1.y) <= (point1.y - v.y) * (point2.x - point1.x);
		return isAboveLine;
	}
}

public static class POMUtils
{
	public static Vector2[]? AddRealPosition(Vector2[]? Polygon, Vector2 pos)
	{
		if (Polygon == null) return null;
		Vector2[] result = new Vector2[Polygon.Length];
		for (int i = 0; i < Polygon.Length; i++)
		{ result[i] = Polygon[i] + pos; }
		return result;
	}

	public static bool PositionWithinPoly(Vector2[]? Polygon, Vector2 point)
	{
		if (Polygon == null) return false;
		bool result = true;
		for (int i = 0; i < Polygon.Length; i++)
		{
			if (IsAboveEquationByTwoPoints(Polygon[i], Polygon[(i + 1) % Polygon.Length], point)
				!= IsAboveEquationByTwoPoints(Polygon[i], Polygon[(i + 1) % Polygon.Length], Polygon[(i + 2) % Polygon.Length])) result = false;
		}
		return result;
	}
	private static bool IsAboveEquationByTwoPoints(Vector2 point1, Vector2 point2, Vector2 v)
	{
		bool isAboveLine = (point1.x - v.x) * (point2.y - point1.y) <= (point1.y - v.y) * (point2.x - point1.x);
		return isAboveLine;
	}

	public static Pom.Pom.Vector2ArrayField defaultVectorField => new Pom.Pom.Vector2ArrayField("trigger zone", 4, true, Pom.Pom.Vector2ArrayField.Vector2ArrayRepresentationType.Polygon, Vector2.zero, Vector2.right* 20f, (Vector2.right + Vector2.up) * 20f, Vector2.up* 20f);
}
public interface IReceiveWorldTicks
{
	public void Update();
	public bool SlatedForDeletion { get; }
}
