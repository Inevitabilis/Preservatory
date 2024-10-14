
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static PVStuffMod.StaticStuff;
using System;
using static Pom.Pom;
namespace PVStuffMod.Logic.POM_objects;

public class Teleporter : UpdatableAndDeletable
{
    public static void RegisterObject()
    {
        RegisterFullyManagedObjectType(managedFields, typeof(Teleporter), "PVTeleporter", StaticStuff.PreservatoryPOMCategory);
    }

    //ROM fields
    internal static ManagedField[] managedFields = [
        new FloatField("delay", float.NegativeInfinity, float.PositiveInfinity, 0f, control: ManagedFieldWithPanel.ControlType.text),
        new BooleanField("enabled", false),
        new EnumField<Function>("function", Function.startDream),
        POMUtils.defaultVectorField
    ];

    public Vector2[]? Polygon => POMUtils.AddRealPosition(data.GetValue<Vector2[]>("trigger zone"), pObj.pos);
    public float delay => data.GetValue<float>("delay");
    public bool isEnabled => data.GetValue<bool>("enabled");
    public Function function => data.GetValue<Function>("function");
    public enum Function
    {
        startDream,
        endDream
    }

    ManagedData data;
    PlacedObject pObj;

    uint delayTimer;
    State state = State.awaitingForTrigger;
    enum State
    {
        awaitingForTrigger,
        waitingForDelay,
        teleportationProcess
    }

    #region methods
    public Teleporter(Room room, PlacedObject pObj)
    {
        this.pObj = pObj;
        data = (pObj.data as ManagedData)!;
    }

    public override void Update(bool eu)
    {
        if (!isEnabled || !room.updateList.Exists(UAD => UAD is Player)) return;
        base.Update(eu);
        switch (state)
        {
            case State.awaitingForTrigger:
                {
                    if (AnyPlayersWithinArea)
                    {
                        state = State.waitingForDelay;
                    }
                    break;
                }
            case State.waitingForDelay:
                {
                    delayTimer++;
                    if (delayTimer >= (uint)(TicksPerSecond * delay))
                    {
                        state = State.teleportationProcess;
                        room.PlaySound(SoundID.Void_Sea_Swim_Into_Core, 0f, 1f, 1f);
                        var flasher = RegisterScreenFlasher(room.game.cameras[0]);
                        flasher.TickOnFill += (int hash) =>
                        {
                            if (hash != this.GetHashCode()) return;
                            flasher.RequestColorChange(Color.black);
                        };
                        flasher.TickInTheMiddleOfIdling += Teleportation;
                        flasher.RequestScreenFlash(GetHashCode(), room.game.cameras[0].virtualMicrophone, Color.white, ticksToFadeOut: 200, ticksToFadeIn: 100);
                    }
                    break;
                }
        }
    }

    public void Teleportation(int hash)
    {
        if (hash != this.GetHashCode()) return;
        Destination destination = GetDestination(room.game.StoryCharacter);
        TeleportCreaturesIntoRoom(room.game.AlivePlayers, room.world, room.game, destination);
        if(destination.roomName == "PV_DREAM_TREE03")
        {
            room.game.AlivePlayers.ForEach(x =>
            {
                if (x.realizedCreature is Player p)
                {
                    p.bodyChunks[1].pos.x = p.bodyChunks[0].pos.x + 3f;
                    p.bodyChunks[1].pos.y = p.bodyChunks[0].pos.y;
                    p.sleepCounter = 100;
                    p.sleepWhenStill = false;
                    p.bodyMode = Player.BodyModeIndex.Crawl;
                    p.animation = Player.AnimationIndex.DownOnFours;
                }
            });
        }
        Cleanup(room);

        void Cleanup(Room room)
        {
            var realizer = room.game.roomRealizer;
            realizer.KillRoom(room.abstractRoom);
            if(realizer.realizedRooms.Exists(x => x.room == room.abstractRoom))
            {
                var tracker = realizer.realizedRooms.First(x => x.room == room.abstractRoom);
                realizer.realizedRooms.Remove(tracker);
            }
        }

    }

    bool AnyPlayersWithinArea => room.game.AlivePlayers.Exists((AbstractCreature x) => ROMUtils.PositionWithinPoly(Polygon, x.realizedCreature.mainBodyChunk.pos) && room.abstractRoom == x.Room);
    Destination GetDestination(SlugcatStats.Name name)
    {
        return function switch
        {
            Function.startDream => PVMaps.GetDreamDestination(name),
            _ => PVMaps.GetEndDestination(name)
        };
    }




    public static void TeleportCreaturesIntoRoom(List<AbstractCreature> abstractCreatures, World world, RainWorldGame game, Destination d)
    {
        AbstractRoom room = world.GetAbstractRoom(d.roomName);
        room.RealizeRoom(world, game);
        while (world.loadingRooms.Count > 0)
        {

            for (int j = world.loadingRooms.Count - 1; j >= 0; j--)
            {
                if (world.loadingRooms[j].done)
                {
                    world.loadingRooms.RemoveAt(j);
                }
                else
                {
                    world.loadingRooms[j].Update();
                }
            }

        }
        RWCustom.IntVector2 middleOfRoom = new(room.realizedRoom.TileWidth / 2 + 10, room.realizedRoom.TileHeight / 2);
        WorldCoordinate destination = RWCustom.Custom.MakeWorldCoordinate(room.realizedRoom.GetTilePosition(d.position), room.index);
        abstractCreatures.ForEach(creature =>
        {
            if(creature.realizedCreature is Player p)
            {
                p.slugOnBack?.DropSlug();
                p.spearOnBack?.DropSpear();
                foreach (Creature.Grasp? grasp in p.grasps)
                {
                    grasp?.Release();
                }
            }
            creature.pos = destination;

        });
        abstractCreatures.ForEach(absPlayer =>
        {
            absPlayer.RealizeInRoom();
            if(absPlayer.realizedCreature is Player player)
            {
                player.SuperHardSetPosition(d.position);
                Array.ForEach(player.bodyChunks, chunk => chunk.vel = Vector2.zero);
                player.graphicsModule?.Reset();
                player.standing = true;
            }
        });
        room.world.game.roomRealizer.followCreature = abstractCreatures[0];
        game.cameras[0].MoveCamera(room.realizedRoom, 0);
    }
    #endregion

}
