using PVStuffMod;
using System.Linq;
using UnityEngine;
using static Pom.Pom;

namespace PVStuffMod.Logic.POM_objects;
/// <summary>
/// RedInducedIllness is the class responsible for making red experience vanilla fainting out whenever any of red player enters the trigger area
/// </summary>
public class RedInducedIllness : UpdatableAndDeletable
{
	public static void RegisterObject()
	{
		RegisterFullyManagedObjectType(managedFields, typeof(RedInducedIllness), "PVRedInducedIllness", StaticStuff.PreservatoryPOMCategory);
	}

	internal static ManagedField[] managedFields = [
		new FloatField("effect time", 0f, float.PositiveInfinity, 5f, control: ManagedFieldWithPanel.ControlType.text),
		new FloatField("severity", 0f, 2f, 1f, displayName: "severity"),
		new FloatField("delay", 0f, float.PositiveInfinity, 0f, control: ManagedFieldWithPanel.ControlType.text),
		new BooleanField("malnourishing", false),
		POMUtils.defaultVectorField
	];


	/// <summary>
	/// how much seconds will pass between triggering polygon and striking cat
	/// </summary>
	public float delay => data.GetValue<float>("delay");
	/// <summary>
	/// trigger zone
	/// </summary>
	public Vector2[]? Polygon => POMUtils.AddRealPosition(data.GetValue<Vector2[]>("trigger zone"), pObj.pos);
	/// <summary>
	/// how bad the asthma attack will be, preferably within 0-1
	/// </summary>
	public float asthmaSeconds => data.GetValue<float>("effect time");
	public float severity => data.GetValue<float>("severity");
	public bool triggersMalnourishment => data.GetValue<bool>("malnourishing");

	ManagedData data;
	PlacedObject pObj;

	public RedInducedIllness(Room room, PlacedObject pObj)
	{
		this.pObj = pObj;
		data = (pObj.data as ManagedData)!;
	}

	enum State
	{
		armed,
		tickingBeforeFiring,
		working,
		disarmed
	}
	uint timer;
	State state = State.armed;
	Asthma? effect = null;

	public override void Update(bool eu)
	{
		if (!room.updateList.Exists(x => x is Player)) return;
		base.Update(eu);
		switch(state)
		{
			case State.armed:
				{
					if (room.world.game.Players.Exists(x =>
					{
						return (x.realizedCreature is Player p)
						&& (p.slugcatStats.name == SlugcatStats.Name.Red)
						&& ROMUtils.PositionWithinPoly(Polygon, x.realizedCreature.mainBodyChunk.pos);
					}))
					{
						state = State.tickingBeforeFiring;
					}
						break;
				}
			case State.tickingBeforeFiring:
				{
					timer++;
					if(timer >= delay * StaticStuff.TicksPerSecond)
					{
						var redCat = room.world.game.Players.First(x => (x.realizedCreature is Player p) && (p.slugcatStats.name == SlugcatStats.Name.Red)).realizedCreature as Player;
						if (redCat is null) return;
						state = State.working;
						AsthmaTime(redCat);
					}
					break;
				}
			case State.working:
				{
					if (effect == null || effect.slatedForDeletetion)
					{
						effect = null; //yes i know it looks weird, but if i was to make it logically correct, it would be a bit more complicated
						state = State.disarmed;
						slatedForDeletetion = true;  
					}
						break;
				}
			default:
				{
					break;
				}
		}

	}
	void AsthmaTime(Player redCat)
	{
		effect = new Asthma(redCat, asthmaSeconds, triggersMalnourishment, severity);
		MainLogic.logger.LogInfo("making asthma with " + asthmaSeconds + " seconds of time");
		room.AddObject(effect);
	}
}

/// <summary>
/// Regionkit breaks with OOB index in playergraphics when assigning redsIllness ??= new(...), so we'll stick to our own object instead
/// </summary>
public class Asthma : CosmeticSprite
{
	public Asthma(Player redCat, float strSeconds, bool malnourishing, float severity = 1f)
	{
		this.redCat = redCat;
		strokeSeconds = strSeconds;
		this.malnourishing = malnourishing;
		this.fitSeverity = severity;
	}

	DisembodiedDynamicSoundLoop? soundLoop;
	Player redCat;
	bool malnourishing;
	float strokeSeconds, fitSeverity, progress = 0, sin, fluc, fluc1, fluc2, fluc3, rotDir;
	float strokeTicks => strokeSeconds * StaticStuff.TicksPerSecond;
	float fit => progress;
	float CurrentFitIntensity => Mathf.Pow(Mathf.Clamp01(Mathf.Sin(fit * 3.1415927f) * 1.2f), 1.6f) * fitSeverity;
	public override void Update(bool eu)
	{
		base.Update(eu);
		if (room == null) return;
		progress += 1f / strokeTicks;
		if (progress >= 1f) Destroy();
		
		lastFade = fade;
		lastViableFade = viableFade;
		lastRot = rot;
		sin += 1f / Mathf.Lerp(120f, 30f, fluc3);
		fluc = RWCustom.Custom.LerpAndTick(fluc, fluc1, 0.02f, 0.016666668f);
		fluc1 = RWCustom.Custom.LerpAndTick(fluc1, fluc2, 0.02f, 0.016666668f);
		fluc2 = RWCustom.Custom.LerpAndTick(fluc2, fluc3, 0.02f, 0.016666668f);
		if (Mathf.Abs(fluc2 - fluc3) < 0.01f)
		{
			fluc3 = UnityEngine.Random.value;
		}
		fade = Mathf.Pow(CurrentFitIntensity * (0.85f + 0.15f * Mathf.Sin(sin * 3.1415927f * 2f)), Mathf.Lerp(1.5f, 0.5f, fluc));
		rot += rotDir * fade * (0.5f + 0.5f * fluc) * 7f * (0.1f + 0.9f * Mathf.InverseLerp(1f, 4f, Vector2.Distance(redCat.firstChunk.lastLastPos, redCat.firstChunk.pos)));
		if (!RedsIllness.RedsIllnessEffect.CanShowPlayer(redCat) || redCat.room != room)
		{
			viableFade = Mathf.Max(0f, viableFade - 0.033333335f);
			if (viableFade <= 0f && lastViableFade <= 0f)
			{
				//Destroy();
			}
		}
		else
		{
			viableFade = Mathf.Min(1f, viableFade + 0.033333335f);
			pos = (redCat.firstChunk.pos * 2f + redCat.bodyChunks[1].pos) / 3f;
		}
		if (fade == 0f && lastFade > 0f)
		{
			rotDir = ((UnityEngine.Random.value < 0.5f) ? (-1f) : 1f);
		}
		SoundLoopLogic();
		PlayerInteraction();
	}

	private void PlayerInteraction()
	{
		MainLogic.logger.LogInfo("player interaction. aerobic level is " + redCat.aerobicLevel);
		redCat.aerobicLevel = Mathf.Max(CurrentFitIntensity, redCat.aerobicLevel);
		if(redCat.aerobicLevel >= .8f) redCat.exhausted = true;
		redCat.SetMalnourished(malnourishing);
	}

	public override void Destroy()
	{
		base.Destroy();
		if (soundLoop?.emitter != null)
		{
			soundLoop.emitter.slatedForDeletetion = true;
		}
	}
	void SoundLoopLogic()
	{
		if (soundLoop == null && fade > 0f)
		{
			soundLoop = new DisembodiedDynamicSoundLoop(this)
			{
				sound = SoundID.Reds_Illness_LOOP,
				VolumeGroup = 1
			};
			return;
		}
		if (soundLoop != null)
		{
			soundLoop.Update();
			soundLoop.Volume = RWCustom.Custom.LerpAndTick(soundLoop.Volume, Mathf.Pow((fade + CurrentFitIntensity) / 2f, 0.5f), 0.06f, 0.14285715f);
		}
	}

	#region Graphics
	float TotFade(float timeStacker) => Mathf.Lerp(lastFade, fade, timeStacker) * Mathf.Lerp(lastViableFade, viableFade, timeStacker);
	float fade;
	float lastFade;
	float viableFade;
	float lastViableFade;
	float rot;
	float lastRot;
	public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{
		base.InitiateSprites(sLeaser, rCam);
		sLeaser.sprites = [new FSprite("Futile_White", true) {shader = rCam.game.rainWorld.Shaders["RedsIllness"]}];
		AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("GrabShaders"));
	}
	public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
		FSprite sprite = sLeaser.sprites[0];
		float num = TotFade(timeStacker);
		if (num == 0f)
		{
			sprite.isVisible = false;
			return;
		}
		sprite.isVisible = true;
		sprite.x = Mathf.Clamp(Mathf.Lerp(lastPos.x, pos.x, timeStacker) - camPos.x, 0f, rCam.sSize.x);
		sprite.y = Mathf.Clamp(Mathf.Lerp(lastPos.y, pos.y, timeStacker) - camPos.y, 0f, rCam.sSize.y);
		sprite.rotation = Mathf.Lerp(lastRot,   rot, timeStacker);
		sprite.scaleX = (rCam.sSize.x * (6f - 3f * num) + 2f) / 16f;
		sprite.scaleY = (rCam.sSize.x * (6f - 3f * num) + 2f) / 16f;
		sprite.color = new Color(num, num, 0f, 0f);
	}
	#endregion
}