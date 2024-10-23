using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using UnityEngine;

namespace PVStuffMod
{
    internal static class CreditHooks
    {
        internal static bool DoPVCredits = false;

        public static void ApplyHooks()
        {
            try
            {
                On.Menu.EndCredits.NextStage += EndCredits_NextStage;
                On.Menu.EndCredits.PreviousStage += EndCredits_PreviousStage;
                On.Menu.EndCredits.ExitCredits += EndCredits_ExitCredits;
                On.Menu.EndCredits.SpawnCreditsObject += EndCredits_SpawnCreditsObject;
                IL.Menu.CreditsTextAndImage.ctor += CreditsTextAndImage_ctor;
                On.Menu.CreditsTextAndImage.Update += CreditsTextAndImage_Update;
            }
            catch (System.Exception e)
            {
                MainLogic.logger.LogError(e);
            }
        }

        private static void EndCredits_NextStage(On.Menu.EndCredits.orig_NextStage orig, EndCredits self)
        {
            orig(self);
            if (!DoPVCredits)
            {
                while (PVEnums.Credits.IsPVCredit(self.currentStage))
                {
                    orig(self);
                }
            }
        }

        private static void EndCredits_PreviousStage(On.Menu.EndCredits.orig_PreviousStage orig, EndCredits self)
        {
            orig(self);
            if (!DoPVCredits)
            {
                while (PVEnums.Credits.IsPVCredit(self.currentStage))
                {
                    orig(self);
                }
            }
        }

        private static void EndCredits_ExitCredits(On.Menu.EndCredits.orig_ExitCredits orig, EndCredits self)
        {
            DoPVCredits = false;
            orig(self);
        }

        private static void EndCredits_SpawnCreditsObject(On.Menu.EndCredits.orig_SpawnCreditsObject orig, EndCredits self, bool startFromBottom)
        {
            if (PVEnums.Credits.IsPVCredit(self.currentStage))
            {
                self.currentCreditsObject = new CreditsTextAndImage(self, self.pages[0], self.currentStage, startFromBottom);
                self.pages[0].subObjects.Add(self.currentCreditsObject);
            }
            else
            {
                orig(self, startFromBottom);
            }
        }

        private static void CreditsTextAndImage_ctor(ILContext il)
        {
            var c = new ILCursor(il);

            // Grab local variable so we don't have to deal with if it changes in a future update
            int pathVar = 0;
            c.GotoNext(x => x.MatchLdstr(""), x => x.MatchStloc(out pathVar));

            // Go to spot and add our logic
            c.GotoNext(MoveType.AfterLabel, x => x.MatchLdloc(pathVar), x => x.MatchBrfalse(out _));
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, pathVar);
            c.EmitDelegate((CreditsTextAndImage self, string oldNm) =>
            {
                if (self.stage == PVEnums.Credits.PVLogo)
                {
                    self.subObjects.Add(new MenuIllustration(self.menu, self, "", "MainTitlePreservatory", new Vector2(683f, 384f), true, true) { alpha = 0f });
                    self.scroll = 0f;
                    self.lastScroll = 0f;
                    self.slowDownPos = 0f;
                    self.defaulScrollSpeed = 4f;
                    self.slowDownScrollSpeed = 4f;
                    return null;
                }
                else if (self.stage == PVEnums.Credits.PVCredits)
                {
                    self.defaulScrollSpeed = (ModManager.MMF ? 1.75f : 2f);
                    self.slowDownScrollSpeed = (ModManager.MMF ? 1f : 2f);
                    return "PRESERVATORY"; // it doesn't need to be in all caps but the rest of the code does it anyway so SCREAMING IS ALRIGHT I GUESS
                }
                return oldNm;
            });
            c.Emit(OpCodes.Stloc, pathVar);
        }

        private static void CreditsTextAndImage_Update(On.Menu.CreditsTextAndImage.orig_Update orig, CreditsTextAndImage self)
        {
            if (self.stage == PVEnums.Credits.PVLogo)
            {
                if (self.age < 80)
                {
                    self.pos.y = 0f;
                    self.scroll = 0f;
                    (self.menu as EndCredits)!.scrollSpeed = 0f;
                }
                (self.subObjects[0] as MenuIllustration)!.alpha = Custom.SCurve(Mathf.InverseLerp(0f, 60f, self.age), 0.65f);
            }
            orig(self);
        }
    }
}
