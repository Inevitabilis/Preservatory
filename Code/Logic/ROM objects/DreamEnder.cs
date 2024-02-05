using UnityEngine;
using static PVStuffMod.StaticStuff;
namespace PVStuff.Logic.ROM_objects;

public class DreamEnder : UpdatableAndDeletable, IDrawable
{
    Vector2 position;
    float radius;
    uint timer;
    uint delayTimer;
    float delay;
    State state = DreamEnder.State.awaitingForTrigger;
    float flareStrength => Mathf.InverseLerp(0f, 100f, timer);
    float previousFrameFlareStrength => Mathf.InverseLerp(0f, 100f, timer - 1U);
    enum State
    {
        awaitingForTrigger,
        waitingForDelay,
        teleportationProcess
    }
    public override void Update(bool eu)
    {
        base.Update(eu);
        switch (state)
        {
            case State.awaitingForTrigger:
                {
                    if (room.game.AlivePlayers.Exists((AbstractCreature x) => Vector2.Distance(x.realizedCreature.mainBodyChunk.pos, position) <= radius))
                    {
                        state = State.waitingForDelay;
                    }
                    break;
                }
            case State.waitingForDelay:
                {
                    delayTimer++;
                    if (delayTimer > TicksPerSecond * delay)
                    {
                        state = State.teleportationProcess;
                        room.PlaySound(SoundID.Void_Sea_Swim_Into_Core, 0f, 1f, 1f);
                    }
                    break;
                }
            case State.teleportationProcess:
                {
                    timer++;
                    if (timer == 160)
                    {
                        AbstractRoom teleportDestinationRoom = GetOutOfDreamSequenceRoom(room.world.game.StoryCharacter);
                        RWCustom.IntVector2 middleOfRoom = new(teleportDestinationRoom.realizedRoom.TileWidth / 2, teleportDestinationRoom.realizedRoom.TileHeight / 2);
                        WorldCoordinate destination = RWCustom.Custom.MakeWorldCoordinate(middleOfRoom, teleportDestinationRoom.index);
                        room.game.AlivePlayers.ForEach(player =>
                        {
                            player.pos = destination;
                        });
                    }
                    break;
                }
        }
    }
    #region graphical stuff
    public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        sLeaser.sprites = new FSprite[1];
        sLeaser.sprites[0] = new FSprite("Futile_White", true);
        sLeaser.sprites[0].alpha = 0f;
        sLeaser.sprites[0].shader = rCam.game.rainWorld.Shaders["FlatLightNoisy"];
        AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("Foreground"));
    }

    public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
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
    #region methods

    AbstractRoom GetOutOfDreamSequenceRoom(SlugcatStats.Name name)
    {
        return room.world.GetAbstractRoom((name == SlugcatStats.Name.Red) ? "RED_END" : "END");
    }
    #endregion

}
