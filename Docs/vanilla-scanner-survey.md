# Vanilla Scanner & Mineral API Survey (RimWorld 1.6)

Feasibility groundwork for **LocalMineralScanner**. Everything below was verified by
decompiling the local install's `Assembly-CSharp.dll` (source of truth) and reading
`Data/*/Defs` — not from wiki/memory. Decompiled with `ilspycmd`; see CLAUDE.md § Debugging.

## TL;DR feasibility

A local-map mineral scanner is **highly feasible with very little new C#**. Vanilla ships a
generic, reusable scanner stack (`CompScanner` + `WorkGiver_OperateScanner` +
`JobDriver_OperateScanner`, all def-driven) whose ground-penetrating variant is already a
"local mineral scanner" in the _generate_ sense. The one real design decision is the payload
(`DoFind` override): **generate** deep lumps like vanilla, or **reveal** existing minerals —
and the two mineral domains behave oppositely:

| Domain                           | Representation                                                | Exists before scanning?                                                                                           | Fog interaction                                                               |
| -------------------------------- | ------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------- |
| Surface ore (MineableSteel etc.) | Real spawned `Mineable : Building` things                     | Yes — placed at mapgen by `GenStep_ScatterLumpsMineable`                                                          | Fog hides rendering only; things are always enumerable via `map.listerThings` |
| Deep resources                   | `Verse.DeepResourceGrid` (per-cell `ushort` def-hash + count) | **No** — grid starts zeroed; nothing populates it at mapgen. `CompDeepScanner.DoFind` _conjures_ lumps on success | Grid is fog-agnostic                                                          |

So "reveal what's underground" has no true hidden answer to reveal (deep veins don't exist
until rolled), while "reveal surface ore through fog / map-wide" is trivially readable data
that vanilla merely chooses not to surface.

## 1. The CompScanner mechanism (base class contract)

- `CompScanner` (abstract) + `CompProperties_Scanner`: fields `scanFindMtbDays`,
  `scanFindGuaranteedDays` (default `-1` = disabled — **always set positive**, the inspect
  string divides by it unconditionally and shows garbage otherwise), `scanSpeedStat`,
  `soundWorking`.
- **No tick hook on the comp.** Progress happens only in `Used(Pawn)`, called every tick by
  the job's work toil. Each call adds `scanSpeed/60000` days to
  `daysWorkingSinceLastFinding`; every 59 ticks (`IsHashIntervalTick(59)`) it rolls
  `Rand.MTBEventOccurs(mtbDays / scanSpeed, 60000, 59)`, OR forces success when accumulated
  days ≥ `scanFindGuaranteedDays`. Worker speed compounds through both the accumulator and
  the roll. No pawn ⇒ no progress.
- `CanUseNow` (virtual `AcceptanceReport`): spawned + powered (if `CompPowerTrader` present)
  - not roofed (`RoofUtility.IsAnyCellUnderRoof`, hardcoded) + not forbidden (if comp
    present) + player faction. `CompDeepScanner` adds `Map.Biome.hasBedrock`.
- Saved state: `daysWorkingSinceLastFinding`, `lastUserSpeed`, `lastScanTick` (floats).
- Free from the base: dev "Find now" gizmo, inspect string (worker speed %, avg interval,
  % to guaranteed find).
- **Subclass contract: override `protected abstract void DoFind(Pawn worker)`** (plus
  optionally `CanUseNow`, `PostSpawnSetup`, `PostDrawExtraSelectionOverlays`).

### Work pipeline — zero new C# needed

- `WorkGiverDef` has a dedicated `scannerDef` field. `WorkGiver_OperateScanner` targets
  `ThingRequest.ForDef(def.scannerDef)`, path-ends at the interaction cell, checks
  reservation + `CanUseNow` + not burning, and issues `JobDefOf.OperateScanner` with a
  hardcoded 1500-tick expiry. Vanilla ships two WorkGiverDefs (both `workType=Research`)
  differing only in `<scannerDef>`; a new scanner needs only a third.
- `JobDriver_OperateScanner`: goto interaction cell → endless work toil whose `tickAction`
  calls `comp.Used(actor)`, grants **hardcoded** 0.035 Intellectual XP/tick, comfort from
  chairs. Reusable as-is; write a custom driver only for different skill/XP wiring.
- The shared `OperateScanner` JobDef is reused by both vanilla scanners — reuse it too.

### Vanilla payloads (`DoFind` implementations)

- **`CompDeepScanner`** (ground-penetrating scanner): random non-edge cell
  (`CellFinderLoose.TryFindRandomNotEdgeCellWith(10, CanScatterAt, …)`) → resource picked by
  `DefDatabase<ThingDef>.AllDefs.RandomElementByWeight(d => d.deepCommonality)` (**global**
  pool — any def with `deepCommonality > 0` qualifies, modded ores compose automatically) →
  `GridShapeMaker.IrregularLump(center, map, CeilToInt(deepLumpSizeRange.RandomInRange))` →
  per valid cell `map.deepResourceGrid.SetAt(cell, def, def.deepCountPerCell)` (default 300)
  → `LetterDefOf.PositiveEvent` with `LookTargets`. `CanScatterAt`: not impassable water,
  terrain affordance satisfies `ThingDefOf.DeepDrill.terrainAffordanceNeeded`, cell not
  already occupied in the grid, not in no-build edge area.
- **`CompLongRangeMineralScanner`**: no world logic in C# — builds a `Slate` (map,
  targetMineable, worker) and hands off to QuestGen script
  `LongRangeMineralScannerLump` + letter. Target-mineral gizmo pulls candidates from
  `GenStep_PreciousLump.mineables`. Precedent if any find should be off-map/quest-shaped.
- **`CompOrbitalScanner` (Odyssey) is a false cousin**: extends `ThingComp` directly,
  passive (real `CompTick`, no pawn labor), quest-signal system via
  `OrbitalScannerWorldComponent`. Not a `CompScanner`; don't generalize from it.

## 2. DeepResourceGrid plumbing

- Two `ushort[]` per map: `defGrid` (`ThingDef.shortHash`, 0 = none) + `countGrid`.
  API: `ThingDefAt(cell)`, `CountAt(cell)`, `SetAt(cell, def, count)` (count 0 clears def;
  clamps/errors outside ushort; the only place that dirties the `CellBoolDrawer` — direct
  array pokes desync the overlay). Saved via `MapExposeUtility.ExposeUshort`
  (`"defGrid"`/`"countGrid"`); short hashes are stable per def name, so modded defs are
  save-safe. One def+count per cell — no stacking.
- **Only two writers in the game**: `CompDeepScanner.DoFind` (discovery) and
  `CompDeepDrill.TryProducePortion` (drain). No mapgen genstep pre-populates it.
  `TileMutatorWorker_MineralRich` (Odyssey) only biases _surface_ scatter.
- **Overlay is opt-in, gated by `AnyActiveDeepScannersOnMap()`** (≥1 player-owned, powered
  `CompDeepScanner`). Vanilla triggers `MarkForDraw()` from:
  `CompDeepScanner.PostDrawExtraSelectionOverlays` (scanner selected),
  `PlaceWorker_ShowDeepResources.DrawGhost` (placing drill/scanner ghost), and
  `DeepResourcesOnGUI` mouse-hover readout (single-selected scanner/drill). Cell color is
  depletion-tinted (`count / deepCountPerCell`). A mod can satisfy the gate by shipping its
  own `CompDeepScanner`(-subclass) comp — the vanilla overlay/tooltip machinery then just
  works — or bypass it by calling `map.deepResourceGrid.MarkForDraw()` itself / driving its
  own `CellBoolDrawer`.
- **`CompDeepDrill`**: portion work `10000` scaled by `DeepDrillingSpeed`, yield by
  `MiningYield`; `DeepDrillUtility.GetNextResource` scans a **21-cell radial** around the
  drill for the first grid hit, else falls back to `GetBaseResource` (per-cell-seeded
  deterministic stone-chunk bedrock fallback, `Rand.Seed = cell.GetHashCode()`, not stored —
  cheap to recompute for display). Depletion auto-forbids nearby exhausted drills and
  messages `DeepDrillExhausted(NoFallback)`.
- Risk precedent: `IncidentWorker_DeepDrillInfestation` + `CompProperties_CreatesInfestations`
  - `StorytellerComp_DeepDrillInfestation` tie infestations to drilling _activity_.

## 3. Surface minerals

- Placed at mapgen by `GenStep_RocksFromGrid` → `GenStep_ScatterLumpsMineable`: count per
  10k cells keyed off `TileInfo.HillinessForOreGeneration` (Flat 4 → Impassable 16), def
  weighted by `building.mineableScatterCommonality`, lump size
  `building.mineableScatterLumpSizeRange` (default 20–40), spawned as real `Mineable`
  buildings via `GridShapeMaker.IrregularLump`.
- Key def fields: `ThingDef.mineable`, `building.isNaturalRock` / `isResourceRock` /
  `mineableThing` / `mineableYield` (+ `EffectiveMineableYield` applying difficulty factor) /
  `mineableDropChance` / `veinMineable`.
- **Fog hides rendering only** — `FogGrid` is a bit array over draw/reveal, never Thing
  data. Map-wide enumeration of ore via `map.listerThings` is always possible; surfacing it
  in UI is the design question, not a technical one. `Designator_Mine` already allows
  designating fogged cells blind; `Designator_MineVein` flood-fills contiguous same-def
  cells but stops at fog. `MineStrikeManager.CheckStruckOre` ("struck ore!" message) reacts
  to adjacent cells after actual digging — feedback flavor, not a reveal hook.

## 4. Vanilla def reference (exact values)

- **GroundPenetratingScanner** (Core `Buildings_Misc.xml`): 3×3, `BuildingBase`, 700W
  `CompPowerTrader`, Forbiddable/Breakdownable/Flickable, `canBeUsedUnderRoof=false`,
  `PlaceWorker_NotUnderRoof` + `PlaceWorker_PreventInteractionSpotOverlap`, interaction cell
  (0,0,2) with `DiningChair` icon, `terrainAffordanceNeeded=Heavy`, construction skill 8,
  cost 150 Steel + 4 ComponentIndustrial + 1 ComponentSpacer.
  Comp: `CompProperties_ScannerMineralsDeep` — `scanSpeedStat=ResearchSpeed`,
  `scanFindMtbDays=3`, `scanFindGuaranteedDays=6`, `soundWorking=ScannerGroundPenetrating_Ambience`.
- **LongRangeMineralScanner**: same shape; `scanFindMtbDays=4`, `scanFindGuaranteedDays=8`,
  cost 200 Steel + 6 CI + 2 CS.
- **DeepDrill** (Core `Buildings_Production.xml`): 1×1 minifiable, 200W,
  `CompProperties_DeepDrill` (bare marker) + `CompProperties_CreatesInfestations`,
  `PlaceWorker_DeepDrill`, research `DeepDrilling`.
- **Research chain** (all industrial, hi-tech bench): `MicroelectronicsBasics` →
  `DeepDrilling` (1000) → `GroundPenetratingScanner` (1000); `LongRangeMineralScanner`
  (2000) hangs off MicroelectronicsBasics with hidden prereq `Machining`.
- **WorkGiverDefs** (Core `WorkGivers.xml`): `GroundPenetratingScan` / `LongRangeScan`,
  `giverClass=WorkGiver_OperateScanner`, `workType=Research`, `priorityInType=50`,
  Manipulation required, `canBeDoneByMechs=false`, `<scannerDef>` per building.
- **JobDefs** (`Jobs_Work.xml`): `OperateScanner` → `JobDriver_OperateScanner`,
  `reportString "scanning at TargetA."`, `allowOpportunisticPrefix`.
- **Deep-drill fields on resource defs** (`Items_Resource_Stuff.xml`) —
  `deepCommonality / deepCountPerPortion / deepLumpSizeRange`:
  Steel 4/45/20–30 · Plasteel 1/10/2–10 · Uranium 1/15/4–10 · Silver 0.5/70/2–10 ·
  Gold 0.5/8/1–4 · Jade 0.5/15/1–4. (`deepCountPerCell` defaults to 300; ConfigErrors
  enforces commonality ⇒ lump range ⇒ countPerPortion consistency.)
- **Key translation keys**: `CannotUseScannerRoofed`, `CannotUseScannerNoBedrock`,
  `MessageGroundPenetratingScannerNoBedrock`, `LetterDeepScannerFoundLump` (+Label),
  `LetterFoundPreciousLump`, `DeepDrillNoResources`, `DeepDrillExhausted(NoFallback)`,
  `UserScanAbility`, `ScanAverageInterval`, `ScanningProgressToGuaranteedFind`.
- **Stats**: `ResearchSpeed` (scan speed via data-driven `scanSpeedStat`),
  `DeepDrillingSpeed`, `MiningYield`, `MiningSpeed`. Skill XP (Intellectual, 0.035/tick) is
  hardcoded in the JobDriver.

## 5. Design forks for LocalMineralScanner

1. **Generate deep lumps** (pure `CompDeepScanner` variant — different tuning/pool/lump
   shapes): smallest possible mod; possibly XML-only if vanilla behavior suffices, one small
   comp subclass otherwise. Composes with vanilla drills, overlay, and modded ores for free.
2. **Reveal surface ore map-wide / through fog**: data is free (`listerThings`); work is all
   presentation — an overlay (`CellBoolDrawer` per resource type?), letters, or auto-mine
   designations. `Designator_Mine`'s fogged-cell allowance is precedent that acting on
   unseen ore isn't considered cheating by vanilla.
3. **Quest-shaped local finds**: QuestGen script à la `LongRangeMineralScannerLump` with a
   local-map slate — heavier authoring, XML-based.
4. Hybrid tuning knobs all live on defs (`scanFindMtbDays`, `deepCommonality` pool,
   `deepCountPerCell`), so balance iteration is XML-only in every fork.

Gotchas to carry into design: guaranteedDays must be positive; deep grid holds one def per
cell; only `SetAt` dirties the overlay drawer; overlay is invisible without a powered
gate-satisfying comp; global `deepCommonality` pool means our defs would also enter the
vanilla scanner's pool (override `ChooseLumpThingDef`-equivalent in a custom `DoFind` if
that's undesired).
