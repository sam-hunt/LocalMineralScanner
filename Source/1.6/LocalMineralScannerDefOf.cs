using RimWorld;
using Verse;

namespace LocalMineralScanner;

[DefOf]
public static class LocalMineralScannerDefOf
{
    public static JobDef OperateLocalMineralScanner;

    static LocalMineralScannerDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(LocalMineralScannerDefOf));
}
