using HarmonyLib;
using Verse;

namespace MyRimWorldMod;

[StaticConstructorOnStartup]
public static class ModInit
{
    static ModInit()
    {
        // The Harmony id only needs to be unique; convention is the mod's packageId.
        new Harmony("yourname.myrimworldmod").PatchAll();
        Log.Message("[MyRimWorldMod] Mod loaded.");
    }
}
