# Contributing

Thanks for your interest in improving Local Mineral Scanner! Bug reports,
suggestions and pull requests are welcome. Build and development setup live
in CLAUDE.md.

## Localization

The mod targets the languages below, chosen by RimWorld's per-language
audience size. Contributions for any other language RimWorld supports are
welcome too.

| Language             | Status           | Credit  |
| -------------------- | ---------------- | ------- |
| English              | Source           | —       |
| Simplified Chinese   | Machine-assisted | Fable 5 |
| Russian              | Machine-assisted | Fable 5 |
| Korean               | Machine-assisted | Fable 5 |
| German               | Machine-assisted | Fable 5 |
| Spanish              | Machine-assisted | Fable 5 |
| French               | Machine-assisted | Fable 5 |
| Brazilian Portuguese | Machine-assisted | Fable 5 |
| Japanese             | Machine-assisted | Fable 5 |
| Traditional Chinese  | Machine-assisted | Fable 5 |

Statuses: **Source** (the authoritative English strings), **Machine-assisted**
(generated with terminology grounded against the official RimWorld
localization; awaiting native review), **Native** (written or reviewed by a
native speaker), **Planned** (not started — contributions welcome).

Spanish here means Castilian (RimWorld's `Spanish` language folder). RimWorld also
ships a separate Latin American Spanish (`SpanishLatin`); a translation for it is
welcome as its own folder rather than as edits to this one.

Brazilian Portuguese likewise means RimWorld's `PortugueseBrazilian` folder. European
Portuguese (`Portuguese`) is a separate language folder in RimWorld, so a translation
for it is welcome in its own right rather than as edits to this one.

### Contributing a translation

- Files live under `1.6/Languages/<Language>/` (`Keyed/` and `DefInjected/`),
  mirroring the structure of `1.6/Languages/English/`. English has no
  DefInjected tree (the def XML serves it), so the DefInjected key set comes
  from `Scripts/expected-injections.json`: one `ThingDef`, one `JobDef` and
  one `WorkGiverDef`.
- Every translated entry carries the current English source in a comment
  directly above it, e.g. `<!-- EN: Guaranteed find within -->` — this is how
  stale translations are detected when the English changes.
- Placeholders (`{0}`, `{1}`, ...) must match the English exactly.
- Vanilla def types use bare DefInjected folder names (`ThingDef`, `JobDef`,
  `WorkGiverDef`); the mod defines no Def classes of its own.
- Formatting: UTF-8 without BOM, LF line endings, 2-space indent.
- Validate before opening a PR:

  ```bash
  python3 Scripts/check-translations.py --strict
  ```

  It checks key coverage, placeholders, DefInjected paths, staleness, and
  file hygiene. The checker's engine lives in the `l10n/` git submodule, so
  clone with `git clone --recurse-submodules` (or run
  `git submodule update --init` in an existing clone) before validating.
