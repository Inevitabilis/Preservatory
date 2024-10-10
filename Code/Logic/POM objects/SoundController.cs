using System;
using UnityEngine;
using static Pom.Pom;
using static PVStuffMod.MainLogic;

namespace PVStuffMod.Logic.POM_objects;
/// <summary>
/// ROM can only work with room specific objects as of now, so to prevent it from disappearing the exposed objects are different
/// </summary>
public class InternalSoundController : IReceiveWorldTicks
{
    public WeakReference<DisembodiedLoopEmitter>[]? disembodiedLoopEmitters;
    public ExposedSoundController? controllerReference;
    public bool SlatedForDeletion => !weakRefGame.TryGetTarget(out _);
    public WeakReference<RainWorldGame> weakRefGame;
    public InternalSoundController(RainWorldGame game)
    {
        weakRefGame = new WeakReference<RainWorldGame>(game);
    }


    public void Update()
    {
        SoundLoopMaintenance();
        VolumeSlidersLogic();
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
    void SoundLoopMaintenance()
    {
        if (weakRefGame.TryGetTarget(out var game))
        {
            VirtualMicrophone virtualMicrophone = game.cameras[0].virtualMicrophone;
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
        }
    }
    void VolumeSlidersLogic()
    {
        if (disembodiedLoopEmitters == null) return;

        for (short i = 0; i < disembodiedLoopEmitters.Length; i++)
        {
            if(disembodiedLoopEmitters[i].TryGetTarget(out var disembodiedLoopEmitter))
            {
                disembodiedLoopEmitter.volume = Mathf.Lerp(disembodiedLoopEmitter.volume, controllerReference?.volumeSliders[i] ?? 0f, 0.1f);
            }
        }
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
    //ROM fields

    public static void RegisterObject()
    {
        RegisterFullyManagedObjectType(managedFields, typeof(ExposedSoundController), "PVSoundController", StaticStuff.PreservatoryPOMCategory);
    }

    //ROM fields
    internal static ManagedField[] managedFields = [
        new FloatField("linger", 0, float.PositiveInfinity, 0f, control: ManagedFieldWithPanel.ControlType.text),
        new FloatField("Melody 1", 0, 1f, 0f),
        new FloatField("Melody 2", 0, 1f, 0f),
        new FloatField("Melody 3", 0, 1f, 0f),
        new FloatField("Melody 4", 0, 1f, 0f),
        POMUtils.defaultVectorField
    ];

    public Vector2[]? Polygon => POMUtils.AddRealPosition(data.GetValue<Vector2[]>("trigger zone"), pObj.pos);
    public float[] volumeSliders => [
        data.GetValue<float>("Melody 1"),
        data.GetValue<float>("Melody 2"),
        data.GetValue<float>("Melody 3"),
        data.GetValue<float>("Melody 4")];
    public float linger => data.GetValue<float>("linger");

    private const float RandomOffsetOnCreationMultiplier = 100f;
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

    public override void Update(bool eu)
    {
        base.Update(eu);
        if (lingerTimer > 0) lingerTimer--;
        else if (internalSoundControllerRef.TryGetValue(room.game, out var internalSoundController) && internalSoundController.controllerReference == this) internalSoundController.controllerReference = null;
        if (room.game.AlivePlayers.Exists(abstractCreature => abstractCreature.Room == room.abstractRoom
        && abstractCreature.realizedCreature != null
        && ROMUtils.PositionWithinPoly(Polygon, abstractCreature.realizedCreature.mainBodyChunk.pos))
            && internalSoundControllerRef.TryGetValue(room.game, out var internalSoundController2))
        {
            internalSoundController2.controllerReference = this;
            lingerTimer = (int)(linger * (float)StaticStuff.TicksPerSecond);
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
    }

    public DisembodiedLoopEmitter controller;
}