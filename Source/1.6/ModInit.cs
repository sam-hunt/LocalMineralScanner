using Verse;

namespace LocalMineralScanner;

// Runs once after all defs are loaded, resolved and translated: the earliest point where
// settings that override def fields can be applied (the Mod constructor is too early, it
// runs before any def exists). The mod has no Harmony patches (Verse.MapEvents exposes the
// hooks it needs, see Docs/design-research.md), so this is all the startup work there is.
//
// Once per PROCESS, not per play-data load: a type initializer never runs twice, so an
// in-process reload (a mid-session language change) rebuilds the DefDatabase from XML
// without re-running this, and the overrides revert to their XML defaults until the
// settings window is next closed (LocalMineralScannerMod.WriteSettings applies them again).
// The Harmony-free price of settings-as-def-writes; accepted.
[StaticConstructorOnStartup]
public static class ModInit
{
    static ModInit()
    {
        LocalMineralScannerMod.Settings.Apply();
        Log.Message("[LocalMineralScanner] Mod loaded.");
    }
}
