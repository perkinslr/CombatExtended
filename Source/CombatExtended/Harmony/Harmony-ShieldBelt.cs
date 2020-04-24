﻿using HarmonyLib;
using Verse;
using RimWorld;
using Verse.AI;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(ShieldBelt), "AllowVerbCast")]
    internal static class ShieldBelt_PatchAllowVerbCast
    {
        internal static void Postfix(ref bool __result, IntVec3 root, Map map, LocalTargetInfo targ, Verb verb)
        {
            //Original: 
            //          return !(verb is Verb_LaunchProjectile) || ReachabilityImmediate.CanReachImmediate(root, targ, map, PathEndMode.Touch, null);

            //Preferred form:
            //          return !(verb is Verb_LaunchProjectile || verb is Verb_LaunchProjectileCE) || ReachabilityImmediate.CanReachImmediate(root, targ, map, PathEndMode.Touch, null);

            /*
             *  A   B   C   D       E
             *  0   0   0   1       1
             *  0   0   1   1       0
             *  0   1   0   1       1
             *  0   1   1   1       1
             *  1   0   0   0       0
             *  1   0   1   0       0
             *  1   1   0   1       1
             *  1   1   1   1       1
             * 
             * A = verb is Verb_LaunchProjectile
             * B = ReachabilityImmediate(..)
             * C = verb is Verb_LaunchProjectile_CE
             * D = (!A) || B
             * E = (!A && !C) || B
             * 
             * We know A, C and D. Can we get E?
             * 
             *  A   C   D       E
             *  0   0   1       1
             *  0   1   1       0
             *  0   0   1       1
             *  0   1   1       1   ==> Duplicate of line 2, but a different result
             *  1   0   0       0
             *  1   1   0       0
             *  1   0   1       1
             *  1   1   1       1
             *  
             *  This can only be distinguished if we know B, in which case the solution is B.
             *  And interestingly, the result is correct otherwise! See how D == E for all entries except for line 2.
             */

            //if (A=0 && C=1 && D=1) E=B;
            if (!(verb is Verb_LaunchProjectile) && (verb is Verb_LaunchProjectileCE) && __result)
            {
                __result = ReachabilityImmediate.CanReachImmediate(root, targ, map, PathEndMode.Touch, null);
            }

            //NOTE. The method could maybe be transpiled or something fancy
        }
    }

    [HarmonyPatch(typeof(ShieldBelt), "Tick")]
    internal static class ShieldBelt_DisableOnOperateTurret
    {
        internal static void Postfix(ShieldBelt __instance, ref int ___ticksToReset, int ___StartingTicksToReset)
        {
            if (__instance.Wearer?.CurJob?.def == JobDefOf.ManTurret && (__instance.Wearer?.jobs?.curDriver?.OnLastToil ?? false))
            {
                if (__instance.ShieldState == ShieldState.Active)
                {
                    Traverse.Create(__instance).Method("Break").GetValue();
                    ___ticksToReset = 120;
                }
                if (___ticksToReset < 120) 
                {
                    ___ticksToReset = 120;
                }
            }
        }
    }
}
