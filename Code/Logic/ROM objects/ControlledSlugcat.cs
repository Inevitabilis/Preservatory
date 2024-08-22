using static RWCustom.Custom;
using PVStuff.Logic.ControllerParser;
using UnityEngine;


namespace PVStuff.Logic.ROM_objects;

internal class ControlledSlugcat : UpdatableAndDeletable
{
    //fields
    public bool enabled;
    public string controllerID = "test";
    public Vector2 startPosition;


    AbstractCreature puppet;
    Player puppetPlayer => puppet.realizedCreature as Player;
    WorldCoordinate blockPosition => room.ToWorldCoordinate(startPosition);
    bool initdone;
    public override void Update(bool eu)
    {
        if(!(room.fullyLoaded && room.ReadyForPlayer && room.shortCutsReady && enabled)) return;
        if(!initdone) CreatureSetup();
    }

    private void CreatureSetup()
    {
        puppet = new AbstractCreature(room.world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Slugcat), null, blockPosition, room.game.GetNewID());
        puppet.state = new PlayerState(puppet, 0, SlugcatStats.Name.White, true);
        room.abstractRoom.AddEntity(puppet);
        puppet.RealizeInRoom();
        puppetPlayer.controller = new SlugController(controllerID);
        initdone = true;
    }
}
