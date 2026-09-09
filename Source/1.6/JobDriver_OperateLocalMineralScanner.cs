// Multi-operator variant of vanilla JobDriver_OperateScanner (decompile-verified): reserves
// the assigned operator cell (targetB) via ReserveSittableOrSpot and paths GotoCell/OnCell
// instead of reserving the building and pathing to its singular InteractionCell. See
// WorkGiver_OperateLocalMineralScanner for why vanilla's pair can't seat two pawns.
// The work toil is vanilla's with two substitutions: the cell for the building, and
// comp.Operate(actor, speed) for CompScanner.Used(actor) (Operate IS the scan progress -
// see the comp). The rest is vanilla's: hardcoded 0.035 Intellectual XP/tick, chair
// comfort, working sustainer, fail when CanUseNow flips (e.g. our "no deposits left" gate -
// progress is retained) or the pawn leaves its seat (FailOnCannotTouch on targetB,
// vanilla's on the interaction cell). No facing code: Pawn_RotationTracker.UpdateRotation
// faces the job's rotateToFace target (default targetA, the scanner) whenever the pawn
// stands still, cardinal-adjacent to any footprint cell.
//
// Scan speed cache: Used reads worker.GetStatValue(ResearchSpeed) uncached every tick, a
// full StatWorker.GetValueUnfinalized walk of the pawn's skill, capacities, traits,
// hediffs, precepts, role, genes and apparel plus a nested WorkSpeedGlobal evaluation as a
// stat factor. Profiled (Dubs Performance Analyzer, two operators, 900 TPS) at ~10us of
// the toil's ~15us per operator-tick; the remaining ~4us is the XP and comfort calls. The
// engine's GetStatValue(stat, applyPostProcess, cacheStaleAfterTicks) path is inert here
// because Core does not mark ResearchSpeed <cacheable>, so the toil keeps its own: one
// speed per operator, re-read every SpeedRefreshTicks. It lives in the toil closure, so it
// dies with the job, needs no pruning, and starts cold when a load regenerates the toils.
// The window follows vanilla's own stale-stat reads (Pawn.VacuumResistance 60 ticks,
// Need_Food MaxNutrition 15); a level-up, injury or apparel change reaches the scan rate
// up to one second late. The MTB roll and the worked-days accumulator are unchanged.

using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace LocalMineralScanner;

public class JobDriver_OperateLocalMineralScanner : JobDriver
{
    private const int SpeedRefreshTicks = 60;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.ReserveSittableOrSpot(job.targetB.Cell, job, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        CompLocalMineralScanner scannerComp = job.targetA.Thing.TryGetComp<CompLocalMineralScanner>();
        this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
        this.FailOnBurningImmobile(TargetIndex.A);
        this.FailOn(() => !scannerComp.CanUseNow);
        yield return Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell);
        Toil work = ToilMaker.MakeToil("MakeNewToils");
        float speed = 1f;
        int nextSpeedRefreshTick = 0;
        work.tickAction = delegate
        {
            Pawn actor = work.actor;
            int tick = Find.TickManager.TicksGame;
            if (tick >= nextSpeedRefreshTick)
            {
                speed = scannerComp.ScanSpeedOf(actor);
                nextSpeedRefreshTick = tick + SpeedRefreshTicks;
            }
            scannerComp.Operate(actor, speed);
            actor.skills.Learn(SkillDefOf.Intellectual, 0.035f);
            actor.GainComfortFromCellIfPossible(1, chairsOnly: true);
        };
        work.PlaySustainerOrSound(scannerComp.Props.soundWorking);
        work.AddFailCondition(() => !scannerComp.CanUseNow);
        work.defaultCompleteMode = ToilCompleteMode.Never;
        work.FailOnCannotTouch(TargetIndex.B, PathEndMode.OnCell);
        work.activeSkill = () => SkillDefOf.Intellectual;
        yield return work;
    }
}
