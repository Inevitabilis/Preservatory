using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RWCustom;

namespace PVStuffMod.Logic;

internal class TheEgg : BackgroundScene.BackgroundSceneElement, IDrawable
{
    internal float whiteFade;
    internal float lastWhiteFade;

    internal bool visible;
    internal float eggProximity;
    private float musicVolume;
    private float musicVolumeDirectionBoost;
    private List<float> playerDists;
    private float maxAllowedDist;
    private int fadeWait;
    private bool exitCommand;
    private int counter;
    

    public TheEgg(BackgroundScene scene, Vector2 pos, float depth) : base(scene, pos, depth)
    {}

    #region Graphical stuff
    public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        
        sLeaser.sprites = new FSprite[5];
        sLeaser.sprites[0] = new FSprite("Futile_White", true);
        sLeaser.sprites[0].shader = rCam.game.rainWorld.Shaders["FlatLightNoisy"];
        sLeaser.sprites[sLeaser.sprites.Length - 3] = new FSprite("Futile_White", true);
        sLeaser.sprites[sLeaser.sprites.Length - 3].shader = rCam.game.rainWorld.Shaders["FlatLightNoisy"];
        sLeaser.sprites[sLeaser.sprites.Length - 2] = new FSprite("Futile_White", true);
        sLeaser.sprites[sLeaser.sprites.Length - 2].shader = rCam.game.rainWorld.Shaders["FlatLightNoisy"];
        sLeaser.sprites[sLeaser.sprites.Length - 1] = new FSprite("Futile_White", true);
        sLeaser.sprites[sLeaser.sprites.Length - 1].scaleX = 93.75f;
        sLeaser.sprites[sLeaser.sprites.Length - 1].scaleY = 56.25f;
        sLeaser.sprites[sLeaser.sprites.Length - 1].x = 700f;
        sLeaser.sprites[sLeaser.sprites.Length - 1].y = 400f;
        for (int i = 1; i < sLeaser.sprites.Length - 3; i++)
        {
            sLeaser.sprites[i] = new FSprite("Futile_White", true);
            sLeaser.sprites[i].shader = rCam.game.rainWorld.Shaders["FlatLightNoisy"];
            sLeaser.sprites[i].color = new Color(0f, 0f, 0f);
            sLeaser.sprites[i].anchorY = 0.2f;
        }
        Array.ForEach(sLeaser.sprites, x => x.isVisible = false);
        AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("Foreground"));
    }

    public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        visible = true;
        Vector2 vector = pos;
        if (!new FloatRect(camPos.x, camPos.y, camPos.x + 1400f, camPos.y + 800f).Vector2Inside(pos))
        {
            vector = Custom.RectCollision(camPos + new Vector2(700f, 400f), pos, new FloatRect(camPos.x, camPos.y, camPos.x + 1400f, camPos.y + 800f)).GetCorner(3);
        }
        eggProximity = Mathf.InverseLerp(10000f, 0f, Vector2.Distance(vector, pos));
        
        vector = Custom.MoveTowards(vector, pos, (1f - eggProximity) * Mathf.Lerp(150f, 200f, Mathf.Pow(eggProximity, 4f)) * 4f);
        sLeaser.sprites[0].scale = Mathf.Lerp(150f, 300f, Mathf.Pow(eggProximity, 4f));
        sLeaser.sprites[0].alpha = 0.25f * eggProximity;
        sLeaser.sprites[sLeaser.sprites.Length - 3].scale = Mathf.Lerp(0f, 150f, Mathf.Pow(Mathf.InverseLerp(0.5f, 1f, eggProximity), 4f));
        sLeaser.sprites[sLeaser.sprites.Length - 3].alpha = Mathf.InverseLerp(0.5f, 1f, eggProximity);
        sLeaser.sprites[sLeaser.sprites.Length - 2].scale = Mathf.Lerp(0f, 100f, Mathf.Pow(Mathf.InverseLerp(0.5f, 1f, eggProximity), 2f));
        sLeaser.sprites[sLeaser.sprites.Length - 2].alpha = Mathf.Pow(Mathf.InverseLerp(0.85f, 1f, eggProximity), 3f);
        sLeaser.sprites[0].x = vector.x - camPos.x;
        sLeaser.sprites[0].y = vector.y - camPos.y;
        sLeaser.sprites[sLeaser.sprites.Length - 3].x = vector.x - camPos.x;
        sLeaser.sprites[sLeaser.sprites.Length - 3].y = vector.y - camPos.y;
        sLeaser.sprites[sLeaser.sprites.Length - 2].x = vector.x - camPos.x;
        sLeaser.sprites[sLeaser.sprites.Length - 2].y = vector.y - camPos.y;
        sLeaser.sprites[sLeaser.sprites.Length - 2].isVisible = false;
        base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
        for (int i = 1; i < sLeaser.sprites.Length - 3; i++)
        {
            Player player = null;
            if (i == 1)
            {
                player = room.game.FirstRealizedPlayer;
                if (ModManager.CoopAvailable)
                {
                    player = room.game.RealizedPlayerFollowedByCamera;
                }
            }
            if (player == null)
            {
                sLeaser.sprites[i].isVisible = false;
            }
            else
            {
                sLeaser.sprites[i].isVisible = true;
                PlayerGraphics playerGraphics = player.graphicsModule as PlayerGraphics;
                Vector2 a = Vector2.Lerp(playerGraphics.drawPositions[0, 1], playerGraphics.drawPositions[0, 0], timeStacker);
                Vector2 b = Vector2.Lerp(playerGraphics.drawPositions[1, 1], playerGraphics.drawPositions[1, 0], timeStacker);
                Vector2 vector2 = Vector2.Lerp(vector, pos, 0.1f);
                Vector2 vector3 = (a + b) / 2f - camPos;
                sLeaser.sprites[i].x = vector3.x;
                sLeaser.sprites[i].y = vector3.y;
                float f = Mathf.Abs(Vector2.Dot((a - b).normalized, (vector2 - (a + b) / 2f).normalized));
                sLeaser.sprites[i].scaleX = Mathf.Lerp(100f, 50f, Mathf.Pow(f, 2f)) / 16f;
                sLeaser.sprites[i].scaleY = Mathf.Lerp(600f, 700f, Mathf.Pow(f, 2f)) * Mathf.Pow(1f - eggProximity, 0.5f) / 16f;
                sLeaser.sprites[i].rotation = Custom.AimFromOneVectorToAnother(vector2, (a + b) / 2f);
                sLeaser.sprites[i].alpha = Mathf.Pow(Mathf.Sin(Mathf.Pow(eggProximity, 5f) * (float)Math.PI), 0.5f) * 0.15f * Mathf.Pow(Mathf.InverseLerp(385f, 100f, Vector2.Distance(new Vector2(400f, 400f), Custom.FlattenVectorAlongAxis(vector3 - new Vector2(400f, 400f), 90f, 0.5f) + new Vector2(400f, 400f))), 0.5f);
            }
        }
        sLeaser.sprites[sLeaser.sprites.Length - 1].isVisible = (visible && (lastWhiteFade > 0f || whiteFade > 0f));
        sLeaser.sprites[sLeaser.sprites.Length - 1].alpha = Mathf.Pow(Mathf.Max(0f, Mathf.Lerp(lastWhiteFade, whiteFade, timeStacker)), 1.8f);
    }
    #endregion
    public override void Update(bool eu)
    {
        base.Update(eu);
        Player player = room.game.FirstRealizedPlayer;
        if (ModManager.CoopAvailable)
        {
            player = room.game.RealizedPlayerFollowedByCamera;
        }
        if (player != null)
        {
            float num = Vector2.Distance(player.mainBodyChunk.pos, pos);
            musicVolume = Mathf.InverseLerp(Mathf.Lerp(6000f, 11000f, musicVolumeDirectionBoost), 
                Mathf.Lerp(500f, 3000f, musicVolumeDirectionBoost), 
                num) 
                * Custom.SCurve(Mathf.InverseLerp(100f, 1600f, (float)counter), 0.6f);
                
            playerDists.Insert(0, num);
            if (playerDists.Count > 100)
            {
                playerDists.RemoveAt(playerDists.Count - 1);
            }
            if (Custom.DistLess(player.mainBodyChunk.pos, pos, 350f))
            {
                FadeToWhite();
            }
        }
        lastWhiteFade = whiteFade;
        if (fadeWait > 0)
        {
            fadeWait--;
        }
        else
        {
            if (whiteFade >= 1f)
            {
                if (!exitCommand)
                {
                    EscapeToDream(room.game);
                }
                exitCommand = true;
                fadeWait = 20;
            }
            else if (whiteFade > 0f)
            {
                whiteFade = Mathf.Min(1f, whiteFade + 0.025f);
            }
            
        }
        if (playerDists.Count > 1)
        {
            musicVolumeDirectionBoost = Custom.LerpAndTick(musicVolumeDirectionBoost, 
                Mathf.InverseLerp(100f, -100f, playerDists[0] - playerDists[playerDists.Count - 1]), 
                0.002f, 
                0.033333335f);
        }
    }
    private void FadeToWhite()
    {
        if (whiteFade == 0f)
        {
            whiteFade = 0.002f;
            room.PlaySound(SoundID.Void_Sea_Swim_Into_Core, 0f, 1f, 1f);
        }
    }

    private void EscapeToDream(RainWorldGame game)
    {
        /*if(game.StoryCharacter == )
        game.PlayersToProgressOrWin.ForEach(x =>
        {
            x.pos.room = 
        });*/
    }
}
