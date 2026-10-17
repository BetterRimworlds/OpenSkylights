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

    // One cell past the lit ring. The lighting overlay darkens a corner when
    // any cell touching it is roofed, which otherwise leaves a shadow inside
    // the lit ring. Gameplay light still stops at lit.
    readonly bool[] openToSky;

    public MapComp_Skylights(Map map) : base(map)
    {
        int cells = map.cellIndices.NumGridCells;
        lit = new bool[cells];
        openToSky = new bool[cells];
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

    public bool IsOpenToSky(int index)
    {
        return (uint)index < (uint)openToSky.Length && openToSky[index];
    }

    // Footprint plus the one-cell ring that MarkFootprint records as lit.
    public static CellRect LightRect(IntVec3 loc, Rot4 rot, IntVec2 size)
    {
        return GenAdj.OccupiedRect(loc, rot, size).ExpandedBy(1);
    }

    public override void ExposeData()
    {
        base.ExposeData();

        // The old Dubs_Skylight.MapComp_Skylights node has no matching class
        // and is dropped by the loader. The grid is rebuilt from spawned
        // skylights in FinalizeInit, which runs after Map.FinalizeLoading.
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
            if (!lit[i] && !openToSky[i])
                continue;

            lit[i] = false;
            openToSky[i] = false;
            DirtyGlow(map.cellIndices.IndexToCell(i));
        }
    }

    void MarkFootprint(Building_Skylight skylight)
    {
        CellRect light = LightRect(skylight.Position, skylight.Rotation, skylight.def.size);
        foreach (IntVec3 cell in light)
            MarkCell(cell);

        foreach (IntVec3 cell in light.ExpandedBy(1))
            MarkOpen(cell);
    }

    void MarkCell(IntVec3 cell)
    {
        if (!cell.InBounds(map))
            return;

        int index = map.cellIndices.CellToIndex(cell);
        openToSky[index] = true;
        if (lit[index])
            return;

        lit[index] = true;
        DirtyGlow(cell);
    }

    void MarkOpen(IntVec3 cell)
    {
        if (!cell.InBounds(map))
            return;

        int index = map.cellIndices.CellToIndex(cell);
        if (openToSky[index])
            return;

        openToSky[index] = true;
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
