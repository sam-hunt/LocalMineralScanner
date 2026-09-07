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

## Exhaustion UX (verified)

This scanner can run out of targets; its vanilla siblings cannot (the long-range scanner
spawns quests, the deep scanner conjures lumps). Survey of how vanilla handles a
player-owned work source that runs dry, and what we adopted:

| Precedent | Channel | When | Pauses/forbids? | Inspect line? |
| --- | --- | --- | --- | --- |
| `CompScanner` roofed / `CompDeepScanner` no bedrock | `CanUseNow` reason → `JobFailReason` ("Cannot scan: …"), `FailOn` ends the job | every job attempt | job refused; accumulator frozen | no |
| `CompDeepScanner` spawned on no-bedrock map | `Messages.Message(NegativeEvent, historical:false)` | once, at spawn | — | no |
| `CompDeepDrill` last portion drained, fallback stone exists | `Messages.Message(TaskCompletion)` "…drill automatically forbidden…" + `SetForbidden(true)` on drained neighbours | edge-triggered | forbid (else pawns keep drilling chunks) | `DeepDrillNoResources` |
| `CompDeepDrill` last portion drained, no fallback | `Messages.Message(TaskCompletion)` only | edge-triggered | `CanDrillNow` false, no forbid | `DeepDrillNoResources` |
| `Zone_Fishing` under population target | inspect "CannotFish (reason)" + greyed float-menu option | standing | work flag false | yes |
| `Bill` missing ingredients | `JobFailReason` (forced only) + 500–600 tick re-search cooldown | standing | — | no |
| `Bill_Production` repeat count hits 0 | `Messages.Message(TaskCompletion)` | edge-triggered | bill stops | no |
| Research bench, no project | standing `Alert_NeedResearchProject` | standing | — | "Current project: None" |
| `CompToxifier` cannot pollute | standing `Alert_ToxifierGeneratorStopped` | standing | — | no (silent) |
| `IncidentWorker_Raid*`, `CompBiosculpterPod_*` | conditional trailing `"\n\n" + extra` paragraph on the letter | with the letter | — | — |

Findings: vanilla's term is "exhausted"; every "ran dry" *event* is a `TaskCompletion`
message; standing conditions get an inspect line or an Alert, never both for the same
thing; no vanilla scanner offers an "any mineral" target or retunes itself, and the only
automatic resource switch (`DeepDrillUtility.GetNextResource`) concerns a resource the
player never chose. `CompScanner.CompInspectStringExtra` never renders `CanUseNow`.
Forced-job fail text is `CapitalizeFirst()`ed by `FloatMenuOptionProvider_WorkGivers`, so
fragment keys may be lower-case (vanilla mixes: `CannotUseScannerRoofed` = "Blocked by
roof", `FishingSpotUnderTargetPopulation` = "below minimum fish population").

Adopted (see `CompLocalMineralScanner.cs` header): keep the `CanUseNow` gate (identical
plumbing to the roofed/no-bedrock reasons; no auto-forbid, matching the drill's no-fallback
branch since our gate already stops the work); add a standing inspect line; note "that was
the last deposit" as a trailing paragraph of the find letter that exhausted the mineral
(one persistent notification instead of a letter plus a fading message on the same tick);
grey out exhausted minerals in the tuning menu (the disabled-`FloatMenuOption` idiom:
`Building_Bed`'s "UseMedicalBed (NotInjured)", `Zone_Fishing`). Rejected: auto-retune /
"any" option (no precedent, silently overrides a player setting); an Alert (none for drills
either; roofing is already covered by `Alert_CannotBeUsedRoofed`); a spawn-time warning like
`MessageGroundPenetratingScannerNoBedrock` (that condition is permanent, ours is fixed by
retuning and is visible in the inspect pane).

Default target: gold for vanilla parity with the long-range scanner, but
`GenStep_ScatterLumpsMineable` weights gold at 0.07 of 2.405 (~2.9%) with 4–15 lumps per
10k cells by hilliness, so a 250×250 small-hills map averages ~1.5 gold lumps and a fresh
scanner would often start exhausted. Adopted: on its first spawn only, an exhausted scanner
tunes itself to the most valuable undiscovered mineral. Precedent: `Zone_Growing.PlantDefToGrow`
resolves its default from map state (toxipotato when entirely polluted) the first time it is
read, and `DeepDrillUtility.GetNextResource` derives the drill's resource from the map; both
are defaults chosen where the player has made no choice. Value ordering is per deposit cell,
`mineableThing.BaseMarketValue * building.mineableYield`, the product `GenStep_PreciousLump`
sizes lumps by (gold 400, plasteel 360, uranium 240, jade 200, steel 76, components 64,
silver 40); raw market value would rank components (32) above gold (10). The one-shot flag
is set in `Initialize` (runs on `PostMake` and on load) and consumed by the first
`PostSpawnSetup`, so load (`respawningAfterLoad`) and reinstall (flag already consumed)
never override a saved tuning. The flag is scribed because the load-time `Initialize`
re-arms it, and a never-installed minified scanner (trade/quest reward) has no load-time
spawn to consume it; the saved value overrides the re-arm.

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
