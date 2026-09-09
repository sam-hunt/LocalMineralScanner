# Simplified Chinese glossary — Local Mineral Scanner

Machine-assisted, 2026-09 initial generation. No native review yet. Grounded
against Core-only vanilla data (this mod requires no DLC).

## Coined terms

| English | Chinese | Grounding / rationale |
|---|---|---|
| local mineral scanner (mod title) | 本地矿物扫描仪 | Core `LongRangeMineralScanner.label`=远距离矿物扫描仪 and `GroundPenetratingScanner.label`=地质扫描仪 both anchor on 矿物扫描仪; 本地 is the ordinary word for "local" paired with it, mirroring the English title's own "local" + "mineral scanner" split. Also the Workshop title and `LocalMineralScanner_SettingsCategory` value — kept identical everywhere per the title-coupling rule. |
| tuned to (mineral) | 调整 (为...) | Core `LongRangeMineralScanner.description`: "但可以将其调整为只扫描特定类型的资源" — reused verbatim for our own "tuned to a specific mineral type" and "tuned to another mineral" phrasing. `CommandSelectMineralToScanFor`=目标 (vanilla, reused unchanged, not retranslated). |
| twin consoles / two operators | 双控制台 / 两名操作者、两名殖民者 | No vanilla analog (no other building lets two pawns operate at once); coined plainly rather than a compound. Flagged for native review — "双控制台" vs. a more colloquial "双工作台" is a judgment call. |
| undiscovered (deposit) | 未被发现的 | Grounded against Core `QuestScriptDef` `LongRangeMineralScannerLump`'s `questNameRules`, whose `discovered` variants include 被发现的/被查明的/被定位的/被扫描的 — 未被发现的 is the negation of the first. |
| deposit | 矿藏 | Same quest script's `lump` variants (矿脉/矿块/矿产/矿藏/矿层/原矿) — 矿藏 chosen as the most literal match for "deposit". |
| guaranteed find | 保证发现 | Reuses vanilla `ScanningProgressToGuaranteedFind`=扫描进度's own "guaranteed find" concept name; our `FindGuaranteedDays` label 保证发现时限 ("guaranteed-find deadline") is coined to fit the settings-slider slot (label + ASCII ": " + `PeriodDays`-formatted value, so the label itself carries no trailing colon or unit). |
| combined scanning speed | 合计扫描速度 | Paired with vanilla `UserScanAbility`=扫描速度 (reused unchanged for the single-operator case); 合计 ("combined/total") prefixed only for the multi-operator inspect line, with the operator count in full-width parens: 合计扫描速度（{0}名操作者）. |
| the Workshop title | 本地矿物扫描仪 | Identical to `LocalMineralScanner_SettingsCategory` (byte-for-byte), per `.steamworkshop/README.md`. |

## Grounding decisions

- **Letter label** ("Mineral deposit located") mirrors the nearer vanilla
  analog rather than a literal translation: Core `LetterLabelFoundPreciousLump`
  ="Distant resource scanned"→扫描到稀有矿物 uses the "扫描到 + noun" pattern
  for a scanner-found-letter title, so ours became 扫描到矿藏 instead of a
  literal 矿藏已定位.
- **Letter body** mirrors Core `LetterDeepScannerFoundLump`'s structure
  ("{FINDER_nameDef}使用穿透地面的扫描仪发现了一块被掩埋的{0}！") — same
  `{FINDER}使用[scanner]...{0}` shape, adapted for "in an undiscovered area"
  (在一处未被发现的区域) and ending in 。 (not ！) since our English source
  ends in a period, not an exclamation mark.
- **ThingDef description** mirrors Core's own scanner descriptions almost
  verbatim: "lateral sensor unit used by researchers to detect..." →
  研究人员用来探测...的横波传感器 (from `LongRangeMineralScanner.description`);
  "doesn't work under a roof" → 此装置只能露天使用 (from
  `GroundPenetratingScanner.description`, the nearer analog since that
  building shares our own roof restriction). "Can be repositioned freely" has
  no usable vanilla precedent — Core's own `DeepDrill.description` Chinese
  translation drops that sentence entirely (vanilla incompleteness, not
  style guidance) — so 可自由重新放置 is coined plainly.
- **WorkGiverDef verb/gerund** reuse Core's `LongRangeScan`/`GroundPenetratingScan`
  value verbatim: both fields render identically as 扫描矿物于 in vanilla, so
  ours does too rather than inventing a distinct gerund form.
- **JobDef reportString** reuses Core `OperateScanner.reportString`
  ("scanning at TargetA.") verbatim: 用TargetA扫描。
- **NoneUndiscovered** ("none undiscovered", a float-menu fragment appended
  as `label + " (" + ... + ")"` by C#, parens are ASCII and hardcoded, not
  part of the translated string) was rendered as 全部已发现 ("all
  discovered") rather than a literal negation — logically equivalent, reads
  far more naturally as a short Chinese fragment. Flagged for native review.
- **DefaultSuffix** (" (default)", leading space required) uses a half-width
  leading space with full-width parens, " （默认）", matching vanilla's own
  mixed ASCII/full-width punctuation in labels like `TechprintLabel`.

## Pending native review

- 双控制台 for "twin consoles" — no vanilla precedent, plain coinage.
- 全部已发现 for `NoneUndiscovered` — meaning-equivalent but not a literal
  rendering of "none undiscovered".
- 可自由重新放置 for "Can be repositioned freely" — vanilla's own Chinese
  data omits the equivalent `DeepDrill` sentence, so this has no anchor.
