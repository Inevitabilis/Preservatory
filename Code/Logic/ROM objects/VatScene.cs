using Newtonsoft.Json.Linq;
using PVStuffMod;
using ROM.RoomObjectService;
using ROM.UserInteraction.InroomManagement;
using ROM.UserInteraction.ObjectEditorElement;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace PVStuffMod.Logic.ROM_objects;

public class VatScene : UpdatableAndDeletable
{
    public bool isEnabled;
    public float delayBeforeFlash;
    public enum State
    {
        preControlTaken,
        timedIdle,
        flashCommencing,
        disposable
    }

    private uint timer;
    private State state;

    public override void Update(bool eu)
    {
        if (!isEnabled || !room.updateList.Exists(UAD => UAD is Player)) return;
        base.Update(eu);
        room.game.Players.ForEach(absply =>
        {
            if (absply.realizedCreature is Player player) { player.airInLungs = 1f; }
        });
        switch(state)
        {
            case State.preControlTaken:
                {
                    room.game.Players.ForEach(absply =>
                    {
                        if (absply.realizedCreature is Player player) player.controller = null;
                    });
                    state = State.timedIdle;
                    break;
                }
            case State.timedIdle:
                {
                    timer++;
                    if(timer == delayBeforeFlash * StaticStuff.TicksPerSecond)
                    {
                        state = State.flashCommencing;
                        ScreenFlasher flasher = StaticStuff.RegisterScreenFlasher(room.game.cameras[0]);
                        flasher.TickOnCompletion += Ending;
                        flasher.RequestScreenFlash(this.GetHashCode(), Color.black);
                    }
                    break;
                }
            default:
                {
                    break;
                }
        }
        
    }

    private void Ending(int obj)
    {
        if (this.GetHashCode() != obj) return;
        state = State.disposable;
        slatedForDeletetion = true;
        NonPlayerCenteredLogic.BeatGameModeStasis(room.game);
    }
}

public class VatSceneOperator : TypeOperator<VatScene>
{
    private static VersionedLoader<VatScene> VersionedLoader { get; } =
            TypeOperatorUtils.CreateVersionedLoader(defaultLoad: TypeOperatorUtils.TrivialLoad<VatScene>);
    public override string TypeId => nameof(VatScene);

    public override void AddToRoom(VatScene obj, Room room)
    {
        room.AddObject(obj);
    }

    public override VatScene CreateNew(Room room, Rect currentCameraRect)
    {
        return new(){ room = room };
    }

    public override IEnumerable<IObjectEditorElement> GetEditorElements(VatScene obj, Room room)
    {
        yield return Elements.Checkbox("Enabled?", () => obj.isEnabled, x => obj.isEnabled = x);
        yield return Elements.TextField("Pre flash start delay", getter: () => obj.delayBeforeFlash, setter: x => obj.delayBeforeFlash = x);
    }

    public override VatScene Load(JToken dataJson, Room room)
    {
        return VersionedLoader.Load(dataJson, room);
    }

    public override void RemoveFromRoom(VatScene obj, Room room)
    {
        room.RemoveObject(obj);
    }

    public override JToken Save(VatScene obj)
    {
        return TypeOperatorUtils.GetTrivialVersionedSaveCall<VatScene>("0.0.0").Invoke(obj);
    }
}