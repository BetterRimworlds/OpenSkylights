/*
 * This file is part of BetterRimworlds.OpenSkylights, a Better Rimworlds Project.
 *
 * Copyright © 2026 Theodore R. Smith
 * Author: Theodore R. Smith <hopeseekr@gmail.com>
 *   GPG Fingerprint: D8EA 6E4D 5952 159D 7759  2BB4 EEB6 CE72 F441 EC41
 *   https://github.com/BetterRimworlds/BetterRimworlds.OpenSkylights
 *
 * This file is licensed under the MIT License.
 */

using System.Reflection;
using HarmonyLib;
using Verse;

namespace BetterRimworlds.OpenSkylights;

static class GlowSky
{
    public static void Apply(Map map, IntVec3 cell, ref float result)
    {
        if (map == null || !MapComp_Skylights.Lit(map, cell))
            return;

        float sky = map.skyManager.CurSkyGlow;
        if (sky > result)
            result = sky;
    }
}

// 1.5+ asks GroundGlowAt. ignoreSky callers want artificial light only.
[HarmonyPatch]
public static class Patch_GroundGlowAt
{
    static bool Prepare()
    {
        return Ground() != null;
    }

    static MethodBase TargetMethod()
    {
        return Ground();
    }

    static void Postfix(Map ___map, IntVec3 c, bool ignoreSky, ref float __result)
    {
        if (!ignoreSky)
            GlowSky.Apply(___map, c, ref __result);
    }

    static MethodBase Ground()
    {
        return AccessTools.Method(typeof(GlowGrid), "GroundGlowAt", new[] { typeof(IntVec3), typeof(bool), typeof(bool) });
    }
}

// 1.2–1.4 ask GameGlowAt and have no ignoreSky argument.
[HarmonyPatch]
public static class Patch_GameGlowAt
{
    static bool Prepare()
    {
        return AccessTools.Method(typeof(GlowGrid), "GroundGlowAt", new[] { typeof(IntVec3), typeof(bool), typeof(bool) }) == null
            && Game() != null;
    }

    static MethodBase TargetMethod()
    {
        return Game();
    }

    static void Postfix(Map ___map, IntVec3 c, ref float __result)
    {
        GlowSky.Apply(___map, c, ref __result);
    }

    static MethodBase Game()
    {
        return AccessTools.Method(typeof(GlowGrid), "GameGlowAt", new[] { typeof(IntVec3), typeof(bool) });
    }
}
