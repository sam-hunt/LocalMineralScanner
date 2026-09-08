using RimWorld;
using Verse;

namespace LocalMineralScanner;

[DefOf]
public static class LocalMineralScannerDefOf
{
    public static JobDef OperateLocalMineralScanner;

    public static ThingDef LocalMineralScanner;

    // Core def with no vanilla ThingDefOf entry; the settings tooltips quote its label and
    // scan timings so translators never retype vanilla's text or values.
    public static ThingDef LongRangeMineralScanner;

    static LocalMineralScannerDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(LocalMineralScannerDefOf));
}
