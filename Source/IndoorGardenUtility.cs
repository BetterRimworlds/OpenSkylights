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
using UnityEngine;
using Verse;
using Verse.AI;

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

    public static bool IsInGarden(Pawn pawn)
    {
        if (pawn == null || !pawn.Spawned)
            return false;

        return IsIndoorGarden(pawn.GetRoom());
    }

    public static bool IsRaining(Map map)
    {
        if (map?.weatherManager?.curWeather == null)
            return false;

        if (map.weatherManager.curWeather.rainRate > 0.1f)
            return true;

#if RIMWORLD16 || RIMWORLD17
        if (map.weatherManager.curWeather.preventSkygaze)
            return true;
#endif

        return false;
    }

    public static bool IsComfortable(Pawn pawn, Room room)
    {
        return pawn != null
            && room != null
            && pawn.ComfortableTemperatureRange().Includes(room.Temperature);
    }

    public static bool WantsToBeOutside(Pawn pawn)
    {
        Need_Outdoors outdoors = pawn?.needs?.outdoors;
        return outdoors != null && outdoors.CurCategory != OutdoorsCategory.Free;
    }

    public static bool TryFindStandableCell(Pawn pawn, Room room, out IntVec3 cell)
    {
        List<IntVec3> candidates = new List<IntVec3>();
        foreach (IntVec3 candidate in room.Cells)
        {
            if (ValidWalkCell(pawn, candidate) && pawn.CanReach(candidate, PathEndMode.OnCell, Danger.Some))
                candidates.Add(candidate);
        }

        if (candidates.Count == 0)
        {
            cell = IntVec3.Invalid;
            return false;
        }

        cell = candidates.RandomElement();
        return true;
    }

    public static bool TryFindWalkPath(Pawn pawn, Room room, IntVec3 root, out List<IntVec3> path)
    {
        path = new List<IntVec3> { root };
        IntVec3 current = root;
        int startIndex = GenRadial.NumCellsInRadius(14f);
        int endIndex = GenRadial.NumCellsInRadius(2f);
        const int stride = 3;

        for (int i = 0; i < 8; i++)
        {
            IntVec3 best = IntVec3.Invalid;
            float bestScore = -1f;

            for (int j = startIndex; j > endIndex; j -= stride)
            {
                IntVec3 candidate = current + GenRadial.RadialPattern[j];
                if (!room.ContainsCell(candidate)
                    || !ValidWalkCell(pawn, candidate)
                    || !GenSight.LineOfSight(current, candidate, pawn.Map))
                {
                    continue;
                }

                float score = 10000f;
                for (int k = 0; k < path.Count; k++)
                    score += (path[k] - candidate).LengthManhattan;

                float fromRoot = (candidate - root).LengthManhattan;
                if (fromRoot > 40f)
                    score *= Mathf.InverseLerp(70f, 40f, fromRoot);

                if (path.Count >= 2)
                {
                    float previousAngle = (path[path.Count - 1] - path[path.Count - 2]).AngleFlat;
                    float candidateAngle = (candidate - current).AngleFlat;
                    float turn = candidateAngle > previousAngle
                        ? candidateAngle - previousAngle
                        : candidateAngle + 360f - previousAngle;
                    if (turn > 110f)
                        score *= 0.01f;
                }

                if (path.Count >= 4 && (current - root).LengthManhattan < (candidate - root).LengthManhattan)
                    score *= 1e-05f;

                if (score > bestScore)
                {
                    best = candidate;
                    bestScore = score;
                }
            }

            if (bestScore < 0f)
            {
                path = null;
                return false;
            }

            path.Add(best);
            current = best;
        }

        path.Add(root);
        return true;
    }

    static bool ValidWalkCell(Pawn pawn, IntVec3 cell)
    {
        Map map = pawn.Map;
        return cell.InBounds(map)
            && !cell.Fogged(map)
            && !cell.GetTerrain(map).avoidWander
            && cell.Standable(map)
            && !cell.IsForbidden(pawn)
            && !PawnUtility.KnownDangerAt(cell, map, pawn);
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
