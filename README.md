# Local Mineral Scanner

Never strip-mine again. A RimWorld 1.6 mod adding a compact **local mineral scanner**: a
pawn-operated 2×2 building that locates and reveals mineral deposits hidden in fogged parts
of the current map.

## Features

- **Reveals real ore.** Each successful scan unfogs one contiguous deposit of the targeted
  mineral that map generation actually placed — nothing is conjured. Fully hidden deposits
  are found first; partially exposed ones are the fallback.
- **Tunable target.** Like the long-range mineral scanner, it can be tuned to a specific
  mineral (gold, silver, steel, plasteel, components, uranium, jade). A newly built scanner
  starts on gold, or on the most valuable mineral still hidden on the map if no gold is.
- **Two operators.** Twin consoles let two pawns scan simultaneously, each contributing
  their full research speed — a genuine second seat, not queueing.
- **Half the effort per find** of the vanilla long-range mineral scanner, in exchange for
  results limited to the current map.
- **Knows when it's done.** When no undiscovered deposits of the tuned mineral remain, the
  scanner pauses (progress retained) instead of wasting pawn labor. The find that reveals the
  last deposit says so, the inspect pane shows why the scanner is idle, and the tuning menu
  greys out exhausted minerals.
- **Minifiable**, can be re-deployed on new maps for mining trips etc as needed.
- Unlocked by the vanilla **long-range mineral scanner** research; no new research project.

No dependencies: no Harmony, no DLC.

## Building from source

```bash
dotnet build LocalMineralScanner.sln -c Release
```

The project auto-detects a RimWorld installation on common Steam paths (Windows, Linux,
macOS, and WSL targeting a Windows install). When one is found, every Release build
automatically stages the mod into the game's `Mods/` folder — no manual copying. Debug
builds never deploy. If your installation is elsewhere:

```bash
RIMWORLD_PATH="/path/to/RimWorld" dotnet build LocalMineralScanner.sln -c Release
# or: dotnet build LocalMineralScanner.sln -p:RimWorldPath="/path/to/RimWorld"
```

| Platform | Default path                                                    |
| -------- | --------------------------------------------------------------- |
| Windows  | `C:\Program Files (x86)\Steam\steamapps\common\RimWorld`        |
| Linux    | `~/.local/share/Steam/steamapps/common/RimWorld`                |
| macOS    | `~/Library/Application Support/Steam/steamapps/common/RimWorld` |

## Project structure

```
About/              - Mod metadata (About.xml)
Textures/           - Version-independent art
Languages/          - Version-independent translations
1.6/                - RimWorld 1.6 specific content
  Assemblies/       - Compiled DLLs (build output)
  Defs/             - XML definitions (building, work giver, job)
  Patches/          - XML patches to modify base game/other mods
Source/1.6/         - C# source code
LoadFolders.xml     - Tells RimWorld which folders to load per game version
docs/               - Design research against the decompiled 1.6 API
```

## Releases

Add the version's section to `CHANGELOG.md`, bump `<modVersion>` in `About/About.xml` and the
versions in `Source/1.6/Properties/AssemblyInfo.cs`, then push a `v*.*.*` tag. GitHub Actions
builds, packages, and creates the release, using that CHANGELOG section as the release body
(and failing if it's missing). The `/release` Claude Code skill automates the whole flow.

## Requirements

- .NET SDK (for building)
- RimWorld 1.6 (for assembly references)
