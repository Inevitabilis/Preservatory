using UnityEngine;
using System;

namespace PVStuffMod.Logic;
/// <summary>
/// as of currently the intent of ScreenFlasher is to be a room nonspecific object that would be a generalized screen flash
/// because it's been happening in multiple places, there was a decision to make it a separate thing
/// it's event driven, the hash that is being passed in to request it to start working is used to know which object should act on given event
/// </summary>
public class ScreenFlasher : IDrawable, IReceiveWorldTicks
{
    int SummonerHash, ticksToFadeIn, ticksToFadeOut, idlingTicks, colorLerpingTicks;
    Color color, previousColor, colorToLerpTo;
    State state = State.Idle;
    public bool SlatedForDeletion
    {
        get; private set;
    }
    

    int timer, colorTimer;
    int PreviousTick
    {
        get { return timer > 0 ? timer - 1 : 0; }
    }
    float FlareStrength
    {
        get
        {
            switch (state)
            {
                case State.fadingIn:
                    {
                        return Mathf.InverseLerp(0, ticksToFadeIn, timer);
                    }
                case State.fadingOut:
                    {
                        return Mathf.InverseLerp(ticksToFadeOut, 0, timer);
                    }
                case State.idlingFilled: return 1;
                default: return 0;
            }
        }
    }
    float PreviousFrameFlareStrength
    {
        get
        {
            switch (state)
            {
                case State.fadingIn:
                    {
                        return Mathf.InverseLerp(0, ticksToFadeIn, PreviousTick);
                    }
                case State.fadingOut:
                    {
                        return Mathf.InverseLerp(ticksToFadeOut, 0, PreviousTick);
                    }
                case State.idlingFilled: return 1;
                default: return 0;
            }
        }
    }


    public event Action<int>? TickAtTheEndOfWhiteScreen, TickOnFill, TickOnCompletion;
    enum State
    {
        Idle,
        fadingIn,
        idlingFilled,
        fadingOut
    }

    public void Update()
    {
        switch (state)
        {
            case State.Idle:
                {
                    return;
                }
            case State.fadingIn:
                {
                    timer++;
                    if (timer >= ticksToFadeIn)
                    {
                        timer = 0;
                        state = State.idlingFilled;
                        TickOnFill?.Invoke(SummonerHash);
                    }
                    break;
                }
            case State.idlingFilled:
                {
                    timer++;
                    if (timer >= idlingTicks)
                    {
                        timer = 0;
                        state = State.fadingOut;
                        TickAtTheEndOfWhiteScreen?.Invoke(SummonerHash);
                    }
                    break;
                }
            case State.fadingOut:
                {
                    timer++;
                    if (timer >= ticksToFadeOut)
                    {
                        timer = 0;
                        SlatedForDeletion = true;
                        state = State.Idle;
                        TickOnCompletion?.Invoke(SummonerHash);
                    }
                    break;
                }
        }

        //color logic
        if (color != colorToLerpTo)
        {
            color = Color.Lerp(previousColor, colorToLerpTo, colorTimer / (float)colorLerpingTicks);
            colorTimer++;
        }
        else
        {
            previousColor = color;
        }


    }
    public void RequestScreenFlash(int ownerHash,
        Color color,
        int ticksToFadeIn = StaticStuff.TicksPerSecond * 1,
        int idleTicks = StaticStuff.TicksPerSecond * 3,
        int ticksToFadeOut = StaticStuff.TicksPerSecond * 1)
    {
        SummonerHash = ownerHash;
        this.ticksToFadeIn = ticksToFadeIn;
        idlingTicks = idleTicks;
        this.ticksToFadeOut = ticksToFadeOut;
        this.color = color;
        colorToLerpTo = color;
        state = State.fadingIn;
    }
    public void RequestColorChange(Color color, int ticksToApply = StaticStuff.TicksPerSecond * 1)
    {
        colorToLerpTo = color;
        colorLerpingTicks = ticksToApply;
    }
    #region Graphics
    public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        sLeaser.sprites = new FSprite[1];
        sLeaser.sprites[0] = new FSprite("Futile_White", true)
        {
            alpha = 0f,
            shader = rCam.game.rainWorld.Shaders["FlatLightNoisy"]
        };
        AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("Bloom"));
    }

    public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        sLeaser.sprites[0].scale = Mathf.Lerp(150f, 1000f, Mathf.Pow(FlareStrength, 4f));
        sLeaser.sprites[0].alpha = Mathf.Lerp(PreviousFrameFlareStrength, FlareStrength, timeStacker);
        sLeaser.sprites[0].x = camPos.x;
        sLeaser.sprites[0].y = camPos.y;
    }

    public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
    {
        newContatiner ??= rCam.ReturnFContainer("Water");
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
