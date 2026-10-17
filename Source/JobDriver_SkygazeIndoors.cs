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

public class JobDriver_SkygazeIndoors : JobDriver
{
    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return true;
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOn(() => IndoorGardenUtility.IsRaining(pawn.Map));
        yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);

        Toil toil = new Toil();
        toil.initAction = () =>
        {
            pawn.jobs.posture = PawnPosture.LayingOnGroundFaceUp;
        };
        toil.tickAction = () =>
        {
#if RIMWORLD16 || RIMWORLD17
            JoyUtility.JoyTickCheckEnd(pawn, 1, JoyTickFullJoyAction.EndJob);
#else
            JoyUtility.JoyTickCheckEnd(pawn, JoyTickFullJoyAction.EndJob);
#endif
        };
        toil.defaultCompleteMode = ToilCompleteMode.Delay;
        toil.defaultDuration = job.def.joyDuration;
        toil.FailOn(() => !IndoorGardenUtility.IsInGarden(pawn));
        yield return toil;
    }
}
