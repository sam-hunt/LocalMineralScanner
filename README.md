# RimWorld Mod Template

A ready-to-use template for creating RimWorld 1.6 mods with C# code.

## Quick Start

1. **Use this template** - Click "Use this template" on GitHub or clone/download
2. **Rename your mod** - See [Customization](#customization) below
3. **Set up RimWorld path** - See [Build Setup](#build-setup)
4. **Build** - Run `dotnet build MyRimWorldMod.sln -c Release`

When a local RimWorld install is detected, every Release build automatically stages the mod
into the game's `Mods/` folder — no manual copying. Debug builds never deploy.

## Build Setup

The project auto-detects your RimWorld installation on common Steam paths. If your installation is elsewhere, set the `RIMWORLD_PATH` environment variable:

**Windows (PowerShell):**
```powershell
$env:RIMWORLD_PATH = "D:\Games\RimWorld"
dotnet build MyRimWorldMod.sln
```

**Linux/macOS:**
```bash
export RIMWORLD_PATH="$HOME/Games/RimWorld"
dotnet build MyRimWorldMod.sln
```

Or pass it directly:
```bash
dotnet build MyRimWorldMod.sln -p:RimWorldPath="/path/to/RimWorld"
```

### Default Paths

| Platform | Default Path |
|----------|--------------|
| Windows | `C:\Program Files (x86)\Steam\steamapps\common\RimWorld` |
| Linux | `~/.local/share/Steam/steamapps/common/RimWorld` |
| macOS | `~/Library/Application Support/Steam/steamapps/common/RimWorld` |

## Customization

When creating a new mod from this template, rename these files and update their contents:

| File | What to Change |
|------|----------------|
| `About/About.xml` | `<name>`, `<author>`, `<packageId>`, `<modVersion>`, `<description>` |
| `Source/1.6/ModInit.cs` | Namespace, Harmony id, log message |
| `Source/1.6/Properties/AssemblyInfo.cs` | Title/product/description, fresh GUID, version |
| `MyRimWorldMod.sln` | Rename file, update project name inside |
| `Source/1.6/MyRimWorldMod.csproj` | Rename file |
| `.github/workflows/release.yml` | The `.sln`/`.csproj` names in the Build and Stage steps |
| `CHANGELOG.md` | The release-tag link's repository URL |
| `.claude/skills/` | Mod name in `release` and `rimworld-logs` |
| `.vscode/settings.json` | `dotnet.defaultSolution` |

### Package ID Format

Use the format `authorname.modname`, all lowercase (e.g., `johndoe.coolmod`) — the game's
`MayRequire`/mod-list matching is case-sensitive-lowercase. It must be unique across all
RimWorld mods.

## Project Structure

```
About/              - Mod metadata (About.xml)
Common/             - Version-independent assets (Languages, Textures)
1.6/                - RimWorld 1.6 specific content
  Assemblies/       - Compiled DLLs (build output)
  Defs/             - XML definitions (ThingDefs, etc.)
  Patches/          - XML patches to modify base game/other mods
Source/1.6/         - C# source code
LoadFolders.xml     - Tells RimWorld which folders to load per game version
```

## Adding Dependencies

To require a DLC or another mod, add to `About/About.xml`:

```xml
<modDependencies>
    <li>
        <packageId>ludeon.rimworld.biotech</packageId>
        <displayName>Biotech</displayName>
    </li>
</modDependencies>
<loadAfter>
    <li>ludeon.rimworld.biotech</li>
</loadAfter>
```

The template already declares the [Harmony](https://github.com/pardeike/HarmonyRimWorld) mod
(`brrainz.harmony`) as a dependency, since the C# side references `Lib.Harmony`.

Common DLC package IDs:
- `ludeon.rimworld.royalty`
- `ludeon.rimworld.ideology`
- `ludeon.rimworld.biotech`
- `ludeon.rimworld.anomaly`
- `ludeon.rimworld.odyssey`

## Releases

Add the version's section to `CHANGELOG.md`, bump `<modVersion>` in `About/About.xml` and the
versions in `Source/1.6/Properties/AssemblyInfo.cs`, then push a `v*.*.*` tag. GitHub Actions
builds, packages, and creates the release, using that CHANGELOG section as the release body
(and failing if it's missing). The `/release` Claude Code skill automates the whole flow.

## Requirements

- .NET SDK (for building)
- RimWorld 1.6 (for assembly references)

## License

[Choose a license for your mod]
