using MoreSlugcats;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PVStuffMod;
using PVStuffMod.Logic.ROM_objects;
using ROM.RoomObjectService;
using ROM.UserInteraction.InroomManagement;
using ROM.UserInteraction.ObjectEditorElement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PVStuff.Logic.ROM_objects;

public class PVSlugNPC : UpdatableAndDeletable
{
    public Vector2 point = new Vector2(500,600);
    public bool fullgrown = true;
    public bool forcepos = false;
    public SpecialNPC whoami;
    public enum SpecialNPC
    {
        none,
        monk,
        survivor,
        gourmand
    }

    [JsonIgnore]
    AbstractCreature? abstractSlug;
    public override void Update(bool eu)
    {
        ErrorHandling();
        if (abstractSlug == null || abstractSlug.realizedCreature == null) throw new Exception("something's wrong"); //to wade off warnings
        if (abstractSlug.abstractAI is SlugNPCAbstractAI absAI) absAI.toldToStay = new WorldCoordinate?(room.ToWorldCoordinate(point)); 
        if (forcepos) abstractSlug.realizedCreature.mainBodyChunk.pos = point;
        if(StaticStuff.devBuild && Input.GetKey(KeyCode.KeypadEnter)) ResetSlug();
    }
    public void Initiate()
    {

        abstractSlug = new AbstractCreature(room.world,
            StaticWorld.GetCreatureTemplate(MoreSlugcats.MoreSlugcatsEnums.CreatureTemplateType.SlugNPC), null, room.ToWorldCoordinate(point), room.game.GetNewID());
        if (abstractSlug.state is PlayerState state) state.forceFullGrown = fullgrown;
        room.abstractRoom.AddEntity(abstractSlug);
        abstractSlug.RealizeInRoom();
    }
    public void ResetSlug()
    {
        if (abstractSlug != null)
        {
            room.updateList.Remove(abstractSlug.realizedCreature);
            room.abstractRoom.RemoveEntity(abstractSlug);
        }
        Initiate();
    }
    void ErrorHandling()
    {
        if (abstractSlug == null) throw new Exception("abstractslug is null");
        if (abstractSlug != null && abstractSlug.realizedCreature == null) throw new Exception("abstractslug didn't have realized creature");
    }
}



public class PVSlugNPCOperator : TypeOperator<PVSlugNPC>
{
    private static VersionedLoader<PVSlugNPC> VersionedLoader { get; } =
            TypeOperatorUtils.CreateVersionedLoader(defaultLoad: TypeOperatorUtils.TrivialLoad<PVSlugNPC>);
    public override string TypeId => nameof(PVSlugNPC);

    public override void AddToRoom(PVSlugNPC obj, Room room)
    {
        room.AddObject(obj);
        obj.Initiate();
    }

    public override PVSlugNPC CreateNew(Room room, Rect currentCameraRect)
    {
        return new() { room = room, point = currentCameraRect.center };
    }

    public override IEnumerable<IObjectEditorElement> GetEditorElements(PVSlugNPC obj, Room room)
    {
        yield return Elements.Point("Position", "p", () => obj.point, x => obj.point = x);
        yield return Elements.Checkbox("Fullgrown?", () => obj.fullgrown, x => obj.fullgrown = x);
        yield return Elements.Checkbox("Force position?", () => obj.forcepos, x=> obj.forcepos = x);
        yield return Elements.CollapsableOptionSelect<PVSlugNPC.SpecialNPC>("who am i?", () => obj.whoami, x => obj.whoami = x);
        yield return Elements.TextField("Color", () => );
    }

    public override PVSlugNPC Load(JToken dataJson, Room room)
    {
        return VersionedLoader.Load(dataJson, room);
    }

    public override void RemoveFromRoom(PVSlugNPC obj, Room room)
    {
        room.RemoveObject(obj);
    }

    public override JToken Save(PVSlugNPC obj)
    {
        return TypeOperatorUtils.GetTrivialVersionedSaveCall<PVSlugNPC>("0.0.0").Invoke(obj);
    }
}
