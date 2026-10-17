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
using Verse.AI;

namespace BetterRimworlds.OpenSkylights;

public class JoyGiver_GoForWalkIndoors : JoyGiver
{
    public override Job TryGiveJob(Pawn pawn)
    {
        if (pawn == null || !pawn.Spawned)
            return null;

        Room room = pawn.GetRoom();
        if (!IndoorGardenUtility.IsIndoorGarden(room))
            return null;

        if (IndoorGardenUtility.IsRaining(pawn.Map))
            return null;

        if (IndoorGardenUtility.WantsToBeOutside(pawn))
            return null;

        if (PawnUtility.WillSoonHaveBasicNeed(pawn))
            return null;

        if (!IndoorGardenUtility.TryFindStandableCell(pawn, room, out IntVec3 root))
            return null;

        if (!IndoorGardenUtility.TryFindWalkPath(pawn, room, root, out List<IntVec3> path))
            return null;

        Job job = JobMaker.MakeJob(def.jobDef, path[0]);
        job.targetQueueA = new List<LocalTargetInfo>();
        for (int i = 1; i < path.Count; i++)
            job.targetQueueA.Add(path[i]);

        job.locomotionUrgency = LocomotionUrgency.Walk;
        return job;
    }
}
