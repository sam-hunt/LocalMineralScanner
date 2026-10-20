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
| ore vein (Workshop description, "contiguous ore vein") | Erzader | Not vanilla-attested (this mechanic is mod-specific), but built on the real German mining word `Ader` (vein), as in the everyday compound `Goldader` (gold vein) — reads as ordinary German mining vocabulary rather than a calque. |
| unfog / fogged (Workshop description) | lichtet den Kriegsnebel / unter dem Kriegsnebel | Core's German tar attests no fog-of-war term (`TriggerUnfogged.label` = "Auslöser Anti-Nebel" is a dev label; bare `Nebel` is the Fog weather), so the established German strategy-game term `Kriegsnebel` is used. Never collapse to `aufdecken`/`unentdeckt`: those lose *where* the ore hides, and the Tiefenscanner comparison then reads as the mod's function (see the Russian glossary's matching row). |
| "Never strip-mine again." (Workshop tagline) | Nie wieder blind Tunnel graben. | Mirrors the README's "tunnel blindly"; `die ganze Karte abtragen` read as levelling the surface, not mining rock. |
| ancient cryptosleep casket | antike Kryptoschlafkapsel | vanilla `AncientCryptosleepCasket.label` / `AncientCryptosleepPod.label` (both variants share the feminine label, so "keine unentdeckte {0} mehr" agrees) |
| ancient danger | antike Gefahr | vanilla `LetterLabelAncientShrineWarning` |
| dormant | ruht | vanilla `DormantCompInactive` (verb form: "was in der Nähe ruht") |
| tuning menu (casket setting tooltip) | Einstellmenü | coined; avoids a clash with Einstellungen (settings) |
| any mineral (tuning row) | Jedes Mineral | Vanilla's "Any <noun>" pattern is gender-agreed Jeder/Jede/Beliebiger (`Jeder Brief` = `AutomaticPauseMode_AnyLetter`, `Jede Bedrohung` = `AutomaticPauseMode_AnyThreat`, `Beliebiger Arbeiter` = `AnyWorker`); `Mineral` is neuter, so `Jedes Mineral`. Reads as `Eingestellt auf: Jedes Mineral` and, being neuter, is identical in the accusative (`auf jedes Mineral`). Quoted with ASCII single quotes per `German.md` style (`'Jedes Mineral'`). |

## Pending native review

- "lokaler Mineralscanner" and "Mineralscanner" generally: a coined compound,
  not a verbatim vanilla string (vanilla itself never says "Mineralscanner" —
  it says "Langstreckenscanner"/"Tiefenscanner" and drops "Mineral" from the
  label entirely, keeping it only in prose). A native speaker should confirm
  the coinage reads naturally rather than as a translated-English calque.
- "Nahbereichs-Sensoreinheit" (ThingDef description's opening noun, for
  "short-range ... sensor unit") is an analogous coinage mirroring vanilla's
  "Langstrecken-" prefix pattern in reverse; not itself vanilla-attested.
- "Jedes Mineral" (rather than vanilla's `Beliebiger` form, which is masculine/worker-specific): the gender-agreed Jeder/Jede pattern is vanilla-attested, but the pairing with the coined "Mineral" tuning is not; also `nichts Unentdecktes mehr zu finden` (a coined lower-case fragment) and the "Zum Beschränken ... deaktivieren" tooltip wording.
- "Kostenposten" (ResourceCostDesc's "it" substitute, avoiding gendered
  pronoun agreement across Stahl/Bauteil/Hightech-Bauteil) is a plain
  dictionary word, not vanilla-grounded — flag for a native check that it
  reads naturally in context.
- Whole Workshop description prose (`.steamworkshop/Description/German.txt`)
  is a first machine-assisted pass; FAQ tone and register deserve a native
  read especially around the informal-du banter in the "imbalanced" answer.
