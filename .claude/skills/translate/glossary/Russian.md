# Russian glossary — Local Mineral Scanner

Initial generation, 2026-09-09 (machine-assisted, pending native review). Only
mod-specific coinages and grounding decisions; family-wide mechanics/style
live in `l10n/languages/Russian.md`.

| English | Russian | Grounding |
|---|---|---|
| local mineral scanner (coined term) | локальный сканер минералов | `сканер минералов` is the vanilla noun phrase inside `LongRangeMineralScanner.label` = "сканер минералов дальнего действия"; `локальный` is the ordinary adjective for "local". Lowercase as a building label, per Russian's lowercase-noun-phrase convention. |
| Workshop title / `LocalMineralScanner_SettingsCategory` | Локальный сканер минералов | Byte-identical to the title line of `Russian.txt`. Only the first word is capitalized (Russian titles use sentence case, not English-style title case). |
| deposit (of a mineral) | скопление | Grounded against `LongRangeMineralScanner.description`, which renders "detect a specific type of mineral across the planet" as "обнаруживать скопления минералов по всей планете" — vanilla's own word for the concept in this exact device family. Vanilla is inconsistent across other scanner-adjacent text (`GroundPenetratingScanner`'s found-letter label uses `залежи`, `DeepDrill.description` uses `месторождение`); `скопление` was picked for being the nearest analog (same ThingDef family, mineral scanner rather than deep drill/ground-penetrating), and used consistently everywhere "deposit" appears in this mod. |
| undiscovered (deposit/area) | необнаруженный | Coined from the same root as `обнаружил`/`обнаружено` (discover/detect), used throughout so the letter, message and fragments read as one family of words rather than mixing verbs. |
| tuned to (a mineral) | (reused verbatim) | `CommandSelectMineralToScanFor` = "Ищется" is a vanilla key reused in place, not retranslated here. |
| twin consoles / two operators | два пульта управления / операторы | Plain, non-vanilla-attested coinage (vanilla's own scanners are single-operator, so there is no vanilla precedent for a two-operator console). Flagged below. |
| combined scanning speed | суммарная скорость сканирования | Built on vanilla's own `UserScanAbility` = "Эффективность сканирования" register, `суммарная` (combined/total) is a plain, common word. |
| resource {0} placeholders | `{lookup: {0}; Case; N}` | `{0}` receives the resource's already-localized nominative label (e.g. `золото`, `нефрит`) — all seven (gold/silver/steel/plasteel/components/uranium/jade) are literal rows in Core's `WordInfo/Case.txt`, so `{lookup: {0}; Case; 1}` reliably declines them to genitive (`золота`, `нефрита`, ...) wherever the sentence needs it. A `lookup` miss degrades to nominative, never errors. |
| the local mineral scanner (literal mention, not injected) | локального сканера минералов (genitive, hand-declined) | Not a `{0}` in the English source (it's constant text), so it is safe to hand-decline the coined compound directly rather than reach for `lookup` (which only resolves single vanilla-attested words). |
| roof rule / doesn't work under a roof | Не работает под крышей | Verbatim match: `GroundPenetratingScanner.description` uses this exact clause for the same mechanic. |
| can be uninstalled / uninstalling | демонтировать | Reuses vanilla's `DesignatorUninstall`-family verb root (`DesignatorUninstallDesc` = "Демонтировать, чтобы..."). |
| deconstructed | разобрать | Reuses vanilla's `DesignatorDeconstruct` = "Разобрать" verbatim. |
| "Can be repositioned freely." | Можно перемещать с места на место. | Verbatim reuse: `DeepDrill.description` ends with the identical English sentence, translated exactly this way. |
| WorkGiver verb/gerund; JobDef reportString | сканировать, используя / выполняет сканирование, используя TargetA | Mirrored exactly from vanilla's `LongRangeScan`/`GroundPenetratingScan`/`OperateScanner` (identical English source for verb, gerund and reportString): both vanilla WorkGivers use the same string for `verb` and `gerund`. `reportString`s take no trailing period per `l10n/languages/Russian.md`, confirmed directly against `OperateScanner.reportString`'s own Russian translation, whose English source (`scanning at TargetA.`) is byte-identical to ours. |
| WorkGiver label | работать с локальным сканером минералов | Mirrors vanilla's `работать со сканером дальнего действия` / `работать с глубинным сканером` pattern ("work with the [instrumental-case scanner]"). |
| Research (work type, cited in FAQ) | «Учёный» | Core's `WorkTypeDef` `Research.label` is actually "Учёный" (a profession noun, like `Smithing.label` = "Кузнец"), not an activity noun — the work-tab column really does say "Учёный". Quoted in guillemets per the family's UI-citation convention. |
| ore vein (Workshop description, "contiguous ore vein") | рудная жила | Plain geological term for a contiguous ore body, distinct from `скопление` (the broader "deposit" used elsewhere in this mod); used once, where the English specifically calls out contiguity rather than the general deposit concept. |

## Pending native review

- "два пульта управления" (twin consoles) and the whole two-operator framing
  in the ThingDef description and `CombinedScanSpeed`: no vanilla scanner has
  more than one operator, so there is no official phrasing to check this
  against.
- "скопление" as the mod-wide word for "deposit": defensible (see grounding
  above) but vanilla itself is not consistent, and a native speaker may
  prefer "месторождение" throughout instead.
- Whole Workshop description prose (`.steamworkshop/Description/Russian.txt`)
  is a first machine-assisted pass, particularly the FAQ tone and whether
  "Часть черновой работы с кодом" reads naturally for "some of the grunt
  code".
