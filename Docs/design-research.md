# LocalMineralScanner — Design Research (narrowed spec)

Companion to `vanilla-scanner-survey.md`. Spec as directed: a **2×2, minifiable,
pawn-operated surface-ore revealer**, unlocked by the existing `LongRangeMineralScanner`
research project, tunable to the same resource types as the long-range scanner, the same
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
  `MapFogged`, `ThingSpawned`/`ThingDespawned` and their `Building*` counterparts. Vanilla's
  `PathFinderMapData` subscribes to these for the same dirty-flag/lazy-recompute pattern.
- Resolved (`MapComponent_FoggedMinerals`): a per-def "any fogged cell remains" set behind a
  dirty flag, invalidated by `CellFogChanged`, `BuildingSpawned`/`BuildingDespawned` and
  `MapFogged`, rebuilt lazily from `listerThings.ThingsOfDef`. The cluster index and the
  periodic self-healing recompute considered here were not built: the events are exhaustive,
  and clusters are only needed at find time (`CompLocalMineralScanner.FindDepositToReveal`).
  `CanUseNow` returns a translated `AcceptanceReport` reason when the set is empty (shows as
  job-fail text). Progress pause is automatic: when no jobs are issued,
  `daysWorkingSinceLastFinding` (saved) simply stops moving.
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
branch since our gate already stops the work); add a standing inspect line; have the find
that exhausts the tuned mineral post the drill's `Messages.Message(TaskCompletion)`
targeting the scanner beside its find letter (the message is the vanilla "ran dry" channel;
a trailing letter paragraph was tried first and read as less consistent with the drill);
grey out exhausted minerals in the tuning menu (the disabled-`FloatMenuOption` idiom:
`Building_Bed`'s "UseMedicalBed (NotInjured)", `Zone_Fishing`). Rejected: auto-retune /
"any" option (no precedent, silently overrides a player setting); an Alert (none for drills
either; roofing is already covered by `Alert_CannotBeUsedRoofed`); a spawn-time warning like
`MessageGroundPenetratingScannerNoBedrock` (that condition is permanent, ours is fixed by
retuning and is visible in the inspect pane); announcing exhaustion by other routes (a pawn
mining or exploring into the last deposit) from a set diff in
`MapComponent_FoggedMinerals.MapComponentTick`: built, then dropped, because those routes
are rare next to the scanner's own find and the inspect line and job-fail text already
cover them, so the per-map tick and set diff bought little.

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
re-arms it, and a scanner that was installed, tuned and uninstalled before the save has
no load-time spawn to consume it, so the re-armed flag would override the tuning at
reinstall; the saved value overrides the re-arm. (A never-installed minified scanner -
trade or quest reward - saves the flag still set and correctly gets the fallback at its
first install; it cannot have been tuned, as `MinifiedThing.GetGizmos` does not forward
inner-comp gizmos.)

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
- Same effort per find as the long-range scanner: `scanFindMtbDays 4`,
  `scanFindGuaranteedDays 8`. The two-seat design, the 2x2 footprint and minifiability are
  the local unit's whole advantage; a cheaper find on top of them overtunes it. Two
  operators do not out-produce two single-seat buildings: each find is a renewal at
  min(Exp(λ), cap) with λ = Σspeed / mtbDays and cap = guaranteedDays / Σspeed, so λ·cap =
  guaranteedDays / mtbDays is speed-invariant and the long-run rate λ / (1 - e^(-λ·cap)) is
  linear in Σspeed. The shared accumulator only tightens the worst-case gap between finds.
- Minifiable: `<minifiedDef>MinifiedThing</minifiedDef>` + `uninstallWork` + `Mass`
  (deep-drill precedent).
- Resource tuning gizmo: mirror `CompLongRangeMineralScanner`'s FloatMenu over
  `GenStep_PreciousLump.mineables` = MineableGold, MineableSilver, MineableSteel,
  MineablePlasteel, MineableComponentsIndustrial, MineableUranium, MineableJade.
  Open question: `MineableComponentsIndustrial` never spawns as ambient scatter — filter to
  `building.mineableScatterCommonality > 0`, add an "any" option, or keep vanilla-identical.

## Design decisions (resolved)

1. **Multi-operator**: shipped the SchoolDesk-pattern stack in v0.1.0 rather than a
   single-operator v1 — `WorkGiver_OperateLocalMineralScanner`,
   `JobDriver_OperateLocalMineralScanner`, and `CompLocalMineralScanner.Operate` for the
   combined-speed inspect string. Vanilla's trio is reused only through `CompScanner`.
2. **Interaction offsets**: `(0,0,2)` and `(1,0,2)`, both on the `+z` face, so the operators
   stand where the commissioned art fronts its consoles (see ThingDef values below).
3. **Target list**: vanilla-identical (`GenStep_PreciousLump.mineables`, components
   included). Exhausted entries are greyed out rather than filtered; no "any mineral" option
   (see Exhaustion UX).
4. **Partially-fogged fallback**: as proposed — a cluster with ≥1 fogged cell, revealing only
   its fogged cells; "fully fogged" means every cell of the cluster is fogged.
5. **Letter target**: a single cell (the middle of the reveal list), not the cell list.
6. **Forced jobs**: a player-forced scan with both seats taken evicts a sitter, via
   `CanReserveSittableOrSpot(ignoreOtherReservations: forced)` and
   `ReservationManager.Reserve`'s `job.playerForced` branch — the same eviction vanilla's
   forced scanner job performs on its single seat.

## ThingDef values (Buildings_LocalMineralScanner.xml)

The def is modeled on vanilla's `LongRangeMineralScanner` (Core `Buildings_Misc.xml`). Where
it deviates, this is why:

- **`scanFindMtbDays` 4 / `scanFindGuaranteedDays` 8** match the long-range scanner exactly:
  same effort per find, so the local unit's advantages are the second seat, the 2x2 footprint
  and portability, not a cheaper find. Two operators halve the time to a find at no
  throughput gain over two separate buildings (rate-neutrality argument under Def-space
  tuning above).
- **`multipleInteractionCellOffsets` instead of `hasInteractionCell`:** two operator spots,
  each reserved per pawn by the custom WorkGiver/JobDriver (rationale in
  `WorkGiver_OperateLocalMineralScanner.cs`); `CompScanner.Used()` accumulating per worker
  per tick makes the second operator genuinely double progress. An even-width face has no
  centered cell (`Position` is the min corner), hence the x=0 and x=1 columns.
- **Spots on the +z face** (offsets authored for North rotation; the game rotates them),
  because the art fronts its consoles toward a north-side operator at North rotation, like
  vanilla's long-range scanner: vanilla pairs offset `(0,0,2)` with `defaultPlacingRot South`
  + `interactionCellIconReverse`, which we mirror. Authoring the spots on -z (the SchoolDesk
  convention) renders the sprite 180 degrees away from the operators.
  `GenAdj.AdjustForRotation`'s even-size center shifts keep both cells flush against the
  correct face at all four rotations (hand-verified).
- **`PlaceWorker_PreventInteractionSpotOverlap`** checks `multipleInteractionCellOffsets` on
  both the placed def and neighbors, so it covers both spots.
- **Minifiable** (deep-drill precedent: `MinifiedThing` + `uninstallWork` + `Mass`).
  `uninstallWork` 1500 scales the drill's 1800-of-10000 to our 8000 `WorkToBuild`.
  `terrainAffordanceNeeded Medium` sits between the drill's Light and the fixed scanners'
  Heavy, since a portable unit shouldn't demand a heavy foundation.
- **Power 400W:** between the deep drill (200) and the big scanners (700), for a smaller
  unit.
- **Texture:** commissioned art in the root `Textures/`, named after the defName per the
  vanilla `Things/Building/Misc/<DefName>_<rot>` idiom. Only north/south/west ship: the
  delivered side view has its consoles on -x, which is the WEST sprite under the game's
  convention (`Rot4.East` rotates the +z offsets onto +x, so the East face's consoles must
  sit on +x). `Graphic_Multi` synthesises the missing east sprite by mirroring west
  (`eastFlipped`), so one file serves both sides. `drawSize` 2.8: the sprite's full alpha
  extent (dish tip to console skirt) is 188 of the 256px canvas, so 2.8 keeps that inside
  ~2.05 cells; the chassis itself (163px) then sits at ~1.8 cells with a small margin, like
  vanilla 2x2 buildings. 3.1 (chassis exactly 2 cells) spilled past the footprint.
- **`uiOrder` 2998** slots the build gizmo directly after the long-range scanner (see
  Architect menu order below).
- **Roof rule** is a mod setting (on by default). `canBeUsedUnderRoof` feeds only vanilla's
  `Alert_CannotBeUsedRoofed`; the rule itself is `PlaceWorker_NotUnderRoofIfRequired`
  (vanilla's `PlaceWorker_NotUnderRoof` behind the setting) plus the comp's `CanUseNow`,
  which bypasses `CompScanner`'s hardcoded `RoofUtility` check when the setting is off.
- **Settings overwrite** `costList`, `Mass`, `minifiedDef`, `canBeUsedUnderRoof`,
  `scanFindMtbDays` and `scanFindGuaranteedDays` at startup and on settings-window close.
  The XML values are the defaults and must equal the `*Default` consts in
  `LocalMineralScannerSettings.cs`, whose header says which game reads make each write take
  effect live.

## Architect menu order (Patches/ArchitectMenuOrder.xml)

Goal: the local scanner sits directly after vanilla's long-range mineral scanner in
Architect > Misc.

How the grid orders build gizmos (decompile-verified, 1.6):

- `Designator_Build.Order` returns `BuildableDef.uiOrder` (float, default 2999f).
- `GizmoGridDrawer.DrawGizmoGrid` stable-sorts the tab's designators by Order
  (`GenCollection.SortStable`); `DesignationCategoryDef` and `ArchitectCategoryTab` do no
  ordering of their own.
- Ties keep `DefDatabase` order: Core, then DLCs, then mods. A modded def tied on `uiOrder`
  therefore always lands after every vanilla def sharing the value.

Core leaves six Misc buildings on the 2999 default, in this XML order: MoisturePump,
GroundPenetratingScanner, LongRangeMineralScanner, MultiAnalyzer, VitalsMonitor,
ToolCabinet. With no `uiOrder` of our own we'd tie at 2999 and sort after all six. There is
no value strictly between two tied defs, so the only way in is to lower the long-range
scanner below 2999 and place ours just above it.

All four defs share 2998 (ours is set in the ThingDef), so the tie-break that orders them
today keeps ordering them: the three vanilla defs stay first, in the same relative order,
and ours follows as the first mod def. One value keeps the cluster a single concept and lets
another mod join it by picking 2998. Staying just under the default keeps the trio as close
as possible to their original place among other mods' deliberately chosen values. The
moisture pump is included only because leaving it at 2999 would move it from ahead of the
scanners to behind ours.

Each def is patched via `PatchOperationConditional` so the operation replaces an existing
`uiOrder` rather than adding a duplicate node, which the XML loader rejects ("defines the
same field twice").
