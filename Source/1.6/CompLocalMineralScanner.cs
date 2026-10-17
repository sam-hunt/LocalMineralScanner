// The scanner comp. Subclasses vanilla CompScanner (decompile-verified), which drives all
// progress from Used(Pawn), called every tick by the operating pawn's job toil: each call
// adds scanSpeed/60000 worked-days and rolls Rand.MTBEventOccurs every 59 ticks, with a
// forced success at scanFindGuaranteedDays. Two simultaneous operators therefore double
// progress with no extra code here. Used() is non-virtual, looks the scan speed up itself
// with an uncached GetStatValue every tick (the driver header has the cost), and its scalar
// lastUserSpeed/lastScanTick are clobbered by concurrent callers, so the job driver calls
// Operate() instead: Used's body with the speed passed in, which also sums the tick's
// operator speeds for the overridden CompInspectStringExtra. Both operators roll on the
// same hash-interval tick, so two rolls can succeed at once; DoFind reveals for the first
// and ignores the second (Operate has already reset the accumulator for it).
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
// Ancient cryptosleep caskets (opt-in; MapComponent_FoggedMinerals says which defs count and
// how the setting gates them) reveal by the same rules with the casket's OccupiedRect as the
// cluster. A casket's fillPercent is 0.5, so it is no fog blocker and Unfog clears only the
// cell it is given: FindCasketToReveal lists both cells itself. Docs/design-research.md
// ("Ancient cryptosleep caskets") holds the decompile evidence that this wakes nothing: a
// temple's mechanoids wake only when THEIR OWN cell is unfogged, a hive likewise, and neither
// can occupy a casket cell. Two guards anyway: a casket with a dormant occupant on a fogged
// cell (an inactive-minded pawn, or a thing whose CompCanBeDormant is asleep) is skipped, and
// caskets covering the centre cell of a live destroy-if-unfogged RectTrigger (vanilla's
// "ancient danger" warning, which self-destructs when that cell unfogs) rank behind the
// rest; disarming one is accepted when nothing else remains, and the find letter is worded
// as a warning so nothing is lost by it.
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
// - The find that reveals the LAST deposit also posts a TaskCompletion message targeting
//   the scanner (the DeepDrillExhausted idiom), alongside its find letter. The other routes
//   to exhaustion (a pawn mining or exploring into the last deposit) are rare and
//   player-caused; they get only the inspect line and job-fail text.
// - The tuning menu greys out exhausted minerals with a parenthesised reason (the disabled
//   FloatMenuOption idiom). No auto-retune and no "any mineral" option.
// - Default target: gold, for parity with CompLongRangeMineralScanner. Gold is rare (~2.9%
//   of ore scatter), so on its FIRST spawn only, a scanner whose target is exhausted tunes
//   itself to the most valuable mineral still undiscovered
//   (MapComponent_FoggedMinerals.MostValuableFoggedDeposit). Save-load and reinstall never
//   touch the saved target; pendingInitialTarget's declaration explains the one-shot plumbing.
//
// The target-mineral gizmo follows CompLongRangeMineralScanner's (same FloatMenu idiom and
// vanilla Keyed strings), retargeted at this comp so multi-select tuning works across
// several scanners, over MapComponent_FoggedMinerals.TrackedDefs instead of the long-range
// scanner's fixed list (that header has the rationale). The menu lists the ores this map
// holds, plus the current target so the button's label always has a row and the menu is
// never empty (vanilla's FloatMenu errors and closes on zero options); rows whose every
// cell is already revealed are greyed, never hidden.

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
    // but a scanner that was installed, tuned, uninstalled and then saved has no load-time
    // spawn to consume it, and a re-armed flag would override the player's tuning at
    // reinstall. The saved value (false, consumed at its first spawn) wins over the re-arm.
    // A never-installed minified one saves true and correctly gets the fallback when first
    // installed; it cannot have been tuned, since MinifiedThing shows no inner-comp gizmos.
    private bool pendingInitialTarget;

    // Tick of the last DoFind reveal, so a second same-tick success (see the header) is a
    // no-op rather than a second deposit and letter. Transient: nothing spans a tick.
    private int lastFindTick = -1;

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

    // Why the target yields nothing: the setting is off (a casket target only), or no fogged
    // cell of it remains. The menu's greyed rows use the same text in parentheses.
    private static TaggedString ExhaustedReason(ThingDef target)
    {
        if (!MapComponent_FoggedMinerals.IsAncientCasket(target))
        {
            return "LocalMineralScanner_NoFoggedDeposits".Translate(target.building.mineableThing.label);
        }
        return LocalMineralScannerMod.Settings.scanAncientCaskets
            ? "LocalMineralScanner_NoFoggedCaskets".Translate(PluralLabel(target))
            : "LocalMineralScanner_CasketsDisabled".Translate();
    }

    // The casket strings take the plural ("ancient cryptosleep caskets"); vanilla's
    // LanguageWorker forms it per language, resolving the noun's gender itself.
    private static string PluralLabel(ThingDef casket) =>
        Find.ActiveLanguageWorker.Pluralize(casket.label);

    // The job driver's per-tick entry point, replacing CompScanner.Used: its body verbatim
    // (decompile-verified, 1.6: stamp lastScanTick/lastUserSpeed, add speed/60000 worked
    // days, TickDoesFind -> DoFind and reset; keep in step if it changes) except that the
    // speed arrives from the driver's cache instead of a per-tick stat lookup, and without
    // the "Used while CanUseNow is false" error guard, which the job's two fail conditions
    // already enforce. Summing speeds is exact, not an approximation: every operator rolls
    // Rand.MTBEventOccurs at rate speed/scanFindMtbDays on the same hash-interval tick (rates
    // of independent events add), and the worked-days accumulator gains speed/60000 per
    // call. lastUserSpeed keeps Used's meaning (the last operator's speed) for the base
    // class; the reported figure is combinedSpeed.
    public void Operate(Pawn worker, float speed)
    {
        int tick = Find.TickManager.TicksGame;
        if (tick != operatingTick)
        {
            operatingTick = tick;
            operatorCount = 0;
            combinedSpeed = 0f;
        }
        operatorCount++;
        combinedSpeed += speed;
        lastScanTick = tick;
        lastUserSpeed = speed;
        daysWorkingSinceLastFinding += speed / 60000f;
        if (TickDoesFind(speed))
        {
            DoFind(worker);
            daysWorkingSinceLastFinding = 0f;
        }
    }

    // The speed Used would look up: Props.scanSpeedStat (ResearchSpeed for this def), 1 when
    // the def sets none. Uncached and costly; the driver calls it on its refresh interval.
    public float ScanSpeedOf(Pawn worker) =>
        Props.scanSpeedStat != null ? worker.GetStatValue(Props.scanSpeedStat) : 1f;

    // Replaces CompScanner's string (same three lines and vanilla keys) with the combined
    // figures from Operate(). ResearchSpeed has a 0.1 floor, so the divisions are safe. OnGUI
    // runs after the frame's ticks, so the sum is never read half accumulated.
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
        int tick = Find.TickManager.TicksGame;
        if (tick == lastFindTick)
        {
            return;
        }
        lastFindTick = tick;
        Map map = parent.Map;
        bool casket = MapComponent_FoggedMinerals.IsAncientCasket(targetMineable);
        List<IntVec3> revealCells = casket ? FindCasketToReveal(map) : FindDepositToReveal(map);
        if (revealCells.NullOrEmpty())
        {
            // Unreachable while the CanUseNow gate and the Find*ToReveal methods agree on what
            // counts as undiscovered (the casket guard is the one deliberate gap, see the
            // header); Operate still resets the accumulator, so say so if it ever happens.
            Log.Warning("[LocalMineralScanner] Find succeeded but no undiscovered "
                + targetMineable.defName + " was found on " + map + "; progress reset.");
            return;
        }
        foreach (IntVec3 cell in revealCells)
        {
            map.fogGrid.Unfog(cell);
        }
        ThingDef display = MapComponent_FoggedMinerals.DisplayDef(targetMineable);
        string letterLabel;
        TaggedString letterText;
        if (casket)
        {
            letterLabel = "LocalMineralScanner_LetterLabelFoundCasket".Translate() + ": " + display.LabelCap;
            letterText = "LocalMineralScanner_LetterFoundCasket".Translate(
                Find.ActiveLanguageWorker.WithIndefiniteArticle(display.label), worker.Named("FINDER"));
        }
        else
        {
            letterLabel = "LocalMineralScanner_LetterLabelFoundDeposit".Translate() + ": " + display.LabelCap;
            letterText = "LocalMineralScanner_LetterFoundDeposit".Translate(display.label, worker.Named("FINDER"));
        }
        Find.LetterStack.ReceiveLetter(letterLabel, letterText, LetterDefOf.PositiveEvent,
            new LookTargets(revealCells[revealCells.Count / 2], map));
        // The Unfog calls above already dirtied the fogged-minerals cache, so this reads the
        // post-reveal state.
        if (TargetExhausted)
        {
            Messages.Message(
                casket
                    ? "LocalMineralScanner_MessageExhaustedCaskets".Translate(PluralLabel(display))
                    : "LocalMineralScanner_MessageExhausted".Translate(display.label),
                parent,
                MessageTypeDefOf.TaskCompletion);
        }
    }

    // Returns the fogged cells of one chosen casket of the target def, or null when none
    // remain. Candidates rank by tier, lowest first, random within a tier: 0 fully fogged,
    // 1 partially fogged, each +2 when a fogged cell is the centre of a live warning trigger
    // (see the header). A casket with a dormant occupant on a fogged cell is skipped outright.
    private List<IntVec3> FindCasketToReveal(Map map)
    {
        HashSet<IntVec3> warningCentres = null;
        List<List<IntVec3>>[] tiers = new List<List<IntVec3>>[4];
        foreach (Thing casket in map.listerThings.ThingsOfDef(targetMineable))
        {
            List<IntVec3> fogged = null;
            bool disarmsWarning = false;
            bool blocked = false;
            int size = 0;
            foreach (IntVec3 cell in casket.OccupiedRect())
            {
                size++;
                if (!map.fogGrid.IsFogged(cell))
                {
                    continue;
                }
                if (HoldsDormantOccupant(cell, map))
                {
                    blocked = true;
                    break;
                }
                warningCentres ??= WarningTriggerCentres(map);
                if (warningCentres.Contains(cell))
                {
                    disarmsWarning = true;
                }
                (fogged ??= new List<IntVec3>()).Add(cell);
            }
            if (blocked || fogged == null)
            {
                continue;
            }
            int tier = (fogged.Count == size ? 0 : 1) + (disarmsWarning ? 2 : 0);
            (tiers[tier] ??= new List<List<IntVec3>>()).Add(fogged);
        }
        for (int i = 0; i < tiers.Length; i++)
        {
            if (tiers[i] != null)
            {
                return tiers[i].RandomElement();
            }
        }
        return null;
    }

    // True when something on the cell is asleep and would be woken by unfogging it: a pawn
    // whose mind is inactive (Pawn.Notify_Unfogged wakes its sleep-then-assault lord) or a
    // thing whose CompCanBeDormant is asleep (a dormant hive wakes when its own cell unfogs).
    private static bool HoldsDormantOccupant(IntVec3 cell, Map map)
    {
        List<Thing> things = cell.GetThingList(map);
        for (int i = 0; i < things.Count; i++)
        {
            Thing thing = things[i];
            if (thing is Pawn pawn && pawn.mindState?.Active == false)
            {
                return true;
            }
            if (thing.TryGetComp<CompCanBeDormant>()?.Awake == false)
            {
                return true;
            }
        }
        return false;
    }

    // Centre cells of the map's live destroy-if-unfogged RectTriggers (the vanilla ancient
    // danger warning; RectTrigger.Tick destroys the trigger once that one cell is unfogged).
    private static HashSet<IntVec3> WarningTriggerCentres(Map map)
    {
        HashSet<IntVec3> centres = new HashSet<IntVec3>();
        List<Thing> triggers = map.listerThings.ThingsOfDef(ThingDefOf.RectTrigger);
        for (int i = 0; i < triggers.Count; i++)
        {
            if (triggers[i] is RectTrigger trigger && trigger.destroyIfUnfogged)
            {
                centres.Add(trigger.Rect.CenterCell);
            }
        }
        return centres;
    }

    // Returns the fogged cells of one chosen deposit: a random fully-fogged cluster of the
    // target mineable, falling back to a random partially-fogged one; null when none remain
    // (the CanUseNow gate applies the same test, so DoFind only warns).
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
        ThingDef targetDisplay = MapComponent_FoggedMinerals.DisplayDef(targetMineable);
        Command_Action command = new Command_Action
        {
            defaultLabel = "CommandSelectMineralToScanFor".Translate() + ": " + targetDisplay.LabelCap,
            defaultDesc = "CommandSelectMineralToScanForDesc".Translate(),
            icon = targetDisplay.uiIcon,
            iconAngle = targetDisplay.uiIconAngle,
            iconOffset = targetDisplay.uiIconOffset,
            action = delegate
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (ThingDef mineable in foggedMinerals.TrackedDefs)
                {
                    ThingDef localMineable = mineable;
                    if (localMineable != targetMineable && !foggedMinerals.IsPresent(localMineable))
                    {
                        continue;
                    }
                    ThingDef display = MapComponent_FoggedMinerals.DisplayDef(localMineable);
                    string label = display.LabelCap;
                    if (!foggedMinerals.AnyFoggedDepositOf(localMineable))
                    {
                        TaggedString reason = MapComponent_FoggedMinerals.IsAncientCasket(localMineable)
                            && !LocalMineralScannerMod.Settings.scanAncientCaskets
                            ? "LocalMineralScanner_CasketsDisabled".Translate()
                            : "LocalMineralScanner_NoneUndiscovered".Translate();
                        options.Add(new FloatMenuOption(
                            label + " (" + reason + ")", null,
                            MenuOptionPriority.Default, null, null, 29f,
                            (Rect rect) => Widgets.InfoCardButton(rect.x + 5f, rect.y + (rect.height - 24f) / 2f, display)));
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
                    (Rect rect) => Widgets.InfoCardButton(rect.x + 5f, rect.y + (rect.height - 24f) / 2f, display)));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
        };
        yield return command;
    }
}
