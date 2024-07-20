using MonoMod.RuntimeDetour;
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

public class PVSlugNPC : UpdatableAndDeletable, INotifyWhenRoomIsReady
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
    public static void ApplyHooks()
    {
        new Hook(typeof(Creature).GetProperty(nameof(Creature.Consious)).GetGetMethod(), CreatureConsiousHook);
    }

    public static bool CreatureConsiousHook(Func<Creature, bool> orig, Creature self)
    {
        return orig(self) && !(self is Player p && p.isNPC /*&& IsControlledByPVStuff */);
    }
    private const float playerApproachDisengageRange = 20f;
    #endregion
    #region runtime required references
    /// <summary>
    /// those are references that don't need to be saved
    /// </summary>
    [JsonIgnore]
    //for switching between what points slugcat wants to go to right now
    private short currentPathingIndex = 0;
    [JsonIgnore]
    //the manipulated object
    AbstractCreature? abstractSlug;
    [JsonIgnore]
    //for when the mode is for following player
    Player? followingPlayer;
    [JsonIgnore]
    //for detecting behaviour change
    Behaviour lastBehaviour;
    [JsonIgnore]
    bool readyForAI;
    #endregion
    public override void Update(bool eu)
    {
        if (!ShouldSpawnForThisSlugcat() || !readyForAI) return;
        if (abstractSlug == null) Initiate();
        if (StaticStuff.devBuild && Input.GetKey(KeyCode.V)) ResetSlug();
        ErrorHandling();
        CognitiveFunctions();
        if (StaticStuff.devBuild) UpdateColor();
    }
    #region major helping functions
    void CognitiveFunctions()
    {
        if (lastBehaviour != behaviour) OnBehaviourChange();
        lastBehaviour = behaviour;
        if (abstractSlug?.abstractAI is SlugNPCAbstractAI absAI && absAI.RealAI is SlugNPCAI realAI && abstractSlug.realizedCreature is Player player)
        {
            switch (behaviour)
            {
                case Behaviour.sleeping:
                    {
                        //this behaviour is largely copypasted from Saint intro MSC room specific script. Except slugpup jumps when it is touched by anyone. Null controller helps it a lot though
                        player.touchedNoInputCounter = 10;
                        player.sleepCounter = 99;
                        player.standing = false;
                        player.flipDirection = 1;
                        player.sleepCurlUp = 1f;
                        player.controller = new Player.NullController();
                        ((PlayerGraphics)player.graphicsModule)?.LookAtNothing();
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
#warning will probably be deleted?
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
        var game = room.game;
        abstractSlug = new AbstractCreature(room.world,
            StaticWorld.GetCreatureTemplate(MoreSlugcatsEnums.CreatureTemplateType.SlugNPC), null, room.ToWorldCoordinate(point), game.GetNewID());
        if (abstractSlug.state is PlayerState state) state.forceFullGrown = fullgrown;

        abstractSlug.state = new PlayerNPCState(abstractSlug, 0);
        Player player = new Player(abstractSlug, game.world);
        player.npcCharacterStats = new SlugcatStats(SlugcatStats.Name.White, malnourished: false);
        abstractSlug.abstractAI = new SlugNPCAbstractAI(game.world, abstractSlug);
        abstractSlug.abstractAI.RealAI = new SlugNPCAI(abstractSlug, game.world);
        room.abstractRoom.AddEntity(abstractSlug);

        abstractSlug.RealizeInRoom();
        //abstractSlug.abstractAI.RealAI.pathFinder.Reset(room);
        if (forcepos) ApplySaintSleeping(abstractSlug.realizedCreature as Player, point);
        UpdateColor();
        OnBehaviourChange();
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
        if (abstractSlug?.abstractAI.RealAI is SlugNPCAI realAI)
        {
            realAI.nap = false;
            realAI.behaviorType = PVEnums.NPCBehaviour.completelyStill;
            realAI.SetDestination(default);
            switch (behaviour)
            {
                case Behaviour.sleeping:
                    {
                        realAI.nap = true;
                        var p = realAI.cat;
                        p.bodyMode = Player.BodyModeIndex.Crawl;
                        p.animation = Player.AnimationIndex.DownOnFours;
                        ((PlayerGraphics)p.graphicsModule)?.LookAtNothing();
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
        if (abstractSlug?.realizedCreature is not Player p || p.npcStats == null) return;

        var c = RWCustom.Custom.RGB2HSL(color);
        Vector3 d = new(p.npcStats.H, p.npcStats.S, p.npcStats.L);
        if (c == d) return;

        p.npcStats.H = c.x;
        p.npcStats.S = c.y;
        p.npcStats.L = c.z;
        p.npcStats.Dark = c.z == 0f;
        Array.ForEach(room.game.cameras, c => c.ApplyPalette());
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
    static void ApplySaintSleeping(Player player, Vector2 pos)
    {
        player.sleepCounter = 99;
        if (player.graphicsModule != null)
            player.SuperHardSetPosition(pos);
        else
        {
            Array.ForEach(player.bodyChunks, b => b.HardSetPosition(pos));
            player.bodyChunks[1].pos.x = player.bodyChunks[0].pos.x - 1f;
        }
        player.bodyChunks[1].pos.x = player.bodyChunks[0].pos.x + 3f;
        player.bodyChunks[1].pos.y = player.bodyChunks[0].pos.y;
        player.bodyChunks[0].vel *= 0f;
        player.bodyChunks[1].vel *= 0f;
        player.graphicsModule?.Reset();
    }

    public override void Destroy()
    {
        if (abstractSlug != null)
        {
            Array.ForEach(room.game.cameras, cam => RemoveObject(cam.spriteLeasers, abstractSlug.realizedCreature.graphicsModule));
            room.RemoveObject(abstractSlug.realizedCreature);
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

    public void ShortcutsReady()
    {}

    public void AIMapReady()
    {
        readyForAI = true;
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
