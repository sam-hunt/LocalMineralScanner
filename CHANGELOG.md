# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

<!-- Convention: no [Unreleased] heading — each release adds its section at the top,
     directly below this intro, with a link reference at the bottom of the file.
     The release workflow lifts the tagged version's section into the GitHub release
     body verbatim and FAILS the release if the section is missing, so write the
     section before tagging. The /release skill walks through all of this. -->

## [1.1.0] - 2026-09-10

### Changed

- Faster default scans: 3 days average / 6 guaranteed, matching vanilla's ground-penetrating scanner.
- Scan-time sliders step in quarter days and tag both vanilla scanners' values.
- Scanner outlines evened out to match vanilla line weight.

## [1.0.0] - 2026-09-09

### Added

- Pawn-operated 2x2 building that reveals undiscovered mineral deposits on the current map.
- Reveals one deposit per find, fully hidden deposits before partially exposed ones.
- Minifiable default, so a scanner can be uninstalled and moved.
- Tunable to every ore the map holds: vanilla, modded and asteroid ores alike.
- Targets ore types listed in order of most valuable first.
- Two operator seats; each pawn contributes their full research speed.
- Scanner inspect pane shows the combined scan speed.
- Same default per-find effort as the long-range mineral scanner (4/8 days).
- Stop message when the last undiscovered deposit is revealed.
- Exhausted minerals greyed/disabled in the tuning gizmo menu.
- Mod settings for the roof rule, uninstalling, scan times, build cost and mass.
- Settings apply when the window closes; no restart needed.
- Unlocked by the vanilla long-range mineral scanner research.
- Sits directly after the long-range mineral scanner in Architect > Misc.

[1.1.0]: https://github.com/sam-hunt/LocalMineralScanner/releases/tag/v1.1.0
[1.0.0]: https://github.com/sam-hunt/LocalMineralScanner/releases/tag/v1.0.0
