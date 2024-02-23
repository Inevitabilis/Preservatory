using System.Linq;
using UnityEngine;
using static PVStuffMod.StaticStuff;
namespace PVStuffMod.Logic.ROM_objects;

public class DreamEnder : UpdatableAndDeletable
{
    //ROM fields
    public Vector2 position;
    public float radius;
    public float delay;


    uint timer;
    uint delayTimer;
    State state = DreamEnder.State.awaitingForTrigger;
    float FlareStrength => Mathf.InverseLerp(0f, 100f, timer);
    float PreviousFrameFlareStrength => Mathf.InverseLerp(0f, 100f, timer - 1U);
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
                        room.game.AlivePlayers.ForEach(player => player.pos = destination);
                    }
                    break;
                }
        }
    }
    #region methods

    AbstractRoom GetOutOfDreamSequenceRoom(SlugcatStats.Name name)
    {
        return room.world.GetAbstractRoom((name == SlugcatStats.Name.Red) ? "RED_END" : "END");
    }
    #endregion

}
