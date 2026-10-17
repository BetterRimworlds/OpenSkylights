# OpenSkylights

OpenSkylights is a clean-room, MIT-licensed replacement for Dubs Skylights.
The closed mod ships behavior only as a DLL, and that DLL fails on RimWorld 1.7.
This mod reimplements the player-facing features against the game API, in our
namespace, with our art.

The published Dubs package is the save-game contract and the feature list.
Read its def names, labels, and descriptions. Do not decompile
`Dubs Skylight.dll`, do not copy its C#, and do not treat recovered IL as a
spec. Behavior below is the behavior we are choosing so a colony plays the
same way.

## Save compatibility

Yes, for the placed objects. A colony built with Dubs Skylights loads under
OpenSkylights when the def names below exist and Dubs Skylights is turned off.
The save does record C# class names. RimWorld 1.7 resolves a missing class
through `ScribeExtractor.SaveableFromNode`: it asks
`BackCompatibility.GetBackCompatibleType`, then `GenTypes.GetTypeInAnyAssembly`.
When that returns null, a `Thing` falls back to the `thingClass` of the `def`
written in the same node. A `MapComponent` has no such fallback. The loader
tries the abstract `MapComponent` base, fails, and stores a null slot.

Stable names, present from 1.0 through the 1.7 package:

| Kind | defNames |
| --- | --- |
| Skylights | `SkyLightA`, `SkyLightD`, `SkyLightB`, `SkyLightC` |
| Material | `GlassPane` |
| Recipes | `SmeltGlass`, and `SmeltGlass4x` from 1.3 onward |
| Room and jobs | `IndoorGarden`, `SkygazeIndoors`, `GoForWalkIndoors` |
| Architect tab | `skylights` |
| Sound | `SmashedSkylight` |

`Window` and `Dubs_SkylightExtension` appear in the published files and are
not live content. `Window` has always been inside an XML comment. We do not
publish either name.

What the player has to do: disable Dubs Skylights, enable OpenSkylights, and
load the save. The two mods cannot be on together. Their def names would
collide.

What carries over:

- Placed skylights, stacked glass, and unfinished glass bills. Each saved
  skylight node says `Class="Dubs_Skylight.Building_skyLight"` and
  `<def>SkyLightA</def>` (or D, B, C). The missing class produces one error
  per building, then the loader uses our `thingClass` for that def. Base
  `Thing` fields (position, rotation, hit points, stuff) still load. Our
  `ExposeData` must ignore extra nodes the old class wrote.
- Blueprints and frames whose def is one of the four skylights.
- Pawns who are mid skygaze or mid indoor walk. Our driver may restart the
  toil. The job def name is what the save keeps.
- Rooms. The indoor-garden role is recomputed from the room. It is not frozen
  in the save.

What does not carry over by itself:

- The mod list entry `Dubwise.DubsSkylights`. RimWorld will say that mod is
  missing. That warning is expected. We keep package id
  `HopeSeekr.BetterRimworlds.OpenSkylights` so we do not impersonate the
  workshop listing or fight its updates.
- Mod settings. RimWorld stores them under the package id. On first run,
  import `HideWindows` and `SunlightHeatsRoom` from the Dubs settings file
  when our own file is still at defaults. After that, our file is the source
  of truth.
- The saved `Dubs_Skylight.MapComp_Skylights` node, including whatever grid
  it stored. Load order in `Map.ExposeData` is: build a fresh component list,
  replace it from the save, then `FillComponents`. `FillComponents` deletes
  null slots and constructs every loaded `MapComponent` subclass that is not
  already in the list, so our component appears empty. Buildings spawn later,
  in `Map.FinalizeLoading`, before `MapComponent.FinalizeInit`. The rebuild
  belongs in that `FinalizeInit`: walk spawned skylights and fill the grid
  from their footprints. `SpawnSetup` can register a skylight placed during
  play. We do not declare a type in the `Dubs_Skylight` namespace, and we do
  not read the old component's fields.
- Textures and the glass break clip. Saves do not depend on those bytes. We
  ship original art and our own sound.

Published numbers stay, because bills and existing buildings assume them.
`GlassPane` is manufactured, mass 3, stack 50. `SmeltGlass` spends 4 steel
and 1600 work for one pane. `SmeltGlass4x` spends 16 steel and 6400 work for
four. Both recipes require Crafting 5 and run at the electric smelter, fueled
smithy, and electric smithy. Each skylight has 80 HP, WorkToBuild 135, Beauty
5, and costs 1, 2, 3, or 2 glass for A, D, B, and C. Sizes are 1x1, 2x2, 3x3,
and 1x4. Joy durations stay 4000 and 8000.

## Features

A player who had Dubs Skylights should find the same colony tools:

- An architect tab, Skylights, with the four skylights and a remove tool.
- Skylights that sit in a constructed roof, let daylight through, and stay
  walkable. They are not edifices and they do not hold the roof.
- Placement rejected off a constructed roof, on impassable cells, and on a
  cell that already has a skylight. The messages stay
  `NotUnderConstructedRoof`, `NotWithinRoomBounds`, and the vanilla
  identical-thing messages.
- Glass panes crafted from steel, then spent as the skylight cost.
- Plants, including trees, grow on the lit cells under a skylight.
- A setting to hide the skylight frame, and a setting so sunlight heats the
  room while the sun is up.
- Removing a skylight with the remove tool designates it for deconstruction.
  Destroying one plays `SmashedSkylight`.
- A room of at least 100 cells that is lit by skylights and planted with
  beauty plants becomes an Indoor Garden.
- In that room, pawns who want the outdoors can skygaze and go for a walk.
  Skygaze requires sight. Neither job starts in the rain.

The hidden wall-window block in the published XML is unfinished and stays
out.

## Behavior we own

These rules are ours. They match the public description. They are allowed to
differ from the closed DLL where that DLL is undocumented or broken.

Light. A map component remembers every cell covered by a skylight footprint.
Those cells report full sky glow, capped by the current sky glow, when the
game asks how bright the ground is. Plants on those cells ignore the
constructed roof. One ring of cells around the footprint is included so a pawn
standing against the frame, and a plant in that ring, still count as lit.
Rebuild the grid when a skylight spawns, despawns, or the map finishes
loading.

Roof queries used by `Need_Outdoors` and by joy that refuses a roofed
building return "open sky" on lit cells inside an Indoor Garden. Other rooms
keep a normal roof. Patch each target type on its own, with a version
fallback. 1.2 signatures often differ from later ones. One failed patch must
not skip the rest. The mod constructor already does this. Keep that.

Heat. With Sunlight Heats Rooms on, each skylight pushes heat on its rare
tick while the sun is shining. Scale by footprint size and current sky glow.
Push nothing at night, and nothing when the outdoor temperature is already
hot. Tune the scale in play until a 12x12 room with two skylights warms in
daylight and does not cook the room.

Indoor Garden. The worker scores the room only when all of these hold: the
room is enclosed, it has at least 100 cells (the bar in the public mod
description), a majority of those cells are lit, and at least ten living
plants are there for beauty. Score equals the number of those beauty plants.
Other plants do not qualify the room. A room that fails any bar scores 0.
Change these bars in this plan before changing the code.

Skygaze. Offer it in an Indoor Garden that is comfortable and not forbidden,
on a standable cell that is allowed. Duration and joy kind come from the job
def. Rain cancels the offer.

Indoor walk. Offer it when the pawn does not currently want to be outside and
no basic need is about to empty. Pick a short loop of standable, allowed, safe
cells inside the garden, on the order of a vanilla outdoor walk, then return
to the start. Stay inside the room. Duration comes from the job def.

Hide Windows. When set, skylights draw no frame. Changing it redraws the map.

Removal. The designator accepts only our skylights, respects fog and
ownership the way the vanilla deconstruct tool does, and designates
deconstruction. Anything else reports `MustBeSkylight`.

## Code and versions

Follow `AGENTS.md`. Project file stays `Source/OpenSkylights.csproj`.
Namespace stays `BetterRimworlds.OpenSkylights`. Harmony id stays
`BetterRimworlds.OpenSkylights`. Shared XML and textures stay under
`OpenSkylights/`. Assemblies stay in `1.x/Assemblies` after `build.sh`.

RimWorld 1.2 through 1.4 expose `GlowGrid.GameGlowAt`. RimWorld 1.5, 1.6, and
the installed 1.7.4981 build expose `GlowGrid.GroundGlowAt` and have no
`GameGlowAt`. The 1.7 game assembly targets `.NETCoreApp,Version=v10.0` and
lives at `/rimworld/1.7-steam/Assembly-CSharp.dll` beside `UnityStubs.dll`.
1.2 through 1.6 stay `net48` with the existing `Release v1.2` … `Release v1.6`
configurations.

Add `Release v1.7` and `RIMWORLD17`. That configuration sets
`TargetFramework` to `net10.0` and references the 1.7 assembly, `UnityStubs.dll`,
and Harmony with `Private=false`. The workshop Harmony DLL on this machine is
still .NET Framework 4.7.2. Use it as a compile reference when the compiler
accepts it. If it does not, reference a net10 Harmony only at compile time and
still load the workshop mod at runtime. Do not ship a second Harmony.

`build.sh` gains `v1.7` and syncs the pack to `/rimworld/1.7-steam/Mods`.
`About.xml` lists 1.7. One def set is the goal. Split with `LoadFolders.xml`
only if a version rejects a node.

Glow, roof, and plant patches use `#if RIMWORLD12` / `RIMWORLD17` or an
`AccessTools` fallback chain. Probe the method that version actually has.

Original textures go under `OpenSkylights/Textures/` for the tab, the four
frames, the blueprint, the glass pane, and the remove icon. Original break
sound under `Sounds/`. Keyed English strings use the same keys as the
published UI so the messages above resolve. Translations can follow later.

## Work order

Each step builds, leaves the game able to boot, and keeps earlier steps working.

1. **1.7 build.** Add the `Release v1.7` configuration, `About.xml` version,
   and `build.sh` sync. Boot an empty 1.7 mod list entry.
2. **Defs.** Glass, both recipes, four skylights, architect tab, sound, room
   role, joy givers, and job defs, pointed at our classes. Stub every named
   class so XML resolves.
3. **Placement and grid.** Place worker, building spawn and despawn, light
   grid, rebuild on load.
4. **Light and plants.** Versioned glow patch and the plant-versus-roof rule.
   Confirm a day/night cell under `SkyLightA` and a planter on that cell.
5. **Removal, heat, hide frames.** Designator, both settings, one-time import
   of the two Dubs bools, redraw on hide.
6. **Indoor Garden.** Room worker with the bars in this plan.
7. **Outdoors need and joy.** Roof-query patches, skygaze, indoor walk.
8. **Save pass.** Load a Dubs colony on 1.6 and on 1.7 with Dubs disabled.
   Confirm the four skylight def names, glass bills, and a mid-job pawn. Record
   the expected missing-mod warning and the dropped map component.

## Playtest

Run these on 1.7 first, then once on 1.2 and once on 1.6. The glow method
differs across that span, so a 1.7-only pass is not enough.

- Build a sealed 12x12 constructed-roof room. Place A and D at least two cells
  from the wall. Lit cells brighten with the sky. A neighbor just outside the
  footprint is lit. A cell farther away is not.
- A rice plant and a tree grow on a lit cell. The same plant under bare
  constructed roof does not.
- Placement fails with the three messages above, in ordinary play, including
  a second skylight on the same cell.
- Hide Windows redraws without frames. Sunlight Heats Rooms warms at noon and
  is idle at night.
- The remove tool deconstructs a player skylight and rejects a wall.
- A 100-cell lit room with ten beauty plants becomes Indoor Garden. A smaller
  room, or a dark room, does not.
- A pawn skygazes and walks there in clear weather, and does neither in the
  rain. Their outdoors need moves while they are in the garden.
- Breaking a skylight plays the sound and the cell goes dark after the grid
  rebuilds.
- An old Dubs save opens with buildings and bills intact.

## Out of scope

Decompiling or byte-matching the closed DLL. Copying its textures or audio.
Taking package id `Dubwise.DubsSkylights`. Supporting 1.0 or 1.1. The
commented wall window. New skylight sizes. Bug-for-bug copies of whatever
makes the closed 1.7 DLL fail.
