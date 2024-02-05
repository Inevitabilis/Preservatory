using PVStuffMod;
using System;
using UnityEngine;

namespace PVStuff.Logic.ROM_objects;

public class VatScene : UpdatableAndDeletable, IDrawable
{
    //ROM fields
    public float timeFlashTakesToFadeIn, timeFlashTakesToFadeOut, idlingTime, blackScreenTime;
    public Vector2 position;



    bool controlTaken = false;
    State state = State.fadingIn;
    uint fadingInTimer = 0, idlingTimer = 0, fadingOutTimer = 0, blackscreenTimer = 0;
    float flareStrength
    {
        get
        {
            if (state == State.fadingIn) return Mathf.InverseLerp(timeFlashTakesToFadeIn * StaticStuff.TicksPerSecond, 0, fadingInTimer);
            if (state == State.fadingOut) return Mathf.InverseLerp(0, timeFlashTakesToFadeOut * StaticStuff.TicksPerSecond, fadingOutTimer);
            throw new IndexOutOfRangeException("Flarestrength was requested in wrong initiate phase");
        }
    }
    float previousFrameFlareStrength
    {
        get
        {
            if (state == State.fadingIn) return Mathf.InverseLerp(timeFlashTakesToFadeIn * StaticStuff.TicksPerSecond, 0, fadingInTimer == 0 ? 0 : fadingInTimer - 1);
            if (state == State.fadingOut) return Mathf.InverseLerp(0, timeFlashTakesToFadeOut * StaticStuff.TicksPerSecond, fadingOutTimer == 0 ? 0 : fadingOutTimer - 1);
            throw new IndexOutOfRangeException("PreviousFlareStrength was requested in wrong initiate phase");
        }
    }
    enum State
    {
        fadingIn,
        idling,
        fadingOut,
        blackScreen
    }
        

    public override void Update(bool eu)
    {
        base.Update(eu);
        switch(state)
        {
            case State.fadingIn:
                {
                    fadingInTimer++;
                    if (!controlTaken)
                    {
                        room.game.Players.ForEach(player => player.controlled = false);
                        controlTaken = true;
                    }
                    if (fadingInTimer >= timeFlashTakesToFadeIn * StaticStuff.TicksPerSecond) state = State.idling;
                    break;
                }
            case State.idling:
                {
                    idlingTimer++;
                    if (idlingTimer >= idlingTime * StaticStuff.TicksPerSecond) state = State.fadingOut;
                    break;
                }
            case State.fadingOut:
                {
                    fadingOutTimer++;
                    if (fadingOutTimer >= timeFlashTakesToFadeOut * StaticStuff.TicksPerSecond) state = State.blackScreen;
                    break;
                }
            case State.blackScreen:
                {
                    blackscreenTimer++;
                    if (blackscreenTimer >= blackScreenTime * StaticStuff.TicksPerSecond) room.game.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Credits);
                    break;
                }
        }
    }

    #region graphical stuff
    public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        sLeaser.sprites = new FSprite[1];
        sLeaser.sprites[0] = new FSprite("Futile_White", true);
        sLeaser.sprites[0].alpha = 1f;
        sLeaser.sprites[0].shader = rCam.game.rainWorld.Shaders["FlatLightNoisy"];
        AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("Foreground"));
    }

    public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        if (state == State.idling)
        {
            sLeaser.sprites[0].color = Color.black;
        }
        sLeaser.sprites[0].scale = Mathf.Lerp(150f, 300f, Mathf.Pow(flareStrength, 4f));
        sLeaser.sprites[0].alpha = 0.25f * Mathf.Lerp(previousFrameFlareStrength, flareStrength, timeStacker);
        sLeaser.sprites[0].x = position.x - camPos.x;
        sLeaser.sprites[0].y = position.y - camPos.y;
        if (slatedForDeletetion || room != rCam.room) sLeaser.CleanSpritesAndRemove();
    }

    public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
    {
        if (newContatiner == null) newContatiner = rCam.ReturnFContainer("Water");
        foreach (FSprite fsprite in sLeaser.sprites)
        {
            fsprite.RemoveFromContainer();
            newContatiner.AddChild(fsprite);
        }
    }

    public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera camera, RoomPalette palette)
    {
    }
    #endregion
}
