using PVStuff.Logic.ControllerParser;
using static Pom.Pom;
using UnityEngine;
using Newtonsoft.Json;
using MoreSlugcats;


namespace PVStuff.Logic.POM_objects;

public class ControlledSlugcat : UpdatableAndDeletable
{
    public static void Register()
    {
        RegisterFullyManagedObjectType(fields, typeof(ControlledSlugcat), "Controlled slugcat");
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public ControlledSlugcat(Room room, PlacedObject pObj)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    {
        this.room = room;
#pragma warning disable CS8601 // Possible null reference assignment.
        this.data = pObj.data as ManagedData;
#pragma warning restore CS8601 // Possible null reference assignment.
    }

    internal static ManagedField[] fields = [
            new StringField("controllerID", "test", "controller ID"),
            new BooleanField("adult", true, displayName: "adult?"),
            new ColorField("color", new(1f,1f,1f), displayName: "color"),
            new EnumField<WhoAmI>("characterSpecificSpawn", WhoAmI.anyone, displayName: "who am i?"),
            new BooleanField("enabled", false, displayName: "enabled?"),
        ];


    //exposed fields
    ManagedData data;

    public Vector2 startPosition => data.owner.pos;
#pragma warning disable CS8603 // Possible null reference return.
    public string controllerID => data.GetValue<string>("controllerID");
#pragma warning restore CS8603 // Possible null reference return.
    public bool adult => data.GetValue<bool>("adult");
    [JsonIgnore]
    public Color color => data.GetValue<Color>("color");
    public WhoAmI whoAmI => data.GetValue<WhoAmI>("characterSpecificSpawn");
    public bool isEnabled => data.GetValue<bool>("enabled");

    public SerializableColor serializableColor = new(1f, 1f, 1f);


    public class SerializableColor
    {
        public float r, g, b;
        public SerializableColor(float r, float g, float b)
        {
            this.r = r;
            this.g = g;
            this.b = b;
        }
        public SerializableColor(Color c) : this(c.r, c.g, c.b)
        { }
        public Color ToColor()
        {
            return new(r, g, b);
        }
    }
    public enum WhoAmI
    {
        anyone,
        survivor,
        monk,
        gourmand
    }
    #region runtime variables
    [JsonIgnore]
    AbstractCreature? puppet;
    [JsonIgnore]
    Player puppetPlayer
    {
        get
        {
            if (puppet?.realizedCreature is Player p) return p;
            else if (puppet is null) throw new System.Exception($"[PRESERVATORY]: {nameof(ControlledSlugcat)}.{nameof(ControlledSlugcat.puppetPlayer)} - puppet was null");
            else throw new System.Exception($"[PRESERVATORY]: {nameof(ControlledSlugcat)}.{nameof(ControlledSlugcat.puppetPlayer)} - realized creature of puppet was not player");
        }
    }
    [JsonIgnore]
    WorldCoordinate blockPosition => room.ToWorldCoordinate(startPosition);
    [JsonIgnore]
    bool initdone;
    #endregion

    public override void Destroy()
    {
        if (puppetPlayer is Player p) p.Destroy();
        base.Destroy();
    }


    public override void Update(bool eu)
    {
        if (initdone && !room.roomSettings.placedObjects.Contains(data.owner)) Destroy();
        if (!(room.fullyLoaded && room.ReadyForPlayer && room.shortCutsReady && isEnabled)) return;
        if (IsValidToSpawnForCharacter() && !initdone) CreatureSetup();
    }

    private void CreatureSetup()
    {
        puppet = new AbstractCreature(room.world, StaticWorld.GetCreatureTemplate(MoreSlugcatsEnums.CreatureTemplateType.SlugNPC), null, blockPosition, room.game.GetNewID());
        puppet.state = new PlayerNPCState(puppet, 0)
        {
            forceFullGrown = adult
        };
        room.abstractRoom.AddEntity(puppet);
        puppet.RealizeInRoom();
        puppetPlayer.controller = new SlugController(controllerID);
        puppetPlayer.standing = true;
        var HSL = RWCustom.Custom.RGB2HSL(color);
        var stats = puppetPlayer.npcStats;
        stats.Dark = false;
        stats.H = HSL.x;
        stats.S = HSL.y;
        stats.L = HSL.z;
        if (puppetPlayer.graphicsModule is PlayerGraphics g)
        {
            //i haven't figured out how to paint slugNPCs properly in update, but there were two methods that handled it
            //both conditional. one required darkenFactor to be above zero so here we are
            g.darkenFactor = 0.01f;
        }
        initdone = true;
    }
    bool IsValidToSpawnForCharacter()
    {
        var campaignCharacter = room.game.GetStorySession.characterStats.name;
        if (whoAmI == WhoAmI.anyone) return true;
        if (whoAmI == WhoAmI.gourmand) return campaignCharacter == SlugcatStats.Name.White || campaignCharacter == SlugcatStats.Name.Yellow;
        if (whoAmI == WhoAmI.monk) return campaignCharacter == SlugcatStats.Name.White || campaignCharacter == MoreSlugcatsEnums.SlugcatStatsName.Gourmand;
        if (whoAmI == WhoAmI.survivor) return campaignCharacter == SlugcatStats.Name.Yellow || campaignCharacter == MoreSlugcatsEnums.SlugcatStatsName.Gourmand;
        return true;
    }
}
