using EffExt;
using UnityEngine;

namespace PVStuffMod.Logic.POM_objects;

public class VatScene : UpdatableAndDeletable
{
    public static void RegisterEffect()
    {
        new EffExt.EffectDefinitionBuilder("PVVatScene")
            .AddField(new EffExt.EBoolField("enabled", false))
            .SetUADFactory((Room room, EffExt.EffectExtraData data, bool firstTimeRealized) => { return new VatScene(data); })
            .Register();
    }

    public bool isEnabled => data.GetBool("enabled");
    public float delayBeforeFlash => data.Amount * 20f;
    public enum State
    {
        preControlTaken,
        timedIdle,
        flashCommencing,
        disposable
    }

    private uint timer;
    private State state = State.preControlTaken;

    EffExt.EffectExtraData data;

    public VatScene(EffectExtraData data)
    {
        this.data = data;
    }

    public override void Update(bool eu)
    {
        room.game.Players.ForEach(absply =>
        {
            if (absply.realizedCreature is Player player) player.airInLungs = 1f;
        });
        if (!isEnabled || !room.updateList.Exists(UAD => UAD is Player)) return;
        base.Update(eu);
        room.game.Players.ForEach(absply =>
        {
            if (absply.realizedCreature is Player player) player.stun = 100;
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
                    if(timer == delayBeforeFlash * StaticStuff.TicksPerSecond)
                    {
                        state = State.flashCommencing;
                        ScreenFlasher flasher = StaticStuff.RegisterScreenFlasher(room.game.cameras[0]);
                        flasher.TickInTheMiddleOfIdling += Ending;
                        flasher.RequestScreenFlash(this.GetHashCode(), room.game.cameras[0].virtualMicrophone, Color.black);
                    }
                    timer++;
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