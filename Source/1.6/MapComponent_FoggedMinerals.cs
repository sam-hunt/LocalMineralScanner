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
// Ancient cryptosleep caskets are a second, opt-in kind of target (the scanAncientCaskets
// setting; Docs/design-research.md, "Ancient cryptosleep caskets", has the decompile
// evidence that revealing one wakes nothing). Tracked defs: every ThingDef whose thingClass
// is Building_AncientCryptosleepCasket or a subclass, so Core's casket and pod and any
// modded casket on the vanilla class all count, by the same "whichever mod defines it"
// logic as the ore test. They sit after the ores, by defName, and never back the default
// target. Rebuild tracks them whether or not the setting is on (two extra per-def list
// lookups), and the setting gates the queries instead: with it off, IsPresent and
// AnyFoggedDepositOf answer false for a casket def, so the menu drops the rows and a
// scanner still tuned to one stops through the exhaustion gate (the comp words the reason
// after the setting), with no settings-to-map plumbing. A casket
// is an edifice and a Building, so the event handlers below cover it unchanged. DisplayDef
// is the def whose label and icon stand for a target: the ore itself for a mineable, the
// casket def for a casket.
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

    public static bool IsAncientCasket(ThingDef def) =>
        def.thingClass != null && typeof(Building_AncientCryptosleepCasket).IsAssignableFrom(def.thingClass);

    // The def whose label and icon represent a target in the UI (see the header).
    public static ThingDef DisplayDef(ThingDef target) =>
        IsAncientCasket(target) ? target : target.building.mineableThing;

    private static float ValuePerCell(ThingDef def) =>
        def.building.mineableThing.BaseMarketValue * def.building.mineableYield;

    // Every tracked def: mineables most valuable per cell first, then caskets (see the header).
    public List<ThingDef> TrackedDefs =>
        trackedDefs ??= LongRangeScannerDefs
            .Union(DefDatabase<ThingDef>.AllDefsListForReading.Where(IsOre))
            .OrderByDescending(ValuePerCell)
            .ThenBy(def => def.defName)
            .Concat(DefDatabase<ThingDef>.AllDefsListForReading.Where(IsAncientCasket).OrderBy(def => def.defName))
            .ToList();

    private HashSet<ThingDef> TrackedDefSet => trackedDefSet ??= new HashSet<ThingDef>(TrackedDefs);

    private static bool Enabled(ThingDef def) =>
        LocalMineralScannerMod.Settings.scanAncientCaskets || !IsAncientCasket(def);

    // True when the map holds at least one of this target, fogged or not (always false for
    // a casket while the setting is off).
    public bool IsPresent(ThingDef targetDef)
    {
        if (dirty)
        {
            Rebuild();
        }
        return Enabled(targetDef) && defsPresent.Contains(targetDef);
    }

    // True when at least one cell of this target remains fogged (always false for a casket
    // while the setting is off).
    public bool AnyFoggedDepositOf(ThingDef targetDef)
    {
        if (dirty)
        {
            Rebuild();
        }
        return Enabled(targetDef) && defsWithFoggedCells.Contains(targetDef);
    }

    // The tracked mineable with fogged cells whose deposits are worth most per cell, or null
    // when no mineable remains undiscovered. Caskets are never a default.
    public ThingDef MostValuableFoggedDeposit()
    {
        List<ThingDef> byValue = TrackedDefs;
        for (int i = 0; i < byValue.Count; i++)
        {
            if (IsAncientCasket(byValue[i]))
            {
                return null;
            }
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
                if (AnyCellFogged(things[i], fog))
                {
                    defsWithFoggedCells.Add(def);
                    break;
                }
            }
        }
    }

    // A mineable is one cell; a casket is two, and a partial reveal (our own, or a debug
    // tool's) can leave either one fogged, so multi-cell things are tested cell by cell.
    private static bool AnyCellFogged(Thing thing, FogGrid fog)
    {
        if (thing.def.size == IntVec2.One)
        {
            return fog.IsFogged(thing.Position);
        }
        foreach (IntVec3 cell in thing.OccupiedRect())
        {
            if (fog.IsFogged(cell))
            {
                return true;
            }
        }
        return false;
    }
}
