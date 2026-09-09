# German glossary — Local Mineral Scanner

Initial generation, 2026-09-09 (machine-assisted, pending native review). Only
mod-specific coinages and grounding decisions; family-wide mechanics/style
live in `l10n/languages/German.md`.

| English | German | Grounding |
|---|---|---|
| local mineral scanner (coined term) | lokaler Mineralscanner | Coined from two vanilla-attested words: `Mineral` (used as a common noun in `LongRangeMineralScanner.description`) + the `-scanner` suffix vanilla itself uses in `Langstreckenscanner`/`Tiefenscanner`. `lokal` is vanilla-attested (`lokale Pflanzenwelt`, `lokalen Regierungsbeamten`, etc., Core BackstoryDefs). Masculine (`der Mineralscanner`), following `der Scanner`. |
| Workshop title / `LocalMineralScanner_SettingsCategory` | Lokaler Mineralscanner | Byte-identical to the title line of `German.txt`. First word capitalized as heading-initial, not because `lokaler` is a noun — mirrors how vanilla ThingDef labels with an adjective (`bionischer Arm`, `gewaltige Skulptur`) keep the adjective lowercase mid-phrase but headings capitalize their first word regardless of part of speech. |
| tuned to (a mineral) | eingestellt auf | Reuses vanilla's own `CommandSelectMineralToScanFor` = "Eingestellt auf" verbatim as the base verb phrase. |
| twin consoles / two operators | zwei Konsolen / zwei Benutzer(n) | "Benutzer" (not "Bediener") chosen to match vanilla's own `UserScanAbility` = "Benutzer-Scangeschwindigkeit" — the mod reuses vanilla's established word for "the person operating a scanner" everywhere "operator" appears (ThingDef description, `CombinedScanSpeed`, both `Desc` tooltips). |
| undiscovered (deposit/area) | unentdeckt | Grounded against Core `CannotPlaceInUndiscovered` = "Kann nicht in unentdeckten Gebieten platziert werden." |
| deposit (of a mineral), as in `{0}vorkommen` | {0}vorkommen (glued, no space) | Mirrors vanilla's own `LetterDeepScannerFoundLump` / `LetterFoundPreciousLump` pattern exactly: `ein unterirdisches {0}vorkommen gefunden` / `ein großes {0}vorkommen gefunden` — the resource label glues directly onto `vorkommen` with no space or hyphen. Reused for the letter, the exhausted message, and both lowercase fragments. |
| guaranteed find | garantierter Fund | Matches vanilla's own `ScanningProgressToGuaranteedFind` = "Fortschritt bis zum garantierten Fund". |
| combined scanning speed | Kombinierte Scangeschwindigkeit | Built on `UserScanAbility`'s "Scangeschwindigkeit". |
| roof rule / doesn't work under a roof | Funktioniert nicht unter einem Dach | Verbatim match: vanilla's `GroundPenetratingScanner.description` uses this exact sentence for the same mechanic. |
| can be uninstalled / uninstalling | kann demontiert werden / Demontieren | Reuses vanilla's `DesignatorUninstall` = "Demontieren" verbatim. |
| deconstructed | abgerissen | Reuses vanilla's `DesignatorDeconstruct` = "Abreißen". |
| WorkGiver verb/gerund/label; JobDef reportString | siehe unten | Mirrored exactly from vanilla's own `LongRangeScan`/`OperateScanner` (identical English source for all four fields): `verb` = "zum Scannen nutzen", `gerund` = "Scannen an", `reportString` = "scannt mit TargetA.", `label` pattern = "{Buildingname} bedienen" → "Lokalen Mineralscanner bedienen" (accusative, no article, strong ending `-en`, matching `Langstreckenscanner bedienen`). |

## Pending native review

- "lokaler Mineralscanner" and "Mineralscanner" generally: a coined compound,
  not a verbatim vanilla string (vanilla itself never says "Mineralscanner" —
  it says "Langstreckenscanner"/"Tiefenscanner" and drops "Mineral" from the
  label entirely, keeping it only in prose). A native speaker should confirm
  the coinage reads naturally rather than as a translated-English calque.
- "Nahbereichs-Sensoreinheit" (ThingDef description's opening noun, for
  "short-range ... sensor unit") is an analogous coinage mirroring vanilla's
  "Langstrecken-" prefix pattern in reverse; not itself vanilla-attested.
- "Kostenposten" (ResourceCostDesc's "it" substitute, avoiding gendered
  pronoun agreement across Stahl/Bauteil/Hightech-Bauteil) is a plain
  dictionary word, not vanilla-grounded — flag for a native check that it
  reads naturally in context.
- Whole Workshop description prose (`.steamworkshop/Description/German.txt`)
  is a first machine-assisted pass; FAQ tone and register deserve a native
  read especially around the informal-du banter in the "imbalanced" answer.
