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
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using UnityEngine;
using static Pom.Pom;

namespace PVStuffMod.Logic.POM_objects;

public class PVSlugNPC : UpdatableAndDeletable
{
    public static void RegisterObjects()
    {
        RegisterFullyManagedObjectType([new BooleanField("fullgrown", true), new ColorField("color", new Color(1f, 1f, 1f), ManagedFieldWithPanel.ControlType.slider)], typeof(PVSlugNPC), "PVSleepingSlug");
    }


    public PlacedObject? pObj;
    public ManagedData? data;
    public void RefreshPOMValues()
    {
        if (pObj == null || data == null) return;
        if (pObj.type.value == "PVSleepingSlug")
        {
            point = pObj.pos;
            fullgrown = data.GetValue<bool>("fullgrown");
            color = data.GetValue<Color>("color");
            behaviour = Behaviour.sleeping;
        }
    }

    public PVSlugNPC(Room room, PlacedObject pObj)
    {
        this.pObj = pObj;
        this.data = pObj.data as ManagedData;
        RefreshPOMValues();
        whoami = SpecialNPC.survivor;
    }


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

    public Color color = new(1f, 1f, 1f);

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
    uint tickcounter = 0;
    #endregion
    public override void Update(bool eu)
    {
        tickcounter++;
        if (!ShouldSpawnForThisSlugcat() || room.aimap == null) return;
        if (abstractSlug == null) SpawnNPC();
        if (StaticStuff.devBuild && Input.GetKey(KeyCode.V)) ResetSlug();
        ErrorHandling();
        CognitiveFunctions();
        RefreshPOMValues();
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
    public void SpawnNPC()
    {
        if (!ShouldSpawnForThisSlugcat()) return;
        abstractSlug = new AbstractCreature(room.world,
            StaticWorld.GetCreatureTemplate(MoreSlugcatsEnums.CreatureTemplateType.SlugNPC), null, room.ToWorldCoordinate(point), room.game.GetNewID());
        if (abstractSlug.state is PlayerState state) state.forceFullGrown = fullgrown;
        room.abstractRoom.AddEntity(abstractSlug);
        abstractSlug.RealizeInRoom();
        if(behaviour == Behaviour.sleeping) ApplySaintSleeping(abstractSlug.realizedCreature as Player, point);
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
        SpawnNPC();
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

