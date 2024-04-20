using PVStuffMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PVStuff.Logic.ROM_objects;
/// <summary>
/// RedInducedIllness is the class responsible for making red experience vanilla fainting out whenever any of red player enters the trigger area
/// </summary>
internal class RedInducedIllness : UpdatableAndDeletable
{
    public float delay;
    public Vector2[] Polygon { get; set; } =
        [
            new Vector2(500, 500),
            new Vector2(500, 600),
            new Vector2(600, 600),
            new Vector2(600, 500)
        ];

    enum State
    {
        armed,
        tickingBeforeFiring,
        working,
        disarmed
    }
    uint timer;
    State state = State.armed;
    RedsIllness.RedsIllnessEffect? effect = null;

    public override void Update(bool eu)
    {
        if (!room.updateList.Exists(x => x is Player)) return;
        base.Update(eu);
        switch(state)
        {
            case State.armed:
                {
                    if (room.world.game.Players.Exists(x =>
                    {
                        return (x.realizedCreature is Player p)
                        && (p.slugcatStats.name == SlugcatStats.Name.Red)
                        && ROMUtils.PositionWithinPoly(Polygon, x.realizedCreature.mainBodyChunk.pos);
                    }))
                    {
                        state = State.tickingBeforeFiring;
                    }
                        break;
                }
            case State.tickingBeforeFiring:
                {
                    timer++;
                    if(timer >= delay * StaticStuff.TicksPerSecond)
                    {
                        var redCat = room.world.game.Players.First(x => (x.realizedCreature is Player p) && (p.slugcatStats.name == SlugcatStats.Name.Red)).realizedCreature as Player;
                        if (redCat is null) return;
                        state = State.working;
                        redCat.redsIllness ??= new(redCat, 0);
                        redCat.redsIllness.effect = new(redCat.redsIllness, room);
                        effect = redCat.redsIllness.effect;
                        room.updateList.Add(redCat.redsIllness.effect);
                    }
                    break;
                }
            case State.working:
                {
                    if (effect == null || effect.slatedForDeletetion)
                    {
                        effect = null; //yes i know it looks weird, but if i was to make it logically correct, it would be a bit more complicated
                        state = State.disarmed;
                        slatedForDeletetion = true;
                    }
                        break;
                }
            default:
                {
                    break;
                }
        }

    }
}
