# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

RimWorld 1.6 mod: a pawn-operated 2x2 building that reveals (unfogs) mineral deposits on the
current map. Player-facing summary in `README.md`; decompile-verified design rationale and
vanilla precedent in `Docs/`.

**Where documentation lives:** this file holds only cross-cutting rules and rationale. Per-item
values and decompile-verified call paths belong in the header comment of the file they describe,
with one exception: shipped XML (Defs, Patches, Languages) is downloaded by every player, so it
carries only a terse pointer, and its rationale lives in `Docs/design-research.md` under a
section named for the file. When adding or changing something, put the *why* there and only add
a line here if it constrains work in other files.

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
1.6/             - RimWorld 1.6 specific content
  Assemblies/    - Compiled DLLs (build output, gitignored)
  Defs/          - XML definitions (building, work giver, job)
  Languages/     - Keyed + DefInjected translations, one folder per language
Source/1.6/      - C# source code targeting net472
Scripts/         - l10n config shims + the expected-injections.json sidecar (not shipped)
l10n/            - rimworld-l10n toolkit, git submodule pinned to a release tag (not shipped)
.steamworkshop/  - Workshop title + BBCode description per language (not shipped)
Docs/            - Design research against the decompiled 1.6 API (not shipped)
LoadFolders.xml  - Tells RimWorld which folders to load per game version
CHANGELOG.md     - Keep a Changelog format; load-bearing for releases (see below)
```

## RimWorld Modding Context

- Target framework: .NET Framework 4.7.2
- References RimWorld assemblies via cross-platform paths in .csproj
- Uses `Verse` namespace for core modding APIs
- `[StaticConstructorOnStartup]` attribute triggers code at game startup
- No Harmony. The mod needs no runtime patches (`Verse.MapEvents` exposes the hooks it uses;
  see `Docs/design-research.md`), so the template's `Lib.Harmony` reference, `PatchAll()`
  bootstrap, and `brrainz.harmony` dependency were removed. If a patch ever becomes necessary,
  restore all three from the template or a sibling mod (e.g. UniqueMeleeWeapons).
- XML Defs define game objects; Patches modify existing Defs via XPath
- Mod settings are def-field writes, not patches: `LocalMineralScannerSettings.Apply()` writes
  the settings onto the live def at startup (`ModInit`) and on settings-window close
  (`WriteSettings`). Only settings whose def field the game reads live belong there; the
  settings header records the decompile evidence per field. Consequences: the XML values are
  defaults that must equal the `*Default` consts, and every setting appears in three places
  with the same default (field initializer, `ExposeData`, `ResetToDefaults`). A rule vanilla
  hardcodes (the roof check) is made optional by a subclass that consults the setting, not
  by a def write.

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
5. **Startup smoke test (pre-release):** `python3 Scripts/integration-smoke-test.py` (game
   closed) boots the mod on a pinned Core-only list, then classifies logged errors by origin and
   fails on anything attributed to this mod. Wired into the release skill; thin shim over the
   shared engine in `l10n/smoke/`.

## Localization and Optional-Content Gating

- `MayRequire`/`MayRequireAnyOf` work on def root nodes and list items, but the DefInjected loader ignores XML attributes entirely. A DefInjected entry for a gated def placed in the main tree loads unconditionally and logs a "found no def named ..." startup error whenever the gating mod/DLC is absent.
- The fix is a folder gate: ship the gated content from a compat load root, loaded via an `IfModActive` entry in `LoadFolders.xml` (e.g. `<li IfModActive="ludeon.rimworld.biotech">1.6/Mods/Biotech</li>`; gating is by packageId, LoadFolders knows nothing about `MayRequire`). Keep `LoadFolders.xml` itself comment-free: it ships to players. Two flavours, mirroring the ungated roots: `1.6/Mods/<Mod Name>/` for version-specific content (Defs, and the DefInjected targeting them) and root-level `Mods/<Mod Name>/` for version-independent content (art). The def's `MayRequire` becomes redundant and should be dropped when it moves.
- Compat roots must sit BESIDE the well-known folders, never inside them: anything under `1.6/Defs/**` or `1.6/Languages/**` loads unconditionally at any depth.
- The game gates a DefInjected entry by the load root that CONTAINS it, never by the def it targets. The reverse mistake also bites: a main-tree def's translation placed in a compat root silently vanishes when the gate is closed.
- A compat root's language files must never reuse a main-tree file's language-relative path (`DefInjected/<Type>/<File>.xml`, `Keyed/<File>.xml`): the game dedups language files per mod by that path and silently skips one whole file, in an enumeration order that is not LoadFolders order. Suffix compat-root filenames with the gate's name (`WeaponTraits_Royalty.xml`).

## Localization Toolchain

English (the Keyed file + def fields) is the source of truth; other languages derive from it via
the `/translate` skill (`.claude/skills/translate/SKILL.md` — this mod's translation surface,
grounding domain, and per-language glossary; the family-wide process lives in the `l10n/`
submodule) and are validated deterministically by `python3 Scripts/check-translations.py` (also
a CI release gate). The DefInjected expected set is the checked-in sidecar
`Scripts/expected-injections.json`: a dump of every injection point the *live* game sees for this
mod, produced by `Scripts/refresh-translation-expectations.py` driving the L10nProbe dev mod
(source at `l10n/probe/`; build/deploy it only from the canonical `~/dev/rimworld-l10n` checkout,
and tick this mod in the probe's settings — it only dumps ticked mods) through the game's own
walker. The checker refuses to run against stale expectations, so new content forces a regen; the
release skill regenerates every release. The public language roster lives in CONTRIBUTING.md and
moves in the same commit as any language change.

- **Shared l10n toolkit (`l10n/` submodule):** the checker/refresh/smoke engines, per-language
  mechanics references, cross-language lessons, and Workshop conventions come from the
  `rimworld-l10n` repo, consumed as a git submodule pinned to a semver release tag (`git submodule
  status` names it; if `l10n/` is empty, run `git submodule update --init`). `Scripts/*.py` are
  thin per-repo config shims over its engines; each shim's comments carry this repo's rationale
  (Core-only mod list, no DLC, no compat roots). Never edit `l10n/` in place here: mod-independent
  learnings go upstream in the canonical checkout; mod-specific ones go in the skill's glossary.
  The pin moves only at release, at the start of a translation pass, or when a new major lands
  (`l10n/tools/bump-consumer.sh`), never per upstream commit.
- **Workshop title coupling:** each language's `LocalMineralScanner_SettingsCategory` Keyed value
  is the localized Workshop title and must equal line 1 of
  `.steamworkshop/Description/<Language>.txt` (checker-enforced). Change both together.
- **Token discipline:** the surface is a few dozen strings. A language pass greps the vanilla tar
  for the handful of grounded terms the skill lists; it never extracts or reads whole tars.
