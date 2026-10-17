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

public static class IndoorGardenUtility
{
    public const int MinCells = 100;

    public const int MinBeautyPlants = 10;

    public static bool IsIndoorGarden(Room room)
    {
        return room?.Role != null && room.Role.defName == "IndoorGarden";
    }

    public static bool MeetsBars(Room room, out int beautyPlants)
    {
        beautyPlants = 0;
        if (room == null || !IsEnclosed(room) || room.CellCount < MinCells)
            return false;

        Map map = room.Map;
        int cells = 0;
        int lit = 0;
        foreach (IntVec3 cell in room.Cells)
        {
            cells++;
            if (MapComp_Skylights.Lit(map, cell))
                lit++;

            Plant plant = cell.GetPlant(map);
            if (IsBeautyPlant(plant))
                beautyPlants++;
        }

        if (cells < MinCells || lit * 2 <= cells || beautyPlants < MinBeautyPlants)
        {
            beautyPlants = 0;
            return false;
        }

        return true;
    }

    public static bool IsBeautyPlant(Plant plant)
    {
        return plant != null
            && plant.Spawned
            && !plant.Destroyed
            && plant.Growth > 0f
            && plant.GetStatValue(StatDefOf.Beauty) > 0f;
    }

    static bool IsEnclosed(Room room)
    {
#if RIMWORLD12
        return !room.TouchesMapEdge && !room.PsychologicallyOutdoors && room.RegionCount <= 60;
#else
        return room.ProperRoom && room.RegionCount <= 60;
#endif
    }
}
