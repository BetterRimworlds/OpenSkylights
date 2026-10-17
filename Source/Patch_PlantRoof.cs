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
using RimWorld;
using Verse;

namespace BetterRimworlds.OpenSkylights;

static class PlantRoof
{
    static readonly MethodInfo Roofed = AccessTools.Method(
        typeof(GridsUtility),
        nameof(GridsUtility.Roofed),
        new[] { typeof(IntVec3), typeof(Map) });

    static readonly MethodInfo CountsAsRoof = AccessTools.Method(typeof(PlantRoof), nameof(RoofBlocksPlant));

    public static bool RoofBlocksPlant(IntVec3 cell, Map map)
    {
        if (MapComp_Skylights.Lit(map, cell))
            return false;

        return cell.Roofed(map);
    }

    public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions, string owner)
    {
        int replaced = 0;
        foreach (CodeInstruction instruction in instructions)
        {
            if (instruction.Calls(Roofed))
            {
                instruction.opcode = OpCodes.Call;
                instruction.operand = CountsAsRoof;
                replaced++;
            }

            yield return instruction;
        }

        if (replaced == 0)
            Log.Warning("[OpenSkylights] " + owner + " had no plant roof check to retarget.");
    }
}

[HarmonyPatch]
public static class Patch_GrowerSowRoof
{
    static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(WorkGiver_GrowerSow), nameof(WorkGiver_GrowerSow.JobOnCell));
    }

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        return PlantRoof.Transpile(instructions, "WorkGiver_GrowerSow.JobOnCell");
    }
}

[HarmonyPatch]
public static class Patch_CompPlantableRoof
{
    static bool Prepare()
    {
        return AccessTools.TypeByName("RimWorld.CompPlantable") != null;
    }

    static MethodBase TargetMethod()
    {
        return AccessTools.Method(AccessTools.TypeByName("RimWorld.CompPlantable"), "CanPlantAt", new[] { typeof(IntVec3), typeof(Map) });
    }

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        return PlantRoof.Transpile(instructions, "CompPlantable.CanPlantAt");
    }
}

[HarmonyPatch]
public static class Patch_ReplantRoof
{
    static bool Prepare()
    {
        return AccessTools.TypeByName("RimWorld.WorkGiver_Replant") != null;
    }

    static MethodBase TargetMethod()
    {
        return AccessTools.Method(AccessTools.TypeByName("RimWorld.WorkGiver_Replant"), "JobOnThing");
    }

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        return PlantRoof.Transpile(instructions, "WorkGiver_Replant.JobOnThing");
    }
}

[HarmonyPatch]
public static class Patch_DesignatorReplantRoof
{
    static bool Prepare()
    {
        return AccessTools.TypeByName("RimWorld.Designator_Replant") != null;
    }

    static MethodBase TargetMethod()
    {
        return AccessTools.Method(AccessTools.TypeByName("RimWorld.Designator_Replant"), "CanDesignateCell");
    }

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        return PlantRoof.Transpile(instructions, "Designator_Replant.CanDesignateCell");
    }
}
