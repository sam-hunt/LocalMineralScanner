// Per-map cache answering "do any fogged cells of this mineable def remain?" in O(1), so
// CompLocalMineralScanner.CanUseNow stays within its budget (idle pawns' job search calls
// it up to ~60x/sec/pawn). Uses the dirty-flag + lazy-recompute pattern vanilla's own
// PathFinderMapData applies to the same events (decompile-verified):
// - map.events.CellFogChanged fires per cell from every FogGrid.Unfog/Refog, including
//   mining through rock (Building.DeSpawn -> Notify_FogBlockerRemoved -> ...Unfog) and our
//   own DoFind reveals; the handler only dirties when the cell still holds a tracked
//   mineable (EdificeGrid indexer lookup). A rock being mined out is already deregistered
//   when its own cell unfogs, so that case rides on BuildingDespawned instead.
// - BuildingSpawned/BuildingDespawned cover mineables being mined out or spawned (Mineable
//   is a Building, and these skip the mote/filth/item/pawn traffic ThingSpawned carries);
//   MapFogged covers full re-fogs (debug tools).
// Rebuild walks listerThings.ThingsOfDef per tracked def - maintained per-def lists, never
// an AllThings scan (there is no ThingRequestGroup covering mineables; BuildingArtificial
// explicitly excludes resource rock). Tracked defs = GenStep_PreciousLump.mineables, the
// same list the tuning gizmo offers, resolved lazily since DefOf isn't ready at load time.
//
// Exhaustion announcements: Rebuild also diffs the new set against the previous one, and
// every def that dropped out is queued for MapComponentTick, which posts one message per
// mineral when a player scanner on this map is tuned to it (the drill's DeepDrillExhausted
// idiom: Messages.Message(TaskCompletion) targeting the building, once, edge-triggered).
// Queries stay side-effect free (they may rebuild, but only the tick announces), and the
// tick body is a bool check when nothing changed. There is no rarer MapComponent hook, and
// a tick-modulo guard would cost more than the flag it gates. Covers every route to
// exhaustion alike: a scanner find, a pawn mining or exploring into the last deposit, or a
// debug reveal. Not a load: the first rebuild has no previous set to diff against.
//
// MostValuableFoggedDeposit backs the fresh scanner's default target. Value is per deposit
// cell, mineableThing.BaseMarketValue * building.mineableYield, the product
// GenStep_PreciousLump sizes its lumps by; raw market value would rank components (32
// silver each, 2 per cell) above gold. Vanilla order: gold 400, plasteel 360, uranium 240,
// jade 200, steel 76, components 64, silver 40.
//
// RimWorld instantiates every MapComponent subclass on every map automatically, so no
// registration is needed. Subscription happens in FinalizeInit (runs on both mapgen and
// save-load, after map systems exist); queries before that still work off the initial
// dirty flag. MapRemoved unsubscribes to keep a discarded map from pinning this component.

using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace LocalMineralScanner;

public class MapComponent_FoggedMinerals : MapComponent
{
    private static List<ThingDef> cachedTrackedDefs;
    private static List<ThingDef> cachedDefsByValue;

    private HashSet<ThingDef> defsWithFoggedCells = new HashSet<ThingDef>();
    private HashSet<ThingDef> previousDefsWithFoggedCells = new HashSet<ThingDef>();
    private readonly List<ThingDef> newlyExhausted = new List<ThingDef>();
    private bool dirty = true;
    private bool subscribed;

    public MapComponent_FoggedMinerals(Map map) : base(map)
    {
    }

    private static List<ThingDef> TrackedDefs =>
        cachedTrackedDefs ??= ((GenStep_PreciousLump)GenStepDefOf.PreciousLump.genStep).mineables;

    private static List<ThingDef> DefsByValueDescending =>
        cachedDefsByValue ??= TrackedDefs
            .OrderByDescending(def => def.building.mineableThing.BaseMarketValue * def.building.mineableYield)
            .ToList();

    public bool AnyFoggedDepositOf(ThingDef mineableDef)
    {
        if (dirty)
        {
            Rebuild();
        }
        return defsWithFoggedCells.Contains(mineableDef);
    }

    // The tracked mineable with fogged cells whose deposits are worth most per cell, or null
    // when nothing tracked remains undiscovered.
    public ThingDef MostValuableFoggedDeposit()
    {
        List<ThingDef> byValue = DefsByValueDescending;
        for (int i = 0; i < byValue.Count; i++)
        {
            if (AnyFoggedDepositOf(byValue[i]))
            {
                return byValue[i];
            }
        }
        return null;
    }

    public override void FinalizeInit()
    {
        base.FinalizeInit();
        if (!subscribed)
        {
            subscribed = true;
            map.events.CellFogChanged += Notify_CellFogChanged;
            map.events.MapFogged += Notify_MapFogged;
            map.events.BuildingSpawned += Notify_BuildingChanged;
            map.events.BuildingDespawned += Notify_BuildingChanged;
        }
        dirty = true;
    }

    public override void MapComponentTick()
    {
        if (dirty)
        {
            Rebuild();
        }
        if (newlyExhausted.Count > 0)
        {
            AnnounceExhausted();
        }
    }

    // One message per newly exhausted mineral, targeting the first player scanner tuned to
    // it; minerals no scanner is tuned to pass silently (the inspect line and greyed tuning
    // menu already cover them when the player next looks). A mineral that regained a fogged
    // cell since it was queued (several rebuilds can precede one tick) is skipped.
    private void AnnounceExhausted()
    {
        List<Thing> scanners = map.listerThings.ThingsOfDef(LocalMineralScannerDefOf.LocalMineralScanner);
        foreach (ThingDef mineable in newlyExhausted)
        {
            if (defsWithFoggedCells.Contains(mineable))
            {
                continue;
            }
            for (int i = 0; i < scanners.Count; i++)
            {
                Thing scanner = scanners[i];
                if (scanner.Faction == Faction.OfPlayer
                    && scanner.TryGetComp<CompLocalMineralScanner>()?.TargetMineable == mineable)
                {
                    Messages.Message(
                        "LocalMineralScanner_MessageExhausted".Translate(mineable.building.mineableThing.label),
                        scanner,
                        MessageTypeDefOf.TaskCompletion);
                    break;
                }
            }
        }
        newlyExhausted.Clear();
    }

    public override void MapRemoved()
    {
        base.MapRemoved();
        if (subscribed)
        {
            subscribed = false;
            map.events.CellFogChanged -= Notify_CellFogChanged;
            map.events.MapFogged -= Notify_MapFogged;
            map.events.BuildingSpawned -= Notify_BuildingChanged;
            map.events.BuildingDespawned -= Notify_BuildingChanged;
        }
    }

    private void Notify_CellFogChanged(IntVec3 cell, bool fogged)
    {
        Building edifice = map.edificeGrid[cell];
        if (edifice != null && TrackedDefs.Contains(edifice.def))
        {
            dirty = true;
        }
    }

    private void Notify_MapFogged()
    {
        dirty = true;
    }

    private void Notify_BuildingChanged(Building building)
    {
        if (TrackedDefs.Contains(building.def))
        {
            dirty = true;
        }
    }

    private void Rebuild()
    {
        dirty = false;
        (previousDefsWithFoggedCells, defsWithFoggedCells) = (defsWithFoggedCells, previousDefsWithFoggedCells);
        defsWithFoggedCells.Clear();
        FogGrid fog = map.fogGrid;
        foreach (ThingDef def in TrackedDefs)
        {
            List<Thing> things = map.listerThings.ThingsOfDef(def);
            for (int i = 0; i < things.Count; i++)
            {
                if (fog.IsFogged(things[i].Position))
                {
                    defsWithFoggedCells.Add(def);
                    break;
                }
            }
        }
        foreach (ThingDef def in previousDefsWithFoggedCells)
        {
            if (!defsWithFoggedCells.Contains(def))
            {
                newlyExhausted.Add(def);
            }
        }
    }
}
