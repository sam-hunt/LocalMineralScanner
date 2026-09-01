---
name: release
description: Prepare and publish a versioned release — version bumps, changelog, build, commit, tag, push
disable-model-invocation: true
argument-hint: "[major|minor|patch]"
---

# Release

Prepare and publish a new release for LocalMineralScanner.

The user may pass a bump type as `$ARGUMENTS` (one of `major`, `minor`, or `patch`). If omitted, ask which bump type they want (at step 3, where the version is first needed).

> **Template note:** this is the trimmed template version of the ecosystem's release
> skill. Released sibling mods (e.g. UniqueMeleeWeapons) insert three more gates
> between build and version-bump once the machinery exists: a translation-expectations
> refresh + checker run (`l10n/` submodule + `Scripts/` shims), a Steam Workshop
> description sync (`.steamworkshop/Description/<Language>.txt`), and a startup smoke
> test (`Scripts/integration-smoke-test.py`). When this mod grows translations or a
> Workshop page, port those steps back from a sibling's release skill.

## Current state

!`git describe --tags --abbrev=0 2>/dev/null || echo "no tags found"`
!`git log "$(git describe --tags --abbrev=0 2>/dev/null || echo 'HEAD~10')..HEAD" --oneline --no-merges`

## Steps

Work through the steps below in order. The release decision — version,
changelog, tag — happens once, at step 3, after everything that can still
change the history. Step 3 is the single release gate; nothing else asks.

### 1. Review changes

The commit log since the last tag is shown above — read it now to understand
what this release contains. If the repo has no tags yet this is the first
release: use the full history (`git log --oneline --no-merges`) and think in
terms of the mod's shipped feature set rather than a diff. No confirmation —
this is orientation, not a decision.

### 2. Clean build and deploy

Run:
```bash
dotnet clean LocalMineralScanner.sln
dotnet build LocalMineralScanner.sln -c Release
```

Report the build result. If the build fails, stop and help the user fix it.

### 3. Version, changelog, and the single release confirmation

Do all of the following, then present it as **one** confirmation:

- Read the current version from `About/About.xml` (`<modVersion>`) and
  calculate the new version from the bump type (`$ARGUMENTS`, or ask now).
- Draft changelog notes from the full log since the last tag, grouped by
  category (Fixes, Features, Polish/Other), omitting chore/version-bump
  commits.
- Update `CHANGELOG.md`: new `## [X.Y.Z] - YYYY-MM-DD` section at the top,
  directly below the Keep a Changelog intro paragraph, using today's date
  (this changelog carries no `[Unreleased]` heading; don't add one), plus a
  `[X.Y.Z]: https://github.com/<owner>/<repo>/releases/tag/vX.Y.Z`
  link reference at the bottom, above any older ones.
- Bump the version string in both files: `About/About.xml`
  (`<modVersion>`), `Source/1.6/Properties/AssemblyInfo.cs`
  (`AssemblyVersion` and `AssemblyFileVersion`, four-part `X.Y.Z.0`).
- Show the user, together: current version → new version (and bump type),
  the changelog notes, the full diff of all three files, and exactly what
  step 4 will do (rebuild, commit `chore: Bump version to X.Y.Z`, tag
  `vX.Y.Z`, push with tags).
- **Ask the user to confirm — this is the only release confirmation.** On
  edits, apply them and re-show only what changed.

### 4. Rebuild, commit, tag, push

No further questions unless something is unexpected:

- Rebuild (`dotnet build LocalMineralScanner.sln -c Release`) so the deployed
  DLL carries the bumped `AssemblyVersion`. Stop on failure.
- Stage only the release files: `About/About.xml`,
  `Source/1.6/Properties/AssemblyInfo.cs`, `CHANGELOG.md`. If
  other tracked files are modified, list them and ask whether to include
  them (the one conditional exception).
- Commit with message: `chore: Bump version to X.Y.Z`
- Tag with: `vX.Y.Z`
- Push: `git push && git push --tags`
- Show `git log --oneline -3` and `git tag -l 'v*' --sort=-v:refname | head -5`.
  The **GitHub** release notes need no paste: the tag-triggered workflow lifts
  this version's `CHANGELOG.md` section into the release body itself (and
  hard-fails the release if the section is missing), so the changelog entry
  written at step 3 is the release body.
