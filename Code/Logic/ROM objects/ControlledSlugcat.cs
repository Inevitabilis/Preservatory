using static RWCustom.Custom;
using PVStuff.Logic.ControllerParser;
using UnityEngine;
using Newtonsoft.Json.Linq;
using ROM.RoomObjectService;
using ROM.UserInteraction.InroomManagement;
using ROM.UserInteraction.ObjectEditorElement;
using System.Collections.Generic;
using Newtonsoft.Json;
using MoreSlugcats;
using PVStuffMod;


namespace PVStuff.Logic.ROM_objects;

public class ControlledSlugcat : UpdatableAndDeletable
{
	//exposed fields
	public Vector2 startPosition;
	public string controllerID = "test";
	public bool adult = true;
	[JsonIgnore]
	public Color color
	{
		get
		{
			return serializableColor.ToColor();
		}
	}
	public WhoAmI whoAmI = WhoAmI.anyone;
	public bool isEnabled;

	public SerializableColor serializableColor = new(1f, 1f, 1f);

	
	public class SerializableColor
	{
		public float r, g, b;
		public SerializableColor(float r, float g, float b)
		{
			this.r = r;
			this.g = g;
			this.b = b;
		}
		public SerializableColor(Color c) : this (c.r, c.g, c.b)
		{}
		public Color ToColor()
		{
			return new(r, g, b);
		}
	}
	public enum WhoAmI
	{
		anyone,
		survivor,
		monk,
		gourmand
	}
	#region runtime variables
	[JsonIgnore]
	AbstractCreature? puppet;
	[JsonIgnore]
	Player puppetPlayer
	{
		get
		{
			if (puppet?.realizedCreature is Player p) return p;
			else if (puppet is null) throw new System.Exception($"[PRESERVATORY]: {nameof(ControlledSlugcat)}.{nameof(ControlledSlugcat.puppetPlayer)} - puppet was null");
			else throw new System.Exception($"[PRESERVATORY]: {nameof(ControlledSlugcat)}.{nameof(ControlledSlugcat.puppetPlayer)} - realized creature of puppet was not player");
		}
	}
	[JsonIgnore]
	WorldCoordinate blockPosition => room.ToWorldCoordinate(startPosition);
	[JsonIgnore]
	bool initdone;
	#endregion
	public override void Update(bool eu)
	{
		if(!(room.fullyLoaded && room.ReadyForPlayer && room.shortCutsReady && isEnabled)) return;
		if(IsValidToSpawnForCharacter() && !initdone) CreatureSetup();
	}

	private void CreatureSetup()
	{
		puppet = new AbstractCreature(room.world, StaticWorld.GetCreatureTemplate(MoreSlugcatsEnums.CreatureTemplateType.SlugNPC), null, blockPosition, room.game.GetNewID());
		puppet.state = new PlayerNPCState(puppet, 0)
		{
			forceFullGrown = adult
		};
		room.abstractRoom.AddEntity(puppet);
		puppet.RealizeInRoom();
		puppetPlayer.controller = new SlugController(controllerID);
		puppetPlayer.standing = true;
		var HSL = RWCustom.Custom.RGB2HSL(color);
		var stats = puppetPlayer.npcStats;
		stats.H = HSL.x;
		stats.S = HSL.y;
		stats.L = HSL.z;
		if(puppetPlayer.graphicsModule is PlayerGraphics g)
		{
			//i haven't figured out how to paint slugNPCs properly in update, but there were two methods that handled it
			//both conditional. one required darkenFactor to be above zero so here we are
			g.darkenFactor = 0.01f;
		}
		initdone = true;
	}
	bool IsValidToSpawnForCharacter()
	{
		var campaignCharacter = room.game.GetStorySession.characterStats.name;
		if (whoAmI == WhoAmI.anyone) return true;
		if (whoAmI == WhoAmI.gourmand) return campaignCharacter == SlugcatStats.Name.White || campaignCharacter == SlugcatStats.Name.Yellow;
		if (whoAmI == WhoAmI.monk) return campaignCharacter == SlugcatStats.Name.White || campaignCharacter == MoreSlugcatsEnums.SlugcatStatsName.Gourmand;
		if (whoAmI == WhoAmI.survivor) return campaignCharacter == SlugcatStats.Name.Yellow || campaignCharacter == MoreSlugcatsEnums.SlugcatStatsName.Gourmand;
		return true;
	}
}

public class ControlledSlugcatOperator : TypeOperator<ControlledSlugcat>
{
	private static VersionedLoader<ControlledSlugcat> VersionedLoader { get; } =
			TypeOperatorUtils.CreateVersionedLoader(defaultLoad: TypeOperatorUtils.TrivialLoad<ControlledSlugcat>);
	public override string TypeId => nameof(ControlledSlugcat);

	public override void AddToRoom(ControlledSlugcat obj, Room room)
	{
		room.AddObject(obj);
	}

	public override ControlledSlugcat CreateNew(Room room, Rect currentCameraRect)
	{
		return new() { room = room };
	}

	public override IEnumerable<IObjectEditorElement> GetEditorElements(ControlledSlugcat obj, Room room)
	{
		yield return Elements.Point("Spawn position", "s", () => obj.startPosition, x => obj.startPosition = x);
		yield return Elements.TextField("script name", () => obj.controllerID, x => obj.controllerID = x);
		yield return Elements.Checkbox("adult?", () => obj.adult, x => obj.adult = x);
		yield return Elements.Scrollbar("Color.R", () => obj.serializableColor.r, x => obj.serializableColor.r = x);
		yield return Elements.Scrollbar("Color.G", () => obj.serializableColor.g, x => obj.serializableColor.g = x);
		yield return Elements.Scrollbar("Color.B", () => obj.serializableColor.b, x => obj.serializableColor.b = x);
		yield return Elements.CollapsableOptionSelect("who am i?", () => obj.whoAmI, x => obj.whoAmI = x);
		yield return Elements.Checkbox("Enabled?", () => obj.isEnabled, x => obj.isEnabled = x);

	}

	public override ControlledSlugcat Load(JToken dataJson, Room room)
	{
		return VersionedLoader.Load(dataJson, room);
	}

	public override void RemoveFromRoom(ControlledSlugcat obj, Room room)
	{
		
		room.RemoveObject(obj);
	}

	public override JToken Save(ControlledSlugcat obj)
	{
		return TypeOperatorUtils.GetTrivialVersionedSaveCall<ControlledSlugcat>("0.0.0").Invoke(obj);
	}
}
