﻿using System.Collections.Generic;
using HarmonyLib;
using TUNING;
using SanchozzONIMods.Lib;

namespace Archaeologist
{
    internal sealed class ArchaeologistPatches : KMod.UserMod2
    {
        private const string Archaeologist = "Archaeologist";

        public override void OnLoad(Harmony harmony)
        {
            if (Utils.LogModVersion()) return;
            base.OnLoad(harmony);
        }

        [HarmonyPatch(typeof(Db), nameof(Db.Initialize))]
        private static class Db_Initialize
        {
            private static void Prefix()
            {
                DUPLICANTSTATS.GOODTRAITS.Add(new DUPLICANTSTATS.TraitVal
                {
                    id = Archaeologist,
                    statBonus = -DUPLICANTSTATS.SMALL_STATPOINT_BONUS,
                    rarity = DUPLICANTSTATS.RARITY_EPIC,
                    mutuallyExclusiveTraits = new List<string> { "CantResearch", "Uncultured" }
                });
            }

            private static void Postfix(ref Db __instance)
            {
                __instance.traits.Get(Archaeologist).PositiveTrait = true;
            }
        }

        [HarmonyPatch(typeof(Localization), nameof(Localization.Initialize))]
        internal static class Localization_Initialize
        {
            private static void Postfix()
            {
                Utils.InitLocalization(typeof(STRINGS));
                LocString.CreateLocStringKeys(typeof(STRINGS.DUPLICANTS));
            }
        }
    }
}
