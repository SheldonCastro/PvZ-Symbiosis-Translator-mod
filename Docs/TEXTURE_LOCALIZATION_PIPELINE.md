# Texture Localization Pipeline

This document records the implemented runtime architecture. Translator instructions are in [Textures](TEXTURES.md) and [Texture Contributions](TEXTURE_CONTRIBUTING.md).

## Candidate construction

`TextureStore.Load` reads the manifest, normalizes explicit rules, discovers unreferenced PNGs deterministically, hashes content, and decodes only changed hashes. Active dictionaries and component assignments remain untouched during candidate construction.

Manifest structure, duplicate IDs/source identities, duplicate automatic name-and-size identities, or explicit/automatic conflicts reject the candidate. Individual invalid changed PNGs retain the previous entry only when ID, relative path, and match identity are unchanged. New invalid files stay inactive. This provides per-entry last-known-good behavior without accepting an inconsistent manifest.

## Automatic and explicit rules

An automatic rule uses exact ordinal filename (without `.png`) and decoded width/height. Directories are organizational. Explicit manifest rules may use exact runtime metadata ID or texture name, dimensions, and optional sprite/scene/context qualifiers. Explicit mapping suppresses automatic discovery for that Texture2D name.

`TextureMatch.Resolve` accepts repeated components backed by one Texture2D atlas. It rejects a rule resolving to distinct Texture2D instances. If several mappings target one component, the component remains original.

## Activation

After validation, the store restores prior assignments, publishes candidate maps, retains referenced cache objects, and destroys only unused mod-owned textures. `Apply` inventories active game-owned Image, RawImage, and SpriteRenderer components and makes deterministic assignments.

New sprites preserve rect, normalized pivot, pixels-per-unit, border, and supported non-rectangular geometry. The original component remains in place, preserving Image type. Replacement textures inherit filter mode, wrap mode, and anisotropic level. Conflicting sampling profiles skip the affected assignment.

## Lifetime

Decoded textures carry `DontUnloadUnusedAsset` and `DontDestroyOnLoad` because they have no serialized scene owner. The store owns decoded replacements and created sprites; it never destroys game originals. `Restore` reverses assignments and destroys sprites. `Dispose` restores, destroys cached textures, clears mappings, and runs at mod deinitialization.

Scene initialization and explicit/auto reload are bounded apply points. No full-scene per-frame texture polling or broad new IL2CPP hook is used.

## QA and diagnostics

QA fully decodes supported RGB/RGBA 8-bit non-interlaced PNGs, checks paths/files/types, and detects explicit and automatic conflicts. Runtime remains the final Unity decoder check.

Diagnostics expose enabled state, explicit/automatic/total mappings, cached textures, created sprites, applied components, retained-last-good count, duration, and sanitized error text.

## Memory and performance

Content hashes avoid unchanged decode. Normal logging is a summary; byte/decode detail requires `debugLogging`. The safe IL2CPP byte-copy path remains byte-by-byte. Replacement `LoadImage` currently uses `markNonReadable=false`; changing it requires a real-game reload/readback matrix because large atlases trade CPU memory against diagnostic and compatibility behavior.

## Verified runtime slice

On 2026-09-26 the opt-in probe passed Image, RawImage, SpriteRenderer, sprite properties/geometry, source sampling, changed-file reload, invalid-save retention, restoration, and one non-readable original-texture readback. The deployed 4096x8192 `Home.png` loaded as one automatic mapping; its artwork is untranslated and excluded from source/public promotion.
