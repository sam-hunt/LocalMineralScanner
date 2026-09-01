using HarmonyLib;
using Verse;

namespace LocalMineralScanner;

[StaticConstructorOnStartup]
public static class ModInit
{
    static ModInit()
    {
        // The Harmony id only needs to be unique; convention is the mod's packageId.
        new Harmony("samhunt.localmineralscanner").PatchAll();
        Log.Message("[LocalMineralScanner] Mod loaded.");
    }
}
