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

public class PlaceWorker_Skylight : PlaceWorker
{
    public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
    {
        ThingDef thingDef = checkingDef as ThingDef;
        if (thingDef == null)
            return true;

        foreach (IntVec3 cell in GenAdj.OccupiedRect(loc, rot, thingDef.size))
        {
            if (!cell.InBounds(map))
                return new AcceptanceReport("OutOfBounds".Translate());

            if (!cell.Walkable(map))
                return new AcceptanceReport("NotWithinRoomBounds".Translate());

            if (map.roofGrid.RoofAt(cell) != RoofDefOf.RoofConstructed)
                return new AcceptanceReport("NotUnderConstructedRoof".Translate());

            AcceptanceReport occupied = CellAlreadyHasSkylight(cell, map, thingToIgnore);
            if (!occupied.Accepted)
                return occupied;
        }

        return true;
    }

    static AcceptanceReport CellAlreadyHasSkylight(IntVec3 cell, Map map, Thing thingToIgnore)
    {
        List<Thing> things = cell.GetThingList(map);
        for (int i = 0; i < things.Count; i++)
        {
            Thing thing = things[i];
            if (thing == thingToIgnore || !Building_Skylight.IsSkylight(thing))
                continue;

            if (thing is Blueprint)
                return new AcceptanceReport("IdenticalBlueprintExists".Translate());

            return new AcceptanceReport("IdenticalThingExists".Translate());
        }

        return true;
    }
}
