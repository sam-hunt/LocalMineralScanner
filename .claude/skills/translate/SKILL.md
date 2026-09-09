---
name: translate
description: Generate, update, or audit mod localization (Keyed + DefInjected) for a target language, grounded in vanilla (Core) RimWorld mining/scanner/research terminology for Local Mineral Scanner. Use when asked to add a language, update translations, or check translation freshness.
argument-hint: "[language, e.g. German | update | check]"
---

# Translate

Produce or refresh localization files for Local Mineral Scanner. English is
the source of truth; every other language derives from it.

**The family-wide process lives in the `l10n/` submodule — load these first,
and only these** (progressive disclosure; if `l10n/` is empty, run
`git submodule update --init`):

- `l10n/process.md` — non-negotiables, file/format conventions, terminology
  grounding method, and the generation / update / audit workflows. This is
  the workflow authority; follow it step by step.
- `l10n/languages/<Language>.md` — the target language's engine mechanics,
  style rules, and vanilla-grounded common vocabulary. Read ONLY the target
  language's file.
- `glossary/<Language>.md` (beside this file) — this mod's own coined-term
  table for the target language. Read it in the same pass.
- `l10n/lessons.md` — cross-language lessons; read when generating a new
  language, skim otherwise.
- `l10n/workshop.md` — Steam Workshop description/title conventions;
  `.steamworkshop/README.md` names this mod's anchor term and title-coupling
  key (`LocalMineralScanner_SettingsCategory`).

**Where learnings land:** mod-independent findings (engine mechanics, a
language's grammar rule, corpus style facts) go in the `l10n/` submodule —
edit the canonical checkout at `~/dev/rimworld-l10n`, commit and tag there.
Mod-specific findings (coined terms, phrasing decisions) go in
`glossary/<Language>.md`.

**Before any pass, bump the pin:** run `l10n/tools/bump-consumer.sh` (fetches
upstream's release tags, checks out the latest, commits the pointer as `chore:
Bump l10n submodule vOLD -> vNEW`; no-op when already current). This is one of
the three moments a pin moves (release, pass start, new upstream major), never
per upstream commit. If it reports a MAJOR bump, read the upstream release
notes for the shim or flow edit this repo owes before continuing.

## This mod's translation surface

The whole surface is small — a few dozen strings — so a full language pass is
a bounded task; do not let it sprawl into extracting or reading whole vanilla
language tars. Grep the tar for the handful of grounded terms below.

- English Keyed source: `1.6/Languages/English/Keyed/LocalMineralScanner.xml`
  — a single file covering the find letter, the exhausted message, two
  inspect/fail-text fragments, the combined-speed stat line, and the settings
  window (section headers, toggles, slider descriptions, resource-cost rows).
  Every key is `LocalMineralScanner_`-prefixed. There is no second Keyed file.
  Its translator comments name what each `{0}`/`{1}` receives (a vanilla
  building label, a resource label, a count) — plan the surrounding grammar
  around them, and remember `NoFoggedDeposits` is a lower-case fragment that
  the code `CapitalizeFirst()`s.
- **DefInjected:** the mod ships one `ThingDef` (the building: `label`,
  `description`), one `JobDef` (`reportString`) and one `WorkGiverDef`
  (`label`, `verb`, `gerund`). There is no English DefInjected tree (English
  is served by the def XML itself), so **enumerate the target key set from
  `Scripts/expected-injections.json`, never from `1.6/Languages/English/`**,
  and take the English source text from each entry's `english` field. Type
  folders are bare (`ThingDef`, `JobDef`, `WorkGiverDef`): the building's C#
  class is a `thingClass`, not a Def subclass.
- **No gated compat load roots:** the mod is Core-only, so every translation
  goes in the main `1.6/Languages/<Language>/` tree.
- **Vanilla strings the mod reuses in place** (do NOT re-translate; they
  arrive localized): the tuning gizmo (`CommandSelectMineralToScanFor` and
  its `Desc`), the inspect lines (`ScanAverageInterval`,
  `ScanningProgressToGuaranteedFind`, `UserScanAbility`), `PeriodDays`,
  `Cost`, and `RestoreToDefaultSettings`. Read their target-language values
  from the vanilla tar so our own strings sit beside them consistently.

## This mod's grounding domain

Domain DLC: **none — Core only.** Ground against the Core tar alone. Terms
that MUST be grounded before use, with the vanilla defName or key to grep in
the tar's `DefInjected`/`Keyed` trees:

- the long-range mineral scanner (`ThingDef` `LongRangeMineralScanner`
  label/description) — the vocabulary source for "mineral scanner" and the
  Workshop-title anchor;
- the ground-penetrating scanner (`GroundPenetratingScanner`) and deep drill
  (`DeepDrill`) — the settings tooltips name them via `{0}`;
- the vanilla scanner work: the `WorkGiverDef` and `JobDef` behind
  "operate long-range mineral scanner" / "scanning at TargetA." — mirror their
  `label`/`verb`/`gerund`/`reportString` phrasing exactly for ours;
- research vocabulary: the Research work type, `ResearchSpeed` stat, "research
  ability" as used in `UserScanAbility`;
- mineral/ore deposit vocabulary: the `Mineable*` ThingDef labels (gold,
  silver, steel, plasteel, components, uranium, jade), "deposit", "undiscovered"
  as vanilla phrases fogged/unrevealed terrain;
- roof (`Roofed`/"under a roof" phrasing), minifiable/uninstall (`Uninstall`
  designation), mass (`Mass` stat), caravan.

The vanilla-grounded answers for common words live in
`l10n/languages/<Language>.md`; this mod's coined terms ("local mineral
scanner" itself, "twin consoles", "tuned to") live in `glossary/<Language>.md`.
A new language starts from nothing and gets its terms grounded and recorded
per `l10n/process.md`.

## Workflows

Follow `l10n/process.md`'s Initial generation / Update pass / Audit-only
workflows verbatim. This mod's specifics on top:

- The checker: `python3 Scripts/check-translations.py` (`--strict` for new
  languages). Sidecar regen: `python3
  Scripts/refresh-translation-expectations.py` (game must be closed; drives
  the deployed L10nProbe, which must have this mod ticked in its settings).
- `LocalMineralScanner_SettingsCategory` is that language's localized
  Workshop title and must stay in sync with the title line of
  `.steamworkshop/Description/<Language>.txt` — change both together.
- The public roster (and credits) is CONTRIBUTING.md's localization table —
  update it in the same commit as any language addition or native review.
