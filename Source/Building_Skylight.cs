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

using RimWorld;
using Verse;

namespace BetterRimworlds.OpenSkylights;

public class Building_Skylight : Building
{
    public static bool IsSkylight(Thing thing)
    {
        if (thing is Building_Skylight)
            return true;

        return thing?.def?.entityDefToBuild is ThingDef built
            && built.thingClass == typeof(Building_Skylight);
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        map.GetComponent<MapComp_Skylights>()?.Register(this);
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        Map current = Map;
        base.DeSpawn(mode);
        current?.GetComponent<MapComp_Skylights>()?.Deregister(this);
    }
}
