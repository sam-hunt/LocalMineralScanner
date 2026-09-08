using RimWorld;
using Verse;

namespace LocalMineralScanner;

// Vanilla's PlaceWorker_NotUnderRoof, made conditional on the "doesn't work under a roof"
// setting. The vanilla worker checks RoofGrid.Roofed unconditionally (it never reads
// ThingDef.canBeUsedUnderRoof), so a def that lists it can't be toggled by a def-field
// write; subclassing and short-circuiting is the smallest way to make the rule optional.
// Reads the setting on every placement check, so a change applies without a restart.
public class PlaceWorker_NotUnderRoofIfRequired : PlaceWorker_NotUnderRoof
{
    public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map,
        Thing thingToIgnore = null, Thing thing = null)
    {
        if (!LocalMineralScannerMod.Settings.requireUnroofed)
        {
            return true;
        }
        return base.AllowsPlacing(checkingDef, loc, rot, map, thingToIgnore, thing);
    }
}
