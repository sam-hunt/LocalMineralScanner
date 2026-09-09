# Japanese glossary — Local Mineral Scanner

Machine-assisted (2026-09), no native review yet. Mod-specific coined terms and
grounding decisions only; family-wide mechanics live in `l10n/languages/Japanese.md`.

| English | Japanese | Grounding / notes |
|---|---|---|
| local mineral scanner (building label, Workshop title) | 近距離鉱物探査スキャナー | `長距離鉱物探査スキャナー` (Core `LongRangeMineralScanner.label`) with 長→近; keeps vanilla's own "mineral scanner" term (鉱物探査スキャナー) intact and pairs it with 近距離 (short/close-range), the natural antonym of 長距離 already used by the sibling building — satisfies the Workshop title's "mineral scanner term + ordinary local/nearby word" rule without inventing vocabulary |
| operate local mineral scanner (WorkGiverDef.label) | 近距離スキャナーを操作 | Vanilla's own `LongRangeScan.label`/`GroundPenetratingScan.label` shorten their ThingDef labels by dropping 鉱物探査 before appending を操作 (`長距離鉱物探査スキャナー` → `長距離スキャナーを操作`); mirrored the same drop here rather than reusing the full building label |
| tuned to (a mineral) | 調整 | Reused verbatim from vanilla `CommandSelectMineralToScanFor` (the gizmo label itself is "調整"); prose forms use 調整できる/調整することができ |
| twin consoles / two operators | 2基のコンソール / 2人のオペレーター | No vanilla precedent (mod-coined mechanic); chose a plain counted-object phrase over a katakana "ツインコンソール" loanword since RimWorld ja prose favours native compounds for hardware nouns of this kind — **flag for native review**, a native speaker may prefer the katakana form |
| undiscovered (deposit) | 未知 | Vanilla `Undiscovered` (Keyed) = 未知; `CannotPlaceInUndiscovered` confirms the same word for "undiscovered area" (未知の区域) |
| guaranteed find | 確実な発見 | 確実 (certain/guaranteed) drawn from vanilla `ScanningProgressToGuaranteedFind` = 確実な鉱脈検出までの進捗 |
| combined scanning speed | 合計スキャン速度 | スキャン速度 reused verbatim from vanilla `UserScanAbility`; 合計 (total/combined) is a plain, uncoined modifier |
| the Workshop title | 近距離鉱物探査スキャナー | Byte-identical to `LocalMineralScanner_SettingsCategory`, per `workshop.md`'s title-settings coupling rule |

## Other decisions

- **Letter/message register mirrors vanilla's own scanner-find letters**, not a
  generic ですます tone: `LetterFoundPreciousLump`/`LetterDeepScannerFoundLump`
  both use plain past tense (発見した) with `FINDER_nameDef`/`WORKER_labelShort`
  as subject, no polite form. `LocalMineralScanner_LetterFoundDeposit` and the
  letter label follow that shape; `LocalMineralScanner_MessageExhausted` and all
  settings-window `*Desc` tooltips use ですます + period instead, matching the
  family rule that descriptions/tooltips take polite register while job-report
  and letter-body text mirroring a specific vanilla analog follows that analog.
- **Resource names in `{0}` cost rows and the tuning list are bare vanilla
  ItemDef labels** (ゴールド, シルバー, スチール, プラスチール, コンポーネント,
  ウラン, ヒスイ — from `Items_Resource_Stuff.xml`/`Items_Resource_Manufactured.xml`),
  not the `Mineable*` rock labels (ゴールド鉱石 etc.) — the mod's `{0}` injects
  the resource itself, matching how vanilla's own `LetterFoundPreciousLump`
  injects the resource label directly before 鉱床.
- **`reportString` mirrors vanilla `OperateScanner.reportString`
  (`TargetAでスキャン中`) exactly** — no trailing period, progressive 〜中 form,
  bare `TargetA`.
- **`verb`/`gerund` reuse vanilla's `LongRangeScan`/`GroundPenetratingScan`
  values verbatim** (`でスキャン` / `でスキャンする`) — both vanilla WorkGiverDefs
  agree, so there was no ambiguity to resolve.
- **Building description mirrors `DeepDrill.description`'s literal, complete
  style** rather than `LongRangeMineralScanner.description`'s looser paraphrase
  (which drops the "tuned"/roof sentences in vanilla's own ja text) — chose the
  more literal analog so every English sentence gets a translated counterpart.
  "Can be repositioned freely" reuses `DeepDrill.description`'s closing
  sentence (自由に再配置することができます) verbatim.

## Pending native review

- Whether "twin consoles" should be a katakana loan (ツインコンソール) instead
  of 2基のコンソール.
- Whether 近距離 (short/close-range) reads naturally as the Workshop-search
  "local/nearby" pairing, or whether 現地 (on-site/local) would be preferred —
  近距離 was chosen for its direct antonym relationship to vanilla's own
  長距離, but that relationship is not itself a vanilla-attested phrase.
- General phrasing throughout: this is a first machine-assisted pass with no
  native-speaker review.
