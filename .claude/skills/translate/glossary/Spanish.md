# Spanish (Castilian) — Local Mineral Scanner terminology

Machine-assisted, generated 2026-09-09. Pending native review (see the list
below). Grounded against Core's Castilian tar only (no domain DLC).

## Coined terms

| English | Spanish | Grounding / rationale |
|---|---|---|
| local mineral scanner (ThingDef label, Workshop title) | escáner de minerales local | `LongRangeMineralScanner.label` = "escáner de minerales de largo alcance"; "escáner de minerales" is vanilla's anchor phrase, "local" swaps in for "de largo alcance" |
| tuned to (gizmo, reused in place) | Sintonizado para | vanilla `CommandSelectMineralToScanFor` — not retranslated; our own prose uses the verb `sintonizar` (`sintonizarse para`, `sintonízalo con`) instead of repeating the gizmo phrase verbatim |
| twin consoles / two operators | consolas gemelas / (dos) operadores | no vanilla noun for "operator" exists; "operador" matches the `operar` verb already used in the `WorkGiverDef` label |
| undiscovered (deposit / area) | sin descubrir / sin revelar | `Undiscovered` Keyed key = "Sin descubrir" (used for `_NoFoggedDeposits`/`_NoneUndiscovered`); `CannotPlaceInUndiscovered` = "áreas sin revelar" (used for "an undiscovered area", since that key is specifically about fogged terrain) |
| deposit (of a mineral) | depósito | `Script_LongRangeMineralScannerLump` quest text: one `lump->` option is "Un depósito de [mineral]" |
| guaranteed find | descubrimiento garantizado | `ScanningProgressToGuaranteedFind` = "Progreso hasta descubrimiento garantizado" — reused rather than coining "hallazgo garantizado" |
| combined scanning speed | velocidad de escaneo combinada | "escaneo" built from `escanear`, the verb vanilla's `WorkGiverDef.verb`/`.gerund` use (`LongRangeScan.verb` = "escanear en") |
| Workshop title | Escáner de minerales local | byte-identical to `LocalMineralScanner_SettingsCategory` |
| ore vein (Workshop copy, "contiguous ore vein") | veta de mineral | no vanilla term found; "veta" is standard Spanish mining vocabulary for a mineral vein, paired with the existing "mineral" noun |
| mineable ore (type) (Workshop copy, "any mineable ore type") | mineral extraíble | describes the post-update any-ore tuning (no longer a fixed vanilla list); "extraíble" avoids inventing an adjective on "minable" |

## Grounding decisions

- `WorkGiverDef.verb`/`.gerund` mirror vanilla's own "escanear en" for both
  fields (`LongRangeScan.verb`/`.gerund`), unlike the true gerund in
  `JobDef.reportString` ("escaneando en TargetA.", copied verbatim from
  `OperateScanner.reportString` since our English source is identical).
- "Deconstructed" renders as the verb `desarmar` (from
  `DesignatorDeconstructDesc`'s "Desarma este objeto..."), not the button
  label `Deconstruir` — `MinifiableDesc` is descriptive prose, so it follows
  the description register.
- "Can be repositioned freely" and "depends on the operator's research
  ability" are copied verbatim from vanilla's `DeepDrill.description`
  ("Puede ser recolocado a voluntad") and `LongRangeMineralScanner.description`
  ("depende de la habilidad del investigador").
- `ResourceCostDesc`'s "removes it from the cost" uses `lo` (masculine)
  safely: the only three resources this key ever reaches (`Steel`,
  `ComponentIndustrial`, `ComponentSpacer`) are all masculine — checked the
  call sites in `LocalMineralScannerSettings.cs` before deciding.
- `RequireUnroofedDesc`/`MinifiableDesc` drop the article before `{0}` ("Al
  igual que {0}...") even though both current values happen to be masculine
  — the injected label's gender is unpinned at the call site, so the
  article-drop technique from `l10n/languages/Spanish.md` was applied
  rather than relying on the current values' shared gender.
- "Working days" is "días laborables"; "average ... between" mirrors
  `PrisonBreakMTBDaysDescription`'s "La media de días entre..." pattern.
- `100%` stays tight, per the family style rule; days follow `PeriodDays` =
  "{0} días" (spaced).

## Pending native review

- Whole pass is machine-assisted; no native speaker has checked it yet.
- `LocalMineralScanner_NoneUndiscovered` ("nada sin descubrir") is
  deliberately gender-invariant to fit `"{label} (nada sin descubrir)"` in
  the tuning float menu regardless of the paired mineral's gender.
- The Workshop FAQ's "P:"/"R:" convention (Pregunta/Respuesta, replacing
  English's "Q:"/"A:") is a translation choice, not vanilla-attested.
