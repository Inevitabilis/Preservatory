using MoreSlugcats;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PVStuffMod;
using ROM.RoomObjectService;
using ROM.UserInteraction.InroomManagement;
using ROM.UserInteraction.ObjectEditorElement;
using ROM.UserInteraction.ObjectEditorElement.TextField;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace PVStuff.Logic.ROM_objects;

public class PVSlugNPC : UpdatableAndDeletable
{
    #region exposed variables
    public Vector2 point = new(500, 600);
    public Vector2[] path = [
            new Vector2(500, 500),
            new Vector2(500, 600),
            new Vector2(600, 600),
            new Vector2(600, 500)
        ];
    public Vector2[] FollowPlayerActiveZone = [
            new Vector2(500, 500),
            new Vector2(500, 600),
            new Vector2(600, 600),
            new Vector2(600, 500)
        ];
    public bool fullgrown = true;
    public bool forcepos = false;
    public SpecialNPC whoami;
    public Behaviour behaviour;
    public float playerRelationship;
    [JsonIgnore]
    public Color color = new(1f, 1f, 1f);

    internal SerializableColor Serializablecolor
    {
        get
        {
            return new SerializableColor(color);
        }
        set
        {
            color = new Color(value.r, value.g, value.b, value.a);
        }
    }
    internal struct SerializableColor
    {
        public float r;
        public float g;
        public float b;
        public float a;
        public SerializableColor(Color color)
        {
            r = color.r;
            g = color.g;
            b = color.b;
            a = color.a;
        }
    }
    #endregion
    #region staticStuff
    public enum SpecialNPC
    {
        none,
        monk,
        survivor,
        gourmand,
        artiBrothers
    }
    public enum Behaviour
    {
        sleeping,
        standing,
        cycling,
        followPlayer
    }
    public static HashSet<AbstractCreature> sleepingSlugcats = new HashSet<AbstractCreature>();
    public static void ApplyHooks()
    {

    }
    private const float playerApproachDisengageRange = 20f;
    #endregion
    #region runtime required references
    /// <summary>
    /// those are references that don't need to be saved
    /// </summary>
    [JsonIgnore]
    private short currentPathingIndex = 0;
    [JsonIgnore]
    AbstractCreature? abstractSlug;
    [JsonIgnore]
    Player? followingPlayer;
    [JsonIgnore]
    Behaviour lastBehaviour;
    #endregion
    public override void Update(bool eu)
    {
        if (!ShouldSpawnForThisSlugcat() || !room.game.Players.Exists(absPly => absPly.realizedCreature.room == room)) return;
        if(abstractSlug == null) Initiate();
        if (StaticStuff.devBuild && Input.GetKey(KeyCode.V)) ResetSlug();
        ErrorHandling();
        if (abstractSlug == null || abstractSlug.realizedCreature == null) throw new Exception("something's wrong"); //to wade off warnings
        CognitiveFunctions();
        if (forcepos) abstractSlug.realizedCreature.mainBodyChunk.pos = point;
        if (StaticStuff.devBuild) UpdateColor();

    }
    #region major helping functions
    void CognitiveFunctions()
    {
        if (lastBehaviour != behaviour) OnBehaviourChange();
        lastBehaviour = behaviour;
        if (abstractSlug.abstractAI is SlugNPCAbstractAI absAI && absAI.RealAI is SlugNPCAI realAI && abstractSlug.realizedCreature is Player player)
        {
            switch (behaviour)
            {
                case Behaviour.sleeping:
                    {
                        player.standing = false;
                        player.sleepCounter = -1;
                        player.bodyMode = Player.BodyModeIndex.Crawl;
                        player.animation = Player.AnimationIndex.DownOnFours;
                        player.forceSleepCounter = 210;
                        player.wantToJump = 0;
                        //player.stun = 100;
                        break;
                    }
                case Behaviour.standing:
                    {
                        break;
                    }
                case Behaviour.cycling:
                    {
                        if (Vector2.Distance(abstractSlug.realizedCreature.mainBodyChunk.pos, point) < 10f) realAI.SetDestination(GetNewDestination());
                        break;
                    }
                case Behaviour.followPlayer:
                    {
                        FollowingLogic(absAI, realAI);
                        break;
                    }
            }
        }
    }
    private void FollowingLogic(SlugNPCAbstractAI absAI, SlugNPCAI realAI)
    {
        absAI.toldToStay = null;
        if (followingPlayer == null)
        {
            var playersWithinZone = room.game.AlivePlayers.Select(x => x.realizedCreature).Where(x => ROMUtils.PositionWithinPoly(FollowPlayerActiveZone, x.mainBodyChunk.pos));
            if (playersWithinZone.Any())
            {
                followingPlayer = playersWithinZone.First() as Player;
                realAI.friendTracker.friend = followingPlayer;
                realAI.behaviorType = SlugNPCAI.BehaviorType.Following;
            }
        }
        else
        {
            if (!ROMUtils.PositionWithinPoly(FollowPlayerActiveZone, followingPlayer.mainBodyChunk.pos))
            {
                realAI.friendTracker.friend = null;
                realAI.behaviorType = SlugNPCAI.BehaviorType.Idle;
                followingPlayer = null;
            }
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
        if (whoami == SpecialNPC.artiBrothers) room.updateList.ForEach(x =>
        {
            if (x is PVSlugNPC npc
            && npc != this
            && npc.whoami == SpecialNPC.artiBrothers
            && npc.abstractSlug != null)
            {
                var relationship = abstractSlug.state.socialMemory.GetOrInitiateRelationship(npc.abstractSlug.ID);
                relationship.InfluenceLike(1f);
                relationship.InfluenceTempLike(1f);
            }
        });
    }
    public void ResetSlug()
    {
        if (abstractSlug != null)
        {
            abstractSlug.realizedCreature.slatedForDeletetion = true;
            abstractSlug.slatedForDeletion = true;
            abstractSlug = null;
        }
        Initiate();
    }
    #endregion
    #region minor helper functions
    void OnBehaviourChange()
    {
        if (abstractSlug.abstractAI.RealAI is SlugNPCAI realAI)
        {
            realAI.nap = false;
            realAI.behaviorType = PVEnums.NPCBehaviour.completelyStill;
            realAI.SetDestination(default);

            switch (behaviour)
            {
                case Behaviour.sleeping:
                    {
                        realAI.nap = true;
                        realAI.cat.bodyMode = Player.BodyModeIndex.Crawl;

                        break;
                    }
                case Behaviour.standing:
                    {
                        realAI.cat.bodyMode = Player.BodyModeIndex.Stand;
                        break;
                    }
                case Behaviour.followPlayer:
                    {
                        break;
                    }
                case Behaviour.cycling:
                    {
                        realAI.AddModule(new StandardPather(realAI, room.world, abstractSlug));
                        realAI.pathFinder.stepsPerFrame = 30;
                        break;
                    }
            }
        }
    }
    void UpdateColor()
    {
        //var stats = (abstractSlug.realizedCreature as Player).npcStats;
        //var hsl = RWCustom.Custom.RGB2HSL(color);
        OverrideColors(color);
    }

    private void OverrideColors(Color color)
    {
        var rcam = room.game.cameras.FirstOrDefault(x => x.spriteLeasers.Exists(x => x.drawableObject == abstractSlug.realizedCreature.graphicsModule));
        if (rcam == default) return;
        var sLeaser = rcam.spriteLeasers.FirstOrDefault(x => x.drawableObject == abstractSlug.realizedCreature.graphicsModule);
        if (sLeaser == default) return;
        for(ushort i = 0; i<sLeaser.sprites.Length; i++)
        {
            if(Array.Exists(StaticStuff.playerColorableSpritesIndices, x => x == i)) sLeaser.sprites[i].color = color;
        }
    }

    static void assignHSL(Player.NPCStats stats, Color color)
    {
        stats.H = color.r;
        stats.S = color.g;
        stats.L = color.b;
    }
    private bool ShouldSpawnForThisSlugcat()
    {
        if (whoami == SpecialNPC.none || whoami == SpecialNPC.artiBrothers) return true;
        if (room.game.session is StoryGameSession story)
        {
            return (story.characterStats.name == SlugcatStats.Name.White && (whoami == SpecialNPC.gourmand || whoami == SpecialNPC.monk))
                || (story.characterStats.name == SlugcatStats.Name.Yellow && (whoami == SpecialNPC.survivor || whoami == SpecialNPC.gourmand))
                || (story.characterStats.name == MoreSlugcatsEnums.SlugcatStatsName.Gourmand && (whoami == SpecialNPC.monk || whoami == SpecialNPC.survivor));
        }
        return true;
    }
    void ErrorHandling()
    {
        if (abstractSlug == null) throw new Exception("abstractslug is null");
        if (abstractSlug != null && abstractSlug.realizedCreature == null) throw new Exception("abstractslug didn't have realized creature");
    }
    private WorldCoordinate GetNewDestination()
    {
        WorldCoordinate destination = RWCustom.Custom.MakeWorldCoordinate(room.GetTilePosition(path[currentPathingIndex]), room.abstractRoom.index);
        currentPathingIndex = (short)((currentPathingIndex + 1) % path.Length);
        return destination;
    }
    public override void Destroy()
    {
        if (abstractSlug != null)
        {
            Array.ForEach(room.game.cameras, cam => RemoveObject(cam.spriteLeasers, abstractSlug.realizedCreature.graphicsModule));
            room.updateList.Remove(abstractSlug.realizedCreature);
            room.abstractRoom.RemoveEntity(abstractSlug);
        }
        base.Destroy();
    }
    static void RemoveObject(List<RoomCamera.SpriteLeaser> sleaser, IDrawable obj)
    {
        for (int i = 0; i < sleaser.Count; i++)
        {
            if (sleaser[i].drawableObject == obj) sleaser[i].CleanSpritesAndRemove();
        }
    }
    #endregion
}



public class PVSlugNPCOperator : TypeOperator<PVSlugNPC>
{
    private static VersionedLoader<PVSlugNPC> VersionedLoader { get; } =
            TypeOperatorUtils.CreateVersionedLoader(defaultLoad: TypeOperatorUtils.TrivialLoad<PVSlugNPC>);
    public override string TypeId => nameof(PVSlugNPC);

    public override void AddToRoom(PVSlugNPC obj, Room room)
    {
        try
        {

            room.AddObject(obj);
        }
        catch (Exception e)
        {   
            MainLogic.Log(e.ToString()); }

    }

    public override PVSlugNPC CreateNew(Room room, Rect currentCameraRect)
    {
        return new() { room = room, point = currentCameraRect.center };
    }

    public override IEnumerable<IObjectEditorElement> GetEditorElements(PVSlugNPC obj, Room room)
    {
        yield return Elements.CollapsableOptionSelect("BehaviourType", () => obj.behaviour, x => obj.behaviour = x);
        yield return Elements.Point("Position (for still behaviour)", "p", () => obj.point, x => obj.point = x);
        yield return Elements.Checkbox("Force position?", () => obj.forcepos, x => obj.forcepos = x);
        yield return Elements.Polygon("pathing (for dynamic behaviour)", obj.path);
        yield return Elements.Polygon("Area of frolicking and following (for following behaviour)", obj.FollowPlayerActiveZone);
        yield return Elements.Scrollbar("Player Relationship", () => obj.playerRelationship, x => obj.playerRelationship = x);
        yield return Elements.Checkbox("Fullgrown?", () => obj.fullgrown, x => obj.fullgrown = x);
        yield return Elements.Scrollbar("R", () => obj.color.r, x => obj.color.r = x);
        yield return Elements.Scrollbar("G", () => obj.color.g, x => obj.color.g = x);
        yield return Elements.Scrollbar("B", () => obj.color.b, x => obj.color.b = x);
        yield return Elements.CollapsableOptionSelect("who am i?", () => obj.whoami, x => obj.whoami = x);
        TextFieldConfiguration<Color> configuration = new TextFieldConfiguration<Color>(
            formatter: Format, parser: TryParse, (_) => true, null);
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
