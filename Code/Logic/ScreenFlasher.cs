﻿using UnityEngine;
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
	VirtualMicrophone? microphone;
	Color color, previousColor, colorToLerpTo;
	State state = State.readyForAction;
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
	public bool testingCenterOfScreen = false;


	public event Action<int>? TickAtTheEndOfWhiteScreen, TickOnFill, TickOnCompletion, TickInTheMiddleOfIdling;
	enum State
	{
		readyForAction,
		fadingIn,
		idlingFilled,
		fadingOut,
		waitingForRemoval
	}

	public void Update()
	{
		switch (state)
		{
			case State.readyForAction:
				{
					return;
				}
			case State.fadingIn:
				{
					timer++;
					if(microphone != null)
					{
						microphone.globalSoundMuffle = FlareStrength;
					}
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
					if(timer == idlingTicks/2) TickInTheMiddleOfIdling?.Invoke(SummonerHash);
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
					if (microphone != null)
					{
						microphone.globalSoundMuffle = FlareStrength;
					}
					if (timer >= ticksToFadeOut)
					{
						timer = 0;
						SlatedForDeletion = true;
						state = State.waitingForRemoval;
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
		VirtualMicrophone virtualMicrophone,
		Color color,
		int ticksToFadeIn = StaticStuff.TicksPerSecond * 1,
		int idleTicks = StaticStuff.TicksPerSecond * 3,
		int ticksToFadeOut = StaticStuff.TicksPerSecond * 1)
	{
		if (state != State.readyForAction) return;
		this.microphone = virtualMicrophone;
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
		sLeaser.sprites = new FSprite[testingCenterOfScreen ? 2 : 1];
		for(int i = 0;  i < sLeaser.sprites.Length; i++) 
		{
			sLeaser.sprites[i] = new FSprite("Futile_White", true)
			{
				alpha = 0f,
			};
		}
		sLeaser.sprites[0].shader = rCam.game.rainWorld.Shaders["FlatLightNoisy"];
		AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("Bloom"));
	}

	public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		Array.ForEach(sLeaser.sprites, sprite =>
		{
			sprite.scale = Mathf.Lerp(100f, 1000f, Mathf.Pow(Mathf.Lerp(PreviousFrameFlareStrength, FlareStrength, timeStacker), 4f));
			sprite.alpha = Mathf.Lerp(PreviousFrameFlareStrength, FlareStrength, timeStacker);
			sprite.color = this.color;
			sprite.SetPosition(rCam.sSize/2);            
		});
		if(testingCenterOfScreen)
		{
			sLeaser.sprites[1].color = Color.green;
			sLeaser.sprites[1].alpha = 1f;
			sLeaser.sprites[1].scale = 1f;
		}
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
