using Newtonsoft.Json.Linq;
using ROM.RoomObjectService;
using ROM.UserInteraction.InroomManagement;
using ROM.UserInteraction.ObjectEditorElement;
using System;
using System.Collections.Generic;
using MoreSlugcats;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using PVStuffMod;

namespace PVStuff.Logic.ROM_objects;

public class NPC2 : MoreSlugcats.MSCRoomSpecificScript.ArtificerDream
{
    public bool SetPosition = false;
    public Vector2 Position = new(500,500);
    private void Log(string e) => MainLogic.Log(e);


    public NPC2(Room room)
    {
        this.room = room;
        room.game.cameras[0].followAbstractCreature = null;
    }
    public NPC2() { }

    public override Player.InputPackage GetInput(int index)
    {
        AbstractCreature firstAlivePlayer = room.game.FirstAlivePlayer;
        if (firstAlivePlayer.realizedCreature is not Player player) return default;
        if (sceneTimer < 160)
        {
            if (firstAlivePlayer != null)
            {
                player.SuperHardSetPosition(artyPlayerPuppet.firstChunk.pos);
            }
            return default;
        }
        if (sceneTimer == 160)
        {
            artyPlayerPuppet.bodyChunks[0].vel *= 0f;
            artyPlayerPuppet.bodyChunks[1].vel *= 0f;
            artyPlayerPuppet.bodyChunks[0].pos = new Vector2(1900f, 340f);
            artyPlayerPuppet.bodyChunks[1].pos = new Vector2(1900f, 320f);
            return new Player.InputPackage(false, Options.ControlSetup.Preset.None, 1, 1, false, false, true, false, false);
        }
        if (sceneTimer == 165)
        {
            return new Player.InputPackage(false, Options.ControlSetup.Preset.None, 1, 1, false, false, true, false, false);
        }
        if (sceneTimer < 166)
        {
            return new Player.InputPackage(false, Options.ControlSetup.Preset.None, 1, 1, false, false, true, false, false);
        }
        if (sceneTimer == 166)
        {
            artyPlayerPuppet.bodyChunks[0].vel += new Vector2(10f, 13f);
            artyPlayerPuppet.bodyChunks[1].vel += new Vector2(10f, 13f);
            room.AddObject(new ExplosionSpikes(room, artyPlayerPuppet.bodyChunks[0].pos + new Vector2(0f, -artyPlayerPuppet.bodyChunks[0].rad), 8, 7f, 5f, 5.5f, 40f, new Color(1f, 1f, 1f, 0.5f)));
            room.PlaySound(SoundID.Slugcat_Rocket_Jump, artyPlayerPuppet.bodyChunks[0], false, 1f, 1f);
            return new Player.InputPackage(false, Options.ControlSetup.Preset.None, 1, 1, true, false, true, false, false);
        }
        if (sceneTimer <= 190)
        {
            return new Player.InputPackage(false, Options.ControlSetup.Preset.None, 1, 1, true, false, false, false, false);
        }
        if (sceneTimer <= 210)
        {
            return new Player.InputPackage(false, Options.ControlSetup.Preset.None, 0, 1, false, false, false, false, false);
        }
        if (sceneTimer == 211)
        {
            return new Player.InputPackage(false, Options.ControlSetup.Preset.None, 0, 0, false, false, false, false, false);
        }
        if (sceneTimer == 212)
        {
            return new Player.InputPackage(false, Options.ControlSetup.Preset.None, 0, 1, false, false, false, false, false);
        }
        if (sceneTimer <= 239)
        {
            return default(Player.InputPackage);
        }
        if (sceneTimer <= 300)
        {
            return new Player.InputPackage(false, Options.ControlSetup.Preset.None, 1, 1, false, false, false, false, false);
        }
        if (sceneTimer == 301)
        {
            return new Player.InputPackage(false, Options.ControlSetup.Preset.None, 0, 0, false, false, false, false, false);
        }
        if (sceneTimer == 302)
        {
            artyPlayerPuppet.slugOnBack.DropSlug();
            if (firstAlivePlayer != null)
            {
                player.Stun(5);
                player.firstChunk.vel = new Vector2(5f, 5f);
                player.standing = true;
            }
            return new Player.InputPackage(false, Options.ControlSetup.Preset.None, 0, 0, false, false, false, false, false);
        }
        if (sceneTimer <= 304)
        {
            return new Player.InputPackage(false, Options.ControlSetup.Preset.None, 1, 0, false, false, false, false, false);
        }
        if (sceneTimer <= 325)
        {
            ArtyGoalPos = room.MiddleOfTile(116, 18);
            return new Player.InputPackage(false, Options.ControlSetup.Preset.None, 1, 1, true, false, false, false, false);
        }
        if (sceneTimer > 325)
        {
            bool flag = false;
            int num = 0;
            if (artyPlayerPuppet.firstChunk.pos.x < ArtyGoalPos.x - 9f)
            {
                num = 1;
            }
            else if (artyPlayerPuppet.firstChunk.pos.x > ArtyGoalPos.x + 9f)
            {
                num = -1;
                flag = sceneTimer % 20 <= 5 && artyPlayerPuppet.bodyMode != Player.BodyModeIndex.ClimbingOnBeam;
            }
            int num2 = 0;
            if (artyPlayerPuppet.bodyMode == Player.BodyModeIndex.ClimbingOnBeam)
            {
                if (artyPlayerPuppet.firstChunk.pos.y < ArtyGoalPos.y - 5f)
                {
                    if (artyPlayerPuppet.firstChunk.pos.x > ArtyGoalPos.x + 9f)
                    {
                        num2 = ((sceneTimer % 20 <= 5) ? 0 : 1);
                    }
                    else
                    {
                        num2 = 1;
                    }
                }
                else if (artyPlayerPuppet.firstChunk.pos.y > ArtyGoalPos.y + 5f)
                {
                    num2 = -1;
                }
            }
            else
            {
                num2 = UnityEngine.Random.Range(0, 2);
            }
            if (pup2Puppet.state.dead && sceneTimer < 2000)
            {
                Log("Other pup died! cut early");
                sceneTimer = 1999;
            }
            return new Player.InputPackage(false, Options.ControlSetup.Preset.None, num, num2, flag, false, false, false, false);
        }
        return default(Player.InputPackage);
    }

    private void SpawnAmbientCritters()
    {
        for (int i = 0; i < 3; i++)
        {
            AbstractCreature abstractCreature = new AbstractCreature(room.world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.CicadaA), null, new WorldCoordinate(room.abstractRoom.index, 200 + (int)(UnityEngine.Random.value * 80f), 32 + (int)(UnityEngine.Random.value * 10f), -1), room.game.GetNewID());
            room.abstractRoom.AddEntity(abstractCreature);
            abstractCreature.RealizeInRoom();
        }
        for (int j = 0; j < 2; j++)
        {
            AbstractCreature abstractCreature2 = new AbstractCreature(room.world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.CicadaB), null, new WorldCoordinate(room.abstractRoom.index, 200 + (int)(UnityEngine.Random.value * 80f), 36 + (int)(UnityEngine.Random.value * 10f), -1), room.game.GetNewID());
            room.abstractRoom.AddEntity(abstractCreature2);
            abstractCreature2.RealizeInRoom();
        }
        for (int k = 0; k < 15; k++)
        {
            AbstractCreature abstractCreature3 = new AbstractCreature(room.world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Fly), null, new WorldCoordinate(room.abstractRoom.index, 90 + (int)(UnityEngine.Random.value * 80f), 22 + (int)(UnityEngine.Random.value * 10f), -1), room.game.GetNewID());
            room.abstractRoom.AddEntity(abstractCreature3);
            abstractCreature3.RealizeInRoom();
        }
    }

    public override void SceneSetup()
    {
        if (artificerPuppet == null)
        {
            artificerPuppet = new AbstractCreature(room.world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Slugcat), null, new WorldCoordinate(room.abstractRoom.index, 87, 8, -1), room.game.GetNewID());
            artificerPuppet.state = new PlayerState(artificerPuppet, 0, MoreSlugcatsEnums.SlugcatStatsName.Artificer, true);
            room.abstractRoom.AddEntity(artificerPuppet);
            pup2Puppet = new AbstractCreature(room.world, StaticWorld.GetCreatureTemplate(MoreSlugcatsEnums.CreatureTemplateType.SlugNPC), null, new WorldCoordinate(room.abstractRoom.index, 87, 8, -1), room.game.GetNewID());
            pup2Puppet.ID.setAltSeed(1001);
            pup2Puppet.state = new PlayerNPCState(pup2Puppet, 0);
            room.abstractRoom.AddEntity(pup2Puppet);
            artificerPuppet.RealizeInRoom();
            pup2Puppet.RealizeInRoom();
        }
        if (artyPlayerPuppet == null && artificerPuppet.realizedCreature != null)
        {
            artyPlayerPuppet = artificerPuppet.realizedCreature as Player;
        }
        if (pupPlayerPuppet == null && pup2Puppet.realizedCreature != null)
        {
            pupPlayerPuppet = pup2Puppet.realizedCreature as Player;
        }
        AbstractCreature firstAlivePlayer = room.game.FirstAlivePlayer;
        if (firstAlivePlayer != null && artyPlayerPuppet != null && pupPlayerPuppet != null && firstAlivePlayer.realizedCreature != null)
        {
            Log("scene start");
            SpawnAmbientCritters();
            pup2Puppet.state.socialMemory.GetOrInitiateRelationship(firstAlivePlayer.ID).InfluenceLike(1f);
            pup2Puppet.state.socialMemory.GetOrInitiateRelationship(firstAlivePlayer.ID).InfluenceTempLike(1f);
            artyPlayerPuppet.controller = new MSCRoomSpecificScript.ArtificerDream.StartController(this, 0);
            artyPlayerPuppet.standing = true;
            artyPlayerPuppet.slugcatStats.visualStealthInSneakMode = 2f;
            if (firstAlivePlayer.realizedCreature != null)
            {
                (firstAlivePlayer.realizedCreature as Player).SuperHardSetPosition(artyPlayerPuppet.firstChunk.pos);
                firstAlivePlayer.pos = artyPlayerPuppet.abstractCreature.pos;
            }
            artyPlayerPuppet.slugOnBack.SlugToBack(firstAlivePlayer.realizedCreature as Player);
            sceneStarted = true;
        }
    }

    public override void CameraSetup()
    {
        //room.game.cameras[0].MoveCamera(2);
    }

    public override void TimedUpdate(int timer)
    {
        if (artyPlayerPuppet.firstChunk.pos.y < 220f)
        {
            ArtyGoalPos = artyPlayerPuppet.firstChunk.pos;
        }
        if (sceneTimer == 2000)
        {
            Log("Dream over");
            room.game.ArtificerDreamEnd();
        }
    }

    private Player artyPlayerPuppet;

    private Player pupPlayerPuppet;

    private AbstractCreature artificerPuppet;

    private AbstractCreature pup2Puppet;

    private Vector2 ArtyGoalPos;
}


public class NPC2Operator : TypeOperator<NPC2>
{
    private static VersionedLoader<NPC2> VersionedLoader { get; } =
            TypeOperatorUtils.CreateVersionedLoader<NPC2>(defaultLoad: TypeOperatorUtils.TrivialLoad<NPC2>);
    public override string TypeId => nameof(NPC2);

    public override void AddToRoom(NPC2 obj, Room room)
    {
        room.AddObject(obj);
    }

    public override NPC2 CreateNew(Room room, Rect currentCameraRect)
    {
        return new() { room = room };
    }

    public override IEnumerable<IObjectEditorElement> GetEditorElements(NPC2 obj, Room room)
    {
        yield return Elements.Checkbox("Force?", () => obj.SetPosition, x => obj.SetPosition = x);
        yield return Elements.Point("position?", "p", () => obj.Position, x => obj.Position = x);
    }

    public override NPC2 Load(JToken dataJson, Room room)
    {
        return VersionedLoader.Load(dataJson, room);
    }

    public override void RemoveFromRoom(NPC2 obj, Room room)
    {
        room.RemoveObject(obj);
    }

    public override JToken Save(NPC2 obj)
    {
        return TypeOperatorUtils.GetTrivialVersionedSaveCall<NPC2>("0.0.0").Invoke(obj);
    }
}
