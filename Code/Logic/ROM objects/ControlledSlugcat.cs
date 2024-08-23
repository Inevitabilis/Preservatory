using static RWCustom.Custom;
using PVStuff.Logic.ControllerParser;
using UnityEngine;
using Newtonsoft.Json.Linq;
using PVStuffMod.Logic.ROM_objects;
using ROM.RoomObjectService;
using ROM.UserInteraction.InroomManagement;
using ROM.UserInteraction.ObjectEditorElement;
using System.Collections.Generic;


namespace PVStuff.Logic.ROM_objects;

public class ControlledSlugcat : UpdatableAndDeletable
{
    //exposed fields
    public Vector2 startPosition;
    public string controllerID = "test";
    public bool isEnabled;


    AbstractCreature? puppet;
    Player puppetPlayer => puppet.realizedCreature as Player;
    WorldCoordinate blockPosition => room.ToWorldCoordinate(startPosition);
    bool initdone;
    public override void Update(bool eu)
    {
        if(!(room.fullyLoaded && room.ReadyForPlayer && room.shortCutsReady && isEnabled)) return;
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

public class ControlledSlugcatOperator : TypeOperator<ControlledSlugcat>
{
    private static VersionedLoader<ControlledSlugcat> VersionedLoader { get; } =
            TypeOperatorUtils.CreateVersionedLoader(defaultLoad: TypeOperatorUtils.TrivialLoad<ControlledSlugcat>);
    public override string TypeId => nameof(ControlledSlugcat);

    public override void AddToRoom(ControlledSlugcat obj, Room room)
    {
        room.AddObject(obj);
    }

    public override ControlledSlugcat CreateNew(Room room, Rect currentCameraRect)
    {
        return new() { room = room };
    }

    public override IEnumerable<IObjectEditorElement> GetEditorElements(ControlledSlugcat obj, Room room)
    {
        yield return Elements.Point("Spawn position", "s", () => obj.startPosition, x => obj.startPosition = x);
        yield return Elements.TextField("script name", () => obj.controllerID, x => obj.controllerID = x);
        yield return Elements.Checkbox("Enabled?", () => obj.isEnabled, x => obj.isEnabled = x);
        
    }

    public override ControlledSlugcat Load(JToken dataJson, Room room)
    {
        return VersionedLoader.Load(dataJson, room);
    }

    public override void RemoveFromRoom(ControlledSlugcat obj, Room room)
    {
        room.RemoveObject(obj);
    }

    public override JToken Save(ControlledSlugcat obj)
    {
        return TypeOperatorUtils.GetTrivialVersionedSaveCall<ControlledSlugcat>("0.0.0").Invoke(obj);
    }
}
