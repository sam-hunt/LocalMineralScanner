# French — Local Mineral Scanner terminology

Machine-assisted, generated 2026-09-09. Pending native review (see the list
below). Grounded against Core's French tar only (no domain DLC).

## Coined terms

| English | French | Grounding / rationale |
|---|---|---|
| local mineral scanner (ThingDef label, Workshop title) | scanner géologique local | `LongRangeMineralScanner.label` = "scanner géologique à longue portée"; "géologique" is vanilla's anchor word for "mineral scanner", "local" is the ordinary word swapped in for "à longue portée" |
| tuned to (gizmo, reused in place) | Réglé sur | vanilla `CommandSelectMineralToScanFor` — not retranslated, cited for consistency |
| twin consoles | consoles jumelles | "console" is feminine in French; "jumelles" (twin) agrees |
| two operators / operators | (deux) opérateurs | matches vanilla's "l'opérateur" (`LongRangeMineralScanner.description`) |
| undiscovered (deposit) | inexploré(e) | vanilla `Undiscovered` Keyed key = "Inexploré"; `CannotPlaceInUndiscovered` = "zone inexplorée" |
| deposit (of a mineral) | gisement | vanilla `Script_LongRangeMineralScannerLump` quest text: "un gisement de [mineral]" |
| guaranteed find | découverte garantie | pairs with "Découverte garantie sous [N] jours" ("sous" = idiomatic French "within", cf. "livraison sous 48h") |
| combined scanning speed | vitesse de détection combinée | vanilla `UserScanAbility` ("User scanning speed") = "Vitesse de détection" — reused as the noun for "scan speed" throughout |
| Workshop title | Scanner géologique local | byte-identical to `LocalMineralScanner_SettingsCategory` |
| ore vein (Workshop copy) | gisement | reuses the existing "deposit" mapping rather than coining "filon"; the Workshop page doesn't distinguish "vein" from "deposit" |
| fogged mineable ore (Workshop FAQ) | minerai exploitable inexploré | reuses vanilla `Undiscovered` = "Inexploré" for "fogged"; "minerai exploitable" mirrors the mineable-ore stat wording |
| minifiable (Workshop copy) | miniaturisé | matches vanilla `MinifiedThing` = "objet miniaturisé" |
| unfog / fogged (Workshop description) | dissipe le brouillard de guerre / sous le brouillard de guerre | Core's French tar attests no fog-of-war term (`TriggerUnfogged.label` is mistranslated; bare `brouillard` is the Fog weather), so the established French gaming term is used. Never collapse to `révèle`/`inexploré` alone: those lose *where* the ore hides (see the Russian glossary's matching row). |
| ancient cryptosleep casket | caisson d'hypersommeil antique (m.) / ancienne capsule d'hypersommeil (f.) | vanilla `AncientCryptosleepCasket.label` / `AncientCryptosleepPod.label`. The variants differ in gender, so the casket strings agree with a neutral head noun: "plus aucun exemplaire inexploré de {0}"; the worker elides "de" before a vowel |
| ancient danger | danger antique | vanilla `LetterLabelAncientShrineWarning` |
| dormant | dormant | vanilla `DormantCompInactive` |
| tuning menu (casket setting tooltip) | menu de réglage | coined from vanilla "Réglé sur" |
| opt-in (Workshop bullet) | Optionnel : | plain word |

## Grounding decisions

- **Three different French words for "scan" depending on grammatical role,
  all vanilla-attested, all kept distinct rather than unified**: the stat
  noun uses "détection" (`UserScanAbility`), the `WorkGiverDef` verb/gerund
  use "analyser"/"analyse" (`LongRangeScan.verb`/`.gerund`), and the
  `JobDef.reportString` uses the verb "scanner" itself ("scanne à TargetA.",
  from `OperateScanner.reportString` — copied verbatim since our English is
  identical). Mirroring vanilla's own inconsistency per-context beat forcing
  one word everywhere.
- **`WorkGiverDef.label` drops the "geological/mineral" qualifier**, matching
  vanilla's own `LongRangeScan.label` = "utiliser un scanner longue portée"
  (not "... scanner géologique à longue portée"): ours is
  "utiliser un scanner local", not "... scanner géologique local".
- **"Deconstructed" is "démoli", not "déconstruit"** — `DesignatorDeconstruct`
  = "Démolir".
- **`ResourceCostDesc`'s "removes it from the cost" uses "la" (feminine)**,
  agreeing with "quantité" (the amount), not with the injected resource's
  (unknown) gender — sidesteps the "gender defaults to Male" trap noted in
  `l10n/languages/French.md`.
- `100%` stays tight (no space before `%`), per the family style rule.

## Pending native review

- Whole pass is machine-assisted; no native speaker has checked it yet.
- `LocalMineralScanner_NoneUndiscovered` ("aucun inexploré") is deliberately
  terse to fit `"{label} (aucun inexploré)"` in the tuning float menu — worth
  a native ear for naturalness in that exact slot.
- The Workshop FAQ's tone (informal "vous"-form, direct address) hasn't been
  checked against how other French Workshop pages in this genre read.
