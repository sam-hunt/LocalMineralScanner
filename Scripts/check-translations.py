#!/usr/bin/env python3
# LocalMineralScanner's config shim over the shared translation checker
# (l10n/checker/check_translations.py — the rimworld-l10n submodule). The
# engine holds all logic; this file holds only this repo's config and the
# rationale behind it. Usage is unchanged:
#   python3 Scripts/check-translations.py [--strict] [--root PATH]
# If l10n/ is empty, run: git submodule update --init

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent / "l10n" / "checker"))
import check_translations as engine  # noqa: E402  (import after sys.path edit)

engine.REPO_ROOT = Path(__file__).resolve().parent.parent

# No [TranslationCanChangeCount]-style matching-token fields in this repo:
# the surface is a settings window, a letter, a message, two inspect
# fragments, and the building/job/work-giver def fields.
engine.PARITY_EXEMPT_FIELDS = set()

# RATIONALE: no DLC is required or MayRequire-gated. The mod is Core-only
# (LoadFolders.xml has no IfModActive roots, no def carries MayRequire); it
# merely loadAfter's the DLCs so its def writes land last. An empty set is
# the correct spelling of "no DLC required" (None is not legal here).
engine.REQUIRED_DLCS = set()

# The mod's only def types are vanilla ThingDef / JobDef / WorkGiverDef; the
# building's C# subclass (Building_LocalMineralScanner) is a thingClass, not
# a Def subclass, so nothing dumps under a namespace-qualified name.
engine.DEF_TYPE_ALIASES = {}

# This mod ships a real Keyed surface (1.6/Languages/English/Keyed/
# LocalMineralScanner.xml), so a missing Languages/ tree is a hard config
# error, not a legal state.
engine.ALLOW_NO_KEYED_SURFACE = False

# The localized Steam Workshop title lives in this Keyed key (the
# settings-window header); the checker enforces the title-coupling rule
# against each .steamworkshop/Description/<Language>.txt title line.
engine.WORKSHOP_TITLE_KEY = "LocalMineralScanner_SettingsCategory"

raise SystemExit(engine.main())
