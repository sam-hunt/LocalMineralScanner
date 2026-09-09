#!/usr/bin/env python3
# Pre-release integration smoke test: boots the real game once with
# LocalMineralScanner on a pinned minimal list where the baseline is a clean
# log, then classifies every logged error/warning by origin and fails on
# anything attributed to this mod. Thin shim over the shared engine in
# l10n/smoke/startup_smoke.py (see its header for mechanics and the
# BetterTradersGuild v1.1.0 CWTL incident this exists to catch).
#
# Run this before every release, with the game closed:
#   python3 Scripts/integration-smoke-test.py              # boot + scan
#   python3 Scripts/integration-smoke-test.py --no-launch  # rescan last log
#   python3 Scripts/integration-smoke-test.py --strict     # any error fails

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent / "l10n" / "smoke"))
import startup_smoke as engine  # noqa: E402

engine.REPO_ROOT = Path(__file__).resolve().parent.parent

engine.PACKAGE_ID = "samhunt.localmineralscanner"

# RATIONALE: this list is this repo's l10n CANONICAL_ACTIVE_MODS (Core only:
# no DLC, no Harmony, no siblings). Probe last (auto-quit).
engine.SMOKE_ACTIVE_MODS = [
    "ludeon.rimworld",
    "samhunt.localmineralscanner",
    "shunter.l10nprobe",
]

# Assembly/namespace name, log prefix, and the Keyed-key prefix. The
# "[Local Mineral Scanner]" display-name pattern is derived from About.xml
# by the engine.
engine.OWN_PATTERNS = ["LocalMineralScanner", "[LocalMineralScanner]", "LocalMineralScanner_"]

# No cross-mod seams: the mod reads only vanilla (GenStep_PreciousLump,
# Verse.MapEvents) and nothing reads it back.
engine.INTEGRATION_PATTERNS = {}

raise SystemExit(engine.main())
