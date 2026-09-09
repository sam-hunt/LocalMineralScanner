// Per-map cache answering "does this map hold any of this mineable def, and do any of its
// cells remain fogged?" in O(1), so CompLocalMineralScanner.CanUseNow stays within its
// budget (idle pawns' job search calls it up to ~60x/sec/pawn). Uses the dirty-flag + lazy-
// recompute pattern vanilla's own PathFinderMapData applies to the same events
// (decompile-verified):
// - map.events.CellFogChanged fires per cell from every FogGrid.Unfog/Refog, including
//   mining through rock (Building.DeSpawn -> Notify_FogBlockerRemoved -> ...Unfog), our
//   own DoFind reveals and the debug rectangle fog tool; the handler only dirties when the
//   cell still holds a tracked mineable (EdificeGrid indexer lookup). A rock being mined
//   out is already deregistered when its own cell unfogs, so that case rides on
//   BuildingDespawned instead.
// - BuildingSpawned/BuildingDespawned cover mineables being mined out or spawned by any
//   route (mapgen, skyfallers, quest sites, mods, the debug spawner; Mineable is a
//   Building, and these skip the mote/filth/item/pawn traffic ThingSpawned carries);
//   MapFogged covers full re-fogs (debug tools).
// Rebuild walks listerThings.ThingsOfDef per tracked def - maintained per-def lists, never
// an AllThings scan (there is no ThingRequestGroup covering mineables; BuildingArtificial
// explicitly excludes resource rock). Presence is the list's count; the fogged walk stops
// at the first fogged cell. Defs absent from the map cost one dictionary lookup.
//
// Tracked defs = every ThingDef with building.isResourceRock and a mineableThing, the test
// vanilla itself uses for "ore" (TileMutatorWorker_MineralRich's candidate enumeration and
// ThingSetMaker_Meteorite's ore/rock split, decompile-verified), so an ore is tracked
// whichever mod defines it and whatever placed it, plus GenStep_PreciousLump.mineables (the
// long-range scanner's list, which ore mods may patch into). Order is per-cell value,
// descending (the ranking below), ties by defName so it does not depend on load order; the
// long-range scanner's XML order is not kept, since it carries no meaning once modded ores
// interleave. The menu offers an ore only while the map holds some (IsPresent), vanilla or
// modded alike, plus the scanner's current target, so ores that exist only on asteroid,
// landmark or quest maps do not pad the menu at home;
// Docs/design-research.md (decision 3) records the mod survey behind that split. The list
// is built once per component from DefDatabase. Nothing def-derived is cached in a static:
// an in-process play-data reload (a mid-session language change) rebuilds the DefDatabase
// with new instances, and a static holding the old ones would never match a live
// building.def again, reporting every mineral exhausted until the process restarts. DefOf
// fields are rebound on that reload, and this component's instance caches die with the
// map, so both are safe.
//
// MostValuableFoggedDeposit backs the fresh scanner's default target and walks the same
// ordering. Value is per deposit cell, mineableThing.BaseMarketValue *
// building.mineableYield, the product GenStep_PreciousLump sizes its lumps by; raw market
// value would rank components (32 silver each, 2 per cell) above gold. Vanilla order: gold
// 400, plasteel 360, uranium 240, jade 200, steel 76, components 64, silver 40; modded ores
// rank by the same product.
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
    private readonly HashSet<ThingDef> defsPresent = new HashSet<ThingDef>();
    private readonly HashSet<ThingDef> defsWithFoggedCells = new HashSet<ThingDef>();
    private bool dirty = true;
    private bool subscribed;

    // Per-map views of the tracked defs (see the header for why they are not static): the
    // value-ordered list and a set for the event handlers' membership tests.
    private List<ThingDef> trackedDefs;
    private HashSet<ThingDef> trackedDefSet;

    public MapComponent_FoggedMinerals(Map map) : base(map)
    {
    }

    private static List<ThingDef> LongRangeScannerDefs =>
        ((GenStep_PreciousLump)GenStepDefOf.PreciousLump.genStep).mineables;

    private static bool IsOre(ThingDef def) =>
        def.building?.isResourceRock == true && def.building.mineableThing != null;

    private static float ValuePerCell(ThingDef def) =>
        def.building.mineableThing.BaseMarketValue * def.building.mineableYield;

    // Every tracked mineable def, most valuable per cell first (see the header).
    public List<ThingDef> TrackedDefs =>
        trackedDefs ??= LongRangeScannerDefs
            .Union(DefDatabase<ThingDef>.AllDefsListForReading.Where(IsOre))
            .OrderByDescending(ValuePerCell)
            .ThenBy(def => def.defName)
            .ToList();

    private HashSet<ThingDef> TrackedDefSet => trackedDefSet ??= new HashSet<ThingDef>(TrackedDefs);

    // True when the map holds at least one cell of this mineable, fogged or not.
    public bool IsPresent(ThingDef mineableDef)
    {
        if (dirty)
        {
            Rebuild();
        }
        return defsPresent.Contains(mineableDef);
    }

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
        List<ThingDef> byValue = TrackedDefs;
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
        defsPresent.Clear();
        defsWithFoggedCells.Clear();
        FogGrid fog = map.fogGrid;
        List<ThingDef> tracked = TrackedDefs;
        for (int d = 0; d < tracked.Count; d++)
        {
            ThingDef def = tracked[d];
            List<Thing> things = map.listerThings.ThingsOfDef(def);
            if (things.Count == 0)
            {
                continue;
            }
            defsPresent.Add(def);
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
