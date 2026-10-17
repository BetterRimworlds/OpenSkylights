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

using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace BetterRimworlds.OpenSkylights;

public class OpenSkylights : Mod
{
    public OpenSkylights(ModContentPack content) : base(content)
    {
        var harmony = new Harmony("BetterRimworlds.OpenSkylights");

        foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
        {
            try
            {
                if (!type.IsDefined(typeof(HarmonyPatch), inherit: true))
                    continue;

                harmony.CreateClassProcessor(type).Patch();
            }
            catch (Exception ex)
            {
                Log.Error(
                    $"[OpenSkylights] Harmony patch failed on {type.FullName}: {ex}");
            }
        }
    }
}
