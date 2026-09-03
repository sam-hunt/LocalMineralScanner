// Multi-operator variant of vanilla JobDriver_OperateScanner (decompile-verified): reserves
// the assigned operator cell (targetB) via ReserveSittableOrSpot and paths GotoCell/OnCell
// instead of reserving the building and pathing to its singular InteractionCell - the
// SchoolDesk (JobDriver_Lessontaking) pattern that lets two pawns work one building.
// The work toil mirrors vanilla exactly: comp.Operate(actor) every tick (our wrapper around
// CompScanner.Used, which IS the scan progress - see the comp for why), hardcoded 0.035
// Intellectual XP/tick, chair comfort, working sustainer, fail when CanUseNow flips (e.g.
// our "no deposits left" gate - progress is retained). Facing is manual (handlingFacing +
// FaceTarget) since the engine only auto-faces single-cell interaction buildings.

using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace LocalMineralScanner;

public class JobDriver_OperateLocalMineralScanner : JobDriver
{
    private Building Scanner => (Building)job.targetA.Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.ReserveSittableOrSpot(job.targetB.Cell, job, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        CompLocalMineralScanner scannerComp = Scanner.TryGetComp<CompLocalMineralScanner>();
        this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
        this.FailOnBurningImmobile(TargetIndex.A);
        this.FailOn(() => !scannerComp.CanUseNow);
        yield return Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell);
        Toil work = ToilMaker.MakeToil("MakeNewToils");
        work.tickAction = delegate
        {
            Pawn actor = work.actor;
            actor.rotationTracker.FaceTarget(Scanner);
            scannerComp.Operate(actor);
            actor.skills.Learn(SkillDefOf.Intellectual, 0.035f);
            actor.GainComfortFromCellIfPossible(1, chairsOnly: true);
        };
        work.handlingFacing = true;
        work.PlaySustainerOrSound(scannerComp.Props.soundWorking);
        work.AddFailCondition(() => !scannerComp.CanUseNow);
        work.defaultCompleteMode = ToilCompleteMode.Never;
        work.activeSkill = () => SkillDefOf.Intellectual;
        yield return work;
    }
}
