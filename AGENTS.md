# Project agent notes (BetterRimworlds.OpenSkylights)

Prefer these over generic RimWorld mod tutorials. CryoRegenesis is the reference
implementation this skeleton came from.

Do **not** invent a different layout. Agents routinely break bootstrapping by:

- Putting the `.csproj` at the repo root instead of `Source/`
- Using `netcoreapp` / `net6.0` / `net8.0` instead of `net48` for 1.2–1.6
- Building 1.7 as `net48`. RimWorld 1.7 is `net10.0` (`Release v1.7`, `RIMWORLD17`)
- Dropping the `Release v1.2` … `Release v1.7` configurations
- Calling `harmony.PatchAll(Assembly)` (one failed patch kills every other patch)
- Writing assemblies into `Assemblies/` at the mod root instead of `1.2/Assemblies`, …
- Forgetting Harmony as a workshop dependency when the C# project references 0Harmony
- Committing `obj/`, `bin/`, `*.dll`, or `*.zip`

## Layout (do not flatten)

```
OpenSkylights/
  Source/                  C# project and .sln (not the repo root)
  OpenSkylights/             RimWorld mod pack (About, Defs, Languages, …)
    About/About.xml
    Defs/
    Languages/English/Keyed/
    Patches/
    Textures/
  build.sh                 builds all RimWorld versions, then syncs into /rimworld/*/Mods
  release.sh               zips the 1.2 Mods copy
```

`build.sh` uses `basename $PWD` as the mod name. Run it from the **mod repo root**,
not from `Source/`.

Assemblies are compiled into `/rimworld/1.2/Mods/<identity>/<version>/Assemblies`,
then `build.sh` copies that whole mod folder to the other RimWorld installs. XML and
textures live unversioned under the inner pack folder in git (shared across versions).

## C# conventions

- Namespace: `BetterRimworlds.OpenSkylights`
- Mod class: `OpenSkylights : Mod` in `Source/OpenSkylights.cs`
- Settings: `Source/Settings.cs`, `GetSettings<Settings>()` in the Mod ctor
- Harmony id: `BetterRimworlds.OpenSkylights`
- packageId: `HopeSeekr.BetterRimworlds.OpenSkylights`
- Version symbols: `RIMWORLD12` … `RIMWORLD17` (see the csproj). 1.2–1.6 stay
  `net48`. 1.7 uses `net10.0` and references `/rimworld/1.7-steam/Assembly-CSharp.dll`
  plus `UnityStubs.dll`.
- Apply Harmony patches **per type** with `CreateClassProcessor(type).Patch()` in its
  own try/catch. Never one `PatchAll` for the assembly when any `TargetMethod` is
  version-sensitive. 1.2 method signatures often differ from 1.3+.

## Language

Never use “latch” as a verb (or latches / latched / latching). Prefer records,
remembers, keeps, sets once, marks, stores, remains set, one-time flag.

## Commits and changelog

- Match this repo’s subject style: short, past tense, optional `[Quests]` / `[m]` tag.
- `CHANGELOG.md` is updated by an external tool. Do not edit, stage, or commit it
  unless the maintainer asks.

## Git signing

This environment signs commits with GPG. If signing fails for lack of a TTY, leave
changes staged and ask the maintainer to unlock gpg-agent.
