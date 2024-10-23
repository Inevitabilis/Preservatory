using PVStuff.Logic.ControllerParser;
using static Pom.Pom;
using UnityEngine;
using Newtonsoft.Json;
using MoreSlugcats;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using PVStuffMod;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace PVStuff.Logic.POM_objects;

public class ControlledSlugcat : UpdatableAndDeletable
{
	public static void Register()
	{
		RegisterFullyManagedObjectType(fields, typeof(ControlledSlugcat), "Controlled slugcat", StaticStuff.PreservatoryPOMCategory);
		//Player.SleepUpdate explicitly requests actual player input to do sleep processing. fucked up and evil.
		NPCHooks.Hook();
	}

	public ControlledSlugcat(Room room, PlacedObject pObj)
	{
		this.room = room;
		this.firstTime = room.abstractRoom.firstTimeRealized;
		if (pObj.data is ManagedData data) this.data = data;
		else throw new ArgumentException("Controlled slugcat got invalid data as input");
	}
	#region fields
	internal static ManagedField[] fields = [
			new StringField("controllerID", "test", "controller ID"),
			new BooleanField("adult", true, displayName: "adult?"),
			new ColorField("color", new(1f,1f,1f), displayName: "color"),
			new EnumField<WhoAmI>("characterSpecificSpawn", WhoAmI.anyone, displayName: "who am i?"),
			new BooleanField("enabled", false, displayName: "enabled?"),
		];


	//exposed fields
	ManagedData data;

	public Vector2 startPosition => data.owner.pos;
#pragma warning disable CS8603 // Possible null reference return.
	public string controllerID => data.GetValue<string>("controllerID");
#pragma warning restore CS8603 // Possible null reference return.
	public bool adult => data.GetValue<bool>("adult");
	[JsonIgnore]
	public Color color => data.GetValue<Color>("color");
	public WhoAmI whoAmI => data.GetValue<WhoAmI>("characterSpecificSpawn");
	public bool isEnabled => data.GetValue<bool>("enabled");

	public SerializableColor serializableColor = new(1f, 1f, 1f);
	#endregion

	public class SerializableColor
	{
		public float r, g, b;
		public SerializableColor(float r, float g, float b)
		{
			this.r = r;
			this.g = g;
			this.b = b;
		}
		public SerializableColor(Color c) : this(c.r, c.g, c.b)
		{ }
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
	WeakReference<AbstractCreature>? puppetWeakRef;
	[JsonIgnore]
	Player puppetPlayer
	{
		get
		{
			if (puppetWeakRef is not null && puppetWeakRef.TryGetTarget(out var puppet) && puppet.realizedCreature is Player p) return p;
			else if (puppetWeakRef is null) throw new System.Exception($"[PRESERVATORY]: {nameof(ControlledSlugcat)}.{nameof(ControlledSlugcat.puppetPlayer)} - puppet was null");
			else throw new System.Exception($"[PRESERVATORY]: {nameof(ControlledSlugcat)}.{nameof(ControlledSlugcat.puppetPlayer)} - realized creature of puppet was not player");
		}
	}
	[JsonIgnore]
	WorldCoordinate blockPosition => room.ToWorldCoordinate(startPosition);
	[JsonIgnore]
	bool initdone;
	[JsonIgnore]
	bool firstTime;
	#endregion

	public override void Destroy()
	{
		base.Destroy();
		if (puppetPlayer is Player p) p.Destroy();
		if (puppetWeakRef is not null && puppetWeakRef.TryGetTarget(out var puppet))
		{
			foreach (var absRoom in room.world.abstractRooms)
			{
				if(absRoom.entities.Contains(puppet))
				{
					absRoom.RemoveEntity(puppet);
					break;
				}
			}
		}
	}


	public override void Update(bool eu)
	{
		if (isEnabled
			&& !initdone
			&& firstTime
			&& IsValidToSpawnForCharacter()
			&& room.fullyLoaded
			&& room.ReadyForPlayer
			&& room.shortCutsReady)
		{
			var puppet = new AbstractCreature(room.world, StaticWorld.GetCreatureTemplate(MoreSlugcatsEnums.CreatureTemplateType.SlugNPC), null, blockPosition, room.game.GetNewID());
			//adding to the list of abstract creatures that don't receive movement updates
			if (NPCHooks.lobotomizedAbstractCreatures.TryGetValue(room.game, out var hashes)) hashes.Add(puppet.abstractAI.GetHashCode());
			puppetWeakRef = new(puppet);
			puppet.state = new PlayerNPCState(puppet, 0)
			{
				forceFullGrown = adult
			};
			room.abstractRoom.AddEntity(puppet);

			SlugcatDataPackage slugcatDataPackage = new SlugcatDataPackage(color, adult, controllerID);
			NPCHooks.characterStats.Add(puppet, slugcatDataPackage);

			puppet.RealizeInRoom();

			initdone = true;
		}

		//kill slugcat when controlling object is destroyed
		if (initdone && !room.roomSettings.placedObjects.Contains(data.owner)) Destroy();
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

internal static class NPCHooks
{
	public static ConditionalWeakTable<AbstractCreature, SlugcatDataPackage> characterStats = new();
	public static void Hook()
	{
		//replace last input with controller data package if it exists
		IL.Player.SleepUpdate += Player_SleepUpdate;
		//using jolly coop's sleep variable i can make a check to make them sleep
		On.Player.SleepUpdate += Player_SleepUpdate1;
		//apply existing character stats to abstract NPC on realization
		On.AbstractCreature.Realize += AbstractCreature_Realize;
		//if existing slugNPCs abstractize, they happen to wander around. to prevent this, we ask it to not process abstract movement related logic
		On.AbstractCreatureAI.Update += AbstractCreatureAI_Update;
		
	}

	public static ConditionalWeakTable<RainWorldGame, HashSet<int>> lobotomizedAbstractCreatures = new();
	private static void AbstractCreatureAI_Update(On.AbstractCreatureAI.orig_Update orig, AbstractCreatureAI self, int time)
	{
		if (lobotomizedAbstractCreatures.TryGetValue(self.world.game, out var hashset) && hashset.Contains(self.GetHashCode())) return;
		orig(self,time);
	}

	private static void AbstractCreature_Realize(On.AbstractCreature.orig_Realize orig, AbstractCreature self)
	{
		orig(self);
		if(self.creatureTemplate.TopAncestor().type == MoreSlugcatsEnums.CreatureTemplateType.SlugNPC
			&& characterStats.TryGetValue(self, out var stats))
		{
			if (self.realizedCreature is Player puppetPlayer)
			{
				puppetPlayer.controller = new SlugController(stats.controllerID);
				puppetPlayer.standing = true;
				var HSL = RWCustom.Custom.RGB2HSL(stats.color);
				var NPCstats = puppetPlayer.npcStats;
				NPCstats.Dark = false;
				NPCstats.H = HSL.x;
				NPCstats.S = HSL.y;
				NPCstats.L = HSL.z;

				// it gets painted properly without setting darkenFactor
				/*if (puppetPlayer.graphicsModule is PlayerGraphics g)
				{
					//i haven't figured out how to paint slugNPCs properly in update, but there were two methods that handled it
					//both conditional. one required darkenFactor to be above zero so here we are
					g.darkenFactor = 0.01f;
				}*/
			}
			else MainLogic.logger.LogError("realizing PV slugcat but its realized creature is not Player. Expect no color change and controller assignment");
			
		}
	}

	private static void Player_SleepUpdate1(On.Player.orig_SleepUpdate orig, Player self)
	{
		orig(self);
		if (!self.standing 
			&& self.input[0].y < 0 
			&& !self.input[0].jmp 
			&& !self.input[0].thrw 
			&& !self.input[0].pckp 
			&& self.IsTileSolid(1, 0, -1) 
			&& (self.input[0].x == 0 
				|| ((!self.IsTileSolid(1, -1, -1) 
						|| !self.IsTileSolid(1, 1, -1)) && self.IsTileSolid(1, self.input[0].x, 0))))
		{
			self.emoteSleepCounter += 0.028f;
		}
		else
		{
			self.emoteSleepCounter = 0f;
		}
		if (self.emoteSleepCounter > 1.4f)
		{
			self.sleepCurlUp = Mathf.SmoothStep(self.sleepCurlUp, 1f, self.emoteSleepCounter - 1.4f);
			return;
		}
		self.sleepCurlUp = Mathf.Max(0f, self.sleepCurlUp - 0.1f);
	}

	private static void Player_SleepUpdate(MonoMod.Cil.ILContext il)
	{
		ILCursor c = new(il);

		ILLabel jump = c.DefineLabel();

		// Player.InputPackage inputPackage = RWInput.PlayerInput(this.playerState.playerNumber);
		// <if(this.controller != null) inputPackage = this.input[0];>
		if(c.TryGotoNext(MoveType.After, x => x.MatchCall(nameof(RWInput),nameof(RWInput.PlayerInput)))
			&& c.TryGotoNext(MoveType.After, x => x.MatchStloc(2)))
		{
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate<Predicate<Player>>((Player p) => p.controller != null);
			c.Emit(OpCodes.Brfalse, jump);

			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate<Func<Player, Player.InputPackage>>((Player p) =>
			{
				var result = p.input[0];
				if(result.x == 0 && p.IsTileSolid(1, 0, -1)) {
					p.forceSleepCounter++;
				}
				MainLogic.logger.LogInfo("controlled update. sleep counter is " + p.forceSleepCounter);
				return result;
			});
			c.Emit(OpCodes.Stloc_2);

			c.MarkLabel(jump);
		}
	}
}

internal class SlugcatDataPackage
{
	public SlugcatDataPackage(UnityEngine.Color color, bool adult, string controllerID)
	{
		this.color = color;
		this.adult = adult;
		this.controllerID = controllerID;
	}
	public UnityEngine.Color color;
	public bool adult;
	public string controllerID;
	
}
