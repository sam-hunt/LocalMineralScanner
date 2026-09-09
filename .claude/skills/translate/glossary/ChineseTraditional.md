# Traditional Chinese glossary — Local Mineral Scanner

Machine-assisted (2026-09), no native review yet. Mod-specific coined terms
and grounding decisions only; family-wide mechanics and the zh-Hant/zh-Hans
inversion table live in `l10n/languages/ChineseTraditional.md`.

| English | Traditional Chinese | Grounding / notes |
|---|---|---|
| mineral scanner (anchor term) | 礦物掃描器 | Core `LongRangeMineralScanner.label` = 長程礦物掃描器 |
| local (Workshop title / building label) | 在地 | Core Keyed `Credits_Localization_Anomaly`/`Credits_LeadLocalizationProjectManager` use 在地化 for "localization"; 在地 is ordinary Taiwan Mandarin for "local" and was not already spent on another RimWorld concept |
| local mineral scanner (building label, Workshop title) | 在地礦物掃描器 | 在地 + vanilla's own 礦物掃描器, unchanged — satisfies the title rule (vanilla term + ordinary local/nearby word) without coining new scanner vocabulary |
| operate local mineral scanner (WorkGiverDef.label) | 操作在地礦物掃描器 | Mirrors `LongRangeScan.label`=操作長程礦物掃描器 and `GroundPenetratingScan.label`=操作地質掃描器: 操作 + full building label, unlike ja's shortened form |
| scan at / scanning at (WorkGiverDef verb/gerund) | 操作 | Both vanilla scanner WorkGiverDefs (`LongRangeScan`, `GroundPenetratingScan`) render verb AND gerund as 操作, not a literal "scan" verb |
| scanning at TargetA. (JobDef reportString) | 在TargetA掃描 | Byte-identical to Core `OperateScanner.reportString`; no trailing 。 per the family reportString rule |
| tuned to (a mineral) | 調整為 | Reused verbatim from vanilla `CommandSelectMineralToScanFor` |
| undiscovered (deposit/area) | 未探索 | Vanilla Keyed `Undiscovered`=未探索; `CannotPlaceInUndiscovered`=不能放置在未探索的區域 confirms the same word for "undiscovered area" |
| deposit (of a mineral) | 礦脈 | Core `DesignatorMineVein`=開採礦脈 and mining-tale RulePacks use 礦脈 for a mineable vein; the `LongRangeMineralScannerLump` quest's lump→deposit synonym padding renders "deposit" as 沈澱 (sediment), rejected as not the grounded noun for our own "a deposit of X" |
| operator (the scanning pawn) | 操作員 | Both scanner ThingDef descriptions: "the operator's research ability"→操作員的研究能力 |
| guaranteed find / progress to guaranteed find | 尋獲 / 尋獲進度 | Reused vanilla Keyed `ScanningProgressToGuaranteedFind`=尋獲進度 verbatim |
| combined scanning speed | 綜合掃描速度 | 掃描速度 half reused from `UserScanAbility`=使用者掃描速度; 綜合 (combined/overall) is a plain, uncoined modifier |
| twin consoles / two operators | 雙控制台 / 兩位操作員 | No vanilla precedent for this mechanic; 控制台 (console) drawn from Odyssey's pilot console=駕駛控制台 (family vocabulary table); 雙 (twin/paired) is plain, uncoined |
| deconstruct / uninstall | 拆除 / 移除 | Core `DesignatorDeconstruct`=拆除, `DesignatorUninstall`=移除 |
| cost (settings row) | 消費 | Reused verbatim from vanilla Keyed `Cost` |
| the Workshop title | 在地礦物掃描器 | Byte-identical to `LocalMineralScanner_SettingsCategory`, per `workshop.md`'s title-settings coupling rule |

## Other decisions

- **Resource names in `{0}` cost rows, the tuning list, and the found/exhausted
  letters are bare vanilla resource ItemDef labels** (黃金, 白銀, 鋼鐵, 塑鋼,
  零件, 鈾, 翡翠), not the `Mineable*` rock labels (黃金礦石 etc.) — matches
  how the mod's C# passes `mineableThing.label`/`resource.label` verbatim.
- **`LetterFoundDeposit` mirrors `LetterDeepScannerFoundLump`'s shape**
  (`{FINDER_nameDef}穿透地質掃描器發現了隱藏的{0}！`), sharing its
  `{FINDER_nameDef}` + "via the scanner" + bare-resource structure; neither
  vanilla analog wraps the injected resource label in 「」, so ours doesn't.
- **Building description mirrors `DeepDrill.description`'s literal, complete
  style**, reusing its exact closing sentence for "Can be repositioned
  freely" (可以重新放置安裝。) and `LongRangeMineralScanner.description`'s
  "can be tuned to find a specific X" clause (可以調整掃描器來找到特定X).
- `{0}`/`{1}` in `FindMtbDaysDesc`/`FindGuaranteedDaysDesc` are left bare
  (`{0}使用{1}。`), matching English, which never appends a unit to `{1}`
  either (a raw day count, not a pre-formatted `PeriodDays` string) —
  confirmed against the C# call sites.
- `LocalMineralScanner_FindGuaranteedDays`/`_DefaultSuffix` are concatenated
  onto other text by C# with a literal ASCII `": "` / a bare suffix, not one
  `Translate()` call — full-width ： could not be applied there without a
  code change, out of this pass's scope.

## Pending native review

- 在地 vs 當地 for "local": both attested (在地化 = localization; Odyssey's
  當地生態系統 = "the local ecosystem"); 在地 chosen as the more common
  standalone adjective.
- 礦脈 for "deposit": a good fit for a mining vein, but the mod's "deposit" is
  a single revealed tile rather than a vein players dig along — 礦藏/礦床 may
  read better to a native speaker.
- General phrasing throughout: a first machine-assisted pass, no
  native-speaker review yet.
