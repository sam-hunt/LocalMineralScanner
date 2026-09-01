// Multi-operator variant of vanilla WorkGiver_OperateScanner (decompile-verified). Vanilla
// hardcodes a building-level maxPawns=1 reservation, which refuses the second operator
// outright, and paths via PathEndMode.InteractionCell, which only ever resolves the
// SINGULAR Thing.InteractionCell (with multipleInteractionCellOffsets set it falls back to
// an arbitrary adjacent cell). This giver instead assigns each pawn a specific free cell
// from Thing.InteractionCells - the Biotech SchoolDesk pattern (SchoolUtility +
// JobDriver_Lessontaking): validate with CanReserveSittableOrSpot, carry the assigned cell
// in targetB, and let the driver reserve the CELL rather than the building, so operators
// never contend on the same reservation target.
//
// The building itself is intentionally never reserved: mirroring SchoolDesk, mutually
// exclusive uses (uninstall, deconstruct) end our job via FailOnDespawnedNullOrForbidden
// when they land. def.scannerDef is vanilla WorkGiverDef API, reused so this class stays
// generic over any future CompScanner building of ours.

using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace LocalMineralScanner;

public class WorkGiver_OperateLocalMineralScanner : WorkGiver_Scanner
{
    public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(def.scannerDef);

    public override PathEndMode PathEndMode => PathEndMode.Touch;

    public override Danger MaxPathDanger(Pawn pawn) => Danger.Deadly;

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (t.Faction != pawn.Faction)
        {
            return false;
        }
        if (!(t is Building building) || building.IsBurning())
        {
            return false;
        }
        CompScanner comp = building.TryGetComp<CompScanner>();
        if (comp == null)
        {
            return false;
        }
        AcceptanceReport canUseNow = comp.CanUseNow;
        if (!canUseNow)
        {
            if (!canUseNow.Reason.NullOrEmpty())
            {
                JobFailReason.Is(canUseNow.Reason);
            }
            return false;
        }
        return TryFindFreeOperatorCell(pawn, building, out _);
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (!TryFindFreeOperatorCell(pawn, (Building)t, out IntVec3 cell))
        {
            return null;
        }
        Job job = JobMaker.MakeJob(LocalMineralScannerDefOf.OperateLocalMineralScanner, t, 1500, checkOverrideOnExpiry: true);
        job.targetB = cell;
        return job;
    }

    private static bool TryFindFreeOperatorCell(Pawn pawn, Building building, out IntVec3 cell)
    {
        List<IntVec3> cells = building.InteractionCells;
        for (int i = 0; i < cells.Count; i++)
        {
            IntVec3 candidate = cells[i];
            if (!candidate.InBounds(pawn.Map)
                || !candidate.Standable(pawn.Map)
                || candidate.IsForbidden(pawn)
                || !pawn.CanReserveSittableOrSpot(candidate)
                || !pawn.CanReach(candidate, PathEndMode.OnCell, Danger.Deadly))
            {
                continue;
            }
            cell = candidate;
            return true;
        }
        cell = IntVec3.Invalid;
        return false;
    }
}
