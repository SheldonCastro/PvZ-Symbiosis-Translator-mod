# Texture Localization

## Translator workflow

Put a complete PNG under `Localization/<locale>/Textures` with the exact runtime `Texture2D` name, preserving atlas width, height, and alpha. A file such as `Textures/UI/Home.png` maps automatically to `TextureName=Home` plus its decoded dimensions. Subfolders organize files and do not namespace identities.

Use `Textures/manifest.json` only for a metadata ID or a rule requiring sprite, scene, or hierarchy context. Explicit mappings take precedence over automatic discovery for the same texture name. Duplicate automatic name-and-size identities and duplicate explicit source identities are errors.

## Reload behavior

The store builds and validates a candidate before restoring active assignments. An invalid changed PNG with the same ID/path/match retains the last working texture. A malformed manifest or identity conflict rejects the whole candidate. Unchanged hashes reuse cached decoded textures.

## Runtime preservation

Replacement atlas dimensions must equal the source. New sprites preserve rect, pivot, pixels-per-unit, border, and supported custom geometry. Replacement textures inherit filter mode, wrap mode, and anisotropic level. Conflicting sampling profiles for one shared replacement are skipped safely.

The mod restores game assignments before destroying only mod-owned sprites/textures. `Dispose` runs during deinitialization.

## Performance

A 4096x8192 RGBA32 atlas is about 128 MiB before considering both CPU and GPU residency. Current decode keeps replacement textures CPU-readable because non-readable mode has not passed the full reload/readback matrix. The managed-to-IL2CPP byte copy remains a measured optimization target; unsafe copy tricks are not used.

## Current boundary

`Home.png` in the deployed runtime is an untranslated technical vertical slice. It is not maintained source and must not be promoted. See [Texture Contributions](TEXTURE_CONTRIBUTING.md) and [Known Limitations](KNOWN_LIMITATIONS.md).
