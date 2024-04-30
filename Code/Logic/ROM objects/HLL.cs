using MoreSlugcats;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PVStuffMod;
using PVStuffMod.Logic.ROM_objects;
using ROM.RoomObjectService;
using ROM.UserInteraction;
using ROM.UserInteraction.InroomManagement;
using ROM.UserInteraction.ObjectEditorElement;
using ROM.UserInteraction.ObjectEditorElement.Scrollbar;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PVStuff.Logic.ROM_objects;

public class HLL : UpdatableAndDeletable
{
    public UnityEngine.Vector2 position;
    public Vector2[] polygon = [
            new Vector2(500, 500),
            new Vector2(500, 600),
            new Vector2(600, 600),
            new Vector2(600, 500)
        ];
    public float speed;
    public float amplitude;

    [JsonIgnore]
    AbstractCreature? abstractDaddy;
    [JsonIgnore]
    DaddyLongLegs? daddy;
    [JsonIgnore]
    uint counter;
    [JsonIgnore]
    const float timeCoefficient = 40;
    float Perlin(float x) => (Mathf.Sin(2f*x*speed/timeCoefficient) + Mathf.Sin(Mathf.PI*x*speed/timeCoefficient))/2f;
    public override void Update(bool eu)
    {
        ErrorHandling();
        if (daddy == null || room == null || polygon == null) throw new Exception("something's wrong");
        counter++;
        daddy.mainBodyChunk.pos = position;
        daddy.Stun(100);
        daddy.g = 0;
        DaddyTentacle[] tnt = daddy.tentacles;
        Array.ForEach(tnt, tnt => tnt.limp = false);
        for(int i = 0; i < tnt.Length; i++) 
        {
            var chunk = tnt[i].Tip;
            chunk.pos = (polygon[i%polygon.Length]) + new Vector2(Perlin(counter+i*20f), Perlin(-counter+i*20f)) * amplitude;
        }
        
    }
    public void Initiate()
    {
        room.abstractRoom.AddEntity(abstractDaddy = new AbstractCreature(room.world, 
            StaticWorld.GetCreatureTemplate(MoreSlugcatsEnums.CreatureTemplateType.HunterDaddy), 
            null, 
            RWCustom.Custom.MakeWorldCoordinate(room.GetTilePosition(position), room.abstractRoom.index), 
            room.world.game.GetNewID()));
        daddy = new DaddyLongLegs(abstractDaddy, room.world);
        abstractDaddy.realizedCreature = daddy;
        daddy.PlaceInRoom(room);
        daddy.Stun(1000);
        new DaddyAI(abstractDaddy, room.world);
    }
    public void PrepareForDestruction()
    {
        room.RemoveObject(daddy);
        slatedForDeletetion = true;
    }
    void ErrorHandling()
    {
        if (room == null) throw new ArgumentException("room is null");
        if (daddy == null) throw new Exception("daddy is null");
        if (polygon == null) throw new Exception("No Polygon for tentacles defined");
    }
}

public class HLLOperator : TypeOperator<HLL>
{
    private static VersionedLoader<HLL> VersionedLoader { get; } =
                TypeOperatorUtils.CreateVersionedLoader<HLL>(defaultLoad: TypeOperatorUtils.TrivialLoad<HLL>);
    public override string TypeId => nameof(HLL);

    public override void AddToRoom(HLL obj, Room room)
    {
        room.AddObject(obj);
        obj.Initiate();
    }

    public override HLL CreateNew(Room rm, Rect currentCameraRect)
    {
        Vector2 center = currentCameraRect.center;
        return new()
        {
            room = rm,
            position = center
        };
    }

    public override IEnumerable<IObjectEditorElement> GetEditorElements(HLL obj, Room room)
    {
        yield return Elements.Point("center", "p", () => obj.position, val => obj.position = val);
        yield return Elements.Polygon("tentacles", obj.polygon);
        var a = new ScrollbarConfiguration<float>(0f, 5f, x => x, x => x, x => x.ToString("0.##", CultureInfo.InvariantCulture));
        yield return Elements.Scrollbar("Tentacle endpoint deviation amplitude", getter: () => obj.amplitude/20f, setter: value => obj.amplitude = value*20f, a);
        var b = new ScrollbarConfiguration<float>(0f, 2f, x => x, x => x, x => x.ToString("0.##", CultureInfo.InvariantCulture));
        yield return Elements.Scrollbar("Tentacle move speed coefficient", () => obj.speed, val => obj.speed = val, b);

    }

    public override HLL Load(JToken dataJson, Room room)
    {
        return VersionedLoader.Load(dataJson, room);
    }

    public override void RemoveFromRoom(HLL obj, Room room)
    {
        obj.PrepareForDestruction();
        room.RemoveObject(obj);
    }

    public override JToken Save(HLL obj)
    {
        return TypeOperatorUtils.GetTrivialVersionedSaveCall<HLL>("0.0.0").Invoke(obj);
    }
}