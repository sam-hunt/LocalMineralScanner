// The scanner comp. Subclasses vanilla CompScanner (decompile-verified), which drives all
// progress from Used(Pawn) called every tick by the operating pawn's job toil: each call
// adds scanSpeed/60000 worked-days and rolls Rand.MTBEventOccurs every 59 ticks, with a
// forced success at scanFindGuaranteedDays. Two simultaneous operators therefore double
// progress with no extra code here; the base comp's scalar lastUserSpeed/lastScanTick only
// feed the inspect string, so concurrent Used() calls clobbering them is cosmetic.
//
// DoFind reveals (unfogs) one contiguous deposit of the targeted mineral instead of
// generating deep resources like CompDeepScanner:
// - map.fogGrid.Unfog(cell) per cell, never FloodUnfogAdjacent: Unfog is non-cascading and
//   does all mesh/roof/temperature dirtying itself (FogGrid.UnfogWorker), while the flood
//   variants would spill the reveal through open floor beyond the deposit.
// - Clusters are contiguous same-def mineable cells (cardinal adjacency, matching
//   Designator_MineVein's vein walk). Fully fogged clusters are preferred; partially
//   fogged ones are the fallback, and only their fogged cells are revealed.
// - Letter idiom copied from CompDeepScanner.DoFind: LetterDefOf.PositiveEvent (blue,
//   non-bouncing, pauses only under the player's "pause on any letter" preference) with a
//   cell-targeted LookTargets.
//
// CanUseNow adds a "no undiscovered deposits of the tuned mineral remain" gate on top of
// the base power/roof/forbidden/faction checks. It must be O(1): idle pawns' job search
// calls WorkGiver.HasJobOnThing -> CanUseNow up to ~60x/sec, so the answer comes from
// MapComponent_FoggedMinerals' event-invalidated cache. While gated, no jobs are issued and
// the saved progress accumulator simply freezes (the "paused at current progress" behavior
// comes free from the base class).
//
// The target-mineral gizmo is CompLongRangeMineralScanner's verbatim (same candidate list,
// GenStep_PreciousLump.mineables, and the same vanilla Keyed strings), retargeted at this
// comp so multi-select tuning works across several scanners.

using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace LocalMineralScanner;

public class CompProperties_LocalMineralScanner : CompProperties_Scanner
{
    public CompProperties_LocalMineralScanner() => compClass = typeof(CompLocalMineralScanner);
}

public class CompLocalMineralScanner : CompScanner
{
    private ThingDef targetMineable;

    // Cached in PostSpawnSetup (re-fetched on minify/reinstall like the base comp's
    // powerComp): Map.GetComponent is a linear scan, too heavy for CanUseNow's call rate.
    private MapComponent_FoggedMinerals foggedMinerals;

    public override void Initialize(CompProperties props)
    {
        base.Initialize(props);
        SetDefaultTargetMineral();
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        foggedMinerals = parent.Map.GetComponent<MapComponent_FoggedMinerals>();
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Defs.Look(ref targetMineable, "targetMineable");
        if (Scribe.mode == LoadSaveMode.PostLoadInit && targetMineable == null)
        {
            SetDefaultTargetMineral();
        }
    }

    private void SetDefaultTargetMineral()
    {
        targetMineable = ThingDefOf.MineableGold;
    }

    public override AcceptanceReport CanUseNow
    {
        get
        {
            AcceptanceReport baseReport = base.CanUseNow;
            if (!baseReport.Accepted)
            {
                return baseReport;
            }
            if (!foggedMinerals.AnyFoggedDepositOf(targetMineable))
            {
                return "LocalMineralScanner_NoFoggedDeposits".Translate(targetMineable.building.mineableThing.label);
            }
            return true;
        }
    }

    protected override void DoFind(Pawn worker)
    {
        Map map = parent.Map;
        List<IntVec3> revealCells = FindDepositToReveal(map);
        if (revealCells.NullOrEmpty())
        {
            return;
        }
        foreach (IntVec3 cell in revealCells)
        {
            map.fogGrid.Unfog(cell);
        }
        ThingDef mineral = targetMineable.building.mineableThing;
        Find.LetterStack.ReceiveLetter(
            "LocalMineralScanner_LetterLabelFoundDeposit".Translate() + ": " + mineral.LabelCap,
            "LocalMineralScanner_LetterFoundDeposit".Translate(mineral.label, worker.Named("FINDER")),
            LetterDefOf.PositiveEvent,
            new LookTargets(revealCells[revealCells.Count / 2], map));
    }

    // Returns the fogged cells of one chosen deposit: a random fully-fogged cluster of the
    // target mineable, falling back to a random partially-fogged one; null when none remain
    // (only reachable through a same-tick race with the CanUseNow gate, so no fallback).
    private List<IntVec3> FindDepositToReveal(Map map)
    {
        List<Thing> things = map.listerThings.ThingsOfDef(targetMineable);
        if (things.Count == 0)
        {
            return null;
        }
        HashSet<IntVec3> unvisited = new HashSet<IntVec3>();
        foreach (Thing thing in things)
        {
            unvisited.Add(thing.Position);
        }

        List<List<IntVec3>> fullyFogged = new List<List<IntVec3>>();
        List<List<IntVec3>> partiallyFogged = new List<List<IntVec3>>();
        Queue<IntVec3> open = new Queue<IntVec3>();
        while (unvisited.Count > 0)
        {
            IntVec3 seed = IntVec3.Invalid;
            foreach (IntVec3 cell in unvisited)
            {
                seed = cell;
                break;
            }
            unvisited.Remove(seed);
            open.Enqueue(seed);

            int clusterSize = 0;
            List<IntVec3> foggedCells = new List<IntVec3>();
            while (open.Count > 0)
            {
                IntVec3 cell = open.Dequeue();
                clusterSize++;
                if (map.fogGrid.IsFogged(cell))
                {
                    foggedCells.Add(cell);
                }
                for (int i = 0; i < GenAdj.CardinalDirections.Length; i++)
                {
                    IntVec3 neighbor = cell + GenAdj.CardinalDirections[i];
                    if (unvisited.Remove(neighbor))
                    {
                        open.Enqueue(neighbor);
                    }
                }
            }
            if (foggedCells.Count == clusterSize)
            {
                fullyFogged.Add(foggedCells);
            }
            else if (foggedCells.Count > 0)
            {
                partiallyFogged.Add(foggedCells);
            }
        }

        if (fullyFogged.Count > 0)
        {
            return fullyFogged.RandomElement();
        }
        if (partiallyFogged.Count > 0)
        {
            return partiallyFogged.RandomElement();
        }
        return null;
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra())
        {
            yield return gizmo;
        }
        if (parent.Faction != Faction.OfPlayer)
        {
            yield break;
        }
        ThingDef mineableThing = targetMineable.building.mineableThing;
        Command_Action command = new Command_Action
        {
            defaultLabel = "CommandSelectMineralToScanFor".Translate() + ": " + mineableThing.LabelCap,
            defaultDesc = "CommandSelectMineralToScanForDesc".Translate(),
            icon = mineableThing.uiIcon,
            iconAngle = mineableThing.uiIconAngle,
            iconOffset = mineableThing.uiIconOffset,
            action = delegate
            {
                List<ThingDef> mineables = ((GenStep_PreciousLump)GenStepDefOf.PreciousLump.genStep).mineables;
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (ThingDef mineable in mineables)
                {
                    ThingDef localMineable = mineable;
                    options.Add(new FloatMenuOption(localMineable.building.mineableThing.LabelCap, delegate
                    {
                        foreach (object selectedObject in Find.Selector.SelectedObjects)
                        {
                            if (selectedObject is Thing thing)
                            {
                                CompLocalMineralScanner comp = thing.TryGetComp<CompLocalMineralScanner>();
                                if (comp != null)
                                {
                                    comp.targetMineable = localMineable;
                                }
                            }
                        }
                    }, MenuOptionPriority.Default, null, null, 29f,
                    (Rect rect) => Widgets.InfoCardButton(rect.x + 5f, rect.y + (rect.height - 24f) / 2f, localMineable.building.mineableThing)));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
        };
        yield return command;
    }
}
