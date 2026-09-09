using RimWorld;
using Verse;

namespace LocalMineralScanner;

[DefOf]
public static class LocalMineralScannerDefOf
{
    public static JobDef OperateLocalMineralScanner;

    public static ThingDef LocalMineralScanner;

    // Core defs with no vanilla ThingDefOf entry; the settings window quotes their labels and
    // scan timings so translators never retype vanilla's text or values.
    public static ThingDef LongRangeMineralScanner;
    public static ThingDef GroundPenetratingScanner;

    static LocalMineralScannerDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(LocalMineralScannerDefOf));
}
