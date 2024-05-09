using Microsoft.SqlServer.Server;
using MoreSlugcats;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PVStuffMod;
using PVStuffMod.Logic.ROM_objects;
using ROM.RoomObjectService;
using ROM.UserInteraction.InroomManagement;
using ROM.UserInteraction.ObjectEditorElement;
using ROM.UserInteraction.ObjectEditorElement.TextField;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PVStuff.Logic.ROM_objects;

public class PVSlugNPC : UpdatableAndDeletable
{
    public Vector2 point = new Vector2(500, 600);
    public bool fullgrown = true;
    public bool forcepos = false;
    public SpecialNPC whoami;
    public Color color;
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
        if(!ShouldSpawnForThisSlugcat() || !room.game.Players.Exists(absPly => absPly.Room == room.abstractRoom)) return;
        ErrorHandling();
        if (abstractSlug == null || abstractSlug.realizedCreature == null) throw new Exception("something's wrong"); //to wade off warnings
        if (abstractSlug.abstractAI is SlugNPCAbstractAI absAI) absAI.toldToStay = new WorldCoordinate?(room.ToWorldCoordinate(point));
        if (forcepos) abstractSlug.realizedCreature.mainBodyChunk.pos = point;
        if (StaticStuff.devBuild && Input.GetKey(KeyCode.KeypadEnter)) ResetSlug();
        if (StaticStuff.devBuild) UpdateColor();
    }
    void UpdateColor()
    {
        var sleaser = room.game.cameras[0].spriteLeasers.FirstOrDefault(x => x.drawableObject == abstractSlug.realizedCreature);
        for (short i = 0; i < sleaser.sprites.Length; i++)
        {
            if (StaticStuff.playerColorableSpritesIndices.Contains(i)) sleaser.sprites[i].color = color;
        }
    }
    public void Initiate()
    {
        if (!ShouldSpawnForThisSlugcat()) return;
        abstractSlug = new AbstractCreature(room.world,
            StaticWorld.GetCreatureTemplate(MoreSlugcats.MoreSlugcatsEnums.CreatureTemplateType.SlugNPC), null, room.ToWorldCoordinate(point), room.game.GetNewID());
        if (abstractSlug.state is PlayerState state) state.forceFullGrown = fullgrown;
        room.abstractRoom.AddEntity(abstractSlug);
        abstractSlug.RealizeInRoom();
        UpdateColor();
    }

    private bool ShouldSpawnForThisSlugcat()
    {
        if(whoami == SpecialNPC.none) return true;
        if(room.game.session is StoryGameSession story)
        {
            return (story.characterStats.name == SlugcatStats.Name.White && (whoami == SpecialNPC.gourmand || whoami == SpecialNPC.monk))
                || (story.characterStats.name == SlugcatStats.Name.Yellow && (whoami == SpecialNPC.survivor || whoami == SpecialNPC.gourmand))
                || (story.characterStats.name == MoreSlugcatsEnums.SlugcatStatsName.Gourmand && (whoami == SpecialNPC.monk || whoami == SpecialNPC.survivor));
        }
        return true;
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
    public override void Destroy()
    {
        if (abstractSlug != null)
        {
            room.updateList.Remove(abstractSlug.realizedCreature);
            room.abstractRoom.RemoveEntity(abstractSlug);
        }
        base.Destroy();
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
        yield return Elements.Checkbox("Force position?", () => obj.forcepos, x => obj.forcepos = x);
        yield return Elements.CollapsableOptionSelect("who am i?", () => obj.whoami, x => obj.whoami = x);
        TextFieldConfiguration<Color> configuration = new TextFieldConfiguration<Color>(
            formatter: Format, parser: TryParse, (_) => true, null);
        yield return Elements.TextField(displayName: "Color",
            getter: () => obj.color,
            setter: value => obj.color = value,
            configuration: configuration);
    }

    public override PVSlugNPC Load(JToken dataJson, Room room)
    {
        return VersionedLoader.Load(dataJson, room);
    }

    public override void RemoveFromRoom(PVSlugNPC obj, Room room)
    {
        obj.Destroy();
        room.RemoveObject(obj);
    }

    public override JToken Save(PVSlugNPC obj)
    {
        return TypeOperatorUtils.GetTrivialVersionedSaveCall<PVSlugNPC>("0.0.0").Invoke(obj);
    }


    private static string Format(Color color)
    {
        return "#" + ColorHexFromFloat(color.r) + ColorHexFromFloat(color.g) + ColorHexFromFloat(color.b);
    }
    private static string ColorHexFromFloat(float x) => ((int)(x * 255)).ToString("X").PadLeft(2, '0');
    private static float ColorFloatFromHex(int x) => ((float)x) / 255;

    private static bool TryParse(string input, out Color color)
    {
        color = Color.cyan;
        if (input.Length != 7 || input[0] != '#') return false;
        string hexString = input.Substring(1, 6);
        if (!int.TryParse(hexString, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int hexnum)) return false;
        color.b = ((float)(hexnum & 255)) / 255f;
        color.g = ((float)(hexnum & (255 << 8))) / 255f;
        color.r = ((float)(hexnum & (255 << 16))) / 255f;
        return true;
    }
}
