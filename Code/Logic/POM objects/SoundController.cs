using System;
using UnityEngine;
using static Pom.Pom;

namespace PVStuffMod.Logic.POM_objects;
/// <summary>
/// ROM can only work with room specific objects as of now, so to prevent it from disappearing the exposed objects are different
/// </summary>
public class InternalSoundController : IReceiveWorldTicks
{
	public InternalSoundController(RainWorldGame game)
	{
		weakRefGame = new WeakReference<RainWorldGame>(game);
	}

	public WeakReference<DisembodiedLoopEmitter>[]? disembodiedLoopEmitters;
	public WeakReference<RainWorldGame> weakRefGame;
	public bool SlatedForDeletion => !weakRefGame.TryGetTarget(out _);
	public bool ShouldWork;

	public ExposedSoundController? controllerReference { get; private set; }
	int ticksSinceCRefSet;
	float fadeProgressionPerTick;
	float[] volumesAtStartOfChange = [0, 0, 0, 0];
	float[] desiredVolumes = [0, 0, 0, 0];
	public void SetControllerReference(ExposedSoundController? exposedSoundController, float fadeProgressionPerTick)
	{
		controllerReference = exposedSoundController;
		ticksSinceCRefSet = 0;
		this.fadeProgressionPerTick = fadeProgressionPerTick;

		if (exposedSoundController is not null)
		{
			for (short i = 0; i < desiredVolumes.Length; i++)
			{
				desiredVolumes[i] = exposedSoundController.volumeSliders[i];
			}
		}
		else desiredVolumes = [0, 0, 0, 0];


		if (disembodiedLoopEmitters != null)
		{
			for (short i = 0; i < volumesAtStartOfChange.Length; i++)
			{
				if (disembodiedLoopEmitters[i].TryGetTarget(out var emitter))
				{
					volumesAtStartOfChange[i] = emitter.volume;
				}
				else MainLogic.logger.LogError("InternalSoundController.SetControllerReference: weakref array at " + i + " was empty");
			}
		}
		else MainLogic.logger.LogError("InternalSoundController.SetControllerReference: weakref array to loop emitters was null");
	}


	public void Update()
	{
		if(!ShouldWork 
			&& disembodiedLoopEmitters is not null 
			&& disembodiedLoopEmitters[0].TryGetTarget(out var emitter)
			&& weakRefGame.TryGetTarget(out var game1))
		{
			VirtualMicrophone vm1 = game1.cameras[0].virtualMicrophone;
			foreach (var soundloopweakref in disembodiedLoopEmitters)
			{
				if (soundloopweakref.TryGetTarget(out var soundloopEmitter)) vm1.soundObjects.Remove(soundloopEmitter.currentSoundObject);
			}
		}

		if (ShouldWork && weakRefGame.TryGetTarget(out var game))
		{
			VirtualMicrophone virtualMicrophone = game.cameras[0].virtualMicrophone;

			//create disembodied soundloops if they don't exist yet
			if (disembodiedLoopEmitters == null || !disembodiedLoopEmitters[0].TryGetTarget(out _))
			{
				disembodiedLoopEmitters =
				[
					new(CreateNewSoundLoop(PVEnums.Melody.approach0, 0, 1, 0)),
					new(CreateNewSoundLoop(PVEnums.Melody.approach1, 0, 1, 0)),
					new(CreateNewSoundLoop(PVEnums.Melody.approach2, 0, 1, 0)),
					new(CreateNewSoundLoop(PVEnums.Melody.approach3, 0, 1, 0)),
				];
				Array.ForEach(disembodiedLoopEmitters, x =>
				{
					if (x.TryGetTarget(out var target))
					{
						target.requireActiveUpkeep = false;
					}
				});
			}
			//sound logic
			for (short i = 0; i < disembodiedLoopEmitters.Length; i++)
			{
				if (disembodiedLoopEmitters[i].TryGetTarget(out var disembodiedLoopEmitter))
				{
					var currentSCurveXProgression = Mathf.Min(ticksSinceCRefSet * fadeProgressionPerTick, 1f);
					var currentSCurveYProgression = RWCustom.Custom.SCurve(currentSCurveXProgression, 0.5f);
					disembodiedLoopEmitter.volume = Mathf.Lerp(volumesAtStartOfChange[i], desiredVolumes[i], currentSCurveYProgression);
				}
			}
		}
		ticksSinceCRefSet++;
	}


	DisembodiedLoopEmitter CreateNewSoundLoop(SoundID? soundID, float vol, float pitch, float pan)
	{
		DisembodiedLoopEmitter emitter = new(vol, pitch, pan);
		if (weakRefGame.TryGetTarget(out var game))
		{
			PlayRoomlessDisembodiedLoop(game.cameras[0].virtualMicrophone, soundID, emitter, pan, vol, pitch);
		}
		return emitter;
	}
	static void PlayRoomlessDisembodiedLoop(VirtualMicrophone mic, SoundID? soundId, DisembodiedLoopEmitter emitter, float pan, float vol, float pitch)
	{
		if (!mic.AllowSound(soundId))
		{
			return;
		}
		if (!mic.soundLoader.TriggerPlayAll(soundId))
		{
			SoundLoader.SoundData soundData = mic.GetSoundData(soundId, -1);
			if (mic.SoundClipReady(soundData))
			{
				mic.soundObjects.Add(new RoomlessDisembodiedLoop(mic, soundData, emitter, pan, vol, pitch, false));
				return;
			}
		}
		else
		{
			for (int i = 0; i < mic.soundLoader.TriggerSamples(soundId); i++)
			{
				SoundLoader.SoundData soundData2 = mic.GetSoundData(soundId, i);
				if (mic.SoundClipReady(soundData2))
				{
					mic.soundObjects.Add(new RoomlessDisembodiedLoop(mic, soundData2, emitter, pan, vol, pitch, false));
				}
			}
		}
	}


}

public class ExposedSoundController : UpdatableAndDeletable
{
	#region fields
	public static void RegisterObject()
	{
		RegisterFullyManagedObjectType(managedFields, typeof(ExposedSoundController), "PVSoundController", StaticStuff.PreservatoryPOMCategory);
	}

	//ROM fields
	internal static ManagedField[] managedFields = [
		new FloatField("linger",
			min: 0f,
			max: 20f,
			defaultValue: 0f,
			control: ManagedFieldWithPanel.ControlType.slider,
			displayName: "Linger"),
		new FloatField("Melody 1", 0f, 1f, 0f),
		new FloatField("Melody 2", 0f, 1f, 0f),
		new FloatField("Melody 3", 0f, 1f, 0f),
		new FloatField("Melody 4", 0f, 1f, 0f),
		POMUtils.defaultVectorField,
		new FloatField("FadeInSeconds",
			min: 0.1f,
			max: 20f,
			defaultValue: 1f,
			control: ManagedFieldWithPanel.ControlType.slider),
		new FloatField("FadeOutSeconds",
			min: 0.1f,
			max: 20f,
			defaultValue: 1f,
			control: ManagedFieldWithPanel.ControlType.slider),
	];

	public Vector2[]? Polygon => POMUtils.AddRealPosition(data.GetValue<Vector2[]>("trigger zone"), pObj.pos);
	public float[] volumeSliders => [
		data.GetValue<float>("Melody 1"),
		data.GetValue<float>("Melody 2"),
		data.GetValue<float>("Melody 3"),
		data.GetValue<float>("Melody 4")];
	float FadeInTicks => data.GetValue<float>("FadeInSeconds") * 40;
	public float FadeInTickIncrement => 1f / FadeInTicks;

	public float FadeOutTicks => data.GetValue<float>("FadeOutSeconds") * 40;
	public float FadeOutTickIncrement => 1f / FadeOutTicks;
	float linger => data.GetValue<float>("linger");

	private int lingerTimer;

	ManagedData data;
	PlacedObject pObj;
	#endregion

	#region methods
	public ExposedSoundController(Room room, PlacedObject pObj)
	{
		this.pObj = pObj;
		data = (pObj.data as ManagedData)!;
	}

	bool PlayerInZone => Polygon is not null
		&& room.PlayersInRoom.Exists(Player => ROMUtils.PositionWithinPoly(Polygon, Player.mainBodyChunk.pos));
	public override void Update(bool eu)
	{
		base.Update(eu);
		if (PlayerInZone)
		{
			if (MainLogic.internalSoundControllerRef.TryGetValue(room.game, out var intSoundController)
				&& intSoundController.controllerReference != this)
			{
				intSoundController.SetControllerReference(this, FadeInTickIncrement);
			}
			lingerTimer = (int)(linger * StaticStuff.TicksPerSecond);
		}
		else
		{
			if (lingerTimer > 0) lingerTimer--;
			else if (MainLogic.internalSoundControllerRef.TryGetValue(room.game, out var internalSoundController)
				&& internalSoundController.controllerReference == this) internalSoundController.SetControllerReference(null, FadeOutTickIncrement);
		}
	}
	#endregion
}

public class RoomlessDisembodiedLoop : VirtualMicrophone.SoundObject
{
	public override bool Done
	{
		get
		{
			return ((audioSource == null || (!audioSource.isPlaying && loadOp == null)) && (!soundData.dontAutoPlay || allowPlay)) || controller.slatedForDeletetion;
		}
	}

	public override bool PlayAgain
	{
		get
		{
			return audioSource != null && loop && !controller.slatedForDeletetion;
		}
	}

	public RoomlessDisembodiedLoop(VirtualMicrophone mic, SoundLoader.SoundData sData, DisembodiedLoopEmitter controller, float pan, float volume, float pitch, bool startAtRandomTime)
		: base(mic, sData, true, pan, volume, pitch, startAtRandomTime)
	{
		this.controller = controller;
	}

	public override void Update(float timeStacker, float timeSpeed)
	{
		SetPitch = controller.pitch;
		SetVolume = controller.volume;
		audioSource.panStereo = controller.pan;
		volumeGroup = controller.volumeGroup;
		controller.lastSoundPlayingFrame = Time.frameCount;
		controller.soundStillPlaying = true;
		base.Update(timeStacker, timeSpeed);
		if(aspi is not null && aspi.alpf is not null)   aspi.alpf.cutoffFrequency = 22000f;
	}

	public DisembodiedLoopEmitter controller;
}