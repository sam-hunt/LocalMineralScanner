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
  speed. The inspect pane shows the combined speed and time to a guaranteed find.
- Half the per-find effort of the long-range mineral scanner (2/4 days vs 4/8).
- Exhaustion handling: when no undiscovered deposits of the tuned mineral remain the scanner
  idles with its progress kept, the inspect pane says why, the letter for the last deposit
  says it was the last, and the tuning menu greys out exhausted minerals.
- Unlocked by the vanilla long-range mineral scanner research. No Harmony, no DLC.

[0.1.0]: https://github.com/sam-hunt/LocalMineralScanner/releases/tag/v0.1.0
