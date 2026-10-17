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
using Verse.Sound;

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

    public override void ExposeData()
    {
        base.ExposeData();

        // Dubs' Building_skyLight wrote its own fields into the same node.
        // They are not part of our save contract, and RimWorld ignores unknown
        // child nodes. Only the base Thing state and the def name carry over.
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

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        IntVec3 pos = Position;
        Map current = Map;
        bool spawned = Spawned;
        base.Destroy(mode);
        if (spawned && Smashed(mode) && current != null)
            PlaySmashed(pos, current);
    }

    public override void TickRare()
    {
        base.TickRare();
        if (!Spawned || OpenSkylights.Settings == null || !OpenSkylights.Settings.sunlightHeatsRoom)
            return;

        float glow = Map.skyManager.CurSkyGlow;
        if (glow <= 0.01f)
            return;

        if (Map.mapTemperature.OutdoorTemp >= MapComp_Skylights.HotOutdoors)
            return;

        int cells = def.size.x * def.size.z;
        GenTemperature.PushHeat(Position, Map, MapComp_Skylights.HeatPerFootprintCell * cells * glow);
    }

    public override void Print(SectionLayer layer)
    {
        if (OpenSkylights.Settings != null && OpenSkylights.Settings.hideWindows)
            return;

        base.Print(layer);
    }

    static bool Smashed(DestroyMode mode)
    {
        if (mode == DestroyMode.KillFinalize)
            return true;

#if !RIMWORLD12 && !RIMWORLD13
        if (mode == DestroyMode.KillFinalizeLeavingsOnly)
            return true;
#endif
        return false;
    }

    static void PlaySmashed(IntVec3 pos, Map map)
    {
        SoundDef sound = DefDatabase<SoundDef>.GetNamed("SmashedSkylight", false);
        if (sound == null)
            return;

        sound.PlayOneShot(SoundInfo.InMap(new TargetInfo(pos, map), MaintenanceType.None));
    }
}
