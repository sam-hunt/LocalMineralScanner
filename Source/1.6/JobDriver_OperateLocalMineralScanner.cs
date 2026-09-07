// Multi-operator variant of vanilla JobDriver_OperateScanner (decompile-verified): reserves
// the assigned operator cell (targetB) via ReserveSittableOrSpot and paths GotoCell/OnCell
// instead of reserving the building and pathing to its singular InteractionCell. See
// WorkGiver_OperateLocalMineralScanner for why vanilla's pair can't seat two pawns.
// The work toil is vanilla's with the cell substituted for the building: comp.Operate(actor)
// every tick (our wrapper around CompScanner.Used, which IS the scan progress - see the
// comp), hardcoded 0.035 Intellectual XP/tick, chair comfort, working sustainer, fail when
// CanUseNow flips (e.g. our "no deposits left" gate - progress is retained) or the pawn
// leaves its seat (FailOnCannotTouch on targetB, vanilla's on the interaction cell). No
// facing code: Pawn_RotationTracker.UpdateRotation faces the job's rotateToFace target
// (default targetA, the scanner) whenever the pawn stands still, cardinal-adjacent to any
// footprint cell.

using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace LocalMineralScanner;

public class JobDriver_OperateLocalMineralScanner : JobDriver
{
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
        work.tickAction = delegate
        {
            Pawn actor = work.actor;
            scannerComp.Operate(actor);
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
