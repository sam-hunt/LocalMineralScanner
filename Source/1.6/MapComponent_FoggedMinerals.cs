// Per-map cache answering "do any fogged cells of this mineable def remain?" in O(1), so
// CompLocalMineralScanner.CanUseNow stays within its budget (idle pawns' job search calls
// it up to ~60x/sec/pawn). Uses the dirty-flag + lazy-recompute pattern vanilla's own
// PathFinderMapData applies to the same events (decompile-verified):
// - map.events.CellFogChanged fires per cell from every FogGrid.Unfog/Refog, including
//   mining through rock (Building.DeSpawn -> Notify_FogBlockerRemoved -> ...Unfog) and our
//   own DoFind reveals; the handler only dirties when the cell holds a tracked mineable
//   (EdificeGrid indexer lookup - at unfog time the rock is still spawned).
// - ThingSpawned/ThingDespawned cover mineables being mined out or spawned; MapFogged
//   covers full re-fogs (debug tools).
// Rebuild walks listerThings.ThingsOfDef per tracked def - maintained per-def lists, never
// an AllThings scan (there is no ThingRequestGroup covering mineables; BuildingArtificial
// explicitly excludes resource rock). Tracked defs = GenStep_PreciousLump.mineables, the
// same list the tuning gizmo offers, resolved lazily since DefOf isn't ready at load time.
//
// RimWorld instantiates every MapComponent subclass on every map automatically, so no
// registration is needed. Subscription happens in FinalizeInit (runs on both mapgen and
// save-load, after map systems exist); queries before that still work off the initial
// dirty flag. MapRemoved unsubscribes to keep a discarded map from pinning this component.

using System.Collections.Generic;
using RimWorld;
using Verse;

namespace LocalMineralScanner;

public class MapComponent_FoggedMinerals : MapComponent
{
    private static List<ThingDef> cachedTrackedDefs;

    private readonly HashSet<ThingDef> defsWithFoggedCells = new HashSet<ThingDef>();
    private bool dirty = true;
    private bool subscribed;

    public MapComponent_FoggedMinerals(Map map) : base(map)
    {
    }

    private static List<ThingDef> TrackedDefs =>
        cachedTrackedDefs ??= ((GenStep_PreciousLump)GenStepDefOf.PreciousLump.genStep).mineables;

    public bool AnyFoggedDepositOf(ThingDef mineableDef)
    {
        if (dirty)
        {
            Rebuild();
        }
        return defsWithFoggedCells.Contains(mineableDef);
    }

    public override void FinalizeInit()
    {
        base.FinalizeInit();
        if (!subscribed)
        {
            subscribed = true;
            map.events.CellFogChanged += Notify_CellFogChanged;
            map.events.MapFogged += Notify_MapFogged;
            map.events.ThingSpawned += Notify_ThingChanged;
            map.events.ThingDespawned += Notify_ThingChanged;
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
            map.events.ThingSpawned -= Notify_ThingChanged;
            map.events.ThingDespawned -= Notify_ThingChanged;
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

    private void Notify_ThingChanged(Thing thing)
    {
        if (TrackedDefs.Contains(thing.def))
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
