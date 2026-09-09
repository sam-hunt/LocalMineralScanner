# Brazilian Portuguese — Local Mineral Scanner glossary

Machine-assisted first pass (2026-09-09), pending native review. Family mechanics/style live in
`l10n/languages/PortugueseBrazilian.md`; only this mod's coined terms and decisions are here.

## Coined terms

| English | pt-BR | Grounding / rationale |
|---|---|---|
| local mineral scanner (ThingDef label) | escaneadora local de minerais | Parallel to Core `LongRangeMineralScanner.label`=escaneadora de minerais de longo alcance, reordered so the single-word modifier `local` sits directly after `escaneadora` — `escaneadora de minerais local` is grammatically resolvable (number agreement forces `local` onto the singular head noun) but reads ambiguous at a glance; `de minerais de longo alcance` avoids this only because its modifier is a multi-word phrase, so the parallel doesn't carry over to a one-word adjective |
| Workshop title / `LocalMineralScanner_SettingsCategory` | Escaneadora Local de Minerais | Title Case, byte-identical to the Description file's first line; contains the vanilla "escaneadora ... minerais" anchor plus the ordinary word for local/nearby (`local`), no English brand appended |
| operate local mineral scanner (WorkGiverDef label) | - Operação de escaneadoras locais de minerais | Every vanilla WorkGiverDef label in pt-BR follows `- Operação/Construção/... de <plural noun>` (leading dash IS vanilla precedent, confirmed across the whole `WorkGivers.xml`); reordered to `escaneadoras locais de minerais` (not `escaneadoras de minerais locais`) since with both nouns plural, `locais` cannot be disambiguated by number the way the singular label can |
| tuned to (a mineral) | sintonizado(a) para | Directly grounded: Core's own gizmo key `CommandSelectMineralToScanFor`=Sintonizado, reused verbatim by this mod. Vanilla hardcodes it masculine regardless of the building's own gender (gender resolution is dead per the family engine notes) — that reused key is untouched, but our own prose agrees the participle with whatever noun is actually the grammatical subject of the sentence |
| twin consoles | consoles gêmeos | `console` grounded as masculine via Odyssey's "console do piloto" (family common-vocabulary table); `gêmeos` agrees plural masculine |
| two operators / operator | dois operadores / operador | Ordinary word, not vanilla-attested (vanilla calls the scanning pawn "pesquisador" via `Research.pawnLabel`) — this mod's own English text already coins "operator" rather than reusing vanilla's "researcher", so pt-BR mirrors that coinage literally rather than importing "pesquisador" |
| undiscovered (deposit/area) | inexplorado(a) | Chose Core's `Undiscovered`=Inexplorado (the map-fog mouseover indicator) over `CannotPlaceInUndiscovered`'s "desconhecidas" — vanilla itself is inconsistent between the two, `Inexplorado` is the nearer semantic analog (an unrevealed map tile, not merely "unknown") |
| deposit | depósito | Not vanilla-attested — vanilla itself avoids the word here (`GroundPenetratingScanner`/`LongRangeMineralScanner` descriptions say "resource"/"buried resource"; `depósito` elsewhere in Core means "warehouse", not "ore deposit"). English's own text already deviates from vanilla by choosing "deposit", so `depósito` is an ordinary, unambiguous literal translation rather than a coined term |
| combined scanning speed | velocidade de escaneamento combinada | `velocidade de escaneamento` grounded via `UserScanAbility`="Velocidade de escaneamento do usuário" |
| vanilla (the base game, in Workshop-description prose only) | padrão do jogo / jogo base | No vanilla pt-BR precedent for the modding-jargon "vanilla" (Core never refers to itself that way); avoided the English loanword in player-facing prose in favor of an ordinary phrase, flagged here since a different translator might reasonably keep "vanilla" as an accepted loanword instead |
| ore vein (Workshop description, "What it does" bullet) | veio de minério | Not vanilla-attested (vanilla's own scanner descriptions say "resource"/"buried resource", never "vein"); `veio` is the ordinary Portuguese geological term for a mineral vein/lode, chosen as the literal analog to English's own choice of "vein" over "deposit" in this one bullet |
| minifiable (Workshop description, "What it does" bullet) | miniaturizável | Not vanilla-attested in this file's sources; derived adjective from `miniaturizar`/`miniaturizado` (RimWorld's own minification mechanic), parallel formation to `sintonizável`-style coinages already implicit in this glossary |

## Grounding decisions worth double-checking

- `OperateScanner.reportString` in vanilla pt-BR (Core `Jobs_Work.xml`) ships **without** its
  trailing period ("escaneando em TargetA" vs the reportString norm of "gerund + period" that every
  other Core `JobDef` in the same file follows). Treated as a one-off vanilla slip and NOT mirrored;
  our own `OperateLocalMineralScanner.reportString` keeps the period.
- `LetterLabelAreaRevealed`="Área Revelada" (Title Case) was the closest vanilla analog for our own
  "Mineral deposit located" letter label — used as precedent for Title Case there.
- The ThingDef description reuses "sensor" (masculine) as its prose head noun rather than restating
  the (feminine) def label, mirroring vanilla's own habit of switching to an unrelated descriptive
  noun/gender in prose (Core's `GroundPenetratingScanner.description` calls itself "radar", masc.,
  despite its own label being feminine).
- `{0}` in `RequireUnroofedDesc`/`MinifiableDesc`/`FindMtbDaysDesc`/`FindGuaranteedDaysDesc` injects
  a vanilla building label (long-range mineral scanner and/or deep drill); both grounded pt-BR forms
  (escaneadora, broca) are feminine, so the fixed article "a {0}" is safe without a gender hedge.

## Pending native review

- The reordered "escaneadora local de minerais" / "escaneadoras locais de minerais" choice above —
  a native speaker should confirm it reads unambiguously and naturally in the build menu and work tab.
- "Padrão do jogo" / "jogo base" for Workshop-description "vanilla" — a native/community reviewer
  familiar with the Brazilian RimWorld modding scene may prefer keeping "vanilla" as a loanword.
- General machine-assisted-first-pass review of all four deliverables per `l10n/process.md`.
