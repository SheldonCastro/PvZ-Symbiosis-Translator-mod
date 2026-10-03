# Contributing

Forks and pull requests are welcome. Translation corrections, new language packs, texture artwork, bug fixes, and compatibility reports all help. Keep each change focused so it is easy to review.

Use [Issues](https://github.com/SheldonCastro/PvZ-Symbiosis-Translator-mod/issues/new/choose) for bugs, translation suggestions, or feature ideas. Include the affected screen and game/mod versions; screenshots are useful for layout problems. Remove personal paths and unrelated data from log excerpts.

## Translations and textures

Read [Localization](Docs/LOCALIZATION.md) before editing a pack. Preserve Chinese source keys and functional placeholders exactly. For PT-BR, follow the [style guide](Docs/PTBR_STYLE_GUIDE.md) and pack glossary. Explain the source, proposed translation, and context in your PR, then validate the pack and preview the screen.

Follow [Textures](Docs/TEXTURES.md) for image replacements. Preserve atlas dimensions and alpha, and include a screenshot and the artwork's provenance. Only submit material you may distribute; keep game binaries, generated assemblies, and extracted asset dumps out of the repository.

## Code

See [Development](Docs/DEVELOPMENT.md) for setup, standalone tests, and deployment. With a local game installation:

```powershell
./build.ps1 -Configuration Release -GameDir 'C:\Games\PvZ_Symbiosis'
```

Add tests for changed behavior and update the relevant guide when needed. Preserve exact matching and the native UI's zero-Canvas rule. For runtime hooks, fonts, textures, or UI changes, describe what you tested in-game and any checks still needed.
