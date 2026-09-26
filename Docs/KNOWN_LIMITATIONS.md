# Known Limitations

- The pt-BR pack is partial; entry counts do not prove whole-game coverage.
- Compatibility is verified only for the documented game/Unity/MelonLoader baseline.
- Legacy `TextMesh` enumeration is unavailable in this game's IL2CPP bindings; TMP and Unity UI Text are supported.
- Texture names/dimensions must identify one runtime Texture2D. Ambiguous assets require context or metadata IDs.
- Automatic texture subfolders do not create namespaces.
- Very large RGBA32 atlases can consume substantial CPU/GPU memory.
- Replacement decoding remains CPU-readable pending a full non-readable compatibility test.
- Managed PNG bytes are copied into an IL2CPP array byte by byte; this is safe but expensive for large files.
- Sampling conflicts for a shared replacement are skipped.
- Late-created graphics require an established apply/lifecycle event; no global per-frame scan is used.
- Runtime catalog/source availability checks cannot be proven by offline pack QA alone.
- Audio replacement has no maintained acceptance pack.
- No source license has been selected. ContinuumBold and derivative visual assets require clearance before public inclusion.
- `Home.png` is an untranslated technical test and is not a release asset.
