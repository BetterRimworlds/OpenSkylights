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
using RimWorld;
using Verse;

namespace BetterRimworlds.OpenSkylights;

public class MapComp_Skylights : MapComponent
{
    // Rare tick is 250 ticks. A thin constructed roof leaks about
    // (outdoor - indoor) * 0.006 per 120 ticks, so 1 energy on a rare tick
    // settles near +80/roomCells degrees. 3.5 per footprint cell is about
    // +10C at noon in a 12x12 with SkyLightA and SkyLightD.
    public const float HeatPerFootprintCell = 3.5f;

    public const float HotOutdoors = 30f;

    readonly List<Building_Skylight> skylights = new List<Building_Skylight>();

    readonly bool[] lit;

    public MapComp_Skylights(Map map) : base(map)
    {
        lit = new bool[map.cellIndices.NumGridCells];
    }

    public static bool Lit(Map map, IntVec3 cell)
    {
        if (map == null)
            return false;

        return map.GetComponent<MapComp_Skylights>()?.IsLit(cell) ?? false;
    }

    public bool IsLit(IntVec3 cell)
    {
        if (!cell.InBounds(map))
            return false;

        int index = map.cellIndices.CellToIndex(cell);
        return (uint)index < (uint)lit.Length && lit[index];
    }

    public bool IsLit(int index)
    {
        return (uint)index < (uint)lit.Length && lit[index];
    }

    public override void FinalizeInit()
    {
        Rebuild();
    }

    public void Register(Building_Skylight skylight)
    {
        if (skylights.Contains(skylight))
            return;

        skylights.Add(skylight);
        MarkFootprint(skylight);
    }

    public void Deregister(Building_Skylight skylight)
    {
        if (!skylights.Remove(skylight))
            return;

        Rebuild();
    }

    public void DirtyFrames()
    {
        for (int i = 0; i < skylights.Count; i++)
        {
            if (skylights[i].Spawned)
                skylights[i].DirtyMapMesh(map);
        }
    }

    public static void DirtyAllFrames()
    {
        if (Current.ProgramState != ProgramState.Playing)
            return;

        foreach (Map map in Find.Maps)
            map.GetComponent<MapComp_Skylights>()?.DirtyFrames();
    }

    void Rebuild()
    {
        ClearLit();
        skylights.Clear();
        Collect(map.listerBuildings.allBuildingsColonist);
        Collect(map.listerBuildings.allBuildingsNonColonist);
        for (int i = 0; i < skylights.Count; i++)
            MarkFootprint(skylights[i]);
    }

    void Collect(List<Building> buildings)
    {
        for (int i = 0; i < buildings.Count; i++)
        {
            if (buildings[i] is Building_Skylight skylight && !skylight.Destroyed)
                skylights.Add(skylight);
        }
    }

    void ClearLit()
    {
        for (int i = 0; i < lit.Length; i++)
        {
            if (!lit[i])
                continue;

            lit[i] = false;
            DirtyGlow(map.cellIndices.IndexToCell(i));
        }
    }

    void MarkFootprint(Building_Skylight skylight)
    {
        foreach (IntVec3 cell in skylight.OccupiedRect())
        {
            MarkCell(cell);
            for (int i = 0; i < 8; i++)
                MarkCell(cell + GenAdj.AdjacentCells[i]);
        }
    }

    void MarkCell(IntVec3 cell)
    {
        if (!cell.InBounds(map))
            return;

        int index = map.cellIndices.CellToIndex(cell);
        if (lit[index])
            return;

        lit[index] = true;
        DirtyGlow(cell);
    }

    static void DirtyGlow(Map map, IntVec3 cell)
    {
        if (Current.ProgramState != ProgramState.Playing)
            return;

#if RIMWORLD12 || RIMWORLD13 || RIMWORLD14
        map.mapDrawer.MapMeshDirty(cell, MapMeshFlag.GroundGlow);
#else
        map.mapDrawer.MapMeshDirty(cell, MapMeshFlagDefOf.GroundGlow);
#endif
    }

    void DirtyGlow(IntVec3 cell)
    {
        DirtyGlow(map, cell);
    }
}
