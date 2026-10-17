/*
 * This file is part of BetterRimworlds.OpenSkylights, a Better Rimworlds Project.
 *
 * Copyright © 2026 Theodore R. Smith
 * Author: Theodore R. Smith <hopeseekr@gmail.com>
 *   GPG Fingerprint: D8EA 6E4D 5952 159D 7759  2BB4 EEB6 CE72 F441 EC41
 *   https://github.com/BetterRimworlds/BetterRimworlds.OpenSkylights
 *
 * This file is licensed under the MIT License.
 *
 * =============================================================================
 * HARMONY PATCH LOADING — DO NOT REGRESS
 * =============================================================================
 *
 * Never call harmony.PatchAll(Assembly) for the whole assembly when any
 * TargetMethod is version-sensitive. One failed patch aborts the rest.
 *
 * Apply each [HarmonyPatch] type with CreateClassProcessor(type).Patch()
 * inside its own try/catch (see ctor).
 *
 * When resolving vanilla methods by signature, probe every supported RimWorld
 * version (1.2 often differs). Prefer ordered AccessTools.Method fallbacks.
 *
 * If a float-menu / job feature "does nothing" on one game version only, check
 * the player log first for Harmony load failures before rewriting gameplay
 * conditions.
 *
 * After changing any TargetMethod / patch attribute, boot each target version
 * (or at least 1.2 and latest) and confirm no Harmony patch failed lines at
 * startup.
 * =============================================================================
 */

using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace BetterRimworlds.OpenSkylights;

public class OpenSkylights : Mod
{
    public static Settings Settings;

    public OpenSkylights(ModContentPack content) : base(content)
    {
        Settings = GetSettings<Settings>() ?? new Settings();
        Settings.ImportDubsOnce();

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

    public override void DoSettingsWindowContents(Rect inRect)
    {
        base.DoSettingsWindowContents(inRect);
        Settings.DoSettingsWindowContents(inRect);
    }

    public override string SettingsCategory()
    {
        return "OpenSkylights";
    }
}
