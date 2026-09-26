# Changelog

This file records verified project changes. Earlier private development history is summarized only where supported by Git and current checkpoint evidence.

## [Unreleased]

### Added

- Private-to-public repository promotion plan and documentation model.
- Allowlist-based public candidate preparation and validation workflow.
- Community issue/PR templates and contribution guides.
- Deterministic local public candidate manifests, archive generation, and safety scans.

### Changed

- Current developer, localization, texture, QA, diagnostics, and release documentation.

### Fixed

- Texture reload now retains the last working replacement during a transient invalid PNG save.
- Texture QA decodes PNG data and reports duplicate automatic/explicit identities and unsupported files.
- Replacement textures preserve source filter, wrap, and anisotropic settings.
- Texture ownership has explicit shutdown cleanup and quieter normal logging.
- Legacy UI/export shortcut hints now match Insert, PageUp, and PageDown.

### Validation

- Final Release build: zero warnings and zero errors.
- Automated suite: 159 passing tests.
- Real-game texture probe: Image, RawImage, SpriteRenderer, geometry/sampling preservation, valid/invalid reload, readback, restoration, and zero translator Canvas passed.
- Local public candidate: 167 files; forbidden-content scan passed; content digest and ZIP SHA-256 reproduced on consecutive builds; no push performed.

## [1.1.0] - 2026-09-26

### Added

- Native five-page Translator Settings UI with zero translator-created Canvas objects.
- QA, Diagnostics, Translator Mode, capture/export, and auto reload.
- Sectioned PT-BR exact translations, custom font support, and texture localization vertical slice.

### Validation

- 1,735 exact translations and 3 dynamic rules.
- Home/Almanac/native UI lifecycle and texture replacement probes recorded in project documentation.

[Unreleased]: Docs/PROJECT_STATUS.md
[1.1.0]: Docs/PROJECT_STATUS.md
