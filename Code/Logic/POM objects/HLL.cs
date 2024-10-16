using MoreSlugcats;
using System;
using System.Linq;
using UnityEngine;
using static Pom.Pom;

namespace PVStuffMod.Logic.POM_objects;

public class HLL : UpdatableAndDeletable
{
	public static void RegisterObject()
	{
		RegisterFullyManagedObjectType(managedFields, typeof(HLL), "HLLSpawner", StaticStuff.PreservatoryPOMCategory);
	}

	internal static ManagedField[] managedFields = [
	new FloatField("speed", 0f, 1f, 0f),
	new FloatField("amplitude", 0f, 20f, 0f),
	new Vector2ArrayField("tentacles", 4, false, Vector2ArrayField.Vector2ArrayRepresentationType.Polygon, new Vector2(-50, -50), new Vector2(-50, 50), new Vector2(50, 50), new Vector2(50, -50))];

	public Vector2[]? polygon => POMUtils.AddRealPosition(data.GetValue<Vector2[]>("tentacles"), pObj.pos);

	public float speed => data.GetValue<float>("speed");

	public float amplitude => data.GetValue<float>("amplitude");

	AbstractCreature? abstractDaddy;
	DaddyLongLegs? daddy;
	uint counter;
	const float timeCoefficient = 40;
	float Perlin(float x) => (Mathf.Sin(2f*x*speed/timeCoefficient) + Mathf.Sin(Mathf.PI*x*speed/timeCoefficient))/2f;
	public override void Update(bool eu)
	{
		ErrorHandling();
		if (daddy == null || room == null || polygon == null) throw new Exception("something's wrong");
		counter++;
		daddy.mainBodyChunk.pos = pObj.pos;
		daddy.Stun(100);
		daddy.g = 0;
		DaddyTentacle[] tnt = daddy.tentacles;
		Array.ForEach(tnt, tnt => tnt.limp = false);
		for(int i = 0; i < tnt.Length; i++) 
		{
			var chunk = tnt[i].Tip;
			chunk.pos = (polygon[i%polygon.Length]) + new Vector2(Perlin(counter+i*20f), Perlin(-counter-i*20f)) * amplitude;
		}
	}

	public override void Destroy()
	{
		daddy?.Destroy();
	}

	public PlacedObject pObj;
	public ManagedData data;

	public HLL(Room room, PlacedObject pObj)
	{
		this.pObj = pObj;
		this.data = (pObj.data as ManagedData)!;
		UnityEngine.Debug.Log(string.Join(", ", polygon.Select(p => p.ToString())));
		room.abstractRoom.AddEntity(abstractDaddy = new AbstractCreature(room.world, 
			StaticWorld.GetCreatureTemplate(MoreSlugcatsEnums.CreatureTemplateType.HunterDaddy), 
			null, 
			RWCustom.Custom.MakeWorldCoordinate(room.GetTilePosition(pObj.pos), room.abstractRoom.index), 
			room.world.game.GetNewID()));
		daddy = new DaddyLongLegs(abstractDaddy, room.world);
		abstractDaddy.realizedCreature = daddy;
		daddy.PlaceInRoom(room);
		daddy.Stun(1000);
		new DaddyAI(abstractDaddy, room.world);
	}
	void ErrorHandling()
	{
		if (room == null) throw new ArgumentException("room is null");
		if (daddy == null) throw new Exception("daddy is null");
		if (polygon == null) throw new Exception("No Polygon for tentacles defined");
	}
}