using Newtonsoft.Json.Linq;
using ROM.RoomObjectService;
using ROM.UserInteraction.InroomManagement;
using ROM.UserInteraction.ObjectEditorElement.Scrollbar;
using ROM.UserInteraction.ObjectEditorElement;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static PVStuffMod.StaticStuff;
using System;
namespace PVStuffMod.Logic.ROM_objects;

public class Teleporter : UpdatableAndDeletable
{
    //ROM fields
    public Vector2[] Polygon { get; set; } =
        [
            new Vector2(500, 500),
            new Vector2(500, 600),
            new Vector2(600, 600),
            new Vector2(600, 500)
        ];
    public float delay;
    public bool isEnabled = false;
    public Function function;
    public enum Function
    {
        startDream,
        endDream
    }


    uint delayTimer;
    State state = State.awaitingForTrigger;
    enum State
    {
        awaitingForTrigger,
        waitingForDelay,
        teleportationProcess
    }
    
    #region methods
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
                        flasher.RequestScreenFlash(GetHashCode(), Color.white, ticksToFadeOut: 80, ticksToFadeIn: 40);
                    }
                    break;
                }
        }
    }

    public void Teleportation(int hash)
    {
        if (hash != this.GetHashCode()) return;
        Destination destination = GetDestination(room.game.StoryCharacter);
        room.game.AlivePlayers.TeleportCreaturesIntoRoom(room.world, room.game, destination);
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

    bool AnyPlayersWithinArea => room.game.AlivePlayers.Exists((AbstractCreature x) => ROMUtils.PositionWithinPoly(Polygon, x.realizedCreature.mainBodyChunk.pos));
    Destination GetDestination(SlugcatStats.Name name)
    {
        return function switch
        {
            Function.startDream => PVMaps.GetDreamDestination(name),
            _ => PVMaps.GetEndDestination(name)
        };
    }
    #endregion

}

public class DreamEnderOperator : TypeOperator<Teleporter>
{
    private static VersionedLoader<Teleporter> VersionedLoader { get; } =
            TypeOperatorUtils.CreateVersionedLoader<Teleporter>(defaultLoad: TypeOperatorUtils.TrivialLoad<Teleporter>);

    public override string TypeId => nameof(Teleporter);

    public override Teleporter CreateNew(Room room, Rect currentScreenRect)
    {
        Vector2 center = currentScreenRect.center;

        float screenWidth = currentScreenRect.width;
        float screenHeight = currentScreenRect.height;
        Vector2[] polygon = [
                center + new Vector2(screenWidth/8, screenHeight/8),
                center + new Vector2(screenWidth/8, - screenHeight/8),
                center + new Vector2(-screenWidth/8, -screenHeight/8),
                center + new Vector2(-screenWidth/8, screenHeight/8)
            ];
        return new()
        {
            room = room,
            Polygon = polygon
        };
    }
    public override Teleporter Load(JToken dataJson, Room room)
    {
        return VersionedLoader.Load(dataJson, room);
    }

    public override JToken Save(Teleporter obj)
    {
        return TypeOperatorUtils.GetTrivialVersionedSaveCall<Teleporter>("0.0.0").Invoke(obj);
    }

    public override void AddToRoom(Teleporter obj, Room room)
    {
        room.AddObject(obj);
    }

    public override void RemoveFromRoom(Teleporter obj, Room room)
    {
        room.RemoveObject(obj);
    }

    public override IEnumerable<IObjectEditorElement> GetEditorElements(Teleporter obj, Room room)
    {
        yield return Elements.Polygon("Trigger Zone", obj.Polygon);
        yield return Elements.Checkbox("Enabled", () => obj.isEnabled, value => obj.isEnabled = value);
        yield return Elements.CollapsableOptionSelect("Type of object", () => obj.function, value => obj.function = value);
        yield return Elements.TextField("Lingering", getter: () => obj.delay, setter: x => obj.delay = x);
    }
}
