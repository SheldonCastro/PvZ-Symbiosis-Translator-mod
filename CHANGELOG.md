# Changelog

## Unreleased

### Changed

- Consolidated contributor documentation and clarified installation instructions.
- Shortened issue forms and the pull request checklist.
- Removed unused UI code and reduced asset-loading log noise.

### Fixed

- Included the zero-Canvas guard required by the build script and CI.

## 1.1.0 — 2026-09-26

Released as [v1.1.0-beta.1](https://github.com/SheldonCastro/PvZ-Symbiosis-Translator-mod/releases/tag/v1.1.0-beta.1).

### Added

- Native Translator Settings with General, Content, Translator, QA, and Diagnostics pages.
- Translator Mode, text capture/export, and automatic reload.
- External PT-BR language pack and custom font support.
- Automatic and explicit PNG texture replacement.

### Fixed

- Texture reload keeps the previous image during a temporarily invalid PNG save.
- Texture QA detects unsupported PNGs and conflicting mappings.
- Replacement textures preserve source sampling settings and release owned objects on shutdown.
- Shortcut hints match Insert, PageUp, and PageDown.
