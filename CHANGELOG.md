# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

<!-- Convention: no [Unreleased] heading — each release adds its section at the top,
     directly below this intro, with a link reference at the bottom of the file.
     The release workflow lifts the tagged version's section into the GitHub release
     body verbatim and FAILS the release if the section is missing, so write the
     section before tagging. The /release skill walks through all of this. -->

## [0.1.0] - TBD

### Added

- Local mineral scanner: a minifiable, pawn-operated 2x2 building that reveals one
  contiguous undiscovered deposit of its tuned mineral per find, on the current map.
  Fully hidden deposits are found first; partially exposed ones are the fallback.
- Tunable target (gold, silver, steel, plasteel, components, uranium, jade), matching the
  long-range mineral scanner. A newly built scanner starts on gold, or on the most valuable
  mineral still hidden on the map when no gold remains.
- Two operator seats: two pawns can scan at once, each contributing their full research
  speed. The inspect pane shows the combined speed.
- Same per-find effort as the long-range mineral scanner (4/8 days).
- Exhaustion handling: when no undiscovered deposits of the tuned mineral remain the scanner
  idles with its progress kept, the find that reveals the last deposit says so in a message,
  the inspect pane says why, and the tuning menu greys out exhausted minerals.
- Mod settings: toggles for the roof rule and for uninstalling, sliders for the random find
  interval, the guaranteed-find time, the build cost and the mass. Applied when the settings
  window closes; no restart needed.
- Unlocked by the vanilla long-range mineral scanner research. No Harmony, no DLC.
- Sits directly after the long-range mineral scanner in the Architect > Misc tab.

[0.1.0]: https://github.com/sam-hunt/LocalMineralScanner/releases/tag/v0.1.0
