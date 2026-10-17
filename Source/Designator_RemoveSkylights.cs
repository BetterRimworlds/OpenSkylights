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
using UnityEngine;
using Verse;

namespace BetterRimworlds.OpenSkylights;

public class Designator_RemoveSkylights : Designator
{
#if RIMWORLD16 || RIMWORLD17
    public override DrawStyleCategoryDef DrawStyleCategory => DrawStyleCategoryDefOf.Orders;
#else
    public override int DraggableDimensions => 2;
#endif

    public Designator_RemoveSkylights()
    {
        defaultLabel = "RemoveSkylights".Translate();
        defaultDesc = "DeconstructLights".Translate();
        icon = ContentFinder<Texture2D>.Get("UI/RemoveSkylights");
        useMouseIcon = true;
        soundDragSustain = SoundDefOf.Designate_DragStandard;
        soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
        soundSucceeded = SoundDefOf.Designate_Deconstruct;
    }

    public override AcceptanceReport CanDesignateCell(IntVec3 loc)
    {
        if (!loc.InBounds(Map))
            return false;

        if (!DebugSettings.godMode && loc.Fogged(Map))
            return false;

        Building_Skylight skylight = SkylightAt(loc);
        if (skylight == null)
            return new AcceptanceReport("MustBeSkylight".Translate());

        return CanDesignateThing(skylight);
    }

    public override void DesignateSingleCell(IntVec3 loc)
    {
        Building_Skylight skylight = SkylightAt(loc);
        if (skylight != null)
            DesignateThing(skylight);
    }

    public override AcceptanceReport CanDesignateThing(Thing t)
    {
        if (t is not Building_Skylight building)
            return new AcceptanceReport("MustBeSkylight".Translate());

#if RIMWORLD16 || RIMWORLD17
        if (!building.DeconstructibleBy(Faction.OfPlayer).Accepted)
            return false;
#else
        if (!building.DeconstructibleBy(Faction.OfPlayer))
            return false;
#endif

        if (Map.designationManager.DesignationOn(t, DesignationDefOf.Deconstruct) != null)
            return false;

        if (Map.designationManager.DesignationOn(t, DesignationDefOf.Uninstall) != null)
            return false;

        return true;
    }

    public override void DesignateThing(Thing t)
    {
        if (DebugSettings.godMode || t.def.IsFrame)
        {
            t.Destroy(DestroyMode.Deconstruct);
            return;
        }

        Map.designationManager.AddDesignation(new Designation(t, DesignationDefOf.Deconstruct));
    }

    public override void SelectedUpdate()
    {
        GenUI.RenderMouseoverBracket();
    }

    Building_Skylight SkylightAt(IntVec3 loc)
    {
        List<Thing> things = loc.GetThingList(Map);
        for (int i = 0; i < things.Count; i++)
        {
            if (things[i] is Building_Skylight skylight)
                return skylight;
        }

        return null;
    }
}
