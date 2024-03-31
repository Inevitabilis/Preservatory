using BepInEx;
using System.Security.Permissions;
using System.Security;
using System.Runtime.CompilerServices;
using UnityEngine;
using MonoMod.Cil;
using BepInEx.Logging;
using Mono.Cecil.Cil;
using static System.Reflection.BindingFlags;
//using Fisobs.Core;
using MonoMod.RuntimeDetour;
using System;
//using System.Linq;

#pragma warning disable CS0618
[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace PVStuffMod;

[BepInPlugin("lb-fgf-m4r-ik.pv-lab-stuff", "PVStuff", "1.0.0")]
sealed class PVStuff : BaseUnityPlugin
{
    class PVProperties
    {
        public const float MAX_SPEED = 5f, /*MAX_LIMB_SPEED = 9f, /*MAX_LIMB_QUICKNESS = .8f, MAX_NO_GRIP_SPEED = .25f,*/ MAX_LUNGE_SPEED = 1.9f,
            /*MAX_SWIM_SPEED = 1.9f,*/ MAX_AGG_CURVE_EXP = .7f, /*SLOW_SPEED = .015f, */MAX_WIGGLE_SPEED = .5f;
        public static readonly LizardBreedParams.SpeedMultiplier
            MaxTerrainSpeed1 = new(1f, 1f, 1f, 1f),
            MaxTerrainSpeed2 = new(1f, 1f, .9f, 1f),
            MaxTerrainSpeed3 = new(.9f, 1f, .6f, 1f),
            MaxTerrainSpeed4 = new(1f, 1f, 1f, 1f),
            MaxTerrainSpeed5 = new(.6f, 1f, 1f, 1f)/*,
            SlowTerrainSpeed = new(SLOW_SPEED, SLOW_SPEED, SLOW_SPEED, SLOW_SPEED)*/;
        public static readonly Color MinColor = new(.160784f, .152941f, .164706f), MaxColor = Lerp(MinColor, Color.white, .5f),
            InspectorColor = new(198f / 255f, 1f, 174 / 255f), MinColorInv = Lerp(Color.red, Color.black, .5f), MaxColorInv = Color.red;
        //public Color Color = MinColor;
        /*public float Speed = SLOW_SPEED, LimbSpeed = SLOW_SPEED, LimbQuickness = SLOW_SPEED, NoGripSpeed = SLOW_SPEED, LungeSpeed = SLOW_SPEED,
            SwimSpeed = SLOW_SPEED, AggressionCurveExponent = SLOW_SPEED;*/
        public float Lerper;
        public bool InvMode;
        /*public LizardBreedParams.SpeedMultiplier TerrainSpeed0 = SlowTerrainSpeed, TerrainSpeed1 = SlowTerrainSpeed, TerrainSpeed2 = SlowTerrainSpeed,
            TerrainSpeed3 = SlowTerrainSpeed, TerrainSpeed4 = SlowTerrainSpeed, TerrainSpeed5 = SlowTerrainSpeed;*/
    }

    const string K_PV = "PV";
    static ConditionalWeakTable<AbstractCreature, PVProperties> s_props = new();
    public static ManualLogSource? s_logger;

    public void OnEnable()
    {

        s_logger = Logger;
        On.RainWorld.OnModsInit += static (orig, self) =>
        {
            orig(self);
            MainLogic.Startup();
            if (!Futile.atlasManager.DoesContainAtlas("pvstuffspr"))
                Futile.atlasManager.LoadAtlas("atlases/pvstuffspr");
        };
        On.RainWorld.UnloadResources += static (orig, self) =>
        {
            orig(self);
            if (Futile.atlasManager.DoesContainAtlas("pvstuffspr"))
                Futile.atlasManager.UnloadAtlas("pvstuffspr");
        };
        On.AbstractCreature.ctor += static (orig, self, world, creatureTemplate, realizedCreature, pos, ID) =>
        {
            orig(self, world, creatureTemplate, realizedCreature, pos, ID);
            if (!s_props.TryGetValue(self, out _) && /*(creatureTemplate.type == CreatureTemplateType.LizardLivingCorpse || (*/ValidEnum(creatureTemplate.type) && (world.region?.name is K_PV || self.Room?.name is "rafters" or "specimens")/*))*/)
                s_props.Add(self, new() { InvMode = ModManager.MSC && world.game?.session is StoryGameSession s && s.saveStateNumber == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel });
        };
        IL.Lizard.ctor += static il =>
        {
            var c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After, x => x.MatchStfld<Lizard>(nameof(Lizard.lizardParams))))
            {
                c.Emit(OpCodes.Ldarg_0)
                 .Emit(OpCodes.Ldarg_1)
                 .Emit(OpCodes.Call, typeof(PVStuff).GetMethod(nameof(TryCopyParams), Public | NonPublic | Static));
            }
            else
                s_logger.LogError("Couldn't ILHook Lizard.ctor!");
        };
        /*On.Lizard.ctor += static (orig, self, abstractCreature, world) =>
        {
            orig(self, abstractCreature, world);
            if (abstractCreature.creatureTemplate.type == CreatureTemplateType.LizardLivingCorpse)
            {
                var chs = self.bodyChunks;
                for (var i = 0; i < chs.Length; i++)
                    chs[i].mass += .5f;
            }
        };
        On.LizardBreeds.BreedTemplate_Type_CreatureTemplate_CreatureTemplate_CreatureTemplate_CreatureTemplate += static (orig, type, lizardAncestor, pinkTemplate, blueTemplate, greenTemplate) =>
        {
            if (type == CreatureTemplateType.LizardLivingCorpse)
            {
                var template = orig(CreatureTemplate.Type.PinkLizard, lizardAncestor, pinkTemplate, blueTemplate, greenTemplate);
                var breedParams = (LizardBreedParams)template.breedParameters;
                breedParams.template = type;
                breedParams.danger = 0f;
                template.dangerousToPlayer = 0f;
                template.type = type;
                template.name = "LizardLivingCorpse";
                template.throwAction = "N/A";
                template.pickupAction = "N/A";
                template.jumpAction = "N/A";
                breedParams.aggressionCurveExponent = PVProperties.SLOW_SPEED;
                breedParams.baseSpeed = PVProperties.SLOW_SPEED;
                breedParams.standardColor = PVProperties.MinColor;
                breedParams.swimSpeed = PVProperties.SLOW_SPEED;
                breedParams.loungeSpeed = PVProperties.SLOW_SPEED;
                template.requireAImap = true;
                template.doPreBakedPathing = false;
                template.preBakedPathingAncestor = StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.PinkLizard);
                for (var i = 1; i < 6; i++)
                    breedParams.terrainSpeeds[i] = PVProperties.SlowTerrainSpeed;
                return template;
            }
            return orig(type, lizardAncestor, pinkTemplate, blueTemplate, greenTemplate);
        };*/
        On.Lizard.Update += static (orig, self, eu) =>
        {
            var acrit = self.abstractCreature;
            //var flag = s_props.TryGetValue(acrit, out var props) && !acrit.InDen;
            if (s_props.TryGetValue(acrit, out var props) && !acrit.InDen)
            //{
                //var prms = self.lizardParams;
                //var ts = prms.terrainSpeeds;
                /*if (acrit.creatureTemplate.type == CreatureTemplateType.LizardLivingCorpse)
                {
                    prms.aggressionCurveExponent = prms.swimSpeed = prms.loungeSpeed = prms.wiggleSpeed =/*prms.noGripSpeed = prms.limbQuickness = prms.limbSpeed =* prms.baseSpeed = PVProperties.SLOW_SPEED;
                    for (var i = 1; i < 6; i++)
                        ts[i] = PVProperties.SlowTerrainSpeed;
                    //self.effectColor = prms.standardColor = PVProperties.MinColor;
                }
                else
                {*/
                    /*var lrp = props.Lerper;
                    prms.baseSpeed = Lerp(prms.baseSpeed, PVProperties.MAX_SPEED, lrp);
                    //prms.limbSpeed = Lerp(prms.limbSpeed, PVProperties.MAX_LIMB_SPEED, lrp);
                    //prms.limbQuickness = Lerp(prms.limbQuickness, PVProperties.MAX_LIMB_QUICKNESS, lrp);
                    //prms.noGripSpeed = Lerp(prms.noGripSpeed, PVProperties.MAX_NO_GRIP_SPEED, lrp);
                    prms.loungeSpeed = Lerp(prms.loungeSpeed, PVProperties.MAX_LUNGE_SPEED, lrp);
                    //prms.swimSpeed = Lerp(prms.swimSpeed, PVProperties.MAX_SWIM_SPEED, lrp);
                    prms.aggressionCurveExponent = Lerp(prms.aggressionCurveExponent, PVProperties.MAX_AGG_CURVE_EXP, lrp);
                    prms.wiggleSpeed = Lerp(prms.wiggleSpeed, PVProperties.MAX_WIGGLE_SPEED, lrp);
                    ts[1] = Lerp(ts[1], PVProperties.MaxTerrainSpeed1, lrp);
                    ts[2] = Lerp(ts[2], PVProperties.MaxTerrainSpeed2, lrp);
                    ts[3] = Lerp(ts[3], PVProperties.MaxTerrainSpeed3, lrp);
                    ts[4] = Lerp(ts[4], PVProperties.MaxTerrainSpeed4, lrp);
                    ts[5] = Lerp(ts[5], PVProperties.MaxTerrainSpeed5, lrp);*/
                    //self.effectColor = prms.standardColor = PVProperties.MinColor;//Lerp(prms.standardColor, PVProperties.MaxColor, lrp);
                //}
                self.effectColor = self.lizardParams.standardColor = props.InvMode ? PVProperties.MinColorInv : PVProperties.MinColor;
            //}
            orig(self, eu);
            /*if (flag && self.Consious)
            {
                var chs = self.bodyChunks;
                if (acrit.creatureTemplate.type != CreatureTemplateType.LizardLivingCorpse)
                {
                    for (var i = 0; i < chs.Length; i++)
                    {
                        chs[i].vel.x = Lerp(chs[i].vel.x * .9f, chs[i].vel.x * 1.15f, props.Lerper);
                        if (chs[i].vel.x > 5f)
                            chs[i].vel.x = 5f;
                    }
                }
                else
                {
                    for (var i = 0; i < chs.Length; i++)
                        chs[i].vel.x *= .9f;
                }
            }*/
        };
        IL.LizardAI.IUseARelationshipTracker_UpdateDynamicRelationship += static il =>
        {
            var c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After,
                static x => x.MatchCall(typeof(Math).GetMethod(nameof(Math.Max), [typeof(int), typeof(int)])),
                static x => x.MatchStfld<LizardAI.LizardTrackState>(nameof(LizardAI.LizardTrackState.vultureMask))))
            {
                c.Emit(OpCodes.Ldarg_0)
                 .Emit(OpCodes.Ldarg_1)
                 .EmitDelegate(static (LizardAI self, RelationshipTracker.DynamicRelationship dRelation) =>
                 {
                     if (s_props.TryGetValue(self.creature, out _))
                         (dRelation.state as LizardAI.LizardTrackState)!.vultureMask = 0;
                 });
            }
            else
                s_logger.LogError("Couldn't ILHook LizardAI.IUseARelationshipTracker_UpdateDynamicRelationship!");
        };
        On.LizardAI.IUseARelationshipTracker_UpdateDynamicRelationship += static (orig, self, dRelation) =>
        {
            var res = orig(self, dRelation);
            var tp = dRelation.trackerRep.representedCreature.creatureTemplate.type;
            if (s_props.TryGetValue(self.creature, out _) && /*(*/ValidEnum(tp) /*|| tp == CreatureTemplateType.LizardLivingCorpse || self.creature.creatureTemplate.type == CreatureTemplateType.LizardLivingCorpse)*/)
            {
                res.type = CreatureTemplate.Relationship.Type.Ignores;
                res.intensity = 0f;
            }
            return res;
        };
        On.LizardAI.Update += static (orig, self) =>
        {
            orig(self);
            if (s_props.TryGetValue(self.creature, out var props)/* && self.creature.creatureTemplate.type != CreatureTemplateType.LizardLivingCorpse*/)
            {
                if (self.behavior == LizardAI.Behavior.Hunt && self.lizard?.Consious is true && self.preyTracker?.currentPrey?.critRep is Tracker.CreatureRepresentation rep && rep.representedCreature.creatureTemplate.type == CreatureTemplate.Type.Slugcat && rep.VisualContact)
                    props.Lerper = Min(props.Lerper + .01f, 1f);
                else
                    props.Lerper = Max(props.Lerper - .01f, 0f);
            }
        };
        On.LizardGraphics.Update += static (orig, self) =>
        {
            orig(self);
            if (self.lizard is Lizard l && s_props.TryGetValue(l.abstractCreature, out _) && self.lightSource is LightSource ls)
                ls.setAlpha = 0f;
        };
        On.LizardGraphics.DrawSprites += static (orig, self, sLeaser, rCam, timeStacker, camPos) =>
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            if (!self.debugVisualization && !self.culled && self.lizard is Lizard l && s_props.TryGetValue(l.abstractCreature, out var props))
            {
                for (int num4 = self.SpriteLimbsStart, num3 = self.SpriteLimbsColorStart - self.SpriteLimbsStart; num4 < self.SpriteLimbsEnd; num4++)
                    sLeaser.sprites[num4 + num3].color = props.InvMode ? PVProperties.MinColorInv : PVProperties.MinColor;
                if (l.lizardParams.headGraphics[4] is 0 or 1)
                {
                    var spr = sLeaser.sprites[self.SpriteHeadStart + 4];
                    spr.element = Futile.atlasManager.GetElementWithName("PV" + spr.element.name);
                }
            }
        };
        On.LizardAI.LurkTracker.Utility += static (orig, self) =>
        {
            var res = orig(self);
            if (self.AI is ArtificialIntelligence ai && s_props.TryGetValue(ai.creature, out var _))
                res = .5f;
            return res;
        };
        On.LizardAI.ctor += (orig, self, creature, world) =>
        {
            orig(self, creature, world);
            if (s_props.TryGetValue(creature, out _))
            {
                self.AddModule(self.lurkTracker = new(self, self.lizard));
                self.utilityComparer.AddComparedModule(self.lurkTracker, null, Mathf.Lerp(.4f, .3f, creature.personality.energy), 1f);
            }
        };
        On.LizardCosmetics.Antennae.ctor += static (orig, self, lGraphics, startSprite) =>
        {
            orig(self, lGraphics, startSprite);
            if (s_props.TryGetValue(lGraphics.lizard.abstractCreature, out var props))
                self.redderTint = props.InvMode ? PVProperties.MaxColorInv : PVProperties.MaxColor;
        };
        On.LizardSpit.ApplyPalette += static (orig, self, sLeaser, rCam, palette) =>
        {
            orig(self, sLeaser, rCam, palette);
            if (self.lizard?.abstractCreature is AbstractCreature c && s_props.TryGetValue(c, out var props))
                sLeaser.sprites[self.DotSprite].color = props.InvMode ? PVProperties.MaxColorInv : PVProperties.MinColor;
        };
        /*On.RainWorld.OnModsDisabled += static (orig, self, newlyDisabledMods) =>
        {
            orig(self, newlyDisabledMods);
            for (var i = 0; i < newlyDisabledMods.Length; i++)
            {
                if (newlyDisabledMods[i].id == "lb-fgf-m4r-ik.pv-lab-stuff")
                {
                    if (MultiplayerUnlocks.CreatureUnlockList.Contains(SandboxUnlockID.LizardLivingCorpse))
                        MultiplayerUnlocks.CreatureUnlockList.Remove(SandboxUnlockID.LizardLivingCorpse);
                    CreatureTemplateType.UnregisterValues();
                    SandboxUnlockID.UnregisterValues();
                    break;
                }
            }
        };*/
        new Hook(typeof(LizardGraphics).GetMethod("get_HeadColor2", Instance | Public | NonPublic), static (Func<LizardGraphics, Color> orig, LizardGraphics self) =>
        {
            var res = orig(self);
            if (self.lizard?.abstractCreature is AbstractCreature c && s_props.TryGetValue(c, out var props))
                res = props.InvMode ? Lerp(PVProperties.MinColorInv, PVProperties.MaxColorInv, props.Lerper) : Lerp(PVProperties.MinColor, PVProperties.MaxColor, props.Lerper);
            return res;
        });
        new Hook(typeof(MoreSlugcats.Inspector).GetMethod("get_OwneriteratorColor", Instance | Public | NonPublic), static (Func<MoreSlugcats.Inspector, Color> orig, MoreSlugcats.Inspector self) =>
        {
            if (self.abstractCreature is AbstractCreature c && (c.world?.region?.name is K_PV || c.Room?.name is "rafters" or "specimens"))
                return PVProperties.InspectorColor;
            return orig(self);
        });
        //Content.Register(new LizardLivingCorpseCritob());
    }

    /*[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Lerp(float a, float b, float t) => a + (b - a) * t;*/

    /*[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LizardBreedParams.SpeedMultiplier Lerp(LizardBreedParams.SpeedMultiplier a, LizardBreedParams.SpeedMultiplier b, float t)
    {
        a.speed += (b.speed - a.speed) * t;
        a.horizontal += (b.horizontal - a.horizontal) * t;
        a.up += (b.up - a.up) * t;
        a.down += (b.down - a.down) * t;
        return a;
    }*/

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color Lerp(Color a, Color b, float t)
    {
        a.r += (b.r - a.r) * t;
        a.g += (b.g - a.g) * t;
        a.b += (b.b - a.b) * t;
        return a;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Min(float a, float b) => (a < b) ? a : b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Max(float a, float b) => (a > b) ? a : b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ValidEnum(CreatureTemplate.Type type) => type == CreatureTemplate.Type.PinkLizard || type == CreatureTemplate.Type.BlueLizard ||
        type == CreatureTemplate.Type.GreenLizard || type == CreatureTemplate.Type.RedLizard || type == CreatureTemplate.Type.YellowLizard ||
        (ModManager.MSC && type == MoreSlugcats.MoreSlugcatsEnums.CreatureTemplateType.TrainLizard);

    public static void TryCopyParams(Lizard self, AbstractCreature abstractCreature)
    {
        if (s_props.TryGetValue(abstractCreature, out var props))
        {
            var op = self.lizardParams;
            self.lizardParams = new LizardBreedParams(null)
            {
                template = op.template,
                toughness = op.toughness,
                stunToughness = op.stunToughness,
                biteDamage = op.biteDamage,
                biteDamageChance = op.biteDamageChance,
                aggressionCurveExponent = PVProperties.MAX_AGG_CURVE_EXP,//PVProperties.SLOW_SPEED, //op.aggressionCurveExponent,
                danger = op.danger,
                biteDelay = op.biteDelay,
                baseSpeed = PVProperties.MAX_SPEED,//PVProperties.SLOW_SPEED, //op.baseSpeed,
                biteInFront = op.biteInFront,
                biteRadBonus = op.biteRadBonus,
                biteHomingSpeed = op.biteHomingSpeed,
                biteChance = op.biteChance,
                attemptBiteRadius = op.attemptBiteRadius,
                getFreeBiteChance = op.getFreeBiteChance,
                baseSpeedMultiplier = op.baseSpeedMultiplier,
                standardColor = props.InvMode ? PVProperties.MinColorInv : PVProperties.MinColor, //op.standardColor,
                regainFootingCounter = op.regainFootingCounter,
                bodyMass = op.bodyMass,
                bodySizeFac = op.bodySizeFac,
                bodyLengthFac = op.bodyLengthFac,
                bodyRadFac = op.bodyRadFac,
                pullDownFac = op.pullDownFac,
                floorLeverage = op.floorLeverage,
                maxMusclePower = op.maxMusclePower,
                terrainSpeeds = (LizardBreedParams.SpeedMultiplier[])op.terrainSpeeds.Clone(),
                wiggleSpeed = op.wiggleSpeed,
                wiggleDelay = op.wiggleDelay,
                bodyStiffnes = op.bodyStiffnes,
                swimSpeed = op.swimSpeed,//PVProperties.SLOW_SPEED, //op.swimSpeed,
                idleCounterSubtractWhenCloseToIdlePos = op.idleCounterSubtractWhenCloseToIdlePos,
                headShieldAngle = op.headShieldAngle,
                canExitLounge = op.canExitLounge,
                canExitLoungeWarmUp = op.canExitLoungeWarmUp,
                findLoungeDirection = op.findLoungeDirection,
                loungeDistance = op.loungeDistance,
                preLoungeCrouch = op.preLoungeCrouch,
                preLoungeCrouchMovement = op.preLoungeCrouchMovement,
                loungeSpeed = PVProperties.MAX_LUNGE_SPEED,//PVProperties.SLOW_SPEED, //op.loungeSpeed,
                loungePropulsionFrames = op.loungePropulsionFrames,
                loungeMaximumFrames = op.loungeMaximumFrames,
                loungeJumpyness = op.loungeJumpyness,
                loungeDelay = op.loungeDelay,
                riskOfDoubleLoungeDelay = op.riskOfDoubleLoungeDelay,
                postLoungeStun = op.postLoungeStun,
                loungeTendensy = op.loungeTendensy,
                perfectVisionAngle = op.perfectVisionAngle,
                periferalVisionAngle = op.periferalVisionAngle,
                shakePrey = op.shakePrey,
                biteDominance = op.biteDominance,
                limbSize = op.limbSize,
                limbThickness = op.limbThickness,
                stepLength = op.stepLength,
                liftFeet = op.liftFeet,
                feetDown = op.feetDown,
                noGripSpeed = op.noGripSpeed,
                limbSpeed = op.limbSpeed,
                limbQuickness = op.limbQuickness,
                limbGripDelay = op.limbGripDelay,
                smoothenLegMovement = op.smoothenLegMovement,
                legPairDisplacement = op.legPairDisplacement,
                walkBob = op.walkBob,
                tailSegments = op.tailSegments,
                tailStiffness = op.tailStiffness,
                tailStiffnessDecline = op.tailStiffnessDecline,
                tailLengthFactor = op.tailLengthFactor,
                tailColorationStart = op.tailColorationStart,
                tailColorationExponent = op.tailColorationExponent,
                headSize = op.headSize,
                neckStiffness = op.neckStiffness,
                jawOpenAngle = op.jawOpenAngle,
                jawOpenLowerJawFac = op.jawOpenLowerJawFac,
                jawOpenMoveJawsApart = op.jawOpenMoveJawsApart,
                headGraphics = (int[])op.headGraphics.Clone(),
                framesBetweenLookFocusChange = op.framesBetweenLookFocusChange,
                tongue = op.tongue,
                tongueAttackRange = op.tongueAttackRange,
                tongueWarmUp = op.tongueWarmUp,
                tongueSegments = op.tongueSegments,
                tongueChance = op.tongueChance,
                tamingDifficulty = op.tamingDifficulty
            };
            var ts = self.lizardParams.terrainSpeeds;
            /*for (var i = 1; i < 6; i++)
                ts[i] = PVProperties.SlowTerrainSpeed;*/
            ts[1] = PVProperties.MaxTerrainSpeed1;
            ts[2] = PVProperties.MaxTerrainSpeed2;
            ts[3] = PVProperties.MaxTerrainSpeed3;
            ts[4] = PVProperties.MaxTerrainSpeed4;
            ts[5] = PVProperties.MaxTerrainSpeed5;
        }
    }

    public void OnDisable()
    {
        s_props = null!;
        s_logger = null;
    }
}