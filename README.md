# PvZ Symbiosis Translator

A MultiLang translation mod for Plants vs. Zombies: Symbiosis, built with MelonLoader. Translations live in external language packs; Brazilian Portuguese (`pt-BR`) is currently the most complete pack.

The project is in public beta. Text translation is the most mature part of the mod. Translation coverage is incomplete, texture support is still being expanded, and audio replacement is experimental.

## Features

- Runtime text translation, with the original Chinese text as fallback
- Independent language packs and custom font support
- PNG texture replacements without editing game archives
- In-game settings, translation reload, and text export
- Localization QA and runtime diagnostics

## Installation

The supported baseline is **Windows x64, Symbiosis 1.2.0 (Unity 6000.0.41f1), and MelonLoader 0.7.3**. Other versions are unverified.

1. Install the game separately, then install [MelonLoader 0.7.3](https://github.com/LavaGang/MelonLoader/releases/tag/v0.7.3) for its executable. Launch the game once and close it.
2. Download the mod ZIP from [Releases](https://github.com/SheldonCastro/PvZ-Symbiosis-Translator-mod/releases), rather than GitHub's source-code archive.
3. Extract the ZIP. Copy `PvZ_Symbiosis_Translator.dll` and the `PvZ_Symbiosis_Translator` data folder into the game's `Mods` folder:

   ```text
   <game>/Mods/
   ├── PvZ_Symbiosis_Translator.dll
   └── PvZ_Symbiosis_Translator/
       ├── Config/
       └── Localization/
           └── pt-BR/
   ```

4. Start the game. Open **Idiomas** on the Home screen to choose a language and adjust settings.

When updating, back up your existing mod folder first, especially any edited language packs. Keep only one translator DLL installed. If the mod does not load, check `<game>/MelonLoader/Latest.log` and include the relevant excerpt in a [bug report](https://github.com/SheldonCastro/PvZ-Symbiosis-Translator-mod/issues/new/choose).

## Usage

| Default key | Action |
| --- | --- |
| `Insert` | Toggle translation |
| `PageUp` | Reload localization |
| `PageDown` | Export translation diagnostics |

Settings are stored in `Mods/PvZ_Symbiosis_Translator/Config/translation_config.json`. Texture replacement, audio replacement, and automatic reload are off by default.

## Language packs

The source packs are in [PvZ_Symbiosis_Translator/Localization](PvZ_Symbiosis_Translator/Localization). Copy `_template` to add a language; no DLL rebuild is needed. See [Localization](Docs/LOCALIZATION.md), the [PT-BR style guide](Docs/PTBR_STYLE_GUIDE.md), and [Textures](Docs/TEXTURES.md).

## Contributing and development

Translation corrections, new packs, bug reports, and pull requests are welcome. Read [Contributing](CONTRIBUTING.md) for where to start, [Development](Docs/DEVELOPMENT.md) to build and test, or [Architecture](Docs/ARCHITECTURE.md) for a tour of the code.

## Credits and license

Maintained by **SheldonCastro (Xyll)**. Thanks to **厘子gg**, creator of Symbiosis, and **Teyliu / PVZFusionTranslation** for inspiration. See [Credits](CREDITS.md).

Project-owned source and material use the [MIT License](LICENSE). Game assets and other third-party material remain under their owners' terms; see [Third-party notices](THIRD_PARTY.md). This is an unofficial fan project, unaffiliated with the game’s rights holders.
