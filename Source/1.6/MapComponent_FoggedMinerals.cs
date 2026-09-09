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
// same list the tuning gizmo offers, read through GenStepDefOf on every use. Nothing
// def-derived is cached in a static: an in-process play-data reload (a mid-session language
// change) rebuilds the DefDatabase with new instances, and a static holding the old ones
// would never match a live building.def again, reporting every mineral exhausted until the
// process restarts. DefOf fields are rebound on that reload, and this component's instance
// caches die with the map, so both are safe.
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
    private readonly HashSet<ThingDef> defsWithFoggedCells = new HashSet<ThingDef>();
    private bool dirty = true;
    private bool subscribed;

    // Per-map views of TrackedDefs (see the header for why they are not static): a set for
    // the event handlers' membership tests, and the value ordering behind
    // MostValuableFoggedDeposit.
    private HashSet<ThingDef> trackedDefSet;
    private List<ThingDef> defsByValueDescending;

    public MapComponent_FoggedMinerals(Map map) : base(map)
    {
    }

    private static List<ThingDef> TrackedDefs =>
        ((GenStep_PreciousLump)GenStepDefOf.PreciousLump.genStep).mineables;

    private HashSet<ThingDef> TrackedDefSet => trackedDefSet ??= new HashSet<ThingDef>(TrackedDefs);

    private List<ThingDef> DefsByValueDescending =>
        defsByValueDescending ??= TrackedDefs
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
        if (edifice != null && TrackedDefSet.Contains(edifice.def))
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
        if (TrackedDefSet.Contains(building.def))
        {
            dirty = true;
        }
    }

    private void Rebuild()
    {
        dirty = false;
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
    }
}
