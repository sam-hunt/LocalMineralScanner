# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

RimWorld mod template for version 1.6. Use this as a starting point for new mods.

**Where documentation lives:** this file holds only cross-cutting rules and rationale. Per-item
values and decompile-verified call paths belong in the header comment of the file they describe.
When adding or changing something, put the *why* there and only add a line here if it constrains
work in other files.

## Build Commands

```bash
# Build (outputs to 1.6/Assemblies/ AND atomically redeploys to the RimWorld Mods folder)
dotnet build LocalMineralScanner.sln -c Release

# Stage the mod into an arbitrary folder (used by CI; same manifest as the local deploy)
dotnet build Source/1.6/LocalMineralScanner.csproj -c Release \
  -t:StageMod -p:StageDir=/path/to/output/LocalMineralScanner

# Override RimWorld install path
RIMWORLD_PATH="/path/to/RimWorld" dotnet build LocalMineralScanner.sln -c Release
# Or: dotnet build -p:RimWorldPath="/path/to/RimWorld"
```

The build auto-detects the RimWorld install (Windows/Linux/Mac, including WSL targeting a Windows
install), falling back to the `Krafs.Rimworld.Ref` NuGet package in CI. Debug builds go to the
default `bin/` and never deploy — only Release builds touch `1.6/Assemblies/` and the Mods folder.

**WSL setup:** `RIMWORLD_PATH` in `~/.bashrc` pointing at the Windows install, e.g.
`/mnt/c/Program Files (x86)/Steam/steamapps/common/RimWorld`.

### Deployment

The repo lives outside the Mods folder; every local Release build redeploys automatically and
atomically.

- **One manifest, one place:** the `_ModFiles` ItemGroup in the `StageMod` target of
  `Source/1.6/LocalMineralScanner.csproj` — see that target's comments for how it globs and what it
  excludes. It is generic over folders, so a new `1.7/` or `Sounds/` needs no build change; only a
  brand-new *file type* does. Local deploy and CI release both call it, so they can't drift.
- **Optional Stop hook:** sibling mods run a local-only `.claude/hooks/sync-mod.sh` (gitignored)
  that rebuilds+redeploys after a Claude turn when mod-relevant files changed, wired via a `Stop`
  hook in `.claude/settings.local.json`. Copy both from a sibling mod (e.g. UniqueMeleeWeapons) if
  wanted.

**`.claude/` is only partly gitignored.** `.gitignore` carries `.claude/*` followed by
`!.claude/skills/`, so the skills are tracked and shared while hooks and settings are local
per-machine. Editing a skill is a committed, team-visible change and must keep in step with
whatever it automates (e.g. `/release` encodes the CHANGELOG layout).

## Project Structure

```
About/           - Mod metadata (About.xml)
Textures/        - Version-independent art (well-known folder at the mod root)
Languages/       - Version-independent translations (well-known folder at the mod root)
1.6/             - RimWorld 1.6 specific content
  Assemblies/    - Compiled DLLs (build output, gitignored)
  Defs/          - XML definitions (ThingDefs, etc.)
  Patches/       - XML patches to modify base game/other mods
Source/1.6/      - C# source code targeting net472
LoadFolders.xml  - Tells RimWorld which folders to load per game version
CHANGELOG.md     - Keep a Changelog format; load-bearing for releases (see below)
```

## RimWorld Modding Context

- Target framework: .NET Framework 4.7.2
- References RimWorld assemblies via cross-platform paths in .csproj
- Uses `Verse` namespace for core modding APIs
- `[StaticConstructorOnStartup]` attribute triggers code at game startup
- Harmony is referenced (`Lib.Harmony`, compile-only) and bootstrapped in `ModInit.cs`; the
  runtime DLL comes from the `brrainz.harmony` mod dependency declared in About.xml
- XML Defs define game objects; Patches modify existing Defs via XPath

**Releases:** run the `/release` skill, or by hand: add the version's `## [X.Y.Z]` section to
`CHANGELOG.md`, bump `About/About.xml` `<modVersion>` and `Source/1.6/Properties/AssemblyInfo.cs`,
then push a `v*.*.*` tag. The GitHub Actions workflow (`.github/workflows/release.yml`) builds,
stages via `StageMod`, lifts the tag's CHANGELOG section into the release body — and **fails the
release if that section is missing**.

## Debugging

1. **Dev Mode:** Settings > Dev Mode > Logging.
2. **Log:** `%USERPROFILE%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Player.log`
   (WSL: `/mnt/c/Users/*/AppData/LocalLow/Ludeon Studios/RimWorld by Ludeon Studios/Player.log`;
   Linux: `~/.config/unity3d/Ludeon Studios/RimWorld by Ludeon Studios/Player.log`).
3. **Logging convention:** `Log.Message("[LocalMineralScanner] ...")` — grep the prefix to isolate our
   output.
4. **Inspect the API:** `monodis` for signatures, `ilspycmd -t "Namespace.ClassName"` for method
   bodies, both against the local install's `Assembly-CSharp.dll` (source of truth over the
   Krafs ref package). The `rimworld-logs` skill covers both.

## Localization and Optional-Content Gating

- `MayRequire`/`MayRequireAnyOf` work on def root nodes and list items, but the DefInjected loader ignores XML attributes entirely. A DefInjected entry for a gated def placed in the main tree loads unconditionally and logs a "found no def named ..." startup error whenever the gating mod/DLC is absent.
- The fix is a folder gate: ship the gated content from a compat load root, loaded via an `IfModActive` entry in `LoadFolders.xml` (commented example there). Two flavours, mirroring the ungated roots: `1.6/Mods/<Mod Name>/` for version-specific content (Defs, and the DefInjected targeting them) and root-level `Mods/<Mod Name>/` for version-independent content (art). The def's `MayRequire` becomes redundant and should be dropped when it moves.
- Compat roots must sit BESIDE the well-known folders, never inside them: anything under `1.6/Defs/**` or `1.6/Languages/**` loads unconditionally at any depth.
- The game gates a DefInjected entry by the load root that CONTAINS it, never by the def it targets. The reverse mistake also bites: a main-tree def's translation placed in a compat root silently vanishes when the gate is closed.
- A compat root's language files must never reuse a main-tree file's language-relative path (`DefInjected/<Type>/<File>.xml`, `Keyed/<File>.xml`): the game dedups language files per mod by that path and silently skips one whole file, in an enumeration order that is not LoadFolders order. Suffix compat-root filenames with the gate's name (`WeaponTraits_Royalty.xml`).
- If the mod grows translations, adopt the ecosystem tooling: the shared `rimworld-l10n` toolkit is consumed as a git submodule at `l10n/` (`git submodule add ../rimworld-l10n.git l10n`, pinned to a release tag), with thin per-repo config shims in `Scripts/` (`check-translations.py`, `refresh-translation-expectations.py`, `integration-smoke-test.py` — copy the `SHIM_TEMPLATE.py` files from the toolkit's `checker/`, `refresh/`, `smoke/` dirs), an `expected-injections.json` sidecar generated by the toolkit's L10nProbe dev mod, and the `translate` skill (copy the shape from BetterTradersGuild). The checker validates all of the above bullets mechanically, including English DefInjected files, and gets wired into the release workflow. The `.steamworkshop/Description/<Language>.txt` convention (Workshop title + BBCode description per language) also comes with it.

## Customization Checklist

When creating a new mod from this template, update:
1. `About/About.xml` - mod name, author, packageId (keep it lowercase), modVersion, description
2. `Source/1.6/ModInit.cs` - namespace, Harmony id (= packageId), log message
3. `Source/1.6/Properties/AssemblyInfo.cs` - title/description/product, a freshly generated GUID, version
4. `MyRimWorldMod.sln` - rename file and update project name
5. `Source/1.6/MyRimWorldMod.csproj` - rename file (deploy folder name follows the project name)
6. `MyRimWorldMod.sln` / `.github/workflows/release.yml` - the workflow builds the .sln and csproj by name; update both references
7. `CHANGELOG.md` - update the release-tag link's repo URL
8. `.claude/skills/` - swap the `MyRimWorldMod` name/log-prefix in `release` and `rimworld-logs`
9. `.vscode/settings.json` - `dotnet.defaultSolution`
