# PvZ Symbiosis Translator

An experimental MelonLoader translation framework for Plants vs. Zombies: Symbiosis, currently focused on Brazilian Portuguese.

## Status

Public beta candidate. Text translation, custom fonts, native settings, QA, diagnostics, and automatic texture mapping are implemented. Translation and localized visual coverage remain incomplete. Audio replacement is experimental.

## Compatibility

Tested on Windows x64, game 1.2.0, Unity 6000.0.41f1, IL2CPP, and MelonLoader 0.7.3. Other versions may not work.

## Features

- 1,735 exact PT-BR translations and 3 bounded dynamic rules
- context-aware translation with original-text fallback
- custom font and fallback support
- native five-page Settings UI with no additional Canvas
- QA, diagnostics, text capture/export, and auto reload
- translator-friendly PNG replacement by exact Texture2D filename
- explicit texture rules for ambiguous assets
- transactional last-known-good texture reload

## Installation

Obtain the game and MelonLoader separately. Download an approved project release, close the game, and copy its DLL/data tree into the documented `Mods` locations. Do not download builds from untrusted sources. Source checkouts require local game-generated references to compile.

## Usage

Use the Home **Idiomas** button or default keys: `Insert` toggles translation, `PageUp` reloads, and `PageDown` exports text diagnostics.

## Screenshots

Screenshots will be added only after they are reviewed for rights, current UI accuracy, and personal information.

## Development

Read `Docs/DEVELOPMENT_SETUP.md` and `Docs/BUILDING.md`. The public tree excludes game binaries and generated references, so provide your own installation.

## Translation Contributions

Read `CONTRIBUTING.md`, `Docs/LOCALIZATION.md`, and the style/glossary documents. Preserve exact sources, placeholders, and valid markup.

## Bug Reports and Pull Requests

Use the repository templates. Include versions, scene/context, reproduction steps, screenshots where safe, and a sanitized log excerpt.

## Current Limitations

Coverage is incomplete; large texture atlases use substantial memory; late-created graphics require lifecycle application; audio has no maintained acceptance pack; and compatibility is limited to the tested baseline.

## Disclaimer

This unofficial fan project is not affiliated with PopCap, EA, CherryGG, or other rights holders. It does not distribute the game, game binaries, generated game assemblies, or extracted asset dumps.

## Open Source

PvZ Symbiosis Translator is open source.

You are free to fork, modify, redistribute, port, sublicense, learn from, or incorporate the project's original source code into your own projects.

Community forks, modifications and contributions are welcome.

The original copyright and MIT license notice must be preserved.

See [LICENSE](LICENSE) for the complete license terms.

## Third-Party Content

The MIT License applies only to the original source code and original project-owned material contained in this repository.

Plants vs. Zombies, PvZ Symbiosis, their characters, artwork, textures, audio, trademarks and other original game content belong to their respective owners.

This project is an unofficial fan-made localization/mod and is not affiliated with or endorsed by the original rights holders.

The project license does NOT grant rights over third-party game assets.

## Credits

Original translator project by Xyll. Repository maintained by project contributors. Third-party notices are in `THIRD_PARTY.md`.
