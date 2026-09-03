# LocalMineralScanner — Design Research (narrowed spec)

Companion to `vanilla-scanner-survey.md`. Spec as directed: a **2×2, minifiable,
pawn-operated surface-ore revealer**, unlocked by the existing `LongRangeMineralScanner`
research project, tunable to the same resource types as the long-range scanner, ~50% of its
per-find effort. On a find: unfog one contiguous block of fogged mineral cells on the
current map (fully fogged nodes first, partially fogged as fallback) and pop a non-pausing
blue letter with a jump-to link. All claims below verified by decompile (see survey doc for
method).

## Unfog mechanics (verified)

- `map.fogGrid.Unfog(cell)` is the whole reveal API: clears that cell's fog bit, dirties
  map-mesh (FogOfWar|Things), roof and temperature drawers itself, deletes stale `Mine`
  designations on non-mineable cells, fires `map.events.Notify_CellFogChanged(c, false)`,
  and calls `Notify_Unfogged()` on things there (harmless for `Mineable`). If the cell holds
  a `Fillage.Full` multi-cell thing it unfogs that thing's whole `OccupiedRect`.
- It does **not** cascade. `FloodUnfogAdjacent` / `FloodFillerFog.FloodUnfog` do — and must
  NOT be used, or the reveal spills through open floor beyond the lump. Loop
  `Unfog(cell)` over exactly the cluster's cells; no manual dirtying needed.
- Every vanilla mineable rock inherits `RockBase` `fillPercent=1` ⇒ `MakeFog=true`: ore
  cells are themselves fog blockers, so revealing only the lump's cells shows the ore faces
  without opening the surrounding cavern. Fog never affects passability or Thing data.
- Vanilla precedent for direct single-cell unfog: an ancient-security-terminal comp
  (Odyssey) unfogs its own position; debug UnfogRect loops `Unfog` over a rect.

## Letter idiom (verified)

`LetterDefOf.PositiveEvent`: color (120,176,216) blue, `bounce=false`, `pauseMode=AnyLetter`
default ⇒ pauses only if the player opted into "pause on any letter". Exactly what
`CompDeepScanner.DoFind` uses. Jump-to needs no Thing:

```csharp
Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.PositiveEvent,
    new LookTargets(clusterCenterCell, map));   // cell+map ctor; a cell LIST also works
```

## "No fogged nodes left" gating (verified)

- Call budget: `WorkGiver_OperateScanner.HasJobOnThing` → `CompScanner.CanUseNow` runs from
  idle pawns' job search, up to ~60×/sec per idle pawn. `CanUseNow` must be O(1).
- **`Verse.MapEvents` provides public C# events — no Harmony needed**:
  `CellFogChanged (Action<IntVec3,bool>)` fired by every `Unfog`/`Refog` (mining through
  rock included, via `Building.DeSpawn → Notify_FogBlockerRemoved → …Unfog`), plus
  `MapFogged`, `ThingSpawned`, `ThingDespawned`. Vanilla's `PathFinderMapData` subscribes to
  these for the same dirty-flag/lazy-recompute pattern.
- Plan: a `MapComponent` owns the cache — set of fogged mineable cells / cluster index —
  incrementally maintained from `ThingSpawned`/`ThingDespawned` (def.mineable) and
  `CellFogChanged`; `CanUseNow` reads a cached bool and returns a translated
  `AcceptanceReport` reason when false (shows as job-fail tooltip). Defensive full recompute
  on a rare tick interval only as self-healing. Progress pause is automatic: when no jobs
  are issued, `daysWorkingSinceLastFinding` (saved) simply stops moving.
- Enumeration without scans: no `ThingRequestGroup` covers mineables
  (`BuildingArtificial` explicitly excludes resource rock); use
  `map.listerThings.ThingsOfDef(def)` (maintained dict lookup) per targeted mineable def.
- Cluster logic (ours to write): flood-fill contiguous same-def mineable cells
  (`Designator_MineVein` precedent walks contiguous same-ThingDef edifices), classify fully
  vs partially fogged, prioritize fully fogged.

## Interaction cell mechanics (verified)

- `Thing.InteractionCell = def.interactionCellOffset.RotatedBy(rot) + Position`. Offset is
  authored for North and auto-rotated: E `(z,y,-x)`, S `(-x,y,-z)`, W `(-z,y,x)`. **Not
  separately configurable per rotation** in def-space — one offset, rotated.
- **Even-size quirk:** `Position` is the footprint's _min-corner_ cell for even sizes
  (odd sizes center it). The offset is added to `Position` with no size compensation, so a
  2×2 has no centered interaction column — offset x picks the min (`x=0`) or max (`x=1`)
  column of a face. Vanilla 2×2 `hasInteractionCell` precedents: mortar base `(0,0,-1)`,
  SerumCentrifuge `(0,0,-1)`, MechStabilizer `(0,0,2)`, GeneExtractor `(1,0,2)`.
- `interactionCellIcon` (e.g. `DiningChair`) + `interactionCellIconReverse` drive the ghost
  icon (`GenDraw.DrawInteractionCells`); keep `PlaceWorker_PreventInteractionSpotOverlap`
  (it checks all cells incl. `multipleInteractionCellOffsets` of neighbors).
- Reservations: scanner jobs reserve the _building_ (`maxPawns=1`, hardcoded in both
  `WorkGiver_OperateScanner.HasJobOnThing` and `JobDriver_OperateScanner`'s
  `TryMakePreToilReservations`). `ReservationManager` additionally cross-checks that no one
  else has separately reserved the interaction cell.

## Multi-operator (verified — possible, but not def-only)

- `ThingDef.multipleInteractionCellOffsets` (`List<IntVec3>`) is real 1.6 API, **mutually
  exclusive with `hasInteractionCell`** (ConfigErrors). Precedent: Biotech **SchoolDesk**
  (2×1, offsets `[(1,0,-1) student, (0,0,-1) teacher]`), consumed via
  `Thing.InteractionCells[i]`; each pawn reserves its _cell_ (`ReserveSittableOrSpot`) and
  paths `GotoCell(spot, PathEndMode.OnCell)` — the desk Thing is never reserved.
- The vanilla scanner trio cannot use it: `PathEndMode.InteractionCell` resolves only the
  singular `InteractionCell` (with multipleOffsets + no hasInteractionCell it falls back to
  an arbitrary adjacent cell); both WorkGiver and JobDriver hardcode building-level
  `maxPawns=1`; `CompScanner`'s `lastUserSpeed`/`lastScanTick` are single scalars (two
  concurrent `Used()` callers clobber them, and progress would double silently).
- Cost of true multi-operator: custom WorkGiver (pick an unclaimed cell index per pawn),
  custom JobDriver (reserve that cell, goto OnCell), per-slot comp state. Mechanically
  proven, but it forfeits most of the "reuse vanilla trio unchanged" win.

## Def-space tuning (from spec)

- Research: `<researchPrerequisites><li>LongRangeMineralScanner</li></...>` — no new project.
- ~50% effort per find: `scanFindMtbDays 2`, `scanFindGuaranteedDays 4` (long-range is 4/8).
- Minifiable: `<minifiedDef>MinifiedThing</minifiedDef>` + `uninstallWork` + `Mass`
  (deep-drill precedent).
- Resource tuning gizmo: mirror `CompLongRangeMineralScanner`'s FloatMenu over
  `GenStep_PreciousLump.mineables` = MineableGold, MineableSilver, MineableSteel,
  MineablePlasteel, MineableComponentsIndustrial, MineableUranium, MineableJade.
  Open question: `MineableComponentsIndustrial` never spawns as ambient scatter — filter to
  `building.mineableScatterCommonality > 0`, add an "any" option, or keep vanilla-identical.

## Open design decisions

1. **Multi-operator**: ship single-operator v1 (pure vanilla-trio reuse) vs invest in the
   SchoolDesk-pattern stack now. Recommendation: single operator v1; the multi-op stack is
   additive later.
2. **Interaction offset column** for the 2×2 (x=0 vs x=1) and face (front `-z` vs back `+z`).
3. **Target list semantics** (vanilla-identical vs scatter-filtered vs "any mineral").
4. **Partially-fogged fallback definition**: cluster with ≥1 fogged cell; reveal only its
   fogged cells. "Fully fogged" = every cell of the cluster fogged.
5. Whether the letter should target the cluster's cell list (multi-cell highlight) or just
   its center.
