using Verse;

namespace LocalMineralScanner;

// Startup marker only: the mod has no Harmony patches (Verse.MapEvents exposes the
// hooks it needs, see Docs/design-research.md), so there is nothing to bootstrap.
// The log line lets Player.log confirm the assembly loaded.
[StaticConstructorOnStartup]
public static class ModInit
{
    static ModInit()
    {
        Log.Message("[LocalMineralScanner] Mod loaded.");
    }
}
