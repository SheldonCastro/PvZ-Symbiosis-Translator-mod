# Public Release Tools

`prepare_public_repo.ps1` creates a fresh local public candidate from an explicit committed private revision. It uses `git archive`, copies only the versioned allowlist, removes excluded/private assets, sanitizes local-only font entries, promotes the public README/roadmap, scans the result, writes source/release manifests, and creates a deterministic ZIP.

`validate_public_snapshot.ps1` can rescan any candidate. `generate_release_notes.py` drafts notes only from Git subjects and the maintained changelog. No tool configures a remote, commits, pushes, or publishes.

Run from a clean private tree after tests/build/runtime validation:

```powershell
./Tools/PublicRelease/prepare_public_repo.ps1 -SourceCommit HEAD -Version 1.1.0
```

The command stops at a local candidate. Read `PUBLIC_RELEASE_MANIFEST.md`, review the largest files and scan output, resolve blockers, and follow `Docs/PUBLIC_RELEASE_CHECKLIST.md`. Publication always requires explicit owner approval and manual Git/release actions.
