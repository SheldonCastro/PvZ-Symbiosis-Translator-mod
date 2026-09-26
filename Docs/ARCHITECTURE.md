# Architecture

The translator is a MelonLoader mod for Unity IL2CPP. It keeps maintained files outside game archives and applies changes at runtime.

## Flow

1. `ModPaths` resolves the deployed mod root.
2. `ConfigManager` and `LocaleManager` build a validated localization candidate.
3. `TranslationService` resolves exact ordinal entries, context overrides, then bounded dynamic rules.
4. Harmony/game lifecycle integration refreshes supported TMP and Unity UI Text components while retaining original source text.
5. `FontStore`, `TextureStore`, and `AudioStore` manage optional assets independently so one optional subsystem cannot invalidate text translation.
6. Native UI integration clones approved game controls below an existing game Canvas. It never creates a Canvas.
7. QA validates source packs; Diagnostics describes observed runtime state; exports write translator-owned data below the deployed mod root.

## State ownership

Game textures, sprites, objects, and canvases remain game-owned. The mod owns decoded replacement textures, replacement sprites, callbacks, reports, and caches. Texture cleanup restores assignments before destroying only mod-owned objects.

## Reload guarantees

Text/config activation is candidate-based. Texture reload builds a candidate mapping without mutating active assignments. Invalid changed PNGs retain the matching prior ID/path/source mapping; structural manifest or identity conflicts reject the pack candidate.

## Compatibility boundary

Generated IL2CPP and Unity assemblies are referenced from the user's game installation and are not redistributed. Runtime behavior is verified for the versions in [Project Status](PROJECT_STATUS.md); other versions require validation.
