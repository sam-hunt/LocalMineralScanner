// The scanner comp. Subclasses vanilla CompScanner (decompile-verified), which drives all
// progress from Used(Pawn), called every tick by the operating pawn's job toil: each call
// adds scanSpeed/60000 worked-days and rolls Rand.MTBEventOccurs every 59 ticks, with a
// forced success at scanFindGuaranteedDays. Two simultaneous operators therefore double
// progress with no extra code here. Used() is non-virtual and its scalar
// lastUserSpeed/lastScanTick are clobbered by concurrent callers, so the job driver calls
// Operate() (a wrapper that also sums the tick's operator speeds) and
// CompInspectStringExtra is overridden to report that combined figure.
//
// DoFind reveals (unfogs) one contiguous deposit of the targeted mineral instead of
// generating deep resources like CompDeepScanner:
// - map.fogGrid.Unfog(cell) per cell, never FloodUnfogAdjacent: Unfog is non-cascading and
//   does all mesh/roof/temperature dirtying itself (FogGrid.UnfogWorker), while the flood
//   variants would spill the reveal through open floor beyond the deposit.
// - Clusters are contiguous same-def mineable cells (cardinal adjacency, matching
//   Designator_MineVein's vein walk). Fully fogged clusters are preferred; partially
//   fogged ones are the fallback, and only their fogged cells are revealed.
// - Letter idiom copied from CompDeepScanner.DoFind: LetterDefOf.PositiveEvent with a
//   cell-targeted LookTargets.
//
// Exhaustion: unlike its vanilla siblings, this scanner can run out of targets. The
// handling follows vanilla's "ran dry" idioms; Docs/design-research.md (Exhaustion UX)
// holds the precedent survey and the rejected alternatives. As implemented:
// - CanUseNow adds a "no undiscovered deposits of the tuned mineral remain" reason on top
//   of the base checks, the same channel as CompDeepScanner's no-bedrock reason: it reaches
//   the player as the forced-job fail text, the running job ends via the driver's FailOn,
//   and the saved progress accumulator freezes. Nothing is auto-forbidden. The gate must be
//   O(1): idle pawns' job search calls WorkGiver.HasJobOnThing -> CanUseNow up to ~60x/sec,
//   so the answer comes from MapComponent_FoggedMinerals' event-invalidated cache.
// - The inspect string carries a standing exhaustion line (CompDeepDrill's
//   "DeepDrillNoResources" idiom). No Alert.
// - The find that reveals the LAST deposit says so in a trailing paragraph of its letter.
//   The other routes to exhaustion (exploring, mining, retuning) are player-caused and get
//   only the inspect line and job-fail text.
// - The tuning menu greys out exhausted minerals with a parenthesised reason (the disabled
//   FloatMenuOption idiom). No auto-retune and no "any mineral" option.
// - Default target: gold, for parity with CompLongRangeMineralScanner. Gold is rare (~2.9%
//   of ore scatter), so on its FIRST spawn only, a scanner whose target is exhausted tunes
//   itself to the most valuable mineral still undiscovered
//   (MapComponent_FoggedMinerals.MostValuableFoggedDeposit). Save-load and reinstall never
//   touch the saved target; pendingInitialTarget's declaration explains the one-shot plumbing.
//
// The target-mineral gizmo is CompLongRangeMineralScanner's verbatim (same candidate list,
// GenStep_PreciousLump.mineables, and the same vanilla Keyed strings), retargeted at this
// comp so multi-select tuning works across several scanners.

using System.Collections.Generic;
using System.Text;
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

    // Operators and summed scan speed for the most recent tick Operate() ran. Transient by
    // design: the inspect string only reads them within a 30-tick grace of that tick (the
    // vanilla window), so nothing stale survives a save/load.
    private int operatingTick = -1;
    private int operatorCount;
    private float combinedSpeed;

    // One-shot flag behind the header's default-target fallback: set by Initialize (fresh
    // construction, or a traded/quest MinifiedThing), consumed by the first PostSpawnSetup,
    // which applies the fallback only when !respawningAfterLoad. Saved, because Initialize
    // also re-runs on load (ThingWithComps.ExposeData -> InitializeComps) and would re-arm
    // it: harmless for a spawned scanner (the load-time PostSpawnSetup consumes it again),
    // but a never-installed minified one - a trade or quest reward - has no spawn to consume
    // it, and a fallback at install would override a tuning the player made while it sat in
    // storage. The saved value wins over the re-arm.
    private bool pendingInitialTarget;

    public override void Initialize(CompProperties props)
    {
        base.Initialize(props);
        SetDefaultTargetMineral();
        pendingInitialTarget = true;
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        foggedMinerals = parent.Map.GetComponent<MapComponent_FoggedMinerals>();
        if (pendingInitialTarget)
        {
            pendingInitialTarget = false;
            if (!respawningAfterLoad && TargetExhausted)
            {
                targetMineable = foggedMinerals.MostValuableFoggedDeposit() ?? targetMineable;
            }
        }
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Defs.Look(ref targetMineable, "targetMineable");
        Scribe_Values.Look(ref pendingInitialTarget, "pendingInitialTarget", defaultValue: false);
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
            AcceptanceReport baseReport = LocalMineralScannerMod.Settings.requireUnroofed
                ? base.CanUseNow
                : CanUseNowIgnoringRoof;
            if (!baseReport.Accepted)
            {
                return baseReport;
            }
            if (TargetExhausted)
            {
                return ExhaustedReason(targetMineable);
            }
            return true;
        }
    }

    // CompScanner.CanUseNow (decompile-verified, 1.6) minus its RoofUtility.IsAnyCellUnderRoof
    // check, which is hardcoded there rather than read from def.canBeUsedUnderRoof, so the
    // "doesn't work under a roof" setting has to bypass the whole property. The other checks
    // are the base's (spawned, powered, not forbidden, player-owned); keep them in step with
    // it if it changes.
    private AcceptanceReport CanUseNowIgnoringRoof
    {
        get
        {
            if (!parent.Spawned)
            {
                return false;
            }
            if (powerComp?.PowerOn == false)
            {
                return false;
            }
            if (forbiddable?.Forbidden == true)
            {
                return false;
            }
            return parent.Faction == Faction.OfPlayer;
        }
    }

    // True when no fogged cell of the tuned mineral remains on the map. Only meaningful while
    // spawned: foggedMinerals is the spawn-time map's component.
    private bool TargetExhausted => !foggedMinerals.AnyFoggedDepositOf(targetMineable);

    private static TaggedString ExhaustedReason(ThingDef mineable) =>
        "LocalMineralScanner_NoFoggedDeposits".Translate(mineable.building.mineableThing.label);

    // The job driver's per-tick entry point, wrapping the non-virtual CompScanner.Used so the
    // inspect string can report the combined rate. Summing speeds is exact, not an
    // approximation: every operator rolls Rand.MTBEventOccurs at rate speed/scanFindMtbDays
    // on the same hash-interval tick (rates of independent events add), and the worked-days
    // accumulator gains speed/60000 per call. The speed lookup mirrors Used's.
    public void Operate(Pawn worker)
    {
        int tick = Find.TickManager.TicksGame;
        if (tick != operatingTick)
        {
            operatingTick = tick;
            operatorCount = 0;
            combinedSpeed = 0f;
        }
        operatorCount++;
        combinedSpeed += Props.scanSpeedStat != null ? worker.GetStatValue(Props.scanSpeedStat) : 1f;
        Used(worker);
    }

    // Replaces CompScanner's string (same lines and vanilla keys, plus a guaranteed-find ETA)
    // with the combined figures from Operate(). ResearchSpeed has a 0.1 floor, so the
    // divisions are safe. OnGUI runs after the frame's ticks, so the sum is never read half
    // accumulated.
    public override string CompInspectStringExtra()
    {
        StringBuilder sb = new StringBuilder();
        if (operatorCount > 0 && operatingTick > Find.TickManager.TicksGame - 30)
        {
            TaggedString speedLabel = operatorCount == 1
                ? "UserScanAbility".Translate()
                : "LocalMineralScanner_CombinedScanSpeed".Translate(operatorCount);
            sb.AppendLine(speedLabel + ": " + combinedSpeed.ToStringPercent());
            sb.AppendLine("ScanAverageInterval".Translate() + ": "
                + "PeriodDays".Translate((Props.scanFindMtbDays / combinedSpeed).ToString("F1")));
            if (Props.scanFindGuaranteedDays > 0f)
            {
                float daysLeft = Mathf.Max(0f, (Props.scanFindGuaranteedDays - daysWorkingSinceLastFinding) / combinedSpeed);
                sb.AppendLine("LocalMineralScanner_GuaranteedFindWithin".Translate() + ": "
                    + "PeriodDays".Translate(daysLeft.ToString("F1")));
            }
        }
        sb.Append("ScanningProgressToGuaranteedFind".Translate() + ": "
            + (daysWorkingSinceLastFinding / Props.scanFindGuaranteedDays).ToStringPercent());
        if (parent.Spawned && TargetExhausted)
        {
            sb.AppendLine();
            sb.Append(ExhaustedReason(targetMineable).CapitalizeFirst());
        }
        return sb.ToString();
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
        TaggedString text = "LocalMineralScanner_LetterFoundDeposit".Translate(mineral.label, worker.Named("FINDER"));
        if (TargetExhausted)
        {
            text += "\n\n" + "LocalMineralScanner_LetterFoundDepositLast".Translate(mineral.label);
        }
        Find.LetterStack.ReceiveLetter(
            "LocalMineralScanner_LetterLabelFoundDeposit".Translate() + ": " + mineral.LabelCap,
            text,
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
                    string label = localMineable.building.mineableThing.LabelCap;
                    if (!foggedMinerals.AnyFoggedDepositOf(localMineable))
                    {
                        options.Add(new FloatMenuOption(
                            label + " (" + "LocalMineralScanner_NoneUndiscovered".Translate() + ")", null,
                            MenuOptionPriority.Default, null, null, 29f,
                            (Rect rect) => Widgets.InfoCardButton(rect.x + 5f, rect.y + (rect.height - 24f) / 2f, localMineable.building.mineableThing)));
                        continue;
                    }
                    options.Add(new FloatMenuOption(label, delegate
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
