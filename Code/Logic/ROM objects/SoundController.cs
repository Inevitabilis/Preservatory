using Newtonsoft.Json.Linq;
using ROM.RoomObjectService;
using ROM.UserInteraction.InroomManagement;
using ROM.UserInteraction.ObjectEditorElement;
using ROM.UserInteraction.ObjectEditorElement.Scrollbar;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using static PVStuffMod.StaticStuff;
using static PVStuffMod.MainLogic;

namespace PVStuffMod.Logic.ROM_objects;
/// <summary>
/// ROM can only work with room specific objects as of now, so to prevent it from disappearing the exposed objects are different
/// </summary>
public class InternalSoundController : UpdatableAndDeletable, IReceiveWorldTicks
{
    DisembodiedLoopEmitter[]? disembodiedLoopEmitters;
    public ExposedSoundController? controllerReference;
    
    public void Update()
    {
        if (room == null) return;
        SoundLoopMaintenance();
        VolumeSlidersLogic();
    }
    DisembodiedLoopEmitter CreateNewSoundLoop(SoundID? soundID, float vol, float pitch, float pan)
    {
        DisembodiedLoopEmitter emitter = new(vol, pitch, pan);
        for (int i = 0; i < room.game.cameras.Length; i++)
        {
            if (room.game.cameras[i].room == room)
            {
                PlayRoomlessDisembodiedLoop(room.game.cameras[i].virtualMicrophone, soundID, emitter, pan, vol, pitch);
            }
        }
        return emitter;
    }
    void SoundLoopMaintenance()
    {
        VirtualMicrophone virtualMicrophone = room.game.cameras[0].virtualMicrophone;
        if (disembodiedLoopEmitters == null || !virtualMicrophone.soundObjects.Exists(x => x is RoomlessDisembodiedLoop loop && loop.controller == disembodiedLoopEmitters[0]))
        {
            disembodiedLoopEmitters =
            [
                CreateNewSoundLoop(Melody.approach0, 0, 1, 1),
                CreateNewSoundLoop(Melody.approach1, 0, 1, 1),
                CreateNewSoundLoop(Melody.approach2, 0, 1, 1),
                CreateNewSoundLoop(Melody.approach3, 0, 1, 1),
            ];
            Array.ForEach(disembodiedLoopEmitters, x => x.requireActiveUpkeep = false);
        }
    }
    void VolumeSlidersLogic()
    {
        if (disembodiedLoopEmitters == null) return;
        
        for (short i = 0; i < disembodiedLoopEmitters.Length; i++)
        {
            disembodiedLoopEmitters[i].volume = Mathf.Lerp(disembodiedLoopEmitters[i].volume, (controllerReference ?? new()).volumeSliders[i], 0.1f);
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
    public Vector2[] Polygon { get; set; } =
        {
            new Vector2(500, 500),
            new Vector2(500, 600),
            new Vector2(600, 600),
            new Vector2(600, 500)
        };
    public float[] volumeSliders = new float[4];
    public float linger = 0;

    private const float RandomOffsetOnCreationMultiplier = 100f;
    private int lingerTimer;
    #endregion

    #region methods
    public override void Update(bool eu)
    {
        base.Update(eu);

        UpdateSoundController();

        if (lingerTimer > 0) lingerTimer--;
        else if (internalSoundController.controllerReference == this) internalSoundController.controllerReference = null;
        if (room.game.AlivePlayers.Exists(abstractCreature => abstractCreature.Room == room.abstractRoom && ROMUtils.PositionWithinPoly(Polygon, abstractCreature.realizedCreature.mainBodyChunk.pos)))
        {
            internalSoundController.controllerReference = this;
            lingerTimer = (int)(linger * (float)StaticStuff.TicksPerSecond);
        }
    }
    private void UpdateSoundController()
    {
        internalSoundController.room ??= this.room;
    }


    #endregion
}

public class ExposedSoundControllerOperator : TypeOperator<ExposedSoundController>
{
    private static VersionedLoader<ExposedSoundController> VersionedLoader { get; } =
            TypeOperatorUtils.CreateVersionedLoader<ExposedSoundController>(defaultLoad: TypeOperatorUtils.TrivialLoad<ExposedSoundController>);

    public override string TypeId => nameof(ExposedSoundController);

    public override ExposedSoundController CreateNew(Room room, Rect currentScreenRect)
    {
        Vector2 center = currentScreenRect.center;

        float screenWidth = currentScreenRect.width;
        float screenHeight = currentScreenRect.height;

        Vector2[] polygon = {
                center + new Vector2(screenWidth/8, screenHeight/8),
                center + new Vector2(screenWidth/8, - screenHeight/8),
                center + new Vector2(-screenWidth/8, -screenHeight/8),
                center + new Vector2(-screenWidth/8, screenHeight/8)
            };
        return new()
        {
            room = room,
            Polygon = polygon
        };
    }
    public override ExposedSoundController Load(JToken dataJson, Room room)
    {
        return VersionedLoader.Load(dataJson, room);
    }

    public override JToken Save(ExposedSoundController obj)
    {
        return TypeOperatorUtils.GetTrivialVersionedSaveCall<ExposedSoundController>("0.0.0").Invoke(obj);
    }

    public override void AddToRoom(ExposedSoundController obj, Room room)
    {
        room.AddObject(obj);
    }

    public override void RemoveFromRoom(ExposedSoundController obj, Room room)
    {
        room.RemoveObject(obj);
    }

    public override IEnumerable<IObjectEditorElement> GetEditorElements(ExposedSoundController obj, Room room)
    {
        yield return Elements.Polygon("Trigger Zone", obj.Polygon);
        new ScrollbarConfiguration<float>(0f, 1f, x => x, x => x, x => x.ToString("0.##", CultureInfo.InvariantCulture));
        yield return Elements.Scrollbar("Melody 0", getter: () => obj.volumeSliders[0], setter: value => obj.volumeSliders[0] = value);
        yield return Elements.Scrollbar("Melody 1", getter: () => obj.volumeSliders[1], setter: value => obj.volumeSliders[1] = value);
        yield return Elements.Scrollbar("Melody 2", getter: () => obj.volumeSliders[2], setter: value => obj.volumeSliders[2] = value);
        yield return Elements.Scrollbar("Melody 3", getter: () => obj.volumeSliders[3], setter: value => obj.volumeSliders[3] = value);
        yield return Elements.TextField("Lingering", getter: () => obj.linger, setter: x => obj.linger = x);
    }
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