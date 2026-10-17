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

static class GardenRoof
{
    public static bool UsesOutdoorTemperature(IntVec3 cell, Map map)
    {
        return OpenSky(cell, map) || cell.UsesOutdoorTemperature(map);
    }

    public static RoofDef GetRoof(IntVec3 cell, Map map)
    {
        return OpenSky(cell, map) ? null : cell.GetRoof(map);
    }

    public static bool Roofed(IntVec3 cell, Map map)
    {
        return !OpenSky(cell, map) && cell.Roofed(map);
    }

    static bool OpenSky(IntVec3 cell, Map map)
    {
        return map != null
            && MapComp_Skylights.Lit(map, cell)
            && IndoorGardenUtility.IsIndoorGarden(cell.GetRoom(map));
    }
}

// Need_Outdoors asks both whether the cell uses outdoor temperature and which
// roof is overhead. Open only the lit cells of an Indoor Garden. Other rooms
// keep their normal roof.
[HarmonyPatch]
public static class Patch_OutdoorsNeedRoof
{
    static readonly MethodInfo UsesOutdoorTemperature = AccessTools.Method(
        typeof(GridsUtility),
        nameof(GridsUtility.UsesOutdoorTemperature),
        new[] { typeof(IntVec3), typeof(Map) });

    static readonly MethodInfo GetRoof = AccessTools.Method(
        typeof(GridsUtility),
        nameof(GridsUtility.GetRoof),
        new[] { typeof(IntVec3), typeof(Map) });

    static readonly MethodInfo OpenTemperature = AccessTools.Method(
        typeof(GardenRoof),
        nameof(GardenRoof.UsesOutdoorTemperature));

    static readonly MethodInfo OpenRoof = AccessTools.Method(
        typeof(GardenRoof),
        nameof(GardenRoof.GetRoof));

    static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(Need_Outdoors), nameof(Need_Outdoors.NeedInterval));
    }

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        int replaced = 0;
        foreach (CodeInstruction instruction in instructions)
        {
            if (UsesOutdoorTemperature != null && instruction.Calls(UsesOutdoorTemperature))
            {
                instruction.opcode = OpCodes.Call;
                instruction.operand = OpenTemperature;
                replaced++;
            }
            else if (GetRoof != null && instruction.Calls(GetRoof))
            {
                instruction.opcode = OpCodes.Call;
                instruction.operand = OpenRoof;
                replaced++;
            }

            yield return instruction;
        }

        if (replaced == 0)
            Log.Warning("[OpenSkylights] Need_Outdoors had no roof query to retarget.");
    }
}
