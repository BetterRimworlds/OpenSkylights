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

public class JobDriver_GoForWalkIndoors : JobDriver
{
    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return true;
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOn(() => IndoorGardenUtility.IsRaining(pawn.Map));
        this.FailOn(() => !IndoorGardenUtility.IsInGarden(pawn));

        Toil goToil = Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);
        goToil.tickAction = () =>
        {
            if (Find.TickManager.TicksGame > startTick + job.def.joyDuration)
            {
                EndJobWith(JobCondition.Succeeded);
                return;
            }

#if RIMWORLD16 || RIMWORLD17
            JoyUtility.JoyTickCheckEnd(pawn, 1);
#else
            JoyUtility.JoyTickCheckEnd(pawn);
#endif
        };

        yield return goToil;

        Toil next = new Toil();
        next.initAction = () =>
        {
            if (job.targetQueueA.Count > 0)
            {
                LocalTargetInfo target = job.targetQueueA[0];
                job.targetQueueA.RemoveAt(0);
                job.targetA = target;
                JumpToToil(goToil);
            }
        };
        yield return next;
    }
}
