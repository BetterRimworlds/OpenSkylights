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

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace BetterRimworlds.OpenSkylights;

// The lighting overlay darkens every constructed-roof cell, and it darkens a
// corner when any cell touching that corner is roofed. Lit cells and the one
// cell past them stay open here, so that fringe falls outside the lit ring.
// Other roof queries are unchanged.
[HarmonyPatch]
public static class Patch_DaylightOverlay
{
    static readonly MethodInfo RoofAt = AccessTools.Method(typeof(RoofGrid), nameof(RoofGrid.RoofAt), new[] { typeof(int) });

    static readonly MethodInfo Roofed = AccessTools.Method(typeof(RoofGrid), nameof(RoofGrid.Roofed), new[] { typeof(int) });

    static readonly MethodInfo OpenRoofAt = AccessTools.Method(typeof(Patch_DaylightOverlay), nameof(RoofAtOpen));

    static readonly MethodInfo OpenRoofed = AccessTools.Method(typeof(Patch_DaylightOverlay), nameof(RoofedOpen));

    static readonly AccessTools.FieldRef<RoofGrid, Map> MapOf = AccessTools.FieldRefAccess<RoofGrid, Map>("map");

    static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(SectionLayer_LightingOverlay), "GenerateLightingOverlay")
            ?? AccessTools.Method(typeof(SectionLayer_LightingOverlay), "Regenerate");
    }

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        int replaced = 0;
        foreach (CodeInstruction instruction in instructions)
        {
            if (instruction.Calls(RoofAt))
            {
                instruction.opcode = OpCodes.Call;
                instruction.operand = OpenRoofAt;
                replaced++;
            }
            else if (instruction.Calls(Roofed))
            {
                instruction.opcode = OpCodes.Call;
                instruction.operand = OpenRoofed;
                replaced++;
            }

            yield return instruction;
        }

        if (replaced == 0)
            Log.Warning("[OpenSkylights] Lighting overlay had no roof calls to open.");
    }

    public static RoofDef RoofAtOpen(RoofGrid grid, int index)
    {
        if (Lit(grid, index))
            return null;

        return grid.RoofAt(index);
    }

    public static bool RoofedOpen(RoofGrid grid, int index)
    {
        if (Lit(grid, index))
            return false;

        return grid.Roofed(index);
    }

    static bool Lit(RoofGrid grid, int index)
    {
        Map map = MapOf(grid);
        return map != null && map.GetComponent<MapComp_Skylights>()?.IsOpenToSky(index) == true;
    }
}
